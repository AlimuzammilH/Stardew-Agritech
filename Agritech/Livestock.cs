using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Characters;
using StardewValley.GameData.FarmAnimals;
using SObject = StardewValley.Object;

namespace Agritech
{
    /// <summary>New breeds, retiring animals for hides and meat, manure, and horse husbandry.</summary>
    internal static class Livestock
    {
        /// <summary>Breed definitions: new id, vanilla template, display name, produce, price, unlock skill, recolour.</summary>
        internal static readonly (string id, string template, string name, string desc, string produce, int price, int skill, Color tint, float strength)[] Breeds =
        {
            (Ids.Wagyu, "White Cow", "Wagyu", "Black Japanese cattle. Calm, slow-growing, and the finest marbling there is. Retire an adult for Wagyu cuts.", Ids.VMilk, 8000, 3, new Color(38, 30, 30), 0.85f),
            (Ids.Merino, "Sheep", "Merino Sheep", "Fine-fleeced sheep. Sheared Merino wool grades above ordinary wool.", Ids.O(Ids.MerinoWool), 12000, 1, new Color(240, 228, 196), 0.55f),
            (Ids.CashmereGoat, "Goat", "Cashmere Goat", "Combed for its soft undercoat. Use shears to harvest raw cashmere.", Ids.O(Ids.RawCashmere), 10000, 2, new Color(176, 170, 162), 0.6f),
            (Ids.Angora, "Rabbit", "Angora Rabbit", "A cloud of a rabbit. Use shears for raw angora.", Ids.O(Ids.RawAngora), 12000, 2, new Color(250, 246, 240), 0.6f)
        };

        public static void EditFarmAnimals(IAssetData asset)
        {
            var data = asset.AsDictionary<string, FarmAnimalData>().Data;
            foreach (var b in Breeds)
            {
                if (!data.TryGetValue(b.template, out FarmAnimalData template))
                    continue;
                // deep copy through JSON so the vanilla entry stays untouched
                FarmAnimalData d = JsonConvert.DeserializeObject<FarmAnimalData>(JsonConvert.SerializeObject(template));
                d.DisplayName = b.name;
                d.ShopDisplayName = b.name;
                d.ShopDescription = b.desc;
                d.PurchasePrice = b.price;
                d.AlternatePurchaseTypes = null;
                d.UnlockCondition = $"Ali.Agritech_SKILL {b.skill}";
                d.ProduceItemIds = new List<FarmAnimalProduce> { new() { Id = "Default", ItemId = b.produce } };
                d.DeluxeProduceItemIds = new List<FarmAnimalProduce>();
                if (b.id == Ids.CashmereGoat || b.id == Ids.Angora)
                {
                    d.HarvestType = FarmAnimalHarvestType.HarvestWithTool;
                    d.HarvestTool = "Shears";
                    d.DaysToProduce = 4;
                }
                d.ShopTexture = TextureName(b.id, "shop");
                d.ShopSourceRect = new Rectangle(0, 0, template.ShopSourceRect.Width, template.ShopSourceRect.Height);
                d.Texture = TextureName(b.id, "adult");
                d.BabyTexture = template.BabyTexture != null ? TextureName(b.id, "baby") : null;
                d.HarvestedTexture = template.HarvestedTexture != null ? TextureName(b.id, "harvested") : null;
                data[b.id] = d;
            }
        }

        public static string TextureName(string breed, string kind) => $"Mods/Ali.Agritech/Animals/{breed}_{kind}";

