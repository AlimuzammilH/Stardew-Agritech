using System.Collections.Generic;
using System.Linq;

namespace Agritech
{
    /// <summary>Every item, machine, building and animal id the mod defines.</summary>
    internal static class Ids
    {
        public const string P = "Ali.Agritech_";

        // big craftables (sprite index in bigcraftables.png)
        public const string SensorPost = P + "SensorPost";      // 0
        public const string GradingTable = P + "GradingTable";  // 1
        public const string TanningVat = P + "TanningVat";      // 2
        public const string AgeingLocker = P + "AgeingLocker";  // 3
        public const string FtUnit = P + "FTUnit";              // 4
        public const string DroneDock = P + "DroneDock";        // 5
        public const string ColdStore = P + "ColdStore";        // 6
        public const string DryingRack = P + "DryingRack";      // 7 (sensor post tier looks: 8-11)

        // objects (sprite index in items.png)
        public const string Collar = P + "Collar";              // 0
        public const string MerinoWool = P + "MerinoWool";      // 1
        public const string RawCashmere = P + "RawCashmere";    // 2
        public const string RawAngora = P + "RawAngora";        // 3
        public const string RawHide = P + "RawHide";            // 4
        public const string Leather = P + "TannedLeather";      // 5
        public const string WagyuCut = P + "WagyuCut";          // 6
        public const string BeefCut = P + "BeefCut";            // 7
        public const string AgedWagyu = P + "DryAgedWagyu";     // 8
        public const string Manure = P + "Manure";              // 9
        public const string Fuel = P + "SyntheticFuel";         // 10
        public const string ButcherTag = P + "ButcherTag";      // 11
        public const string Brush = P + "GroomingBrush";        // 12
        public const string SurveyDrone = P + "SurveyDrone";    // 13
        public const string SprayDrone = P + "SprayDrone";      // 14
        public const string HerdingDrone = P + "HerdingDrone";  // 15
        public const string DeliveryDrone = P + "DeliveryDrone";// 16
        public const string DriedHide = P + "DriedHide";        // 17
        public const string OatFeed = P + "OatFeed";            // 18
        public const string Saddle = P + "Saddle";              // 19
        public const string WagyuSteak = P + "WagyuSteak";      // 20
        public const string BeefStew = P + "BeefStew";          // 21
        public const string WagyuTasting = P + "WagyuTasting";  // 22

        // buildings
        public const string HubPrefix = P + "Hub";
        public const string FreightDepot = P + "FreightDepot";
        public const string Warehouse = P + "Warehouse";

        // animals
        public const string Wagyu = P + "Wagyu";
        public const string Merino = P + "Merino";
        public const string CashmereGoat = P + "CashmereGoat";
        public const string Angora = P + "Angora";
        public const string HorsePrefix = P + "Horse";          // herd horses: HorseBay, HorseBlack, HorseGrey

        public static string BC(string id) => "(BC)" + id;
        public static string O(string id) => "(O)" + id;

        // vanilla
        public const string VWool = "(O)440";
        public const string VCloth = "(O)428";
        public const string VMilk = "(O)184";
        public const string VLargeMilk = "(O)186";
        public const string VGoatMilk = "(O)436";
        public const string VLargeGoatMilk = "(O)438";
        public const string VCheese = "(O)424";
        public const string VGoatCheese = "(O)426";
        public const string VBattery = "(O)787";
        public static readonly string[] Fertilisers = { "(O)919", "(O)369", "(O)368" };

        /// <summary>Luxury tier names, index 1-4 (0 = ungraded).</summary>
        public static readonly string[] TierNames = { "", "Fine", "Prime", "Estate", "Couture" };
        public static readonly float[] TierMultipliers = { 1f, 1.25f, 1.7f, 2.5f, 3.6f };

        /// <summary>Perishable goods and their shelf life in days.</summary>
        public static readonly Dictionary<string, int> ShelfLife = new()
        {
            [O(WagyuCut)] = 3,
            [O(BeefCut)] = 3,
            [O(AgedWagyu)] = 6,
            [VMilk] = 4, [VLargeMilk] = 4, [VGoatMilk] = 4, [VLargeGoatMilk] = 4
        };
    }

    /// <summary>A gradable material: raw inputs it is graded from and the graded items per tier.</summary>
    internal class Material
    {
        public string Id;            // e.g. "Cashmere"
        public string Name;          // display name in graded items, e.g. "Cashmere"
        public int BasePrice;
        public bool Perishable;
        public string[] InputIds;    // qualified ids of raw inputs from this mod or vanilla
        public string[] NameMatches; // lowercase substrings that match items from other mods (Mizu's alpacas, silk mods...)
        public string[] HerdTypes;   // lowercase substrings of FarmAnimal.type the herd bonus reads
        public int Row;              // row in graded.png

