using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StardewModdingAPI;
using StardewValley;

namespace Agritech
{
    /// <summary>
    /// Writes one line per in-game day to Mods/Agritech/balance/&lt;save&gt;.csv while you play normally:
    /// skill progress, where the XP came from, contract and tour income next to shipping-bin income.
    /// It's what the balance check reads; it changes nothing in the game.
    /// </summary>
    internal static class BalanceDiary
    {
        private static readonly Dictionary<string, int> XpToday = new();
        public static int ContractIncome;
        public static int TourIncome;
        public static int GradedToday;

        public static void RecordXp(int amount, string file, string member)
        {
            string source = $"{Path.GetFileNameWithoutExtension(file)}.{member}";
            XpToday[source] = XpToday.TryGetValue(source, out int v) ? v + amount : amount;
        }

        public static void WriteDay(IModHelper helper)
        {
            try
            {
                if (SelfTest.Running)
                    return;
                var bin = Game1.getFarm()?.getShippingBin(Game1.player);
                int binValue = bin?.Where(i => i != null).Sum(i => Utility.getSellToStorePriceOfItem(i)) ?? 0;
                var collared = ModEntry.State.Collars.Count;
                int horses = Herd.AllHerd().Count();
                int open = ModEntry.State.Contracts.Count(c => c.Status == "Open");
                string xp = string.Join(";", XpToday.OrderByDescending(p => p.Value).Select(p => $"{p.Key}:{p.Value}"));

                string dir = Path.Combine(helper.DirectoryPath, "balance");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, $"{Constants.SaveFolderName}.csv");
                if (!File.Exists(path))
                    File.WriteAllText(path, "date,totalDays,skillLevel,skillXp,xpToday,xpBySource,contractIncome,tourIncome,shippingBinValue,money,hubTier,collared,herdHorses,openContracts,gradedToday\n");
                string date = $"Y{Game1.year} {Game1.currentSeason} {Game1.dayOfMonth}";
                File.AppendAllText(path, string.Join(",", date, Game1.Date.TotalDays, Skill.Level(), ModEntry.State.SkillXp, XpToday.Values.Sum(),
                    $"\"{xp}\"", ContractIncome, TourIncome, binValue, Game1.player.Money, FarmData.HubTier(), collared, horses, open, GradedToday) + "\n");
            }
            catch
            {
                // the diary must never get in the way of the night
            }
            finally
            {
                XpToday.Clear();
                ContractIncome = TourIncome = GradedToday = 0;
            }
        }
    }
}