        /// <summary>Provide recoloured animal sprite sheets built from the vanilla ones.</summary>
        public static void OnAssetRequested(StardewModdingAPI.Events.AssetRequestedEventArgs e, IModHelper helper)
        {
            foreach (var b in Breeds)
            {
                foreach (string kind in new[] { "adult", "baby", "harvested", "shop" })
                {
                    if (!e.NameWithoutLocale.IsEquivalentTo(TextureName(b.id, kind)))
                        continue;
                    var tint = b.tint;
                    var strength = b.strength;
                    string template = b.template;
                    e.LoadFrom(() =>
                    {
                        var data = Game1.content.Load<Dictionary<string, FarmAnimalData>>("Data/FarmAnimals");
                        FarmAnimalData t = data[template];
                        if (kind == "shop")
                            return Recolour(Crop(helper.GameContent.Load<Texture2D>(t.ShopTexture), t.ShopSourceRect), tint, strength);
                        string source = kind switch { "baby" => t.BabyTexture, "harvested" => t.HarvestedTexture, _ => t.Texture };
                        return Recolour(helper.GameContent.Load<Texture2D>(source), tint, strength);
                    }, StardewModdingAPI.Events.AssetLoadPriority.Medium);
                }
            }
        }

        private static Texture2D Crop(Texture2D source, Rectangle area)
        {
            var pixels = new Color[area.Width * area.Height];
            source.GetData(0, area, pixels, 0, pixels.Length);
            var result = new Texture2D(Game1.graphics.GraphicsDevice, area.Width, area.Height);
            result.SetData(pixels);
            return result;
        }

        /// <summary>Tint a sprite sheet toward a colour, keeping light and shade, and keeping dark outlines and eyes.</summary>
        private static Texture2D Recolour(Texture2D source, Color tint, float strength)
        {
            var pixels = new Color[source.Width * source.Height];
            source.GetData(pixels);
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                if (c.A == 0)
                    continue;
                float lum = (0.299f * c.R + 0.587f * c.G + 0.114f * c.B) / 255f;
                if (lum < 0.18f)
                    continue; // outlines, eyes
                // shade the tint by the pixel's own brightness, relative to a mid-light body
                float shade = Math.Clamp(lum / 0.8f, 0.25f, 1.25f);
                var shaded = new Color(
                    (int)Math.Clamp(tint.R * shade + (tint.R < 80 ? 40 * (shade - 0.6f) : 0), 0, 255),
                    (int)Math.Clamp(tint.G * shade + (tint.G < 80 ? 40 * (shade - 0.6f) : 0), 0, 255),
                    (int)Math.Clamp(tint.B * shade + (tint.B < 80 ? 40 * (shade - 0.6f) : 0), 0, 255));
                Color mixed = Color.Lerp(c, shaded, strength);
                mixed.A = c.A;
                pixels[i] = mixed;
            }
            var result = new Texture2D(Game1.graphics.GraphicsDevice, source.Width, source.Height);
            result.SetData(pixels);
            return result;
        }

        /*********
        ** Retiring an animal
        *********/
        private static readonly string[] Cattle = { "cow", "wagyu", "buffalo", "yak", "bison", "cattle" };
        private static readonly string[] SmallHide = { "goat", "sheep", "merino" };

        public static bool CanRetire(FarmAnimal a, out string why)
        {
            why = null;
            string type = (a.type.Value ?? "").ToLowerInvariant();
            if (!a.isAdult()) { why = $"{a.displayName} isn't an adult yet."; return false; }
            if (!Cattle.Any(type.Contains) && !SmallHide.Any(type.Contains)) { why = $"{a.displayName} can't be retired for hide or meat."; return false; }
            int left = MinAdultDays - AdultDays(a);
            if (left > 0) { why = $"{a.displayName} needs a full season as an adult first ({left} more day{(left == 1 ? "" : "s")})."; return false; }
            return true;
        }

        /// <summary>Days an animal must have been an adult before it can be retired, so stock is raised, not flipped.</summary>
        public static int MinAdultDays => ModEntry.Config?.MinAdultDaysToRetire ?? 28;

        public static int AdultDays(FarmAnimal a)
        {
            int mature = a.GetAnimalData()?.DaysToMature ?? 0;
            return Math.Max(0, a.age.Value - mature);
        }

        public static void AskRetire(FarmAnimal a)
        {
            if (!CanRetire(a, out string why))
            {
                Game1.addHUDMessage(new HUDMessage(why, HUDMessage.error_type));
                return;
            }
            Game1.currentLocation.createQuestionDialogue(
                $"Retire {a.displayName}? This is permanent. You'll get hide and, for cattle, meat.",
                Game1.currentLocation.createYesNoResponses(),
                (who, answer) => { if (answer == "Yes") Retire(a); });
        }

