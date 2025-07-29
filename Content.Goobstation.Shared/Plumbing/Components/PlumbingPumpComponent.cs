using Content.Goobstation.Maths.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Plumbing.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PlumbingPumpComponent : Component
{
    public string InletName = "inlet";
    public string OutletName = "outlet";

    /// <summary>
    ///     The desired throughput of this pump in units.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Rate;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool Enabled = true;
}
