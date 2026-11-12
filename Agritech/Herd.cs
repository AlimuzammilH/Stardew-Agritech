using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.FarmAnimals;

namespace Agritech
{
    /// <summary>
    /// The horse herd: horses as barn livestock (bay, black, grey), bought from Marnie, bred through the barn like cattle.
    /// Oat feed and grooming build friendship; a saddle trains an adult, and a trained horse sells for double.
    /// </summary>
    internal static class Herd
    {
        private const string FedKey = "Ali.Agritech/FedDay";
        private const string GroomedKey = "Ali.Agritech/GroomedDay";
        private const string TrainedKey = "Ali.Agritech/Trained";

        private static readonly (string coat, string name, string desc, int price)[] Coats =
        {
            ("Bay", "Bay Horse", "A steady bay horse for the herd. Lives in a barn, breeds like cattle, and a trained one sells for a fortune.", 9000),
            ("Black", "Black Horse", "A glossy black horse for the herd. Lives in a barn, breeds like cattle, and a trained one sells for a fortune.", 11000),
            ("Grey", "Grey Horse", "A dappled grey horse for the herd. Lives in a barn, breeds like cattle, and a trained one sells for a fortune.", 10000)
        };

        public static string Id(string coat) => Ids.HorsePrefix + coat;
        private static string Tex(string coat, string kind) => $"Mods/Ali.Agritech/Animals/Horse{coat}_{kind}";

        public static bool IsHerdHorse(FarmAnimal a) => a?.type.Value?.StartsWith(Ids.HorsePrefix) == true;

        /// <summary>Load the hand-drawn horse sheets. Returns true if the asset was ours.</summary>
        public static bool LoadTexture(AssetRequestedEventArgs e, IModHelper helper)
        {
            foreach (var c in Coats)
            {
                foreach (string kind in new[] { "adult", "baby", "shop" })
                {
                    if (!e.NameWithoutLocale.IsEquivalentTo(Tex(c.coat, kind)))
                        continue;
                    string coat = c.coat;
                    e.LoadFrom(() =>
                    {
                        // built from the game's own horse sprite at load time; no game art ships with the mod
                        Texture2D source = helper.GameContent.Load<Texture2D>("Animals/horse");
                        var src = new Color[HorseArt.SrcWidth * HorseArt.SrcHeight];
                        source.GetData(0, new Rectangle(0, 0, HorseArt.SrcWidth, HorseArt.SrcHeight), src, 0, src.Length);
                        var (pixels, w, h) = HorseArt.Build(src, coat, kind);
                        var tex = new Texture2D(Game1.graphics.GraphicsDevice, w, h);
                        tex.SetData(pixels);
                        return tex;
                    }, AssetLoadPriority.Medium);
                    return true;
                }
            }
            return false;
        }

        public static void EditFarmAnimals(IAssetData asset)
        {
            var data = asset.AsDictionary<string, FarmAnimalData>().Data;
            if (!data.TryGetValue("White Cow", out FarmAnimalData template))
                return;
            foreach (var c in Coats)
            {
                FarmAnimalData d = JsonConvert.DeserializeObject<FarmAnimalData>(JsonConvert.SerializeObject(template));
                d.DisplayName = c.name;
                d.ShopDisplayName = c.name;
                d.ShopDescription = c.desc;
                d.PurchasePrice = c.price;
                d.SellPrice = c.price / 2;
                d.AlternatePurchaseTypes = null;
                d.UnlockCondition = "Ali.Agritech_SKILL 2";
                d.House = "Barn";
                d.ProduceItemIds = new List<FarmAnimalProduce>();
                d.DeluxeProduceItemIds = new List<FarmAnimalProduce>();
                d.StatToIncrementOnProduce?.Clear();
                d.CanGetPregnant = true;
                d.Sound = null;
                d.BabySound = null;
                d.Texture = Tex(c.coat, "adult");
                d.BabyTexture = Tex(c.coat, "baby");
                d.HarvestedTexture = null;
                d.ShopTexture = Tex(c.coat, "shop");
                d.ShopSourceRect = new Rectangle(0, 0, 32, 16);
                d.UseFlippedRightForLeft = false;
                d.Skins = null;
                d.DaysToMature = 7;
                data[Id(c.coat)] = d;
            }
        }

