using System;
using System.IO;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using GilDelta.Events;
using GilDelta.Events.Rules;
using GilDelta.Localization;

namespace GilDelta;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "GilDelta";

    private readonly Configuration _config;
    private readonly EventStore _store;
    private readonly EventLog _log;
    private readonly Inferrer _inferrer;
    private readonly WindowSystem _windowSystem = new("GilDelta");

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

        Service.PluginInterface.UiBuilder.Draw += DrawUi;
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

    private void DrawUi() => _windowSystem.Draw();

    public void Dispose()
    {
        Service.ClientState.Login  -= OnLogin;
        Service.ClientState.Logout -= OnLogout;
        Service.CommandManager.RemoveHandler("/gildelta");
        Service.CommandManager.RemoveHandler("/gd");
        Service.PluginInterface.UiBuilder.Draw -= DrawUi;
        Service.PluginInterface.SavePluginConfig(_config);
    }
}
