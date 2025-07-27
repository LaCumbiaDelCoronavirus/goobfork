
using Content.Goobstation.Maths.FixedPoint;
using Content.Goobstation.Server.Plumbing.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.Plumbing.EntitySystems;

public sealed class PlumbingSynthesizerSystem : EntitySystem
{
    [Dependency] private readonly NodeContainerSystem _nodeContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlumbingSynthesizerComponent, PlumbingDeviceProcessEvent>(SynthesizerProcess);
    }

    private void SynthesizerProcess(Entity<PlumbingSynthesizerComponent> entity, ref PlumbingDeviceProcessEvent args)
    {
        var (owner, synthesizerComponent) = entity;
        if (!_nodeContainerSystem.TryGetNode(owner, synthesizerComponent.OutletName, out PlumbingNode? node) ||
            node.NetSolution is not { } netSolution)
            return;

        netSolution.AddReagent(synthesizerComponent.ProducedReagent, FixedPoint2.Min(netSolution.MaxVolume - netSolution.Volume, synthesizerComponent.Rate));
    }
}

public sealed class PlumbingOutputSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainerSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlumbingOutputComponent, PlumbingDeviceProcessEvent>(OutputProcess);
    }

    private void OutputProcess(Entity<PlumbingOutputComponent> entity, ref PlumbingDeviceProcessEvent args)
    {
        var (owner, outputComponent) = entity;

        if (!_nodeContainerSystem.TryGetNode(owner, outputComponent.InletName, out PlumbingNode? node) ||
            node.NetSolution == null ||
            !_solutionContainerSystem.TryGetSolution(owner, outputComponent.SolutionName, out _, out var solution))
            return;

        solution.AddSolution(node.NetSolution, _prototypeManager);
    }
}