        private static void Retire(FarmAnimal a)
        {
            string type = (a.type.Value ?? "").ToLowerInvariant();
            int health = FarmData.HealthScore(a);
            int hideQuality = health >= 85 ? 2 : health >= 60 ? 1 : 0;
            if (health >= 95 && a.friendshipTowardFarmer.Value >= 900)
                hideQuality = 4;

            var drops = new List<Item>();
            bool cattle = Cattle.Any(type.Contains);
            drops.Add(ItemRegistry.Create(Ids.O(Ids.RawHide), cattle ? 2 : 1, hideQuality));
            if (cattle)
            {
                bool wagyu = type.Contains("wagyu");
                // 2 base, +1 per season as an adult (up to +3), +0-2 for health: 5 after one season, up to 7
                int cuts = 2 + Math.Min(3, AdultDays(a) / 28) + health / 40;
                Item meat = ItemRegistry.Create(Ids.O(wagyu ? Ids.WagyuCut : Ids.BeefCut), cuts, hideQuality);
                meat.modData[ModEntry.MadeKey] = Game1.Date.TotalDays.ToString();
                drops.Add(meat);
            }

            long id = a.myID.Value;
            GameLocation loc = a.currentLocation ?? Game1.currentLocation;
            if (a.home?.GetIndoors() is AnimalHouse house)
                house.animalsThatLiveHere.Remove(id);
            loc.animals.Remove(id);
            ModEntry.State.Collars.Remove(id);

            foreach (Item drop in drops)
                Game1.createItemDebris(drop, a.Position, Game1.player.FacingDirection, loc);
            Game1.playSound("leafrustle");
            Skill.AddXp(15);
        }

        /*********
        ** Manure
        *********/
        public static void SpawnManure(Random rng)
        {
            Utility.ForEachLocation(loc =>
            {
                if (loc is not AnimalHouse house || house.animals.Length == 0)
                    return true;
                int existing = loc.objects.Values.Count(o => o.QualifiedItemId == Ids.O(Ids.Manure));
                int toSpawn = Math.Min(house.animals.Length / 3 + 1, 12 - existing);
                for (int n = 0; n < toSpawn; n++)
                {
                    for (int attempt = 0; attempt < 30; attempt++)
                    {
                        var tile = new Vector2(rng.Next(1, loc.Map.Layers[0].LayerWidth - 1), rng.Next(1, loc.Map.Layers[0].LayerHeight - 1));
                        if (loc.objects.ContainsKey(tile) || !loc.CanItemBePlacedHere(tile))
                            continue;
                        SObject manure = ItemRegistry.Create<SObject>(Ids.O(Ids.Manure));
                        manure.IsSpawnedObject = true;
                        loc.objects.Add(tile, manure);
                        break;
                    }
                }
                return true;
            });
        }

        /*********
        ** Horses
        *********/
        public static IEnumerable<Horse> AllHorses()
        {
            var list = new List<Horse>();
            Utility.ForEachLocation(loc =>
            {
                foreach (NPC c in loc.characters)
                    if (c is Horse h && h.HorseId != Guid.Empty && !IsTractor(h))
                        list.Add(h);
                return true;
            });
            if (Game1.player.mount != null && !IsTractor(Game1.player.mount) && !list.Contains(Game1.player.mount))
                list.Add(Game1.player.mount);
            return list;
        }

        /// <summary>Tractor Mod's tractor is a Horse marked in modData; it isn't livestock.</summary>
        public static bool IsTractor(Horse h) => h?.modData.ContainsKey("Pathoschild.TractorMod") == true;

        public static HorseRecord Record(Horse h)
        {
            string key = h.HorseId.ToString();
            if (!ModEntry.State.Horses.TryGetValue(key, out HorseRecord rec))
                ModEntry.State.Horses[key] = rec = new HorseRecord { HorseId = key };
            rec.Name = h.displayName;
            return rec;
        }

