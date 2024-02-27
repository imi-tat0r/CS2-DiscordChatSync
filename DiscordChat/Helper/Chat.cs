using System.Reflection;
using CounterStrikeSharp.API.Modules.Utils;

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
    
    // take template + varargs
    public static IEnumerable<string> FormatDiscordMessageForChat(string messageTemplate, string channel, string username, string userColor, List<string> messageParts)
    {
        if (messageParts.Count == 0)
            return Array.Empty<string>();
        
        var firstLine = messageTemplate;
        
        firstLine = FormatColorChatInMessage(firstLine);
        firstLine = FormatConstantsInChatMessage(firstLine);
        
        firstLine = firstLine.Replace("{Channel}", channel);
        firstLine = firstLine.Replace("{Username}", username);
        firstLine = firstLine.Replace("{UsernameStyled}", $"{ColorHelper.HexColorToChatColor(userColor)}{username}{ChatColors.Default}");
        
        // if we have multiple lines, they are printed separately. If we have only one line, we print it inline 
        firstLine = firstLine.Replace("{Message}", messageParts[0]);
        
        var lines = new List<string> { firstLine };
        
        if (messageParts.Count <= 1)
            return lines;
        
        lines.AddRange(messageParts.Skip(1));
        return lines;
    }
    
    private static string FormatColorChatInMessage(string message)
    {
        foreach (var color in ChatColorTemplateVariables)
            message = message.Replace(color.Key, $"{color.Value}");

        return message;
    }
    private static string FormatConstantsInChatMessage(string message)
    {
        foreach (var constant in OtherTemplateVariables)
            message = constant.Value(message);

        return message;
    }
}