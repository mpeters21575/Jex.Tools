using Jex.Tools.CLI.Core;
using Jex.Tools.SolutionCodeExtractor.Models;
using Jex.Tools.SolutionCodeExtractor.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jex.Tools.SolutionCodeExtractor;

/// <summary>
/// CLI module for extracting all code files from a solution into a single text file.
/// </summary>
public sealed class SolutionCodeExtractorModule : ICliModule
{
    public string Name => "Solution Code Extractor";
    public string Description => "Extracts all code, JSON, and XML files from a solution into a single text file";
    public string Command => "extract-code";
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

            // Set default output paths
            if (config.SeparateFilePerProject)
            {
                if (string.IsNullOrEmpty(config.OutputDirectory))
                {
                    var solutionName = Path.GetFileNameWithoutExtension(config.SolutionPath);
                    config.OutputDirectory = Path.Combine(
                        Path.GetDirectoryName(config.SolutionPath) ?? Environment.CurrentDirectory,
                        $"{solutionName}_extracted_layers");
                }
            }
            else if (string.IsNullOrEmpty(config.OutputPath))
            {
                // Default output file name based on solution name
                var solutionName = Path.GetFileNameWithoutExtension(config.SolutionPath);
                config.OutputPath = Path.Combine(
                    Path.GetDirectoryName(config.SolutionPath) ?? Environment.CurrentDirectory,
                    $"{solutionName}_extracted_code.txt");
            }

            // Build configuration
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Setup services
            var services = new ServiceCollection();
            services.AddSingleton(config);
            services.AddScoped<ICodeExtractionService, CodeExtractionService>();

            await using var serviceProvider = services.BuildServiceProvider();

            var extractor = serviceProvider.GetRequiredService<ICodeExtractionService>();
            
            if (config.SeparateFilePerProject)
            {
                return await extractor.ExtractSeparateAsync(config.SolutionPath, config.OutputDirectory!);
            }
            else
            {
                return await extractor.ExtractAsync(config.SolutionPath, config.OutputPath!);
            }
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
        Console.WriteLine("Usage: jex-tools extract-code <sln-file> [options]");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  <sln-file>           Path to the .sln file to analyze");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -o, --output <file>  Output file path (default: <solution-name>_extracted_code.txt)");
        Console.WriteLine("  -d, --output-dir <dir> Output directory for separate project files");
        Console.WriteLine("  --separate-projects  Generate separate file per project/layer");
        Console.WriteLine("  --max-tokens <n>     Maximum tokens per file for Claude context (default: 190000)");
        Console.WriteLine("  --disable-split      Disable automatic file splitting based on token limits");
        Console.WriteLine("  --include <ext>      Include only files with these extensions (comma-separated)");
        Console.WriteLine("  --exclude <ext>      Exclude files with these extensions (comma-separated)");
        Console.WriteLine("  --max-size <mb>      Maximum file size to include in MB (default: 1)");
        Console.WriteLine("  --encoding <enc>     Text encoding (default: utf-8)");
        Console.WriteLine("  -h, --help           Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  jex-tools extract-code MyApp.sln");
        Console.WriteLine("  jex-tools extract-code MyApp.sln -o combined_code.txt");
        Console.WriteLine("  jex-tools extract-code MyApp.sln --separate-projects -d extracted_layers");
        Console.WriteLine("  jex-tools extract-code MyApp.sln --max-tokens 150000");
        Console.WriteLine("  jex-tools extract-code MyApp.sln --disable-split");
        Console.WriteLine("  jex-tools extract-code MyApp.sln --include .cs,.json");
        Console.WriteLine("  jex-tools extract-code MyApp.sln --exclude .dll,.exe --max-size 2");
    }

    private static CodeExtractionConfiguration GetInitialConfiguration(string[] args)
    {
        var config = new CodeExtractionConfiguration
        {
            DefaultExtensions = new[]
            {
                ".cs", ".csproj", ".sln", ".json", ".xml", ".config",
                ".resx", ".razor", ".cshtml", ".xaml", ".txt", ".md",
                ".yaml", ".yml", ".props", ".targets", ".js", ".ts",
                ".css", ".scss", ".html", ".htm"
            },
            MaxFileSizeMB = 1,
            Encoding = "utf-8"
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

    private static void ParseCommandLineArgs(string[] args, CodeExtractionConfiguration config)
    {
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i].ToLowerInvariant();

            switch (arg)
            {
                case "-o":
                case "--output" when i + 1 < args.Length:
                    config.OutputPath = Path.GetFullPath(args[++i]);
                    break;

                case "-d":
                case "--output-dir" when i + 1 < args.Length:
                    config.OutputDirectory = Path.GetFullPath(args[++i]);
                    break;

                case "--separate-projects":
                    config.SeparateFilePerProject = true;
                    break;

                case "--max-tokens" when i + 1 < args.Length:
                    if (int.TryParse(args[++i], out int tokens))
                    {
                        config.MaxTokensPerFile = tokens;
                    }
                    break;

                case "--disable-split":
                    config.EnableAutoSplit = false;
                    break;

                case "--max-size" when i + 1 < args.Length:
                    if (double.TryParse(args[++i], out double size))
                    {
                        config.MaxFileSizeMB = size;
                    }
                    break;

                case "--encoding" when i + 1 < args.Length:
                    config.Encoding = args[++i];
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
            }
        }
    }

    private static bool IsHelpArgument(string arg) =>
        arg.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
        arg.Equals("-h", StringComparison.OrdinalIgnoreCase);
}