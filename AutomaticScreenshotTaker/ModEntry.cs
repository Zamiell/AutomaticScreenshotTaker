using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AutomaticScreenshotTaker
{
    public class ModEntry : Mod
    {
        // Configuration
        private const SButton CustomHotkeyButton = SButton.V;
        private const int RangeToNodes = 25;

        // Constants
        private const int DiamondParentSheetIndex = 2;
        private const int MysticStoneParentSheetIndex = 46;
        private static readonly int[] NodeTypesToCheck = { DiamondParentSheetIndex, MysticStoneParentSheetIndex };
        private static readonly string[] VoidEnemyNames = { "Shadow Brute", "Shadow Shaman" };

        // Variables
        private bool IsAutomating = false;

        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Player.Warped += this.OnWarped;
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
            {
                return;
            }

            if (e.Button == CustomHotkeyButton)
            {
                CustomHotkeyPressed();
            }
        }

        private void OnWarped(object sender, WarpedEventArgs e)
        {
            if (!Context.IsWorldReady)
            {
                return;
            }

            string areaName = e.NewLocation.Name;
            Log($"Loaded area: {areaName}");

            Log($"Game time interval: {Game1.gameTimeInterval}");

            if (areaName == "Mine") // The top floor of the mine
            {
               if (IsAutomating)
                {
                    GoToNextFloor();
                    return;
                }

                PauseGame();
                return;
            }

            if (areaName.StartsWith("Underground") && !IsOnEmptyElevatorFloor())
            {
                PauseGame();

                if (IsAutomating)
                {
                    if (ShouldStopForNPC() || ShouldStopForNode())
                    {
                        IsAutomating = false;
                        TakeScreenshot();
                    }
                    else
                    {
                        GoToNextFloor();
                    }
                }
                else
                {
                    TakeScreenshot();
                }
            }
        }

        private bool ShouldStopForNPC()
        {
            return CountVoidEnemiesInRange(RangeToNodes) >= 2;
        }

        private bool ShouldStopForNode()
        {
            foreach (int nodeType in NodeTypesToCheck)
            {
                if (IsNodeTypeInRange(nodeType, RangeToNodes))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsNodeTypeInRange(int parentSheetIndex, int range)
        {
            foreach (StardewValley.Object node in Game1.currentLocation.objects.Values)
            {
                if (node.Name == "Stone" && node.ParentSheetIndex == parentSheetIndex && IsNodeInRange(node, range))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsNodeInRange(StardewValley.Object node, int range)
        {
            int nodeX = (int)Math.Floor(node.TileLocation.X);
            int nodeY = (int)Math.Floor(node.TileLocation.Y);
            var nodePoint = new Point(nodeX, nodeY);

            return IsInRange(nodePoint, range);
        }

        private int CountVoidEnemiesInRange(int range)
        {
            int count = 0;

            foreach (string name in VoidEnemyNames)
            {
                count += CountEnemiesInRange(name, range);
            }

            return count;
        }

        private int CountEnemiesInRange(string name, int range)
        {
            int count = 0;

            foreach (StardewValley.NPC npc in Game1.currentLocation.characters)
            {
                if (npc.Name == name && IsNPCInRange(npc, range))
                {
                    count++;
                }
            }

            return count;
        }

        private bool IsNPCInRange(StardewValley.NPC npc, int range)
        {
            int npcX = npc.getTileX();
            int npcY = npc.getTileY();
            var npcPoint = new Point(npcX, npcY);

            return IsInRange(npcPoint, range);
        }

        private void CustomHotkeyPressed()
        {
            Log("Custom hotkey pressed.");

            if (IsAutomating)
            {
                Log("Already automating; doing nothing.");
                return;
            }

            if (!ElevatorMenuIsOpen() && !PlayerStandingNextToElevator())
            {
                Log("Not in the elevator menu and not standing next to the elevator; doing nothing.");
                return;
            }

            IsAutomating = true;
            GoToNextFloor();
        }

        private bool ElevatorMenuIsOpen()
        {
            if (Game1.activeClickableMenu == null)
            {
                return false;
            }

            return Game1.activeClickableMenu is StardewValley.Menus.MineElevatorMenu;
        }

        private bool PlayerStandingNextToElevator()
        {
            if (!(Game1.currentLocation is StardewValley.Locations.MineShaft))
            {
                return false;
            }

            Vector2 tileBeneathElevator = this.Helper.Reflection.GetProperty<Vector2>(Game1.currentLocation, "tileBeneathElevator").GetValue();
            int tileX = (int)Math.Floor(tileBeneathElevator.X);
            int tileY = (int)Math.Floor(tileBeneathElevator.Y);

            return IsPlayerOnTile(tileX, tileY);
        }

        private bool IsPlayerOnTile(int tileX, int tileY)
        {
            int playerX = Game1.player.getTileX();
            int playerY = Game1.player.getTileY();

            return playerX == tileX && playerY == tileY;
        }

        private void GoToNextFloor()
        {
            int nextFloorNum = GetNextFloorNum();
            if (nextFloorNum == 0)
            {
                // Mostly copied from "MineElevatorMenu.cs"
                Game1.warpFarmer("Mine", 17, 4, flip: true);
                StopScreenFade();
                Game1.changeMusicTrack("none");
                Game1.exitActiveMenu();
            }
            else
            {
                // Mostly copied from "MineElevatorMenu.cs"
                Game1.player.ridingMineElevator = true;
                Game1.enterMine(nextFloorNum);
                StopScreenFade();
                Game1.exitActiveMenu();
            }
        }

        private int GetNextFloorNum()
        {
            int? minesFloor = GetMineFloorNum();

            if (minesFloor == 0)
            {
                return 85;
            }
            else if (minesFloor == 85)
            {
                return 95;
            }
            else if (minesFloor == 95)
            {
                return 105;
            }

            return 0;
        }

        private bool IsOnEmptyElevatorFloor()
        {
            int? mineFloorNum = GetMineFloorNum();
            if (mineFloorNum == null)
            {
                return false;
            }

            return mineFloorNum % 10 == 0;
        }

        private int? GetMineFloorNum()
        {
            string areaName = Game1.currentLocation.Name;

            if (areaName == "Mine")
            {
                return 0;
            }

            if (!areaName.StartsWith("UndergroundMine"))
            {
                return null;
            }

            Regex regex = new Regex(@"\d+");
            Match match = regex.Match(areaName);
            if (!match.Success)
            {
                return null;
            }

            var floorNumString = match.Value;
            return Int32.Parse(floorNumString);
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

        private void StopScreenFade()
        {
            var screenFade = this.Helper.Reflection.GetField<StardewValley.BellsAndWhistles.ScreenFade>(typeof(Game1), "screenFade").GetValue();
            screenFade.fadeToBlackAlpha = 1f;
        }

        private void Log(string msg)
        {
            this.Monitor.Log(msg, LogLevel.Debug);
        }

        private bool IsInRange(Point point, int range)
        {
            int playerX = Game1.player.getTileX();
            int playerY = Game1.player.getTileY();
            var playerPoint = new Point(playerX, playerY);

            var distanceBetweenPlayerAndThing = GetBFSDistance(playerPoint, point);

            Log($"Player tile: {playerX}, {playerY}");
            Log($"NPC tile: {point.X}, {point.Y}");
            Log($"DISTANCE: {distanceBetweenPlayerAndThing}");

            return distanceBetweenPlayerAndThing <= range;
        }

        private int GetManhattanDistance(int x1, int y1, int x2, int y2)
        {
            return Math.Abs(x1 - x2) + Math.Abs(y1 - y2);
        }

        private int GetBFSDistance(Point start, Point finish)
        {
            var open = new Queue<(Point node, int distance)>();
            open.Enqueue((start, 0));

            var closed = new HashSet<Point>();
            while (open.TryDequeue(out var currentTuple))
            {
                var (currentNode, distance) = currentTuple;

                // Check if node is already visited
                if (!closed.Add(currentNode))
                {
                    continue;
                }

                // Check if we've reached the target
                if (currentNode.Equals(finish))
                {
                    return distance;
                }

                // Visit each of this node's neighbors
                foreach (var neighbor in GetNeighbors(currentNode))
                {
                    open.Enqueue((neighbor, distance + 1));
                }
            }

            throw new("No path found");
        }

        private static Point[] neighborOffsets = new Point[]
        {
            new Point(0, -1), // Top
            new Point(-1, 0), // Left
            new Point(1, 0), // Right
            new Point(0, 1), // Bottom
        };

        private IEnumerable<Point> GetNeighbors(Point node)
        {
            return neighborOffsets
                .Select(offset => offset + node)
                .Where(neighbor => {
                    var tileLocation = new xTile.Dimensions.Location(neighbor.X, neighbor.Y);

                    // "isTilePassable()" returns false for tiles that are outside of the world,
                    // but true for tiles that are covered by breakable stones/objects, which is what we want
                    return Game1.currentLocation.isTilePassable(tileLocation, Game1.viewport);
                });
        }
    }
}
