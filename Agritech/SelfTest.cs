using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Machines;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;

namespace Agritech
{
    /// <summary>
    /// "agritech_selftest run": drives every feature once inside the real game and logs PASS / FAIL per check,
    /// so a single run on a throwaway save shows what works without playing through it. Results also go to
    /// Mods/Agritech/selftest.txt.
    /// </summary>
    internal static class SelfTest
    {
        public static bool Running { get; private set; }

        private static IModHelper helper;
        private static IMonitor monitor;
        private static readonly List<string> results = new();
        private static int passed, failed;

        // everything placed for the test, removed afterwards
        private static readonly List<Vector2> placedObjects = new();
        private static readonly List<Vector2> placedDirt = new();
        private static readonly List<Building> placedBuildings = new();
        private static Farm farm;
        private static Point origin;

        // menu checks run across frames, so a crash while drawing shows up
        private static readonly Queue<(string name, Func<IClickableMenu> open, Type expected)> menuSteps = new();
        private static (string name, Type expected)? waiting;
        private static int waitTicks;

        /*********
        ** Run
        *********/
        public static void Start(IModHelper h, IMonitor m)
        {
            helper = h;
            monitor = m;
            results.Clear();
            passed = failed = 0;
            placedObjects.Clear();
            placedDirt.Clear();
            placedBuildings.Clear();
            farm = Game1.getFarm();
            Running = true;
            Herd.SkipConsume = true;
            Log("Agritech self-test started on " + Constants.SaveFolderName);

            if (!FindFreeArea(14, 10, out origin))
            {
                Fail("setup", "no free 14x10 area on the farm to place test machines; clear some space and run again");
                Finish();
                return;
            }
            Log($"Using a free area at tile {origin.X},{origin.Y}");

            try
            {
                TestContent();
                TestTextures();
                TestSkill();
                TestMaterials();
                TestHub();
                TestMachines();
                TestRecipes();
                TestSensors();
                TestColdChain();
                TestContracts();
                TestDrones();
                TestAnimals();
                TestHerd();
                TestVisitors();
                TestNightly();
                TestTooltip();
                TestIntegration();
            }
            catch (Exception ex)
            {
                Fail("runner", ex.ToString());
            }

            CloseDialogue();
            menuSteps.Clear();
            menuSteps.Enqueue(("contract board draws", () => new ContractsMenu(null), typeof(ContractsMenu)));
            menuSteps.Enqueue(("terminal draws", () => { ModEntry.OpenTerminal(); return Game1.activeClickableMenu; }, typeof(TerminalMenu)));
        }

        /// <summary>Called every tick: steps through the menu checks, then cleans up.</summary>
        public static void Tick()
        {
            if (!Running)
                return;
            if (waiting != null)
            {
                if (--waitTicks > 0)
                    return;
                var (name, expected) = waiting.Value;
                if (Game1.activeClickableMenu?.GetType() == expected)
                    Pass(name);
                else
                    Fail(name, "the menu closed itself while drawing; see the error just above in the log");
                Game1.activeClickableMenu = null;
                waiting = null;
                return;
            }
            if (menuSteps.Count > 0)
            {
                var step = menuSteps.Dequeue();
                try
                {
                    IClickableMenu menu = step.open();
                    if (menu != null)
                        Game1.activeClickableMenu = menu;
                    waiting = (step.name, step.expected);
                    waitTicks = 30;
                }
                catch (Exception ex)
                {
                    Fail(step.name, ex.Message);
                }
                return;
            }
            Finish();
        }

        private static void Finish()
        {
            Cleanup();
            Herd.SkipConsume = false;
            Running = false;
            string summary = $"Agritech self-test finished: {passed} passed, {failed} failed.";
            monitor.Log(summary, failed == 0 ? LogLevel.Alert : LogLevel.Warn);
            results.Add(summary);
            try { File.WriteAllLines(Path.Combine(helper.DirectoryPath, "selftest.txt"), results); }
            catch { /* the log has it anyway */ }
            Game1.addHUDMessage(new HUDMessage(summary, failed == 0 ? HUDMessage.achievement_type : HUDMessage.error_type));
        }

