using Microsoft.Extensions.Hosting;

namespace Jex.Tools.OpenPullRequests;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        await Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                var startup = new Startup(context.Configuration);
                startup.ConfigureServices(services);
            })
            .UseConsoleLifetime(options => options.SuppressStatusMessages = true)
            .RunConsoleAsync();
    }
}