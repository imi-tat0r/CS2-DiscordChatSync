using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace DiscordChat.Extensions;

public static class PlayerControllerExtensions
{
    internal static bool IsPlayer(this CCSPlayerController? player)
    {
        return player is { IsValid: true, IsHLTV: false, IsBot: false, UserId: not null, SteamID: >0 };
    }

    internal static string GetTeamString(this CCSPlayerController? player, bool abbreviate = false)
    {
        return (player?.Team ?? 0) switch
        {
            CsTeam.None => abbreviate ? "-" : "None",
            CsTeam.CounterTerrorist => abbreviate ? "CT" : "Counter-Terrorist",
            CsTeam.Terrorist => abbreviate ? "T" : "Terrorist",
            CsTeam.Spectator => abbreviate ? "Spec" : "Spectator",
            _ => ""
        };
    }

    internal static char GetChatColor(this CCSPlayerController? player)
    {
        return (player?.Team ?? 0) switch
        {
            CsTeam.Terrorist => ChatColors.Orange,
            CsTeam.CounterTerrorist => ChatColors.Blue,
            CsTeam.Spectator => ChatColors.LightPurple,
            _ => ChatColors.Default
        };
    }
}