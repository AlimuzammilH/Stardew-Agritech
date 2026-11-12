using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Objects;

namespace Agritech
{
    /// <summary>Adds the mod's items, machines, recipes, buildings, animals, mail and textures to the game's data.</summary>
    internal static class ContentRegistry
    {
        // kept for older references
        public const string SensorPostId = Ids.SensorPost;
        public const string CollarId = Ids.Collar;
        public const string HubPrefix = Ids.HubPrefix;
        public const string SensorPostQualified = "(BC)" + Ids.SensorPost;
        public const string CollarQualified = "(O)" + Ids.Collar;

        public const string ItemsTexture = "Mods/Ali.Agritech/Items";
        public const string GradedTexture = "Mods/Ali.Agritech/Graded";
        public const string BigTexture = "Mods/Ali.Agritech/BigCraftables";
        private static string HubTexture(int tier) => $"Mods/Ali.Agritech/Hub{tier}";
        private const string DepotTexture = "Mods/Ali.Agritech/FreightDepot";
        private const string WarehouseTexture = "Mods/Ali.Agritech/Warehouse";

        public static readonly string[] Characters = { "Vellier", "Ardenti", "Halvorsen", "Koen", "BrassPheasant", "tourist1", "tourist2" };

        private static readonly string[] HubDescriptions =
        {
            "Tier 1 Agritech Hub. Sensor posts mark field coverage; collars track where each animal is.",
            "Tier 2 Agritech Hub. Sensors read soil moisture and nutrients; collars report health and heat cycles. Morning alerts.",
            "Tier 3 Agritech Hub. Heat-map overlay, virtual fences and the drone fleet.",
            "Tier 4 Agritech Hub. Yield forecasts, full health histories, certified provenance for Estate and Couture grades, air freight."
        };

        private record ObjDef(string Id, string Name, string Desc, int Price, int Category, int Sprite, int Edibility = -300);

        private static readonly ObjDef[] Objects =
        {
            new(Ids.Collar, "Smart Collar", "Hold it and click a farm animal to collar it. Paired with your sensor posts through the Agritech Hub.", 300, 0, 0),
            new(Ids.MerinoWool, "Merino Wool", "Fine fleece from a Merino sheep. Grade it for the luxury market.", 640, -18, 1),
            new(Ids.RawCashmere, "Raw Cashmere", "The soft undercoat of a cashmere goat. Grade it for the luxury market.", 1100, -18, 2),
            new(Ids.RawAngora, "Raw Angora", "Light, warm angora fibre. Grade it for the luxury market.", 780, -18, 3),
            new(Ids.RawHide, "Raw Hide", "Dry it on a drying rack, then tan it in a tanning vat with oak resin.", 260, -18, 4),
            new(Ids.Leather, "Tanned Leather", "Vegetable-tanned leather. Grade it for the fashion houses.", 720, -18, 5),
            new(Ids.WagyuCut, "Wagyu Cut", "Marbled beef. Perishable: 3 days of shelf life. Age it for more.", 900, -5, 6),
            new(Ids.BeefCut, "Beef Cut", "Good beef. Perishable: 3 days of shelf life.", 320, -5, 7),
            new(Ids.AgedWagyu, "Dry-Aged Wagyu", "Aged Wagyu, deep and nutty. 6 days of shelf life. Grade it for the restaurant houses.", 1400, -5, 8),
            new(Ids.Manure, "Manure", "Barn by-product. Feeds a Fischer-Tropsch unit.", 5, -20, 9),
            new(Ids.Fuel, "Synthetic Fuel", "Liquid fuel made from farm waste. Powers drones and trucks, or makes battery packs.", 240, 0, 10),
            new(Ids.ButcherTag, "Butcher's Tag", "Hold it and click an adult animal to retire it for hide and meat.", 50, 0, 11),
            new(Ids.Brush, "Grooming Brush", "Hold it and click your horse once a day to build your bond.", 100, 0, 12),
            new(Ids.SurveyDrone, "Survey Drone", "Keep in a chest next to a Drone Dock. Extends sensor coverage across the whole farm.", 1500, 0, 13),
            new(Ids.SprayDrone, "Spray Drone", "Keep in a chest next to a Drone Dock. Fertilises depleted plots and waters dry ones each morning.", 1500, 0, 14),
            new(Ids.HerdingDrone, "Herding Drone", "Keep in a chest next to a Drone Dock. Herds collared animals into the active virtual fence zone.", 1500, 0, 15),
            new(Ids.DeliveryDrone, "Delivery Drone", "Keep in a chest next to a Drone Dock. Ships any contract the chest can fill, free, overnight.", 1500, 0, 16),
            new(Ids.DriedHide, "Dried Hide", "A stretched, dried hide. Tan it in a tanning vat with oak resin.", 380, -18, 17),
            new(Ids.OatFeed, "Oat Feed", "Hold it and click a herd horse once a day. Builds friendship and fills it up.", 40, 0, 18),
            new(Ids.Saddle, "Saddle", "Hold it and click an adult herd horse with 3+ hearts to saddle-train it. Trained horses sell for double.", 600, 0, 19),
            new(Ids.WagyuSteak, "Wagyu Steak", "Seared Wagyu, rested and sliced. The restaurant houses order these.", 1800, -7, 20, 90),
            new(Ids.BeefStew, "Beef Stew", "Slow-cooked beef with potatoes and tomato.", 650, -7, 21, 70),
            new(Ids.WagyuTasting, "Wagyu Tasting", "Three cuts, fresh and dry-aged, plated for a tasting menu.", 3200, -7, 22, 120)
        };

