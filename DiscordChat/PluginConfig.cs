using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace DiscordChat;

public class DiscordChatSyncConfig : BasePluginConfig
{
    [JsonPropertyName("DiscordToken")] public string DiscordToken { get; set; } = "";
    [JsonPropertyName("SyncChannelId")] public ulong SyncChannelId { get; set; } = 0;
    [JsonPropertyName("AdditionalReadChannelIds")] public List<ulong> AdditionalReadChannelIds { get; set; } = new();
    [JsonPropertyName("SyncTeamChat")] public bool SyncTeamChat { get; set; }
    [JsonPropertyName("MessagePrefix")] public string MessagePrefix { get; set; } = "";
    [JsonPropertyName("IgnoreChatTriggers")] public bool IgnoreChatTriggers { get; set; } = true;
}