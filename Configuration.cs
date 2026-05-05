using System.Numerics;
using Dalamud.Configuration;
using GilDelta.Theme;
using GilDelta.Windows.Dashboard;
using GilDelta.Windows.Widget;

namespace GilDelta;

public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public int Language { get; set; } = -1;
    public ThemeName Theme { get; set; } = ThemeName.MidnightCoin;

    public WidgetDensity Density { get; set; } = WidgetDensity.Compact;
    public bool ShowWidget { get; set; } = true;
    public bool HideInCutscene { get; set; } = true;
    public bool HideInDuty { get; set; } = false;
    public bool LockWidget { get; set; } = false;
    public Vector2 WidgetPosition { get; set; } = Vector2.Zero;

    public DashboardTab DefaultTab { get; set; } = DashboardTab.Chart;
    public Vector2 DashboardSize { get; set; } = new(720, 480);

    public int RetentionDays { get; set; } = 0;
}
