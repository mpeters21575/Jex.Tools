namespace Jex.Tools.SolutionStructureAnalyzer.Services;

public interface IDisplayService
{
    void ShowProgress(string message);
    void ShowError(string message);
    void ShowWarning(string message);
    void ShowInfo(string message);
    void Clear();
        
    // Color output methods
    void WriteFolder(string text);
    void WriteFile(string text);
    void WriteProject(string text);
    void WriteSolutionFolder(string text);
    void WriteStructure(string text);
    void WriteError(string text);
    void WriteLine();
}

public class ConsoleDisplayService : IDisplayService
{
    private int _lastMessageLength = 0;

    public void ShowProgress(string message)
    {
        ClearCurrentLine();
        Console.Write(message);
        _lastMessageLength = message.Length;
    }

    public void ShowError(string message)
    {
        ClearCurrentLine();
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"ERROR: {message}");
        Console.ForegroundColor = originalColor;
        _lastMessageLength = 0;
    }

    public void ShowWarning(string message)
    {
        ClearCurrentLine();
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"WARNING: {message}");
        Console.ForegroundColor = originalColor;
        _lastMessageLength = 0;
    }

    public void ShowInfo(string message)
    {
        ClearCurrentLine();
        Console.WriteLine(message);
        _lastMessageLength = 0;
    }

    public void Clear()
    {
        ClearCurrentLine();
        _lastMessageLength = 0;
    }

    public void WriteFolder(string text)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.Write(text);
        Console.ForegroundColor = originalColor;
    }

    public void WriteFile(string text)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(text);
        Console.ForegroundColor = originalColor;
    }

    public void WriteProject(string text)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(text);
        Console.ForegroundColor = originalColor;
    }

    public void WriteSolutionFolder(string text)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.Write(text);
        Console.ForegroundColor = originalColor;
    }

    public void WriteStructure(string text)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(text);
        Console.ForegroundColor = originalColor;
    }

    public void WriteError(string text)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write(text);
        Console.ForegroundColor = originalColor;
    }

    public void WriteLine()
    {
        Console.WriteLine();
    }

    private void ClearCurrentLine()
    {
        if (_lastMessageLength > 0)
        {
            Console.Write("\r" + new string(' ', _lastMessageLength) + "\r");
        }
    }
}