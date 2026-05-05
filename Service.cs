using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace GilDelta;

internal sealed class Service
{
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static IFramework            Framework        { get; private set; } = null!;
    [PluginService] public static IClientState          ClientState      { get; private set; } = null!;
    [PluginService] public static IPlayerState          PlayerState      { get; private set; } = null!;
    [PluginService] public static IGameGui              GameGui          { get; private set; } = null!;
    [PluginService] public static ICommandManager       CommandManager   { get; private set; } = null!;
    [PluginService] public static IPluginLog            Log              { get; private set; } = null!;
    [PluginService] public static ICondition            Condition        { get; private set; } = null!;
}
