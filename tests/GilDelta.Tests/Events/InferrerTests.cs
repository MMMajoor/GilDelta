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

    [Fact]
    public void Returns_unmatched_Misc_when_all_rules_decline()
    {
        // The "should be unreachable" branch in Inferrer.Classify is reachable
        // when the chain forgets a fallback. Pin its contract here.
        var inferrer = new Inferrer(new IInferenceRule[] { new AlwaysFalseRule() });
        var diff = new WalletDiff(
            new WalletId(WalletKind.Self, ""), 0, 1, DateTimeOffset.UnixEpoch);

        var ev = inferrer.Classify(diff, GameContext.Empty(diff.At));

        Assert.Equal(GilEventCategory.Misc, ev.Category);
        Assert.Contains("unmatched", ev.Note);
    }

    [Fact]
    public void Throwing_rule_is_skipped_and_chain_continues()
    {
        var inferrer = new Inferrer(new IInferenceRule[]
        {
            new ThrowingRule(),
            new MiscRule(),
        });
        var diff = new WalletDiff(
            new WalletId(WalletKind.Self, ""), 0, 5, DateTimeOffset.UnixEpoch);

        var ev = inferrer.Classify(diff, GameContext.Empty(diff.At));

        Assert.Equal(GilEventCategory.Misc, ev.Category);
        Assert.Contains("MiscRule", ev.Note);
    }

    private sealed class FakeRule(GilEventCategory category) : IInferenceRule
    {
        public bool TryClassify(WalletDiff diff, GameContext ctx, [NotNullWhen(true)] out GilEvent? ev)
        {
            ev = new GilEvent(diff.At, diff.Id, diff.Delta, category, "fake");
            return true;
        }
    }

    private sealed class AlwaysFalseRule : IInferenceRule
    {
        public bool TryClassify(WalletDiff diff, GameContext ctx, [NotNullWhen(true)] out GilEvent? ev)
        {
            ev = null;
            return false;
        }
    }

    private sealed class ThrowingRule : IInferenceRule
    {
        public bool TryClassify(WalletDiff diff, GameContext ctx, [NotNullWhen(true)] out GilEvent? ev)
            => throw new InvalidOperationException("rule blew up");
    }
}
