using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace Jex.Tools.OpenPullRequests.Services;

/// <summary>
/// Defines operations for working with Azure DevOps repositories.
/// </summary>
public interface IRepositoryService
{
    /// <summary>
    /// Gets all repositories in a project.
    /// </summary>
    /// <param name="projectId">Project identifier.</param>
    /// <returns>List of repositories.</returns>
    Task<List<GitRepository>> GetRepositoriesAsync(string projectId);
}

/// <summary>
/// Handles Azure DevOps repository operations.
/// </summary>
public sealed class RepositoryService(VssConnection connection) : IRepositoryService
{
    private readonly VssConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    public async Task<List<GitRepository>> GetRepositoriesAsync(string projectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        
        try
        {
            using var gitClient = await _connection.GetClientAsync<GitHttpClient>();
            var repositories = await gitClient.GetRepositoriesAsync(projectId);
            return repositories ?? [];
        }
        catch
        {
            return [];
        }
    }
}