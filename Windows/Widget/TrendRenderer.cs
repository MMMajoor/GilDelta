using System;
using System.Linq;
using Dalamud.Bindings.ImGui;
using GilDelta.Localization;

namespace GilDelta.Windows.Widget;

public sealed class TrendRenderer : IWidgetRenderer
{
    public WidgetDensity Density => WidgetDensity.Trend;

    public void Draw(WidgetContext ctx)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, ctx.Theme.TextMuted);
        ImGui.TextUnformatted(Strings.TotalWealth.ToUpperInvariant());
        ImGui.PopStyleColor();

        ImGui.PushStyleColor(ImGuiCol.Text, ctx.Theme.TextPrimary);
        ImGui.SetWindowFontScale(1.5f);
        ImGui.TextUnformatted($"{ctx.Total:N0}");
        ImGui.SetWindowFontScale(1.0f);
        ImGui.PopStyleColor();

        var spark = BuildSparkline(ctx);
        if (spark.Length > 1)
        {
            ImGui.PushStyleColor(ImGuiCol.PlotLines, ctx.Theme.PositiveDelta);
            ImGui.PlotLines("##spark", new ReadOnlySpan<float>(spark), spark.Length, "", float.MaxValue, float.MaxValue, new System.Numerics.Vector2(220, 32), sizeof(float));
            ImGui.PopStyleColor();
        }

        ImGui.PushStyleColor(ImGuiCol.Text, ctx.Theme.TextMuted);
        ImGui.TextUnformatted($"{Strings.Today}: {ctx.TodayDelta:+#,0;-#,0;0}");
        ImGui.PopStyleColor();
    }

    private static float[] BuildSparkline(WidgetContext ctx)
    {
        var since = ctx.Now.AddHours(-24);
        var hours = ctx.RecentEvents
            .Where(e => e.Timestamp >= since)
            .GroupBy(e => (int)Math.Floor((ctx.Now - e.Timestamp).TotalHours))
            .OrderBy(g => g.Key)
            .Select(g => (float)g.Sum(e => e.Amount))
            .ToArray();
        return hours.Length == 0 ? new float[] { 0 } : hours;
    }
}
