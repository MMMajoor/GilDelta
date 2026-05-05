using System.Collections.Generic;
using GilDelta.Events;
using GilDelta.Events.Rules;
using GilDelta.Wallet;

namespace GilDelta.Tests.Events.Rules;

public class NpcShopSellRuleTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;
    private static GameContext Ctx(params string[] addons) =>
        new(new HashSet<string>(addons), Array.Empty<WalletDiff>(), T0);

    [Fact]
    public void Self_increase_with_Shop_addon_classifies_as_NpcShopSell()
    {
        var rule = new NpcShopSellRule();
        var diff = new WalletDiff(
            new WalletId(WalletKind.Self, ""), 1_000, 5_000, T0);

        var ok = rule.TryClassify(diff, Ctx("Shop"), out var ev);

        Assert.True(ok);
        Assert.Equal(GilEventCategory.NpcShopSell, ev!.Category);
        Assert.Equal(4_000, ev.Amount);
    }

    [Fact]
    public void Self_decrease_does_not_match()
    {
        var rule = new NpcShopSellRule();
        var diff = new WalletDiff(
            new WalletId(WalletKind.Self, ""), 5_000, 1_000, T0);

        Assert.False(rule.TryClassify(diff, Ctx("Shop"), out _));
    }

    [Fact]
    public void No_Shop_addon_does_not_match()
    {
        var rule = new NpcShopSellRule();
        var diff = new WalletDiff(
            new WalletId(WalletKind.Self, ""), 1_000, 5_000, T0);

        Assert.False(rule.TryClassify(diff, Ctx(), out _));
    }
}
