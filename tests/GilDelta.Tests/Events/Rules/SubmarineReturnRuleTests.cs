using GilDelta.Events;
using GilDelta.Events.Rules;
using GilDelta.Wallet;

namespace GilDelta.Tests.Events.Rules;

public class SubmarineReturnRuleTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    [Fact]
    public void FC_chest_increase_classifies_as_SubmarineReturn()
    {
        var rule = new SubmarineReturnRule();
        var diff = new WalletDiff(
            new WalletId(WalletKind.FreeCompanyChest, "yagi2 FC"),
            Before: 18_000_000, After: 18_450_000, At: T0);

        var ok = rule.TryClassify(diff, GameContext.Empty(T0), out var ev);

        Assert.True(ok);
        Assert.Equal(GilEventCategory.SubmarineReturn, ev!.Category);
        Assert.Equal(450_000, ev.Amount);
    }

    [Fact]
    public void FC_chest_decrease_does_not_match()
    {
        var rule = new SubmarineReturnRule();
        var diff = new WalletDiff(
            new WalletId(WalletKind.FreeCompanyChest, "yagi2 FC"),
            Before: 18_000_000, After: 17_500_000, At: T0);

        Assert.False(rule.TryClassify(diff, GameContext.Empty(T0), out _));
    }

    [Fact]
    public void Self_increase_does_not_match()
    {
        var rule = new SubmarineReturnRule();
        var diff = new WalletDiff(
            new WalletId(WalletKind.Self, ""), 1_000, 2_000, T0);

        Assert.False(rule.TryClassify(diff, GameContext.Empty(T0), out _));
    }

    [Fact]
    public void Retainer_increase_does_not_match()
    {
        var rule = new SubmarineReturnRule();
        var diff = new WalletDiff(
            new WalletId(WalletKind.Retainer, "Yui"), 100, 1_000, T0);

        Assert.False(rule.TryClassify(diff, GameContext.Empty(T0), out _));
    }
}