        /*********
        ** Checks
        *********/
        private static void TestContent()
        {
            string[] objects =
            {
                Ids.Collar, Ids.MerinoWool, Ids.RawCashmere, Ids.RawAngora, Ids.RawHide, Ids.Leather, Ids.WagyuCut, Ids.BeefCut,
                Ids.AgedWagyu, Ids.Manure, Ids.Fuel, Ids.ButcherTag, Ids.Brush, Ids.SurveyDrone, Ids.SprayDrone, Ids.HerdingDrone,
                Ids.DeliveryDrone, Ids.DriedHide, Ids.OatFeed, Ids.Saddle, Ids.WagyuSteak, Ids.BeefStew, Ids.WagyuTasting
            };
            Check("all 23 items exist", () => Missing(objects.Select(Ids.O)));
            Check("all 36 graded lots exist", () => Missing(Material.All.SelectMany(m => Enumerable.Range(1, 4).Select(m.GradedQualified))));
            string[] bcs = { Ids.SensorPost, Ids.GradingTable, Ids.TanningVat, Ids.AgeingLocker, Ids.FtUnit, Ids.DroneDock, Ids.ColdStore, Ids.DryingRack };
            Check("all 8 machines exist", () => Missing(bcs.Select(Ids.BC)));

            var machines = Game1.content.Load<Dictionary<string, MachineData>>("Data/Machines");
            Check("machine rules registered", () => Absent(new[] { Ids.GradingTable, Ids.TanningVat, Ids.AgeingLocker, Ids.FtUnit, Ids.DryingRack }.Select(Ids.BC), machines.ContainsKey));
            var buildings = Game1.content.Load<Dictionary<string, BuildingData>>("Data/Buildings");
            Check("buildings registered", () => Absent(new[] { Ids.HubPrefix + 1, Ids.HubPrefix + 2, Ids.HubPrefix + 3, Ids.HubPrefix + 4, Ids.FreightDepot, Ids.Warehouse }, buildings.ContainsKey));
            var animals = Game1.content.Load<Dictionary<string, FarmAnimalData>>("Data/FarmAnimals");
            Check("breeds and horses registered", () => Absent(Livestock.Breeds.Select(b => b.id).Concat(new[] { "Bay", "Black", "Grey" }.Select(Herd.Id)), animals.ContainsKey));
            var crafting = Game1.content.Load<Dictionary<string, string>>("Data/CraftingRecipes");
            Check("crafting recipes registered", () => Absent(Skill.RecipeUnlocks.Select(r => r.recipe), crafting.ContainsKey));
            var cooking = Game1.content.Load<Dictionary<string, string>>("Data/CookingRecipes");
            Check("cooking recipes registered", () => Absent(Skill.CookingUnlocks.Select(r => r.recipe), cooking.ContainsKey));
        }

        private static void TestTextures()
        {
            var names = new List<string> { ContentRegistry.ItemsTexture, ContentRegistry.GradedTexture, ContentRegistry.BigTexture,
                "Mods/Ali.Agritech/FreightDepot", "Mods/Ali.Agritech/Warehouse" };
            names.AddRange(Enumerable.Range(1, 4).Select(t => $"Mods/Ali.Agritech/Hub{t}"));
            foreach (string who in Buyer.All.Where(b => b.VisitDay > 0).Select(b => b.Id).Concat(new[] { "tourist1", "tourist2" }))
            {
                names.Add(Visitors.SpriteAsset(who));
                names.Add(Visitors.PortraitAsset(who));
            }
            foreach (var b in Livestock.Breeds)
                foreach (string kind in new[] { "adult", "baby", "shop" })
                    names.Add(Livestock.TextureName(b.id, kind));
            Check($"{names.Count} textures load", () =>
            {
                var bad = new List<string>();
                foreach (string n in names)
                {
                    try { if (helper.GameContent.Load<Texture2D>(n) == null) bad.Add(n); }
                    catch (Exception ex) { bad.Add($"{n} ({ex.Message})"); }
                }
                return bad.Count == 0 ? null : "failed: " + string.Join(", ", bad);
            });
            foreach (string coat in new[] { "Bay", "Black", "Grey" })
            {
                Check($"{coat} horse sprites build", () =>
                {
                    var adult = helper.GameContent.Load<Texture2D>($"Mods/Ali.Agritech/Animals/Horse{coat}_adult");
                    var baby = helper.GameContent.Load<Texture2D>($"Mods/Ali.Agritech/Animals/Horse{coat}_baby");
                    var shop = helper.GameContent.Load<Texture2D>($"Mods/Ali.Agritech/Animals/Horse{coat}_shop");
                    if (adult.Width != 128 || adult.Height != 160 || baby.Width != 128 || shop.Width != 32 || shop.Height != 16)
                        return $"wrong sizes: adult {adult.Width}x{adult.Height}, foal {baby.Width}x{baby.Height}, icon {shop.Width}x{shop.Height}";
                    var px = new Color[adult.Width * adult.Height];
                    adult.GetData(px);
                    return px.Count(c => c.A > 0) > 2000 ? null : "the adult sheet is nearly empty";
                });
            }
        }

