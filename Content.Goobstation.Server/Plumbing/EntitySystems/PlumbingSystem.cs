using System.Collections.Concurrent;
using System.Threading.Tasks;
using Content.Goobstation.Maths.FixedPoint;
using Content.Goobstation.Server.Plumbing.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server.Plumbing.EntitySystems;

public sealed class PlumbingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    // I know theres FixedPoint2.Zero but this is just for clarity.
    // Also they're readonly statics which ig are better than FixedPoint2's just statics.
    private static readonly FixedPoint2 Fp2Zero = FixedPoint2.Zero;
    // Why would anyone do this?!
    private static readonly FixedPoint2 Fp2One = FixedPoint2.New(1);


    private Stopwatch _processingStopwatch = new();

    private HashSet<PlumbingNet> _plumbingNets = new();

    // Value is what amount of fluid is pulled/pushed.
    private ConcurrentDictionary<PlumbingNet, Solution> _netPulls = new();
    private ConcurrentDictionary<PlumbingNet, Solution> _netPushes = new();

    private EntityQuery<PlumbingDeviceComponent> _plumbingDeviceQuery;

    public const float UpdateInterval = 1f;
    private float _updateAccumulator;

    public override void Initialize()
    {
        base.Initialize();

        _plumbingDeviceQuery = GetEntityQuery<PlumbingDeviceComponent>();
    }

    public bool AddPlumbingNet(PlumbingNet net)
        => _plumbingNets.Add(net);

    public bool RemovePlumbingNet(PlumbingNet net)
        => _plumbingNets.Remove(net);

    // On a top-level this is how it looks like:
    // 2. Go by every pipenet and cache the net's solution's FillFraction for whatever to use
    // 2. Go by every pipenet, and add to it the output of every applied machine
    // 3. Go by every pipenet, and steal from it according to every applied machine
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _updateAccumulator += frameTime;

        if (_updateAccumulator < UpdateInterval)
            return;

        var deltaTime = _updateAccumulator;
        _updateAccumulator = 0f;

        Process(deltaTime);
    }

    // TODO: Apply Ilya's nuclear subframe-killing solution to this.
    public void Process(float deltaTime)
    {
        _processingStopwatch.Restart();

        _netPulls.Clear();
        _netPushes.Clear();

        Parallel.ForEach(_plumbingNets, net =>
        {
            // Not like i care.
            net.QueuedInputs.Clear();
            net.QueuedTransfers.Clear();

            net.CachedVolume = net.Solution.Volume;
            net.CachedFillFraction = net.Solution.FillFraction;
        });

        var plumbingDeviceEnumerator = EntityQueryEnumerator<PlumbingDeviceComponent>();
        while (plumbingDeviceEnumerator.MoveNext(out var uid, out var plumbingDeviceComponent))
        {
            // I wonder if I can just raise this, not directed to any entity, and it wouldn't be race-condition-ops.
            PlumbingDeviceProcessEvent processEvent = new(deltaTime);
            RaiseLocalEvent(uid, ref processEvent);
        }

        // I AM THE GOD OF HELLFIRE
        // First just handle the thingamajigs that spawn in fluid from nowhere instead of transferring it.
        Parallel.ForEach(_plumbingNets, net =>
        {
            var c = net.QueuedInputs.Count;
            for (var i = 0; i < c; ++i)
                net.Solution.AddSolution(net.QueuedInputs[i], _prototypeManager);
        });

        // This handles fluid that this pipenet is losing.
        // help me.

        // This crits the update-order-trolling.
        foreach (var net in _plumbingNets)
        {
            var originallyAvailableVolume = net.AvailableVolume;

            var queuedTransfers = net.QueuedTransfers;
            var c = queuedTransfers.Count;

            float totalRequested = 0f;

            for (int i = 0; i < c; ++i)
                totalRequested += (float) queuedTransfers[i].MovedSolution.Volume;

            if (totalRequested <= 0)
            {
                Log.Debug("Skipped plumbingnet as no fluid was requested to move.");
                continue;
            }

            var fractionalVolumeFilled = (totalRequested > 0f) ? MathF.Min(1f, originallyAvailableVolume / totalRequested) : 0f;
            for (int i = 0; i < c; ++i)
            {
                var transfer = queuedTransfers[i];
                var transferredSolution = transfer.MovedSolution;
                transferredSolution.ScaleSolution(fractionalVolumeFilled);

                // Uh good enough.
                net.Solution.SplitSolution(transferredSolution.Volume);
                if (transfer.TargetSolution is { } target)
                    target.AddSolution(transferredSolution, _prototypeManager);
            }
        }

        Log.Debug($"Took {_processingStopwatch.Elapsed.TotalMilliseconds}ms to process {_plumbingNets.Count} plumbingnets.");
    }
}
