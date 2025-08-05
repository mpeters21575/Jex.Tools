using System.Text.RegularExpressions;
using Jex.Tools.SolutionStructureAnalyzer.Models;

namespace Jex.Tools.SolutionStructureAnalyzer.Services;

public interface ISolutionStructureService
{
    Task<int> ScanAsync(string solutionPath);
    void PrintStructure();
}

public class SolutionStructureService : ISolutionStructureService
{
    private readonly SolutionStructureConfiguration _configuration;
    private readonly IDisplayService _displayService;
    private readonly HashSet<string> _processedPaths = new();
    private List<ProjectInfo> _projects = new();
    private string _solutionPath = string.Empty;

    public SolutionStructureService(SolutionStructureConfiguration configuration, IDisplayService displayService)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _displayService = displayService ?? throw new ArgumentNullException(nameof(displayService));
    }

    public async Task<int> ScanAsync(string solutionPath)
    {
        try
        {
            _solutionPath = solutionPath;

            if (!File.Exists(solutionPath))
            {
                Console.WriteLine($"Error: Solution file '{solutionPath}' not found.");
                return 1;
            }

            if (!solutionPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Error: The specified file is not a .sln file.");
                return 1;
            }

            Console.WriteLine($"Scanning solution: {solutionPath}");
            Console.WriteLine($"Solution directory: {Path.GetDirectoryName(solutionPath)}");

            await Task.Run(() => { _projects = ParseSolutionFile(solutionPath); });

            Console.WriteLine($"Found {_projects.Count} projects in solution");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error analyzing solution: {ex.Message}");
            return 1;
        }
    }

    public void PrintStructure()
    {
        if (string.IsNullOrEmpty(_solutionPath))
        {
            Console.WriteLine("Error: No solution has been scanned.");
            return;
        }

        Console.WriteLine($"Solution: {Path.GetFileName(_solutionPath)}");
        Console.WriteLine($"Location: {Path.GetDirectoryName(_solutionPath)}");
        Console.WriteLine();

        string solutionDir = Path.GetDirectoryName(_solutionPath) ?? "";

        Console.WriteLine("Solution Structure:");
        _displayService.WriteStructure("├── ");
        Console.WriteLine(Path.GetFileName(_solutionPath));

        // Build solution folder structure from .sln file
        var solutionFolders = GetSolutionFolders(_solutionPath);

        // Print the hierarchical structure
        PrintHierarchicalStructure(solutionDir, solutionFolders);

        Console.WriteLine();
    }

    private List<ProjectInfo> ParseSolutionFile(string slnPath)
    {
        var projects = new List<ProjectInfo>();
        string content = File.ReadAllText(slnPath);

        var projectRegex = new Regex(
            @"Project\(""{[^}]+}""\)\s*=\s*""([^""]+)""\s*,\s*""([^""]+)""\s*,\s*""{([^}]+)}""",
            RegexOptions.Multiline);

        foreach (Match match in projectRegex.Matches(content))
        {
            string projectName = match.Groups[1].Value;
            string projectPath = match.Groups[2].Value;
            string projectId = match.Groups[3].Value;

            if (projectPath.StartsWith("{") || !projectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                continue;

            // Convert Windows paths to Unix-style paths for macOS/Linux
            projectPath = projectPath.Replace('\\', Path.DirectorySeparatorChar);

            string fullPath = Path.Combine(Path.GetDirectoryName(slnPath) ?? "", projectPath);
            projects.Add(new ProjectInfo
            {
                Name = projectName,
                Path = fullPath,
                Id = projectId
            });
        }

        return projects;
    }

    private Dictionary<string, SolutionFolder> GetSolutionFolders(string slnPath)
    {
        var folders = new Dictionary<string, SolutionFolder>(StringComparer.OrdinalIgnoreCase);
        string content = File.ReadAllText(slnPath);

        // Parse all projects and determine which are solution folders
        var projectRegex = new Regex(
            @"Project\(""{([A-F0-9]{8}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{12})}""\)\s*=\s*""([^""]+)""\s*,\s*""([^""]+)""\s*,\s*""{([^}]+)}""",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);

        foreach (Match match in projectRegex.Matches(content))
        {
            string projectTypeGuid = match.Groups[1].Value;
            string projectName = match.Groups[2].Value;
            string projectPath = match.Groups[3].Value;
            string projectId = match.Groups[4].Value.ToUpperInvariant();

            // Check if this is a solution folder (path is same as name for folders)
            if (projectPath == projectName)
            {
                folders[projectId] = new SolutionFolder
                {
                    Name = projectName,
                    Id = projectId,
                    Projects = new List<ProjectInfo>(),
                    ChildFolders = new List<SolutionFolder>()
                };
            }
        }

        // Parse nested relationships
        var nestedRegex = new Regex(
            @"\s*{([^}]+)}\s*=\s*{([^}]+)}",
            RegexOptions.Multiline);

        var nestedSection = content.IndexOf("GlobalSection(NestedProjects)");
        if (nestedSection > -1)
        {
            var endSection = content.IndexOf("EndGlobalSection", nestedSection);
            var nestedContent = content.Substring(nestedSection, endSection - nestedSection);

            foreach (Match match in nestedRegex.Matches(nestedContent))
            {
                string childId = match.Groups[1].Value.ToUpperInvariant();
                string parentId = match.Groups[2].Value.ToUpperInvariant();

                // Check if it's a project
                var project = _projects.FirstOrDefault(p =>
                    string.Equals(p.Id, childId, StringComparison.OrdinalIgnoreCase));

                if (project != null && folders.ContainsKey(parentId))
                {
                    folders[parentId].Projects.Add(project);
                }
                else if (folders.ContainsKey(childId) && folders.ContainsKey(parentId))
                {
                    folders[parentId].ChildFolders.Add(folders[childId]);
                }
            }
        }

        return folders;
    }

    private void PrintHierarchicalStructure(string solutionDir, Dictionary<string, SolutionFolder> folders)
    {
        // Find root folders (folders not nested in other folders)
        var rootFolders = folders.Values.Where(f =>
                !folders.Values.Any(parent => parent.ChildFolders.Contains(f)))
            .OrderBy(f => f.Name);

        // Print root folders and their contents
        foreach (var folder in rootFolders)
        {
            PrintFolder(folder, "│   ", solutionDir, folders);
        }

        // Print any projects not in folders
        var projectsInFolders = folders.Values.SelectMany(f => f.Projects).ToHashSet();
        var orphanProjects = _projects.Where(p => !projectsInFolders.Contains(p)).OrderBy(p => p.Name);

        foreach (var project in orphanProjects)
        {
            PrintProject(project, "│   ", solutionDir);
        }
    }

    private void PrintFolder(SolutionFolder folder, string indent, string solutionDir,
        Dictionary<string, SolutionFolder> allFolders)
    {
        _displayService.WriteStructure($"{indent}├── ");
        _displayService.WriteSolutionFolder($"{folder.Name}/");
        _displayService.WriteLine();

        string subIndent = indent + "│   ";

        // Print child folders
        foreach (var childFolder in folder.ChildFolders.OrderBy(f => f.Name))
        {
            PrintFolder(childFolder, subIndent, solutionDir, allFolders);
        }

        // Print projects in this folder
        foreach (var project in folder.Projects.OrderBy(p => p.Name))
        {
            PrintProject(project, subIndent, solutionDir);
        }
    }

    private void PrintProject(ProjectInfo project, string indent, string solutionDir)
    {
        if (!File.Exists(project.Path))
        {
            _displayService.WriteStructure($"{indent}├── ");
            _displayService.WriteError($"{project.Name} (NOT FOUND)");
            _displayService.WriteLine();
            return;
        }

        _displayService.WriteStructure($"{indent}├── ");
        _displayService.WriteProject(project.Name);
        _displayService.WriteLine();

        string projectDir = Path.GetDirectoryName(project.Path) ?? "";

        if (!_processedPaths.Contains(projectDir))
        {
            _processedPaths.Add(projectDir);
            PrintDirectoryContents(projectDir, indent + "│   ", solutionDir, 0);
        }
    }

    private void PrintDirectoryContents(string directory, string indent, string baseDir, int depth)
    {
        if (depth > _configuration.MaxDepth)
            return;

        try
        {
            var entries = new List<string>();
            entries.AddRange(Directory.GetDirectories(directory));
            entries.AddRange(Directory.GetFiles(directory));

            // Sort with proper platform comparison
            entries.Sort((a, b) =>
                string.Compare(Path.GetFileName(a), Path.GetFileName(b),
                    StringComparison.CurrentCultureIgnoreCase));

            for (int i = 0; i < entries.Count; i++)
            {
                string entry = entries[i];
                bool isLastEntry = i == entries.Count - 1;
                string connector = isLastEntry ? "└──" : "├──";
                string nextIndent = isLastEntry ? "    " : "│   ";

                if (Directory.Exists(entry))
                {
                    string dirName = Path.GetFileName(entry);

                    if (ShouldSkipDirectory(dirName))
                        continue;

                    _displayService.WriteStructure($"{indent}{connector} ");
                    _displayService.WriteFolder($"{dirName}{Path.DirectorySeparatorChar}");
                    _displayService.WriteLine();
                    PrintDirectoryContents(entry, indent + nextIndent, baseDir, depth + 1);
                }
                else
                {
                    string fileName = Path.GetFileName(entry);

                    if (ShouldShowFile(fileName))
                    {
                        _displayService.WriteStructure($"{indent}{connector} ");
                        _displayService.WriteFile(fileName);

                        if (_configuration.ShowFileSize)
                        {
                            var fileInfo = new FileInfo(entry);
                            Console.Write($" ({FormatFileSize(fileInfo.Length)})");
                        }

                        _displayService.WriteLine();
                    }
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            _displayService.WriteStructure($"{indent}");
            _displayService.WriteError("[Access Denied]");
            _displayService.WriteLine();
        }
        catch (Exception ex)
        {
            _displayService.WriteStructure($"{indent}");
            _displayService.WriteError($"[Error: {ex.Message}]");
            _displayService.WriteLine();
        }
    }

    private bool ShouldSkipDirectory(string dirName)
    {
        if (!_configuration.ShowBuildArtifacts &&
            (dirName == "bin" || dirName == "obj"))
            return true;

        if (!_configuration.ShowHiddenFiles &&
            (dirName.StartsWith(".")))
            return true;

        return false;
    }

    private bool ShouldShowFile(string fileName)
    {
        string extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (!_configuration.ShowHiddenFiles && fileName.StartsWith("."))
            return false;

        if (_configuration.IncludeExtensions?.Length > 0)
        {
            return _configuration.IncludeExtensions.Any(ext =>
                string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase));
        }

        if (_configuration.ExcludeExtensions?.Length > 0)
        {
            return !_configuration.ExcludeExtensions.Any(ext =>
                string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase));
        }

        return IsRelevantFile(extension);
    }

    private bool IsRelevantFile(string extension)
    {
        var relevantExtensions = new HashSet<string>(
            _configuration.DefaultRelevantExtensions,
            StringComparer.OrdinalIgnoreCase);

        return relevantExtensions.Contains(extension);
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }

    private class ProjectInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
    }

    private class SolutionFolder
    {
        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public List<ProjectInfo> Projects { get; set; } = new();
        public List<SolutionFolder> ChildFolders { get; set; } = new();
    }
}