using System;
using System.Collections.Generic;
using GilDelta.Wallet;

namespace GilDelta.Events;

public sealed class GameContext
{
    public IReadOnlySet<string> OpenAddons { get; }
    public IReadOnlyList<WalletDiff> RecentDiffs { get; }
    public DateTimeOffset Now { get; }

    /// <summary>True if the local player cast Teleport within the recency window
    /// at the time this diff was detected. Drives <c>TeleportRule</c>.</summary>
    public bool RecentlyCastTeleport { get; }

    public GameContext(
        IReadOnlySet<string> openAddons,
        IReadOnlyList<WalletDiff> recentDiffs,
        DateTimeOffset now,
        bool recentlyCastTeleport = false)
    {
        OpenAddons           = openAddons;
        RecentDiffs          = recentDiffs;
        Now                  = now;
        RecentlyCastTeleport = recentlyCastTeleport;
    }

    public static GameContext Empty(DateTimeOffset now) =>
        new(new HashSet<string>(), Array.Empty<WalletDiff>(), now);
}
