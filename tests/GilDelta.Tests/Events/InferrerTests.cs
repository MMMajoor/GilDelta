using System.Diagnostics.CodeAnalysis;
using GilDelta.Events;
using GilDelta.Events.Rules;
using GilDelta.Wallet;

namespace GilDelta.Tests.Events;

public class InferrerTests
{
    [Fact]
    public void Falls_back_to_Misc_when_no_rule_matches()
    {
        var inferrer = new Inferrer(new IInferenceRule[] { new MiscRule() });
        var diff = new WalletDiff(
            new WalletId(WalletKind.Self, ""),
            Before: 100, After: 200, At: DateTimeOffset.UnixEpoch);

        var ev = inferrer.Classify(diff, GameContext.Empty(diff.At));

        Assert.Equal(GilEventCategory.Misc, ev.Category);
        Assert.Equal(100, ev.Amount);
    }

    [Fact]
    public void First_matching_rule_wins()
    {
        var fakeAlwaysSubmarine = new FakeRule(GilEventCategory.SubmarineReturn);
        var inferrer = new Inferrer(new IInferenceRule[]
        {
            fakeAlwaysSubmarine,
            new MiscRule(),
        });

        var diff = new WalletDiff(
            new WalletId(WalletKind.Self, ""), 100, 200, DateTimeOffset.UnixEpoch);

        var ev = inferrer.Classify(diff, GameContext.Empty(diff.At));

        Assert.Equal(GilEventCategory.SubmarineReturn, ev.Category);
    }

    private sealed class FakeRule(GilEventCategory category) : IInferenceRule
    {
        public bool TryClassify(WalletDiff diff, GameContext ctx, [NotNullWhen(true)] out GilEvent? ev)
        {
            ev = new GilEvent(diff.At, diff.Id, diff.Delta, category, "fake");
            return true;
        }
    }
}
