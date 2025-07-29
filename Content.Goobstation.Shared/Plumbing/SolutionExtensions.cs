using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Utility;

namespace Content.Goobstation.Server.Plumbing.Extensions;

public static class SolutionExtensions
{
    /// <summary>
    ///     Equivalent to <see cref="Solution.SplitSolution(FixedPoint2)"/>.
    ///         However, does not change the solution being split.
    /// </summary>
    /// <remarks>
    ///     Cheaper performance-wise, and has no debug asserts.
    /// </remarks>
    public static Solution CopySplitSolution(this Solution solution, FixedPoint2 toTake)
    {
        if (toTake <= FixedPoint2.Zero)
            return new Solution();

        Solution newSolution;

        if (toTake >= solution.Volume)
            return solution.Clone();

        var origVol = solution.Volume;
        var effVol = solution.Volume.Value;

        newSolution = new Solution(solution.Contents.Count) { Temperature = solution.Temperature };
        var remaining = (long) toTake.Value;

        for (var i = solution.Contents.Count - 1; i >= 0; i--) // iterate backwards because of remove swap.
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

            newSolution.Contents.Add(
                new ReagentQuantity(
                    reagent,
                    FixedPoint2.FromCents((int) split))
            );
            remaining -= split;
        }

        newSolution.Volume = origVol - solution.Volume;

        DebugTools.Assert(remaining >= 0);
        DebugTools.Assert(remaining == 0 || solution.Volume == FixedPoint2.Zero);

        newSolution.UpdateHeatCapacity(null);
        return newSolution;
    }
}
