using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Discord;
using Discord.WebSocket;
using DiscordChat.Extensions;
using DiscordChat.Helper;
using Serilog;

namespace DiscordChat.Services;

public class MessageService
{
    private readonly DiscordChatSync _plugin;

    public MessageService(DiscordChatSync plugin)
    {
        _plugin = plugin;
    }
    
    public bool ShouldSyncDiscordMessage(ulong channelId)
    {
        return _plugin.Config.SyncChannelId == channelId || _plugin.Config.AdditionalReadChannelIds.Contains(channelId);
    }
    public bool ShouldSyncChatMessage(CommandInfo info, bool all, out string outMessage)
    {
        var inMessage = info.ArgString;
        
        // strip quotes from the message
        if (inMessage.StartsWith("\"") && inMessage.EndsWith("\""))
            inMessage = inMessage[1..^1];
        
        outMessage = inMessage.Trim();
        
        if (!all && !_plugin.Config.SyncTeamChat)
            return false;
        
        // ignore empty messages
        if (string.IsNullOrWhiteSpace(outMessage))
            return false;

        // if both are empty, we don't sync anything (MessagePrefix is deprecated)
        if (string.IsNullOrEmpty(_plugin.Config.ChatFormatOptions.SyncTrigger) &&
            string.IsNullOrEmpty(_plugin.Config.MessagePrefix))
            return true;
        
        // deprecated MessagePrefix
        if (!string.IsNullOrEmpty(_plugin.Config.MessagePrefix))
        {
            // warn about deprecation
            Log.Information("{Message}",
                "MessagePrefix is deprecated. Please use ChatFormatOptions.SyncTrigger instead.");
        
            // ignore messages that don't start with the message prefix
            if (!outMessage.StartsWith(_plugin.Config.MessagePrefix))
                return false;
            
            // remove the prefix from the message
            outMessage = outMessage[_plugin.Config.MessagePrefix.Length..].Trim();
            
            return true;
        }
        
        // ignore messages that don't start with the sync trigger
        if (!outMessage.StartsWith(_plugin.Config.ChatFormatOptions.SyncTrigger))
            return false;
            
        // remove the prefix from the message
        outMessage = outMessage[_plugin.Config.ChatFormatOptions.SyncTrigger.Length..].Trim();
            
        return true;
    }
    
    public bool ShouldSyncTeamChat()
    {
        return _plugin.Config.SyncTeamChat;
    }


    public void SyncDiscordMessage(SocketUserMessage msg, SocketGuildUser user)
    {
        var userRole = user.GetHighestRole();
        
        var messageSplit = GetMessageArray(msg);

        var lines = Chat.FormatDiscordMessageForChat(_plugin.Config.ChatFormatOptions.ServerOutputFormat,
            msg.Channel.Name, user.DisplayName, userRole.GetHexColor(), messageSplit);

        Server.NextWorldUpdate(() =>
        {
            foreach (var line in lines)
                Server.PrintToChatAll(line);
        });
    }

    private static List<string> GetMessageArray(IMessage msg)
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
}