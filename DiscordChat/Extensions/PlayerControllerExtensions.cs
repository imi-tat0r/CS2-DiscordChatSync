using CounterStrikeSharp.API.Core;

namespace DiscordChat.Extensions;

public static class PlayerControllerExtensions
{
    internal static bool IsPlayer(this CCSPlayerController? player)
    {
        return player is { IsValid: true, IsHLTV: false, IsBot: false, UserId: not null };
    }
}