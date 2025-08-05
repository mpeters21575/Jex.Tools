namespace Jex.Tools.OpenPullRequests.Configuration;

public static class ConfigurationValidator
{
    public static bool Validate(AzureDevOpsConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.OrganizationUrl) || config.OrganizationUrl.Contains("{"))
        {
            Console.WriteLine("Please set the Azure DevOps organization URL.");
            Console.WriteLine("Either update the constant in the code or set the AZDEVOPS_ORG_URL environment variable.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.PersonalAccessToken) || config.PersonalAccessToken.Contains("{"))
        {
            Console.WriteLine("Please set your Azure DevOps Personal Access Token (PAT).");
            Console.WriteLine("Either update the constant in the code or set the AZDEVOPS_PAT environment variable.");
            return false;
        }

        return true;
    }
}