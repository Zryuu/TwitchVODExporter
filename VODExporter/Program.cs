using System.Text;
using VODExporter.Exporter;
using VODExporter.GUI;
using VODExporter.Twitch;

namespace VODExporter
{
    public class Program
    {
        public readonly TwitchApi TwitchApi;
        private readonly Browser _browser;
        private readonly StreamerManager _streamerManager;
        private readonly Logger _logger;

        private const int UpdateDelay = 60;     // Delay between each UpdateLoop (Minutes).
        public const int ThrottleCooldown = 27; // How many hours to wait before allowing more uploads (when throttled)
        public readonly TimeSpan ExportCooldown = TimeSpan.FromMinutes(1);   // How many minutes to wait in between multiple exports.

        private Program(Logger logger)
        {
            _logger = logger;
            _streamerManager = new StreamerManager(this, logger);
            _browser = new Browser(this, _streamerManager, logger);
            TwitchApi = new TwitchApi(logger);
        }

        /// <summary>
        /// Starts the Program. Init's/Creates all needed objects.
        /// </summary>
        /// <param name="token">Cancellation Token</param>
        public async Task StartUp(CancellationToken token)
        {
            await _streamerManager.InitStreamManager();
            await _browser.LaunchBrowser();
            await TwitchApi.ConnectToTwitch();
            
            await UpdateLoop(token);
        }

        /// <summary>
        /// UpdateLoop. Runs every <see cref="UpdateDelay"/> minute(s). Handles the loop and canceling the loop.
        /// Actual login in <see cref="Program.OnUpdate()"/>
        /// </summary>
        public async Task UpdateLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await OnUpdate();
                
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(UpdateDelay));
                }
                catch (TaskCanceledException ce)
                {
                    _logger.Log(ce.Message, ELogType.Error);
                    break;
                }
            }
        }

        /// <summary>
        /// Ran during UpdateLoop. Handles the actual logic.
        /// </summary>
        public async Task OnUpdate()
        {
            foreach (var streamer in _streamerManager.Streamers)
            {
                await ProcessStreamerAsync(streamer);
            }
        }

        /// <summary>
        /// Handles Updating the streamer's status, and Exporting their vods.
        /// </summary>
        /// <param name="streamer">StreamerAdt of the Streamer.</param>
        private async Task ProcessStreamerAsync(StreamerAdt streamer)
        {
            _streamerManager.RemoveExportedTime(streamer.Username);
            
            var isLive = await TwitchApi.CheckIfLiveFromApi(streamer);
            _streamerManager.UpdateStreamerLiveStatus(streamer.Username, isLive);

            if (streamer.LastExportedVod == null)
                await HandleFirstExportAsync(streamer);
            else
                await HandleSubsequentExportsAsync(streamer);
            
            await _browser.ProcessVodQueue(streamer.Username);
        }

        /// <summary>
        /// Adds the most recent VOD gotten from the TwitchAPI from given Streamer.
        /// Only runs if it's the streamer's first Export.
        /// </summary>
        /// <param name="streamer">StreamerAdt of the Streamer.</param>
        private async Task HandleFirstExportAsync(StreamerAdt streamer)
        {
            if (streamer.IsCurrentlyLive)
            {
                _logger.Log($"{streamer.Username} is live. Waiting till they're offline to preform first export.");
                return;
            }
            var videos = await TwitchApi.GetStreamersVideos(streamer, 1);

            if (videos == null || videos.Length == 0)
            {
                _logger.Log($"No videos found for {streamer.Username} in the API.");
                return;
            }

            var mostRecent = videos[0];
            if (streamer.BlackList?.Any(b => b == mostRecent.Id) == true) return;
            _streamerManager.AddVod(streamer.Username, mostRecent.Id);
        }

        /// <summary>
        /// Gets all VODs since last stream and adds them to the VodQueue of the given Streamer.
        /// </summary>
        /// <param name="streamer">Streamer's StreamerAdt</param>
        private async Task HandleSubsequentExportsAsync(StreamerAdt streamer)
        {
            var videos = await TwitchApi.FindAllStreamsSinceLastExported(streamer);
            if (videos.Count == 0)
            {
                _logger.Log($"No videos found for {streamer.Username} in the API. Skipping");
                return;
            }

            if (videos.Count == 1 && streamer.IsCurrentlyLive)
            {
                _logger.Log($"Skipping {streamer.Username}'s export as current Stream isn't finished.");
                return;
            }

            if (streamer.LastExportedVod == videos[0].Id)
            {
                _logger.Log($"Skipping {streamer.Username}'s export. No new VODs.");
                return;
            }

            var lastExportedIndex = videos.FindIndex(v => v.Id == streamer.LastExportedVod);

            foreach (var video in videos.Take(lastExportedIndex).Reverse())
            {
                if (!TwitchApi.ParseVideoDuration(video.Duration, out var span))
                {
                    _logger.Log($"Program::HandleSubsequentExportsAsync():: Duration couldn't be parsed.", ELogType.Warning);
                }
                else
                {
                    if (span.TotalHours < 1)
                    {
                        _logger.Log($"Removing {video.Id} from {streamer.Username}'s " +
                                    $"list as it's less than an hour long.");
                        continue;
                    }
                }
                
                if (streamer.VodQueue.VodList.Any(v => v.Id == video.Id)) continue;
                if (streamer.BlackList?.Any(b => b == video.Id) == true) continue;
                _streamerManager.AddVod(streamer.Username, video.Id);
            }
        }
        
        public static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
            
            using CancellationTokenSource cts = new();

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };
            
            var logger = new Logger();
            var program = new Program(logger);
            var tui = new Tui(program._streamerManager, logger);

            try
            {
                logger.Log("Starting program...");
                var programTask = program.StartUp(cts.Token);
                var tuiTask = tui.Run(cts.Token);
                
                await Task.WhenAny(programTask, tuiTask);
                
                if (programTask.IsFaulted)
                    logger.Log($"Program crashed: {programTask.Exception?.GetBaseException().Message}", ELogType.Error);

                if (tuiTask.IsFaulted)
                    logger.Log($"TUI crashed: {tuiTask.Exception?.GetBaseException().Message}", ELogType.Error);
            }
            catch (Exception e)
            {
                logger.Log($"Exception: {e.Message}", ELogType.Error);
            }
            finally
            {
                logger.Log($"Shutting down...Press any key to exit.\"");
                Console.ReadKey();
            }
        }
    }
}








