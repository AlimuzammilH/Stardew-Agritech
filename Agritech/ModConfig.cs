using StardewModdingAPI;

namespace Agritech
{
    /// <summary>Player settings, saved to config.json and editable in-game through Generic Mod Config Menu.</summary>
    public class ModConfig
    {
        /*** Keys ***/
        /// <summary>Opens the contract board.</summary>
        public SButton ContractsKey { get; set; } = SButton.F7;

        /// <summary>Opens the farm data terminal.</summary>
        public SButton TerminalKey { get; set; } = SButton.F8;

        /// <summary>Toggles the sensor heat-map overlay (Hub tier 3+).</summary>
        public SButton OverlayKey { get; set; } = SButton.F9;

        /// <summary>Starts drawing a virtual fence zone: press, click two corners (Hub tier 3+).</summary>
        public SButton FenceKey { get; set; } = SButton.F10;

        /// <summary>Cycles which fence zone collared animals are herded into.</summary>
        public SButton CycleZoneKey { get; set; } = SButton.F11;

        /*** Farm ***/
        /// <summary>Morning alerts when sensors read dry or depleted fields (Hub tier 2+).</summary>
        public bool MorningAlerts { get; set; } = true;

        /// <summary>Buyers visit the farm on their day each season.</summary>
        public bool BuyerVisits { get; set; } = true;

        /// <summary>Tourists come on weekends once the Hub is built.</summary>
        public bool Tourists { get; set; } = true;

        /// <summary>Docked drones are drawn hovering over the Drone Dock.</summary>
        public bool ShowDockedDrones { get; set; } = true;

        /// <summary>With Tractor Mod installed, the tractor burns Synthetic Fuel (or coal).</summary>
        public bool TractorNeedsFuel { get; set; } = true;

        /*** Economy ***/
        /// <summary>Meat and milk have a shelf life; spoiled deliveries pay half.</summary>
        public bool Spoilage { get; set; } = true;

        /// <summary>Agritech skill XP multiplier.</summary>
        public float XpMultiplier { get; set; } = 1f;

        /// <summary>Multiplier on the rewards of newly posted contracts.</summary>
        public float ContractPayMultiplier { get; set; } = 1f;

        /// <summary>Days an animal must have been an adult before the Butcher's Tag works.</summary>
        public int MinAdultDaysToRetire { get; set; } = 28;
    }
}
