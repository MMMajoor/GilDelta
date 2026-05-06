using System.Collections.Generic;
using Dalamud.Plugin.Services;

namespace GilDelta.Wallet;

public static class AddonProbe
{
    /// <summary>Names of Dalamud addons the inference rules care about.</summary>
    private static readonly string[] Watched = {
        "Shop", "InclusionShop", "ItemSearch", "Repair", "Teleport",
    };

    public static IReadOnlySet<string> OpenAddons(IGameGui gui)
    {
        var open = new HashSet<string>();
        foreach (var name in Watched)
        {
            var addon = gui.GetAddonByName(name);
            // Dalamud API 15 returns AtkUnitBasePtr; .Address != 0 means the addon is loaded.
            // (For "visible" specifically, would need .IsVisible; "loaded" is fine for v1.)
            if (addon.Address != 0)
                open.Add(name);
        }
        return open;
    }
}
