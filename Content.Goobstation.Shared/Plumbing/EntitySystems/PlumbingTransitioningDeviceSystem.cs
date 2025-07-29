using Content.Goobstation.Shared.Plumbing.Components;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Plumbing.EntitySystems;

public sealed class PlumbingTransitioningDeviceSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var trDeviceQuery = EntityQueryEnumerator<PlumbingTransitioningDeviceComponent>();
        while (trDeviceQuery.MoveNext(out var uid, out var deviceComponent))
        {
            if (deviceComponent.NextStateChange is not { } nextStateTime ||
                _gameTiming.CurTime < nextStateTime)
                continue;

            // Icl ts pmo
            ref PlumbingDeviceState state = ref deviceComponent.State;
            var oldState = state;

            state = state == PlumbingDeviceState.ToOn ? PlumbingDeviceState.On : PlumbingDeviceState.Off;
            if (oldState != state)
                UpdateState((uid, deviceComponent));
        }
    }

    /// <summary>
    ///     Used to set a device to a certain state. If in an inbetween state,
    ///         and <paramref name="time"/> is specified, will change to the
    ///         next proper state after that time. Otherwise just sets it etc..
    ///         Will resolve the entity's component.
    /// </summary>
    /// <param name="ignoreCurrent">
    ///     Whether to abort and return false if the device is already at the
    ///         specified state. Defaults to false.
    /// </param>
    /// <returns>
    ///     Whether the device state was changed. Always returns false if the
    ///         entity doesn't have <see cref="PlumbingTransitioningDeviceComponent"/>.
    /// </returns>
    public bool TrySetDeviceState(Entity<PlumbingTransitioningDeviceComponent?> device, PlumbingDeviceState state, TimeSpan? time, bool ignoreCurrent = false)
    {
        ref PlumbingTransitioningDeviceComponent? deviceComponent = ref device.Comp;
        if (!Resolve(device, ref deviceComponent, logMissing: false))
            return false;

        if (!ignoreCurrent && deviceComponent.State == state)
            return false;

        deviceComponent.State = state;
        deviceComponent.NextStateChange = time;

        return true;
    }

    /// <summary>
    ///     Just read the code, do i really have to explain this to you??!!
    ///         Resolves the component.
    /// </summary>
    public bool TryTransitionStateToOpposite(Entity<PlumbingTransitioningDeviceComponent?> device)
    {
        ref PlumbingTransitioningDeviceComponent? deviceComponent = ref device.Comp;
        if (!Resolve(device, ref deviceComponent, logMissing: false))
            return false;

        if (deviceComponent.State == PlumbingDeviceState.Off)
            deviceComponent.State = PlumbingDeviceState.ToOn;
        else if (deviceComponent.State == PlumbingDeviceState.On)
            deviceComponent.State = PlumbingDeviceState.ToOff;

        return true;
    }

    private void UpdateState(Entity<PlumbingTransitioningDeviceComponent> device)
        => _appearanceSystem.SetData(device, PlumbingDeviceVisuals.State, device.Comp.State);
}
