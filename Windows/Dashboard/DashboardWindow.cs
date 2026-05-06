using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace GilDelta.Windows.Dashboard;

public sealed class DashboardWindow : Window
{
    private readonly Configuration _config;
    private readonly IReadOnlyDictionary<DashboardTab, IDashboardTab> _tabs;
    private readonly Func<DashboardContext?> _ctxProvider;

    /// <summary>
    /// True only on the very first Draw() after the window opens. Used to
    /// honor <see cref="Configuration.DefaultTab"/> exactly once instead of
    /// re-selecting it every frame (which would prevent the user from ever
    /// switching tabs).
    /// </summary>
    private bool _selectDefaultTabOnce;

    public DashboardWindow(
        Configuration config,
        IEnumerable<IDashboardTab> tabs,
        Func<DashboardContext?> ctxProvider)
        : base("GilDelta Dashboard##dashboard", ImGuiWindowFlags.None)
    {
        _config      = config;
        _tabs        = tabs.ToDictionary(t => t.Identity);
        _ctxProvider = ctxProvider;

        Size          = config.DashboardSize;
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(480, 320),
            MaximumSize = new Vector2(1600, 1200),
        };
    }

    public override void OnOpen() => _selectDefaultTabOnce = true;

    public override void Draw()
    {
        var ctx = _ctxProvider();
        if (ctx is null)
        {
            ImGui.TextDisabled("waiting for game state...");
            return;
        }

        if (ImGui.BeginTabBar("##dashtabs"))
        {
            foreach (var tab in _tabs.Values)
            {
                // Only force-select the default tab on the first frame after
                // the window opens; otherwise the user can never switch away.
                var flags = (_selectDefaultTabOnce && tab.Identity == _config.DefaultTab)
                    ? ImGuiTabItemFlags.SetSelected
                    : ImGuiTabItemFlags.None;

                if (ImGui.BeginTabItem(tab.Title, flags))
                {
                    tab.Draw(ctx);
                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();
        }

        _selectDefaultTabOnce = false;
    }

    public override void OnClose() => _config.DashboardSize = Size ?? _config.DashboardSize;
}
