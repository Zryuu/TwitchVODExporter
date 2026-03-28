using VODExporter.Exporter;

namespace VODExporter.Twitch;

// TODO:: Add Blacklist for vods. Write to JSON file. (maybe delete entry after 30-days)

/// <summary>
/// Class holds info related to the Streamer.
/// </summary>
public class StreamerAdt
{
    /// <summary>
    /// The Twitch username of the streamer.
    /// </summary>
    public required string Username { get; init; }
    
    /// <summary>
    /// Queue that holds the VODs to be exported.
    /// </summary>
    public VodQueue VodQueue { get; init; }

    public List<string>? ExportHistory { get; init; } = [];

    /// <summary>
    /// Indicates whether the streamer is currently live.
    /// </summary>
    public bool IsCurrentlyLive { get; set; }

    /// <summary>
    /// The time the live status was last checked via API.
    /// </summary>
    public DateTime LastCheckedTimestamp { get; set; }

    /// <summary>
    /// The ID of the last exported VOD, if any.
    /// </summary>
    public string? LastExportedVod { get; set; }
    
    /// <summary>
    /// Timestamp of the last exported VOD, if any.
    /// </summary>
    public DateTime? LastExportedTimestamp { get; set; }
    
    /// <summary>
    /// Twitch ID of the streamer.
    /// </summary>
    public required string Id  { get; init; }
    
    /// <summary>
    /// Amount of VODs exported in the last 27 hours. Hast to be tracked differently than ExportedTimes.Count
    /// due to the fact that videos above 15hours are uploaded in separate parts if the user isn't verified with YT.
    /// </summary>
    public int AmountExported { get; set; } = 0;

    /// <summary>
    /// Limit on exports (within 24h).
    /// </summary>
    public int LimitThrottle { get; init; } = 2;

    /// <summary>
    /// If the Streamer's uploads are Throttled.
    /// </summary>
    public bool Throttled { get; set; }
    
    /// <summary>
    /// Time when Throttled.
    /// </summary>
    public DateTime? ThrottledTimestamp { get; set; } = null;

    /// <summary>
    /// List holding all ExportedTimes. Clears an entry if more than <see cref="Program.ThrottleCooldown"/> has passed.
    /// </summary>
    public List<DateTime>? ExportTimes { get; set; } = [];

    /// <summary>
    /// List holding VOD ID's that Shouldn't be exported.
    /// </summary>
    public List<string>? BlackList { get; set; } = [];

    /// <summary>
    /// If the exporter should keep adding new videos. If it's false, it'll export all remaining videos without adding
    /// new ones.
    /// </summary>
    public bool Active { get; set; } = true;
    
    /// <summary>
    /// If the Streamer is on Cooldown between individual Exports (Not ThrottleCooldown).
    /// </summary>
    public bool Export { get; set; } = true;

    public StreamerAdt()
    {
        VodQueue = new VodQueue {};
    }
    
}