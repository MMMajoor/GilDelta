using GilDelta.Events;
using GilDelta.Events.Rules;
using GilDelta.Wallet;

namespace GilDelta.Tests.Events.Rules;

public class RetainerSaleRuleTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    [Fact]
    public void Retainer_increase_classifies_as_RetainerSale()
    {
        var rule = new RetainerSaleRule();
        var diff = new WalletDiff(
            new WalletId(WalletKind.Retainer, "Yui"), 0, 412_800, T0);

        var ok = rule.TryClassify(diff, GameContext.Empty(T0), out var ev);

        Assert.True(ok);
        Assert.Equal(GilEventCategory.RetainerSale, ev!.Category);
        Assert.Equal(412_800, ev.Amount);
        Assert.Contains("Yui", ev.Note);
    }

    [Fact]
    public void Retainer_decrease_does_not_match()
    {
        var rule = new RetainerSaleRule();
        var diff = new WalletDiff(
            new WalletId(WalletKind.Retainer, "Yui"), 500, 0, T0);

        Assert.False(rule.TryClassify(diff, GameContext.Empty(T0), out _));
    }

    [Fact]
    public void Self_increase_does_not_match()
    {
        var rule = new RetainerSaleRule();
        var diff = new WalletDiff(
            new WalletId(WalletKind.Self, ""), 0, 1_000, T0);

        Assert.False(rule.TryClassify(diff, GameContext.Empty(T0), out _));
    }

    [Fact]
    public void FC_chest_increase_does_not_match()
    {
        var rule = new RetainerSaleRule();
        var diff = new WalletDiff(
            new WalletId(WalletKind.FreeCompanyChest, "yagi2 FC"), 0, 100_000, T0);

        Assert.False(rule.TryClassify(diff, GameContext.Empty(T0), out _));
    }
}