        private static void TestSkill()
        {
            Check("skill levels from xp", () =>
                Skill.Level(0) == 0 && Skill.Level(100) == 1 && Skill.Level(6800) == 10 ? null : $"levels came out {Skill.Level(0)}, {Skill.Level(100)}, {Skill.Level(6800)}");
            Check("max skill grants every recipe", () =>
            {
                if (ModEntry.State.SkillXp < 15000)
                    Skill.AddXp(15000 - ModEntry.State.SkillXp);
                Skill.GrantRecipes();
                var missing = Skill.RecipeUnlocks.Where(r => !Game1.player.craftingRecipes.ContainsKey(r.recipe)).Select(r => r.recipe)
                    .Concat(Skill.CookingUnlocks.Where(r => !Game1.player.cookingRecipes.ContainsKey(r.recipe)).Select(r => r.recipe)).ToList();
                return missing.Count == 0 ? null : "not granted: " + string.Join(", ", missing);
            });
            Check("skill condition for Marnie's shop", () => GameStateQuery.CheckConditions("Ali.Agritech_SKILL 2") ? null : "Ali.Agritech_SKILL 2 is false at level 10");
        }

        private static void TestMaterials()
        {
            Check("fibre matching", () =>
            {
                var cases = new (string id, string name, string expect)[]
                {
                    ("(O)440", "Wool", "Wool"),
                    (Ids.O(Ids.MerinoWool), Ids.MerinoWool, "Merino"),
                    ("(O)Mizu.AlpacaLlama_AlpacaWool", "Mizu.AlpacaLlama_AlpacaWool", "Alpaca"),
                    ("(O)Mizu.AlpacaLlama_LlamaWool", "Mizu.AlpacaLlama_LlamaWool", "Llama"),
                    ("(O)Mizu.AlpacaLlama_AlpacaMeat", "Mizu.AlpacaLlama_AlpacaMeat", null),
                    ("(O)Mizu.AlpacaLlama_LlamaYarn", "Mizu.AlpacaLlama_LlamaYarn", null),
                    ("(O)selph.TextileExpansion_Cashmere", "selph.TextileExpansion_Cashmere", "Cashmere"),
                    ("(O)selph.TextileExpansion_Silkworm", "selph.TextileExpansion_Silkworm", null),
                    (Material.Get("Wool").GradedQualified(2), "graded", null)
                };
                var wrong = cases.Where(c => Material.ForInput(c.id, c.name)?.Id != c.expect).Select(c => $"{c.name} -> {Material.ForInput(c.id, c.name)?.Id ?? "none"}").ToList();
                return wrong.Count == 0 ? null : string.Join("; ", wrong);
            });
            Check("herd matching", () =>
                Material.Get("Llama").IsHerd("Mizu.AlpacaLlama_WhiteLlama") && !Material.Get("Alpaca").IsHerd("Mizu.AlpacaLlama_WhiteLlama")
                && Material.Get("Cashmere").IsHerd(Ids.CashmereGoat) ? null : "llama/alpaca/goat herd matching is wrong");
            Check("graded ids parse back", () =>
                Material.TryParseGraded(Material.Get("Cashmere").GradedQualified(3), out Material m, out int t) && m.Id == "Cashmere" && t == 3 ? null : "round trip failed");
        }

        private static void TestHub()
        {
            Check("Hub tier 4 counts", () =>
            {
                Building hub = PlaceBuilding(Ids.HubPrefix + 4, 0, 0);
                return FarmData.HubTier() == 4 ? null : $"HubTier() is {FarmData.HubTier()}";
            });
            Check("sensor post look follows Hub tier", () => ContentRegistry.SensorSprite() == 11 ? null : $"sprite {ContentRegistry.SensorSprite()} instead of 11");
            Check("Freight Depot counts", () =>
            {
                PlaceBuilding(Ids.FreightDepot, 6, 0);
                return Logistics.HasFreightDepot() ? null : "HasFreightDepot() is false";
            });
        }

