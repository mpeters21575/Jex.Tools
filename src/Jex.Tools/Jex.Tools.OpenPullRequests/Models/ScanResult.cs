namespace Jex.Tools.OpenPullRequests.Models;

public class ScanResult
{
    public Dictionary<string, List<RepositoryPullRequests>> ProjectPullRequests { get; } = new();
    public int TotalProjects { get; set; }
    public int ProjectsWithPullRequests => ProjectPullRequests.Count;
    public int TotalPullRequests => ProjectPullRequests.Values
        .SelectMany(r => r)
        .Sum(r => r.PullRequests.Count);
}