![Copyright ev0lve Digital](https://img.shields.io/badge/Copyright-ev0lve%20Digital-blue) ![GitHub License](https://img.shields.io/github/license/imi-tat0r/CS2-DiscordChatSync) ![Issues](https://img.shields.io/github/issues/imi-tat0r/CS2-DiscordChatSync) ![Downloads](https://img.shields.io/github/downloads/imi-tat0r/CS2-DiscordChatSync/total) ![Stars](https://img.shields.io/github/stars/imi-tat0r/CS2-DiscordChatSync)

# CS2 Discord Chat Sync (1.1.0)
![image](https://du.hurenso.hn/r/17JDsG.png)

# About
CS2 Discord Chat Sync is a plugin for Counter-Strike 2 that syncs chat between the game and a Discord channel. It features vast customization options for the message formatting and the Discord embeds. It also supports syncing team chat and chat from additional channels.

# Features
- Syncs chat between the game and a Discord channel
- Syncs team chat (optional)
- Syncs chat from additional channels (optional) - useful for news and announcements
- Ignores chat and silent triggers from core config (optional)
- Customizable Discord template for chat messages
- Customizable message template for Discord messages
- SyncPrefix to only sync messages with a certain prefix

# Dependencies
[Metamod:Source (2.x)](https://www.sourcemm.net/downloads.php/?branch=master)  
[CounterStrikeSharp(v166)](https://github.com/roflmuffin/CounterStrikeSharp/releases)

# Installation
1. Install Metamod:Source and CounterStrikeSharp
2. Place the `addons` folder in your servers `game/csgo/` directory  
   ![extract](https://du.hurenso.hn/r/0NyFPY.png)
3. Create a Discord bot and invite it to your server  
   3.1. [Create a Discord bot](https://discord.com/developers/applications)  
   3.2. [Invite the bot to your server](https://discordapi.com/permissions.html)  
   3.3. The Discord bot requires the following Privileged Gateway Intent:  
   ![perms](https://du.hurenso.hn/r/kTDZ8O.png)
4. Add your Discord bot token and channel id to the config file  
   4.1. Located at `addons/counterstrikesharp/configs/plugins/DiscordChat/DiscordChat.json`
4. Restart your server

# Message formatting
## 1. Discord => CS2
The following placeholders can be used in the `ServerOutputFormat` to dynamically display certain information about the Discord message or user.
### 1.1. Message Information
- `{Channel}` - The Discord channel name
- `{Username}` - The player's name
- `{UsernameStyled}` - The player's name styled with their Discord role color

### 1.2. Time and Date
- `{Time}` - The current time according to `TimeFormat`
- `{Date}` - The current date according to `DateFormat`
- [More Information](https://learn.microsoft.com/dotnet/standard/base-types/custom-date-and-time-format-strings)


## 2. CS2 => Discord
The following placeholders can be used in the `DiscordEmbedFormat` and the `DiscordEmbedFields` to dynamically display certain information about the server or the player.
### 2.1. Player
- `{Player.Name}` - The player's name
- `{Player.SteamID}` - The player's SteamID
- `{Player.Team}` - The player's short team name
- `{Player.TeamName}` - The player's full team name
- `{Player.TeamNum}` - The player's team number
### 2.2. Server
- `{Server.Name}` - The server's name
- `{Server.MapName}` - The server's map name
- `{Server.CurPlayers}` - The server's current player count
- `{Server.MaxPlayers}` - The server's maximum player count
### 2.3. Chat
- `{Chat.Message}` - The chat message
- `{Chat.Team}` - The receiving team of the chat message (All, T, CT, Spec)

### 2.4. Time and Date
- `{Time}` - The current time according to `TimeFormat`
- `{Date}` - The current date according to `DateFormat`
- [More Information](https://learn.microsoft.com/dotnet/standard/base-types/custom-date-and-time-format-strings)

# Config
```json
{
   // your discord bot token
   "DiscordToken": "",
   // the channel id to sync from and to
   "SyncChannelId": 0,
   // additional channels to read from (for news and announcements)
   "AdditionalReadChannelIds": [],
   // if false, team chat will not be synced
   "SyncTeamChat": false,
   // DEPRECATED - use ChatFormatOptions.SyncPrefix instead
   "MessagePrefix": "",
   // if true, chat and silent triggers from the core config will be ignored
   "IgnoreChatTriggers": true,
   
   // options for output formatting
   "ChatFormatOptions": {
      // time + date format 
      "TimeFormat": "HH:mm:ss",
      "DateFormat": "dd.MM.yyyy",
      // format string for the chat output of discord messages
      "ServerOutputFormat": "[From Discord #{Channel}] {UsernameStyled} - {Green}{Date} {Blue}{Time}{Default}: {Message}",
      // prefix for messages that should be synced (empty for all messages)
      "SyncPrefix": "",
      
      // dictionary of fields for the discord embed
      // you can use all chat placeholders
      "DiscordEmbedFields": {
         "Server": "{Server.Name}",
         "Map": "{Server.MapName}",
         "Players": "{Server.CurPlayers}/{Server.MaxPlayers}",
         ":ballot_box_with_check: Player": "[{Player.Name} ({Player.SteamID})](https://steamcommunity.com/profiles/{Player.SteamID})",
         "Team": "{Player.TeamName} - {Player.Team} - Num: {Player.TeamNum}",
         "Message": "[{Chat.Team}] - {Chat.Message}",
         "Time": "{Time}",
         "Date": "{Date}"
      },
      
      // dictionary of general embed settings
      // you can set the author, avatar (requires author), title, thumbnail, footer and color
      // you can use all chat placeholders
      "DiscordEmbedFormat": {
         "Author": "{Server.Name}", // author of the embed (optional)
         "AvatarUrl": "", // url to the author's avatar (optional) - requires author
         "Title": "Message from {Player.Name}",
         "ThumbnailUrl": "", // url to the thumbnail (optional)
         "Footer": "https://github.com/imi-tat0r/CS2-DiscordChatSync",
         "FooterIconUrl": "", // url to the footer icon (optional)
         
         // hex color code #RRGGBB or "{TeamColor}" for the player's team color
         "Color": "{TeamColor}"
      }
   },
   
   "ConfigVersion": 2 // do not change
}
```

# Credits
- [Metamod:Source](https://www.sourcemm.net/)
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)
- [Discord.Net](https://github.com/discord-net/Discord.Net)
