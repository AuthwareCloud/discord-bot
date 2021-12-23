<p align="center">
  <img src="https://github.com/AuthwareCloud/AuthwareDotNet/raw/main/authware-s.png" width="75" height="75">
  <h1 align="center">Authware Bot</h1>
  <p align="center">The Discord bot we built for our community Discord server, it provides moderation &amp; a load of other useful features</p>
</p>

## 📲 Installation / Self-hosting
Follow the instructions in Compilation first in-order to generate the binaries and settings to run the bot.

Once you've compiled the bot, you need an 'appsettings.json' file, we've excluded ours as it contains our bot token and other private details, here's a template for one though. Copy the template and create a file called 'appsettings.json' in the folder the bot files are in. Change the bot token in the 'appsettings.json' file to whatever your bot token is.

```js
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "System": "Information",
      "Microsoft": "Information"
    }
  },
  "SocketConfig": {
    "MessageCacheSize": 1000,
    "UseSystemClock": true,
    "AlwaysAcknowledgeInteractions": true,
    "AlwaysDownloadUsers": true
  },
  "AntiSpam": {
    "StrikeThreshold": 5,
    "MessageInterval": 0.75
  },
  "Token": "Your bot token"
}
```

Then see the section below for compiling the bot

## 🖥️ Compilation
In-order for compilation of the bot, you must have the following:

- .NET SDK 6.0+

Note: Compilation with .NET 6.0+ is required for the instructions noted here.

1. Clone the repository

```
git clone https://github.com/AuthwareCloud/AuthwareBot.git && cd AuthwareBot
```

2. Tell `dotnet` to compile the bot

```
dotnet build
```

3. All done! Navigate to the bin/Release or bin/Debug folder to find the bot files.

## 📜 License
Licensed under the MIT license, see LICENSE.MD

## 📖 Open-source libraries
- [Discord.Net](https://github.com/discord-net/Discord.Net)
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)
