using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Jex.Tools.OpenPullRequests.Models;

public class RepositoryPullRequests
{
    public string ProjectName { get; set; }
    public string RepositoryName { get; set; }
    public List<GitPullRequest> PullRequests { get; set; } = new();
}