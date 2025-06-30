using System.Text;
using System.Text.RegularExpressions;
using Jex.Tools.SolutionCodeExtractor.Models;

namespace Jex.Tools.SolutionCodeExtractor.Services;

public interface ICodeExtractionService
{
    Task<int> ExtractAsync(string solutionPath, string outputPath);
    Task<int> ExtractSeparateAsync(string solutionPath, string outputDirectory);
}

public class CodeExtractionService : ICodeExtractionService
{
    private readonly CodeExtractionConfiguration _configuration;

    public CodeExtractionService(CodeExtractionConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<int> ExtractAsync(string solutionPath, string outputPath)
    {
        try
        {
            if (!File.Exists(solutionPath))
                return ExitWithError($"Solution file '{solutionPath}' not found.");
            if (!solutionPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                return ExitWithError("The specified file is not a .sln file.");

            Console.WriteLine($"Extracting code from solution: {solutionPath}");
            Console.WriteLine($"Output file: {outputPath}");
            Console.WriteLine($"Auto-split enabled: {_configuration.EnableAutoSplit}");
            Console.WriteLine($"Max tokens per file: {_configuration.MaxTokensPerFile:N0}");
            Console.WriteLine();

            var projects = ParseSolutionFile(solutionPath);
            Console.WriteLine($"Found {projects.Count} projects in solution");

            // STEP 1: Collect ALL file contents first
            var allFileContents = await CollectAllFileContentsAsync(solutionPath, projects);
            Console.WriteLine($"Successfully collected {allFileContents.Count} files");

            if (!_configuration.EnableAutoSplit)
            {
                // Simple single file output
                return await WriteSingleFileAsync(allFileContents, outputPath);
            }

            // STEP 2: Split collected contents by token limit
            var fileParts = SplitContentsByTokenLimit(allFileContents);
            Console.WriteLine($"Split into {fileParts.Count} parts based on {_configuration.MaxTokensPerFile:N0} token limit");

            // STEP 3: Write each part to disk
            await WritePartsToFilesAsync(fileParts, outputPath);

            Console.WriteLine();
            Console.WriteLine("Extraction complete!");
            Console.WriteLine($"Total files processed: {allFileContents.Count}");
            if (fileParts.Count > 1)
            {
                Console.WriteLine($"Output split into {fileParts.Count} files due to token limits");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during extraction: {ex.Message}");
            return 1;
        }
    }

    public async Task<int> ExtractSeparateAsync(string solutionPath, string outputDirectory)
    {
        try
        {
            if (!File.Exists(solutionPath))
                return ExitWithError($"Solution file '{solutionPath}' not found.");
            if (!solutionPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                return ExitWithError("The specified file is not a .sln file.");

            Console.WriteLine($"Extracting code from solution: {solutionPath}");
            Console.WriteLine($"Output directory: {outputDirectory}");
            Console.WriteLine($"Mode: Separate file per project/layer");
            Console.WriteLine($"Auto-split enabled: {_configuration.EnableAutoSplit}");
            Console.WriteLine($"Max tokens per file: {_configuration.MaxTokensPerFile:N0}");
            Console.WriteLine();

            Directory.CreateDirectory(outputDirectory);

            var projects = ParseSolutionFile(solutionPath);
            Console.WriteLine($"Found {projects.Count} projects in solution");

            // Extract solution-level files first
            await ExtractSolutionLevelFilesAsync(solutionPath, outputDirectory);

            // Extract each project separately
            int totalFilesProcessed = 0;
            foreach (var project in projects)
            {
                var filesProcessed = await ExtractProjectAsync(project, outputDirectory);
                totalFilesProcessed += filesProcessed;
            }

            Console.WriteLine();
            Console.WriteLine("Extraction complete!");
            Console.WriteLine($"Total projects processed: {projects.Count}");
            Console.WriteLine($"Total files processed: {totalFilesProcessed}");
            Console.WriteLine($"Output directory: {outputDirectory}");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during extraction: {ex.Message}");
            return 1;
        }
    }

    private async Task<List<FileContent>> CollectAllFileContentsAsync(string solutionPath, List<ProjectInfo> projects)
    {
        var fileContents = new List<FileContent>();
        
        // Add solution file
        try
        {
            var content = await File.ReadAllTextAsync(solutionPath);
            fileContents.Add(new FileContent { Path = solutionPath, Content = content });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not read solution file: {ex.Message}");
        }

        // Add all project files
        foreach (var project in projects)
        {
            if (!File.Exists(project.Path)) continue;

            // Add project file
            try
            {
                var content = await File.ReadAllTextAsync(project.Path);
                fileContents.Add(new FileContent { Path = project.Path, Content = content });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not read project file {project.Path}: {ex.Message}");
                continue;
            }

            // Add all files in project directory
            var projectDir = Path.GetDirectoryName(project.Path) ?? "";
            try
            {
                var projectFiles = Directory.GetFiles(projectDir, "*", SearchOption.AllDirectories);
                foreach (var file in projectFiles)
                {
                    if (ShouldIncludeFile(file) && !string.Equals(file, project.Path, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var content = await File.ReadAllTextAsync(file);
                            fileContents.Add(new FileContent { Path = file, Content = content });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Warning: Could not read file {file}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not scan directory {projectDir}: {ex.Message}");
            }
        }

        return fileContents.OrderBy(f => f.Path).ToList();
    }

    private List<List<FileContent>> SplitContentsByTokenLimit(List<FileContent> allFiles)
    {
        var parts = new List<List<FileContent>>();
        var currentPart = new List<FileContent>();
        int currentTokens = 0;

        Console.WriteLine($"Splitting {allFiles.Count} files with max {_configuration.MaxTokensPerFile:N0} tokens per part:");

        foreach (var file in allFiles)
        {
            int fileTokens = EstimateTokenCount(file.Content);
            
            // Skip files that are too large
            if (fileTokens > _configuration.MaxTokensPerFile)
            {
                Console.WriteLine($"  Skipping oversized file: {Path.GetFileName(file.Path)} ({fileTokens:N0} tokens)");
                continue;
            }

            // Start new part if adding this file would exceed limit
            if (currentTokens + fileTokens > _configuration.MaxTokensPerFile && currentPart.Count > 0)
            {
                Console.WriteLine($"  Part {parts.Count + 1}: {currentPart.Count} files, {currentTokens:N0} tokens");
                parts.Add(currentPart);
                currentPart = new List<FileContent>();
                currentTokens = 0;
            }

            currentPart.Add(file);
            currentTokens += fileTokens;
        }

        // Add final part
        if (currentPart.Count > 0)
        {
            Console.WriteLine($"  Part {parts.Count + 1}: {currentPart.Count} files, {currentTokens:N0} tokens");
            parts.Add(currentPart);
        }

        return parts;
    }

    private async Task WritePartsToFilesAsync(List<List<FileContent>> parts, string basePath)
    {
        var baseName = Path.GetFileNameWithoutExtension(basePath);
        var extension = Path.GetExtension(basePath);
        var directory = Path.GetDirectoryName(basePath) ?? ".";

        Directory.CreateDirectory(directory);
        var encoding = GetEncoding(_configuration.Encoding);

        for (int i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            string filePath;
            
            if (parts.Count == 1)
            {
                filePath = basePath;
            }
            else
            {
                var partName = $"{baseName}_part{i + 1:D2}{extension}";
                filePath = Path.Combine(directory, partName);
            }

            Console.WriteLine($"Writing part {i + 1}: {part.Count} files to {Path.GetFileName(filePath)}");

            using var writer = new StreamWriter(filePath, false, encoding);
            await WriteFileHeader(writer, i + 1, parts.Count, part.Count);

            foreach (var fileContent in part)
            {
                await WriteFileContentToOutputAsync(writer, fileContent);
            }
        }
    }

    private async Task<int> WriteSingleFileAsync(List<FileContent> files, string outputPath)
    {
        var encoding = GetEncoding(_configuration.Encoding);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
        
        using var writer = new StreamWriter(outputPath, false, encoding);
        await WriteFileHeader(writer, 1, 1, files.Count);

        foreach (var fileContent in files)
        {
            await WriteFileContentToOutputAsync(writer, fileContent);
        }

        Console.WriteLine();
        Console.WriteLine("Extraction complete!");
        Console.WriteLine($"Total files processed: {files.Count}");
        Console.WriteLine($"Output file: {outputPath}");

        return 0;
    }

    private async Task WriteFileHeader(StreamWriter writer, int partNumber, int totalParts, int fileCount)
    {
        if (totalParts > 1)
        {
            await writer.WriteLineAsync($"# Code Extraction from Solution - Part {partNumber} of {totalParts}");
        }
        else
        {
            await writer.WriteLineAsync("# Code Extraction from Solution");
        }
        
        await writer.WriteLineAsync($"# Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        await writer.WriteLineAsync($"# Files in this part: {fileCount}");
        
        if (_configuration.EnableAutoSplit)
        {
            await writer.WriteLineAsync($"# Max tokens per file: {_configuration.MaxTokensPerFile:N0}");
        }
        
        await writer.WriteLineAsync();
        await writer.WriteLineAsync(new string('=', 80));
        await writer.WriteLineAsync();
    }

    private List<ProjectInfo> ParseSolutionFile(string slnPath)
    {
        var projects = new List<ProjectInfo>();
        var content = File.ReadAllText(slnPath);

        var regex = new Regex(@"Project\(""\{[^}]+\}""\)\s*=\s*""(?<name>[^""]+)""\s*,\s*""(?<path>[^""]+)""\s*,\s*""(?<id>\{[^}]+\})""", RegexOptions.Multiline);

        foreach (Match match in regex.Matches(content))
        {
            string name = match.Groups["name"].Value;
            string relPath = match.Groups["path"].Value.Replace('\\', Path.DirectorySeparatorChar);
            string id = match.Groups["id"].Value;

            if (!relPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                continue;

            var fullPath = Path.Combine(Path.GetDirectoryName(slnPath) ?? string.Empty, relPath);
            projects.Add(new ProjectInfo { Name = name, Path = fullPath, Id = id });
        }

        return projects;
    }

    private bool ShouldIncludeFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var directory = Path.GetDirectoryName(filePath) ?? "";

        // Skip hidden files (except common config files)
        if (fileName.StartsWith(".") && fileName != ".gitignore" && fileName != ".editorconfig")
            return false;

        // Skip build artifacts directories
        var dirName = Path.GetFileName(directory);
        if (dirName == "bin" || dirName == "obj" || dirName == "node_modules" || dirName == ".git")
            return false;

        // Check file size
        try
        {
            var fileInfo = new FileInfo(filePath);
            var maxSizeBytes = (long)(_configuration.MaxFileSizeMB * 1024 * 1024);
            if (fileInfo.Length > maxSizeBytes)
            {
                Console.WriteLine($"Skipping large file: {filePath} ({FormatFileSize(fileInfo.Length)})");
                return false;
            }
        }
        catch
        {
            return false;
        }

        // Check extensions
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

        // Use default relevant extensions
        var relevantExtensions = new HashSet<string>(
            _configuration.DefaultExtensions,
            StringComparer.OrdinalIgnoreCase);

        return relevantExtensions.Contains(extension);
    }

    private async Task ExtractSolutionLevelFilesAsync(string solutionPath, string outputDirectory)
    {
        var solutionDir = Path.GetDirectoryName(solutionPath) ?? "";
        var solutionName = Path.GetFileNameWithoutExtension(solutionPath);
        var outputPath = Path.Combine(outputDirectory, $"00_Solution_{solutionName}.txt");

        var encoding = GetEncoding(_configuration.Encoding);
        using var writer = new StreamWriter(outputPath, false, encoding);

        await writer.WriteLineAsync($"# Solution Level Files: {solutionName}");
        await writer.WriteLineAsync($"# Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync(new string('=', 80));
        await writer.WriteLineAsync();

        // Add solution file
        await WriteFileToOutputAsync(writer, solutionPath);

        // Add solution-level files (in solution directory but not in project directories)
        try
        {
            var solutionLevelFiles = Directory.GetFiles(solutionDir, "*", SearchOption.TopDirectoryOnly)
                .Where(f => ShouldIncludeFile(f) && !string.Equals(f, solutionPath, StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f);

            foreach (var file in solutionLevelFiles)
            {
                await WriteFileToOutputAsync(writer, file);
            }
        }
        catch (Exception ex)
        {
            await writer.WriteLineAsync($"# Error reading solution directory: {ex.Message}");
        }

        Console.WriteLine($"Extracted solution level files to: {Path.GetFileName(outputPath)}");
    }

    private async Task<int> ExtractProjectAsync(ProjectInfo project, string outputDirectory)
    {
        Console.WriteLine($"Processing project: {project.Name}");

        if (!File.Exists(project.Path))
        {
            Console.WriteLine($"  Warning: Project file not found: {project.Path}");
            return 0;
        }

        // Collect all project files
        var projectFiles = await CollectProjectFilesAsync(project);
        Console.WriteLine($"  Collected {projectFiles.Count} files from project");

        var safeProjectName = GetSafeFileName(project.Name);

        if (!_configuration.EnableAutoSplit)
        {
            // Single file per project
            var outputPath = Path.Combine(outputDirectory, $"{safeProjectName}.txt");
            return await WriteProjectFilesAsync(project, projectFiles, outputPath, 1, 1);
        }

        // Token-based splitting for project
        var fileParts = SplitContentsByTokenLimit(projectFiles);
        int totalFilesProcessed = 0;

        for (int i = 0; i < fileParts.Count; i++)
        {
            var part = fileParts[i];
            string outputPath;

            if (fileParts.Count == 1)
            {
                outputPath = Path.Combine(outputDirectory, $"{safeProjectName}.txt");
            }
            else
            {
                outputPath = Path.Combine(outputDirectory, $"{safeProjectName}_part{i + 1:D2}.txt");
            }

            var filesProcessed = await WriteProjectFilesAsync(project, part, outputPath, i + 1, fileParts.Count);
            totalFilesProcessed += filesProcessed;
        }

        if (fileParts.Count > 1)
        {
            Console.WriteLine($"  Project split into {fileParts.Count} files due to token limits");
        }

        return totalFilesProcessed;
    }

    private async Task<List<FileContent>> CollectProjectFilesAsync(ProjectInfo project)
    {
        var files = new List<FileContent>();
        
        // Add project file
        try
        {
            var content = await File.ReadAllTextAsync(project.Path);
            files.Add(new FileContent { Path = project.Path, Content = content });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Warning: Could not read project file: {ex.Message}");
        }

        var projectDir = Path.GetDirectoryName(project.Path) ?? "";

        try
        {
            var allFiles = Directory.GetFiles(projectDir, "*", SearchOption.AllDirectories);
            foreach (var file in allFiles)
            {
                if (ShouldIncludeFile(file) && !string.Equals(file, project.Path, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(file);
                        files.Add(new FileContent { Path = file, Content = content });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Warning: Could not read file {file}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Warning: Error scanning project directory: {ex.Message}");
        }

        return files.OrderBy(f => f.Path).ToList();
    }

    private async Task<int> WriteProjectFilesAsync(ProjectInfo project, List<FileContent> files, string outputPath, int partNumber, int totalParts)
    {
        var encoding = GetEncoding(_configuration.Encoding);
        using var writer = new StreamWriter(outputPath, false, encoding);

        if (totalParts > 1)
        {
            await writer.WriteLineAsync($"# Project: {project.Name} - Part {partNumber} of {totalParts}");
        }
        else
        {
            await writer.WriteLineAsync($"# Project: {project.Name}");
        }

        await writer.WriteLineAsync($"# Project Path: {project.Path}");
        await writer.WriteLineAsync($"# Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        await writer.WriteLineAsync($"# Files in this part: {files.Count}");

        if (_configuration.EnableAutoSplit)
        {
            await writer.WriteLineAsync($"# Max tokens per file: {_configuration.MaxTokensPerFile:N0}");
        }

        await writer.WriteLineAsync();
        await writer.WriteLineAsync(new string('=', 80));
        await writer.WriteLineAsync();

        foreach (var fileContent in files)
        {
            await WriteFileContentToOutputAsync(writer, fileContent);
        }

        var outputFileName = totalParts > 1 
            ? $"{Path.GetFileNameWithoutExtension(outputPath)}_part{partNumber:D2}.txt"
            : Path.GetFileName(outputPath);

        Console.WriteLine($"  Extracted {files.Count} files to: {outputFileName}");
        return files.Count;
    }

    private async Task WriteFileToOutputAsync(StreamWriter writer, string filePath)
    {
        await writer.WriteLineAsync(filePath);
        await writer.WriteLineAsync(new string('-', filePath.Length));
        await writer.WriteLineAsync();

        if (_configuration.SkipBinaryFiles && IsBinaryFile(filePath))
        {
            await writer.WriteLineAsync("# [Binary file - content not extracted]");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync();
            return;
        }

        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            await writer.WriteLineAsync(content);
        }
        catch (Exception ex)
        {
            await writer.WriteLineAsync($"# [Error reading file: {ex.Message}]");
        }

        await writer.WriteLineAsync();
        await writer.WriteLineAsync();
    }

    private async Task WriteFileContentToOutputAsync(StreamWriter writer, FileContent fileContent)
    {
        await writer.WriteLineAsync(fileContent.Path);
        await writer.WriteLineAsync(new string('-', fileContent.Path.Length));
        await writer.WriteLineAsync();

        if (_configuration.SkipBinaryFiles && IsBinaryFile(fileContent.Path))
        {
            await writer.WriteLineAsync("# [Binary file - content not extracted]");
        }
        else
        {
            await writer.WriteLineAsync(fileContent.Content);
        }

        await writer.WriteLineAsync();
        await writer.WriteLineAsync();
    }

    private static bool IsBinaryFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        var binaryExtensions = new HashSet<string>
        {
            ".exe", ".dll", ".bin", ".obj", ".pdb", ".lib", ".so", ".dylib",
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".ico", ".tiff",
            ".pdf", ".zip", ".rar", ".7z", ".tar", ".gz",
            ".mp3", ".mp4", ".wav", ".avi", ".mov",
            ".nupkg", ".vsix"
        };

        return binaryExtensions.Contains(extension);
    }

    private static string GetSafeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var safeName = new string(fileName.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
        
        // Ensure it's not too long
        if (safeName.Length > 100)
        {
            safeName = safeName.Substring(0, 100);
        }
        
        return safeName;
    }

    private static int EstimateTokenCount(string content)
    {
        // More sophisticated token estimation
        // Count words + characters/5 as a reasonable approximation
        var words = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var charBasedTokens = content.Length / 5;
        return Math.Max(words, charBasedTokens);
    }

    private static Encoding GetEncoding(string encodingName)
    {
        try
        {
            return encodingName.ToLowerInvariant() switch
            {
                "utf-8" or "utf8" => Encoding.UTF8,
                "utf-16" or "utf16" => Encoding.Unicode,
                "ascii" => Encoding.ASCII,
                _ => Encoding.GetEncoding(encodingName)
            };
        }
        catch
        {
            Console.WriteLine($"Warning: Unknown encoding '{encodingName}', using UTF-8");
            return Encoding.UTF8;
        }
    }

    private static string FormatFileSize(long bytes)
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

    private static int ExitWithError(string message)
    {
        Console.WriteLine($"Error: {message}");
        return 1;
    }

    private class ProjectInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
    }

    private class FileContent
    {
        public string Path { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}