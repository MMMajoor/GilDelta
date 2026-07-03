using System;
using System.Linq;
using Dalamud.Bindings.ImGui;
using GilDelta.Events;
using GilDelta.Localization;

namespace GilDelta.Windows.Dashboard;

public sealed class TimelineTab : IDashboardTab
{
    public DashboardTab Identity => DashboardTab.Timeline;
    public string Title => Strings.TabTimeline;

    private static readonly GilEventCategory[] ReclassifyChoices =
        Enum.GetValues<GilEventCategory>();

    public void Draw(DashboardContext ctx)
    {
        var events = ctx.Events.OrderByDescending(e => e.Timestamp).Take(200).ToList();
        if (events.Count == 0)
        {
            ImGui.TextDisabled("(no events recorded yet)");
            return;
        }

        if (ImGui.BeginTable("##timeline", 4,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("when",     ImGuiTableColumnFlags.WidthFixed, 130);
            ImGui.TableSetupColumn("category", ImGuiTableColumnFlags.WidthFixed, 130);
            ImGui.TableSetupColumn("wallet",   ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("amount",   ImGuiTableColumnFlags.WidthFixed, 110);
            ImGui.TableHeadersRow();

            foreach (var ev in events)
            {
                // Stable per-event popup id (Ticks survives row reordering across frames).
                var popupId = $"##reclass{ev.Timestamp.Ticks}";

                ImGui.TableNextRow();

                // Column 0 is a row-spanning Selectable so a left-click anywhere on
                // the row opens the reclassify popup. The timestamp is its label.
                ImGui.TableSetColumnIndex(0);
                ImGui.PushStyleColor(ImGuiCol.Text, ctx.Theme.TextMuted);
                if (ImGui.Selectable(ev.Timestamp.ToString("MM-dd HH:mm:ss") + popupId,
                        false, ImGuiSelectableFlags.SpanAllColumns)
                    && ctx.Reclassify is not null)
                {
                    ImGui.OpenPopup(popupId);
                }
                ImGui.PopStyleColor();

                ImGui.TableSetColumnIndex(1);
                ImGui.PushStyleColor(ImGuiCol.Text, ctx.Theme.TextAccent);
                ImGui.TextUnformatted(CategoryLabel(ev.Category));
                ImGui.PopStyleColor();
                if (!string.IsNullOrEmpty(ev.Note) && ImGui.IsItemHovered())
                    ImGui.SetTooltip(ev.Note);

                ImGui.TableSetColumnIndex(2);
                var walletText = ev.Wallet.Kind switch
                {
                    GilDelta.Wallet.WalletKind.Self             => "self",
                    GilDelta.Wallet.WalletKind.Retainer         => $"retainer:{ev.Wallet.Identifier}",
                    GilDelta.Wallet.WalletKind.FreeCompanyChest => "fc-chest",
                    _                                           => ev.Wallet.Kind.ToString(),
                };
                ImGui.TextUnformatted(walletText);

                ImGui.TableSetColumnIndex(3);
                var col = ev.Amount >= 0 ? ctx.Theme.PositiveDelta : ctx.Theme.NegativeDelta;
                ImGui.PushStyleColor(ImGuiCol.Text, col);
                ImGui.TextUnformatted($"{ev.Amount:+#,0;-#,0;0}");
                ImGui.PopStyleColor();

                if (ctx.Reclassify is not null && ImGui.BeginPopup(popupId))
                {
                    ImGui.TextDisabled(Strings.ReclassifyAs);
                    ImGui.Separator();
                    foreach (var cat in ReclassifyChoices)
                    {
                        var isCurrent = cat == ev.Category;
                        if (ImGui.MenuItem(CategoryLabel(cat), "", isCurrent) && !isCurrent)
                            ctx.Reclassify(ev, cat);
                    }
                    ImGui.EndPopup();
                }
            }
            ImGui.EndTable();
        }
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
