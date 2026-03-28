using Spectre.Console;
using VODExporter.Twitch;

namespace VODExporter.GUI;

/// <summary>
/// This class will handle the TUI (Terminal User Interface) of the program.
/// This is the canvas class that'll house all widget classes.
/// </summary>
public class Tui(StreamerManager sm, Logger logger)
{
    private readonly TableWidget _table = new (sm);
    private readonly LoggerWidget _logger = new ();
    private readonly CommandManager _cm = new (sm, logger);
    
    public async Task Run(CancellationToken token)
    {
        _logger.Subscribe(logger);

        await AnsiConsole.Live(RenderData())
            .AutoClear(false)
            .StartAsync(async ctx =>
            {
                while (!token.IsCancellationRequested)
                {
                    _table.UpdateTable();
                    ctx.UpdateTarget(RenderData());

                    if (Console.KeyAvailable)
                    {
                        var cmd = Console.ReadLine();
                        if (!string.IsNullOrWhiteSpace(cmd))
                            HandleCommand(cmd.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                    }
                    
                    await Task.Delay(250, token);
                }
            });
    }

    private Grid RenderData()
    {
        AnsiConsole.Clear();
        
        var grid = new Grid();
        grid.AddColumn(new GridColumn());
        grid.AddRow(_table.Render());
        grid.AddRow(new Panel(_logger.Render()).BorderColor(Color.Grey));
        grid.AddRow(CommandWidget.Render());

        return grid;
    }

    private void HandleCommand(string[] parts)
    {

        if (parts.Length == 0)
        {
            logger.Log("Empty Command, skipping parsing.", ELogType.Warning);
            return;
        }
        
        string command = parts[0].ToLower();
        string[] args = parts.Skip(1).ToArray();

        switch (command)
        {
            case "help":
                _cm.HelpCommand();
                return;
            case "add":
                _cm.AddStreamerToList(args[0]);
                return;
            case "remove":
                _cm.RemoveStreamerFromList(args[0]);
                return;
            case "addvod":
                _cm.AddVodToList(args[0], args[1]);
                return;
            case "removevod":
                _cm.RemoveVodFromList(args[0], args[1]);
                return;
            case "bl":
                _cm.BlackListVod(args[0], args[1]);
                return;
            case "rbl":
                _cm.RemoveBlacklistVod(args[0], args[1]);
                return;
            case "restore":
                _cm.RestoreVod(args[0], args[1]);
                return;
            case "active":
                _cm.Active(args[0]);
                return;
            default:
                logger.Log($"Invalid command: {command}. Type 'help' for a list of commands.", ELogType.Error);
                return;
        }
    }
}