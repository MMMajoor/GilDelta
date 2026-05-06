using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using GilDelta.Events;
using GilDelta.Events.Rules;
using GilDelta.Localization;
using GilDelta.Wallet;
using GilDelta.Windows.Widget;

namespace GilDelta;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "GilDelta";

    private readonly Configuration _config;
    private readonly EventStore _store;
    private readonly EventLog _log;
    private readonly Inferrer _inferrer;
    private readonly WindowSystem _windowSystem = new("GilDelta");

    private readonly List<Wallet.WalletDiff> _recentDiffs = new();
    private readonly AddonStateTracker _addonState = new();
    private static readonly TimeSpan AddonRecencyWindow = TimeSpan.FromSeconds(2);
    private WalletReader? _reader;
    private WalletWatcher? _watcher;
    private WidgetWindow? _widget;

    public Plugin(IDalamudPluginInterface pi)
    {
        // Dalamud SDK 15: Inject is non-generic and takes an instance whose
        // [PluginService] members (static or instance) it fills via reflection.
        // The Service instance is throwaway because all properties are static.
        pi.Inject(new Service());

        _config = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Strings.SetLanguage(ResolveLanguage(_config.Language));

        _inferrer = new Inferrer(new IInferenceRule[]
        {
            new PairedTransferRule(),
            new SubmarineReturnRule(),
            new RetainerSaleRule(),
            new NpcShopBuyRule(),
            new NpcShopSellRule(),
            new MarketBoardBuyRule(),
            new RepairRule(),
            new TeleportRule(),
            new MiscRule(),
        });

        _store = new EventStore(EventStorePathFor(Service.PlayerState.ContentId));
        _log = new EventLog();
        _log.LoadFromStore(_store);

        // Sample addon state every Framework tick. Subscribe BEFORE the
        // WalletWatcher so the addon snapshot is fresh whenever a diff fires.
        Service.Framework.Update += SampleAddonState;

        _reader = new WalletReader();
        _watcher = new WalletWatcher(Service.Framework, _reader);
        _watcher.OnDiff += HandleDiff;

        var renderers = new IWidgetRenderer[]
        {
            new MinimalRenderer(),
            new CompactRenderer(),
            new TrendRenderer(),
            new TileRenderer(),
        };
        _widget = new WidgetWindow(_config, renderers, BuildWidgetContext);
        _windowSystem.AddWindow(_widget);

        Service.ClientState.Login  += OnLogin;
        Service.ClientState.Logout += OnLogout;

        Service.CommandManager.AddHandler("/gildelta", new CommandInfo(OnCommand)
        {
            HelpMessage = "Open GilDelta dashboard. /gildelta config opens settings.",
        });
        Service.CommandManager.AddHandler("/gd", new CommandInfo(OnCommand)
        {
            HelpMessage = "Alias for /gildelta.",
            ShowInHelp = false,
        });

        Service.PluginInterface.UiBuilder.Draw         += DrawUi;
        Service.PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
        Service.PluginInterface.UiBuilder.OpenMainUi   += OnOpenMainUi;
    }

    private string EventStorePathFor(ulong contentId)
    {
        var dir = Path.Combine(Service.PluginInterface.GetPluginConfigDirectory(), "events");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"{contentId}.jsonl");
    }

    private static Language ResolveLanguage(int code)
    {
        if (code >= 0 && code <= 5) return (Language)code;
        return Service.ClientState.ClientLanguage switch
        {
            Dalamud.Game.ClientLanguage.Japanese => Language.Japanese,
            Dalamud.Game.ClientLanguage.German   => Language.German,
            Dalamud.Game.ClientLanguage.French   => Language.French,
            _                                    => Language.English,
        };
    }

    private void OnLogin()
    {
        try
        {
            var newStore = new EventStore(EventStorePathFor(Service.PlayerState.ContentId));
            _log.Clear();
            _log.LoadFromStore(newStore);
        }
        catch (Exception e)
        {
            Service.Log.Warning(e, "OnLogin handler failed");
        }
    }

    private void OnLogout(int type, int code)
    {
        Service.Log.Information($"GilDelta: logout type={type} code={code}");
    }

    private void OnCommand(string command, string args)
    {
        // Plan 3 wires the dashboard / config window. For Plan 2 just log.
        Service.Log.Information($"GilDelta command: {command} {args}");
    }

    private void SampleAddonState(Dalamud.Plugin.Services.IFramework _)
    {
        try
        {
            _addonState.Tick(AddonProbe.OpenAddons(Service.GameGui));
        }
        catch
        {
            // GetAddonByName can throw if the game is mid-teardown; skip the tick.
        }
    }

    private void HandleDiff(Wallet.WalletDiff diff)
    {
        // Use the rolling 2-second window of recently-seen addons so the rules
        // still match even when the Shop / Teleport / Repair addon was closed
        // by the time WalletWatcher detected the diff on the next tick.
        var openAddons = _addonState.RecentlyOpen(AddonRecencyWindow);
        var ctx = new GameContext(openAddons, _recentDiffs.ToArray(), DateTimeOffset.Now);
        _recentDiffs.Add(diff);
        if (_recentDiffs.Count > 32) _recentDiffs.RemoveAt(0);

        var ev = _inferrer.Classify(diff, ctx);

        // Diagnostic breadcrumb so misclassifications can be debugged from /xllog.
        Service.Log.Information(
            "Diff {Kind}({Id}) {Delta:+#,0;-#,0;0} -> {Cat}; recentAddons=[{Addons}]",
            diff.Id.Kind, diff.Id.Identifier, diff.Delta, ev.Category,
            string.Join(",", openAddons));

        try
        {
            _store.Append(ev);
            _log.Add(ev);
        }
        catch (Exception e)
        {
            Service.Log.Warning(e, "EventStore.Append failed");
        }
    }

    private WidgetContext? BuildWidgetContext()
    {
        var snapshot = _watcher?.LastSnapshot;
        if (snapshot is null || snapshot.Count == 0) return null;
        return new WidgetContext(
            wallets: snapshot,
            recentEvents: _log.All,
            now: DateTimeOffset.Now,
            theme: Theme.MidnightCoin.Instance);
    }

    private void DrawUi() => _windowSystem.Draw();

    // Stub callbacks so the Dalamud plugin installer's "Open config UI" / "Open
    // main UI" buttons resolve. Plan 3 will wire these to the real ConfigWindow
    // and DashboardWindow respectively.
    private void OnOpenConfigUi() =>
        Service.Log.Information("GilDelta: OpenConfigUi requested (config window pending Plan 3)");

    private void OnOpenMainUi() =>
        Service.Log.Information("GilDelta: OpenMainUi requested (dashboard pending Plan 3)");

    public void Dispose()
    {
        _watcher?.Dispose();
        Service.Framework.Update   -= SampleAddonState;
        Service.ClientState.Login  -= OnLogin;
        Service.ClientState.Logout -= OnLogout;
        Service.CommandManager.RemoveHandler("/gildelta");
        Service.CommandManager.RemoveHandler("/gd");
        Service.PluginInterface.UiBuilder.Draw         -= DrawUi;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;
        Service.PluginInterface.UiBuilder.OpenMainUi   -= OnOpenMainUi;
        _windowSystem.RemoveAllWindows();
        Service.PluginInterface.SavePluginConfig(_config);
    }
}
