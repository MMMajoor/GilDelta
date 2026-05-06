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
        TryReadFreeCompany(list);
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

    private static void TryReadFreeCompany(List<Wallet> sink)
    {
        unsafe
        {
            try
            {
                var proxy = FFXIVClientStructs.FFXIV.Client.UI.Info.InfoProxyFreeCompany.Instance();
                if (proxy == null) return;
                var fcName = proxy->NameString;
                if (string.IsNullOrEmpty(fcName)) return;

                // FCS exposes the FC chest gil through InventoryManager.GetFreeCompanyGil(),
                // which queries the same backend the in-game FC chest UI uses. The value is
                // only populated while the player is actually in their FC's house / has
                // recently opened the chest; otherwise it will read as 0, which is a fine
                // placeholder for the breakdown row until the user visits the chest.
                long amount = 0;
                var manager = InventoryManager.Instance();
                if (manager != null)
                {
                    amount = (long)manager->GetFreeCompanyGil();
                }

                sink.Add(new Wallet(new WalletId(WalletKind.FreeCompanyChest, fcName), amount));
            }
            catch
            {
                // FCS API moved; skip gracefully so the rest of ReadAll keeps working.
            }
        }
    }
}
