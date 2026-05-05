using System.Diagnostics.CodeAnalysis;
using GilDelta.Wallet;

namespace GilDelta.Events.Rules;

public sealed class MarketBoardBuyRule : IInferenceRule
{
    public bool TryClassify(WalletDiff diff, GameContext ctx, [NotNullWhen(true)] out GilEvent? ev)
    {
        ev = null;
        if (diff.Id.Kind != WalletKind.Self) return false;
        if (diff.Delta >= 0) return false;
        if (!ctx.OpenAddons.Contains("ItemSearch")) return false;

        ev = new GilEvent(diff.At, diff.Id, diff.Delta, GilEventCategory.MarketBoardBuy,
            "rule=MarketBoardBuyRule; addons=[ItemSearch]");
        return true;
    }
}