        private static void TestMachines()
        {
            int x = 0;
            Vector2 Next() => new(origin.X + (x++ % 12), origin.Y + 6 + x / 12);

            Machine("Grading Table grades wool", Ids.GradingTable, ItemRegistry.Create("(O)440", 1, 2), null, Next(), q => q.StartsWith(Material.Get("Wool").GradedQualified(1)[..^1]));
            if (ItemRegistry.Exists("(O)Mizu.AlpacaLlama_LlamaWool"))
                Machine("Grading Table grades llama wool as llama", Ids.GradingTable, ItemRegistry.Create("(O)Mizu.AlpacaLlama_LlamaWool"), null, Next(), q => q.Contains("_Llama_T"));
            if (ItemRegistry.Exists("(O)Mizu.AlpacaLlama_AlpacaMeat"))
                MachineRejects("Grading Table refuses alpaca meat", Ids.GradingTable, ItemRegistry.Create("(O)Mizu.AlpacaLlama_AlpacaMeat"), Next());
            Machine("Drying Rack dries hide", Ids.DryingRack, ItemRegistry.Create(Ids.O(Ids.RawHide)), null, Next(), q => q == Ids.O(Ids.DriedHide));
            Machine("Tanning Vat tans dried hide", Ids.TanningVat, ItemRegistry.Create(Ids.O(Ids.DriedHide)), ItemRegistry.Create("(O)725", 2), Next(), q => q == Ids.O(Ids.Leather));
            MachineRejects("Tanning Vat refuses raw hide", Ids.TanningVat, ItemRegistry.Create(Ids.O(Ids.RawHide)), Next());
            Machine("Ageing Locker ages Wagyu", Ids.AgeingLocker, ItemRegistry.Create(Ids.O(Ids.WagyuCut)), null, Next(), q => q == Ids.O(Ids.AgedWagyu));
            Machine("Fischer-Tropsch Unit makes fuel", Ids.FtUnit, ItemRegistry.Create(Ids.O(Ids.Manure), 5), null, Next(), q => q == Ids.O(Ids.Fuel));
        }

        private static void TestRecipes()
        {
            foreach (var (_, recipe) in Skill.RecipeUnlocks)
                Check($"craft {recipe}", () => new CraftingRecipe(recipe, false).createItem() != null ? null : "made nothing");
            foreach (var (_, recipe) in Skill.CookingUnlocks)
                Check($"cook {recipe}", () =>
                {
                    Item dish = new CraftingRecipe(recipe, true).createItem();
                    return dish is SObject o && o.Edibility > 0 ? null : $"made {dish?.QualifiedItemId ?? "nothing"}, not an edible dish";
                });
        }

        private static void TestSensors()
        {
            Check("sensor post reads soil", () =>
            {
                Vector2 post = new(origin.X + 12, origin.Y + 2);
                PlaceObject(ItemRegistry.Create<SObject>(Ids.BC(Ids.SensorPost)), post);
                for (int i = -1; i <= 1; i++)
                    PlaceDirt(post + new Vector2(i, 1), watered: i == 0);
                var reading = FarmData.SensorPosts().FirstOrDefault(p => p.loc == farm && p.tile == post);
                if (reading.loc == null)
                    return "the placed sensor post wasn't found";
                SensorReading r = FarmData.Read(ModEntry.State, farm, post, FarmData.HubTier());
                return r.TilledTiles >= 3 && r.WateredTiles >= 1 ? null : $"read {r.TilledTiles} tilled, {r.WateredTiles} watered";
            });
            Check("soil updates overnight", () => { FarmData.UpdateSoilOvernight(ModEntry.State); return null; });
        }

