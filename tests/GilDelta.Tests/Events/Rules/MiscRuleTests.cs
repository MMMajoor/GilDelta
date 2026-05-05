using System.Collections.Generic;
using GilDelta.Events;
using GilDelta.Events.Rules;
using GilDelta.Wallet;

namespace GilDelta.Tests.Events.Rules;

public class MiscRuleTests
{
    [Fact]
    public void Always_matches_with_Misc_category()
    {
        var rule = new MiscRule();
        var diff = new WalletDiff(
            new WalletId(WalletKind.Self, ""), 0, 1234, DateTimeOffset.UnixEpoch);

        var ok = rule.TryClassify(diff, GameContext.Empty(diff.At), out var ev);

        Assert.True(ok);
        Assert.NotNull(ev);
        Assert.Equal(GilEventCategory.Misc, ev!.Category);
        Assert.Equal(1234, ev.Amount);
        Assert.Contains("rule=MiscRule", ev.Note);
        Assert.Contains("addons=[]", ev.Note);
    }

    [Fact]
    public void Note_lists_open_addons_when_present()
    {
        var rule = new MiscRule();
        var diff = new WalletDiff(
            new WalletId(WalletKind.Self, ""), 0, 100, DateTimeOffset.UnixEpoch);
        var ctx = new GameContext(
            new HashSet<string> { "Shop", "Repair" },
            Array.Empty<WalletDiff>(),
            DateTimeOffset.UnixEpoch);

        rule.TryClassify(diff, ctx, out var ev);

        Assert.NotNull(ev);
        Assert.Contains("addons=[", ev!.Note);
        // Order isn't guaranteed by HashSet; assert membership instead.
        Assert.Contains("Shop", ev.Note);
        Assert.Contains("Repair", ev.Note);
    }
}
