using System.Reflection;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using DiscordChat.Extensions;
using DiscordChat.Helper;
using DiscordChat.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace DiscordChat;

public class DiscordChatSync : BasePlugin, IPluginConfig<DiscordChatSyncConfig>
{
    public override string ModuleName => "CS2-DiscordChatSync";
    public override string ModuleVersion => "1.1.0";
    public override string ModuleAuthor => "imi-tat0r";
    public override string ModuleDescription => "Syncs chat messages from and to a discord channel.";
    public DiscordChatSyncConfig Config { get; set; } = new();
    
    private static readonly string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "";
    private static readonly string CfgPath = $"{Server.GameDirectory}/csgo/addons/counterstrikesharp/configs/plugins/{AssemblyName}/{AssemblyName}.json";
    
    public ConVar? CvarHostName { get; set; }
    public int CurPlayers { get; set; } = 0;
    
    private readonly IServiceProvider _serviceProvider;
    
    public DiscordChatSync(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public void OnConfigParsed(DiscordChatSyncConfig syncConfig)
    {
        Config = syncConfig;
        
        if (string.IsNullOrWhiteSpace(Config.DiscordToken))
            Console.WriteLine("[DiscordChatSync] Discord token is not set. Please set it in the config file.");
        
        if (Config.SyncChannelId == 0)
            Console.WriteLine("[DiscordChatSync] Sync channel id is not set. Please set it in the config file.");
        
        Chat.TimeFormat = Config.ChatFormatOptions.TimeFormat;
        Chat.DateFormat = Config.ChatFormatOptions.DateFormat;

        UpdateConfig(syncConfig);
        
        Console.WriteLine("[DiscordChatSync] Config parsed");
    }

    // NOTE: the loaded config always has all properties (missing ones are set to their default values)
    // serializing the loaded config back to json will result in a json file with all properties
    // even the ones that were previously not set in the config file
    private static void UpdateConfig<T>(T config) where T : BasePluginConfig, new()
    {
        // get current config version
        var newCfgVersion = new T().Version;
        
        // loaded config is up to date
        if (config.Version == newCfgVersion)
            return;
        
        // update the version
        config.Version = newCfgVersion;
        
        // serialize the updated config back to json
        var updatedJsonContent = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(CfgPath, updatedJsonContent);
    }

    public override void Load(bool hotReload)
    {
        base.Load(hotReload);

        CvarHostName = ConVar.Find("hostname");
        
        Console.WriteLine("[DiscordChatSync] Start loading DiscordChatSync plugin");

        Console.WriteLine("[DiscordChatSync] Registering event handlers");
        var messageService = _serviceProvider.GetRequiredService<MessageService>();
        RegisterListener<Listeners.OnMapStart>(_ => { messageService.SyncSystemMessage("MapChange", null); });
        
        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            messageService.SyncSystemMessage("PlayerDisconnect", @event.Userid);
            CurPlayers = Utilities.GetPlayers().Count(p => p.IsPlayer());
            return HookResult.Continue;
        });
        RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
        {
            messageService.SyncSystemMessage("PlayerConnect", @event.Userid);
            CurPlayers = Utilities.GetPlayers().Count(p => p.IsPlayer());
            return HookResult.Continue;
        });
        Console.WriteLine("[DiscordChatSync] Event handlers registered");

        Console.WriteLine("[DiscordChatSync] Registering global command handler");
        var chatService = _serviceProvider.GetRequiredService<ChatService>();
        AddCommandListener(null, chatService.OnClientCommandGlobalPre);
        Console.WriteLine("[DiscordChatSync] Global command handler registered");
        
        var discordService = _serviceProvider.GetRequiredService<DiscordService>();
        discordService.StartAsync(new CancellationToken());

        Console.WriteLine("[DiscordChatSync] Plugin loaded");
    }
    
    public override void Unload(bool hotReload)
    {
        Console.WriteLine("[DiscordChatSync] Start unloading plugin");
        var discordService = _serviceProvider?.GetRequiredService<DiscordService>();
        discordService?.StopAsync(new CancellationToken()).GetAwaiter().GetResult();

        base.Unload(hotReload);
        Console.WriteLine("[DiscordChatSync] Done unloading plugin");
    }
    
    [ConsoleCommand("css_reload_cfg", "Reload the config in the current session without restarting the server")]
    [RequiresPermissions("@css/generic")]
    [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnReloadConfigCommand(CCSPlayerController? player, CommandInfo info)
    {
        var config = File.ReadAllText(CfgPath);
        try
        {
            OnConfigParsed(JsonSerializer.Deserialize<DiscordChatSyncConfig>(config,
                new JsonSerializerOptions() { ReadCommentHandling = JsonCommentHandling.Skip })!);
        }
        catch (Exception e)
        {
            info.ReplyToCommand($"[DiscordChatSync] Failed to reload config: {e.Message}");
        }
    }
}

public class DiscordChatSyncServiceCollection : IPluginServiceCollection<DiscordChatSync>
{
    public void ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton(this);
        serviceCollection.AddSingleton<DiscordService>();
        serviceCollection.AddSingleton<ChatService>();
        serviceCollection.AddScoped<MessageService>();
        //serviceCollection.AddSingleton<IStringLocalizer>();
    }
}