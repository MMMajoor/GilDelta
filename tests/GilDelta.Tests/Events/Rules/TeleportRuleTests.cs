using System.Collections.Generic;
using GilDelta.Events;
using GilDelta.Events.Rules;
using GilDelta.Wallet;

namespace GilDelta.Tests.Events.Rules;

public class TeleportRuleTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    private static GameContext Ctx(bool teleport) =>
        new(new HashSet<string>(), Array.Empty<WalletDiff>(), T0, recentlyCastTeleport: teleport);

    [Fact]
    public void Self_decrease_with_recent_teleport_cast_classifies_as_Teleport()
    {
        var rule = new TeleportRule();
        var diff = new WalletDiff(new WalletId(WalletKind.Self, ""), 100_000, 99_700, T0);

        var ok = rule.TryClassify(diff, Ctx(teleport: true), out var ev);

        Assert.True(ok);
        Assert.Equal(GilEventCategory.Teleport, ev!.Category);
        Assert.Equal(-300, ev.Amount);
        Assert.Contains("rule=TeleportRule", ev.Note);
    }

    [Fact]
    public void No_recent_teleport_cast_does_not_match()
    {
        var rule = new TeleportRule();
        Assert.False(rule.TryClassify(
            new WalletDiff(new WalletId(WalletKind.Self, ""), 100_000, 99_700, T0),
            Ctx(teleport: false), out _));
    }

    [Fact]
    public void Self_increase_with_teleport_cast_does_not_match()
    {
        // Teleport only ever costs gil; a +Δ during the window isn't a teleport.
        var rule = new TeleportRule();
        Assert.False(rule.TryClassify(
            new WalletDiff(new WalletId(WalletKind.Self, ""), 100, 1_000, T0),
            Ctx(teleport: true), out _));
    }

    [Fact]
    public void Retainer_decrease_with_teleport_cast_does_not_match()
    {
        var rule = new TeleportRule();
        Assert.False(rule.TryClassify(
            new WalletDiff(new WalletId(WalletKind.Retainer, "Yui"), 100_000, 95_000, T0),
            Ctx(teleport: true), out _));
    }
}
