using System.Collections.Generic;
using GilDelta.Events;
using GilDelta.Events.Rules;
using GilDelta.Wallet;

namespace GilDelta.Tests.Events.Rules;

public class PairedTransferRuleTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-05-05T14:00:00+09:00");

    private static GameContext Ctx(params WalletDiff[] recent) =>
        new(new HashSet<string>(), recent, T0);

    [Fact]
    public void Self_minus_paired_with_retainer_plus_classifies_as_Deposit()
    {
        var rule = new PairedTransferRule();
        var selfId = new WalletId(WalletKind.Self, "");
        var retId  = new WalletId(WalletKind.Retainer, "Yui");

        // Currently being classified: retainer +500k.
        var current = new WalletDiff(retId, 0, 500_000, T0);
        // 100ms earlier: self -500k.
        var prior = new WalletDiff(selfId, 500_000, 0, T0.AddMilliseconds(-100));

        var ok = rule.TryClassify(current, Ctx(prior), out var ev);

        Assert.True(ok);
        Assert.Equal(GilEventCategory.RetainerDeposit, ev!.Category);
        Assert.Equal(500_000, ev.Amount);
        Assert.Contains("Yui", ev.Note);
    }

    [Fact]
    public void Self_plus_paired_with_retainer_minus_classifies_as_Withdraw()
    {
        var rule = new PairedTransferRule();
        var selfId = new WalletId(WalletKind.Self, "");
        var retId  = new WalletId(WalletKind.Retainer, "Aki");

        // Currently classifying: self +200k.
        var current = new WalletDiff(selfId, 1_000, 201_000, T0);
        // Same tick: retainer -200k.
        var prior = new WalletDiff(retId, 500_000, 300_000, T0);

        var ok = rule.TryClassify(current, Ctx(prior), out var ev);

        Assert.True(ok);
        Assert.Equal(GilEventCategory.RetainerWithdraw, ev!.Category);
        Assert.Equal(200_000, ev.Amount);
    }

    [Fact]
    public void No_match_when_amounts_differ()
    {
        var rule = new PairedTransferRule();
        var current = new WalletDiff(
            new WalletId(WalletKind.Retainer, "Yui"), 0, 500_000, T0);
        var prior = new WalletDiff(
            new WalletId(WalletKind.Self, ""), 1_000_000, 600_000, T0);  // -400k, not -500k

        var ok = rule.TryClassify(current, Ctx(prior), out _);

        Assert.False(ok);
    }

    [Fact]
    public void No_match_when_window_exceeded()
    {
        var rule = new PairedTransferRule();
        var current = new WalletDiff(
            new WalletId(WalletKind.Retainer, "Yui"), 0, 500_000, T0);
        var prior = new WalletDiff(
            new WalletId(WalletKind.Self, ""), 500_000, 0, T0.AddSeconds(-2));  // 2 s earlier

        var ok = rule.TryClassify(current, Ctx(prior), out _);

        Assert.False(ok);
    }

    [Fact]
    public void No_match_when_FC_chest_involved()
    {
        // FC chest is never the counterparty of a Self transfer; rule should bail.
        var rule = new PairedTransferRule();
        var current = new WalletDiff(
            new WalletId(WalletKind.FreeCompanyChest, "yagi2 FC"), 0, 500_000, T0);
        var prior = new WalletDiff(
            new WalletId(WalletKind.Self, ""), 500_000, 0, T0);

        var ok = rule.TryClassify(current, Ctx(prior), out _);

        Assert.False(ok);
    }
}
