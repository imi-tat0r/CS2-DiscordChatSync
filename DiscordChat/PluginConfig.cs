using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace DiscordChat;

public class ChatFormatOptions
{
    [JsonPropertyName("TimeFormat")] public string TimeFormat { get; set; } = "HH:mm:ss";
    [JsonPropertyName("DateFormat")] public string DateFormat { get; set; } = "dd.MM.yyyy";

    [JsonPropertyName("ServerOutputFormat")]
    public string ServerOutputFormat { get; set; } = "[Discord - {Channel}] {UsernameStyled}: {Message}";

    [JsonPropertyName("SyncPrefix")] public string SyncPrefix { get; set; } = "";

    [JsonPropertyName("DiscordEmbedFields")]
    public Dictionary<string, string> DiscordEmbedFields { get; set; } = new()
    {
        {"Player", "{Player.Name}"},
        {"SteamID", "[{Player.SteamID}](https://steamcommunity.com/profiles/{Player.SteamID})"},
        {"Message", "[{Chat.Team}] - {Chat.Message}"}
    };
    
    [JsonPropertyName("DiscordEmbedFormat")]
    public Dictionary<string, string> DiscordEmbedFormat { get; set; } = new()
    {
        {"Author", ""},
        {"AvatarUrl", ""},
        {"Title", "{Server.Name}"},
        {"ThumbnailUrl", ""},
        {"Footer", "https://github.com/imi-tat0r/CS2-DiscordChatSync"},
        {"FooterIconUrl", ""},
        {"Color", "{TeamColor}"}
    };

    [JsonPropertyName("SystemMessages")]
    public Dictionary<string, string> SystemMessages { get; set; } = new()
    {
        { "PlayerConnect", "{Player.Name} joined the server" },
        { "PlayerDisconnect", "{Player.Name} left the server" },
        { "MapChange", "Changed map to {Server.MapName}" }
    };
}

public class DiscordChatSyncConfig : BasePluginConfig
{
    [JsonPropertyName("DiscordToken")] public string DiscordToken { get; set; } = "";
    [JsonPropertyName("SyncChannelId")] public ulong SyncChannelId { get; set; } = 0;
    [JsonPropertyName("SystemChannelId")] public ulong SystemChannelId { get; set; } = 0;
    [JsonPropertyName("RconChannelId")] public ulong RconChannelId { get; set; } = 0;
    [JsonPropertyName("RconMessagePrefix")] public string RconMessagePrefix { get; set; } = "";
    [JsonPropertyName("AdditionalReadChannelIds")] public List<ulong> AdditionalReadChannelIds { get; set; } = new();
    [JsonPropertyName("SyncTeamChat")] public bool SyncTeamChat { get; set; }
    [JsonPropertyName("SyncConsoleSay")] public bool SyncConsoleSay { get; set; }
    [JsonPropertyName("IgnoreChatTriggers")] public bool IgnoreChatTriggers { get; set; } = true;
    [JsonPropertyName("ChatFormatOptions")] public ChatFormatOptions ChatFormatOptions { get; set; } = new();

    [JsonPropertyName("ConfigVersion")] public override int Version { get; set; } = 4;
}