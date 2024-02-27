using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace DiscordChat;

public class ChatFormatOptions
{
    [JsonPropertyName("TimeFormat")] public string TimeFormat { get; set; } = "HH:mm:ss";
    [JsonPropertyName("DateFormat")] public string DateFormat { get; set; } = "dd.MM.yyyy";

    [JsonPropertyName("ServerOutputFormat")]
    public string ServerOutputFormat { get; set; } = "[Discord - {Channel}] {UsernameStyled}: {Message}";

    [JsonPropertyName("SyncPrefix")] public string SyncTrigger { get; set; } = "";

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

    [JsonPropertyName("ConfigVersion")] public override int Version { get; set; } = 2;
}