using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Discord;
using Discord.WebSocket;
using DiscordChat.Extensions;
using Serilog;

namespace DiscordChat.Helper;

public static class Chat
{
    public static string TimeFormat { get; set; } = "HH:mm:ss";
    public static string DateFormat { get; set; } = "dd.MM.yyyy";

    private static readonly Dictionary<string, char> ChatColorTemplateVariables = typeof(ChatColors)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .ToDictionary(
            field => $"{{{field.Name}}}",
            field => (char)(field.GetValue(null) ?? '\x01')
        );
    
    // a static dictionary of constants and functions which transforms the input value
    private static readonly Dictionary<string, Func<string, string>> OtherTemplateVariables = new()
    {
        { "{Time}", (message) => message.Replace("{Time}", $"{DateTime.Now.ToString(TimeFormat)}") },
        { "{Date}", (message) => message.Replace("{Date}", $"{DateTime.Now.ToString(DateFormat)}") },
    };

    public static string ForceBreakLongWords(string line)
    {
        var split = line.Split(' ');

        var newLine = "";
                
        foreach (var s in split)
        {
            if (s.Length < 50)
                newLine += s + " ";
            else
            {
                newLine += s[..50] + " ";
                newLine += s[50..] + " ";
            }
        }
                
        return newLine;
    }
    
    public static IEnumerable<string> FormatDiscordMessageForChat(string messageTemplate, SocketUserMessage message, SocketGuildUser user, List<string> messageParts)
    {
        if (messageParts.Count == 0)
            return Array.Empty<string>();
        
        var firstLine = ApplyChatMessageFormat(messageTemplate, message, user, messageParts);

        var lines = new List<string> { firstLine };
        
        if (messageParts.Count <= 1)
            return lines;
        
        lines.AddRange(messageParts.Skip(1));
        return lines;
    }
    public static Embed FormatChatMessageForDiscord(Dictionary<string, string> embedFormat, Dictionary<string, string> embedFields, Dictionary<string, string> dynamicReplacements, CCSPlayerController? player, bool teamOnly, string message)
    {
        var embed = new EmbedBuilder();

        var chatTeam = teamOnly ? $"{player.GetTeamString(true)}" : "All";
        
        dynamicReplacements.Add("{Chat.Message}", message);
        dynamicReplacements.Add("{Chat.Team}", chatTeam);
        
        foreach (var format in embedFormat)
        {
            switch (format.Key)
            {
                case "Author":
                    if (string.IsNullOrEmpty(embedFormat["Author"]))
                        break;
                    
                    if (!string.IsNullOrEmpty(embedFormat["AvatarUrl"]))
                        embed.WithAuthor(ApplyDiscordMessageFormat(embedFormat["Author"], dynamicReplacements), embedFormat["AvatarUrl"]);
                    else
                        embed.WithAuthor(ApplyDiscordMessageFormat(embedFormat["Author"], dynamicReplacements));
                    break;
                case "Title":
                    if (!string.IsNullOrEmpty(embedFormat["Title"]))
                        embed.WithTitle(ApplyDiscordMessageFormat(embedFormat["Title"], dynamicReplacements));
                    break;
                case "ThumbnailUrl":
                    if (!string.IsNullOrEmpty(embedFormat["ThumbnailUrl"]))
                        embed.WithThumbnailUrl(embedFormat["ThumbnailUrl"]);
                    break;
                case "Footer":
                    if (string.IsNullOrEmpty(embedFormat["Footer"]))
                        break;

                    if (!string.IsNullOrEmpty(embedFormat["FooterIconUrl"]))
                        embed.WithFooter(ApplyDiscordMessageFormat(embedFormat["Footer"], dynamicReplacements), embedFormat["FooterIconUrl"]);
                    else
                        embed.WithFooter(ApplyDiscordMessageFormat(embedFormat["Footer"], dynamicReplacements));
                    break;
                case "Color":
                    if (string.IsNullOrEmpty(embedFormat["Color"]))
                        break;
                    
                    if (embedFormat["Color"] == "{TeamColor}")
                        embed.WithColor(ColorHelper.ChatColorToDiscordColor(player.GetChatColor()));
                    else if (embedFormat["Color"].StartsWith("#") && embedFormat["Color"].Length == 7)
                        embed.WithColor(ColorHelper.HexColorToDiscordColor(embedFormat["Color"]));
                    else
                        Log.Error("Invalid color format in DiscordEmbedFormat");
                    break;
            }
        }
        
        foreach (var field in embedFields)
        {
            embed.AddField(ApplyDiscordMessageFormat(field.Key, dynamicReplacements),
                ApplyDiscordMessageFormat(field.Value, dynamicReplacements));
        }
        
        return embed.Build();
    }

    private static string ApplyChatMessageFormat(string messageTemplate, SocketMessage message, SocketGuildUser user,
        IReadOnlyList<string> messageParts)
    {
        var firstLine = messageTemplate;
        
        firstLine = FormatColorInChatMessage(firstLine);
        firstLine = FormatConstantsInMessage(firstLine);

        var username = user.Nickname ?? user.DisplayName;
        
        var dynamicReplacements = new Dictionary<string, string>
        {
            { "{Channel}", message.Channel.Name },
            { "{Username}", username },
            { "{UsernameStyled}", $"{ColorHelper.HexColorToChatColor(user.GetHighestRole().GetHexColor())}{username}{ChatColors.Default}" },
        };
        
        firstLine = FormatDynamicReplacements(firstLine, dynamicReplacements);
        
        // if we have multiple lines, they are printed separately. If we have only one line, we print it inline 
        firstLine = firstLine.Replace("{Message}", messageParts[0]);
        return firstLine;
    }
    
    private static string ApplyDiscordMessageFormat(string messageTemplate,
        Dictionary<string, string> dynamicReplacements)
    {
        var message = messageTemplate;
        
        message = FormatConstantsInMessage(message);
        message = FormatDynamicReplacements(message, dynamicReplacements);
        
        return message;
    }
    
    private static string FormatColorInChatMessage(string message)
    {
        foreach (var color in ChatColorTemplateVariables)
            message = message.Replace(color.Key, $"{color.Value}");

        return message;
    }
    private static string FormatConstantsInMessage(string message)
    {
        foreach (var constant in OtherTemplateVariables)
            message = constant.Value(message);

        return message;
    }
    private static string FormatDynamicReplacements(string message, Dictionary<string, string> dynamicReplacements)
    {
        foreach (var replacement in dynamicReplacements)
            message = message.Replace(replacement.Key, replacement.Value);

        return message;
    }
}