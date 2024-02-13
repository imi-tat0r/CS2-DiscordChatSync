using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Discord;
using Discord.WebSocket;
using DiscordChat.Extensions;
using DiscordChat.Helper;
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
            await _client.SetGameAsync($"Syncing chat messages");
            
            Console.WriteLine("[DiscordChatSync] Ready!");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[DiscordChatSync] Exception in Ready: " + ex.Message);
        }
    }
    private Task MessageReceived(SocketMessage m)
    {
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
            
            // replace emojis with their string counter part
            // split the message by new lines
            // remove any empty lines (since it would print the previous line again)
            var messageSplit = Emojis.ReplaceEmojisInString(msg.CleanContent)
                .Split('\n')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();
            
            // if we only have one line, inline the message
            if (messageSplit.Length == 1)
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
    
    public HookResult OnClientCommandGlobalPre(CCSPlayerController? player, CommandInfo info)
    {
        var command = info.GetArg(0);
        
        if (command != "say" && command != "say_team")
            return HookResult.Continue;
        
        return command == "say" ? 
            OnSay(player, info) : 
            OnSayTeam(player, info);
    }

    private HookResult OnSay(CCSPlayerController? player, CommandInfo info)
    {
        Console.WriteLine("[DiscordChatSync] OnSay");
        if (!ShouldSyncMessage(info.GetArg(1), out var message)) 
            return HookResult.Continue;
        
        if (!player.IsPlayer())
            return HookResult.Continue;
        
        SendDiscordMessage(false, player!, message);
        
        return HookResult.Continue;
    }
    private HookResult OnSayTeam(CCSPlayerController? player, CommandInfo info)
    {
        Console.WriteLine("[DiscordChatSync] OnSayTeam");
        if (!_plugin.Config.SyncTeamChat)
            return HookResult.Continue;
        
        if (!ShouldSyncMessage(info.GetArg(1), out var message)) 
            return HookResult.Continue;
        
        if (!player.IsPlayer())
            return HookResult.Continue;
        
        SendDiscordMessage(true, player!, message);
        
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

    #region Helpers

    private bool ShouldSyncMessage(string inMessage, out string outMessage)
    {
        outMessage = inMessage.Trim();
     
        if (string.IsNullOrWhiteSpace(outMessage))
            return false;
        
        // no prefix means we want all messages
        if (string.IsNullOrEmpty(_plugin.Config.MessagePrefix)) 
            return true;
        
        // ignore messages that don't start with the prefix
        if (!outMessage.StartsWith(_plugin.Config.MessagePrefix))
            return false;
            
        // remove the prefix from the message
        outMessage = outMessage[_plugin.Config.MessagePrefix.Length..].Trim();

        return true;
    }
    private void SendDiscordMessage(bool teamOnly, CCSPlayerController player, string message)
    {
        if (_plugin.Config.SyncChannelId == 0)
        {
            for (var i = 0; i < 3; i++)
                Console.WriteLine("[DiscordChatSync] Sync channel id is not set. Please set it in the config file.");
            return;
        }

        // print any message to specific channel
        if (_client?.GetChannel(_plugin.Config.SyncChannelId) is not IMessageChannel channel)
            return;

        var teamColor = player.TeamNum switch
        {
            (byte)CsTeam.Terrorist => ColorHelper.ChatColorToHexColor(ChatColors.Orange),
            (byte)CsTeam.CounterTerrorist => ColorHelper.ChatColorToHexColor(ChatColors.Blue),
            (byte)CsTeam.Spectator => ColorHelper.ChatColorToHexColor(ChatColors.Grey),
            _ => System.Drawing.Color.White
        };

        var discordColor = new Color(teamColor.R, teamColor.G, teamColor.B);

        var chatType = "[ALL] - ";

        if (teamOnly)
        {
            chatType = player.TeamNum switch
            {
                (byte)CsTeam.Terrorist => "[T]",
                (byte)CsTeam.CounterTerrorist => "[CT]",
                (byte)CsTeam.Spectator => "[Spec]",
                _ => ""
            };
        }
        
        var embed = new EmbedBuilder()
            .WithAuthor(chatType + " - " + player.PlayerName)
            .WithDescription(message)
            .WithColor(discordColor)
            .Build();

        try
        {
            channel.SendMessageAsync(embed: embed);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception: {e}");
            return;
        }
    }

    #endregion
}