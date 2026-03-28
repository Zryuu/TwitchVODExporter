namespace VODExporter.Twitch;

/// <summary>
/// Wrapper class for Json data.
/// </summary>
public class StreamerDto
{
    
    public required string Username { get; init; }
    public required string? LastExportedVod { get; init; }
    public required DateTime? LastExportedTimestamp { get; init; }
    public required string Id { get; init; }
    public required List<DateTime>?  ExportedTimes { get; init; }
    
    public required bool Active { get; init; }
    
    public required int LimitThrottle { get; init; }
    
    public required List<string>? BlackList { get; init; }
    
    public required List<string>? ExportHistory { get; init; }
}