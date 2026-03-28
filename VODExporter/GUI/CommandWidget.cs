using Spectre.Console;

namespace VODExporter.GUI;

public class CommandWidget
{
    public static Panel Render()
    {
        return new Panel(new Markup("[bold cyan]>[/] Enter Command: ")).Border(BoxBorder.None);
    }
}