using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace GilDelta.Wallet;

public sealed class WalletReader
{
    /// <summary>FFXIV's gil item ID. Constant.</summary>
    private const uint GilItemId = 1;

    public IReadOnlyList<Wallet> ReadAll()
    {
        var list = new List<Wallet>();
        TryReadSelf(list);
        // Tasks 9 and 10 add retainer and FC reads.
        return list;
    }

    private static void TryReadSelf(List<Wallet> sink)
    {
        unsafe
        {
            var manager = InventoryManager.Instance();
            if (manager == null) return;
            var amount = manager->GetInventoryItemCount(GilItemId);
            sink.Add(new Wallet(new WalletId(WalletKind.Self, ""), (long)amount));
        }
    }
}
