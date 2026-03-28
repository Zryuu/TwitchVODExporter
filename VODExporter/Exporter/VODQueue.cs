using System.Text.RegularExpressions;

namespace VODExporter.Exporter;

public class VodQueue
{
    public LinkedList<Vod> VodList = [];
    public readonly object QueueLock = new();

    public bool RemoveVod(string username, string id)
    {
        lock (QueueLock)
        {
            var vod = VodList.FirstOrDefault(v => v.Id == id && v.Username == username);

            if (vod != null)
            {
                VodList.Remove(vod);
                return true;
            }
            
            return false;
        }
    }
    
    /// <summary>
    /// Adds new VOD to the queue. Doesn't allow duplicate VODs to be added.
    /// </summary>
    /// <param name="username">Twitch username</param>
    /// <param name="id">VOD's ID</param>
    /// <param name="duration">VOD's Duration</param>
    /// <param name="updateJson">If it should update the Json file or not. (Default is true)</param>
    public void AddVod(string username, string id, string duration, bool updateJson = true)
    {
        lock (QueueLock)
        {
            if (VodList.FirstOrDefault(v => v.Id == id) != null) return;
            
            VodList.AddLast(new Vod
            {
                Username = username,
                Id = id,
                UpdateJson = updateJson,
                Duration = ParseVideoDuration(duration),
            });
        }
    }

    /// <summary>
    /// Adds a new VOD to the Queue after the given previous VOD. If previous VOD isnt found, appends on the end.
    /// </summary>
    /// <param name="username">Twitch username</param>
    /// <param name="id">VOD's ID</param>
    /// <param name="duration">VOD's Duration</param>
    /// <param name="prevVodId">ID of the VOD the given VOD will be placed after.</param>
    /// <param name="updateJson">If it should update the Json file or not. (Default is true)</param>
    public bool AddVodAfter(string username, string id, string duration, string prevVodId, bool updateJson = true)
    {
        lock (QueueLock)
        {
            // Check if Vod already exists
            if (VodList.Any(v => v.Id == id))
                return false;

            // Find the previous node
            var prevVod = VodList.FirstOrDefault(v => v.Id == prevVodId);
            if (prevVod != null)
            {
                var prevNode = VodList.Find(prevVod); // Get the LinkedListNode
                if (prevNode != null)
                {
                    // Add new Vod after prevNode
                    VodList.AddAfter(prevNode, new Vod
                    {
                        Username = username,
                        Id = id,
                        UpdateJson = updateJson,
                        Duration = ParseVideoDuration(duration),
                    });
                }
                return true;
            }
            
            VodList.AddLast(new Vod
            {
                Username = username,
                Id = id,
                UpdateJson = updateJson,
                Duration = ParseVideoDuration(duration),
            });
            return true;
        }
    }
    
    /// <summary>
    /// Dequeues the next VOD.
    /// </summary>
    /// <returns><see cref="Vod"/> object, unless Queue is empty. Than, a null object is returned.</returns>
    public Vod? GetNextVod()
    {
        lock (QueueLock)
        {
            if (VodList.First != null)
            {
                var vod = VodList.First.Value;
                VodList.RemoveFirst();
                return vod;
            }
            return null;
        }
    }
    
    /// <summary>
    /// Does the Queue have any VODs.
    /// </summary>
    /// <returns>True if any VODs are in the Queue, Otherwise False.</returns>
    public bool HasVod()
    {
        
        lock (QueueLock)
        {
            return VodList.Count > 0;
        }
    }
    
    // TODO: Prob need to move this to StreamerManager or TwitchApi, that way it can actually use Logger.
    public TimeSpan ParseVideoDuration(string duration)
    {
        var match = Regex.Match(duration, @"(?:(\d+)h)?(?:(\d+)m)?(?:(\d+)s)?");
        
        if (!match.Success) 
        {
            //  Where the throw was that needs to use Logger instead. Related to the TODO above.
            Console.WriteLine($"VODQueue::ParseVideoDuration:: could not parse video duration: {duration}");
            return new TimeSpan(15, 0, 0);
        }
        
        var hours = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
        var minutes = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
        var seconds = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
        
        return new TimeSpan(hours, minutes, seconds);
    }
}