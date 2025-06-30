using Microsoft.TeamFoundation.Core.WebApi;
using Jex.Tools.OpenPullRequests.Display;
using Jex.Tools.OpenPullRequests.Models;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Jex.Tools.OpenPullRequests.Services;

/// <summary>
/// Orchestrates the scanning of an Azure DevOps organization for pull requests.
/// </summary>
public sealed class OrganizationScanner
{
    private readonly IProjectService _projectService;
    private readonly IRepositoryService _repositoryService;
    private readonly IPullRequestService _pullRequestService;
    private readonly IDisplayService _displayService;
    
    public OrganizationScanner(
        IProjectService projectService,
        IRepositoryService repositoryService,
        IPullRequestService pullRequestService,
        IDisplayService displayService)
    {
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
        _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
        _pullRequestService = pullRequestService ?? throw new ArgumentNullException(nameof(pullRequestService));
        _displayService = displayService ?? throw new ArgumentNullException(nameof(displayService));
    }
    
    /// <summary>
    /// Scans the organization for pull requests.
    /// </summary>
    public async Task ScanAsync()
    {
        _displayService.ShowHeader();
        
        var projects = await _projectService.GetAllProjectsAsync();
        _displayService.ShowProjectCount(projects.Count);
        
        var scanResult = new ScanResult
        {
            TotalProjects = projects.Count
        };
        
        foreach (var project in projects.OrderBy(p => p.Name))
        {
            await ScanProjectAsync(project, scanResult);
        }
        
        _displayService.ShowSummary(scanResult);
    }
    
    private async Task ScanProjectAsync(TeamProjectReference project, ScanResult scanResult)
    {
        _displayService.ShowScanningProject(project.Name);
        
        var repositories = await _repositoryService.GetRepositoriesAsync(project.Id.ToString());
        if (repositories.Count == 0)
        {
            return;
        }
        
        var projectPullRequests = new List<RepositoryPullRequests>();
        
        foreach (var repository in repositories.OrderBy(r => r.Name))
        {
            var repoPullRequests = await ScanRepositoryAsync(project, repository);
            if (repoPullRequests != null && repoPullRequests.PullRequests.Count > 0)
            {
                projectPullRequests.Add(repoPullRequests);
            }
        }
        
        if (projectPullRequests.Count > 0)
        {
            scanResult.ProjectPullRequests[project.Name] = projectPullRequests;
            _displayService.ShowProjectPullRequests(project.Name, projectPullRequests);
        }
    }
    
    private async Task<RepositoryPullRequests?> ScanRepositoryAsync(
        TeamProjectReference project, 
        GitRepository repository)
    {
        var pullRequests = await _pullRequestService.GetPullRequestsAsync(
            project.Name, 
            repository.Id.ToString()
        );
        
        if (pullRequests.Count == 0)
        {
            return null;
        }
        
        return new RepositoryPullRequests
        {
            ProjectName = project.Name,
            RepositoryName = repository.Name,
            PullRequests = pullRequests.OrderByDescending(pr => pr.CreationDate).ToList()
        };
    }
}