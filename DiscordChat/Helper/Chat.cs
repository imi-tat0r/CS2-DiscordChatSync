namespace DiscordChat.Helper;

public static class Chat
{
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
}