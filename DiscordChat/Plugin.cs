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

namespace DiscordChat;

public class DiscordChatSync : BasePlugin, IPluginConfig<DiscordChatSyncConfig>
{
    public override string ModuleName => "CS2-DiscordChatSync";
    public override string ModuleVersion => "1.1.0";
    public override string ModuleAuthor => "imi-tat0r";
    public override string ModuleDescription => "Syncs chat messages from and to a discord channel.";
    public DiscordChatSyncConfig Config { get; set; } = new();
    
    private IServiceProvider? _serviceProvider = null;
    
    private static readonly string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "";
    private static readonly string CfgPath = $"{Server.GameDirectory}/csgo/addons/counterstrikesharp/configs/plugins/{AssemblyName}/{AssemblyName}.json";
    
    public ConVar? CvarHostName { get; set; }
    public int CurPlayers { get; set; } = 0;
    
    public void OnConfigParsed(DiscordChatSyncConfig syncConfig)
    {
        Config = syncConfig;
        
        if (string.IsNullOrWhiteSpace(Config.DiscordToken))
        {
            Console.WriteLine("[DiscordChatSync] Discord token is not set. Please set it in the config file.");
            return;
        }
        
        if (Config.SyncChannelId == 0)
        {
            Console.WriteLine("[DiscordChatSync] Sync channel id is not set. Please set it in the config file.");
            return;
        }
        
        Chat.TimeFormat = Config.ChatFormatOptions.TimeFormat;
        Chat.DateFormat = Config.ChatFormatOptions.DateFormat;

        UpdateConfig(syncConfig);
        
        Console.WriteLine("[DiscordChatSync] Config parsed");
    }

    private static void UpdateConfig<T>(T forType) where T : new()
    {
        var jsonContent = File.ReadAllText(CfgPath);
        var currentConfigDict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent, new JsonSerializerOptions() { ReadCommentHandling = JsonCommentHandling.Skip });
        var defaultConfigDict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(new T()), new JsonSerializerOptions() { ReadCommentHandling = JsonCommentHandling.Skip });

        // Check if the CurrentVersion is different
        if (currentConfigDict == null || defaultConfigDict == null || 
            currentConfigDict["ConfigVersion"] == defaultConfigDict["ConfigVersion"])
            return;
        
        var needsUpdate = false;

        // Add missing keys
        foreach (var key in defaultConfigDict!.Keys)
        {
            if (currentConfigDict.ContainsKey(key))
                continue;
            
            currentConfigDict[key] = defaultConfigDict[key];
            needsUpdate = true;
        }

        // Remove extra keys
        var keysToRemove = new List<string>();
        foreach (var key in currentConfigDict!.Keys)
        {
            if (defaultConfigDict.ContainsKey(key))
                continue;

            keysToRemove.Add(key);
            needsUpdate = true;
        }
        
        foreach (var key in keysToRemove)
            currentConfigDict.Remove(key);

        // Update the CurrentVersion
        currentConfigDict["ConfigVersion"] = defaultConfigDict["ConfigVersion"];

        if (!needsUpdate) 
            return;
        
        var updatedJsonContent = JsonSerializer.Serialize(currentConfigDict, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(CfgPath, updatedJsonContent);
    }

    public override void Load(bool hotReload)
    {
        base.Load(hotReload);

        CvarHostName = ConVar.Find("hostname");
        
        Console.WriteLine("[DiscordChatSync] Start loading DiscordChatSync plugin");

        Console.WriteLine("[DiscordChatSync] Add services to DI container");
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSingleton(this);
        serviceCollection.AddSingleton<DiscordService>();
        serviceCollection.AddScoped<MessageService>();

        _serviceProvider = serviceCollection.BuildServiceProvider();
        Console.WriteLine("[DiscordChatSync] DI container done");

        Console.WriteLine("[DiscordChatSync] Registering event handlers");

        var discordService = _serviceProvider.GetRequiredService<DiscordService>();
        RegisterListener<Listeners.OnMapStart>(discordService.OnMapStart);
        RegisterListener<Listeners.OnClientDisconnect>(client =>
        {
            CurPlayers = Utilities.GetPlayers().Count(p => p.IsPlayer());
        });

        RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
        {
            CurPlayers = Utilities.GetPlayers().Count(p => p.IsPlayer());
            return HookResult.Continue;
        });
        Console.WriteLine("[DiscordChatSync] Event handlers registered");

        Console.WriteLine("[DiscordChatSync] Registering global command handler");
        AddCommandListener(null, discordService.OnClientCommandGlobalPre);
        Console.WriteLine("[DiscordChatSync] Global command handler registered");
        
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