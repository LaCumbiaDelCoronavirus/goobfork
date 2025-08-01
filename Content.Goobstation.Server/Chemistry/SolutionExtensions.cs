using System.Linq.Expressions;
using System.Reflection;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Goobstation.Server.Plumbing.Extensions;

/// <summary>
///     Various extensions for solutions because the original methods pissed me off.
/// </summary>
public static class SolutionExtensions
{
    // Exception-farm 2025 if someone changes this
    private static readonly PropertyInfo HeatCapacityProperty = typeof(Solution).GetProperty("_heatCapacity", BindingFlags.Instance | BindingFlags.NonPublic)!
        ?? throw new InvalidOperationException("Couldn't find private field `_heatCapacity` on type `Solution`, was it renamed to something else?");

    private static readonly ParameterExpression HCRS_Sol = Expression.Parameter(typeof(Solution));
    private static readonly ParameterExpression HCRS_Hc = Expression.Parameter(typeof(float));

    /// <summary>Action that sets a solution's heat capacity via reflection.</summary>
    public static readonly Action<Solution, float> ForcedSetHeatCapacity = Expression.Lambda<Action<Solution, float>>(
        Expression.Call(HCRS_Sol, HeatCapacityProperty.GetSetMethod(true)!, HCRS_Hc), HCRS_Sol, HCRS_Hc
    ).Compile();

    /// <summary>Action that gets a solution's heat capacity via reflection; make sure the heatcap is updated!</summary>
    public static readonly Func<Solution, float> ForcedGetHeatCapacity = Expression.Lambda<Func<Solution, float>>(
        Expression.Call(HCRS_Sol, HeatCapacityProperty.GetGetMethod(true)!), HCRS_Sol
    ).Compile();

    /// <summary>
    ///     Equivalent to <see cref="Solution.SplitSolution(FixedPoint2)"/>.
    ///         However, does not change the solution being split.
    /// </summary>
    /// <remarks>
    ///     Cheaper performance-wise.
    /// </remarks>
    public static Solution CopySplitSolution(this Solution solution, FixedPoint2 toTake, IPrototypeManager? prototypeManager = null)
    {
        if (toTake <= FixedPoint2.Zero)
            return new Solution();

        if (toTake >= solution.Volume)
            return solution.Clone();

        var effVol = solution.Volume.Value;
        var newSolution = new Solution(solution.Contents.Count) { Temperature = solution.Temperature };

        var remaining = (long) toTake.Value;
        FixedPoint2 taken = 0;

        for (var i = solution.Contents.Count - 1; i >= 0; --i) // iterate backwards because of remove swap.
        {
            var (reagent, quantity) = solution.Contents[i];

            // This is set up such that integer rounding will tend to take more reagents.
            var split = remaining * quantity.Value / effVol;
            effVol -= quantity.Value;

            if (split <= 0)
            {
                DebugTools.Assert(split == 0, "Negative solution quantity while splitting? Long/int overflow?");
                continue;
            }

            var splitInVolume = FixedPoint2.FromCents((int) split);
            newSolution.Contents.Add(
                new ReagentQuantity(
                    reagent,
                    splitInVolume)
            );

            remaining -= split;
            taken += splitInVolume;
        }

        newSolution.MaxVolume = taken;
        newSolution.Volume = taken;

        DebugTools.Assert(remaining >= 0);
        DebugTools.Assert(remaining == 0 || solution.Volume == FixedPoint2.Zero);

        newSolution.UpdateHeatCapacity(prototypeManager);
        return newSolution;
    }

    /// <summary>
    ///     Scales the amount of solution, however does not validate it.
    ///         Instead, manually scales the heatcapacity according to
    ///         the given <paramref name="scale"/>.
    /// </summary>
    /// <remarks>
    ///     As `_heatCapacity` of <see cref="Solution"/> is private,
    ///         reflection is used in this method to not need to index
    ///         an arbitrary number of <see cref="ReagentPrototype"/>s.
    ///
    ///     However, this means that you must make sure that the solution's
    ///         heat capacity is already properly updated before calling this,
    ///         or dirty and re-calculate heatcapacities for this afterwards.
    /// </remarks>
    /// <param name="scale">The scalar to modify the solution by.</param>
    public static void ScaleSolutionAndHeatCapacity(this Solution solution, float scale)
    {
        if (scale == 1)
            return;

        if (scale == 0)
        {
            solution.RemoveAllSolution();
            return;
        }

        solution.Volume = FixedPoint2.Zero;
        ref List<ReagentQuantity> solutionContents = ref solution.Contents;

        for (int i = solutionContents.Count - 1; i >= 0; --i)
        {
            var old = solutionContents[i];

            // What the fuck? Why isn't this just `old.Volume`? I won't question it though because SURELY there's a good reason for it.
            var newQuantity = old.Quantity * scale;

            if (newQuantity == FixedPoint2.Zero)
                solutionContents.RemoveSwap(i);
            else
            {
                solutionContents[i] = new ReagentQuantity(old.Reagent, newQuantity);
                solution.Volume += newQuantity;
            }
        }

        ForcedSetHeatCapacity(solution, ForcedGetHeatCapacity(solution) * scale);
    }
}
