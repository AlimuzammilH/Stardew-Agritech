using StardewModdingAPI;

namespace Agritech
{
    /// <summary>Player settings, saved to config.json.</summary>
    public class ModConfig
    {
        /// <summary>Opens the farm data terminal.</summary>
        public SButton TerminalKey { get; set; } = SButton.F8;

        /// <summary>Toggles the sensor heat-map overlay (Hub tier 3+).</summary>
        public SButton OverlayKey { get; set; } = SButton.F9;

        /// <summary>Starts drawing a virtual fence zone: press, click two corners (Hub tier 3+).</summary>
        public SButton FenceKey { get; set; } = SButton.F10;

        /// <summary>Cycles which fence zone collared animals are herded into.</summary>
        public SButton CycleZoneKey { get; set; } = SButton.F11;

        /// <summary>Opens the contract board.</summary>
        public SButton ContractsKey { get; set; } = SButton.F7;

        /// <summary>Morning alerts when sensors read dry or depleted fields (Hub tier 2+).</summary>
        public bool MorningAlerts { get; set; } = true;
    }
}
