namespace Jex.Tools.CLI.Core;

/// <summary>
/// Base interface for all CLI modules
/// </summary>
public interface ICliModule
{
    /// <summary>
    /// Unique name of the module
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Description of what the module does
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Command to invoke this module (e.g., "pr-scan")
    /// </summary>
    string Command { get; }
    
    /// <summary>
    /// Version of the module
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// Executes the module
    /// </summary>
    Task<int> ExecuteAsync(string[] args);
    
    /// <summary>
    /// Shows help for this module
    /// </summary>
    void ShowHelp();
}