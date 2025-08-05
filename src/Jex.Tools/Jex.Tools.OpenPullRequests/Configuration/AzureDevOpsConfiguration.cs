namespace Jex.Tools.OpenPullRequests.Configuration;

public class AzureDevOpsConfiguration
{
    public required string OrganizationUrl { get; set; }
    public required string PersonalAccessToken { get; set; }
    public required bool ShowOnlyMyPullRequests { get; set; } = true;
}