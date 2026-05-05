using System;
using System.Collections.Generic;
using GilDelta.Wallet;

namespace GilDelta.Events;

public sealed class Inferrer
{
    private readonly IReadOnlyList<IInferenceRule> _rules;

    public Inferrer(IReadOnlyList<IInferenceRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
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
                // TODO(plan-2): inject IPluginLog and emit Warning here so a
                // throwing rule doesn't silently degrade to Misc with no signal.
            }
        }

        // Reachable only when no rule matched (the chain forgot a fallback).
        // MiscRule as the last rule guarantees this branch is dead in practice;
        // we still produce a well-formed GilEvent so callers never see null.
        return new GilEvent(diff.At, diff.Id, diff.Delta,
            GilEventCategory.Misc, "rule=<unmatched>; no rule matched");
    }
}
