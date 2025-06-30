namespace Jex.Tools.SolutionStructureAnalyzer.Services;

public class SolutionScanner
{
    private readonly ISolutionStructureService _solutionService;
    private readonly IDisplayService _displayService;
        
    public SolutionScanner(
        ISolutionStructureService solutionService,
        IDisplayService displayService)
    {
        _solutionService = solutionService ?? throw new ArgumentNullException(nameof(solutionService));
        _displayService = displayService ?? throw new ArgumentNullException(nameof(displayService));
    }
        
    public async Task<int> ScanAsync(string solutionPath)
    {
        try
        {
            _displayService.ShowProgress("Scanning solution structure...");
                
            int result = await _solutionService.ScanAsync(solutionPath);
                
            if (result != 0)
            {
                _displayService.ShowError("Failed to scan solution.");
                return result;
            }
                
            _displayService.Clear();
            _solutionService.PrintStructure();
                
            return 0;
        }
        catch (Exception ex)
        {
            _displayService.ShowError($"Error during scan: {ex.Message}");
            return 1;
        }
    }
}