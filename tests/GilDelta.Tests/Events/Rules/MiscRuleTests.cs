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
        Assert.Contains("MiscRule", ev.Note);
    }
}
