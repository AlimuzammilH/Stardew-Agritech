using System;
using StardewModdingAPI;

namespace Agritech
{
    /// <summary>The part of Generic Mod Config Menu's API that Agritech uses.</summary>
    public interface IGenericModConfigMenuApi
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
        void AddSectionTitle(IManifest mod, Func<string> text, Func<string> tooltip = null);
        void AddParagraph(IManifest mod, Func<string> text);
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string> tooltip = null, string fieldId = null);
        void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string> tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string> formatValue = null, string fieldId = null);
        void AddNumberOption(IManifest mod, Func<float> getValue, Action<float> setValue, Func<string> name, Func<string> tooltip = null, float? min = null, float? max = null, float? interval = null, Func<float, string> formatValue = null, string fieldId = null);
        void AddKeybind(IManifest mod, Func<SButton> getValue, Action<SButton> setValue, Func<string> name, Func<string> tooltip = null, string fieldId = null);
    }

    /// <summary>Agritech's page in the in-game mod options menu (needs Generic Mod Config Menu; config.json works without it).</summary>
    internal static class ConfigMenu
    {
        public static void Register(IModHelper helper, IManifest manifest, IMonitor monitor)
        {
            if (!helper.ModRegistry.IsLoaded("spacechase0.GenericModConfigMenu"))
                return;
            IGenericModConfigMenuApi gmcm;
            try { gmcm = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu"); }
            catch (Exception ex)
            {
                monitor.Log($"Couldn't add the options page to Generic Mod Config Menu: {ex.Message}", LogLevel.Warn);
                return;
            }
            if (gmcm == null)
                return;

            ModConfig C() => ModEntry.Config;
            gmcm.Register(manifest,
                reset: () => ModEntry.SetConfig(new ModConfig()),
                save: () => helper.WriteConfig(ModEntry.Config));

            gmcm.AddParagraph(manifest, () => "Agritech adds a farm-technology layer: the Hub and its skill, sensors and collars, breeds, grading, contracts, freight, drones and the horse herd. These settings apply right away.");

            gmcm.AddSectionTitle(manifest, () => "Keys");
            gmcm.AddKeybind(manifest, () => C().ContractsKey, v => C().ContractsKey = v, () => "Contract board", () => "Opens the contract board (also: click the Freight Depot).");
            gmcm.AddKeybind(manifest, () => C().TerminalKey, v => C().TerminalKey = v, () => "Farm terminal", () => "Opens the farm data terminal (also: click the Hub).");
            gmcm.AddKeybind(manifest, () => C().OverlayKey, v => C().OverlayKey = v, () => "Soil overlay", () => "Toggles the sensor heat-map overlay. Needs Hub tier 3.");
            gmcm.AddKeybind(manifest, () => C().FenceKey, v => C().FenceKey = v, () => "Draw fence zone", () => "Press, then click two corners to draw a virtual fence zone. Needs Hub tier 3.");
            gmcm.AddKeybind(manifest, () => C().CycleZoneKey, v => C().CycleZoneKey = v, () => "Cycle fence zone", () => "Switches which zone collared animals are herded into.");

            gmcm.AddSectionTitle(manifest, () => "Farm");
            gmcm.AddBoolOption(manifest, () => C().MorningAlerts, v => C().MorningAlerts = v, () => "Morning alerts", () => "Morning messages when sensors read dry or depleted fields. Needs Hub tier 2.");
            gmcm.AddBoolOption(manifest, () => C().BuyerVisits, v => C().BuyerVisits = v, () => "Buyer visits", () => "Fashion and restaurant buyers visit the farm on their day each season.");
            gmcm.AddBoolOption(manifest, () => C().Tourists, v => C().Tourists = v, () => "Weekend tourists", () => "Tourists tour the farm on weekends once the Hub is built.");
            gmcm.AddBoolOption(manifest, () => C().ShowDockedDrones, v => C().ShowDockedDrones = v, () => "Show docked drones", () => "Draws drones hovering over a stocked Drone Dock. They work either way.");
            gmcm.AddBoolOption(manifest, () => C().TractorNeedsFuel, v => C().TractorNeedsFuel = v, () => "Tractor needs fuel", () => "With Tractor Mod installed, the tractor burns Synthetic Fuel, or coal as a fallback.");

            gmcm.AddSectionTitle(manifest, () => "Economy");
            gmcm.AddBoolOption(manifest, () => C().Spoilage, v => C().Spoilage = v, () => "Spoilage", () => "Meat and milk have a shelf life and spoiled deliveries pay half. Off: nothing spoils.");
            gmcm.AddNumberOption(manifest, () => C().XpMultiplier, v => C().XpMultiplier = v, () => "Skill XP speed", () => "How fast the Agritech skill levels. 1.0 is the intended pace.", 0.5f, 3f, 0.1f, v => $"{v:0.0}x");
            gmcm.AddNumberOption(manifest, () => C().ContractPayMultiplier, v => C().ContractPayMultiplier = v, () => "Contract pay", () => "Multiplies the reward of newly posted contracts. 1.0 is the intended pay.", 0.5f, 2f, 0.1f, v => $"{v:0.0}x");
            gmcm.AddNumberOption(manifest, () => C().MinAdultDaysToRetire, v => C().MinAdultDaysToRetire = v, () => "Adult days before retiring", () => "How long an animal must have been an adult before the Butcher's Tag works. 28 is one season.", 0, 56, 1, v => v == 0 ? "no wait" : $"{v} days");
        }
    }
}
