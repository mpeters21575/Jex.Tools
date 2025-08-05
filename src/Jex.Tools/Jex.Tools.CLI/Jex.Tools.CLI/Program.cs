using System.Reflection;
using Jex.Tools.CLI.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jex.Tools.CLI;

/// <summary>
/// Main entry point for the JEX Tools CLI application.
/// </summary>
public sealed class Program
{
    private const string ApplicationName = "JEX Tools CLI";
    private const string ModuleDirectoryName = "modules";
    
    public static async Task<int> Main(string[] args)
    {
        var services = ConfigureServices();
        var serviceProvider = services.BuildServiceProvider();
        
        var moduleLoader = serviceProvider.GetRequiredService<ModuleLoader>();
        
        try
        {
            DisplayHeader();
            LoadModules(moduleLoader);
            DisplayLoadedModules(moduleLoader.Modules);
            
            return await ProcessCommandAsync(args, moduleLoader);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return 1;
        }
    }
    
    private static IServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();
        
        // Use null logger to avoid dependency issues
        services.AddSingleton<ILogger<ModuleLoader>>(NullLogger<ModuleLoader>.Instance);
        services.AddSingleton<ModuleLoader>();
        
        return services;
    }
    
    private static void DisplayHeader()
    {
        Console.WriteLine(ApplicationName);
        Console.WriteLine(new string('=', ApplicationName.Length));
        Console.WriteLine();
    }
    
    private static void LoadModules(ModuleLoader loader)
    {
        // Load modules from the current assembly
        var currentAssembly = Assembly.GetExecutingAssembly();
        Console.WriteLine($"Loading modules from current assembly: {currentAssembly.GetName().Name}");
        loader.LoadModulesFromAssembly(currentAssembly);
        
        // Load from modules directory
        var modulesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ModuleDirectoryName);
        if (Directory.Exists(modulesDirectory))
        {
            Console.WriteLine($"Loading modules from directory: {modulesDirectory}");
            loader.LoadModulesFromDirectory(modulesDirectory);
        }
        else
        {
            Console.WriteLine($"Modules directory not found: {modulesDirectory}");
        }
        
        Console.WriteLine();
    }
    
    private static void DisplayLoadedModules(IReadOnlyList<ICliModule> modules)
    {
        Console.WriteLine($"Loaded {modules.Count} modules:");
        Console.WriteLine();
        
        if (modules.Count == 0)
        {
            Console.WriteLine("  No modules found!");
        }
        else
        {
            foreach (var module in modules.OrderBy(m => m.Command))
            {
                Console.WriteLine($"  {module.Command,-20} - {module.Description}");
            }
        }
        
        Console.WriteLine();
    }
    
    private static async Task<int> ProcessCommandAsync(string[] args, ModuleLoader loader)
    {
        if (args.Length == 0 || IsHelpCommand(args[0]))
        {
            ShowHelp(loader.Modules);
            return 0;
        }
        
        var command = args[0];
        var module = loader.GetModuleByCommand(command);
        
        if (module == null)
        {
            Console.WriteLine($"Unknown command: {command}");
            Console.WriteLine();
            ShowHelp(loader.Modules);
            return 1;
        }
        
        var moduleArgs = args.Skip(1).ToArray();
        return await module.ExecuteAsync(moduleArgs);
    }
    
    private static bool IsHelpCommand(string command) =>
        command.Equals("help", StringComparison.OrdinalIgnoreCase) ||
        command.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
        command.Equals("-h", StringComparison.OrdinalIgnoreCase);
    
    private static void ShowHelp(IReadOnlyList<ICliModule> modules)
    {
        Console.WriteLine("Usage: jex-tools <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Available commands:");
        
        if (modules.Count == 0)
        {
            Console.WriteLine("  No modules available");
        }
        else
        {
            foreach (var module in modules.OrderBy(m => m.Command))
            {
                Console.WriteLine($"  {module.Command,-20} - {module.Description}");
            }
        }
        
        Console.WriteLine();
        Console.WriteLine("For help on a specific command, use:");
        Console.WriteLine("  jex-tools <command> --help");
    }
}