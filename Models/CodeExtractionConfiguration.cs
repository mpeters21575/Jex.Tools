namespace Jex.Tools.SolutionCodeExtractor.Models;

public class SolutionStructureConfiguration
{
    public string? SolutionPath { get; set; }
    public bool ShowHiddenFiles { get; set; } = false;
    public bool ShowBuildArtifacts { get; set; } = false;
    public int MaxDepth { get; set; } = 10;
    public bool ShowFileSize { get; set; } = false;
    public string[]? IncludeExtensions { get; set; }
    public string[]? ExcludeExtensions { get; set; }
    public required IEnumerable<string> DefaultRelevantExtensions { get; set; }
}

public class CodeExtractionConfiguration
{
    public string? SolutionPath { get; set; }
    public string? OutputPath { get; set; }
    public string[]? IncludeExtensions { get; set; }
    public string[]? ExcludeExtensions { get; set; }
    public required IEnumerable<string> DefaultExtensions { get; set; }
    public double MaxFileSizeMB { get; set; } = 1.0;
    public string Encoding { get; set; } = "utf-8";
    public bool SkipBinaryFiles { get; set; } = true;
    public bool ShowProgress { get; set; } = true;
    public bool SeparateFilePerProject { get; set; } = false;
    public string? OutputDirectory { get; set; }
    public int MaxTokensPerFile { get; set; }  // Conservative limit for Claude's 200k context
    public bool EnableAutoSplit { get; set; } = true;
}