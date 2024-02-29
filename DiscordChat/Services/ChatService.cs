using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using DiscordChat.Enums;
namespace DiscordChat.Services;

public class ChatService
{
    private readonly DiscordChatSync _plugin;
    private readonly MessageService _messageService;

    public ChatService(DiscordChatSync plugin, MessageService messageService)
    {
        _plugin = plugin;
        _messageService = messageService;
    }

    public HookResult OnClientCommandGlobalPre(CCSPlayerController? player, CommandInfo info)
    {
        if (info.ArgCount < 2)
            return HookResult.Continue;
        
        var command = info.GetArg(0);
        var message = info.GetArg(1);

        if (command != "say" && command != "say_team")
            return HookResult.Continue;

        if (string.IsNullOrWhiteSpace(message))
            return HookResult.Continue;
        
        var isChatTrigger = CoreConfig.PublicChatTrigger.Contains(message[0].ToString()) ||
                            CoreConfig.SilentChatTrigger.Contains(message[0].ToString());

        if (_plugin.Config.IgnoreChatTriggers && isChatTrigger)
            return HookResult.Continue;

        var type = _messageService.GetChatMessageType(player, command);
        
        return OnSay(player, type, info);
    }

    private HookResult OnSay(CCSPlayerController? player, ChatMessageType type, CommandInfo info)
    {
        Console.WriteLine($"[DiscordChatSync] OnSay");
        if (!_messageService.TryGetFullChatMessage(info, out var message))
            return HookResult.Continue;

        switch (type)
        {
            case ChatMessageType.Unknown:
                break;
            case ChatMessageType.Chat:
            case ChatMessageType.TeamChat:
            case ChatMessageType.Console:
                // ignore console messages if the option is disabled
                if (type == ChatMessageType.Console && !_plugin.Config.SyncConsoleSay)
                    break;
                
                // check and adjust for sync prefix
                if (!CheckSyncPrefix(message))
                    break;
                message = message[_plugin.Config.ChatFormatOptions.SyncPrefix.Length..];
                
                _messageService.SyncChatMessage(player, type == ChatMessageType.TeamChat, message);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        return HookResult.Continue;
    }

    private bool CheckSyncPrefix(string message)
    {
        // either no sync trigger is set or the message starts with it
        return string.IsNullOrEmpty(_plugin.Config.ChatFormatOptions.SyncPrefix) ||
               message.StartsWith(_plugin.Config.ChatFormatOptions.SyncPrefix);
    }
}