using System;
using System.Linq;
using Dalamud.Bindings.ImGui;
using GilDelta.Events;
using GilDelta.Localization;

namespace GilDelta.Windows.Dashboard;

public sealed class BreakdownTab : IDashboardTab
{
    public DashboardTab Identity => DashboardTab.Breakdown;
    public string Title => Strings.TabBreakdown;

    private static readonly string[] Periods = { "Today", "7 days", "30 days", "All" };
    private int _periodIndex = 1;

    public void Draw(DashboardContext ctx)
    {
        ImGui.SetNextItemWidth(120);
        ImGui.Combo("##period", ref _periodIndex, Periods, Periods.Length);

        var (from, to) = PeriodWindow(ctx.Now, _periodIndex);
        var grouped = ctx.NetByCategory(from, to);
        if (grouped.Count == 0)
        {
            ImGui.TextDisabled("(no events in this period)");
            return;
        }

        var max = grouped.Values.Select(Math.Abs).DefaultIfEmpty(1L).Max();
        var ordered = grouped.OrderByDescending(kv => Math.Abs(kv.Value)).ToList();

        if (ImGui.BeginTable("##breakdown", 3,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("category", ImGuiTableColumnFlags.WidthFixed, 160);
            ImGui.TableSetupColumn("bar",      ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("amount",   ImGuiTableColumnFlags.WidthFixed, 130);

            foreach (var kv in ordered)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.PushStyleColor(ImGuiCol.Text, ctx.Theme.TextPrimary);
                ImGui.TextUnformatted(CategoryLabel(kv.Key));
                ImGui.PopStyleColor();

                ImGui.TableNextColumn();
                var ratio = max == 0 ? 0f : (float)Math.Abs(kv.Value) / max;
                var barColor = kv.Value >= 0 ? ctx.Theme.PositiveDelta : ctx.Theme.NegativeDelta;
                var width = ImGui.GetContentRegionAvail().X * ratio;
                var p0 = ImGui.GetCursorScreenPos();
                ImGui.GetWindowDrawList().AddRectFilled(
                    p0, new System.Numerics.Vector2(p0.X + width, p0.Y + 14),
                    ImGui.ColorConvertFloat4ToU32(barColor));
                ImGui.Dummy(new System.Numerics.Vector2(0, 16));

                ImGui.TableNextColumn();
                ImGui.PushStyleColor(ImGuiCol.Text, kv.Value >= 0 ? ctx.Theme.PositiveDelta : ctx.Theme.NegativeDelta);
                ImGui.TextUnformatted($"{kv.Value:+#,0;-#,0;0}");
                ImGui.PopStyleColor();
            }
            ImGui.EndTable();
        }
    }

    private static (DateTimeOffset from, DateTimeOffset to) PeriodWindow(DateTimeOffset now, int idx)
    {
        var to = now;
        var dayStart = new DateTimeOffset(now.Date, now.Offset);
        return idx switch
        {
            0 => (dayStart, to),
            1 => (dayStart.AddDays(-7), to),
            2 => (dayStart.AddDays(-30), to),
            _ => (DateTimeOffset.MinValue, to),
        };
    }

    private static string CategoryLabel(GilEventCategory c) => c switch
    {
        GilEventCategory.SubmarineReturn  => Strings.CategorySubmarineReturn,
        GilEventCategory.RetainerSale     => Strings.CategoryRetainerSale,
        GilEventCategory.RetainerWithdraw => Strings.CategoryRetainerWithdraw,
        GilEventCategory.RetainerDeposit  => Strings.CategoryRetainerDeposit,
        GilEventCategory.NpcShopBuy       => Strings.CategoryNpcShopBuy,
        GilEventCategory.NpcShopSell      => Strings.CategoryNpcShopSell,
        GilEventCategory.MarketBoardBuy   => Strings.CategoryMarketBoardBuy,
        GilEventCategory.Repair           => Strings.CategoryRepair,
        GilEventCategory.Teleport         => Strings.CategoryTeleport,
        _                                 => Strings.CategoryMisc,
    };
}
