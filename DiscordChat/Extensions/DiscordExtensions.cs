using Discord.WebSocket;

namespace DiscordChat.Extensions;

public static class DiscordExtensions
{
    public static bool GetGuildUser(this SocketUser? author, out SocketGuildUser user)
    {
        if (author is SocketGuildUser guildUser)
        {
            user = guildUser;
            return guildUser is { IsBot: false, IsWebhook: false };
        }
        
        user = null!;
        return false;
    }

    public static SocketRole? GetHighestRole(this SocketGuildUser user)
    {
        var roles = user.Roles.ToList();
        return roles.MaxBy(x => x.Position);
    }
    
    public static string GetHexColor(this SocketRole? role)
    {
        return role != null
            ? "#" +
              role.Color.R.ToString("X2") +
              role.Color.G.ToString("X2") +
              role.Color.B.ToString("X2")
            : "#ffffff";
    }
}