using System;
using System.Collections.Generic;
using GilDelta.Wallet;

namespace GilDelta.Events;

public sealed class GameContext
{
    public IReadOnlySet<string> OpenAddons { get; }
    public IReadOnlyList<WalletDiff> RecentDiffs { get; }
    public DateTimeOffset Now { get; }

    public GameContext(
        IReadOnlySet<string> openAddons,
        IReadOnlyList<WalletDiff> recentDiffs,
        DateTimeOffset now)
    {
        OpenAddons  = openAddons;
        RecentDiffs = recentDiffs;
        Now         = now;
    }

    public static GameContext Empty(DateTimeOffset now) =>
        new(new HashSet<string>(), Array.Empty<WalletDiff>(), now);
}
