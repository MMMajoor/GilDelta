using System;
using Dalamud.Bindings.ImGui;
using GilDelta.Localization;

namespace GilDelta.Windows.Dashboard;

public sealed class ChartTab : IDashboardTab
{
    public DashboardTab Identity => DashboardTab.Chart;
    public string Title => Strings.TabChart;

    public void Draw(DashboardContext ctx)
    {
        // Daily net for the last 30 local days, oldest first.
        const int days = 30;
        var totals = new float[days];
        var firstDay = new DateTimeOffset(ctx.Now.Date, ctx.Now.Offset).AddDays(-(days - 1));
        for (var i = 0; i < days; i++)
            totals[i] = (float)ctx.DailyNet(firstDay.AddDays(i));

        // Running total (cumulative) starting from current Total walking backward
        var cumulative = new float[days];
        var running = (float)ctx.Total;
        for (var i = days - 1; i >= 0; i--)
        {
            cumulative[i] = running;
            running -= totals[i];
        }

        ImGui.PushStyleColor(ImGuiCol.Text, ctx.Theme.TextMuted);
        ImGui.TextUnformatted(Strings.TotalWealth.ToUpperInvariant());
        ImGui.PopStyleColor();

        ImGui.PushStyleColor(ImGuiCol.Text, ctx.Theme.TextPrimary);
        ImGui.SetWindowFontScale(1.4f);
        ImGui.TextUnformatted($"{ctx.Total:N0}");
        ImGui.SetWindowFontScale(1.0f);
        ImGui.PopStyleColor();

        ImGui.Spacing();
        ImGui.PushStyleColor(ImGuiCol.PlotLines, ctx.Theme.PositiveDelta);
        ImGui.PlotLines(
            "##wealth",
            new ReadOnlySpan<float>(cumulative),
            cumulative.Length,
            "wealth (last 30d)",
            float.MaxValue,
            float.MaxValue,
            new System.Numerics.Vector2(-1, 100),
            sizeof(float));
        ImGui.PopStyleColor();

        ImGui.Spacing();
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, ctx.Theme.TextAccent);
        ImGui.PlotHistogram(
            "##daily",
            new ReadOnlySpan<float>(totals),
            totals.Length,
            "daily net",
            float.MaxValue,
            float.MaxValue,
            new System.Numerics.Vector2(-1, 80),
            sizeof(float));
        ImGui.PopStyleColor();
    }
}
