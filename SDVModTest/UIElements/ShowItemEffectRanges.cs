using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using System;
using System.Collections.Generic;
using System.Threading;

namespace UIInfoSuite.UIElements
{
    class ShowItemEffectRanges : IDisposable
    {
        private readonly List<Point> _effectiveArea = new List<Point>();
        private readonly IModEvents _events;

        private readonly Mutex _mutex = new Mutex();

        public ShowItemEffectRanges(IModEvents events)
        {
            _events = events;
        }

        public void ToggleOption(bool showItemEffectRanges)
        {
            _events.Display.Rendered -= OnRendered;
            _events.GameLoop.UpdateTicked -= OnUpdateTicked;

            if (showItemEffectRanges)
            {
                _events.Display.Rendered += OnRendered;
                _events.GameLoop.UpdateTicked += OnUpdateTicked;
            }
        }

        public void Dispose()
        {
            ToggleOption(false);
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!e.IsMultipleOf(4))
                return;

            if (_mutex.WaitOne())
            {
                try
                {
                    // check draw tile outlines
                    _effectiveArea.Clear();
                }
                finally
                {
                    _mutex.ReleaseMutex();
                }

            }
            if (Game1.activeClickableMenu == null &&
                        !Game1.eventUp)
            {
                if (Game1.currentLocation is BuildableGameLocation buildableLocation)
                {
                    Building building = buildableLocation.getBuildingAt(Game1.currentCursorTile);

                    if (building is JunimoHut)
                    {
                        foreach (var nextBuilding in buildableLocation.buildings)
                        {
                            if (nextBuilding is JunimoHut nextHut)
                                ParseConfigToHighlightedArea(GetObjectDistanceArray(ObjectWithDistance.JunimoHut), nextHut.tileX.Value + 1, nextHut.tileY.Value + 1);
                        }
                    }
                }

                if (Game1.player.CurrentItem is Item currentItem)
                {
                    String name = currentItem.Name.ToLower();
                    List<StardewValley.Object> objects = null;

                    int[][] arrayToUse = null;

                    if (name.Contains("arecrow") && !name.Contains("sprinkler") )
                    {
                        ObjectWithDistance distanceObj = name.Contains("eluxe") ? ObjectWithDistance.DeluxeScarecrow : ObjectWithDistance.Scarecrow;
                        ParseConfigToHighlightedArea(GetObjectDistanceArray(distanceObj), TileUnderMouseX, TileUnderMouseY);

                        objects = GetObjectsInLocationOfSimilarName("arecrow");
                        foreach (StardewValley.Object next in objects)
                        {
                            distanceObj = next.name.Contains("eluxe") ? ObjectWithDistance.DeluxeScarecrow : ObjectWithDistance.Scarecrow;
                            ParseConfigToHighlightedArea(GetObjectDistanceArray(distanceObj), (int)next.TileLocation.X, (int)next.TileLocation.Y);
                        }
                    }
                    else if (name.Contains("sprinkler"))
                    {
                        if (name.Contains("iridium"))
                        {
                            arrayToUse = GetObjectDistanceArray(ObjectWithDistance.IridiumSprinkler);
                        }
                        else if (name.Contains("quality"))
                        {
                            arrayToUse = GetObjectDistanceArray(ObjectWithDistance.QualitySprinkler);
                        }
                        else if (name.Contains("prismatic"))
                        {
                            arrayToUse = GetObjectDistanceArray(ObjectWithDistance.PrismaticSprinkler);
                        }
                        else
                        {
                            arrayToUse = GetObjectDistanceArray(ObjectWithDistance.Sprinkler);
                        }

                        if (arrayToUse != null)
                            ParseConfigToHighlightedArea(arrayToUse, TileUnderMouseX, TileUnderMouseY);

                        HighlightNearbySprinklers();
                    }
                    else if (name.Contains("bee house"))
                    {
                        ParseConfigToHighlightedArea(GetObjectDistanceArray(ObjectWithDistance.Beehouse), TileUnderMouseX, TileUnderMouseY);
                    }
                    else if (name.Contains("nozzle"))
                    {
                        if (Game1.currentLocation == null)
                        {
                            return;
                        }

                        StardewValley.Object _currentCursorObj;
                        Game1.currentLocation.Objects.TryGetValue(Game1.currentCursorTile, out _currentCursorObj);
                        if (_currentCursorObj != null)
                        {
                            String hoverName = _currentCursorObj.Name.ToLower();
                            if (hoverName.Contains("sprinkler"))
                            {
                                if (hoverName.Contains("iridium"))
                                {
                                    arrayToUse = GetObjectDistanceArray(ObjectWithDistance.IridiumSprinkler, true);
                                }
                                else if (hoverName.Contains("quality"))
                                {
                                    arrayToUse = GetObjectDistanceArray(ObjectWithDistance.QualitySprinkler, true);
                                }
                                else if (hoverName.Contains("prismatic"))
                                {
                                    arrayToUse = GetObjectDistanceArray(ObjectWithDistance.PrismaticSprinkler, true);
                                }
                                else
                                {
                                    arrayToUse = GetObjectDistanceArray(ObjectWithDistance.Sprinkler, true);
                                }

                                if (arrayToUse != null)
                                    ParseConfigToHighlightedArea(arrayToUse, TileUnderMouseX, TileUnderMouseY);
                            }
                        }

                        HighlightNearbySprinklers(_currentCursorObj);
                    }
                }
            }

        }

