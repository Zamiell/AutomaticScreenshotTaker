using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace AutomaticScreenshoterTaker
{
    public class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            helper.Events.Player.Warped += this.OnWarped;
        }


        private void OnWarped(object sender, WarpedEventArgs e)
        {
            if (!Context.IsWorldReady)
            {
                return;
            }

            var msg = $"Loaded area: {e.NewLocation.Name}.";
            this.Monitor.Log(msg, LogLevel.Debug);

            // The game lags every time we take a screenshot,
            // so we only do it when needed
            if (e.NewLocation.Name.StartsWith("Underground"))
            {
                PauseGame();
                TakeScreenshot();
            }
        }

        private void PauseGame()
        {
            Game1.activeClickableMenu = new StardewValley.Menus.GameMenu();
        }

        private void TakeScreenshot()
        {
            float zoomLevel = 1f;
            string fileName = "current_area";

            string mapScreenshotPath = Game1.game1.takeMapScreenshot(zoomLevel, fileName, null);
        }
    }
}
