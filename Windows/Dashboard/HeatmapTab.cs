using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using GilDelta.Localization;

namespace GilDelta.Windows.Dashboard;

public sealed class HeatmapTab : IDashboardTab
{
    public DashboardTab Identity => DashboardTab.Heatmap;
    public string Title => Strings.TabHeatmap;

    private const int Weeks = 12;

    public void Draw(DashboardContext ctx)
    {
        var dayStart = new DateTimeOffset(ctx.Now.Date, ctx.Now.Offset);
        var grid = new long[Weeks, 7];
        long maxAbs = 1;

        for (var w = 0; w < Weeks; w++)
        {
            for (var d = 0; d < 7; d++)
            {
                var day = dayStart.AddDays(-((Weeks - 1 - w) * 7 + (6 - d)));
                var net = ctx.DailyNet(day);
                grid[w, d] = net;
                if (Math.Abs(net) > maxAbs) maxAbs = Math.Abs(net);
            }
        }

        var draw = ImGui.GetWindowDrawList();
        var origin = ImGui.GetCursorScreenPos();
        const float cell = 14f;
        const float gap  = 2f;

        for (var w = 0; w < Weeks; w++)
        {
            for (var d = 0; d < 7; d++)
            {
                var net = grid[w, d];
                var ratio = (float)Math.Abs(net) / maxAbs;
                var baseColor = net >= 0 ? ctx.Theme.PositiveDelta : ctx.Theme.NegativeDelta;
                var col = new Vector4(baseColor.X, baseColor.Y, baseColor.Z, 0.15f + 0.85f * ratio);
                if (net == 0) col = new Vector4(ctx.Theme.BgSecondary.X, ctx.Theme.BgSecondary.Y, ctx.Theme.BgSecondary.Z, 1f);

                var x = origin.X + w * (cell + gap);
                var y = origin.Y + d * (cell + gap);
                draw.AddRectFilled(
                    new Vector2(x, y),
                    new Vector2(x + cell, y + cell),
                    ImGui.ColorConvertFloat4ToU32(col),
                    2f);
            }
        }

        ImGui.Dummy(new Vector2(Weeks * (cell + gap), 7 * (cell + gap)));

        // Mouse-hover detection
        var mouse = ImGui.GetMousePos();
        for (var w = 0; w < Weeks; w++)
        {
            for (var d = 0; d < 7; d++)
            {
                var x = origin.X + w * (cell + gap);
                var y = origin.Y + d * (cell + gap);
                if (mouse.X >= x && mouse.X < x + cell && mouse.Y >= y && mouse.Y < y + cell)
                {
                    var day = dayStart.AddDays(-((Weeks - 1 - w) * 7 + (6 - d)));
                    var net = grid[w, d];
                    ImGui.SetTooltip($"{day:yyyy-MM-dd}\n{net:+#,0;-#,0;0}");
                }
            }
        }
    }
}
