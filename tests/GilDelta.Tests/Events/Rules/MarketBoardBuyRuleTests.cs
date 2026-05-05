using System.Collections.Generic;
using GilDelta.Events;
using GilDelta.Events.Rules;
using GilDelta.Wallet;

namespace GilDelta.Tests.Events.Rules;

public class MarketBoardBuyRuleTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;
    private static GameContext Ctx(params string[] addons) =>
        new(new HashSet<string>(addons), Array.Empty<WalletDiff>(), T0);

    [Fact]
    public void Self_decrease_with_ItemSearch_open_classifies_as_MarketBoardBuy()
    {
        var rule = new MarketBoardBuyRule();
        var diff = new WalletDiff(
            new WalletId(WalletKind.Self, ""), 1_000_000, 750_000, T0);

        var ok = rule.TryClassify(diff, Ctx("ItemSearch"), out var ev);

        Assert.True(ok);
        Assert.Equal(GilEventCategory.MarketBoardBuy, ev!.Category);
        Assert.Equal(-250_000, ev.Amount);
    }

    [Fact]
    public void Self_increase_does_not_match()
    {
        var rule = new MarketBoardBuyRule();
        Assert.False(rule.TryClassify(
            new WalletDiff(new WalletId(WalletKind.Self, ""), 1, 100, T0),
            Ctx("ItemSearch"), out _));
    }

    [Fact]
    public void No_ItemSearch_does_not_match()
    {
        var rule = new MarketBoardBuyRule();
        Assert.False(rule.TryClassify(
            new WalletDiff(new WalletId(WalletKind.Self, ""), 100, 50, T0),
            Ctx(), out _));
    }
}