        /// <summary>Raised after the game draws to the sprite patch in a draw tick, just before the final sprite batch is rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRendered(object sender, RenderedEventArgs e)
        {
            if (_mutex.WaitOne(0))
            {
                try
                {
                    // draw tile outlines
                    foreach (Point point in _effectiveArea)
                    {
                        var position = new Vector2(point.X * Utility.ModifyCoordinateFromUIScale(Game1.tileSize), point.Y * Utility.ModifyCoordinateFromUIScale(Game1.tileSize));
                        Game1.spriteBatch.Draw(
                            Game1.mouseCursors,
                            Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(Utility.ModifyCoordinatesForUIScale(position))),
                            new Rectangle(194, 388, 16, 16),
                            Color.White * 0.7f,
                            0.0f,
                            Vector2.Zero,
                            Utility.ModifyCoordinateForUIScale(Game1.pixelZoom),
                            SpriteEffects.None,
                            0.01f);
                    }
                }
                finally
                {
                    _mutex.ReleaseMutex();
                }
            }
        }

        private void ParseConfigToHighlightedArea(int[][] highlightedLocation, int xPos, int yPos)
        {
            if (highlightedLocation == null)
            {
                return;
            }

            int xOffset = highlightedLocation.Length / 2;

            if (_mutex.WaitOne())
            {
                try
                {
                    for (int i = 0; i < highlightedLocation.Length; ++i)
                    {
                        int yOffset = highlightedLocation[i].Length / 2;
                        for (int j = 0; j < highlightedLocation[i].Length; ++j)
                        {
                            if (highlightedLocation[i][j] == 1)
                                _effectiveArea.Add(new Point(xPos + i - xOffset, yPos + j - yOffset));
                        }
                    }
                }
                finally
                {
                    _mutex.ReleaseMutex();
                }
            }
        }

        private int TileUnderMouseX
        {
            get { return (Game1.getMouseX() + Game1.viewport.X) / Game1.tileSize; }
        }

        private int TileUnderMouseY
        {
            get { return (Game1.getMouseY() + Game1.viewport.Y) / Game1.tileSize; }
        }

        private List<StardewValley.Object> GetObjectsInLocationOfSimilarName(String nameContains)
        {
            List<StardewValley.Object> result = new List<StardewValley.Object>();

            if (!String.IsNullOrEmpty(nameContains))
            {
                nameContains = nameContains.ToLower();
                var objects = Game1.currentLocation.Objects;

                foreach (var nextThing in objects.Values)
                {
                    if (nextThing.name.ToLower().Contains(nameContains))
                        result.Add(nextThing);
                }
            }
            return result;
        }
        
        private enum ObjectWithDistance
        {
            JunimoHut,
            Beehouse,
            Scarecrow,
            DeluxeScarecrow,
            Sprinkler,
            QualitySprinkler,
            IridiumSprinkler,
            PrismaticSprinkler
        }

        private int[][] GetObjectDistanceArray(ObjectWithDistance type, bool hasPressureNozzle = false)
        {
            switch (type)
            {
                case ObjectWithDistance.JunimoHut:
                    int[][] arr = GetCircularMask(8, squareMask: true);
                    // Remove top row of building
                    arr[7][7] = 0;
                    arr[7][8] = 0;
                    arr[7][9] = 0;
                    // Remove tiles left and right of door
                    arr[8][7] = 0;
                    arr[8][9] = 0;
                    return arr;
                case ObjectWithDistance.Beehouse:
                    return GetCircularMask(4.19, inclusiveMax: true);
                case ObjectWithDistance.Scarecrow:
                    return GetCircularMask(8.9);
                case ObjectWithDistance.DeluxeScarecrow:
                    return GetCircularMask(16.9);
                case ObjectWithDistance.Sprinkler:
                    return GetCircularMask(1, squareMask: hasPressureNozzle);
                case ObjectWithDistance.QualitySprinkler:
                    return GetCircularMask(hasPressureNozzle ? 2 : 1, squareMask: true);
                case ObjectWithDistance.IridiumSprinkler:
                    return GetCircularMask(hasPressureNozzle ? 3 : 2, squareMask: true);
                case ObjectWithDistance.PrismaticSprinkler:
                    return GetCircularMask(3, squareMask: true);
                default:
                    return null;
            }
        }

        // Calculates an 2 dimensional int array of 0s and 1s where 1 indicates a covered tile and 0 indicates a non-covered tile
        private static int[][] GetCircularMask(double maxDistance, bool? squareMask = false, bool? inclusiveMax = false)
        {
            int radius = (int)Math.Ceiling(maxDistance);
            int size = 2 * radius + 1;

            int[][] result = new int[size][];
            for (int i = 0; i < size; i++)
            {
                result[i] = new int[size];
                for (int j = 0; j < size; j++)
                {
                    double distance = GetDistance(i, j, radius);
                    int val = 0;

                    if (distance <= maxDistance // Circular Radius check
                        || ((bool)squareMask && j - radius <= maxDistance && i - radius <= maxDistance) // Squared Radius check
                        // inclusiveMax includes a tile that is exactly the radius (Ceiling value of maxDistance) away from the center tile in any of the 4 cardinal directions
                        || ((bool)inclusiveMax && (radius - j == 0 || radius - i == 0)))
                    {
                        val = 1;
                    }

                    result[i][j] = val;
                }
            }

            // The center square isn't covered (where the item is placed)
            result[radius][radius] = 0;
            return result;
        }

        // Calculates the distance from the center tile using Pythagoras's Theorem
        private static double GetDistance(int i, int j, int radius)
        {
            return Math.Sqrt(Math.Pow(radius - i, 2) + Math.Pow(radius - j, 2));
        }

        private void HighlightNearbySprinklers(StardewValley.Object ignoredSprinkler = null)
        {
            List<StardewValley.Object> objects = GetObjectsInLocationOfSimilarName("sprinkler");
            foreach (StardewValley.Object next in objects)
            {
                if (ignoredSprinkler != null && ignoredSprinkler.Equals(next))
                {
                    continue;
                }

                string objectName = next.name.ToLower();
                bool hasPressureNozzle = next.heldObject.Value != null && next.heldObject.Value.DisplayName.Contains("Nozzle");
                int[][] arrayToUse;
                if (objectName.Contains("iridium"))
                {
                    arrayToUse = GetObjectDistanceArray(ObjectWithDistance.IridiumSprinkler, hasPressureNozzle);
                }
                else if (objectName.Contains("quality"))
                {
                    arrayToUse = GetObjectDistanceArray(ObjectWithDistance.QualitySprinkler, hasPressureNozzle);
                }
                else if (objectName.Contains("prismatic"))
                {
                    arrayToUse = GetObjectDistanceArray(ObjectWithDistance.PrismaticSprinkler, hasPressureNozzle);
                }
                else
                {
                    arrayToUse = GetObjectDistanceArray(ObjectWithDistance.Sprinkler, hasPressureNozzle);
                }

                if (arrayToUse != null)
                    ParseConfigToHighlightedArea(arrayToUse, (int)next.TileLocation.X, (int)next.TileLocation.Y);
            }
        }
    }
}
