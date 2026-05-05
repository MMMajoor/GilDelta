using System.Diagnostics.CodeAnalysis;
using GilDelta.Wallet;

namespace GilDelta.Events.Rules;

public sealed class RetainerSaleRule : IInferenceRule
{
    public bool TryClassify(WalletDiff diff, GameContext ctx, [NotNullWhen(true)] out GilEvent? ev)
    {
        ev = null;
        if (diff.Id.Kind != WalletKind.Retainer) return false;
        if (diff.Delta <= 0) return false;

        ev = new GilEvent(
            Timestamp: diff.At,
            Wallet:    diff.Id,
            Amount:    diff.Delta,
            Category:  GilEventCategory.RetainerSale,
            Note:      $"rule=RetainerSaleRule; retainer={diff.Id.Identifier}; +{diff.Delta}");
        return true;
    }
}
