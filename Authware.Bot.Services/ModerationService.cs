using Authware.Bot.Common;
using Authware.Bot.Common.Models;
using Authware.Bot.Common.Utils;
using Authware.Bot.Services.Interfaces;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace Authware.Bot.Services;

public class ModerationService : IModerationService
{
    private readonly IWritableConfigurationService<AuthwareConfiguration> _writableConfiguration;
    private readonly IConfiguration _configuration;
    private readonly DiscordSocketClient _discord;
    private readonly Random _rng = new();
    private readonly List<WarnedUser> _warnedUsers = new();

    public ModerationService(IWritableConfigurationService<AuthwareConfiguration> writableConfiguration,
                             IConfiguration configuration, DiscordSocketClient discord)
    {
        _writableConfiguration = writableConfiguration;
        _configuration = configuration;
        _discord = discord;
        _discord.GuildMemberUpdated += DiscordOnGuildMemberUpdated;
        _discord.MessageReceived += DiscordOnMessageReceived;
        _discord.MessageUpdated += DiscordOnMessageUpdated;
        _discord.ReactionAdded += DiscordOnReactionAdded;
        _discord.ReactionRemoved += DiscordOnReactionRemoved;
    }

    private async Task DiscordOnReactionRemoved(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2, SocketReaction arg3)
    {
        await UpdateWarnedUsers(await _discord.GetUserAsync(arg3.UserId) as SocketGuildUser);
    }

    private async Task DiscordOnReactionAdded(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2, SocketReaction arg3)
    {
        await UpdateWarnedUsers(await _discord.GetUserAsync(arg3.UserId) as SocketGuildUser);
    }

    private async Task DiscordOnMessageUpdated(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
    {
        await UpdateWarnedUsers(arg2.Author as SocketGuildUser);
    }

    private async Task UpdateWarnedUsers(SocketGuildUser user)
    {
        if (user is null) return;
        // Determine if the user has been punished and their punishment has expired
        if (_warnedUsers.Any(x => x.User.Id == user.Id && x.IsBeingPunished) && user.IsTimedOut())
        {
            // They have, fetch the user and reset their user values
            var warnedUser = _warnedUsers.FirstOrDefault(x => x.User.Id == user.Id);
            warnedUser.IsBeingPunished = false;
            warnedUser.WarnCount = 0;
        }
    }

    private async Task DiscordOnMessageReceived(SocketMessage arg)
    {
        await UpdateWarnedUsers(arg.Author as SocketGuildUser);
    }

    private async Task DiscordOnGuildMemberUpdated(Cacheable<SocketGuildUser, ulong> arg1, SocketGuildUser arg2)
    {
        // Determine if the user has been punished and their punishment has expired
        if (_warnedUsers.Any(x => x.User.Id == arg2.Id && x.IsBeingPunished) && arg2.IsTimedOut())
        {
            // They have, fetch the user and reset their user values
            var warnedUser = _warnedUsers.FirstOrDefault(x => x.User.Id == arg2.Id);
            warnedUser.IsBeingPunished = false;
            warnedUser.WarnCount = 0;
        }
    }

    private int GenerateCaseNumber()
    {
        return _rng.Next(10000, 99999);
    }

    public async Task<int> WarnUserAsync(SocketGuildUser user, IUser moderatingUser, string reason)
    {
        if (_warnedUsers.All(x => x.User.Id != user.Id))
        {
            _warnedUsers.Add(new WarnedUser
            {
                WarnCount = 1,
                User = user,
                IsBeingPunished = false
            });
        }
        else
        {
            var warnedUser = _warnedUsers.FirstOrDefault(x => x.User.Id == user.Id);
            warnedUser.WarnCount++;
            if (warnedUser.WarnCount < _writableConfiguration.Load().StrikeThreshold) return 0;
            
            // Punish user
            warnedUser.IsBeingPunished = true;
            await warnedUser.User.SetTimeOutAsync(TimeSpan.FromMinutes(5));
                
            await CreateCaseAsync(user, moderatingUser, "Timed out", reason);
        }

        return 0;
    }

    public async Task<int> CreateCaseAsync(IUser user, IUser moderatingUser, string action, string reason)
    {
        var guild = _discord.GetGuild(ulong.Parse(_configuration["GuildId"]));
        var channel = guild.GetTextChannel(_writableConfiguration.Load().ModerationChannel);

        var caseNumber = GenerateCaseNumber();

        var embed = new AuthwareEmbedBuilder()
                   .WithAuthor($"{moderatingUser.Username}#{moderatingUser.Discriminator}",
                               moderatingUser.GetAvatarUrl())
                   .WithTitle($"Case #{caseNumber}")
                   .AddField("Action", action, true)
                   .AddField("User", user.Mention, true)
                   .AddField("Reason", reason, true)
                   .Build();

        await channel.SendMessageAsync(embed: embed);

        // Try to DM them the case information
        try
        {
            var dmChannel = await user.CreateDMChannelAsync();
            await dmChannel.SendMessageAsync($"You've been moderated in **{guild.Name}**, here's your case info",
                                             embed: embed);
        }
        catch (HttpException)
        {
            // Ignored, DMs are off.
        }

        return caseNumber;
    }
}