using Microsoft.Extensions.Configuration;

namespace Jex.Tools.OpenPullRequests.Configuration;

/// <summary>
/// Handles loading configuration from various sources.
/// </summary>
public static class ConfigurationLoader
{
    /// <summary>
    /// Loads configuration from appsettings.json and environment variables.
    /// </summary>
    /// <returns>Populated configuration object.</returns>
    public static AzureDevOpsConfiguration Load()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        
        // First, try to bind from configuration
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
        else if (configSection["ShowOnlyMyPullRequests"] != null && bool.TryParse(configSection["ShowOnlyMyPullRequests"], out var parsedConfig))
        {
            showOnlyMyPRs = parsedConfig;
        }
        
        // Create configuration with required properties
        return new AzureDevOpsConfiguration
        {
            OrganizationUrl = organizationUrl,
            PersonalAccessToken = personalAccessToken,
            ShowOnlyMyPullRequests = showOnlyMyPRs
        };
    }
}