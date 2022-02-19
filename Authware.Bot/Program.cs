namespace Authware.Bot;

internal static class Program
{
    [STAThread]
    public static async Task Main(string[] args)
    {
        await new Startup(args).RunAsync();
    }
}