using System.Collections.Generic;
using GilDelta.Events;
using GilDelta.Events.Rules;
using GilDelta.Wallet;

namespace GilDelta.Tests.Events.Rules;

public class RepairRuleTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;
    private static GameContext Ctx(params string[] addons) =>
        new(new HashSet<string>(addons), Array.Empty<WalletDiff>(), T0);

    [Fact]
    public void Self_decrease_with_Repair_open_classifies_as_Repair()
    {
        var rule = new RepairRule();
        var diff = new WalletDiff(
            new WalletId(WalletKind.Self, ""), 100_000, 95_000, T0);

        var ok = rule.TryClassify(diff, Ctx("Repair"), out var ev);

        Assert.True(ok);
        Assert.Equal(GilEventCategory.Repair, ev!.Category);
        Assert.Equal(-5_000, ev.Amount);
    }

    [Fact]
    public void No_Repair_addon_does_not_match()
    {
        var rule = new RepairRule();
        Assert.False(rule.TryClassify(
            new WalletDiff(new WalletId(WalletKind.Self, ""), 100, 50, T0),
            Ctx(), out _));
    }

    [Fact]
    public void Self_increase_with_Repair_open_does_not_match()
    {
        var rule = new RepairRule();
        Assert.False(rule.TryClassify(
            new WalletDiff(new WalletId(WalletKind.Self, ""), 100, 1_000, T0),
            Ctx("Repair"), out _));
    }

    [Fact]
    public void Retainer_decrease_with_Repair_open_does_not_match()
    {
        var rule = new RepairRule();
        Assert.False(rule.TryClassify(
            new WalletDiff(new WalletId(WalletKind.Retainer, "Yui"), 100_000, 95_000, T0),
            Ctx("Repair"), out _));
    }
}
