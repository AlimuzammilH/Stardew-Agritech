using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Machines;

namespace Agritech
{
    /// <summary>SpaceCore's API, the part we use to read and feed Cloths & Colors' Sewing skill.</summary>
    public interface ISpaceCoreApi
    {
        string[] GetCustomSkills();
        int GetLevelForCustomSkill(Farmer farmer, string skill);
        void AddExperienceForCustomSkill(Farmer farmer, string skill, int amt);
    }

    /// <summary>Links to Cloths & Colors: its machines take our fibres, and its Sewing skill and our grading feed each other.</summary>
    internal static class Integration
    {
        private const string CC = "selph.TextileExpansion";
        private const string Spinner = "(BC)" + CC + "_YarnSpinner";
        private const string Loom = "(BC)17";

        private static ISpaceCoreApi spaceCore;
        private static string sewingSkill;

        public static void Init(IModHelper helper, IMonitor monitor)
        {
            if (!helper.ModRegistry.IsLoaded("spacechase0.SpaceCore"))
                return;
            try
            {
                spaceCore = helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
                sewingSkill = spaceCore?.GetCustomSkills()?.FirstOrDefault(s => s.Contains("Textile") || s.Contains("Sewing") || s.StartsWith("selph"));
                if (sewingSkill != null)
                    monitor.Log($"Linked to Cloths & Colors' skill '{sewingSkill}'.", LogLevel.Trace);
            }
            catch (System.Exception ex)
            {
                monitor.Log($"Couldn't link to SpaceCore: {ex.Message}", LogLevel.Trace);
                spaceCore = null;
            }
        }

        public static int SewingLevel()
        {
            try { return spaceCore != null && sewingSkill != null ? spaceCore.GetLevelForCustomSkill(Game1.player, sewingSkill) : 0; }
            catch { return 0; }
        }

        public static void AddSewingXp(int amount)
        {
            try { if (spaceCore != null && sewingSkill != null) spaceCore.AddExperienceForCustomSkill(Game1.player, sewingSkill, amount); }
            catch { /* optional link */ }
        }

        /// <summary>
        /// After Cloths & Colors has added its rules: copy its cashmere rule (spinner and loom) for our raw cashmere and raw angora,
        /// and its wool spinner rule for Merino, so our breeds' fibre spins into yarn and weaves into cloth like theirs.
        /// </summary>
        public static void EditMachinesLate(IAssetData asset)
        {
            var data = asset.AsDictionary<string, MachineData>().Data;
            CloneRule(data, Spinner, CC + "_Cashmere", Ids.RawCashmere, 1.0f);
            CloneRule(data, Spinner, CC + "_Cashmere", Ids.RawAngora, 0.8f);
            CloneRule(data, Spinner, CC + "_Wool", Ids.MerinoWool, 1.4f);
            CloneRule(data, Loom, CC + "_Cashmere", Ids.RawCashmere, 1.0f);
            CloneRule(data, Loom, CC + "_Cashmere", Ids.RawAngora, 0.8f);
            CloneRule(data, Loom, CC + "_Cashmere", Ids.MerinoWool, 0.7f);
        }

        private static void CloneRule(IDictionary<string, MachineData> data, string machine, string sourceRuleId, string ourItem, float priceFactor)
        {
            if (!data.TryGetValue(machine, out MachineData m) || m.OutputRules == null)
                return;
            MachineOutputRule source = m.OutputRules.FirstOrDefault(r => r.Id == sourceRuleId);
            string newId = $"{Ids.P}{ourItem}";
            if (source == null || m.OutputRules.Any(r => r.Id == newId))
                return;

            MachineOutputRule rule = JsonConvert.DeserializeObject<MachineOutputRule>(JsonConvert.SerializeObject(source));
            rule.Id = newId;
            foreach (var trigger in rule.Triggers)
            {
                trigger.RequiredItemId = Ids.O(ourItem);
                trigger.RequiredTags = null;
            }
            foreach (var output in rule.OutputItem)
            {
                if (output.PriceModifiers == null)
                    continue;
                foreach (var mod in output.PriceModifiers)
                    mod.Amount = (int)(mod.Amount * priceFactor);
            }
            // insert before generic catch-all rules
            m.OutputRules.Insert(0, rule);
        }
    }
}
