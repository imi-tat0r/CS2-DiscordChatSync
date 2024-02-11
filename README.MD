# CS2 Discord Chat Sync (1.0.0)
![image](https://du.hurenso.hn/r/Gyc70A.png)

# About
CS2 Discord Chat Sync is a plugin for Counter-Strike 2 that syncs chat between the game and a Discord channel.

# Features
- Syncs chat between the game and a Discord channel
- Syncs team chat (optional)
- Syncs chat from additional channels (optional) - useful for news and announcements
- Customizable message prefix (optional) - only messages with the prefix will be synced

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
4. Restart your server~~~~

# Config
```
{
  "DiscordToken": "", // your discord bot token
  "SyncChannelId": 0, // the channel id to sync from and to
  "AdditionalReadChannelIds": [], // additional channels to read from (for news and announcements)
  "SyncTeamChat": false, // if false, team chat will not be synced
  "MessagePrefix": "", // if empty, no prefix is needed
  "ConfigVersion": 1
}
```

# Credits
- [Metamod:Source](https://www.sourcemm.net/)
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)
- [Discord.Net](https://github.com/discord-net/Discord.Net)