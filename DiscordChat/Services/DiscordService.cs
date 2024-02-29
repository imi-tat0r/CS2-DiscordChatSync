using Discord;
using Discord.WebSocket;
using DiscordChat.Enums;
using DiscordChat.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;

namespace DiscordChat.Services;

public class DiscordService : BackgroundService
{
    public static DiscordSocketClient? Client { get; private set; }
    private readonly DiscordChatSync _plugin;
    private readonly MessageService _messageService;
    private readonly IStringLocalizer _localizer;

    public DiscordService(DiscordChatSync plugin, MessageService messageService, IStringLocalizer localizer)
    {
        _plugin = plugin;
        _messageService = messageService;
        _localizer = localizer;
    }

    #region BackgroundService

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("[DiscordChatSync] Starting DiscordService");
        return Initialize(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("[DiscordChatSync] Stopping DiscordService");
        if (Client != null)
        {
            await Client.StopAsync();
            await Client.LogoutAsync();
        }

        await base.StopAsync(cancellationToken);
    }

    #endregion

    #region DiscordBot

    private async Task Initialize(CancellationToken stoppingToken)
    {
        Console.WriteLine("[DiscordChatSync] Initializing DiscordService");
        // create the client
        var c = new DiscordSocketClient(new DiscordSocketConfig()
        {
            AlwaysDownloadUsers = true,
            GatewayIntents = GatewayIntents.MessageContent | GatewayIntents.Guilds | GatewayIntents.GuildMessages,
            UseInteractionSnowflakeDate = false
        });

        if (string.IsNullOrEmpty(_plugin.Config.DiscordToken))
        {
            for (var i = 0; i < 3; i++)
                Console.WriteLine("[DiscordChatSync] Discord token is not set. Please set it in the config file.");
            return;
        }

        Console.WriteLine("[DiscordChatSync] Logging in");

        c.Log += (msg) =>
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        };
        c.Connected += Connected;
        c.MessageReceived += MessageReceived;

        // login and start the bot
        await c.LoginAsync(TokenType.Bot, _plugin.Config.DiscordToken);
        await c.StartAsync();

        Client = c;

        await Task.Delay(-1, stoppingToken);

        Console.WriteLine("[DiscordChatSync] Why are we here? Just to suffer?");
    }

    private async Task Connected()
    {
        if (Client == null)
            return;

        try
        {
            // update info
            await Client.SetStatusAsync(UserStatus.Online);
            await Client.SetGameAsync(_localizer[_plugin.Config.DiscordStatusMessage]);

            Console.WriteLine("[DiscordChatSync] Ready!");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[DiscordChatSync] Exception while setting status: " + ex.Message);
        }
    }

    private Task MessageReceived(SocketMessage m)
    {
        // if not a user message, ignore
        if (!m.Author.GetGuildUser(out var user) || m is not SocketUserMessage msg)
            return Task.CompletedTask;

        var type = _messageService.GetDiscordMessageType(m);

        switch (type)
        {
            case DiscordMessageType.Unknown:
                break;
            case DiscordMessageType.Rcon:
                _messageService.HandleRconDiscordMessage(msg);
                break;
            case DiscordMessageType.Chat:
            case DiscordMessageType.Broadcast:
                _messageService.SyncDiscordMessage(msg, user);
                break;
        }

        return Task.CompletedTask;
    }

    #endregion
}