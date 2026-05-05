using System.Diagnostics.CodeAnalysis;
using GilDelta.Wallet;

namespace GilDelta.Events.Rules;

public sealed class RepairRule : IInferenceRule
{
    public bool TryClassify(WalletDiff diff, GameContext ctx, [NotNullWhen(true)] out GilEvent? ev)
    {
        ev = null;
        if (diff.Id.Kind != WalletKind.Self) return false;
        if (diff.Delta >= 0) return false;
        if (!ctx.OpenAddons.Contains("Repair")) return false;

        ev = new GilEvent(diff.At, diff.Id, diff.Delta, GilEventCategory.Repair,
            "rule=RepairRule; addons=[Repair]");
        return true;
    }
}
