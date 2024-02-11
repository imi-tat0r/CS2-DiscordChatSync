﻿using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DiscordChat;

public class DiscordChatSync : BasePlugin, IPluginConfig<DiscordChatSyncConfig>
{
    public override string ModuleName => "DiscordChatSync";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "imi-tat0r";
    public override string ModuleDescription => "Syncs chat messages from and to a discord channel.";
    public DiscordChatSyncConfig Config { get; set; } = new();
    
    private IServiceProvider? _serviceProvider = null;
    
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
        
        Console.WriteLine("[DiscordChatSync] Config parsed");
    }

    public override void Load(bool hotReload)
    {
        base.Load(hotReload);

        Console.WriteLine("[DiscordChatSync] Start loading DiscordChatSync plugin");

        Console.WriteLine("[DiscordChatSync] Add services to DI container");
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSingleton<DiscordChatSync>(this);
        serviceCollection.AddSingleton<DiscordService>();

        _serviceProvider = serviceCollection.BuildServiceProvider();
        Console.WriteLine("[DiscordChatSync] DI container done");

        Console.WriteLine("[DiscordChatSync] Registering event handlers");

        var discordService = _serviceProvider.GetRequiredService<DiscordService>();
        RegisterEventHandler<EventPlayerChat>(discordService.OnPlayerChat);
        RegisterListener<Listeners.OnMapStart>(discordService.OnMapStart);
        Console.WriteLine("[DiscordChatSync] Event handlers registered");

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
}