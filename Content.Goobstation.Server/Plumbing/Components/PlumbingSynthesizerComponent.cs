using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.Plumbing.Components;

[RegisterComponent]
public sealed partial class PlumbingSynthesizerComponent : Component
{
    public string OutletName = "outlet";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<ReagentPrototype> ProducedReagent = "Water";

    /// <summary>
    ///     The rate at which the <see cref="ProducedReagent"/> is synthesized.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Rate;
}
