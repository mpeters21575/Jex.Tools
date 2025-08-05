using Jex.Tools.OpenPullRequests.Configuration;
using Jex.Tools.OpenPullRequests.Display;
using Jex.Tools.OpenPullRequests.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Jex.Tools.OpenPullRequests;

public class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register configuration
        services.AddSingleton<AzureDevOpsConfiguration>(_ =>
        {
            var configSection = configuration.GetSection("AzureDevOps");
            
            // Get values with environment variable overrides
            var organizationUrl = Environment.GetEnvironmentVariable("AZDEVOPS_ORG_URL") 
                ?? configSection["OrganizationUrl"] 
                ?? string.Empty;
                
            var personalAccessToken = Environment.GetEnvironmentVariable("AZDEVOPS_PAT") 
                ?? configSection["PersonalAccessToken"] 
                ?? string.Empty;
            
            var showOnlyMyPRs = true; // Default value
            var showOnlyMyPRsEnv = Environment.GetEnvironmentVariable("AZDEVOPS_SHOW_ONLY_MY_PRS");
            if (!string.IsNullOrEmpty(showOnlyMyPRsEnv) && bool.TryParse(showOnlyMyPRsEnv, out var parsedEnv))
            {
                showOnlyMyPRs = parsedEnv;
            }
            else if (bool.TryParse(configSection["ShowOnlyMyPullRequests"], out var parsedConfig))
            {
                showOnlyMyPRs = parsedConfig;
            }
            
            // Create configuration with required properties
            var config = new AzureDevOpsConfiguration
            {
                OrganizationUrl = organizationUrl,
                PersonalAccessToken = personalAccessToken,
                ShowOnlyMyPullRequests = showOnlyMyPRs
            };

            if (!ConfigurationValidator.Validate(config))
            {
                throw new InvalidOperationException("Invalid configuration");
            }

            return config;
        });

        // Register VssConnection
        services.AddSingleton<VssConnection>(provider =>
        {
            var config = provider.GetRequiredService<AzureDevOpsConfiguration>();
            var credentials = new VssBasicCredential(string.Empty, config.PersonalAccessToken);
            return new VssConnection(new Uri(config.OrganizationUrl), credentials);
        });

        // Register services
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IRepositoryService, RepositoryService>();
        services.AddScoped<IPullRequestService, PullRequestService>();
        services.AddScoped<IDisplayService, ConsoleDisplayService>();

        // Register scanner
        services.AddScoped<OrganizationScanner>();
    }
}