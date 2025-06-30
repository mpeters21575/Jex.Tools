using FluentAssertions;
using Jex.Tools.OpenPullRequests.Services;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Moq;

namespace Jex.Tools.Tests.Services;

public class RepositoryServiceTests
{
    private readonly Mock<VssConnection> _connectionMock;
    private readonly Mock<GitHttpClient> _gitClientMock;
    private readonly IRepositoryService _service;

    public RepositoryServiceTests()
    {
        _connectionMock = new Mock<VssConnection>(new Uri("https://test"), null);
        _gitClientMock = new Mock<GitHttpClient>(new Uri("https://test"), null);
        
        _connectionMock.Setup(x => x.GetClientAsync<GitHttpClient>(CancellationToken.None))
            .ReturnsAsync(_gitClientMock.Object);
        
        _service = new RepositoryService(_connectionMock.Object);
    }

    [Fact]
    public async Task GetRepositoriesAsync_WithValidProjectId_ShouldReturnRepositories()
    {
        // Arrange
        var projectId = "test-project-id";
        var repositories = new List<GitRepository>
        {
            new() { Name = "Repo1" },
            new() { Name = "Repo2" }
        };
        
        _gitClientMock.Setup(x => x.GetRepositoriesAsync(projectId, It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositories);

        // Act
        var result = await _service.GetRepositoriesAsync(projectId);

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Repo1");
        result[1].Name.Should().Be("Repo2");
    }

    [Fact]
    public async Task GetRepositoriesAsync_WithNullOrWhitespaceProjectId_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetRepositoriesAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetRepositoriesAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetRepositoriesAsync("   "));
    }

    [Fact]
    public async Task GetRepositoriesAsync_WhenExceptionOccurs_ShouldReturnEmptyList()
    {
        // Arrange
        var projectId = "test-project-id";
        _gitClientMock.Setup(x => x.GetRepositoriesAsync(projectId, It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API Error"));

        // Act
        var result = await _service.GetRepositoriesAsync(projectId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRepositoriesAsync_WhenReturnsNull_ShouldReturnEmptyList()
    {
        // Arrange
        var projectId = "test-project-id";
        _gitClientMock.Setup(x => x.GetRepositoriesAsync(projectId, It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<GitRepository>)null!);

        // Act
        var result = await _service.GetRepositoriesAsync(projectId);

        // Assert
        result.Should().BeEmpty();
    }
}