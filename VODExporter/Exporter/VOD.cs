namespace VODExporter.Exporter;




/// <summary>
/// VOD object. Holds data for the vod that will be exported.
/// </summary>
public class Vod()
{
    
    public required string Username { get; init; }
    public required string Id { get; init; }
    public required TimeSpan Duration { get; init; }

    public required bool UpdateJson { get; init; } = true;

}