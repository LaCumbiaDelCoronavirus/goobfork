using Content.Goobstation.Shared.Plumbing.Components;
using Content.Shared.Interaction;
using Content.Shared.NodeContainer;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Plumbing.EntitySystems;

public abstract class SharedPlumbingPumpSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedNodeContainerSystem _nodeContainerSystem = default!;
    [Dependency] private readonly PlumbingTransitioningDeviceSystem _transitioningDeviceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlumbingPumpComponent, ActivateInWorldEvent>(OnPumpActivate);
    }

    private void OnPumpActivate(Entity<PlumbingPumpComponent> entity, ref ActivateInWorldEvent args)
    {
        _transitioningDeviceSystem.TryTransitionStateToOpposite(entity.Owner);
    }
}