        public static Horse HorseAt(Vector2 pixel)
        {
            foreach (NPC c in Game1.currentLocation.characters)
            {
                if (c is not Horse h || IsTractor(h))
                    continue;
                Rectangle box = h.GetBoundingBox();
                box.Inflate(32, 48);
                if (box.Contains((int)pixel.X, (int)pixel.Y))
                    return h;
            }
            return null;
        }

        public static void Groom(Horse h)
        {
            HorseRecord rec = Record(h);
            if (rec.LastGroomedDay == Game1.Date.TotalDays)
            {
                Game1.addHUDMessage(new HUDMessage($"{rec.Name} has already been groomed today.", HUDMessage.error_type));
                return;
            }
            rec.LastGroomedDay = Game1.Date.TotalDays;
            rec.Bond = Math.Min(100, rec.Bond + 6);
            Game1.playSound("dwop");
            h.doEmote(Character.happyEmote);
            Skill.AddXp(5);
            Game1.addHUDMessage(new HUDMessage($"{rec.Name} groomed. Bond {rec.Bond}/100.", HUDMessage.newQuest_type));
        }

        /// <summary>Training: distance ridden builds a speed bonus while mounted.</summary>
        private static Vector2 lastRidePos;

        public static void TickRiding()
        {
            Horse mount = Game1.player.mount;
            if (mount == null || IsTractor(mount))
            {
                lastRidePos = Vector2.Zero;
                return;
            }
            HorseRecord rec = Record(mount);
            if (lastRidePos != Vector2.Zero)
            {
                float moved = Vector2.Distance(lastRidePos, Game1.player.Position);
                if (moved < 64)
                    rec.TrainingDistance += moved;
            }
            lastRidePos = Game1.player.Position;

            int level = rec.TrainingLevel;
            if (level > 0)
            {
                var effects = new BuffEffects();
                effects.Speed.Value = level * 0.3f;
                Game1.player.buffs.Apply(new Buff("Ali.Agritech_HorseTraining", duration: 1500, effects: effects) { visible = false });
            }
        }

        /// <summary>Nightly: neglected horses lose bond; two bonded horses and a tier-2 Hub produce a foal every 28 days.</summary>
        public static void NightlyHorses()
        {
            var horses = AllHorses().ToList();
            foreach (Horse h in horses)
            {
                HorseRecord rec = Record(h);
                if (Game1.Date.TotalDays - rec.LastGroomedDay > 3)
                    rec.Bond = Math.Max(0, rec.Bond - 1);
            }

            var s = ModEntry.State;
            s.DaysSinceLastFoal++;
            var bonded = horses.Select(Record).Where(r => r.Bond >= 80).ToList();
            if (bonded.Count >= 2 && FarmData.HubTier() >= 2 && s.DaysSinceLastFoal >= 28)
            {
                s.DaysSinceLastFoal = 0;
                s.PendingFoals++;
                bonded[0].Foals++;
                bonded[1].Foals++;
                Game1.addHUDMessage(new HUDMessage($"{bonded[0].Name} and {bonded[1].Name} have a foal! Build a new Stable at Robin's and it moves in.", HUDMessage.achievement_type));
                Skill.AddXp(60);
            }
        }

        /// <summary>When a new stable's horse appears, it's the foal: count it off.</summary>
        public static void ClaimFoals()
        {
            var s = ModEntry.State;
            if (s.PendingFoals <= 0)
                return;
            foreach (Horse h in AllHorses())
            {
                string key = h.HorseId.ToString();
                if (!s.Horses.ContainsKey(key))
                {
                    var rec = Record(h);
                    rec.Bond = 30;
                    s.PendingFoals--;
                    Game1.addHUDMessage(new HUDMessage($"The foal has moved into its stable. Name: {rec.Name}.", HUDMessage.achievement_type));
                    if (s.PendingFoals <= 0)
                        break;
                }
            }
        }
    }
}
