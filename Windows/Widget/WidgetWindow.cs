using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

namespace GilDelta.Windows.Widget;

public sealed class WidgetWindow : Window
{
    private readonly IReadOnlyDictionary<WidgetDensity, IWidgetRenderer> _renderers;
    private readonly Configuration _config;
    private readonly Func<WidgetContext?> _ctxProvider;

    public WidgetWindow(
        Configuration config,
        IEnumerable<IWidgetRenderer> renderers,
        Func<WidgetContext?> ctxProvider)
        : base("GilDelta##widget", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar)
    {
        _config = config;
        _renderers = renderers.ToDictionary(r => r.Density);
        _ctxProvider = ctxProvider;

        RespectCloseHotkey = false;
        IsOpen = config.ShowWidget;
    }

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
    }

    public override void PostDraw()
    {
        var current = ImGui.GetWindowPos();
        if (_config.WidgetPosition != current)
            _config.WidgetPosition = current;
    }
}