        private record BigDef(string Id, string Name, string Desc, int Price, int Sprite);

        private static readonly BigDef[] Bigs =
        {
            new(Ids.SensorPost, "Sensor Post", "Reads the soil around it. What it measures depends on your Agritech Hub tier.", 500, 0),
            new(Ids.GradingTable, "Grading Table", "Grades fibre, tanned leather and dry-aged Wagyu into Fine, Prime, Estate or Couture lots, with a provenance certificate.", 1000, 1),
            new(Ids.TanningVat, "Tanning Vat", "Turns a raw hide into tanned leather in a day. Uses one oak resin.", 800, 2),
            new(Ids.AgeingLocker, "Ageing Locker", "Dry-ages a Wagyu cut over three days.", 1200, 3),
            new(Ids.FtUnit, "Fischer-Tropsch Unit", "Turns manure, fibre or sap into synthetic fuel.", 3000, 4),
            new(Ids.DroneDock, "Drone Dock", "Place a chest beside it with drones and battery packs or fuel. Drones fly every morning (Hub tier 3+).", 2500, 5),
            new(Ids.ColdStore, "Cold Store", "Perishables in chests beside it stop ageing.", 1500, 6),
            new(Ids.DryingRack, "Drying Rack", "Stretches and dries a raw hide in a day, ready for the tanning vat.", 400, 7)
        };

        public static void OnAssetRequested(object sender, AssetRequestedEventArgs e) { }

        /// <summary>Sensor posts look more built-up as the Hub tier rises (sprites 8-11).</summary>
        public static int SensorSprite()
        {
            try { int t = Context.IsWorldReady ? FarmData.HubTier() : 0; return t > 0 ? 7 + t : 0; }
            catch { return 0; }
        }

