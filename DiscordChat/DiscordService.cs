﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Discord;
using Discord.WebSocket;
using DiscordChat.Extensions;
using Microsoft.Extensions.Hosting;

namespace DiscordChat;

public class DiscordService : BackgroundService
{
    private static DiscordSocketClient? _client;
    private readonly DiscordChatSync _plugin;

    public DiscordService(DiscordChatSync plugin)
    {
        _plugin = plugin;
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
        if (_client != null)
        {
            await _client.StopAsync();
            await _client.LogoutAsync();
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
        c.Ready += Ready;
        c.MessageReceived += MessageReceived;
        
        // login and start the bot
        await c.LoginAsync(TokenType.Bot, _plugin.Config.DiscordToken);
        await c.StartAsync();

        _client = c;
        
        await Task.Delay(-1, stoppingToken);
        
        Console.WriteLine("[DiscordChatSync] Why are we here? Just to suffer?");
    }

    private async Task Ready()
    {
        Console.WriteLine("[DiscordChatSync] Ready?");
        
        if (_client == null)
            return;

        try
        {
            // update info
            await _client.SetStatusAsync(UserStatus.Online);
            await _client.SetCustomStatusAsync($"Syncing chat messages");
            
            Console.WriteLine("[DiscordChatSync] Ready!");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[DiscordChatSync] Exception in Ready: " + ex.Message);
        }
    }

    private Task MessageReceived(SocketMessage m)
    {
        Console.WriteLine("[DiscordChatSync] Message received");
        
        // see if the author is the guild user
        var author = m.Author;
        if (author is not SocketGuildUser user || user.IsBot || user.IsWebhook || m is not SocketUserMessage msg)
            return Task.CompletedTask;

        // see if the message is from the correct channel
        if (msg.Channel.Id != _plugin.Config.SyncChannelId && !_plugin.Config.AdditionalReadChannelIds.Contains(msg.Channel.Id))
            return Task.CompletedTask;

        var roles = user.Roles.ToList();
        var highestRole = roles.MaxBy(x => x.Position);


        var hexColor = highestRole != null
            ? "#" +
              highestRole.Color.R.ToString("X2") +
              highestRole.Color.G.ToString("X2") +
              highestRole.Color.B.ToString("X2")
            : "#ffffff";

        Server.NextWorldUpdate(() =>
        {
            var firstLine = $"[Discord - {msg.Channel.Name}] {ColorHelper.HexColorToChatColor(hexColor)}{user.DisplayName}{ChatColors.Default}: ";
            
            // split the message by new lines, and remove any empty lines (since it prints the previous line again)
            var messageSplit = msg.Content.Split('\n').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            
            // if we only have one line, inline the message
            if (messageSplit.Length <= 1)
                firstLine += messageSplit[0];
            
            Server.PrintToChatAll(firstLine);

            if (messageSplit.Length <= 1) 
                return;
            
            foreach (var line in messageSplit)
                Server.PrintToChatAll(line);
        });

        return Task.CompletedTask;
    }

    #endregion

    #region CS2
    
    public HookResult OnPlayerChat(EventPlayerChat eventPlayerChat, GameEventInfo info)
    {
        // account for team chat setting
        if (!_plugin.Config.SyncTeamChat && eventPlayerChat.Teamonly)
            return HookResult.Continue;
        
        var message = eventPlayerChat.Text.Trim();
        
        // account for optional message prefix
        if (!string.IsNullOrEmpty(_plugin.Config.MessagePrefix))
        {
            // ignore messages that don't start with the prefix
            if (!message.StartsWith(_plugin.Config.MessagePrefix))
                return HookResult.Continue;
            
            // remove the prefix from the message
            message = message[_plugin.Config.MessagePrefix.Length..].Trim();
        }

        // we only want to sync chat messages from players
        var player = Utilities.GetPlayerFromUserid(eventPlayerChat.Userid);
        if (!player.IsPlayer())
            return HookResult.Continue;

        if (_plugin.Config.SyncChannelId == 0)
        {
            for (var i = 0; i < 3; i++)
                Console.WriteLine("[DiscordChatSync] Sync channel id is not set. Please set it in the config file.");
            return HookResult.Continue;
        }

        // print any message to specific channel
        if (_client?.GetChannel(_plugin.Config.SyncChannelId) is not IMessageChannel channel)
            return HookResult.Continue;

        var teamColor = player.TeamNum switch
        {
            (byte)CsTeam.Terrorist => ColorHelper.ChatColorToHexColor(ChatColors.Orange),
            (byte)CsTeam.CounterTerrorist => ColorHelper.ChatColorToHexColor(ChatColors.Blue),
            (byte)CsTeam.Spectator => ColorHelper.ChatColorToHexColor(ChatColors.Grey),
            _ => System.Drawing.Color.White
        };

        var discordColor = new Color(teamColor.R, teamColor.G, teamColor.B);

        var chatType = "[ALL] ";

        if (eventPlayerChat.Teamonly)
        {
            chatType = player.TeamNum switch
            {
                (byte)CsTeam.Terrorist => "[T] ",
                (byte)CsTeam.CounterTerrorist => "[CT] ",
                (byte)CsTeam.Spectator => "[Spec] ",
                _ => ""
            };
        }
        
        var embed = new EmbedBuilder()
            .WithAuthor(chatType + player.PlayerName)
            .WithDescription(message)
            .WithColor(discordColor)
            .Build();

        channel.SendMessageAsync(embed: embed);

        return HookResult.Continue;
    }
    public void OnMapStart(string mapName)
    {
        // print any message to specific channel
        if (_client?.GetChannel(_plugin.Config.SyncChannelId) is not IMessageChannel channel)
            return;

        var embed = new EmbedBuilder()
            .WithAuthor("System")
            .WithDescription("Changed map to " + mapName)
            .WithColor(Color.Blue)
            .Build();

        channel.SendMessageAsync(embed: embed);
    }

    #endregion
}