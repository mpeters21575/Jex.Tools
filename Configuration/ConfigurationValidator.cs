using Jex.Tools.SolutionStructureAnalyzer.Models;

namespace Jex.Tools.SolutionStructureAnalyzer.Configuration;

public static class ConfigurationValidator
    {
        public static bool Validate(SolutionStructureConfiguration config)
        {
            if (config == null)
            {
                Console.WriteLine("Error: Configuration is null.");
                return false;
            }
            
            // Don't validate solution path if it's empty - it might be set later via command line
            if (!string.IsNullOrEmpty(config.SolutionPath))
            {
                if (!config.SolutionPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Error: '{config.SolutionPath}' is not a .sln file.");
                    return false;
                }
                
                if (!File.Exists(config.SolutionPath))
                {
                    Console.WriteLine($"Error: Solution file '{config.SolutionPath}' not found.");
                    return false;
                }
            }
            
            if (config.MaxDepth < 1)
            {
                Console.WriteLine("Error: Max depth must be at least 1.");
                return false;
            }
            
            if (config.MaxDepth > 50)
            {
                Console.WriteLine("Warning: Max depth is very high. This may cause performance issues.");
            }
            
            // Validate extensions format
            if (config.IncludeExtensions != null)
            {
                foreach (var ext in config.IncludeExtensions)
                {
                    if (!ext.StartsWith(".") || ext.Length < 2)
                    {
                        Console.WriteLine($"Error: Invalid extension format '{ext}'. Extensions must start with '.' and have at least one character after.");
                        return false;
                    }
                }
            }
            
            if (config.ExcludeExtensions != null)
            {
                foreach (var ext in config.ExcludeExtensions)
                {
                    if (!ext.StartsWith(".") || ext.Length < 2)
                    {
                        Console.WriteLine($"Error: Invalid extension format '{ext}'. Extensions must start with '.' and have at least one character after.");
                        return false;
                    }
                }
            }
            
            return true;
        }
    }