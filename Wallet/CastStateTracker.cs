using System;

namespace GilDelta.Wallet;

/// <summary>
/// Rolling record of when the local player was last seen casting the Teleport
/// action. Mirrors <see cref="AddonStateTracker"/>: the wallet watcher detects
/// the gil deduction on the framework tick *after* the teleport cast completes,
/// by which point <c>IsCasting</c> has already flipped back to false (and the
/// player may even be mid-zone-transition with a null LocalPlayer). Stamping a
/// timestamp on every tick the cast is in progress lets the rule still match the
/// just-finished teleport when the diff arrives a tick or two later.
/// </summary>
public sealed class CastStateTracker
{
    private DateTimeOffset _lastTeleportCast = DateTimeOffset.MinValue;

    /// <summary>Record that the player is (or is not) casting Teleport this tick.</summary>
    public void Tick(bool castingTeleport)
    {
        if (castingTeleport)
            _lastTeleportCast = DateTimeOffset.Now;
    }

    /// <summary>True if a Teleport cast was seen within <paramref name="window"/>.</summary>
    public bool RecentlyCastTeleport(TimeSpan window)
        => DateTimeOffset.Now - _lastTeleportCast <= window;
}
