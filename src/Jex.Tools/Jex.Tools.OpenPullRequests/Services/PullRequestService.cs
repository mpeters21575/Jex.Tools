using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Jex.Tools.OpenPullRequests.Configuration;

namespace Jex.Tools.OpenPullRequests.Services;

/// <summary>
/// Defines operations for working with Azure DevOps pull requests.
/// </summary>
public interface IPullRequestService
{
    /// <summary>
    /// Gets pull requests for a repository.
    /// </summary>
    /// <param name="projectName">Project name.</param>
    /// <param name="repositoryId">Repository identifier.</param>
    /// <returns>List of pull requests.</returns>
    Task<List<GitPullRequest>> GetPullRequestsAsync(string projectName, string repositoryId);
}

/// <summary>
/// Handles Azure DevOps pull request operations.
/// </summary>
public sealed class PullRequestService : IPullRequestService
{
    private readonly VssConnection _connection;
    private readonly AzureDevOpsConfiguration _configuration;
    
    public PullRequestService(VssConnection connection, AzureDevOpsConfiguration configuration)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }
    
    public async Task<List<GitPullRequest>> GetPullRequestsAsync(string projectName, string repositoryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryId);
        
        try
        {
            using var gitClient = await _connection.GetClientAsync<GitHttpClient>();
            
            var searchCriteria = new GitPullRequestSearchCriteria
            {
                Status = PullRequestStatus.Active
            };
            
            // Apply user filter if configured
            if (_configuration.ShowOnlyMyPullRequests)
            {
                searchCriteria.CreatorId = _connection.AuthorizedIdentity.Id;
            }
            
            var pullRequests = await gitClient.GetPullRequestsAsync(
                project: projectName,
                repositoryId: repositoryId,
                searchCriteria: searchCriteria,
                top: 100
            );
            
            return pullRequests ?? [];
        }
        catch
        {
            // Return empty list on failure to allow graceful degradation
            return [];
        }
    }
}