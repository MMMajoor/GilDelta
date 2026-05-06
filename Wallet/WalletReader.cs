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
        TryReadRetainers(list);
        // Task 10 adds FC reads.
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

    private static void TryReadRetainers(List<Wallet> sink)
    {
        unsafe
        {
            var rm = FFXIVClientStructs.FFXIV.Client.Game.RetainerManager.Instance();
            if (rm == null) return;
            for (var i = 0u; i < rm->Retainers.Length; i++)
            {
                var r = rm->Retainers[(int)i];
                if (r.RetainerId == 0) continue;       // empty slot
                var name = r.NameString;
                if (string.IsNullOrEmpty(name)) continue;
                sink.Add(new Wallet(
                    new WalletId(WalletKind.Retainer, name),
                    (long)r.Gil));
            }
        }
    }
}
