using Content.Goobstation.Maths.FixedPoint;
using Content.Goobstation.Shared.Plumbing.Components;
using Content.Goobstation.Shared.Plumbing.EntitySystems;
using Content.Goobstation.Server.Plumbing.Extensions;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Shared.NodeContainer;
using Robust.Shared.Prototypes;
using Content.Shared.Interaction;

namespace Content.Goobstation.Server.Plumbing.EntitySystems;

public sealed class PlumbingPumpSystem : SharedPlumbingPumpSystem
{
    [Dependency] private readonly NodeContainerSystem _nodeContainerSystem = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingPumpComponent, PlumbingDeviceProcessEvent>(PumpProcess);
    }

    private void PumpProcess(Entity<PlumbingPumpComponent> entity, ref PlumbingDeviceProcessEvent args)
    {
        var (owner, pumpComponent) = entity;
        if (!pumpComponent.Enabled ||
            !_powerReceiverSystem.IsPowered(entity) ||
            !_nodeContainerSystem.TryGetNodes(owner, pumpComponent.InletName, pumpComponent.OutletName, out PlumbingNode? inletNode, out PlumbingNode? outletNode) ||
            inletNode.NodeGroup is not PlumbingNet inputNet ||
            outletNode.NodeGroup is not PlumbingNet outputNet)
            return;

        // We can't pull more than is in the input net, or more than how much is in the output net.
        var pulled = FixedPoint2.Min(inputNet.Solution.Volume, pumpComponent.Rate, outputNet.AvailableVolume);
        if (pulled <= FixedPoint2.Zero)
            return;

        var taken = inputNet.Solution.CopySplitSolution(pulled);
        inputNet.QueueTransfer(taken, outputNet);
    }
}