        public static void Handle(AssetRequestedEventArgs e, IModHelper helper)
        {
            var name = e.NameWithoutLocale;

            /*** textures ***/
            if (name.IsEquivalentTo(ItemsTexture)) { e.LoadFromModFile<Texture2D>("assets/items.png", AssetLoadPriority.Medium); return; }
            if (name.IsEquivalentTo(GradedTexture)) { e.LoadFromModFile<Texture2D>("assets/graded.png", AssetLoadPriority.Medium); return; }
            if (name.IsEquivalentTo(BigTexture)) { e.LoadFromModFile<Texture2D>("assets/bigcraftables.png", AssetLoadPriority.Medium); return; }
            if (name.IsEquivalentTo(DepotTexture)) { e.LoadFromModFile<Texture2D>("assets/freight_depot.png", AssetLoadPriority.Medium); return; }
            for (int t = 1; t <= 4; t++)
                if (name.IsEquivalentTo(HubTexture(t))) { e.LoadFromModFile<Texture2D>($"assets/hub{t}.png", AssetLoadPriority.Medium); return; }
            foreach (string c in Characters)
            {
                if (name.IsEquivalentTo(Visitors.SpriteAsset(c))) { e.LoadFromModFile<Texture2D>($"assets/characters/{c}.png", AssetLoadPriority.Medium); return; }
                if (name.IsEquivalentTo(Visitors.PortraitAsset(c))) { e.LoadFromModFile<Texture2D>($"assets/portraits/{c}.png", AssetLoadPriority.Medium); return; }
            }
            if (name.IsEquivalentTo(WarehouseTexture)) { e.LoadFromModFile<Texture2D>("assets/warehouse.png", AssetLoadPriority.Medium); return; }
            if (name.StartsWith("Mods/Ali.Agritech/Animals/"))
            {
                if (Herd.LoadTexture(e, helper))
                    return;
                Livestock.OnAssetRequested(e, helper);
                return;
            }

            /*** data ***/
            if (name.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, ObjectData>().Data;
                    foreach (ObjDef o in Objects)
                    {
                        data[o.Id] = new ObjectData
                        {
                            Name = o.Id, DisplayName = o.Name, Description = o.Desc, Type = o.Edibility > 0 ? "Cooking" : "Basic",
                            Category = o.Category, Price = o.Price, Texture = ItemsTexture, SpriteIndex = o.Sprite, Edibility = o.Edibility
                        };
                    }
                    foreach (Material m in Material.All)
                    {
                        for (int t = 1; t <= 4; t++)
                        {
                            data[m.GradedId(t)] = new ObjectData
                            {
                                Name = m.GradedId(t),
                                DisplayName = $"{Ids.TierNames[t]} {m.Name}",
                                Description = t >= 3
                                    ? $"A certified {Ids.TierNames[t].ToLower()} lot of {m.Name.ToLower()}, traceable to the animal. The fashion and restaurant houses pay the most for these."
                                    : $"A graded {Ids.TierNames[t].ToLower()} lot of {m.Name.ToLower()}.",
                                Type = "Basic", Category = m.Perishable ? -5 : -18, Price = m.GradedPrice(t),
                                Texture = GradedTexture, SpriteIndex = m.Row * 4 + (t - 1), Edibility = -300
                            };
                        }
                    }
                });
            }

