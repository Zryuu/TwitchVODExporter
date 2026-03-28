namespace VODExporter;

public enum ELogType
{
    Info,
    Warning,
    Error,
    Ok
}


public class Logger
{
    public event Action<string>? OnLog;
    
    public void Log(string message, ELogType level = ELogType.Info)
    {
        var formatted = level switch
        {
            ELogType.Error   => $"[bold red][[ERROR]]:[/] {message}",
            ELogType.Warning => $"[bold yellow][[WARN]]:[/] {message}",
            ELogType.Ok      => $"[bold green][[OK]]:[/] {message}",
            ELogType.Info    => $"[bold blue][[INFO]]:[/] {message}"
        };
        
        OnLog?.Invoke(formatted);
    }
}