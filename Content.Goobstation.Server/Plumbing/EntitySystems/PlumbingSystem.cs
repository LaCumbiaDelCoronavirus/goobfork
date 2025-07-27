using System.Collections.Concurrent;
using System.Threading.Tasks;
using Content.Goobstation.Server.Plumbing.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server.Plumbing.EntitySystems;

public sealed class PlumbingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainerSystem = default!;

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

    // There is a little bit of arbitrary update order bias here. So instead we COULD:
    // 1. Go by every pipenet and make every machine push
    // 2. Go by every pipenet and cache the net's solution's FillFraction
    // 3. Go by every pipenet and make every machine pull, with the cached fillfraction
    // However, that leads to alot of overhead. So we do this instead.
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _updateAccumulator += frameTime;

        if (_updateAccumulator < UpdateInterval)
            return;

        _updateAccumulator = 0f;
        Process(frameTime);
    }

    public void Process(float frameTime)
    {
        _processingStopwatch.Restart();

        _netPulls.Clear();
        _netPushes.Clear();

        Parallel.ForEach(_plumbingNets, net =>
        {
            net.CachedFillFraction = net.Solution.FillFraction;
            net.CachedAvailableVolume = net.AvailableVolume;
        });

        var plumbingDeviceEnumerator = EntityQueryEnumerator<PlumbingDeviceComponent>();
        while (plumbingDeviceEnumerator.MoveNext(out var uid, out var plumbingDeviceComponent))
        {
            PlumbingDeviceProcessEvent processEvent = new();
            RaiseLocalEvent(uid, ref processEvent);
        }

        Log.Debug($"Took {_processingStopwatch.Elapsed.TotalMilliseconds}ms to process {_plumbingNets.Count} plumbingnets.");
    }
}
