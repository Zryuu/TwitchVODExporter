using Spectre.Console;
using VODExporter.Twitch;

namespace VODExporter.GUI;

/// <summary>
/// The TableWidget class which will house data related to the Streamer and their Queue's in a table format.
/// </summary>
public class TableWidget
{
    private readonly Table _table;
    private readonly StreamerManager _sm;
    private const int VodsPerRow = 2;

    public TableWidget(StreamerManager sm)
    {
        _sm = sm;
        _table = new Table().Border(TableBorder.Rounded);
        CreateTableColumns();
    }

    public void UpdateTable()
    {
        _table.Rows.Clear();
        RefreshTableData();
    }
    
    private void CreateTableColumns()
    {
        _table.AddColumn(new TableColumn("Streamer").NoWrap());
        _table.AddColumn(new TableColumn("VOD").Width(25).NoWrap().Padding(0,0,0,0));
        _table.AddColumn(new TableColumn("Blacklist").Width(25).NoWrap());
        _table.AddColumn(new TableColumn("Throttled").Width(9).NoWrap());
        _table.AddColumn(new TableColumn("Active").Width(7).NoWrap());
        _table.AddColumn(new TableColumn("24h").Width(3).NoWrap());
    }

    private void RefreshTableData()
    {

        foreach (var streamer in _sm.Streamers)
        {
            var username = streamer.IsCurrentlyLive ? $"[bold springgreen3]{streamer.Username}[/]" 
                                                    : $"[darkred_1]{streamer.Username}[/]";
            var throttled = streamer.Throttled ? $"[green3]{streamer.Throttled}[/]"
                                                    : $"[red3]{streamer.Throttled}[/]";
            var active = streamer.Active       ? $"[green3]{streamer.Active}[/]" 
                                                    : $"[red3]{streamer.Active}[/]";
            
            var dayExports = streamer.AmountExported.ToString();
            var vodList = streamer.VodQueue.VodList.Select(v => v.Id).ToList();
            var blacklist = streamer.BlackList!.ToList();
            var formattedBl = string.Join("\n", blacklist);
            
            //  Create the Text objects.
            var vodLinks = vodList
                .Select(v => $"[cyan link=https://www.twitch.tv/videos/{v}]{v}[/]")
                .ToList();
            
            //  Put them in sets of two.
            var vodChunks = vodLinks.Select((link, index) => new { link, index })
                .GroupBy(link => link.index / VodsPerRow)
                .Select(g => string.Join(", ", g.Select(link => link.link)))
                .ToList();
            
            //  join them together into one string.
            var formattedLines = string.Join("\n", vodChunks);
            
            _table.AddRow(username, $"{formattedLines}", $"[red3]{formattedBl}[/]", 
                          throttled, active, dayExports);

            _table.AddEmptyRow();
        }
    }

    public Table Render() =>  _table;
}