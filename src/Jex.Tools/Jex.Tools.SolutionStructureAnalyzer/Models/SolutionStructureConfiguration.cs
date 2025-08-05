namespace Jex.Tools.SolutionStructureAnalyzer.Models;

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