        public string GradedId(int tier) => $"{Ids.P}{Id}_T{tier}";
        public string GradedQualified(int tier) => "(O)" + GradedId(tier);
        public int GradedPrice(int tier) => (int)(BasePrice * Ids.TierMultipliers[tier]);

        public static readonly List<Material> All = new()
        {
            new() { Id = "Wool", Name = "Wool", BasePrice = 380, Row = 0, InputIds = new[] { Ids.VWool }, NameMatches = new string[0], HerdTypes = new[] { "sheep" } },
            new() { Id = "Merino", Name = "Merino", BasePrice = 640, Row = 1, InputIds = new[] { Ids.O(Ids.MerinoWool) }, NameMatches = new[] { "merino" }, HerdTypes = new[] { "merino" } },
            new() { Id = "Cashmere", Name = "Cashmere", BasePrice = 1200, Row = 2, InputIds = new[] { Ids.O(Ids.RawCashmere), "(O)selph.TextileExpansion_Cashmere" }, NameMatches = new[] { "cashmere" }, HerdTypes = new[] { "goat" } },
            new() { Id = "Alpaca", Name = "Alpaca", BasePrice = 900, Row = 3, InputIds = new[] { "(O)Mizu.AlpacaLlama_AlpacaWool" }, NameMatches = new[] { "alpaca" }, HerdTypes = new[] { "alpaca" } },
            new() { Id = "Llama", Name = "Llama", BasePrice = 700, Row = 4, InputIds = new[] { "(O)Mizu.AlpacaLlama_LlamaWool" }, NameMatches = new[] { "llama" }, HerdTypes = new[] { "llama" } },
            new() { Id = "Angora", Name = "Angora", BasePrice = 850, Row = 5, InputIds = new[] { Ids.O(Ids.RawAngora) }, NameMatches = new[] { "angora" }, HerdTypes = new[] { "rabbit", "angora" } },
            new() { Id = "Silk", Name = "Silk", BasePrice = 1000, Row = 6, InputIds = new[] { "(O)selph.TextileExpansion_SilkCocoon", "(O)selph.TextileExpansion_Silk" }, NameMatches = new[] { "silk" }, HerdTypes = new string[0] },
            new() { Id = "Leather", Name = "Leather", BasePrice = 800, Row = 7, InputIds = new[] { Ids.O(Ids.Leather) }, NameMatches = new string[0], HerdTypes = new[] { "cow", "wagyu", "buffalo", "yak", "bison" } },
            new() { Id = "Wagyu", Name = "Wagyu", BasePrice = 1500, Row = 8, Perishable = true, InputIds = new[] { Ids.O(Ids.AgedWagyu) }, NameMatches = new string[0], HerdTypes = new[] { "wagyu" } }
        };

        public static Material Get(string id) => All.Find(m => m.Id == id);

        /// <summary>Find the material a raw input belongs to, or null.</summary>
        public static Material ForInput(string qualifiedId, string name)
        {
            string n = (name ?? "").ToLowerInvariant();
            foreach (Material m in All)
            {
                if (System.Array.IndexOf(m.InputIds, qualifiedId) >= 0)
                    return m;
            }
            // other mods' items (never our own graded items): match on the item's own part of the name, after any
            // "Author.Mod_" prefix, and only for raw fibre ("AlpacaWool", "Silk"), never meat, dishes, yarn or silkworms
            if (qualifiedId != null && qualifiedId.StartsWith("(O)" + Ids.P))
                return null;
            string own = OwnName(n);
            foreach (Material m in All)
            {
                foreach (string match in m.NameMatches)
                    if (own == match || (own.Contains(match) && FibreWords.Any(own.Contains)))
                        return m;
            }
            return null;
        }

        private static readonly string[] FibreWords = { "wool", "fleece", "fiber", "fibre", "fur", "hair", "cocoon" };

        /// <summary>The part of an internal name after a mod prefix: "Mizu.AlpacaLlama_LlamaWool" -> "llamawool".</summary>
        public static string OwnName(string name)
        {
            string n = (name ?? "").ToLowerInvariant();
            int cut = System.Math.Max(n.LastIndexOf('_'), n.LastIndexOf('.'));
            return cut >= 0 ? n[(cut + 1)..] : n;
        }

        /// <summary>Whether a farm animal type belongs to this material's herd (prefix-safe, so llamas aren't alpacas).</summary>
        public bool IsHerd(string animalType)
        {
            string own = OwnName(animalType);
            return HerdTypes.Any(own.Contains);
        }

        /// <summary>If the id is one of our graded items, return its material and tier.</summary>
        public static bool TryParseGraded(string qualifiedId, out Material material, out int tier)
        {
            material = null;
            tier = 0;
            foreach (Material m in All)
            {
                for (int t = 1; t <= 4; t++)
                {
                    if (m.GradedQualified(t) == qualifiedId)
                    {
                        material = m;
                        tier = t;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
