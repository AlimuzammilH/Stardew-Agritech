using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;

namespace Agritech
{
    /// <summary>
    /// The Drone Dock: drones stored in a chest next to the dock fly every morning (Hub tier 3+), powered by
    /// battery packs or synthetic fuel in the same chest.
    ///  Survey   - extends sensor coverage to every tilled tile on the farm.
    ///  Spray    - fertilises depleted plots (fertiliser from the chest) and waters dry ones in sensor coverage.
    ///  Herding  - enables virtual fence herding.
    ///  Delivery - ships any open contract the chest can fill, by drone, free, arriving tomorrow.
    /// </summary>
    internal static class Drones
    {
        public static readonly string[] Types = { Ids.SurveyDrone, Ids.SprayDrone, Ids.HerdingDrone, Ids.DeliveryDrone };

        private class Dock
        {
            public GameLocation Location;
            public Vector2 Tile;
            public Chest Chest;
        }

        private static List<Dock> FindDocks()
        {
            var docks = new List<Dock>();
            Utility.ForEachLocation(loc =>
            {
                foreach (var pair in loc.objects.Pairs)
                {
                    if (pair.Value?.QualifiedItemId != Ids.BC(Ids.DroneDock))
                        continue;
                    Chest chest = null;
                    foreach (Vector2 t in FarmData.TilesAround(pair.Key, 1))
                        if (loc.objects.TryGetValue(t, out SObject o) && o is Chest c && c.playerChest.Value)
                        {
                            chest = c;
                            break;
                        }
                    docks.Add(new Dock { Location = loc, Tile = pair.Key, Chest = chest });
                }
                return true;
            });
            return docks;
        }

        public static bool Has(string droneId)
            => FarmData.HubTier() >= 3 && FindDocks().Any(d => d.Chest != null && d.Chest.Items.Any(i => i?.QualifiedItemId == Ids.O(droneId)));

        public static bool HerdingActive() => Has(Ids.HerdingDrone);
        public static bool SurveyActive() => Has(Ids.SurveyDrone);

        /// <summary>Morning flights.</summary>
        public static void MorningRun(SaveModel save)
        {
            save.DroneActionsToday = 0;
            save.DroneXpToday = 0;
            if (FarmData.HubTier() < 3)
                return;

            foreach (Dock dock in FindDocks().Where(d => d.Chest != null))
            {
                var items = dock.Chest.Items;
                bool spray = items.Any(i => i?.QualifiedItemId == Ids.O(Ids.SprayDrone));
                bool delivery = items.Any(i => i?.QualifiedItemId == Ids.O(Ids.DeliveryDrone));
                int energy = 0;

                bool Spend()
                {
                    if (energy <= 0)
                    {
                        if (items.ReduceId(Ids.VBattery, 1) > 0) energy += 20;
                        else if (items.ReduceId(Ids.O(Ids.Fuel), 1) > 0) energy += 10;
                        else return false;
                    }
                    energy--;
                    save.DroneActionsToday++;
                    return true;
                }

                if (spray)
                {
                    foreach (var (loc, tile) in TilesToTend(save))
                    {
                        if (!loc.terrainFeatures.TryGetValue(tile, out TerrainFeature tf) || tf is not HoeDirt dirt)
                            continue;
                        float n = FarmData.GetNutrients(save, loc, tile);
                        if (n < 0.5f && string.IsNullOrEmpty(dirt.fertilizer.Value))
                        {
                            string fert = Ids.Fertilisers.FirstOrDefault(f => items.Any(i => i?.QualifiedItemId == f));
                            if (fert != null && dirt.CanApplyFertilizer(fert) && Spend())
                            {
                                dirt.fertilizer.Value = fert;
                                items.ReduceId(fert, 1);
                                save.Soil[FarmData.SoilKey(loc, tile)] = Math.Min(1f, n + 0.3f);
                            }
                        }
                        if (dirt.state.Value != HoeDirt.watered && !(Game1.IsRainingHere(loc) && loc.IsOutdoors) && Spend())
                            dirt.state.Value = HoeDirt.watered;
                    }
                }

                if (delivery)
                {
                    foreach (Contract c in save.Contracts.Where(c => c.Status == "Open").ToList())
                    {
                        if (ContractBook.CountMatching(items, c) >= c.Quantity && Spend())
                        {
                            ContractBook.Ship(c, items, "Drone", chargeFuel: false);
                            Game1.addHUDMessage(new HUDMessage($"Delivery drone shipped {ContractBook.Describe(c)} to {Buyer.Get(c.BuyerId)?.Name}.", HUDMessage.newQuest_type));
                        }
                    }
                }

                if (energy == 0 && !items.Any(i => i?.QualifiedItemId == Ids.VBattery || i?.QualifiedItemId == Ids.O(Ids.Fuel)))
                    Game1.addHUDMessage(new HUDMessage("Drone dock is out of power. Add battery packs or synthetic fuel to its chest.", HUDMessage.error_type));
            }

            int xp = Math.Min(50, save.DroneActionsToday / 2);
            save.DroneXpToday = xp;
            Skill.AddXp(xp);
        }

