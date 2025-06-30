using Jex.Tools.CLI.Core;
using Jex.Tools.OpenPullRequests.Configuration;
using Jex.Tools.OpenPullRequests.Display;
using Jex.Tools.OpenPullRequests.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Jex.Tools.OpenPullRequests;

/// <summary>
/// CLI module for scanning Azure DevOps pull requests.
/// </summary>
public sealed class PullRequestScannerModule : ICliModule
{
    public string Name => "Azure DevOps Pull Request Scanner";
    public string Description => "Scans all projects in Azure DevOps for open pull requests";
    public string Command => "pr-scan";
    public string Version => "1.0.0";
    
    public async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Any(IsHelpArgument))
        {
            ShowHelp();
            return 0;
        }
        
        try
        {
            var config = LoadConfiguration(args);
            
            if (!ConfigurationValidator.Validate(config))
            {
                return 1;
            }
            
            var services = ConfigureServices(config);
            await using var serviceProvider = services.BuildServiceProvider();
            
            var scanner = serviceProvider.GetRequiredService<OrganizationScanner>();
            await scanner.ScanAsync();
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
    
    public void ShowHelp()
    {
        Console.WriteLine($"{Name} v{Version}");
        Console.WriteLine();
        Console.WriteLine("Usage: jex-tools pr-scan [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --org <url>          Azure DevOps organization URL");
        Console.WriteLine("  --pat <token>        Personal Access Token");
        Console.WriteLine("  --my-prs-only        Show only my pull requests (default: true)");
        Console.WriteLine("  --all-prs            Show all pull requests");
        Console.WriteLine("  -h, --help           Show this help message");
        Console.WriteLine();
        Console.WriteLine("Configuration can also be set in appsettings.json or environment variables:");
        Console.WriteLine("  AZDEVOPS_ORG_URL");
        Console.WriteLine("  AZDEVOPS_PAT");
        Console.WriteLine("  AZDEVOPS_SHOW_ONLY_MY_PRS");
    }
    
    private static AzureDevOpsConfiguration LoadConfiguration(string[] args)
    {
        var config = ConfigurationLoader.Load();
        ParseCommandLineArgs(args, config);
        return config;
    }
    
    private static void ParseCommandLineArgs(string[] args, AzureDevOpsConfiguration config)
    {
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i].ToLowerInvariant();
            
            switch (arg)
            {
                case "--org" when i + 1 < args.Length:
                    config.OrganizationUrl = args[++i];
                    break;
                    
                case "--pat" when i + 1 < args.Length:
                    config.PersonalAccessToken = args[++i];
                    break;
                    
                case "--my-prs-only":
                    config.ShowOnlyMyPullRequests = true;
                    break;
                    
                case "--all-prs":
                    config.ShowOnlyMyPullRequests = false;
                    break;
            }
        }
    }
    
    private static IServiceCollection ConfigureServices(AzureDevOpsConfiguration config)
    {
        var services = new ServiceCollection();
        
        services.AddSingleton(config);
        
        services.AddSingleton<VssConnection>(provider =>
        {
            var configuration = provider.GetRequiredService<AzureDevOpsConfiguration>();
            var credentials = new VssBasicCredential(string.Empty, configuration.PersonalAccessToken);
            return new VssConnection(new Uri(configuration.OrganizationUrl), credentials);
        });
        
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IRepositoryService, RepositoryService>();
        services.AddScoped<IPullRequestService, PullRequestService>();
        services.AddScoped<IDisplayService, ConsoleDisplayService>();
        services.AddScoped<OrganizationScanner>();
        
        return services;
    }
    
    private static bool IsHelpArgument(string arg) =>
        arg.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
        arg.Equals("-h", StringComparison.OrdinalIgnoreCase);
}