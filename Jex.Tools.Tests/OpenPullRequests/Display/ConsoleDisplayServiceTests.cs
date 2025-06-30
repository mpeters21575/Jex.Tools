using FluentAssertions;
using Jex.Tools.OpenPullRequests.Configuration;
using Jex.Tools.OpenPullRequests.Display;
using Jex.Tools.OpenPullRequests.Models;

namespace Jex.Tools.Tests.OpenPullRequests.Display;

public class ConsoleDisplayServiceTests
{
    private readonly ConsoleDisplayService _displayService;
    private readonly AzureDevOpsConfiguration _configuration;
    private readonly StringWriter _stringWriter;

    public ConsoleDisplayServiceTests()
    {
        _configuration = new AzureDevOpsConfiguration
        {
            OrganizationUrl = "https://dev.azure.com/testorg",
            PersonalAccessToken = "test",
            ShowOnlyMyPullRequests = true
        };
        _displayService = new ConsoleDisplayService(_configuration);
        
        _stringWriter = new StringWriter();
        Console.SetOut(_stringWriter);
    }

    [Fact]
    public void ShowHeader_ShouldDisplayCorrectInformation()
    {
        // Act
        _displayService.ShowHeader();
        var output = _stringWriter.ToString();

        // Assert
        output.Should().Contain("Azure DevOps Pull Request Scanner");
        output.Should().Contain("Mode: My Pull Requests Only");
    }

    [Fact]
    public void ShowProjectCount_ShouldDisplayCount()
    {
        // Act
        _displayService.ShowProjectCount(5);
        var output = _stringWriter.ToString();

        // Assert
        output.Should().Contain("Found 5 projects");
    }

    [Fact]
    public void ShowScanningProject_ShouldDisplayProjectName()
    {
        // Act
        _displayService.ShowScanningProject("TestProject");
        var output = _stringWriter.ToString();

        // Assert
        output.Should().Contain("Scanning project: TestProject");
    }

    [Fact]
    public void ShowSummary_WithNoResults_ShouldShowCorrectMessage()
    {
        // Arrange
        var scanResult = new ScanResult
        {
            TotalProjects = 5
        };

        // Act
        _displayService.ShowSummary(scanResult);
        var output = _stringWriter.ToString();

        // Assert
        output.Should().Contain("Total projects scanned: 5");
        output.Should().Contain("Projects with open PRs: 0");
        output.Should().Contain("Total open pull requests: 0");
        output.Should().Contain("No open pull requests found that you created!");
    }

    [Fact]
    public void ShowSummary_WithAllPrsMode_ShouldShowCorrectMessage()
    {
        // Arrange
        var config = new AzureDevOpsConfiguration
        {
            OrganizationUrl = "https://dev.azure.com/testorg",
            PersonalAccessToken = "test",
            ShowOnlyMyPullRequests = false
        };
        var displayService = new ConsoleDisplayService(config);
        var scanResult = new ScanResult
        {
            TotalProjects = 3
        };

        // Act
        displayService.ShowSummary(scanResult);
        var output = _stringWriter.ToString();

        // Assert
        output.Should().Contain("Filter: All Pull Requests");
        output.Should().Contain("No open pull requests found!");
    }
}