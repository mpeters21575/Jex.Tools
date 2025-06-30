using Jex.Tools.SolutionStructureAnalyzer.Configuration;
using Jex.Tools.SolutionStructureAnalyzer.Models;
using Jex.Tools.SolutionStructureAnalyzer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jex.Tools.SolutionStructureAnalyzer;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Register configuration
        services.AddSingleton<SolutionStructureConfiguration>(_ =>
        {
            var configSection = _configuration.GetSection("SolutionStructure");

            // Get values with environment variable overrides
            var solutionPath = Environment.GetEnvironmentVariable("SOLUTION_PATH")
                               ?? configSection["SolutionPath"]
                               ?? string.Empty;

            var showHiddenFiles = false; // Default value
            var showHiddenEnv = Environment.GetEnvironmentVariable("SHOW_HIDDEN_FILES");
            if (!string.IsNullOrEmpty(showHiddenEnv) && bool.TryParse(showHiddenEnv, out var parsedHidden))
            {
                showHiddenFiles = parsedHidden;
            }
            else if (bool.TryParse(configSection["ShowHiddenFiles"], out var parsedConfigHidden))
            {
                showHiddenFiles = parsedConfigHidden;
            }

            var showBuildArtifacts = false; // Default value
            var showBuildEnv = Environment.GetEnvironmentVariable("SHOW_BUILD_ARTIFACTS");
            if (!string.IsNullOrEmpty(showBuildEnv) && bool.TryParse(showBuildEnv, out var parsedBuild))
            {
                showBuildArtifacts = parsedBuild;
            }
            else if (bool.TryParse(configSection["ShowBuildArtifacts"], out var parsedConfigBuild))
            {
                showBuildArtifacts = parsedConfigBuild;
            }

            var showFileSize = false; // Default value
            var showSizeEnv = Environment.GetEnvironmentVariable("SHOW_FILE_SIZE");
            if (!string.IsNullOrEmpty(showSizeEnv) && bool.TryParse(showSizeEnv, out var parsedSize))
            {
                showFileSize = parsedSize;
            }
            else if (bool.TryParse(configSection["ShowFileSize"], out var parsedConfigSize))
            {
                showFileSize = parsedConfigSize;
            }

            var maxDepth = 10; // Default value
            var maxDepthEnv = Environment.GetEnvironmentVariable("MAX_DEPTH");
            if (!string.IsNullOrEmpty(maxDepthEnv) && int.TryParse(maxDepthEnv, out var parsedDepth))
            {
                maxDepth = parsedDepth;
            }
            else if (int.TryParse(configSection["MaxDepth"], out var parsedConfigDepth))
            {
                maxDepth = parsedConfigDepth;
            }

            // Handle include/exclude extensions
            var includeExtensions = ParseExtensions(
                Environment.GetEnvironmentVariable("INCLUDE_EXTENSIONS"),
                configSection["IncludeExtensions"]);

            var excludeExtensions = ParseExtensions(
                Environment.GetEnvironmentVariable("EXCLUDE_EXTENSIONS"),
                configSection["ExcludeExtensions"]);

            // Get default relevant extensions
            var defaultRelevantExtensions = ParseExtensions(
                                                Environment.GetEnvironmentVariable("DEFAULT_RELEVANT_EXTENSIONS"),
                                                configSection["DefaultRelevantExtensions"])
                                            ?? new[]
                                            {
                                                ".cs", ".csproj", ".sln", ".json", ".xml", ".config",
                                                ".resx", ".razor", ".cshtml", ".xaml", ".txt", ".md",
                                                ".yaml", ".yml", ".props", ".targets"
                                            };

            // Create configuration
            var config = new SolutionStructureConfiguration
            {
                SolutionPath = solutionPath,
                ShowHiddenFiles = showHiddenFiles,
                ShowBuildArtifacts = showBuildArtifacts,
                ShowFileSize = showFileSize,
                MaxDepth = maxDepth,
                IncludeExtensions = includeExtensions,
                ExcludeExtensions = excludeExtensions,
                DefaultRelevantExtensions = defaultRelevantExtensions
            };

            if (!ConfigurationValidator.Validate(config))
            {
                throw new InvalidOperationException("Invalid configuration");
            }

            return config;
        });

        // Register services
        services.AddSingleton<IDisplayService, ConsoleDisplayService>();
        services.AddScoped<ISolutionStructureService, SolutionStructureService>();

        // Register scanner
        services.AddScoped<SolutionScanner>();
    }

    private static string[]? ParseExtensions(string? envValue, string? configValue)
    {
        var value = envValue ?? configValue;
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(e =>
            {
                var trimmed = e.Trim();
                return trimmed.StartsWith(".") ? trimmed.ToLowerInvariant() : $".{trimmed}".ToLowerInvariant();
            })
            .ToArray();
    }
}