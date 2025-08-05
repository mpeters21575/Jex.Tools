using Jex.Tools.OpenPullRequests.Configuration;
using Jex.Tools.OpenPullRequests.Models;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Jex.Tools.OpenPullRequests.Display;

public class ConsoleDisplayService(AzureDevOpsConfiguration configuration) : IDisplayService
{
    private const string Separator = "==================================================";
    private const string RepositorySeparator = "--------------------------------------------------";

    public void ShowHeader()
    {
        Console.WriteLine("Azure DevOps Pull Request Scanner");
        Console.WriteLine($"Mode: {(configuration.ShowOnlyMyPullRequests ? "My Pull Requests Only" : "All Pull Requests")}");
        Console.WriteLine();
        Console.WriteLine("Fetching all projects in the organization...\n");
    }
    
    public void ShowProjectCount(int count)
    {
        Console.WriteLine($"Found {count} projects\n");
    }
    
    public void ShowScanningProject(string projectName)
    {
        Console.WriteLine($"Scanning project: {projectName}");
    }
    
    public void ShowProjectPullRequests(string projectName, List<RepositoryPullRequests> repositories)
    {
        Console.WriteLine($"\n==== PROJECT: {projectName} ====");
        
        foreach (var repository in repositories)
        {
            ShowRepositoryPullRequests(repository);
        }
    }
    
    public void ShowSummary(ScanResult result)
    {
        Console.WriteLine(Separator);
        Console.WriteLine($"Organization: {configuration.OrganizationUrl}");
        Console.WriteLine($"Filter: {(configuration.ShowOnlyMyPullRequests ? "My Pull Requests Only" : "All Pull Requests")}");
        Console.WriteLine($"Total projects scanned: {result.TotalProjects}");
        Console.WriteLine($"Projects with open PRs: {result.ProjectsWithPullRequests}");
        Console.WriteLine($"Total open pull requests: {result.TotalPullRequests}");
        Console.WriteLine();
        
        if (result.TotalPullRequests == 0)
        {
            if (configuration.ShowOnlyMyPullRequests)
            {
                Console.WriteLine("No open pull requests found that you created!");
            }
            else
            {
                Console.WriteLine("No open pull requests found!");
            }
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
    
    private void ShowRepositoryPullRequests(RepositoryPullRequests repository)
    {
        Console.WriteLine($"\nRepository: {SanitizeForDisplay(repository.RepositoryName)}");
        Console.WriteLine(RepositorySeparator);
        
        foreach (var pr in repository.PullRequests)
        {
            ShowPullRequest(pr, repository.ProjectName, repository.RepositoryName);
        }
    }
    
    private void ShowPullRequest(GitPullRequest pr, string projectName, string repositoryName)
    {
        Console.WriteLine($"PR #{pr.PullRequestId}: {pr.Title}");
        Console.WriteLine($"   Created: {pr.CreationDate:yyyy-MM-dd HH:mm}");
        Console.WriteLine($"   Author: {pr.CreatedBy.DisplayName}");
        Console.WriteLine($"   Source: {pr.SourceRefName.Replace("refs/heads/", "")}");
        Console.WriteLine($"   Target: {pr.TargetRefName.Replace("refs/heads/", "")}");
        
        var prUrl = BuildPullRequestUrl(pr, projectName, repositoryName);
        Console.WriteLine($"   URL: {prUrl}");
        
        if (pr.Reviewers.Length > 0)
        {
            ShowReviewers(pr.Reviewers);
        }
        
        Console.WriteLine();
    }
    
    private void ShowReviewers(IdentityRefWithVote[] reviewers)
    {
        var reviewerDetails = reviewers.Select(r => $"{r.DisplayName} ({GetVoteText(r.Vote)})");
        Console.WriteLine($"   Reviewers: {string.Join(", ", reviewerDetails)}");
    }
    
    private string GetVoteText(short vote) => vote switch
    {
        0 => "No vote",
        10 => "Approved",
        5 => "Approved with suggestions",
        -5 => "Waiting for author",
        -10 => "Rejected",
        _ => "Unknown"
    };
    
    private string BuildPullRequestUrl(GitPullRequest pr, string projectName, string repositoryName)
    {
        // The pull request URL is usually available directly in the pr.Url property
        if (!string.IsNullOrEmpty(pr.Url))
        {
            // The API URL might be in a different format, so let's convert it to the web URL
            if (pr.Url.Contains("/_apis/"))
            {
                // Convert API URL to web URL
                var baseUrl = pr.Url.Substring(0, pr.Url.IndexOf("/_apis"));
                return $"{baseUrl}/_git/{repositoryName}/pullrequest/{pr.PullRequestId}";
            }
            return pr.Url;
        }
        
        // Build URL manually
        // The web URL format is: https://dev.azure.com/{org}/{project}/_git/{repo}/pullrequest/{id}
        return $"{configuration.OrganizationUrl}/{Uri.EscapeDataString(projectName)}/_git/{Uri.EscapeDataString(repositoryName)}/pullrequest/{pr.PullRequestId}";
    }
    
    private string SanitizeForDisplay(string text)
    {
        return text
            .Replace(":", " - ")
            .Replace("/", "-")
            .Replace("\\", "-");
    }
}