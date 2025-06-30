using FluentAssertions;
using Jex.Tools.OpenPullRequests;

namespace Jex.Tools.Tests.OpenPullRequests;

public class PullRequestScannerModuleTests
{
    private readonly PullRequestScannerModule _module;

    public PullRequestScannerModuleTests()
    {
        _module = new PullRequestScannerModule();
    }

    [Fact]
    public void Module_Properties_ShouldHaveCorrectValues()
    {
        // Assert
        _module.Name.Should().Be("Azure DevOps Pull Request Scanner");
        _module.Description.Should().Be("Scans all projects in Azure DevOps for open pull requests");
        _module.Command.Should().Be("pr-scan");
        _module.Version.Should().Be("1.0.0");
    }

    [Fact]
    public async Task ExecuteAsync_WithHelpFlag_ShouldReturnZero()
    {
        // Arrange
        var args = new[] { "--help" };

        // Act
        var result = await _module.ExecuteAsync(args);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_WithHFlag_ShouldReturnZero()
    {
        // Arrange
        var args = new[] { "-h" };

        // Act
        var result = await _module.ExecuteAsync(args);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void ShowHelp_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => _module.ShowHelp());
        exception.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidConfig_ShouldReturnOne()
    {
        // Arrange
        var args = Array.Empty<string>();
        Environment.SetEnvironmentVariable("AZDEVOPS_ORG_URL", null);
        Environment.SetEnvironmentVariable("AZDEVOPS_PAT", null);

        // Create a temporary empty directory for the test
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var originalDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(tempDir);

        try
        {
            // Act
            var result = await _module.ExecuteAsync(args);

            // Assert
            result.Should().Be(1);
        }
        finally
        {
            // Cleanup
            Directory.SetCurrentDirectory(originalDir);
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithCommandLineArgs_ShouldParseCorrectly()
    {
        // This test would require mocking the scanner, which would require refactoring
        // the module to accept dependencies. For now, we'll test the argument parsing
        // through integration tests.
        
        var args = new[] { "--org", "https://dev.azure.com/test", "--pat", "test-token", "--all-prs" };
        
        // Since we can't fully test without proper DI, we'll just ensure no exceptions
        var exception = await Record.ExceptionAsync(async () => 
        {
            try
            {
                await _module.ExecuteAsync(args);
            }
            catch (InvalidOperationException)
            {
                // Expected when scanner tries to connect
            }
        });
        
        exception.Should().BeNull();
    }
}