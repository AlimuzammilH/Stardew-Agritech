using System;
using System.Linq;
using StardewValley;
using StardewValley.GameData.Machines;
using SObject = StardewValley.Object;

namespace Agritech
{
    /// <summary>Custom machine logic, referenced from Data/Machines through OutputMethod.</summary>
    public static class Machines
    {
        /// <summary>
        /// Grading table: turns a raw fibre, tanned leather or dry-aged Wagyu into a graded luxury item.
        /// Tier = input quality + herd bonus (collared herd health, Hub tier 4 provenance) + skill bonus.
        /// </summary>
        public static Item Grade(SObject machine, Item inputItem, bool probe, MachineItemOutput outputData, Farmer player, out int? overrideMinutesUntilReady)
        {
            overrideMinutesUntilReady = null;
            if (inputItem == null)
                return null;
            Material m = Material.ForInput(inputItem.QualifiedItemId, inputItem.Name);
            if (m == null)
                return null;

            // input quality: 0 normal, 1 silver, 2 gold, 4 iridium -> 0..3 points
            int points = inputItem.Quality switch { 4 => 3, 2 => 2, 1 => 1, _ => 0 };

            // herd bonus from collared animals of the matching species
            int hubTier = FarmData.HubTier();
            int herd = 0;
            int herdSize = 0;
            float avgHealth = 0;
            if (m.HerdTypes.Length > 0)
            {
                var collared = FarmData.AllAnimals()
                    .Where(p => ModEntry.State.Collars.ContainsKey(p.animal.myID.Value)
                             && m.IsHerd(p.animal.type.Value))
                    .Select(p => p.animal).ToList();
                herdSize = collared.Count;
                if (herdSize > 0)
                {
                    avgHealth = (float)collared.Average(FarmData.HealthScore);
                    if (avgHealth >= 60) herd = 1;
                    if (avgHealth >= 85 && hubTier >= 4) herd = 2;
                }
            }
            int skillBonus = Skill.Level() >= 8 ? 1 : 0;
            // fibre also benefits from Cloths & Colors' Sewing skill
            bool fibre = m.Id != "Leather" && m.Id != "Wagyu";
            if (fibre && Integration.SewingLevel() >= 5)
                skillBonus++;
            int score = points + herd + skillBonus;
            int tier = score switch { <= 1 => 1, <= 3 => 2, 4 => 3, _ => 4 };

            // Estate and Couture need certified provenance: a collared herd and a tier-4 Hub (silk is exempt from the herd part)
            if (tier >= 3 && (hubTier < 4 || (m.HerdTypes.Length > 0 && herdSize == 0)))
                tier = 2;

            SObject graded = ItemRegistry.Create<SObject>(m.GradedQualified(tier), 1);
            if (probe)
                return graded;

            // provenance certificate
            string farm = Game1.player.farmName.Value;
            string when = $"{Utility.getSeasonNameFromNumber(Game1.Date.SeasonIndex)} {Game1.Date.DayOfMonth}, Year {Game1.Date.Year}";
            string cert = herdSize > 0
                ? $"{Ids.TierNames[tier]} {m.Name}: {farm} Farm, collared herd of {herdSize} (avg health {avgHealth:0}), graded {when}"
                : $"{Ids.TierNames[tier]} {m.Name}: {farm} Farm, graded {when}";
            graded.modData[ModEntry.ProvenanceKey] = cert;
            if (m.Perishable)
                graded.modData[ModEntry.MadeKey] = (inputItem.modData.TryGetValue(ModEntry.MadeKey, out string made) ? made : Game1.Date.TotalDays.ToString());

            var ledger = ModEntry.State.ProvenanceLedger;
            ledger.Add(cert);
            if (ledger.Count > 40)
                ledger.RemoveAt(0);

            Skill.AddXp(5 + tier * 2);
            BalanceDiary.GradedToday++;
            if (fibre)
                Integration.AddSewingXp(4 + tier * 2);
            return graded;
        }

        /// <summary>Ageing locker / tanning vat output: copies the input's made-day so freshness carries through.</summary>
        public static Item CarryFreshness(SObject machine, Item inputItem, bool probe, MachineItemOutput outputData, Farmer player, out int? overrideMinutesUntilReady)
        {
            overrideMinutesUntilReady = null;
            SObject output = ItemRegistry.Create<SObject>(outputData.ItemId, 1, inputItem?.Quality ?? 0);
            if (!probe)
            {
                // freshness starts when ageing finishes (3 days)
                output.modData[ModEntry.MadeKey] = (Game1.Date.TotalDays + 3).ToString();
                Skill.AddXp(3);
            }
            return output;
        }
    }
}
