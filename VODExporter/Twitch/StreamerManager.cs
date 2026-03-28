using System.Text.Json;
using VODExporter.GUI;

namespace VODExporter.Twitch;

public class StreamerManager(Program program, Logger logger)
{
    
    public List<StreamerAdt> Streamers = [];
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };


    /// <summary>
    /// Initializes Streamer Manager by populating List with <see cref="StreamerAdt"/> objects.
    /// </summary>
    /// <returns>Task.CompletedTask</returns>
    public async Task InitStreamManager()
    {
        logger.Log("Initializing stream manager...");
        await SetStreamersFromFile(GetDataFromFile());
        logger.Log("Initialized", ELogType.Ok);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Adds new Streamer to the List and JSON file.
    /// </summary>
    /// <param name="username">Twitch username.</param>
    /// <param name="id">Twitch ID of the user.</param>
    public async Task<bool> AddStreamer(string username)
    {
        var id = await program.TwitchApi.GetStreamerId(username);
        if (id == null)
        {
            logger.Log($"Failed to add new Streamer. {username}'s ID wasn't found in the API. " +
                       $"Please check the spelling.", ELogType.Error);
            return false;
        }
        
        var streamer = new StreamerAdt { Username = username, Id =  id};
        Streamers.Add(streamer);
        WriteToDataFile();
        
        logger.Log($"Added {username} to list.", ELogType.Ok);
        return true;
    }

    /// <summary>
    /// Removes streamer from List and JSON file.
    /// </summary>
    /// <param name="username">Username of streamer.</param>
    /// <returns>True if the streamer was removed, Otherwise False.</returns>
    public bool RemoveStreamer(string username)
    {
        if (Streamers.Find(x => x.Username == username) is { } streamer &&
            Streamers.Remove(streamer))
        {
            WriteToDataFile();
            logger.Log($"Removed {username} from list.", ELogType.Ok);
            return true;
        }

        logger.Log($"Failed to remove {username} from list. Unable to find Streamer in list.", ELogType.Error);
        return false;
    }

    /// <summary>
    /// Gets streamer in List.
    /// </summary>
    /// <param name="username">username of the Streamer.</param>
    /// <returns>Streamer's <see cref="StreamerAdt"/> object. Give's a null object if not found in list.</returns>
    /// <summary>
    /// Gets streamer in List.
    /// </summary>
    /// <param name="username">username of the Streamer.</param>
    /// <returns>Streamer's <see cref="StreamerAdt"/> object. Give's a null object if not found in list.</returns>
    public StreamerAdt? GetStreamer(string username)
    {
        if (Streamers.Find(x => x.Username == username) is { } streamer) return streamer;
        
        return null;
    }

    /// <summary>
    /// Updates the Streamer's Live status in the StreamerAdt.
    /// </summary>
    /// <param name="username">Username of the Streamer.</param>
    /// <param name="isLive">New Live status (Gotten from TwitchAPI).</param>
    public void UpdateStreamerLiveStatus(string username, bool isLive)
    {
        var streamer = GetStreamer(username);

        if (streamer == null)
        {
            logger.Log($"Streamer {username} not found in StreamerManager. " +
                       $"Function: UpdateStreamerLiveStatus", ELogType.Error);
            return;
        }

        streamer.IsCurrentlyLive = isLive;
        streamer.LastCheckedTimestamp = DateTime.Now;
    }
    
    /// <summary>
    /// Updates JSON file.
    /// </summary>
    public void UpdateJsonFile()
    {
        WriteToDataFile();
    }
    
    /// <summary>
    /// Check's is the Streamer is throttled. Also checks if <see cref="Program.ThrottleCooldown"/> time has passed.
    /// </summary>
    /// <param name="username">Twitch username.</param>
    /// <returns>True if Throttled, false otherwise.</returns>
    public bool IsStreamerThrottled(string username)
    {
        var streamer = GetStreamer(username);

        if (streamer == null)
        {
            logger.Log($"Streamer not found in Streamer List. Username: {username}. " +
                       $"Function: IsStreamerThrottled", ELogType.Error);
            return true;
        }

        if (streamer.AmountExported > streamer.LimitThrottle)
        {
            logger.Log($"{streamer}'s AmountExported exceeds Limit. Streamer is Throttled, delaying exports.");
            
            if (!streamer.Throttled)
            {
                streamer.ThrottledTimestamp = DateTime.Now;
                streamer.Throttled = true;
            }
        }
        
        if (HasExportCooldownElapsedForStreamer(username))
        {
            streamer.Throttled = false;
            streamer.ThrottledTimestamp = null;
        }

        return streamer.Throttled;
    }
    
    /// <summary>
    /// Increments <see cref="StreamerAdt.AmountExported"/> for given streamer.
    /// </summary>
    /// <param name="username">Twitch username.</param>
    /// <param name="amount">Amount to increment by.</param>
    public void IncrementAmountExportedForStreamer(string username, int amount = 1)
    {
        var streamer = GetStreamer(username);

        if (streamer == null)
        {
            logger.Log($"Streamer not found in Streamer List. Username: {username}. " +
                       $"Function: IncrementAmountExportedForStreamer", ELogType.Error);
            return;
        }

        streamer.AmountExported += amount;

        if (streamer.AmountExported >= streamer.LimitThrottle && !streamer.Throttled)
        {
            streamer.Throttled = true;
            streamer.ThrottledTimestamp = DateTime.Now;
            logger.Log($"Setting {streamer.Username}'s ThrottledTimestamp");
        }
    }
    
    /// <summary>
    /// Decrease's  <see cref="StreamerAdt.AmountExported"/> for given streamer. If given amount is 0, it'll set
    /// <see cref="StreamerAdt.AmountExported"/> to 0.
    /// </summary>
    /// <param name="username">Twitch username.</param>
    /// <param name="amount">Amount to Decrease AmountExported by. (clamps if below 0)</param>
    public void DecrementAmountExportedForStreamer(string username, int amount = 0)
    {
        Math.Clamp(amount, 0, 99);
        
        var streamer = GetStreamer(username);

        if (streamer == null)
        {
            logger.Log($"Streamer not found in Streamer List. Username: {username}. " +
                       $"Function: DecrementAmountExportedForStreamer()", ELogType.Error);
            return;
        }
        
        if (amount == 0)
        {
            streamer.AmountExported = 0;
            return;
        }
        
        streamer.AmountExported -= amount;
        streamer.AmountExported = Math.Clamp(streamer.AmountExported, 0, 99);
    }

    /// <summary>
    /// Adds new ExportedTime to <see cref="StreamerAdt"/>.
    /// </summary>
    /// <param name="username">Twitch username.</param>
    /// <param name="time"><see cref="DateTime"/> of the export.</param>
    public void AddExportedTime(string username, DateTime time)
    {
        var streamer = GetStreamer(username);

        if (streamer == null)
        {
            logger.Log($"Couldn't find streamer: {streamer}. Function: AddExportedTime()", ELogType.Error);
            return;
        }
        
        streamer.ExportTimes ??= [];
        streamer.ExportTimes.Add(time);
        streamer.LastExportedTimestamp = streamer.ExportTimes.Last();
        UpdateJsonFile();
    }

    /// <summary>
    /// Removes any ExportedTime over <see cref="Program.ThrottleCooldown"/> from <see cref="StreamerAdt.ExportTimes"/>
    /// (Runs auto in <see cref="Program.OnUpdate"/> loop)
    /// </summary>
    /// <param name="username">Streamer's username.</param>
    public void RemoveExportedTime(string username)
    {
        var streamer = GetStreamer(username);

        if (streamer == null)
        {
            logger.Log($"Couldn't find streamer: {streamer}. Function: RemoveExportedTime()", ELogType.Error);
            return;
        }

        if (streamer.ExportTimes == null || streamer.ExportTimes.Count == 0) return;
        
        var exportTimes = streamer.ExportTimes;

        var removedAmount = exportTimes.RemoveAll(e => 
                                                    (DateTime.Now - e).TotalHours > Program.ThrottleCooldown);

        if (removedAmount == 0) return;
        
        DecrementAmountExportedForStreamer(username, removedAmount);
        UpdateJsonFile();
    }

    /// <summary>
    /// Adds an entry to BlackList for given Streamer.
    /// </summary>
    /// <param name="username">Streamer's username</param>
    /// <param name="id">VOD's Id to be added to the BlackList.</param>
    public bool AddBlackListEntry(string username, string id)
    {
        var streamer = GetStreamer(username);

        if (streamer == null)
        {
            logger.Log($"StreamerManager::AddBlackListEntry:: Couldn't find streamer: {streamer}." 
                       + "Skipping Black List Entry.", ELogType.Error);
            return false;
        }

        streamer.BlackList ??= [];
        streamer.BlackList.Add(id);
        UpdateJsonFile();
        return true;
    }

    /// <summary>
    /// Removes an entry from Streamer's BlackList.
    /// </summary>
    /// <param name="username">Streamer's Blacklist.</param>
    /// <param name="id">VOD's Id.</param>
    public bool RemoveBlackListEntry(string username, string id)
    {
        var streamer = GetStreamer(username);

        if (streamer == null)
        {
            logger.Log($"StreamerManager::RemoveBlackListEntry:: Couldn't find streamer: {streamer}."
                + $"Skipped removal of BlackList Entry.", ELogType.Error);
            return false;
        }

        if (streamer.BlackList == null)
        {
            logger.Log($"StreamerManager::RemoveBlackListEntry:: {streamer}'s BlackList is null."
                       + $"Skipped removal of BlackList Entry.", ELogType.Error);
            return false;
        }

        if (!streamer.BlackList.Contains(id))
        {
            logger.Log($"StreamerManager::RemoveBlackListEntry:: {streamer}'s BlackList doesn't contain the id {id}."
                       + $"Skipped removal of BlackList Entry.", ELogType.Error);
            return false;
        }
        
        streamer.BlackList.Remove(id);
        UpdateJsonFile();
        return true;
    }

    /// <summary>
    /// Add's video to the Export list for Streamer.
    /// </summary>
    /// <param name="username">Streamer's username.</param>
    /// <param name="id">VOD's ID.</param>
    /// <param name="json">If the JSON file should be updated.</param>
    public bool AddVod(string username, string id, bool json = false)
    {
        var streamer = GetStreamer(username);

        if (streamer == null)
        {
            logger.Log($"StreamerManager::AddVod:: Streamer is null.", ELogType.Error);
            return false;
        }

        var video = program.TwitchApi.GetVideoInfo(streamer, id).Result;

        if (video == null)
        {
            logger.Log($"StreamerManager::AddVod:: Video gotten from API is null.", ELogType.Error);
            return false;
        }

        if (streamer.BlackList?.Any(b => b == video.Id) == true)
        {
            logger.Log($"StreamerManager::AddVod:: Streamer {video.Id} is already in the BlackList. " +
                       $"Not adding to Export list.");
            return false;
        }
        streamer.VodQueue.AddVod(streamer.Username, video.Id, video.Duration, json);
        if (json) UpdateJsonFile();
        return true;
    }

    /// <summary>
    /// Removes vod from Queue list.
    /// </summary>
    /// <param name="username">Streamer's username</param>
    /// <param name="id">VOD's ID.</param>
    /// <param name="json">If the JSON file should be updated with this change</param>
    public void RemoveVod(string username, string id, bool json = false)
    {
        var streamer = GetStreamer(username);

        if (streamer == null)
        {
            logger.Log($"Couldn't Find Streamer {username}. Function: RemoveVod()", ELogType.Error);
            return;
        }
        
        if (streamer.VodQueue.RemoveVod(streamer.Username, id))
            logger.Log($"VOD Removed from {streamer.Username}'s list.");
        else
            logger.Log($"VOD Removal failed for {id} in {streamer.Username}'s list.", ELogType.Error);
        if (json) UpdateJsonFile();
    }

    /// <summary>
    /// Updates the Streamer's Active bool and writes to JSON file.
    /// </summary>
    /// <param name="username">Streamer's Username.</param>
    /// <param name="newActive">New Active value.</param>
    public bool UpdateActive(string username, bool newActive)
    {
        var streamer = GetStreamer(username);
        if (streamer == null)
        {
            logger.Log($"Couldn't find Streamer {username}. Function: UpdateActive()", ELogType.Error);
            return false;
        }
        
        streamer.Active = newActive;
        UpdateJsonFile();
        return true;
    }
    
    /// <summary>
    /// Creates a timer to prevents uploads happening to fast by introducing a delay between exports.
    /// </summary>
    public async Task StartExportCooldown(string username)
    {
        var streamer = GetStreamer(username);

        if (streamer == null)
        {
            logger.Log($"Couldn't find Streamer {username}. Function: StartExportCooldown()", ELogType.Error);
            return;
        }
        
        if (!streamer.Export) return;

        try
        {
            streamer.Export = !streamer.Export;
            await Task.Delay(program.ExportCooldown);
            streamer.Export = !streamer.Export;
        }
        catch (TaskCanceledException ce)
        {
            logger.Log(ce.Message,  ELogType.Error);
        }
    }

    /// <summary>
    /// Adds exported vod to ExportHistory to be written to disk.
    /// </summary>
    public bool AddVodToHistory(string username, string id)
    {
        var streamer = GetStreamer(username);

        if (streamer == null)
        {
            logger.Log($"StreamerManager::AddVod:: Streamer is null.", ELogType.Error);
            return false;
        }
        
        //  Should make a Rolling list. Saving only the last 5 exports.
        if (streamer.ExportHistory is { Count: >= 5 })  streamer.ExportHistory.RemoveAt(0);

        streamer.ExportTimes ??= [];
        streamer.ExportHistory?.Add(id);
        UpdateJsonFile();
        return true;
    }
    
    /// <summary>
    /// Check's if streamer's exports are on cooldown.
    /// </summary>
    /// <returns>True if more hours than <see cref="Program.ThrottleCooldown"/> has passed.</returns>
    private bool HasExportCooldownElapsedForStreamer(string username)
    {

        var streamer = GetStreamer(username);

        if (streamer == null)
        {
            logger.Log($"Streamer not found in Streamer List. Username: {username}. " +
                       $"Function: HasExportCooldownElapsedForStreamer()", ELogType.Error);
            return false;
        }
        
        //  if these are null, then the streamer cant be throttled (unless restarting from a crash).
        if (streamer.LastExportedTimestamp == null || streamer.ThrottledTimestamp == null) return true;

        //  False if Less time than ThrottleCooldown has passed since the ThrottledTimestamp.
        return (DateTime.Now - streamer.ThrottledTimestamp.Value).TotalHours > Program.ThrottleCooldown;
    }
    
    private Dictionary<string, StreamerDto> GetDataFromFile()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "data", "PermStreamerData.json");

        if (!File.Exists(path))
        {
            logger.Log("Path not found for JSON. Function StreamerManager::GetDataFromFile()", ELogType.Error);
            return [];
        }
        
        var json = File.ReadAllText(path);

        var dict = JsonSerializer.Deserialize<Dictionary<string, StreamerDto>>(json);

        if (dict == null)
        {
            logger.Log("JSON Data returned null. Function StreamerManager::GetDataFromFile()", ELogType.Error);
            return [];
        }

        return dict;
    }

    private void WriteToDataFile()
    {
        Dictionary<string, StreamerDto> data = [];

        foreach (var streamer in Streamers)
        {
            var dto = new StreamerDto { 
                                        Username = streamer.Username, 
                                        LastExportedVod = streamer.LastExportedVod,
                                        LastExportedTimestamp = streamer.LastExportedTimestamp,
                                        Id = streamer.Id,
                                        ExportedTimes = streamer.ExportTimes,
                                        Active = streamer.Active,
                                        LimitThrottle = streamer.LimitThrottle,
                                        BlackList = streamer.BlackList,
                                        ExportHistory = streamer.ExportHistory
                                      };
            
            data.Add(streamer.Username, dto);
        }
        
        var json = JsonSerializer.Serialize(data, _options);
        
        var path = Path.Combine(Directory.GetCurrentDirectory(), "data", "PermStreamerData.json");
        
        File.WriteAllText(path, json);
    }
    
    private async Task SetStreamersFromFile(Dictionary<string, StreamerDto> dict)
    {
        
        foreach (var (name, dto) in dict)
        {

            var recentExport = dto.LastExportedTimestamp != null &&
                (DateTime.Now - dto.LastExportedTimestamp.Value).TotalHours < Program.ThrottleCooldown;
            
            Streamers.Add(new StreamerAdt
            {
                Username = name,
                Id = dto.Id,
                LastExportedVod = dto.LastExportedVod,
                LastExportedTimestamp = dto.LastExportedTimestamp,
                AmountExported = recentExport ? 1 : 0,
                ExportTimes = dto.ExportedTimes == null ? dto.ExportedTimes : null,
                Active = dto.Active,
                LimitThrottle = dto.LimitThrottle,
                BlackList = dto.BlackList,
                ExportHistory = dto.ExportHistory
            });
        }
        
        await Task.CompletedTask;
    }
    
}