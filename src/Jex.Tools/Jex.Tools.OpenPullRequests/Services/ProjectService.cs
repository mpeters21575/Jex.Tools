using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace Jex.Tools.OpenPullRequests.Services;

/// <summary>
/// Defines operations for working with Azure DevOps projects.
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Gets all projects in the organization.
    /// </summary>
    /// <returns>List of projects.</returns>
    Task<List<TeamProjectReference>> GetAllProjectsAsync();
}

/// <summary>
/// Handles Azure DevOps project operations.
/// </summary>
public sealed class ProjectService : IProjectService
{
    private readonly VssConnection _connection;
    
    public ProjectService(VssConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
    
    public async Task<List<TeamProjectReference>> GetAllProjectsAsync()
    {
        try
        {
            using var projectClient = await _connection.GetClientAsync<ProjectHttpClient>();
            var projects = new List<TeamProjectReference>();
            string? continuationToken = null;
            
            do
            {
                var batch = await projectClient.GetProjects(
                    stateFilter: ProjectState.WellFormed,
                    continuationToken: continuationToken
                );
                
                if (batch != null)
                {
                    projects.AddRange(batch);
                    continuationToken = batch.ContinuationToken;
                }
            } while (!string.IsNullOrEmpty(continuationToken));
            
            return projects;
        }
        catch
        {
            // Return empty list on failure to allow graceful degradation
            return [];
        }
    }
}