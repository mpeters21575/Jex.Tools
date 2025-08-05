using Jex.Tools.CLI.Core;
using Jex.Tools.SolutionStructureAnalyzer.Models;
using Jex.Tools.SolutionStructureAnalyzer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jex.Tools.SolutionStructureAnalyzer;

/// <summary>
/// CLI module for printing out the complete folder structure of a project.
/// </summary>
public sealed class SolutionExplorerStructureModule : ICliModule
{
    public string Name => "Solution Explorer Structure Scanner";
    public string Description => "Scans a SLN file, and prints out the folder structure in a visual way";
    public string Command => "f-structure";
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
            var config = GetInitialConfiguration(args);

            if (string.IsNullOrEmpty(config.SolutionPath))
            {
                Console.WriteLine("Error: No solution file specified.");
                Console.WriteLine("Use --help for usage information.");
                return 1;
            }

            // Build configuration
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Setup services
            var services = new ServiceCollection();
            var startup = new Startup(configuration);
            startup.ConfigureServices(services);

            // Override configuration with command line args
            services.AddSingleton(config);

            await using var serviceProvider = services.BuildServiceProvider();

            var scanner = serviceProvider.GetRequiredService<SolutionScanner>();
            return await scanner.ScanAsync(config.SolutionPath);
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
        Console.WriteLine("Usage: jex-tools f-structure <sln-file> [options]");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  <sln-file>           Path to the .sln file to analyze");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --show-hidden        Show hidden files and directories");
        Console.WriteLine("  --show-build         Show build artifacts (bin/obj)");
        Console.WriteLine("  --show-size          Show file sizes");
        Console.WriteLine("  --max-depth <n>      Maximum depth to traverse (default: 10)");
        Console.WriteLine("  --include <ext>      Include only files with these extensions (comma-separated)");
        Console.WriteLine("  --exclude <ext>      Exclude files with these extensions (comma-separated)");
        Console.WriteLine("  --file-types <ext>   Default file extensions to show (comma-separated)");
        Console.WriteLine("  -h, --help           Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  jex-tools f-structure MyApp.sln");
        Console.WriteLine("  jex-tools f-structure MyApp.sln --show-size");
        Console.WriteLine("  jex-tools f-structure MyApp.sln --include .cs,.json");
        Console.WriteLine("  jex-tools f-structure MyApp.sln --exclude .dll,.exe");
        Console.WriteLine("  jex-tools f-structure MyApp.sln --file-types .cs,.json,.xml");
    }

    private static SolutionStructureConfiguration GetInitialConfiguration(string[] args)
    {
        var config = new SolutionStructureConfiguration
        {
            DefaultRelevantExtensions = new[]
            {
                ".cs", ".csproj", ".sln", ".json", ".xml", ".config",
                ".resx", ".razor", ".cshtml", ".xaml", ".txt", ".md",
                ".yaml", ".yml", ".props", ".targets"
            }
        };

        // First non-option argument is the solution path
        for (int i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("-"))
            {
                config.SolutionPath = Path.GetFullPath(args[i]);
                break;
            }
        }

        ParseCommandLineArgs(args, config);
        return config;
    }

    private static void ParseCommandLineArgs(string[] args, SolutionStructureConfiguration config)
    {
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i].ToLowerInvariant();

            switch (arg)
            {
                case "--show-hidden":
                    config.ShowHiddenFiles = true;
                    break;

                case "--show-build":
                    config.ShowBuildArtifacts = true;
                    break;

                case "--show-size":
                    config.ShowFileSize = true;
                    break;

                case "--max-depth" when i + 1 < args.Length:
                    if (int.TryParse(args[++i], out int depth))
                    {
                        config.MaxDepth = depth;
                    }

                    break;

                case "--include" when i + 1 < args.Length:
                    config.IncludeExtensions = args[++i]
                        .Split(',')
                        .Select(e =>
                        {
                            var trimmed = e.Trim();
                            return trimmed.StartsWith(".")
                                ? trimmed.ToLowerInvariant()
                                : $".{trimmed}".ToLowerInvariant();
                        })
                        .ToArray();
                    break;

                case "--exclude" when i + 1 < args.Length:
                    config.ExcludeExtensions = args[++i]
                        .Split(',')
                        .Select(e =>
                        {
                            var trimmed = e.Trim();
                            return trimmed.StartsWith(".")
                                ? trimmed.ToLowerInvariant()
                                : $".{trimmed}".ToLowerInvariant();
                        })
                        .ToArray();
                    break;

                case "--file-types" when i + 1 < args.Length:
                    config.DefaultRelevantExtensions = args[++i]
                        .Split(',')
                        .Select(e =>
                        {
                            var trimmed = e.Trim();
                            return trimmed.StartsWith(".")
                                ? trimmed.ToLowerInvariant()
                                : $".{trimmed}".ToLowerInvariant();
                        })
                        .ToArray();
                    break;
            }
        }
    }

    private static bool IsHelpArgument(string arg) =>
        arg.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
        arg.Equals("-h", StringComparison.OrdinalIgnoreCase);
}