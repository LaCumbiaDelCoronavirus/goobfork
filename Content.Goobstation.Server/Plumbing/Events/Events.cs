namespace Content.Goobstation.Server.Plumbing;

/// <summary>
///     Event for a plumbing device to pull fluids from input pipenets,
///     and process them.
/// </summary>
[ByRefEvent]
public readonly record struct PlumbingDeviceProcessEvent(float DeltaTime);

/// <summary>
///     Event for a plumbing device to push a fixed amount of fluid and
///     nothing else.
/// </summary>
[ByRefEvent]
public readonly record struct PlumbingDevicePushEvent(float DeltaTime);