            if (name.IsEquivalentTo("Data/BigCraftables"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, BigCraftableData>().Data;
                    foreach (BigDef b in Bigs)
                    {
                        data[b.Id] = new BigCraftableData
                        {
                            Name = b.Id, DisplayName = b.Name, Description = b.Desc, Price = b.Price, Fragility = 0,
                            CanBePlacedOutdoors = true, CanBePlacedIndoors = true, Texture = BigTexture,
                            SpriteIndex = b.Id == Ids.SensorPost ? SensorSprite() : b.Sprite
                        };
                    }
                });
            }

            if (name.IsEquivalentTo("Data/CraftingRecipes"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;
                    const string never = "s Farming 99"; // unlocked by the Agritech skill instead
                    void R(string key, string ingredients, string yield, bool big) => data[key] = $"{ingredients}/Home/{yield}/{(big ? "true" : "false")}/{never}/{key}";
                    R("Sensor Post", "388 10 334 2 338 1", Ids.SensorPost, true);
                    R("Smart Collar", "771 10 334 1 338 1", Ids.Collar, false);
                    R("Butcher's Tag", "334 1 771 5", $"{Ids.ButcherTag} 3", false);
                    R("Grooming Brush", "388 10 771 20", Ids.Brush, false);
                    R("Grading Table", "709 10 335 2 338 2", Ids.GradingTable, true);
                    R("Tanning Vat", "388 40 334 5 725 1", Ids.TanningVat, true);
                    R("Ageing Locker", "335 5 338 3 787 1", Ids.AgeingLocker, true);
                    R("Cold Store", "335 5 338 5 787 1", Ids.ColdStore, true);
                    R("Fischer-Tropsch Unit", "335 10 336 2 787 2", Ids.FtUnit, true);
                    R("Fuel Cell", $"{Ids.Fuel} 2 334 1", "787", false);
                    R("Drone Dock", "709 20 335 10 787 2", Ids.DroneDock, true);
                    R("Survey Drone", "335 3 338 2 787 1", Ids.SurveyDrone, false);
                    R("Spray Drone", "335 3 338 2 787 1 92 20", Ids.SprayDrone, false);
                    R("Herding Drone", "335 3 338 2 787 1 771 20", Ids.HerdingDrone, false);
                    R("Delivery Drone", "335 3 338 2 787 1 336 1", Ids.DeliveryDrone, false);
                    R("Drying Rack", "388 30 771 20", Ids.DryingRack, true);
                    R("Oat Feed", "262 3 771 5", $"{Ids.OatFeed} 5", false);
                    R("Saddle", $"{Ids.Leather} 2 335 1", Ids.Saddle, false);
                });
            }

            if (name.IsEquivalentTo("Data/CookingRecipes"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;
                    void C(string key, string ingredients, string yield) => data[key] = $"{ingredients}/10 10/{yield}/s Farming 99/{key}";
                    C("Wagyu Steak", $"{Ids.WagyuCut} 1 248 1", Ids.WagyuSteak);
                    C("Beef Stew", $"{Ids.BeefCut} 2 192 1 256 1", Ids.BeefStew);
                    C("Wagyu Tasting", $"{Ids.AgedWagyu} 1 {Ids.WagyuCut} 1 16 1", Ids.WagyuTasting);
                });
            }

            if (name.IsEquivalentTo("Data/Machines"))
            {
                e.Edit(Integration.EditMachinesLate, AssetEditPriority.Late);
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, MachineData>().Data;
                    const string asm = "Agritech.Machines, Agritech";

                    data[Ids.BC(Ids.GradingTable)] = new MachineData
                    {
                        OutputRules = new List<MachineOutputRule>
                        {
                            new()
                            {
                                Id = "Grade",
                                Triggers = new List<MachineOutputTriggerRule> { new() { Id = "Any", RequiredCount = 1 } },
                                OutputItem = new List<MachineItemOutput> { new() { Id = "Graded", OutputMethod = asm + ": Grade" } },
                                MinutesUntilReady = 120
                            }
                        }
                    };

                    data[Ids.BC(Ids.TanningVat)] = new MachineData
                    {
                        AdditionalConsumedItems = new List<MachineItemAdditionalConsumedItems>
                        {
                            new() { ItemId = "(O)725", RequiredCount = 1, InvalidCountMessage = "The vat needs one oak resin to tan a hide." }
                        },
                        OutputRules = new List<MachineOutputRule>
                        {
                            new()
                            {
                                Id = "Tan",
                                Triggers = new List<MachineOutputTriggerRule> { new() { Id = "Hide", RequiredItemId = Ids.O(Ids.DriedHide), RequiredCount = 1 } },
                                OutputItem = new List<MachineItemOutput> { new() { Id = "Leather", ItemId = Ids.O(Ids.Leather), CopyQuality = true } },
                                DaysUntilReady = 1
                            }
                        }
                    };

                    data[Ids.BC(Ids.DryingRack)] = new MachineData
                    {
                        OutputRules = new List<MachineOutputRule>
                        {
                            new()
                            {
                                Id = "Dry",
                                Triggers = new List<MachineOutputTriggerRule> { new() { Id = "Hide", RequiredItemId = Ids.O(Ids.RawHide), RequiredCount = 1 } },
                                OutputItem = new List<MachineItemOutput> { new() { Id = "Dried", ItemId = Ids.O(Ids.DriedHide), CopyQuality = true } },
                                DaysUntilReady = 1
                            }
                        }
                    };

                    data[Ids.BC(Ids.AgeingLocker)] = new MachineData
                    {
                        OutputRules = new List<MachineOutputRule>
                        {
                            new()
                            {
                                Id = "Age",
                                Triggers = new List<MachineOutputTriggerRule> { new() { Id = "Wagyu", RequiredItemId = Ids.O(Ids.WagyuCut), RequiredCount = 1 } },
                                OutputItem = new List<MachineItemOutput> { new() { Id = "Aged", ItemId = Ids.O(Ids.AgedWagyu), OutputMethod = asm + ": CarryFreshness" } },
                                DaysUntilReady = 3
                            }
                        }
                    };

                    MachineOutputRule Ft(string id, string input, int count, int fuel) => new()
                    {
                        Id = id,
                        Triggers = new List<MachineOutputTriggerRule> { new() { Id = id, RequiredItemId = input, RequiredCount = count } },
                        OutputItem = new List<MachineItemOutput> { new() { Id = id, ItemId = Ids.O(Ids.Fuel), MinStack = fuel, MaxStack = fuel } },
                        MinutesUntilReady = 600
                    };
                    data[Ids.BC(Ids.FtUnit)] = new MachineData
                    {
                        OutputRules = new List<MachineOutputRule>
                        {
                            Ft("Manure", Ids.O(Ids.Manure), 5, 2),
                            Ft("Fiber", "(O)771", 30, 1),
                            Ft("Sap", "(O)92", 20, 1),
                            Ft("Hardwood", "(O)709", 5, 1)
                        }
                    };
                });
            }

            if (name.IsEquivalentTo("Data/Buildings"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, BuildingData>().Data;
                    int[] gold = { 15000, 40000, 90000, 200000 };
                    var mats = new[]
                    {
                        new[] { ("(O)388", 150), ("(O)390", 100), ("(O)334", 10) },
                        new[] { ("(O)709", 50), ("(O)335", 15), ("(O)338", 20) },
                        new[] { ("(O)709", 100), ("(O)336", 10), ("(O)787", 5) },
                        new[] { ("(O)337", 10), ("(O)787", 10), ("(O)74", 1) }
                    };
                    for (int t = 1; t <= 4; t++)
                    {
                        var materials = new List<BuildingMaterial>();
                        foreach (var (id, amount) in mats[t - 1])
                            materials.Add(new BuildingMaterial { ItemId = id, Amount = amount });
                        data[Ids.HubPrefix + t] = new BuildingData
                        {
                            Name = $"Agritech Hub (Tier {t})",
                            Description = HubDescriptions[t - 1],
                            Texture = HubTexture(t),
                            Size = new Point(3, 2),
                            SourceRect = new Rectangle(0, 0, 48, 48),
                            Builder = "Robin",
                            BuildCost = gold[t - 1],
                            BuildMaterials = materials,
                            BuildDays = t == 1 ? 2 : 3,
                            BuildingToUpgrade = t == 1 ? null : Ids.HubPrefix + (t - 1),
                            BuildCondition = t == 1 ? null : $"Ali.Agritech_SKILL {(t - 1) * 3}"
                        };
                    }
                    data[Ids.FreightDepot] = new BuildingData
                    {
                        Name = "Freight Depot",
                        Description = "Loading dock for rail and truck freight. Unlocks faster shipping for contracts.",
                        Texture = DepotTexture,
                        Size = new Point(4, 2),
                        SourceRect = new Rectangle(0, 0, 64, 48),
                        Builder = "Robin",
                        BuildCost = 25000,
                        BuildMaterials = new List<BuildingMaterial>
                        {
                            new() { ItemId = "(O)388", Amount = 200 }, new() { ItemId = "(O)335", Amount = 20 }, new() { ItemId = "(O)709", Amount = 40 }
                        },
                        BuildDays = 2,
                        BuildCondition = "Ali.Agritech_SKILL 4"
                    };
                    data[Ids.Warehouse] = new BuildingData
                    {
                        Name = "Cold Warehouse",
                        Description = "Climate-controlled storage. Perishables kept inside don't age.",
                        Texture = WarehouseTexture,
                        Size = new Point(3, 2),
                        SourceRect = new Rectangle(0, 0, 48, 48),
                        Builder = "Robin",
                        BuildCost = 20000,
                        BuildMaterials = new List<BuildingMaterial>
                        {
                            new() { ItemId = "(O)709", Amount = 60 }, new() { ItemId = "(O)335", Amount = 15 }, new() { ItemId = "(O)787", Amount = 2 }
                        },
                        BuildDays = 2,
                        BuildCondition = "Ali.Agritech_SKILL 4",
                        Chests = new List<BuildingChest> { new() { Id = "Storage", Type = BuildingChestType.Chest } },
                        ActionTiles = new List<BuildingActionTile>
                        {
                            new() { Id = "Storage", Tile = new Point(1, 1), Action = "BuildingChest Storage" }
                        }
                    };
                });
            }

            if (name.IsEquivalentTo("Data/FarmAnimals"))
            {
                e.Edit(Livestock.EditFarmAnimals);
                e.Edit(Herd.EditFarmAnimals);
            }

            if (name.IsEquivalentTo("Data/mail"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;
                    data[ModEntry.ContractsMail] = "New orders have come in from your buyers.^^Open the contract board to see them. Ship by cart, rail, truck or air.^^   -Agritech Exchange[#]New contracts";
                });
            }
        }
    }
}
