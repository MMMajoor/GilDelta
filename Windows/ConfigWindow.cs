using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using GilDelta.Localization;
using GilDelta.Theme;
using GilDelta.Windows.Dashboard;
using GilDelta.Windows.Widget;

namespace GilDelta.Windows;

public sealed class ConfigWindow : Window
{
    private readonly Configuration _config;

    public ConfigWindow(Configuration config)
        : base("GilDelta Settings##config",
               ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize)
    {
        _config = config;
        Size = new System.Numerics.Vector2(420, 360);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("##config"))
        {
            if (ImGui.BeginTabItem("General"))    { DrawGeneral();    ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Widget"))     { DrawWidget();     ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Dashboard"))  { DrawDashboard();  ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Data"))       { DrawData();       ImGui.EndTabItem(); }
            ImGui.EndTabBar();
        }
    }

    private void DrawGeneral()
    {
        var langLabels = new[] { "Auto (follow client)", "English", "日本語", "Deutsch", "Français", "简体中文", "한국어" };
        var current = _config.Language + 1;
        if (ImGui.Combo("Language", ref current, langLabels, langLabels.Length))
        {
            _config.Language = current - 1;
            Strings.SetLanguage(ResolveLanguage(_config.Language));
        }

        var theme = (int)_config.Theme;
        if (ImGui.Combo("Theme", ref theme, new[] { "Midnight Coin" }, 1))
            _config.Theme = (ThemeName)theme;
    }

    private void DrawWidget()
    {
        var show = _config.ShowWidget;
        if (ImGui.Checkbox("Show widget", ref show)) _config.ShowWidget = show;

        var density = (int)_config.Density;
        if (ImGui.Combo("Density", ref density,
                new[] { "Minimal", "Compact", "Trend", "Tile" }, 4))
            _config.Density = (WidgetDensity)density;

        var hideCs = _config.HideInCutscene;
        if (ImGui.Checkbox("Hide in cutscene", ref hideCs)) _config.HideInCutscene = hideCs;

        var hideDuty = _config.HideInDuty;
        if (ImGui.Checkbox("Hide in duty", ref hideDuty)) _config.HideInDuty = hideDuty;

        var locked = _config.LockWidget;
        if (ImGui.Checkbox("Lock widget position", ref locked)) _config.LockWidget = locked;

        if (ImGui.Button("Reset widget position"))
            _config.WidgetPosition = null;
    }

    private void DrawDashboard()
    {
        var def = (int)_config.DefaultTab;
        if (ImGui.Combo("Default tab", ref def,
                new[] { "Timeline", "Chart", "Breakdown", "Heatmap" }, 4))
            _config.DefaultTab = (DashboardTab)def;
    }

    private void DrawData()
    {
        var ret = _config.RetentionDays;
        if (ImGui.InputInt("Retention (days, 0 = forever)", ref ret))
            _config.RetentionDays = System.Math.Max(0, ret);

        ImGui.Spacing();
        ImGui.TextDisabled("Reclassify range / Export CSV: coming in v1.1");
    }

    private static Language ResolveLanguage(int code)
    {
        if (code >= 0 && code <= 5) return (Language)code;
        return Language.English;  // fallback; client-language detection is in Plugin.cs
    }
}
