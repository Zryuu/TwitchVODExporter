using System.Text.Json;
using Microsoft.Playwright;
using VODExporter.Twitch;

namespace VODExporter.Exporter
{
    public class Browser(Program program, StreamerManager sm, Logger logger)
    {
        // Set in LaunchBrowser()
        public IBrowserContext _context = null!;
        public IPlaywright _playwright = null!;

        /// <summary>
        /// Launches Browser.
        /// </summary>
        public async Task LaunchBrowser()
        {
            logger.Log("Launching Browser...");
            
            var userData = Path.Combine(Directory.GetCurrentDirectory(), "data", "GoogleChromePortable",
                                                                                         "Data", "profile");
            var browserPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "GoogleChromePortable", 
                                                                                    "App", "Chrome-bin", "chrome.exe");
            
            _playwright = await Playwright.CreateAsync();
            
            _context = await _playwright.Chromium.LaunchPersistentContextAsync(
                userDataDir: userData,
                new BrowserTypeLaunchPersistentContextOptions
                {
                    Headless = true,
                    ExecutablePath = browserPath
                });


            if (await Login())  logger.Log("Completed", ELogType.Ok);
        }
        
        //  Might be able to swap this string param for a StreamerAdt and skip getting the StreamerAdt via SM.
        /// <summary>
        /// Processes the given streamer's VOD Queue.
        /// </summary>
        /// <param name="username">Twitch username.</param>
        public async Task ProcessVodQueue(string username)
        {

            // Get Streamer from Username
            var streamer = sm.GetStreamer(username);

            if (streamer == null)
            {
                logger.Log($"Given Streamer not found in list. Streamer: {username}. Skipped processing Queue."
                            , ELogType.Error);
                return;
            }

            if (!streamer.Active)
            {
                logger.Log($"{streamer.Username} exports are inactive. Skipping....");
                return;
            }
            
            //  Export VOD in streamer's Queue. Break when either empty or Limit is reached.
            while (streamer.VodQueue.HasVod() && !sm.IsStreamerThrottled(username))
            {
                var vodData = streamer.VodQueue.GetNextVod();
                if (vodData == null) break;
                
                if (streamer.BlackList != null && streamer.BlackList.Contains(vodData.Id))
                {
                    logger.Log($"Video found in streamer's blacklist. Skipping {vodData.Id}.");
                    continue;
                }
                
                await ExportVod(_context, vodData!.Username, vodData.Id);
                await sm.StartExportCooldown(streamer.Username);
                
                //  Updates LastExportedVod, ExportedAmount, and LastExportedTime. (Streamer can't be null here)
                streamer.LastExportedVod = vodData.Id;

                if (vodData.Duration >= TimeSpan.FromHours(12))
                {
                    sm.IncrementAmountExportedForStreamer(vodData.Username, 2);
                }
                else
                {
                    sm.IncrementAmountExportedForStreamer(vodData.Username);
                }
                
                sm.AddExportedTime(streamer.Username, DateTime.Now);
                if (vodData.UpdateJson) sm.UpdateJsonFile();
            }
            
            logger.Log($"Queue Process for {username} has finished.");
        }

        /// <summary>
        /// Checks if user is Logged into Twitch.
        /// </summary>
        /// <returns>True if Logged in. false otherwise.</returns>
        private async Task<bool> Login()
        {
            var page = await _context.NewPageAsync();
            
            await page.GotoAsync("https://dashboard.twitch.tv/u/rivalsonetrickbot/home");

            //  Checks if any access denied element appears. This means we're signed out of the account.
            if (await page.Locator("p.CoreText-sc-1txzju1-0.ScTitleText-sc-d9mj2s-0.ivranM.bzDGwQ.tw-title")
                    .CountAsync() <= 0) return true;
            
            logger.Log("Not logged into Twitch account. Please manually login via included Browser.", ELogType.Error);
            return false;
        }
        
        private async Task<bool> ExportVod(IBrowserContext context, string username, string vodId)
        {
            if (!await OpenExportPage(context, username, vodId))
            {
                logger.Log($"Failed to open export page.", ELogType.Error);
                return false;
            }
            
            var page = context.Pages[0];

            await page.ClickAsync("div[data-a-target='tw-core-button-label-text']:has-text('Export')");

            try
            {
                await page.WaitForSelectorAsync("div.export-youtube-modal", 
                    new PageWaitForSelectorOptions { Timeout = 15000 });
            }
            catch (TimeoutException)
            {
                logger.Log($"Export to Youtube vid not appearing. " +
                           $"{username} might not have Youtube Connected.", ELogType.Error);
                return false;
            }

            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Start Export" }).ClickAsync();
            sm.AddVodToHistory(username, vodId);
            return true;
        }
        
        private async Task<bool> OpenExportPage(IBrowserContext context, string streamer, string vodId)
        {
         
            var page = context.Pages[0];
            
            await page.GotoAsync($"https://dashboard.twitch.tv/u/{streamer}/content/video-producer/edit/{vodId}");

            if (!page.Locator("text=Access Denied").IsVisibleAsync().Result) return true;
            
            logger.Log($"Account not set as editor for {streamer}.", ELogType.Error);
            return false;
        }
    }
}
