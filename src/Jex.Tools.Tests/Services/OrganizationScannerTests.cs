using FluentAssertions;
using Jex.Tools.OpenPullRequests.Display;
using Jex.Tools.OpenPullRequests.Models;
using Jex.Tools.OpenPullRequests.Services;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Moq;

namespace Jex.Tools.Tests.Services;

public class OrganizationScannerTests
{
    private readonly Mock<IProjectService> _projectServiceMock;
    private readonly Mock<IRepositoryService> _repositoryServiceMock;
    private readonly Mock<IPullRequestService> _pullRequestServiceMock;
    private readonly Mock<IDisplayService> _displayServiceMock;
    private readonly OrganizationScanner _scanner;

    public OrganizationScannerTests()
    {
        _projectServiceMock = new Mock<IProjectService>();
        _repositoryServiceMock = new Mock<IRepositoryService>();
        _pullRequestServiceMock = new Mock<IPullRequestService>();
        _displayServiceMock = new Mock<IDisplayService>();
        
        _scanner = new OrganizationScanner(
            _projectServiceMock.Object,
            _repositoryServiceMock.Object,
            _pullRequestServiceMock.Object,
            _displayServiceMock.Object
        );
    }

    [Fact]
    public void Constructor_WithNullDependencies_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OrganizationScanner(
            null!,
            _repositoryServiceMock.Object,
            _pullRequestServiceMock.Object,
            _displayServiceMock.Object
        ));
        
        Assert.Throws<ArgumentNullException>(() => new OrganizationScanner(
            _projectServiceMock.Object,
            null!,
            _pullRequestServiceMock.Object,
            _displayServiceMock.Object
        ));
        
        Assert.Throws<ArgumentNullException>(() => new OrganizationScanner(
            _projectServiceMock.Object,
            _repositoryServiceMock.Object,
            null!,
            _displayServiceMock.Object
        ));
        
        Assert.Throws<ArgumentNullException>(() => new OrganizationScanner(
            _projectServiceMock.Object,
            _repositoryServiceMock.Object,
            _pullRequestServiceMock.Object,
            null!
        ));
    }

    [Fact]
    public async Task ScanAsync_WithNoProjects_ShouldCompleteSuccessfully()
    {
        // Arrange
        _projectServiceMock.Setup(x => x.GetAllProjectsAsync())
            .ReturnsAsync(new List<TeamProjectReference>());

        // Act
        await _scanner.ScanAsync();

        // Assert
        _displayServiceMock.Verify(x => x.ShowHeader(), Times.Once);
        _displayServiceMock.Verify(x => x.ShowProjectCount(0), Times.Once);
        _displayServiceMock.Verify(x => x.ShowSummary(It.IsAny<ScanResult>()), Times.Once);
    }

    [Fact]
    public async Task ScanAsync_WithProjectsButNoRepos_ShouldCompleteSuccessfully()
    {
        // Arrange
        var projects = new List<TeamProjectReference>
        {
            new() { Id = Guid.NewGuid(), Name = "Project1" }
        };
        
        _projectServiceMock.Setup(x => x.GetAllProjectsAsync())
            .ReturnsAsync(projects);
        
        _repositoryServiceMock.Setup(x => x.GetRepositoriesAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<GitRepository>());

        // Act
        await _scanner.ScanAsync();

        // Assert
        _displayServiceMock.Verify(x => x.ShowScanningProject("Project1"), Times.Once);
        _displayServiceMock.Verify(x => x.ShowProjectPullRequests(It.IsAny<string>(), It.IsAny<List<RepositoryPullRequests>>()), Times.Never);
    }

    [Fact]
    public async Task ScanAsync_WithProjectsAndReposButNoPRs_ShouldCompleteSuccessfully()
    {
        // Arrange
        var projects = new List<TeamProjectReference>
        {
            new() { Id = Guid.NewGuid(), Name = "Project1" }
        };
        
        var repositories = new List<GitRepository>
        {
            new() { Id = Guid.NewGuid(), Name = "Repo1" }
        };
        
        _projectServiceMock.Setup(x => x.GetAllProjectsAsync())
            .ReturnsAsync(projects);
        
        _repositoryServiceMock.Setup(x => x.GetRepositoriesAsync(It.IsAny<string>()))
            .ReturnsAsync(repositories);
        
        _pullRequestServiceMock.Setup(x => x.GetPullRequestsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<GitPullRequest>());

        // Act
        await _scanner.ScanAsync();

        // Assert
        _displayServiceMock.Verify(x => x.ShowProjectPullRequests(It.IsAny<string>(), It.IsAny<List<RepositoryPullRequests>>()), Times.Never);
    }

    [Fact]
    public async Task ScanAsync_WithFullData_ShouldDisplayAllPullRequests()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projects = new List<TeamProjectReference>
        {
            new() { Id = projectId, Name = "Project1" }
        };
        
        var repoId = Guid.NewGuid();
        var repositories = new List<GitRepository>
        {
            new() { Id = repoId, Name = "Repo1" }
        };
        
        var pullRequests = new List<GitPullRequest>
        {
            new() { PullRequestId = 1, Title = "PR 1", CreationDate = DateTime.UtcNow },
            new() { PullRequestId = 2, Title = "PR 2", CreationDate = DateTime.UtcNow.AddHours(-1) }
        };
        
        _projectServiceMock.Setup(x => x.GetAllProjectsAsync())
            .ReturnsAsync(projects);
        
        _repositoryServiceMock.Setup(x => x.GetRepositoriesAsync(projectId.ToString()))
            .ReturnsAsync(repositories);
        
        _pullRequestServiceMock.Setup(x => x.GetPullRequestsAsync("Project1", repoId.ToString()))
            .ReturnsAsync(pullRequests);

        // Act
        await _scanner.ScanAsync();

        // Assert
        _displayServiceMock.Verify(x => x.ShowProjectPullRequests("Project1", It.Is<List<RepositoryPullRequests>>(
            list => list.Count == 1 && 
                    list[0].RepositoryName == "Repo1" && 
                    list[0].PullRequests.Count == 2
        )), Times.Once);
        
        _displayServiceMock.Verify(x => x.ShowSummary(It.Is<ScanResult>(
            result => result.TotalProjects == 1 &&
                      result.ProjectsWithPullRequests == 1 &&
                      result.TotalPullRequests == 2
        )), Times.Once);
    }

    [Fact]
    public async Task ScanAsync_ShouldOrderProjectsAndRepositoriesByName()
    {
        // Arrange
        var projects = new List<TeamProjectReference>
        {
            new() { Id = Guid.NewGuid(), Name = "ZProject" },
            new() { Id = Guid.NewGuid(), Name = "AProject" }
        };
        
        var repositories = new List<GitRepository>
        {
            new() { Id = Guid.NewGuid(), Name = "ZRepo" },
            new() { Id = Guid.NewGuid(), Name = "ARepo" }
        };
        
        var capturedProjectNames = new List<string>();
        
        _projectServiceMock.Setup(x => x.GetAllProjectsAsync())
            .ReturnsAsync(projects);
        
        _repositoryServiceMock.Setup(x => x.GetRepositoriesAsync(It.IsAny<string>()))
            .ReturnsAsync(repositories);
        
        _displayServiceMock.Setup(x => x.ShowScanningProject(It.IsAny<string>()))
            .Callback<string>(name => capturedProjectNames.Add(name));
        
        _pullRequestServiceMock.Setup(x => x.GetPullRequestsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<GitPullRequest>());

        // Act
        await _scanner.ScanAsync();

        // Assert
        capturedProjectNames.Should().Equal("AProject", "ZProject");
    }
}