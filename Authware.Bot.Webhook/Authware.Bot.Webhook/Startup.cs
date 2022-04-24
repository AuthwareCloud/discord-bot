namespace Authware.Bot.Webhook;

public class Startup
{
    public static int Run()
    {
        var api = Layout.Create()
            .AddService<PlanService>("users")
            .AddRangeSupport()
            .Add(CorsPolicy.Permissive());

        return Host.Create()
            .Handler(api)
            .Defaults(rangeSupport: true)
            .Console()
#if DEBUG
            .Development()
#endif
            .Run();
    }
}