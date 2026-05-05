using System.Diagnostics.CodeAnalysis;
using System.Linq;
using GilDelta.Wallet;

namespace GilDelta.Events.Rules;

public sealed class NpcShopSellRule : IInferenceRule
{
    private static readonly string[] ShopAddons = { "Shop", "InclusionShop" };

    public bool TryClassify(WalletDiff diff, GameContext ctx, [NotNullWhen(true)] out GilEvent? ev)
    {
        ev = null;
        if (diff.Id.Kind != WalletKind.Self) return false;
        if (diff.Delta <= 0) return false;
        if (!ShopAddons.Any(ctx.OpenAddons.Contains)) return false;

        ev = new GilEvent(diff.At, diff.Id, diff.Delta, GilEventCategory.NpcShopSell,
            $"rule=NpcShopSellRule; addons=[{string.Join(",", ctx.OpenAddons)}]");
        return true;
    }
}