        /*********
        ** Care and training
        *********/
        private static bool Today(FarmAnimal a, string key) => a.modData.TryGetValue(key, out string v) && v == Game1.Date.TotalDays.ToString();
        public static bool IsTrained(FarmAnimal a) => a.modData.ContainsKey(TrainedKey);

        private static void Consume() => Game1.player.reduceActiveItemByOne();

        public static void Feed(FarmAnimal a)
        {
            if (Today(a, FedKey))
            {
                Game1.addHUDMessage(new HUDMessage($"{a.displayName} has already had oats today.", HUDMessage.error_type));
                return;
            }
            a.modData[FedKey] = Game1.Date.TotalDays.ToString();
            a.friendshipTowardFarmer.Value = System.Math.Min(1000, a.friendshipTowardFarmer.Value + 30);
            a.fullness.Value = 255;
            a.happiness.Value = 255;
            a.doEmote(Character.heartEmote);
            Game1.playSound("eat");
            Consume();
            Skill.AddXp(2);
        }

        public static void Groom(FarmAnimal a)
        {
            if (Today(a, GroomedKey))
            {
                Game1.addHUDMessage(new HUDMessage($"{a.displayName} has already been groomed today.", HUDMessage.error_type));
                return;
            }
            a.modData[GroomedKey] = Game1.Date.TotalDays.ToString();
            a.friendshipTowardFarmer.Value = System.Math.Min(1000, a.friendshipTowardFarmer.Value + 20);
            a.happiness.Value = 255;
            a.doEmote(Character.happyEmote);
            Game1.playSound("dwop");
            Skill.AddXp(3);
            Game1.addHUDMessage(new HUDMessage($"{a.displayName} groomed. Friendship {a.friendshipTowardFarmer.Value / 200}/5 hearts.", HUDMessage.newQuest_type));
        }

        public static void SaddleUp(FarmAnimal a)
        {
            if (IsTrained(a)) { Game1.addHUDMessage(new HUDMessage($"{a.displayName} is already trained.", HUDMessage.error_type)); return; }
            if (!a.isAdult()) { Game1.addHUDMessage(new HUDMessage($"{a.displayName} is too young for a saddle.", HUDMessage.error_type)); return; }
            if (a.friendshipTowardFarmer.Value < 600) { Game1.addHUDMessage(new HUDMessage($"{a.displayName} won't take a saddle yet. Needs 3 hearts.", HUDMessage.error_type)); return; }
            a.modData[TrainedKey] = Game1.Date.TotalDays.ToString();
            Consume();
            a.doEmote(Character.exclamationEmote);
            Game1.playSound("achievement");
            Skill.AddXp(25);
            Game1.addHUDMessage(new HUDMessage($"{a.displayName} is saddle-trained. Trained horses sell for double.", HUDMessage.achievement_type));
        }

        /// <summary>Handle a click with oat feed, a brush or a saddle on a herd horse. Returns true if handled.</summary>
        public static bool TryInteract(FarmAnimal a, string held)
        {
            if (!IsHerdHorse(a))
                return false;
            if (held == Ids.O(Ids.OatFeed)) { Feed(a); return true; }
            if (held == Ids.O(Ids.Brush)) { Groom(a); return true; }
            if (held == Ids.O(Ids.Saddle)) { SaddleUp(a); return true; }
            return false;
        }

        public static IEnumerable<FarmAnimal> AllHerd() => FarmData.AllAnimals().Select(p => p.animal).Where(IsHerdHorse);

        /*********
        ** Sale price
        *********/
        public static void Patch(string modId)
        {
            var harmony = new Harmony(modId + ".herd");
            harmony.Patch(AccessTools.Method(typeof(FarmAnimal), nameof(FarmAnimal.getSellPrice)),
                postfix: new HarmonyMethod(typeof(Herd), nameof(SellPricePostfix)));
        }

        private static void SellPricePostfix(FarmAnimal __instance, ref int __result)
        {
            if (IsHerdHorse(__instance) && IsTrained(__instance))
                __result *= 2;
        }
    }
}
