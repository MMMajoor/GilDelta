using System.Diagnostics.CodeAnalysis;
using GilDelta.Wallet;

namespace GilDelta.Events;

// Note format convention for all rules: "rule=<RuleName>; <key>=<value>; ..."
// Keep the leading "rule=<RuleName>;" stable so tooling can grep events by rule.
public interface IInferenceRule
{
    bool TryClassify(WalletDiff diff, GameContext ctx, [NotNullWhen(true)] out GilEvent? ev);
}
