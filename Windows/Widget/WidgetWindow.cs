using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

namespace GilDelta.Windows.Widget;

public sealed class WidgetWindow : Window
{
    // Squared pixel slop: a left-release within this distance of the press is a
    // click (opens the dashboard); anything further is treated as a drag.
    private const float ClickThresholdSq = 25f;

    private readonly IReadOnlyDictionary<WidgetDensity, IWidgetRenderer> _renderers;
    private readonly Configuration _config;
    private readonly Func<WidgetContext?> _ctxProvider;
    private readonly Action _onClick;

    public WidgetWindow(
        Configuration config,
        IEnumerable<IWidgetRenderer> renderers,
        Func<WidgetContext?> ctxProvider,
        Action onClick)
        : base("GilDelta##widget", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar)
    {
        _config = config;
        _renderers = renderers.ToDictionary(r => r.Density);
        _ctxProvider = ctxProvider;
        _onClick = onClick;

        RespectCloseHotkey = false;
        IsOpen = config.ShowWidget;
    }

    // Skip drawing (and thus hide the widget) while bound by duty or watching a
    // cutscene, per the user's config toggles. BoundByDuty56/95 catch the
    // large-scale field ops (Eureka / Bozja / Occult Crescent) that the base
    // BoundByDuty flag doesn't.
    public override bool DrawConditions()
    {
        if (_config.HideInDuty && InDuty()) return false;
        if (_config.HideInCutscene && InCutscene()) return false;
        return true;
    }

    private static bool InDuty() =>
        Service.Condition[ConditionFlag.BoundByDuty]   ||
        Service.Condition[ConditionFlag.BoundByDuty56] ||
        Service.Condition[ConditionFlag.BoundByDuty95];

    private static bool InCutscene() =>
        Service.Condition[ConditionFlag.WatchingCutscene]   ||
        Service.Condition[ConditionFlag.WatchingCutscene78] ||
        Service.Condition[ConditionFlag.OccupiedInCutSceneEvent];

    public override void PreDraw()
    {
        if (_config.LockWidget)
            Flags |= ImGuiWindowFlags.NoMove;
        else
            Flags &= ~ImGuiWindowFlags.NoMove;

        if (_config.WidgetPosition is { } pos)
            ImGui.SetNextWindowPos(pos, ImGuiCond.FirstUseEver);
        else
            ImGui.SetNextWindowPos(new Vector2(ImGui.GetIO().DisplaySize.X - 280, 80), ImGuiCond.FirstUseEver);
    }

    public override void Draw()
    {
        var ctx = _ctxProvider();
        if (ctx is null)
        {
            ImGui.TextDisabled("waiting for game state...");
            return;
        }

        if (!_renderers.TryGetValue(_config.Density, out var renderer))
        {
            ImGui.TextDisabled($"renderer for {_config.Density} not registered");
            return;
        }

        renderer.Draw(ctx);

        // Click-to-open. Reached only after the null-context guard above, so this
        // fires only once a character is loaded and wallet data is flowing. A
        // near-motionless left-release is a click; a longer drag moves the widget
        // (ImGui's own window-move handling runs regardless of this check).
        if (ImGui.IsWindowHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
        {
            var drag = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left);
            if (drag.X * drag.X + drag.Y * drag.Y < ClickThresholdSq)
                _onClick();
            ImGui.ResetMouseDragDelta(ImGuiMouseButton.Left);
        }
    }

    public override void PostDraw()
    {
        var current = ImGui.GetWindowPos();
        if (_config.WidgetPosition != current)
            _config.WidgetPosition = current;
    }
}
