using System.Text.Json;
using FluentAssertions;
using Jex.Tools.OpenPullRequests.Configuration;

namespace Jex.Tools.Tests.OpenPullRequests.Configuration;

public class ConfigurationLoaderTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _originalDirectory;

    public ConfigurationLoaderTests()
    {
        _originalDirectory = Directory.GetCurrentDirectory();
        _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDirectory);
        Directory.SetCurrentDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_originalDirectory);
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public void Load_WithNoConfiguration_ShouldReturnEmptyConfig()
    {
        // Act
        var config = ConfigurationLoader.Load();

        // Assert
        config.Should().NotBeNull();
        config.OrganizationUrl.Should().BeEmpty();
        config.PersonalAccessToken.Should().BeEmpty();
        config.ShowOnlyMyPullRequests.Should().BeTrue(); // Default value
    }

    [Fact]
    public void Load_WithAppSettingsJson_ShouldLoadConfiguration()
    {
        // Arrange
        var appSettings = new
        {
            AzureDevOps = new
            {
                OrganizationUrl = "https://dev.azure.com/testorg",
                PersonalAccessToken = "test-token-123",
                ShowOnlyMyPullRequests = false
            }
        };

        var json = JsonSerializer.Serialize(appSettings);
        File.WriteAllText(Path.Combine(_tempDirectory, "appsettings.json"), json);

        // Act
        var config = ConfigurationLoader.Load();

        // Assert
        config.OrganizationUrl.Should().Be("https://dev.azure.com/testorg");
        config.PersonalAccessToken.Should().Be("test-token-123");
        config.ShowOnlyMyPullRequests.Should().BeFalse();
    }

    [Fact]
    public void Load_WithEnvironmentVariables_ShouldOverrideAppSettings()
    {
        // Arrange
        var appSettings = new
        {
            AzureDevOps = new
            {
                OrganizationUrl = "https://dev.azure.com/jsonorg",
                PersonalAccessToken = "json-token",
                ShowOnlyMyPullRequests = true
            }
        };

        var json = JsonSerializer.Serialize(appSettings);
        File.WriteAllText(Path.Combine(_tempDirectory, "appsettings.json"), json);

        Environment.SetEnvironmentVariable("AZDEVOPS_ORG_URL", "https://dev.azure.com/envorg");
        Environment.SetEnvironmentVariable("AZDEVOPS_PAT", "env-token");
        Environment.SetEnvironmentVariable("AZDEVOPS_SHOW_ONLY_MY_PRS", "false");

        try
        {
            // Act
            var config = ConfigurationLoader.Load();

            // Assert
            config.OrganizationUrl.Should().Be("https://dev.azure.com/envorg");
            config.PersonalAccessToken.Should().Be("env-token");
            config.ShowOnlyMyPullRequests.Should().BeFalse();
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("AZDEVOPS_ORG_URL", null);
            Environment.SetEnvironmentVariable("AZDEVOPS_PAT", null);
            Environment.SetEnvironmentVariable("AZDEVOPS_SHOW_ONLY_MY_PRS", null);
        }
    }

    [Fact]
    public void Load_WithInvalidBooleanEnvironmentVariable_ShouldUseDefault()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AZDEVOPS_SHOW_ONLY_MY_PRS", "invalid-boolean");

        try
        {
            // Act
            var config = ConfigurationLoader.Load();

            // Assert
            config.ShowOnlyMyPullRequests.Should().BeTrue(); // Default value
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("AZDEVOPS_SHOW_ONLY_MY_PRS", null);
        }
    }
}