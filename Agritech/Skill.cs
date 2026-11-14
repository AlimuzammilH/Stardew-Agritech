using System;
using System.Linq;
using StardewValley;
using StardewValley.Delegates;

namespace Agritech
{
    /// <summary>The Agritech skill: levels 0-10, earned by running the tech layer. Gates Hub tiers, machines and buildings.</summary>
    internal static class Skill
    {
        private static readonly int[] Thresholds = { 100, 380, 770, 1300, 2150, 3300, 4100, 4900, 5800, 6800 };

        /// <summary>Recipes unlocked at each level (crafting recipe keys).</summary>
        public static readonly (int level, string recipe)[] RecipeUnlocks =
        {
            (0, "Sensor Post"), (0, "Smart Collar"), (0, "Butcher's Tag"), (0, "Grooming Brush"),
            (2, "Grading Table"),
            (2, "Oat Feed"),
            (3, "Tanning Vat"), (3, "Ageing Locker"), (3, "Drying Rack"), (3, "Saddle"),
            (4, "Cold Store"),
            (5, "Fischer-Tropsch Unit"), (5, "Fuel Cell"),
            (6, "Drone Dock"), (6, "Survey Drone"), (6, "Spray Drone"), (6, "Herding Drone"), (6, "Delivery Drone")
        };

        /// <summary>Cooking recipes unlocked at each level.</summary>
        public static readonly (int level, string recipe)[] CookingUnlocks =
        {
            (3, "Wagyu Steak"), (3, "Beef Stew"), (5, "Wagyu Tasting")
        };

        public static int Level(int xp)
        {
            int level = 0;
            while (level < Thresholds.Length && xp >= Thresholds[level])
                level++;
            return level;
        }

        public static int Level() => Level(ModEntry.State.SkillXp);

        public static int XpForNext(int level) => level >= Thresholds.Length ? 0 : Thresholds[level];

        public static void AddXp(int amount,
            [System.Runtime.CompilerServices.CallerFilePath] string file = "",
            [System.Runtime.CompilerServices.CallerMemberName] string member = "")
        {
            if (amount <= 0)
                return;
            amount = Math.Max(1, (int)Math.Round(amount * ModEntry.Config.XpMultiplier));
            BalanceDiary.RecordXp(amount, file, member);
            int before = Level();
            ModEntry.State.SkillXp += amount;
            int after = Level();
            if (after > before)
            {
                Game1.playSound("achievement");
                Game1.addHUDMessage(new HUDMessage($"Agritech skill level {after}!", HUDMessage.achievement_type));
                GrantRecipes();
                foreach (var (lvl, recipe) in RecipeUnlocks.Concat(CookingUnlocks))
                    if (lvl > before && lvl <= after)
                        Game1.addHUDMessage(new HUDMessage($"New recipe: {recipe}", HUDMessage.newQuest_type));
                if (after == 3 || after == 6 || after == 9)
                    Game1.addHUDMessage(new HUDMessage($"Robin can now upgrade your Agritech Hub to tier {after / 3 + 1}.", HUDMessage.newQuest_type));
                if (after == 4)
                    Game1.addHUDMessage(new HUDMessage("Robin can now build a Freight Depot.", HUDMessage.newQuest_type));
            }
        }

        public static void GrantRecipes()
        {
            int level = Level();
            foreach (var (lvl, recipe) in RecipeUnlocks)
            {
                if (lvl <= level && !Game1.player.craftingRecipes.ContainsKey(recipe))
                    Game1.player.craftingRecipes.Add(recipe, 0);
            }
            foreach (var (lvl, recipe) in CookingUnlocks)
            {
                if (lvl <= level && !Game1.player.cookingRecipes.ContainsKey(recipe))
                    Game1.player.cookingRecipes.Add(recipe, 0);
            }
        }

        /// <summary>Game state query "Ali.Agritech_SKILL &lt;level&gt;": true if the skill is at least that level.</summary>
        public static bool SkillQuery(string[] query, GameStateQueryContext context)
        {
            if (!ArgUtility.TryGetInt(query, 1, out int needed, out string error))
                return GameStateQuery.Helpers.ErrorResult(query, error);
            return Level() >= needed;
        }
    }
}
