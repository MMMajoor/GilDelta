using System.Diagnostics.CodeAnalysis;
using GilDelta.Wallet;

namespace GilDelta.Events.Rules;

public sealed class MiscRule : IInferenceRule
{
    public bool TryClassify(WalletDiff diff, GameContext ctx, [NotNullWhen(true)] out GilEvent? ev)
    {
        var addons = ctx.OpenAddons.Count == 0
            ? "[]"
            : "[" + string.Join(",", ctx.OpenAddons) + "]";

        ev = new GilEvent(
            Timestamp: diff.At,
            Wallet:    diff.Id,
            Amount:    diff.Delta,
            Category:  GilEventCategory.Misc,
            Note:      $"rule=MiscRule; addons={addons}");
        return true;
    }
}
