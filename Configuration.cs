using System.Numerics;
using Dalamud.Configuration;
using GilDelta.Theme;
using GilDelta.Windows.Dashboard;
using GilDelta.Windows.Widget;

namespace GilDelta;

public sealed class Configuration : IPluginConfiguration
{
    // Bumped when the schema changes; Plugin.LoadConfig handles migration.
    public int Version { get; set; } = 0;

    // -1 = follow the FFXIV client language; 0..5 = explicit Localization.Language enum value.
    public int Language { get; set; } = -1;
    public ThemeName Theme { get; set; } = ThemeName.MidnightCoin;

    public WidgetDensity Density { get; set; } = WidgetDensity.Compact;
    public bool ShowWidget { get; set; } = true;
    public bool HideInCutscene { get; set; } = true;
    public bool HideInDuty { get; set; } = false;
    public bool LockWidget { get; set; } = false;

    // null = use the default position (anchored top-right of the screen);
    // any concrete value is honoured verbatim, including (0, 0).
    public Vector2? WidgetPosition { get; set; } = null;

    public DashboardTab DefaultTab { get; set; } = DashboardTab.Chart;
    public Vector2 DashboardSize { get; set; } = new(720, 480);

    // 0 = keep events forever; otherwise prune events older than N days.
    public int RetentionDays { get; set; } = 0;
}