        private static void TestColdChain()
        {
            Check("Cold Store keeps a chest fresh", () =>
            {
                Vector2 unit = new(origin.X + 12, origin.Y + 5);
                PlaceObject(ItemRegistry.Create<SObject>(Ids.BC(Ids.ColdStore)), unit);
                var chest = new Chest(true);
                PlaceObject(chest, unit + new Vector2(1, 0));
                Item milk = ItemRegistry.Create(Ids.VMilk);
                milk.modData[ModEntry.MadeKey] = "100";
                chest.addItem(milk);
                Logistics.ColdStoreTick();
                return milk.modData[ModEntry.MadeKey] == "101" ? null : $"made-day is {milk.modData[ModEntry.MadeKey]}, expected 101";
            });
            Check("Cold Warehouse storage keeps goods fresh", () =>
            {
                Building wh = PlaceBuilding(Ids.Warehouse, 10, 0);
                Chest storage = wh.GetBuildingChest("Storage");
                if (storage == null)
                    return "the warehouse has no storage chest";
                Item cut = ItemRegistry.Create(Ids.O(Ids.WagyuCut));
                cut.modData[ModEntry.MadeKey] = "50";
                storage.addItem(cut);
                Logistics.ColdStoreTick();
                return cut.modData[ModEntry.MadeKey] == "51" ? null : $"made-day is {cut.modData[ModEntry.MadeKey]}, expected 51";
            });
            Check("freshness text", () =>
            {
                Item cut = ItemRegistry.Create(Ids.O(Ids.WagyuCut));
                cut.modData[ModEntry.MadeKey] = Game1.Date.TotalDays.ToString();
                return Logistics.Freshness(cut) != null ? null : "no freshness text on a fresh Wagyu cut";
            });
        }

        private static void TestContracts()
        {
            var S = ModEntry.State;
            foreach (string mode in new[] { "Cart", "Rail", "Truck", "Air", "Drone", "Hand" })
            {
                Check($"ship and get paid by {mode}", () =>
                {
                    var c = TestContract("Wool", 0, 2, 1000);
                    var source = new List<Item> { ItemRegistry.Create("(O)440", 5) };
                    string err = ContractBook.Ship(c, source, mode, chargeFuel: false);
                    if (err != null)
                        return "Ship said: " + err;
                    if (source[0]?.Stack != 3)
                        return $"took the wrong amount, {source[0]?.Stack ?? 0} left of 5";
                    foreach (Shipment s in S.Shipments.Where(s => s.ContractId == c.Id))
                        s.ArriveDay = Game1.Date.TotalDays;
                    int before = Game1.player.Money;
                    ContractBook.SettleArrivals();
                    int paid = Game1.player.Money - before;
                    S.Contracts.Remove(c);
                    return c.Status == "Done" && paid > 0 ? null : $"status {c.Status}, paid {paid}";
                });
            }
            Check("spoiled perishables pay half", () =>
            {
                var c = TestContract("Milk", 0, 1, 1000);
                Item milk = ItemRegistry.Create(Ids.VMilk);
                milk.modData[ModEntry.MadeKey] = (Game1.Date.TotalDays - 20).ToString();   // before the save began on a new farm; still a real date
                ContractBook.Ship(c, new List<Item> { milk }, "Cart", chargeFuel: false);
                var s = S.Shipments.First(x => x.ContractId == c.Id);
                bool spoiled = s.Spoiled;
                s.ArriveDay = Game1.Date.TotalDays;
                int before = Game1.player.Money;
                ContractBook.SettleArrivals();
                int paid = Game1.player.Money - before;
                S.Contracts.Remove(c);
                return spoiled && paid == 500 ? null : $"spoiled={spoiled}, paid {paid}";
            });
            Check("minimum grade is enforced", () =>
            {
                Goods wool = Goods.Get("Wool");
                Item fine = ItemRegistry.Create(Material.Get("Wool").GradedQualified(1));
                return wool.Accepts(fine, 1) && !wool.Accepts(fine, 2) ? null : "a Fine lot was judged wrongly against Fine/Prime orders";
            });
            Check("weekly contracts post", () =>
            {
                int before = S.Contracts.Count;
                ContractBook.PostWeekly(new Random(1));
                int added = S.Contracts.Count - before;
                var posted = S.Contracts.Skip(before).ToList();
                foreach (var c in posted)
                    S.Contracts.Remove(c);
                return added > 0 ? null : "no buyer posted anything at max skill with a tier-4 Hub";
            });
            Check("daily contract update", () => { ContractBook.DailyUpdate(); return null; });
        }

