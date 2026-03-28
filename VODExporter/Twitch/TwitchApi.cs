

using System.Text.RegularExpressions;
using TwitchLib.Api.Helix.Models.Videos.GetVideos;

namespace VODExporter.Twitch
{
    
    /// <summary>
    /// This class handles all events related to TwitchAPI
    /// </summary>
    public class TwitchApi(Logger logger)
    {
        private static TwitchLib.Api.TwitchAPI? _api;

        private readonly string _userId = Environment.GetEnvironmentVariable("USER_ID");
        private readonly string _token = Environment.GetEnvironmentVariable("TOKEN");asd
        
        /// <summary>
        /// Connects to twitch API.
        /// </summary>
        public async Task ConnectToTwitch()
        {
            logger.Log("Connecting to Twitch...");
            _api = new TwitchLib.Api.TwitchAPI { Settings = { ClientId = _userId, AccessToken = _token } };
            logger.Log("Connected", ELogType.Ok);
        }
        
        /// <summary>
        /// Gets given streamer's ID from Twitch API.
        /// </summary>
        /// <param name="streamer">Username of the Streamer.</param>
        /// <returns>String of the ID, otherwise null.</returns>
        public async Task<string?> GetStreamerId(string streamer)
        {

            var api = ValidatedGetApi();
            
            try
            {

                var users = api.Helix.Users.GetUsersAsync(logins: [streamer]).Result.Users;

                if (users[0] != null) return users[0].Id;
                
                logger.Log($"No streamers found with username {streamer} in API.", ELogType.Error);
                return null;
            }
            catch (Exception e)
            {
                logger.Log($"Helix Exception:{e.Message}", ELogType.Error);
                throw;
            }
        }
        
        /// <summary>
        /// Check's if the Streamer is liva via the API.
        /// </summary>
        /// <param name="streamer">StreamerADT reference</param>
        /// <returns>True if live, False otherwise.</returns>
        public async Task<bool> CheckIfLiveFromApi(StreamerAdt streamer)
        {

            var api = ValidatedGetApi();
            
            try
            {
                var stream = api.Helix.Streams.GetStreamsAsync(userLogins: [streamer.Username])
                    .Result.Streams;
                
                return stream.Length > 0;
            }
            catch (Exception ex)
            {
                logger.Log($"Unable to reach Twitch API | {ex.Message}", ELogType.Error);
                return false;
            }
        }

        /// <summary>
        /// Get VOD objects from twitch API.
        /// </summary>
        /// <param name="streamer">Twitch username</param>
        /// <param name="amount">Amount of videos to get from API.</param>
        /// <returns>Array of <see cref="Video"/>'s. Nullable</returns>
        public async Task<Video[]?> GetStreamersVideos(StreamerAdt streamer, int amount)
        {
            
            var api = ValidatedGetApi();
            
            try
            {
                var stream = api.Helix.Videos.GetVideosAsync(userId: streamer.Id, first: amount).Result.Videos;

                if (stream == null)
                {
                    logger.Log($"No video was gotten from API for {streamer.Username}.", ELogType.Error);
                }
                
                return stream;
            }
            catch (Exception ex)
            {
                logger.Log($"Unable to reach Twitch API | {ex.Message}", ELogType.Error);
                return null;
            }
        }

        /// <summary>
        /// Takes the Duration Stream from TwitchAPI and Parses it into TimeSpan.
        /// </summary>
        /// <param name="duration">Duration string from TwitchAPI.</param>
        /// <param name="timeSpan">The Amount of time the VOD is parsed to be.</param>
        /// <returns>Video's Duration in <see cref="TimeSpan"/> formate.</returns>
        public bool ParseVideoDuration(string duration, out TimeSpan timeSpan)
        {
            timeSpan = TimeSpan.Zero;

            if (string.IsNullOrWhiteSpace(duration))
                return false;

            var match = Regex.Match(duration, @"(?:(\d+)h)?(?:(\d+)m)?(?:(\d+)s)?");

            if (!match.Success)
                return false;

            var hours = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
            var minutes = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
            var seconds = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;

            timeSpan = new TimeSpan(hours, minutes, seconds);
            return true;
        }

        /// <summary>
        /// Gets a single Video's info.
        /// </summary>
        /// <param name="streamer">Streamer who owns the video</param>
        /// <param name="videoId">VOd's ID</param>
        /// <returns></returns>
        public Task<Video?> GetVideoInfo(StreamerAdt streamer, string videoId)
        {
            var api = ValidatedGetApi();

            var videos = api.Helix.Videos.GetVideosAsync(videoIds: [ videoId ]).Result.Videos;

            if (videos == null)
            {
                logger.Log($"No videos found for {streamer.Username} with ID {videoId}.", ELogType.Error);
                return Task.FromResult<Video?>(null);
            }
            
            return Task.FromResult(videos.FirstOrDefault());
        }

        /// <summary>
        /// Gets all VODs since Last recorded exported VOD.
        /// </summary>
        /// <param name="streamer">Twitch username</param>
        /// <returns>VOD ID's since last Exported. Capped at 100 ID's</returns>
        public async Task<List<Video>> FindAllStreamsSinceLastExported(StreamerAdt streamer)
        {

            if (streamer.LastExportedVod == null)
            {
                var videos = await GetStreamersVideos(streamer, 1);
                return videos!.ToList();
            }
            
            var amountToGet = 20;
            var result = new List<Video>();
            var lastExportedIndex = 0;
            
            while (amountToGet < 100)
            {
                var videos = await GetStreamersVideos(streamer, amountToGet);

                if (videos == null)
                {
                    logger.Log($"Videos object is null for streamer {streamer.Username}. " +
                               $"Function FindAllStreamsSinceLastExported()", ELogType.Error);
                    return [];
                }

                result.AddRange(videos);
                lastExportedIndex = result.FindIndex(v => v.Id == streamer.LastExportedVod);

                if (lastExportedIndex == -1)
                {
                    amountToGet += 20;
                    result.Clear();
                    continue;
                }
                
                break;
            }

            if (lastExportedIndex > 0)
                result = result.Take(lastExportedIndex + 1).ToList();
            
            return result;
        }

        /// <summary>
        /// A UnrealEngine valid get style method. Used to get a non-nullable <see cref="TwitchLib.Api.TwitchAPI"/>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        private TwitchLib.Api.TwitchAPI ValidatedGetApi()
        {

            if (_api == null)
            {
                logger.Log("TwitchAPI Object not valid.", ELogType.Error);
                throw new NullReferenceException("[Error]: TwitchAPI::ValidatedGetAPI(): TwitchAPI Object not valid.");
            }
            
            return _api;
        }
    }
}