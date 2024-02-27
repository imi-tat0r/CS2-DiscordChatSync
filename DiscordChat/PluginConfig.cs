using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace DiscordChat;

public class ChatFormatOptions
{
    [JsonPropertyName("TimeFormat")] public string TimeFormat { get; set; } = "HH:mm:ss";
    [JsonPropertyName("DateFormat")] public string DateFormat { get; set; } = "dd.MM.yyyy";

    [JsonPropertyName("ServerOutputFormat")]
    public string ServerOutputFormat { get; set; } = "[Discord - {Channel}] {UsernameStyled} ({Date} {Time}): {Message}";

    [JsonPropertyName("SyncPrefix")] public string SyncTrigger { get; set; } = "";

    [JsonPropertyName("DiscordOutputFormat")]
    public Dictionary<string, string> DiscordOutputFormat { get; set; } = new()
    {
        {":ballot_box_with_check: Player", "{PlayerName}"},
        {"SteamID", "{SteamID}"},
        {"Clickable Link", "[{PlayerName}](https://steamcommunity.com/profiles/{SteamID})"},
        {"Team", "{Team}"},
        {"Message", "{Message}"},
        {"Server", "{ServerName}"},
        {"Map", "{MapName}"},
        {"Players", "{PlayerCount}/{MaxPlayers}"},
        {"Time", "{Time}"},
        {"Date", "{Date}"}
    };
}

public class DiscordChatSyncConfig : BasePluginConfig
{
    [JsonPropertyName("DiscordToken")] public string DiscordToken { get; set; } = "";
    [JsonPropertyName("SyncChannelId")] public ulong SyncChannelId { get; set; } = 0;
    [JsonPropertyName("AdditionalReadChannelIds")] public List<ulong> AdditionalReadChannelIds { get; set; } = new();
    [JsonPropertyName("SyncTeamChat")] public bool SyncTeamChat { get; set; }
    [JsonPropertyName("MessagePrefix")] public string MessagePrefix { get; set; } = "";
    [JsonPropertyName("IgnoreChatTriggers")] public bool IgnoreChatTriggers { get; set; } = true;
    
    [JsonPropertyName("ChatFormatOptions")] public ChatFormatOptions ChatFormatOptions { get; set; } = new();
    
}