        private static void TestDrones()
        {
            Check("drone morning run", () =>
            {
                Vector2 dock = new(origin.X + 12, origin.Y + 8);
                PlaceObject(ItemRegistry.Create<SObject>(Ids.BC(Ids.DroneDock)), dock);
                var chest = new Chest(true);
                PlaceObject(chest, dock + new Vector2(1, 0));
                foreach (string d in Drones.Types)
                    chest.addItem(ItemRegistry.Create(Ids.O(d)));
                chest.addItem(ItemRegistry.Create(Ids.VBattery, 5));
                chest.addItem(ItemRegistry.Create("(O)368", 5));
                chest.addItem(ItemRegistry.Create("(O)440", 3));
                var c = TestContract("Wool", 0, 2, 800);

                if (!Drones.SurveyActive() || !Drones.HerdingActive())
                    return "docked drones aren't detected";
                Drones.MorningRun(ModEntry.State);
                bool shipped = c.Status == "Shipped";
                ModEntry.State.Shipments.RemoveAll(s => s.ContractId == c.Id);
                ModEntry.State.Contracts.Remove(c);
                if (ModEntry.State.DroneActionsToday == 0)
                    return "the drones did nothing";
                return shipped ? null : "the delivery drone didn't ship the contract the chest could fill";
            });
        }

        private static void TestAnimals()
        {
            foreach (var b in Livestock.Breeds)
            {
                Check($"{b.name} spawns with its sprite", () =>
                {
                    var a = NewAnimal(b.id);
                    return a.Sprite?.Texture != null ? null : "no sprite texture";
                });
            }
            Check("Butcher's Tag rules", () =>
            {
                var wagyu = NewAnimal(Ids.Wagyu);
                wagyu.age.Value = 100;
                var horse = NewAnimal(Herd.Id("Bay"));
                horse.age.Value = 100;
                bool cow = Livestock.CanRetire(wagyu, out string why1);
                bool hor = Livestock.CanRetire(horse, out _);
                var young = NewAnimal(Ids.Wagyu);
                young.age.Value = 10;
                bool flip = Livestock.CanRetire(young, out _);
                return cow && !hor && !flip ? null : $"season-old Wagyu retire={cow} ({why1}), herd horse retire={hor}, 5-day-adult Wagyu retire={flip}";
            });
        }

        private static void TestHerd()
        {
            foreach (string coat in new[] { "Bay", "Black", "Grey" })
            {
                Check($"{coat} horse: feed, groom, saddle, sell", () =>
                {
                    var a = NewAnimal(Herd.Id(coat));
                    if (a.Sprite?.Texture == null)
                        return "no sprite texture";
                    a.age.Value = 30;
                    a.friendshipTowardFarmer.Value = 550;
                    Herd.Feed(a);
                    Herd.Groom(a);
                    if (a.friendshipTowardFarmer.Value != 600)
                        return $"friendship {a.friendshipTowardFarmer.Value} after oats and brushing, expected 600";
                    int untrained = a.getSellPrice();
                    Herd.SaddleUp(a);
                    if (!Herd.IsTrained(a))
                        return "the saddle didn't train it";
                    int trained = a.getSellPrice();
                    return trained == untrained * 2 ? null : $"sells for {trained}, untrained {untrained}";
                });
            }
        }

        private static void TestVisitors()
        {
            Check("buyers and tourists visit and talk", () =>
            {
                var who = Buyer.All.Where(b => b.VisitDay > 0).Select(b => (b.Id, b.Name)).Concat(new[] { ("tourist1", "Tourist"), ("tourist2", "Day-tripper") }).ToList();
                int i = 0;
                foreach (var (id, name) in who)
                    Visitors.Spawn(helper, farm, id, name, new Vector2(origin.X + i++ * 2, origin.Y + 3));
                var spawned = farm.characters.Where(n => n.modData.ContainsKey("Ali.Agritech/Visitor")).ToList();
                if (spawned.Count != who.Count)
                    return $"{spawned.Count} of {who.Count} spawned";
                var noArt = spawned.Where(n => n.Sprite?.Texture == null || n.Portrait == null).Select(n => n.displayName).ToList();
                if (noArt.Count > 0)
                    return "missing sprite or portrait: " + string.Join(", ", noArt);
                Visitors.Wander(new Random(3));
                foreach (NPC npc in spawned)
                {
                    Visitors.Talk(npc, () => { });
                    CloseDialogue();
                }
                Visitors.Despawn();
                int left = farm.characters.Count(n => n.modData.ContainsKey("Ali.Agritech/Visitor"));
                return left == 0 ? null : $"{left} visitor(s) left behind after despawn";
            });
        }

