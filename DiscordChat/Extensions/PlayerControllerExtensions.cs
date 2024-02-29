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
        var teamNum = player != null ? (int)player.Team : -1;
        var teamStr = teamNum switch
        {
            (int)CsTeam.None => "team.none",
            (int)CsTeam.CounterTerrorist => "team.ct",
            (int)CsTeam.Terrorist => "team.t",
            (int)CsTeam.Spectator => "team.spec",
            _ => ""
        };

        return abbreviate ? teamStr + ".short" : teamStr + ".long";
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