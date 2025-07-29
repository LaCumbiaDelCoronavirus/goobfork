using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Plumbing.Components;

/// <summary>
///     Component for plumbing devices that transition from one state to another,
///         with a state inbetween. With an animation usually. Because we have
///         alot of stuff that needs this..
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PlumbingTransitioningDeviceComponent : Component
{
    [DataField]
    public PlumbingDeviceState State = PlumbingDeviceState.Off;

    /// <summary>
    ///     The next time that this door will proceed to the next state,
    ///         if in an 'inbetween' state such as `ToOff` or `ToOn`.
    /// </summary>
    [AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? NextStateChange;
}

[Serializable, NetSerializable]
public enum PlumbingDeviceState : byte
{
    Off,
    ToOff,
    On,
    ToOn
}

[Serializable, NetSerializable]
public enum PlumbingDeviceVisuals : byte
{
    State
}
