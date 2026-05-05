using System;
using System.Diagnostics.CodeAnalysis;
using GilDelta.Wallet;

namespace GilDelta.Events.Rules;

public sealed class PairedTransferRule : IInferenceRule
{
    private static readonly TimeSpan Window = TimeSpan.FromMilliseconds(500);

    public bool TryClassify(WalletDiff diff, GameContext ctx, [NotNullWhen(true)] out GilEvent? ev)
    {
        ev = null;

        // Only Self<->Retainer pairings are valid transfers.
        if (diff.Id.Kind == WalletKind.FreeCompanyChest)
            return false;

        // We need to find a counterparty in RecentDiffs whose:
        //   - kind is the *other* of (Self, Retainer)
        //   - delta is exactly the negation of ours
        //   - timestamp is within Window of ours
        foreach (var prior in ctx.RecentDiffs)
        {
            if (Math.Abs((diff.At - prior.At).TotalMilliseconds) > Window.TotalMilliseconds)
                continue;

            if (prior.Delta + diff.Delta != 0)
                continue;

            var pairedSelfWithRetainer =
                (diff.Id.Kind == WalletKind.Self && prior.Id.Kind == WalletKind.Retainer) ||
                (diff.Id.Kind == WalletKind.Retainer && prior.Id.Kind == WalletKind.Self);
            if (!pairedSelfWithRetainer)
                continue;

            // Identify which side gained gil.
            var (recipient, _) = diff.Delta > 0 ? (diff.Id, prior.Id) : (prior.Id, diff.Id);
            var category = recipient.Kind == WalletKind.Retainer
                ? GilEventCategory.RetainerDeposit
                : GilEventCategory.RetainerWithdraw;

            var retainerName = (diff.Id.Kind == WalletKind.Retainer ? diff.Id : prior.Id).Identifier;

            ev = new GilEvent(
                Timestamp: diff.At,
                Wallet:    diff.Id,
                Amount:    diff.Delta,
                Category:  category,
                Note:      $"rule=PairedTransferRule; counterparty={prior.Id.Kind}:{retainerName}");
            return true;
        }

        return false;
    }
}
