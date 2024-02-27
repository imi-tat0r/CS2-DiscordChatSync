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

    [JsonPropertyName("DiscordEmbedFields")]
    public Dictionary<string, string> DiscordEmbedFields { get; set; } = new()
    {
        {"Server", "{Server.Name}"},
        {"Map", "{Server.MapName}"},
        {"Players", "{Server.CurPlayers}/{Server.MaxPlayers}"},
        {"Clickable Link", "[{Player.Name} ({Player.SteamID})](https://steamcommunity.com/profiles/{Player.SteamID})"},
        {"Team", "{Player.TeamName} - {Player.Team} - Num: {Player.TeamNum}"},
        {"Message", "[{Chat.Team}] - {Chat.Message}"},
        {"Time", "{Time}"},
        {"Date", "{Date}"}
    };
    
    [JsonPropertyName("DiscordEmbedFormat")]
    public Dictionary<string, string> DiscordEmbedFormat { get; set; } = new()
    {
        {"Author", "{Server.Name}"},
        {"AvatarUrl", "https://hvh.gg/assets/img/logo_colored.png"},
        {"Title", "Message from {Player.Name}"},
        {"ThumbnailUrl", "https://hvh.gg/assets/img/logo_colored.png"},
        {"Footer", "https://github.com/imi-tat0r/CS2-DiscordChatSync"},
        {"Color", "{TeamColor}"}
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