        private static void TestNightly()
        {
            Check("manure, riding horses and foals overnight", () =>
            {
                Livestock.SpawnManure(new Random(5));
                Livestock.NightlyHorses();
                Livestock.ClaimFoals();
                foreach (Horse h in Livestock.AllHorses())
                    Livestock.Record(h);
                return null;
            });
            Check("balance diary writes", () =>
            {
                Running = false;
                try { BalanceDiary.WriteDay(helper); }
                finally { Running = true; }
                string path = Path.Combine(helper.DirectoryPath, "balance", $"{Constants.SaveFolderName}.csv");
                return File.Exists(path) ? null : "no balance file was written";
            });
        }

        private static void TestTooltip()
        {
            Check("provenance shows in the tooltip", () =>
            {
                SObject lot = ItemRegistry.Create<SObject>(Material.Get("Cashmere").GradedQualified(4));
                lot.modData[ModEntry.ProvenanceKey] = "Couture Cashmere: self-test";
                return lot.getDescription().Contains("Provenance") ? null : "no provenance line";
            });
        }

        private static void TestIntegration()
        {
            if (!helper.ModRegistry.IsLoaded("selph.TextileExpansion"))
            {
                Log("SKIP Cloths and Colors link (not installed)");
                return;
            }
            Check("Cloths and Colors spinner takes our fibre", () =>
            {
                var machines = Game1.content.Load<Dictionary<string, MachineData>>("Data/Machines");
                if (!machines.TryGetValue("(BC)selph.TextileExpansion_YarnSpinner", out MachineData spinner))
                    return "C&C's yarn spinner isn't in the machine data";
                var ours = new[] { Ids.RawCashmere, Ids.RawAngora, Ids.MerinoWool }.Where(i => !spinner.OutputRules.Any(r => r.Id == Ids.P + i)).ToList();
                return ours.Count == 0 ? null : "no spinner rule for " + string.Join(", ", ours);
            });
        }

        /*********
        ** Helpers
        *********/
        private static void Machine(string name, string machineId, Item input, Item extra, Vector2 tile, Func<string, bool> expect)
        {
            Check(name, () =>
            {
                SObject m = ItemRegistry.Create<SObject>(Ids.BC(machineId));
                PlaceObject(m, tile);
                bool added = false;
                int slot = -1;
                Item parked = null;
                if (extra != null)
                {
                    // a full inventory just means borrowing the last slot for a moment
                    slot = Game1.player.Items.IndexOf(null);
                    if (slot < 0)
                    {
                        slot = Game1.player.Items.Count - 1;
                        parked = Game1.player.Items[slot];
                    }
                    Game1.player.Items[slot] = extra;
                    added = true;
                }
                try
                {
                    bool ok = m.performObjectDropInAction(input, false, Game1.player);
                    string got = m.heldObject.Value?.QualifiedItemId;
                    if (!ok || got == null)
                        return "the machine refused the input";
                    return expect(got) ? null : $"made {got}";
                }
                finally
                {
                    if (added)
                        Game1.player.Items[slot] = parked;
                }
            });
        }

        private static void MachineRejects(string name, string machineId, Item input, Vector2 tile)
        {
            Check(name, () =>
            {
                SObject m = ItemRegistry.Create<SObject>(Ids.BC(machineId));
                PlaceObject(m, tile);
                bool ok = m.performObjectDropInAction(input, false, Game1.player);
                return !ok && m.heldObject.Value == null ? null : $"it accepted it and made {m.heldObject.Value?.QualifiedItemId}";
            });
        }

        private static Contract TestContract(string goods, int minTier, int qty, int reward)
        {
            var c = new Contract
            {
                Id = ModEntry.State.NextContractId++,
                BuyerId = "Vellier",
                MaterialId = goods,
                MinTier = minTier,
                Quantity = qty,
                PostedDay = Game1.Date.TotalDays,
                DeadlineDay = Game1.Date.TotalDays + 7,
                Reward = reward
            };
            ModEntry.State.Contracts.Add(c);
            return c;
        }

