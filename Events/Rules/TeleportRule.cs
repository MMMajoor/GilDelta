using System.Diagnostics.CodeAnalysis;
using GilDelta.Wallet;

namespace GilDelta.Events.Rules;

/// <summary>
/// Self -Δ that coincides with a recent Teleport cast → Teleport.
/// Keyed on the cast (action id 5) rather than the Teleport addon, because the
/// addon doesn't open for favorites / world-map / aetheryte-interaction flows,
/// whereas every paid teleport casts the Teleport action regardless of how it
/// was started. Return (action 8) is free and aethernet shards cost nothing, so
/// a Self -Δ within the recency window is essentially always a real teleport.
/// </summary>
public sealed class TeleportRule : IInferenceRule
{
    public bool TryClassify(WalletDiff diff, GameContext ctx, [NotNullWhen(true)] out GilEvent? ev)
    {
        ev = null;
        if (diff.Id.Kind != WalletKind.Self) return false;
        if (diff.Delta >= 0) return false;
        if (!ctx.RecentlyCastTeleport) return false;

        ev = new GilEvent(diff.At, diff.Id, diff.Delta, GilEventCategory.Teleport,
            "rule=TeleportRule; cast=Teleport");
        return true;
    }
}
