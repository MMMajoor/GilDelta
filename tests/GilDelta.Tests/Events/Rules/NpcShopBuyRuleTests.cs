using System.Collections.Generic;
using GilDelta.Events;
using GilDelta.Events.Rules;
using GilDelta.Wallet;

namespace GilDelta.Tests.Events.Rules;

public class NpcShopBuyRuleTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    private static GameContext Ctx(params string[] addons) =>
        new(new HashSet<string>(addons), Array.Empty<WalletDiff>(), T0);

    [Fact]
    public void Self_decrease_with_Shop_addon_open_classifies_as_NpcShopBuy()
    {
        var rule = new NpcShopBuyRule();
        var diff = new WalletDiff(
            new WalletId(WalletKind.Self, ""), 100_000, 50_000, T0);

        var ok = rule.TryClassify(diff, Ctx("Shop"), out var ev);

        Assert.True(ok);
        Assert.Equal(GilEventCategory.NpcShopBuy, ev!.Category);
        Assert.Equal(-50_000, ev.Amount);
    }

    [Fact]
    public void InclusionShop_addon_also_matches()
    {
        var rule = new NpcShopBuyRule();
        var diff = new WalletDiff(
            new WalletId(WalletKind.Self, ""), 100_000, 80_000, T0);

        Assert.True(rule.TryClassify(diff, Ctx("InclusionShop"), out _));
    }

    [Fact]
    public void Self_increase_with_Shop_addon_does_not_match()
    {
        var rule = new NpcShopBuyRule();
        var diff = new WalletDiff(
            new WalletId(WalletKind.Self, ""), 100, 1_000, T0);

        Assert.False(rule.TryClassify(diff, Ctx("Shop"), out _));
    }

    [Fact]
    public void Self_decrease_without_Shop_addon_does_not_match()
    {
        var rule = new NpcShopBuyRule();
        var diff = new WalletDiff(
            new WalletId(WalletKind.Self, ""), 100_000, 50_000, T0);

        Assert.False(rule.TryClassify(diff, Ctx(), out _));
    }

    [Fact]
    public void Retainer_decrease_with_Shop_addon_does_not_match()
    {
        var rule = new NpcShopBuyRule();
        var diff = new WalletDiff(
            new WalletId(WalletKind.Retainer, "Yui"), 100, 50, T0);

        Assert.False(rule.TryClassify(diff, Ctx("Shop"), out _));
    }
}
