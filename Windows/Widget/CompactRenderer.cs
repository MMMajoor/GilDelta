using Dalamud.Bindings.ImGui;
using GilDelta.Localization;

namespace GilDelta.Windows.Widget;

public sealed class CompactRenderer : IWidgetRenderer
{
    public WidgetDensity Density => WidgetDensity.Compact;

    public void Draw(WidgetContext ctx)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, ctx.Theme.TextMuted);
        ImGui.TextUnformatted(Strings.TotalWealth.ToUpperInvariant());
        ImGui.PopStyleColor();

        ImGui.PushStyleColor(ImGuiCol.Text, ctx.Theme.TextPrimary);
        ImGui.SetWindowFontScale(1.4f);
        ImGui.TextUnformatted($"{ctx.Total:N0}");
        ImGui.SetWindowFontScale(1.0f);
        ImGui.PopStyleColor();

        ImGui.PushStyleColor(ImGuiCol.Text, ctx.Theme.TextMuted);
        ImGui.TextUnformatted(Strings.Today);
        ImGui.PopStyleColor();
        ImGui.SameLine();

        var dc = ctx.TodayDelta >= 0 ? ctx.Theme.PositiveDelta : ctx.Theme.NegativeDelta;
        var sign = ctx.TodayDelta >= 0 ? "+" : "";
        ImGui.PushStyleColor(ImGuiCol.Text, dc);
        ImGui.TextUnformatted($"{sign}{ctx.TodayDelta:N0}");
        ImGui.PopStyleColor();

        ImGui.Separator();
        ImGui.PushStyleColor(ImGuiCol.Text, ctx.Theme.TextMuted);
        ImGui.TextUnformatted($"{ctx.SelfTotal:N0}  ·  {ctx.RetainerTotal:N0}  ·  {ctx.FreeCompanyTotal:N0}");
        ImGui.PopStyleColor();
    }
}
