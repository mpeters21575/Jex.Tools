using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jex.Tools.CLI.Core;

/// <summary>
/// Discovers and loads CLI modules from assemblies.
/// </summary>
public sealed class ModuleLoader(ILogger<ModuleLoader>? logger = null)
{
    private readonly List<ICliModule> _modules = [];
    private readonly ILogger<ModuleLoader> _logger = logger ?? NullLogger<ModuleLoader>.Instance;
    
    /// <summary>
    /// Gets all loaded modules.
    /// </summary>
    public IReadOnlyList<ICliModule> Modules => _modules.AsReadOnly();

    /// <summary>
    /// Loads modules from a directory containing assemblies.
    /// </summary>
    /// <param name="directory">Directory path to scan for modules.</param>
    public void LoadModulesFromDirectory(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        
        if (!Directory.Exists(directory))
        {
            _logger.LogWarning("Module directory not found: {Directory}", directory);
            return;
        }
        
        var dllFiles = Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly);
        _logger.LogInformation("Found {Count} DLL files in {Directory}", dllFiles.Length, directory);
        
        foreach (var dllFile in dllFiles)
        {
            try
            {
                LoadModulesFromAssembly(dllFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load modules from {File}", Path.GetFileName(dllFile));
            }
        }
    }
    
    /// <summary>
    /// Loads modules from a specific assembly.
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly file.</param>
    public void LoadModulesFromAssembly(string assemblyPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);
        
        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            LoadModulesFromAssembly(assembly);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load assembly {Path}", assemblyPath);
            throw;
        }
    }
    
    /// <summary>
    /// Loads modules from an already loaded assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan for modules.</param>
    public void LoadModulesFromAssembly(Assembly assembly)
    {
        try
        {
            var moduleTypes = assembly.GetTypes()
                .Where(IsValidModuleType)
                .ToList();
            
            _logger.LogInformation("Found {Count} module types in {Assembly}", 
                moduleTypes.Count, assembly.GetName().Name);
            
            foreach (var moduleType in moduleTypes)
            {
                try
                {
                    if (Activator.CreateInstance(moduleType) is ICliModule module)
                    {
                        _modules.Add(module);
                        _logger.LogInformation("Loaded module: {Name} v{Version} [{Command}]", 
                            module.Name, module.Version, module.Command);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create instance of {Type}", moduleType.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan assembly {Assembly}", assembly.GetName().Name);
            throw;
        }
    }
    
    /// <summary>
    /// Gets a module by its command name.
    /// </summary>
    /// <param name="command">Command name to search for.</param>
    /// <returns>Module if found, null otherwise.</returns>
    public ICliModule? GetModuleByCommand(string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        
        return _modules.FirstOrDefault(m => 
            string.Equals(m.Command, command, StringComparison.OrdinalIgnoreCase));
    }
    
    private static bool IsValidModuleType(Type type) =>
        typeof(ICliModule).IsAssignableFrom(type) && 
        !type.IsInterface && 
        !type.IsAbstract &&
        type.GetConstructor(Type.EmptyTypes) != null;
}