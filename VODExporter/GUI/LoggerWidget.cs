using Spectre.Console;

namespace VODExporter.GUI;

/// <summary>
/// This class will handle storing and displaying all logs in the TUI.
/// </summary>
public class LoggerWidget
{

    private readonly List<string> _logs = [];

    public void Log(string message)
    {
        _logs.Add(message);
        if (_logs.Count > 10)
            _logs.RemoveAt(0);
    }

    public void Subscribe(Logger logger)
    {
        logger.OnLog += Log;
    }

    public Panel Render()
    {
        var text = string.Join("\n", _logs);
        return new Panel(new Markup(text));
    }
    
    public IReadOnlyList<string> GetLogs() => _logs.AsReadOnly();
}