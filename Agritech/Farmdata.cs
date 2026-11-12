using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;

namespace Agritech
{
    /// <summary>A sensor post's reading of the tiles around it.</summary>
    internal class SensorReading
    {
        public GameLocation Location;
        public Vector2 Tile;
        public int Radius;
        public int TilledTiles;
        public int WateredTiles;
        public float AverageNutrients;
        public int LowNutrientTiles;

        public float Moisture => TilledTiles == 0 ? 0f : (float)WateredTiles / TilledTiles;
    }

    /// <summary>Reads the farm: hub tier, sensor posts, soil and collared animals.</summary>
    internal static class FarmData
    {
        public const float LowNutrient = 0.35f;

        /// <summary>The highest completed Agritech Hub tier on the farm, or 0 if none is built.</summary>
        public static int HubTier()
        {
            int tier = 0;
            Farm farm = Game1.getFarm();
            if (farm == null)
                return 0;
            foreach (Building b in farm.buildings)
            {
                string type = b.buildingType.Value;
                if (type == null || !type.StartsWith(ContentRegistry.HubPrefix) || b.isUnderConstruction())
                    continue;
                if (int.TryParse(type.Substring(ContentRegistry.HubPrefix.Length), out int t))
                    tier = Math.Max(tier, t);
            }
            return tier;
        }

        /// <summary>Sensor coverage radius at a given hub tier.</summary>
        public static int SensorRadius(int tier) => tier switch
        {
            <= 1 => 2,
            2 => 3,
            3 => 4,
            _ => 5
        };

        public static string SoilKey(GameLocation loc, Vector2 tile) => $"{loc.NameOrUniqueName}|{(int)tile.X},{(int)tile.Y}";

        public static float GetNutrients(SaveModel save, GameLocation loc, Vector2 tile)
            => save.Soil.TryGetValue(SoilKey(loc, tile), out float v) ? v : 1f;

        /// <summary>All placed sensor posts in every location.</summary>
        public static List<(GameLocation loc, Vector2 tile)> SensorPosts()
        {
            var posts = new List<(GameLocation, Vector2)>();
            Utility.ForEachLocation(loc =>
            {
                foreach (var pair in loc.objects.Pairs)
                {
                    if (pair.Value?.QualifiedItemId == ContentRegistry.SensorPostQualified)
                        posts.Add((loc, pair.Key));
                }
                return true;
            });
            return posts;
        }

        public static SensorReading Read(SaveModel save, GameLocation loc, Vector2 post, int tier)
        {
            var r = new SensorReading { Location = loc, Tile = post, Radius = SensorRadius(tier) };
            float nutrientSum = 0;
            foreach (Vector2 tile in TilesAround(post, r.Radius))
            {
                if (!loc.terrainFeatures.TryGetValue(tile, out TerrainFeature tf) || tf is not HoeDirt dirt)
                    continue;
                r.TilledTiles++;
                if (dirt.state.Value == HoeDirt.watered)
                    r.WateredTiles++;
                float n = GetNutrients(save, loc, tile);
                nutrientSum += n;
                if (n < LowNutrient)
                    r.LowNutrientTiles++;
            }
            r.AverageNutrients = r.TilledTiles == 0 ? 1f : nutrientSum / r.TilledTiles;
            return r;
        }

        public static IEnumerable<Vector2> TilesAround(Vector2 center, int radius)
        {
            for (int x = -radius; x <= radius; x++)
                for (int y = -radius; y <= radius; y++)
                    yield return new Vector2(center.X + x, center.Y + y);
        }

        /// <summary>Overnight soil update: crops draw nutrients down, fertiliser and fallow ground restore them.</summary>
        public static void UpdateSoilOvernight(SaveModel save)
        {
            Utility.ForEachLocation(loc =>
            {
                foreach (var pair in loc.terrainFeatures.Pairs)
                {
                    if (pair.Value is not HoeDirt dirt)
                        continue;
                    string key = SoilKey(loc, pair.Key);
                    float n = save.Soil.TryGetValue(key, out float v) ? v : 1f;

                    bool fertilised = !string.IsNullOrEmpty(dirt.fertilizer.Value);
                    if (dirt.crop != null && !dirt.crop.dead.Value)
                        n -= fertilised ? 0.005f : 0.03f;   // a growing crop feeds on the soil
                    else
                        n += 0.02f;                          // fallow ground recovers
                    if (fertilised)
                        n += 0.02f;

                    n = Math.Clamp(n, 0f, 1f);
                    if (n >= 0.999f)
                        save.Soil.Remove(key);              // keep the save small: absent = full
                    else
                        save.Soil[key] = n;
                }
                return true;
            });
        }

        /// <summary>Every farm animal in every location (fields and inside barns and coops).</summary>
        public static List<(FarmAnimal animal, GameLocation loc)> AllAnimals()
        {
            var list = new List<(FarmAnimal, GameLocation)>();
            Utility.ForEachLocation(loc =>
            {
                foreach (var pair in loc.animals.Pairs)
                    list.Add((pair.Value, loc));
                return true;
            });
            return list;
        }

        /// <summary>Health score 0-100 from the animal's happiness and fullness.</summary>
        public static int HealthScore(FarmAnimal a)
        {
            float happy = Math.Clamp(a.happiness.Value / 255f, 0f, 1f);
            float full = Math.Clamp(a.fullness.Value / 255f, 0f, 1f);
            return (int)Math.Round((happy * 0.6f + full * 0.4f) * 100f);
        }

        /// <summary>Simulated 21-day heat cycle; true on the two fertile days.</summary>
        public static bool InHeat(FarmAnimal a)
        {
            if (!a.isAdult())
                return false;
            long phase = (Game1.Date.TotalDays + Math.Abs(a.myID.Value % 21)) % 21;
            return phase < 2;
        }

        public static int DaysToHeat(FarmAnimal a)
        {
            long phase = (Game1.Date.TotalDays + Math.Abs(a.myID.Value % 21)) % 21;
            return phase < 2 ? 0 : (int)(21 - phase);
        }

        /// <summary>Products per week at the animal's logged rate.</summary>
        public static float WeeklyForecast(CollarRecord rec)
            => rec.DaysTracked == 0 ? 0f : rec.ProductsLogged * 7f / rec.DaysTracked;
    }
}