        private static FarmAnimal NewAnimal(string type)
            => new(type, Game1.Multiplayer.getNewID(), Game1.player.UniqueMultiplayerID);

        private static void PlaceObject(SObject o, Vector2 tile)
        {
            if (farm.objects.ContainsKey(tile))
                throw new InvalidOperationException($"tile {tile} is taken");
            o.TileLocation = tile;
            farm.objects.Add(tile, o);
            o.Location = farm;
            placedObjects.Add(tile);
        }

        private static void PlaceDirt(Vector2 tile, bool watered)
        {
            if (farm.terrainFeatures.ContainsKey(tile))
                return;
            var dirt = new HoeDirt(watered ? HoeDirt.watered : HoeDirt.dry, farm);
            farm.terrainFeatures.Add(tile, dirt);
            placedDirt.Add(tile);
        }

        private static Building PlaceBuilding(string type, int dx, int dy)
        {
            Building b = Building.CreateInstanceFromId(type, new Vector2(origin.X + dx, origin.Y + dy));
            farm.buildings.Add(b);
            b.ReloadBuildingData(false, true);   // the step the game runs when Robin starts construction: this is what creates storage chests
            b.FinishConstruction();
            placedBuildings.Add(b);
            return b;
        }

        private static bool FindFreeArea(int w, int h, out Point at)
        {
            int width = farm.Map.Layers[0].LayerWidth, height = farm.Map.Layers[0].LayerHeight;
            for (int y = 4; y < height - h - 4; y++)
                for (int x = 4; x < width - w - 4; x++)
                {
                    bool free = true;
                    for (int yy = 0; yy < h && free; yy++)
                        for (int xx = 0; xx < w && free; xx++)
                        {
                            var t = new Vector2(x + xx, y + yy);
                            if (farm.objects.ContainsKey(t) || farm.terrainFeatures.ContainsKey(t) || !farm.CanItemBePlacedHere(t))
                                free = false;
                        }
                    if (free)
                    {
                        at = new Point(x, y);
                        return true;
                    }
                }
            at = Point.Zero;
            return false;
        }

        private static void Cleanup()
        {
            foreach (Vector2 t in placedObjects)
                farm?.objects.Remove(t);
            foreach (Vector2 t in placedDirt)
                farm?.terrainFeatures.Remove(t);
            foreach (Building b in placedBuildings)
                farm?.buildings.Remove(b);
            placedObjects.Clear();
            placedDirt.Clear();
            placedBuildings.Clear();
            Visitors.Despawn();
            Helper_InvalidateSensorLook();
        }

        private static void Helper_InvalidateSensorLook()
        {
            try { helper.GameContent.InvalidateCache("Data/BigCraftables"); }
            catch { /* cosmetic */ }
        }

        private static void CloseDialogue()
        {
            Game1.activeClickableMenu = null;
            Game1.dialogueUp = false;
            Game1.afterDialogues = null;
            Game1.currentSpeaker = null;
        }

        private static string Missing(IEnumerable<string> qualifiedIds)
        {
            var missing = qualifiedIds.Where(id => !ItemRegistry.Exists(id)).ToList();
            return missing.Count == 0 ? null : "missing: " + string.Join(", ", missing);
        }

        private static string Absent(IEnumerable<string> keys, Func<string, bool> has)
        {
            var missing = keys.Where(k => !has(k)).ToList();
            return missing.Count == 0 ? null : "missing: " + string.Join(", ", missing);
        }

        private static void Check(string name, Func<string> body)
        {
            try
            {
                string problem = body();
                if (problem == null)
                    Pass(name);
                else if (problem.StartsWith("SKIP"))
                    Log($"SKIP {name}: {problem[5..].Trim()}");
                else
                    Fail(name, problem);
            }
            catch (Exception ex)
            {
                Fail(name, $"{ex.GetType().Name}: {ex.Message} @ {ex.StackTrace?.Split('\n').FirstOrDefault()?.Trim()}");
            }
        }

        private static void Pass(string name)
        {
            passed++;
            Log("PASS " + name);
        }

        private static void Fail(string name, string why)
        {
            failed++;
            string line = $"FAIL {name}: {why}";
            results.Add(line);
            monitor.Log(line, LogLevel.Error);
        }

        private static void Log(string line)
        {
            results.Add(line);
            monitor.Log(line, LogLevel.Info);
        }
    }
}
