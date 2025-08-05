using System.Reflection;

namespace Jex.Tools.CLI.Core;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.WriteLine("JEX Tools CLI");
        Console.WriteLine("=============");
        Console.WriteLine();
        
        var loader = new ModuleLoader();
        
        // Load modules from the modules directory
        var modulesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "modules");
        loader.LoadModulesFromDirectory(modulesDirectory);
        
        // Also load modules from the current assembly
        loader.LoadModulesFromAssembly(Assembly.GetExecutingAssembly().Location);
        
        Console.WriteLine();
        Console.WriteLine($"Loaded {loader.Modules.Count} modules:");
        Console.WriteLine();
        
        // Display all available modules
        foreach (var availableModule in loader.Modules.OrderBy(m => m.Command))
        {
            Console.WriteLine($"  {availableModule.Command,-20} - {availableModule.Description}");
        }
        
        Console.WriteLine();
        
        // Parse command
        if (args.Length == 0)
        {
            ShowHelp(loader.Modules);
            return 0;
        }
        
        var command = args[0].ToLowerInvariant();
        
        if (command == "help" || command == "--help" || command == "-h")
        {
            ShowHelp(loader.Modules);
            return 0;
        }
        
        // Find and execute module
        var module = loader.GetModuleByCommand(command);
        
        if (module == null)
        {
            Console.WriteLine($"Unknown command: {command}");
            Console.WriteLine();
            ShowHelp(loader.Modules);
            return 1;
        }
        
        // Pass remaining arguments to the module
        var moduleArgs = args.Skip(1).ToArray();
        
        try
        {
            return await module.ExecuteAsync(moduleArgs);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing module '{module.Name}': {ex.Message}");
            return 1;
        }
    }

    private static void ShowHelp(IReadOnlyList<ICliModule> modules)
    {
        Console.WriteLine("Usage: jex-tools <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Available commands:");
        
        foreach (var module in modules.OrderBy(m => m.Command))
        {
            Console.WriteLine($"  {module.Command,-20} - {module.Description}");
        }
        
        Console.WriteLine();
        Console.WriteLine("For help on a specific command, use:");
        Console.WriteLine("  jex-tools <command> --help");
    }
}