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
                var flags = tab.Identity == _config.DefaultTab
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
    }

    public override void OnClose() => _config.DashboardSize = Size ?? _config.DashboardSize;
}
