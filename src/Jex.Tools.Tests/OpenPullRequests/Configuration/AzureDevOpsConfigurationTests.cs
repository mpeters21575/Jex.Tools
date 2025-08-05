using FluentAssertions;
using Jex.Tools.OpenPullRequests.Configuration;

namespace Jex.Tools.Tests.Core;

public class AzureDevOpsConfigurationTests
{
    [Fact]
    public void Configuration_ShouldHaveDefaultValues()
    {
        // Arrange
        var config = new AzureDevOpsConfiguration
        {
            OrganizationUrl = "https://dev.azure.com/test",
            PersonalAccessToken = "test-token",
            ShowOnlyMyPullRequests = true
        };

        // Assert
        config.OrganizationUrl.Should().Be("https://dev.azure.com/test");
        config.PersonalAccessToken.Should().Be("test-token");
        config.ShowOnlyMyPullRequests.Should().BeTrue();
    }
}