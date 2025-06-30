using Jex.Tools.OpenPullRequests.Models;

namespace Jex.Tools.OpenPullRequests.Display;

public interface IDisplayService
{
    void ShowHeader();
    void ShowProjectCount(int count);
    void ShowScanningProject(string projectName);
    void ShowProjectPullRequests(string projectName, List<RepositoryPullRequests> repositories);
    void ShowSummary(ScanResult result);
}