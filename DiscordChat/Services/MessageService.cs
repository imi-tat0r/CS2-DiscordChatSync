using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using Discord;
using Discord.WebSocket;
using DiscordChat.Enums;
using DiscordChat.Extensions;
using DiscordChat.Helper;
using Microsoft.Extensions.Localization;

namespace DiscordChat.Services;

public class MessageService
{
    private readonly DiscordChatSync _plugin;
    private readonly IStringLocalizer _localizer;

    public MessageService(DiscordChatSync plugin, IStringLocalizer localizer)
    {
        _plugin = plugin;
        _localizer = localizer;
    }


    public DiscordMessageType GetDiscordMessageType(SocketMessage msg)
    {
        if (msg.Channel.Id == _plugin.Config.SyncChannelId)
            return DiscordMessageType.Chat;
        if (_plugin.Config.AdditionalReadChannelIds.Contains(msg.Channel.Id))
            return DiscordMessageType.Broadcast;

        return msg.Channel.Id == _plugin.Config.RconChannelId ? DiscordMessageType.Rcon : DiscordMessageType.Unknown;
    }

    public ChatMessageType GetChatMessageType(CCSPlayerController? player, string command)
    {
        if (player == null)
            return ChatMessageType.Console;

        return command switch
        {
            "say" => ChatMessageType.Chat,
            "say_team" => ChatMessageType.TeamChat,
            _ => ChatMessageType.Unknown
        };
    }

    public void HandleRconDiscordMessage(SocketUserMessage msg)
    {
        var messageSplit = msg.Content.Split('\n')
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (messageSplit.Count != 1)
        {
            msg.ReplyAsync(_localizer["rcon.error_multiple_lines"]);
            return;
        }

        var message = messageSplit[0];

        if (!string.IsNullOrEmpty(_plugin.Config.RconMessagePrefix) &&
            !message.StartsWith(_plugin.Config.RconMessagePrefix))
        {
            msg.ReplyAsync(_localizer["rcon.error_prefix_missing", _plugin.Config.RconMessagePrefix]);
            return;
        }

        if (message.StartsWith(_plugin.Config.RconMessagePrefix))
            message = message[_plugin.Config.RconMessagePrefix.Length..].Trim();

        Server.NextWorldUpdate(() =>
        {
            Server.ExecuteCommand(message);
            msg.ReplyAsync(_localizer["rcon.success", message]);
        });
    }

    public bool TryGetFullChatMessage(CommandInfo info, out string outMessage)
    {
        var inMessage = info.ArgString;

        // strip quotes from the message
        if (inMessage.StartsWith("\"") && inMessage.EndsWith("\""))
            inMessage = inMessage[1..^1];

        outMessage = inMessage.Trim();

        // ignore empty messages
        return !string.IsNullOrWhiteSpace(outMessage);
    }

    public void SyncDiscordMessage(SocketUserMessage msg, SocketGuildUser user)
    {
        var messageSplit = GetChatFriendlyMessage(msg);
        var lines = Chat.FormatDiscordMessageForChat(_plugin.Config.ChatFormatOptions.ServerOutputFormat, msg, user,
            messageSplit);

        Server.NextWorldUpdate(() =>
        {
            foreach (var line in lines)
                Server.PrintToChatAll(line);
        });
    }

    public void SyncChatMessage(CCSPlayerController? player, bool teamOnly, string message)
    {
        // print any message to specific channel
        if (DiscordService.Client?.GetChannel(_plugin.Config.SyncChannelId) is not IMessageChannel channel)
            return;

        var dynamicReplacements = GetDynamicReplacements(player);

        var embedFinal = Chat.FormatChatMessageForDiscord(
            _plugin.Config.ChatFormatOptions.DiscordEmbedFormat,
            _plugin.Config.ChatFormatOptions.DiscordEmbedFields,
            dynamicReplacements, player, teamOnly, message,
            _localizer);

        SendDiscordMessage(channel, embedFinal);
    }

    public void SyncSystemMessage(string type, CCSPlayerController? player)
    {
        // ignore bots
        if (player != null && !player.IsPlayer())
            return;
        

        // ignore if the message type is not defined in the config
        if (!_plugin.Config.ChatFormatOptions.SystemMessages.TryGetValue(type, out var messageTemplate) ||
            string.IsNullOrEmpty(messageTemplate))
            return;

        // print any message to specific channel
        if (DiscordService.Client?.GetChannel(_plugin.Config.SystemChannelId) is not IMessageChannel channel)
            return;

        var dynamicReplacements = GetDynamicReplacements(player);

        var embedFinal = Chat.FormatSystemMessageForDiscord(
            _plugin.Config.ChatFormatOptions.DiscordEmbedFormat,
            dynamicReplacements, messageTemplate, _localizer);

        SendDiscordMessage(channel, embedFinal);
    }

    private static void SendDiscordMessage(IMessageChannel channel, Embed embedFinal)
    {
        try
        {
            channel.SendMessageAsync(embed: embedFinal);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception: {e}");
        }
    }

    private static List<string> GetChatFriendlyMessage(IMessage msg)
    {
        var message = msg.CleanContent;
        var messageSplit = Emojis.ReplaceEmojisInString(message)
            .Split('\n')
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        // if there's any part in the string that's >50 characters without any whitespace, we need to add a whitespace there
        for (var i = 0; i < messageSplit.Count; i++)
            messageSplit[i] = Chat.ForceBreakLongWords(messageSplit[i]);

        // if we have multiple lines, insert a line break after the first line
        if (messageSplit.Count > 1)
            messageSplit.Insert(0, " ");

        return messageSplit;
    }

    private Dictionary<string, string> GetDynamicReplacements(CCSPlayerController? player)
    {
        return new Dictionary<string, string>
        {
            { "{Player.SteamID}", (player?.SteamID ?? 0).ToString() },
            { "{Player.Name}", player?.PlayerName ?? _localizer["player.name.console"] },
            { "{Player.TeamName}", player.GetTeamString() },
            { "{Player.Team}", player.GetTeamString(true) },
            { "{Player.TeamNum}", player != null ? Convert.ToInt32(player.Team).ToString() : "-1" },
            { "{Server.MapName}", Server.MapName },
            { "{Server.CurPlayers}", _plugin.CurPlayers.ToString() },
            { "{Server.MaxPlayers}", Server.MaxPlayers.ToString() },
            { "{Server.Name}", _plugin.CvarHostName?.StringValue ?? _localizer["server.name.unavailable"] },
        };
    }
}