        /// <summary>Tiles the spray drones look after: sensor coverage, or every tilled tile on the farm with a survey drone.</summary>
        private static IEnumerable<(GameLocation, Vector2)> TilesToTend(SaveModel save)
        {
            var seen = new HashSet<string>();
            if (SurveyActive())
            {
                Farm farm = Game1.getFarm();
                foreach (var pair in farm.terrainFeatures.Pairs)
                    if (pair.Value is HoeDirt && seen.Add(FarmData.SoilKey(farm, pair.Key)))
                        yield return (farm, pair.Key);
            }
            int radius = FarmData.SensorRadius(FarmData.HubTier());
            foreach (var (loc, post) in FarmData.SensorPosts())
                foreach (Vector2 t in FarmData.TilesAround(post, radius))
                    if (seen.Add(FarmData.SoilKey(loc, t)))
                        yield return (loc, t);
        }

        /*********
        ** Drawing: drones hover over their dock
        *********/
        private static List<Dock> drawCache = new();
        private static double drawCacheTime = -1;
        private static int drawCacheTier;

        public static void Draw(SpriteBatch b, Texture2D items)
        {
            if (items == null)
                return;
            double time = Game1.currentGameTime.TotalGameTime.TotalSeconds;
            if (time - drawCacheTime > 0.5 || time < drawCacheTime)
            {
                drawCacheTime = time;
                drawCacheTier = FarmData.HubTier();
                drawCache = drawCacheTier >= 3 ? FindDocks() : new List<Dock>();
            }
            foreach (Dock dock in drawCache)
            {
                if (dock.Location != Game1.currentLocation || dock.Chest == null)
                    continue;
                int n = 0;
                foreach (string type in Types)
                {
                    if (!dock.Chest.Items.Any(i => i?.QualifiedItemId == Ids.O(type)))
                        continue;
                    int index = 13 + Array.IndexOf(Types, type);
                    double phase = time * 1.4 + n * Math.PI / 2;
                    Vector2 world = dock.Tile * 64f + new Vector2(16 + (float)Math.Cos(phase) * 70, -40 + (float)Math.Sin(phase * 2) * 14 - 30);
                    Vector2 screen = Game1.GlobalToLocal(Game1.viewport, world);
                    var src = new Rectangle(index % 8 * 16, index / 8 * 16, 16, 16);
                    b.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, dock.Tile * 64f + new Vector2(32 + (float)Math.Cos(phase) * 70, 48)),
                        Game1.shadowTexture.Bounds, Color.White * 0.4f, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 2f, SpriteEffects.None, 0.9f);
                    b.Draw(items, screen, src, Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 0.95f);
                    n++;
                }
            }
        }
    }
}
