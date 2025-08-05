using Jex.Tools.CLI.Core;

namespace Jex.Tools.CLI.Modules;

/// <summary>
/// Provides system and environment information.
/// </summary>
public sealed class SystemInfoModule : ICliModule
{
    public string Name => "System Information";
    public string Description => "Shows system and environment information";
    public string Command => "sysinfo";
    public string Version => "1.0.0";
    
    public Task<int> ExecuteAsync(string[] args)
    {
        if (args.Any(arg => IsHelpArgument(arg)))
        {
            ShowHelp();
            return Task.FromResult(0);
        }
        
        DisplaySystemInfo();
        
        if (args.Contains("--env", StringComparer.OrdinalIgnoreCase))
        {
            DisplayEnvironmentVariables();
        }
        
        return Task.FromResult(0);
    }
    
    public void ShowHelp()
    {
        Console.WriteLine($"{Name} v{Version}");
        Console.WriteLine();
        Console.WriteLine("Usage: jex-tools sysinfo [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --env            Show environment variables");
        Console.WriteLine("  -h, --help       Show this help message");
    }
    
    private static void DisplaySystemInfo()
    {
        Console.WriteLine("System Information");
        Console.WriteLine("==================");
        Console.WriteLine();
        
        Console.WriteLine($"Operating System: {Environment.OSVersion}");
        Console.WriteLine($"Framework: {Environment.Version}");
        Console.WriteLine($"Machine Name: {Environment.MachineName}");
        Console.WriteLine($"User Name: {Environment.UserName}");
        Console.WriteLine($"Processor Count: {Environment.ProcessorCount}");
        Console.WriteLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
        Console.WriteLine($"64-bit Process: {Environment.Is64BitProcess}");
        Console.WriteLine($"Current Directory: {Environment.CurrentDirectory}");
        Console.WriteLine($"System Directory: {Environment.SystemDirectory}");
        Console.WriteLine();
    }
    
    private static void DisplayEnvironmentVariables()
    {
        Console.WriteLine("Environment Variables:");
        Console.WriteLine("======================");
        
        var variables = Environment.GetEnvironmentVariables();
        var sortedKeys = variables.Keys.Cast<string>().OrderBy(k => k);
        
        foreach (var key in sortedKeys)
        {
            var value = variables[key];
            Console.WriteLine($"{key}={value}");
        }
    }
    
    private static bool IsHelpArgument(string arg) =>
        arg.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
        arg.Equals("-h", StringComparison.OrdinalIgnoreCase);
}