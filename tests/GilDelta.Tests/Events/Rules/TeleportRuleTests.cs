using System.Collections.Generic;
using GilDelta.Events;
using GilDelta.Events.Rules;
using GilDelta.Wallet;

namespace GilDelta.Tests.Events.Rules;

public class TeleportRuleTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;
    private static GameContext Ctx(params string[] addons) =>
        new(new HashSet<string>(addons), Array.Empty<WalletDiff>(), T0);

    [Fact]
    public void Self_decrease_with_Teleport_open_classifies_as_Teleport()
    {
        var rule = new TeleportRule();
        var diff = new WalletDiff(
            new WalletId(WalletKind.Self, ""), 10_000, 9_500, T0);

        var ok = rule.TryClassify(diff, Ctx("Teleport"), out var ev);

        Assert.True(ok);
        Assert.Equal(GilEventCategory.Teleport, ev!.Category);
        Assert.Equal(-500, ev.Amount);
    }

    [Fact]
    public void No_Teleport_addon_does_not_match()
    {
        var rule = new TeleportRule();
        Assert.False(rule.TryClassify(
            new WalletDiff(new WalletId(WalletKind.Self, ""), 100, 50, T0),
            Ctx(), out _));
    }
}
