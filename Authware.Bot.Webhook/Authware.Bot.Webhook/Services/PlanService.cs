namespace Authware.Bot.Webhook.Services;

public class PlanService
{
    private readonly DiscordSocketClient _client = Globals.ServiceProvider.GetRequiredService<DiscordSocketClient>();
    private readonly IConfiguration _configuration = Globals.ServiceProvider.GetRequiredService<IConfiguration>();
    private readonly ILogger<PlanService> _logger = Globals.ServiceProvider.GetRequiredService<ILogger<PlanService>>();

    [ResourceMethod(RequestMethod.POST, "update")]
    public async ValueTask<DefaultResponse> UpdateRolesAsync(WebhookUserForm form)
    {
#if DEBUG
        var guildId = _configuration["TestingGuildId"];
#else
        var guildId = _configuration["GuildId"];
#endif

        var guild = _client.GetGuild(ulong.Parse(guildId));
        var user = guild?.GetUser(form.UserId);
        var enterpriseRole =
            guild?.Roles.FirstOrDefault(x => x.Name.Equals("enterprise", StringComparison.OrdinalIgnoreCase));
        var proRole = guild?.Roles.FirstOrDefault(x => x.Name.Equals("pro", StringComparison.OrdinalIgnoreCase));
        var basicRole = guild?.Roles.FirstOrDefault(x => x.Name.Equals("basic", StringComparison.OrdinalIgnoreCase));
        var sellerRole =
            guild?.Roles.FirstOrDefault(x => x.Name.Equals("verified seller", StringComparison.OrdinalIgnoreCase));


        if (user is null || guild is null || enterpriseRole is null || proRole is null || basicRole is null ||
            sellerRole is null)
        {
            var badFormResponse = new DefaultResponse(6, "User or guild not found");
            return badFormResponse;
        }

        _logger.LogInformation("Received {Action} from webhook service", form.Intent);

        switch (form.Intent)
        {
            case WebhookUpdateIntent.USER_LINK_REMOVED:
            {
                await user.RemoveRolesAsync(new[]
                {
                    basicRole.Id,
                    proRole.Id,
                    sellerRole.Id,
                    enterpriseRole.Id
                });

                _logger.LogInformation("Finished processing action");

                break;
            }
            case WebhookUpdateIntent.USER_UPDATED:
            case WebhookUpdateIntent.USER_LINK_ADDED:
            {
                if (form.RemovedRoles is not null)
                {
                    var removedRoleIds = (from roleName in form.RemovedRoles
                        select guild.Roles.FirstOrDefault(x =>
                            x.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase))
                        into role
                        where role is not null
                        select role.Id).ToList();

                    await user.RemoveRolesAsync(removedRoleIds);
                }

                if (form.AddedRoles is not null)
                {
                    var addedRoleIds = (from roleName in form.AddedRoles
                        select guild.Roles.FirstOrDefault(x =>
                            x.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase))
                        into role
                        where role is not null
                        select role.Id).ToList();

                    await user.AddRolesAsync(addedRoleIds);
                }

                _logger.LogInformation("Updated roles for user");
                break;
            }
            default:
            {
                var invalidActionResponse = new DefaultResponse(14, "Invalid action");
                return invalidActionResponse;
            }
        }

        var okResponse = new DefaultResponse(0, "User updated successfully");
        return okResponse;
    }
}