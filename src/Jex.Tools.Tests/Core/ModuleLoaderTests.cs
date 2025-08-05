using Jex.Tools.CLI.Core;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jex.Tools.Tests.Core;

public class ModuleLoaderTests
{
    private readonly Mock<ILogger<ModuleLoader>> _loggerMock;
    private readonly ModuleLoader _moduleLoader;

    public ModuleLoaderTests()
    {
        _loggerMock = new Mock<ILogger<ModuleLoader>>();
        _moduleLoader = new ModuleLoader(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldNotThrow()
    {
        // Act
        var loader = new ModuleLoader(null);

        // Assert
        Assert.NotNull(loader);
        Assert.Empty(loader.Modules);
    }

    [Fact]
    public void LoadModulesFromDirectory_WithInvalidDirectory_ShouldLogWarning()
    {
        // Arrange
        var invalidDirectory = "C:\\NonExistentDirectory";

        // Act
        _moduleLoader.LoadModulesFromDirectory(invalidDirectory);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Module directory not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LoadModulesFromDirectory_WithNullOrWhitespace_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _moduleLoader.LoadModulesFromDirectory(null!));
        Assert.Throws<ArgumentException>(() => _moduleLoader.LoadModulesFromDirectory(""));
        Assert.Throws<ArgumentException>(() => _moduleLoader.LoadModulesFromDirectory("   "));
    }

    [Fact]
    public void GetModuleByCommand_WithValidCommand_ShouldReturnModule()
    {
        // Arrange
        var testModule = new TestModule { Command = "test-command" };
        _moduleLoader.LoadModulesFromAssembly(Assembly.GetExecutingAssembly());

        // Act
        var result = _moduleLoader.GetModuleByCommand("test-command");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-command", result.Command);
    }

    [Fact]
    public void GetModuleByCommand_WithInvalidCommand_ShouldReturnNull()
    {
        // Act
        var result = _moduleLoader.GetModuleByCommand("non-existent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetModuleByCommand_WithNullOrWhitespace_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _moduleLoader.GetModuleByCommand(null!));
        Assert.Throws<ArgumentException>(() => _moduleLoader.GetModuleByCommand(""));
        Assert.Throws<ArgumentException>(() => _moduleLoader.GetModuleByCommand("   "));
    }

    [Fact]
    public void LoadModulesFromAssembly_WithValidAssembly_ShouldLoadModules()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        _moduleLoader.LoadModulesFromAssembly(assembly);

        // Assert
        Assert.NotEmpty(_moduleLoader.Modules);
        Assert.Contains(_moduleLoader.Modules, m => m is TestModule);
    }
}

// Test module for testing
public class TestModule : ICliModule
{
    public string Name { get; set; } = "Test Module";
    public string Description { get; set; } = "Test module for unit tests";
    public string Command { get; set; } = "test";
    public string Version { get; set; } = "1.0.0";

    public Task<int> ExecuteAsync(string[] args)
    {
        return Task.FromResult(0);
    }

    public void ShowHelp()
    {
        Console.WriteLine("Test help");
    }
}

public class InvalidModule : ICliModule
{
    public InvalidModule(string param)
    {
    }

    public string Name => "Invalid";
    public string Description => "Invalid module";
    public string Command => "invalid";
    public string Version => "1.0.0";

    public Task<int> ExecuteAsync(string[] args) => Task.FromResult(0);
    public void ShowHelp() { }
}