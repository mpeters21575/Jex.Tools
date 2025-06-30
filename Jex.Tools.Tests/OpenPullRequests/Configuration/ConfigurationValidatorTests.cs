using FluentAssertions;
using Jex.Tools.OpenPullRequests.Configuration;

namespace Jex.Tools.Tests.OpenPullRequests.Configuration;

public class ConfigurationValidatorTests
{
    [Fact]
    public void Validate_WithValidConfiguration_ShouldReturnTrue()
    {
        // Arrange
        var config = new AzureDevOpsConfiguration
        {
            OrganizationUrl = "https://dev.azure.com/testorg",
            PersonalAccessToken = "test-token",
            ShowOnlyMyPullRequests = true
        };

        // Act
        var result = ConfigurationValidator.Validate(config);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNullOrEmptyOrgUrl_ShouldReturnFalse()
    {
        // Arrange
        var config = new AzureDevOpsConfiguration
        {
            OrganizationUrl = "",
            PersonalAccessToken = "test-token",
            ShowOnlyMyPullRequests = true
        };

        // Act & Assert
        ConfigurationValidator.Validate(config).Should().BeFalse();

        config.OrganizationUrl = null!;
        ConfigurationValidator.Validate(config).Should().BeFalse();

        config.OrganizationUrl = "   ";
        ConfigurationValidator.Validate(config).Should().BeFalse();
    }

    [Fact]
    public void Validate_WithPlaceholderOrgUrl_ShouldReturnFalse()
    {
        // Arrange
        var config = new AzureDevOpsConfiguration
        {
            OrganizationUrl = "https://dev.azure.com/{your-organization}",
            PersonalAccessToken = "test-token",
            ShowOnlyMyPullRequests = true
        };

        // Act & Assert
        ConfigurationValidator.Validate(config).Should().BeFalse();
    }

    [Fact]
    public void Validate_WithNullOrEmptyPat_ShouldReturnFalse()
    {
        // Arrange
        var config = new AzureDevOpsConfiguration
        {
            OrganizationUrl = "https://dev.azure.com/testorg",
            PersonalAccessToken = "",
            ShowOnlyMyPullRequests = true
        };

        // Act & Assert
        ConfigurationValidator.Validate(config).Should().BeFalse();

        config.PersonalAccessToken = null!;
        ConfigurationValidator.Validate(config).Should().BeFalse();

        config.PersonalAccessToken = "   ";
        ConfigurationValidator.Validate(config).Should().BeFalse();
    }

    [Fact]
    public void Validate_WithPlaceholderPat_ShouldReturnFalse()
    {
        // Arrange
        var config = new AzureDevOpsConfiguration
        {
            OrganizationUrl = "https://dev.azure.com/testorg",
            PersonalAccessToken = "{your-personal-access-token}",
            ShowOnlyMyPullRequests = true
        };

        // Act & Assert
        ConfigurationValidator.Validate(config).Should().BeFalse();
    }
}