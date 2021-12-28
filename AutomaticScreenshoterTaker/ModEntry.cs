using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace AutomaticScreenshoterTaker
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.Player.Warped += this.OnWarped;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnWarped(object sender, WarpedEventArgs e)
        {
            if (!Context.IsWorldReady)
            {
                return;
            }

            var msg = $"Loaded area: {e.NewLocation}.";
            this.Monitor.Log(msg, LogLevel.Debug);
        }

        private void TakeScreenshot()
        {
            float zoomLevel = 1;
            string screenshotPath = "a.png";

            string mapScreenshotPath = Game1.game1.takeMapScreenshot(zoomLevel, screenshotPath, () => {});
        }
    }
}
