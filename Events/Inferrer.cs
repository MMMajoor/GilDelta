using System;
using System.Collections.Generic;
using GilDelta.Wallet;

namespace GilDelta.Events;

public sealed class Inferrer
{
    private readonly IReadOnlyList<IInferenceRule> _rules;

    public Inferrer(IReadOnlyList<IInferenceRule> rules)
    {
        if (rules.Count == 0)
            throw new ArgumentException("Inferrer must have at least one rule.", nameof(rules));
        _rules = rules;
    }

    public GilEvent Classify(WalletDiff diff, GameContext ctx)
    {
        foreach (var rule in _rules)
        {
            try
            {
                if (rule.TryClassify(diff, ctx, out var ev))
                    return ev;
            }
            catch
            {
                // Rule blew up; fall through to next rule.
                // Production code logs Warning here; tests don't care.
            }
        }

        // Should be unreachable when MiscRule is the last rule.
        return new GilEvent(diff.At, diff.Id, diff.Delta,
            GilEventCategory.Misc, "rule=<unmatched>; no rule matched");
    }
}
