using Jex.Tools.CLI.Modules;

namespace Jex.Tools.Tests.Core;

public class SystemInfoModuleTests
{
    private readonly SystemInfoModule _module;

    public SystemInfoModuleTests()
    {
        _module = new SystemInfoModule();
    }

    [Fact]
    public void Module_Properties_ShouldHaveCorrectValues()
    {
        // Assert
        Assert.Equal("System Information", _module.Name);
        Assert.Equal("Shows system and environment information", _module.Description);
        Assert.Equal("sysinfo", _module.Command);
        Assert.Equal("1.0.0", _module.Version);
    }

    [Fact]
    public async Task ExecuteAsync_WithHelpFlag_ShouldReturnZero()
    {
        // Arrange
        var args = new[] { "--help" };

        // Act
        var result = await _module.ExecuteAsync(args);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithHFlag_ShouldReturnZero()
    {
        // Arrange
        var args = new[] { "-h" };

        // Act
        var result = await _module.ExecuteAsync(args);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoArgs_ShouldReturnZero()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var result = await _module.ExecuteAsync(args);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithEnvFlag_ShouldReturnZero()
    {
        // Arrange
        var args = new[] { "--env" };

        // Act
        var result = await _module.ExecuteAsync(args);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ShowHelp_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => _module.ShowHelp());
        Assert.Null(exception);
    }
}