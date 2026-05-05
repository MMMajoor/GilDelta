using System.Collections.Generic;
using GilDelta.Events;
using GilDelta.Events.Rules;
using GilDelta.Wallet;

namespace GilDelta.Tests.Events;

public class RuleChainTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    private static Inferrer FullChain() => new(new IInferenceRule[]
    {
        new PairedTransferRule(),
        new SubmarineReturnRule(),
        new RetainerSaleRule(),
        new NpcShopBuyRule(),
        new NpcShopSellRule(),
        new MarketBoardBuyRule(),
        new RepairRule(),
        new TeleportRule(),
        new MiscRule(),
    });

    [Fact]
    public void Retainer_deposit_is_caught_by_PairedTransferRule_not_RetainerSaleRule()
    {
        // If RuleChainTests fails here, rule order is wrong.
        var inferrer = FullChain();
        var selfId = new WalletId(WalletKind.Self, "");
        var retId  = new WalletId(WalletKind.Retainer, "Yui");
        var ctx = new GameContext(
            new HashSet<string>(),
            new[] { new WalletDiff(selfId, 500_000, 0, T0) },
            T0);
        var diff = new WalletDiff(retId, 0, 500_000, T0);

        var ev = inferrer.Classify(diff, ctx);

        Assert.Equal(GilEventCategory.RetainerDeposit, ev.Category);
    }

    [Fact]
    public void Submarine_return_classification_end_to_end()
    {
        var inferrer = FullChain();
        var diff = new WalletDiff(
            new WalletId(WalletKind.FreeCompanyChest, "yagi2 FC"),
            18_000_000, 18_450_000, T0);

        var ev = inferrer.Classify(diff, GameContext.Empty(T0));

        Assert.Equal(GilEventCategory.SubmarineReturn, ev.Category);
    }

    [Fact]
    public void Standalone_retainer_increase_classifies_as_RetainerSale()
    {
        var inferrer = FullChain();
        var diff = new WalletDiff(
            new WalletId(WalletKind.Retainer, "Aki"), 0, 412_800, T0);

        var ev = inferrer.Classify(diff, GameContext.Empty(T0));

        Assert.Equal(GilEventCategory.RetainerSale, ev.Category);
    }

    [Fact]
    public void Self_decrease_in_NPC_shop_classifies_as_NpcShopBuy()
    {
        var inferrer = FullChain();
        var diff = new WalletDiff(
            new WalletId(WalletKind.Self, ""), 100_000, 50_000, T0);
        var ctx = new GameContext(new HashSet<string> { "Shop" }, Array.Empty<WalletDiff>(), T0);

        var ev = inferrer.Classify(diff, ctx);

        Assert.Equal(GilEventCategory.NpcShopBuy, ev.Category);
    }

    [Fact]
    public void Random_self_change_with_no_addons_falls_through_to_Misc()
    {
        var inferrer = FullChain();
        var diff = new WalletDiff(
            new WalletId(WalletKind.Self, ""), 0, 1_234, T0);

        var ev = inferrer.Classify(diff, GameContext.Empty(T0));

        Assert.Equal(GilEventCategory.Misc, ev.Category);
    }
}
