namespace Content.Goobstation.Server.Plumbing.Components;

[RegisterComponent]
public sealed partial class PlumbingOutputComponent : Component
{
    [DataField]
    public string InletName = "inlet";

    [DataField("solution"), ViewVariables(VVAccess.ReadWrite)]
    public string SolutionName = "tank";
}
