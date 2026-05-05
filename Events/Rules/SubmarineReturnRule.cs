using System.Diagnostics.CodeAnalysis;
using GilDelta.Wallet;

namespace GilDelta.Events.Rules;

public sealed class SubmarineReturnRule : IInferenceRule
{
    public bool TryClassify(WalletDiff diff, GameContext ctx, [NotNullWhen(true)] out GilEvent? ev)
    {
        ev = null;
        if (diff.Id.Kind != WalletKind.FreeCompanyChest) return false;
        if (diff.Delta <= 0) return false;

        ev = new GilEvent(
            Timestamp: diff.At,
            Wallet:    diff.Id,
            Amount:    diff.Delta,
            Category:  GilEventCategory.SubmarineReturn,
            Note:      $"rule=SubmarineReturnRule; fc_chest +{diff.Delta}");
        return true;
    }
}
