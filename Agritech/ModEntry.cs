using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;

namespace Agritech
{
    /// <summary>
    /// Agritech: Hub tiers and the Agritech skill, paired sensor posts and collars, soil, virtual fences, drones,
    /// new breeds, leather and Wagyu chains, grading with provenance, contracts with fashion and restaurant houses,
    /// freight and cold storage, waste-to-fuel, horse husbandry, and farm visitors.
    /// </summary>
    public class ModEntry : Mod
    {
        public const string MadeKey = "Ali.Agritech/MadeDay";
        public const string ProvenanceKey = "Ali.Agritech/Provenance";
        public const string ContractsMail = "Ali.Agritech_Contracts";
        private const string SaveKey = "agritech-state";

        public static SaveModel State { get; private set; } = new();
        internal static ModConfig Config { get; private set; }
        internal static void SetConfig(ModConfig config) => Config = config;

        private bool overlayOn;
        private int lastSensorSprite = -1;
        private bool drawingFence;
        private Vector2? fenceCorner;
        private Texture2D itemsTexture;

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();

            GameStateQuery.Register("Ali.Agritech_SKILL", Skill.SkillQuery);
            Polish.PatchTooltips(ModManifest.UniqueID);
            Herd.Patch(ModManifest.UniqueID);

            // console: "agritech" shows the skill; "agritech_xp <amount>" adds skill xp
            helper.ConsoleCommands.Add("agritech", "Show your Agritech skill level, xp and what it has unlocked.", (_, _) =>
            {
                if (!Context.IsWorldReady) { Monitor.Log("Load a save first.", LogLevel.Info); return; }
                int lvl = Skill.Level();
                Monitor.Log($"Agritech skill level {lvl}, {State.SkillXp} xp{(lvl < 10 ? $" (next level at {Skill.XpForNext(lvl)})" : "")}. Hub tier {FarmData.HubTier()}.", LogLevel.Info);
                Monitor.Log("Marnie sells: Merino at level 1; Cashmere Goat, Angora Rabbit and the three horses at 2; Wagyu at 3.", LogLevel.Info);
            });
            helper.ConsoleCommands.Add("agritech_selftest", "Run every Agritech feature once and report pass/fail. Use a throwaway save: it adds buildings, items, money and XP. Usage: agritech_selftest run", (_, args) =>
            {
                if (!Context.IsWorldReady) { Monitor.Log("Load a save first.", LogLevel.Info); return; }
                if (args.Length < 1 || args[0] != "run")
                {
                    Monitor.Log("This changes the save it runs on (buildings, items, money, XP). Load a throwaway save, then type: agritech_selftest run", LogLevel.Warn);
                    return;
                }
                SelfTest.Start(Helper, Monitor);
            });
            helper.Events.GameLoop.UpdateTicked += (_, e) => SelfTest.Tick();
            helper.ConsoleCommands.Add("agritech_xp", "Add Agritech skill xp. Usage: agritech_xp <amount>", (_, args) =>
            {
                if (!Context.IsWorldReady) { Monitor.Log("Load a save first.", LogLevel.Info); return; }
                if (args.Length < 1 || !int.TryParse(args[0], out int amount) || amount <= 0) { Monitor.Log("Usage: agritech_xp <amount>", LogLevel.Info); return; }
                Skill.AddXp(amount);
                Monitor.Log($"Agritech skill is now level {Skill.Level()} ({State.SkillXp} xp).", LogLevel.Info);
            });

            helper.Events.Content.AssetRequested += (_, e) => ContentRegistry.Handle(e, helper);
            helper.Events.GameLoop.GameLaunched += (_, _) => { Integration.Init(helper, Monitor); ConfigMenu.Register(helper, ModManifest, Monitor); };
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.Saving += OnSaving;
            helper.Events.GameLoop.DayEnding += OnDayEnding;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.ReturnedToTitle += (_, _) => { State = new SaveModel(); Visitors.Despawn(); };
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Display.RenderedWorld += OnRenderedWorld;
            helper.Events.Player.InventoryChanged += (_, e) => { if (e.IsLocalPlayer) Logistics.StampPerishables(e.Added); };
        }

        /*********
        ** Save data
        *********/
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            State = (Context.IsMainPlayer ? Helper.Data.ReadSaveData<SaveModel>(SaveKey) : null) ?? new SaveModel();
            Skill.GrantRecipes();
            // register the horses that already exist, so only new stables count as foals
            foreach (Horse h in Livestock.AllHorses())
                Livestock.Record(h);
            itemsTexture = Helper.GameContent.Load<Texture2D>(ContentRegistry.ItemsTexture);
        }

        private void OnSaving(object sender, SavingEventArgs e)
        {
            Visitors.Despawn();
            if (Context.IsMainPlayer)
                Helper.Data.WriteSaveData(SaveKey, State);
        }

        /*********
        ** Daily cycle
        *********/
        private void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            Visitors.Despawn();
            if (!Context.IsMainPlayer)
                return;
            BalanceDiary.WriteDay(Helper);
            FarmData.UpdateSoilOvernight(State);
            Logistics.ColdStoreTick();
            Livestock.NightlyHorses();
            State.HostedVisits.RemoveWhere(k => !k.EndsWith("|" + Game1.Date.TotalDays));
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            if (!Context.IsMainPlayer)
                return;
            var rng = Utility.CreateDaySaveRandom(7711);

            // sensor posts change look with the Hub tier
            int sensorSprite = ContentRegistry.SensorSprite();
            if (sensorSprite != lastSensorSprite)
            {
                lastSensorSprite = sensorSprite;
                Helper.GameContent.InvalidateCache("Data/BigCraftables");
            }

            Livestock.ClaimFoals();
            foreach (Horse h in Livestock.AllHorses())
                Livestock.Record(h);

            // collar logging
            var animals = FarmData.AllAnimals().ToDictionary(p => p.animal.myID.Value, p => p.animal);
            foreach (long id in State.Collars.Keys.ToList())
            {
                if (!animals.TryGetValue(id, out FarmAnimal a))
                {
                    State.Collars.Remove(id);
                    continue;
                }
                CollarRecord rec = State.Collars[id];
                rec.Name = a.displayName;
                rec.DaysTracked++;
                if (!string.IsNullOrEmpty(a.currentProduce.Value))
                    rec.ProductsLogged++;
                rec.HealthHistory.Add(FarmData.HealthScore(a));
                if (rec.HealthHistory.Count > 28)
                    rec.HealthHistory.RemoveAt(0);
            }

            Livestock.SpawnManure(rng);
            ContractBook.DailyUpdate();
            if (Game1.Date.DayOfWeek == DayOfWeek.Monday)
                ContractBook.PostWeekly(rng);
            Drones.MorningRun(State);
            Visitors.MorningSpawn(Helper, rng);

            int tier = FarmData.HubTier();
            if (tier >= 2 && Config.MorningAlerts)
                SendMorningAlerts(tier, animals);
        }

        private void SendMorningAlerts(int tier, Dictionary<long, FarmAnimal> animals)
        {
            int dry = 0, depleted = 0;
            foreach (var (loc, tile) in FarmData.SensorPosts())
            {
                if (Game1.IsRainingHere(loc) && loc.IsOutdoors)
                    continue;
                SensorReading r = FarmData.Read(State, loc, tile, tier);
                if (r.TilledTiles > 0 && r.Moisture < 0.5f)
                    dry++;
                if (r.AverageNutrients < FarmData.LowNutrient)
                    depleted++;
            }
            if (dry > 0)
                Game1.addHUDMessage(new HUDMessage($"Agritech: {dry} sensor zone(s) reading dry.", HUDMessage.newQuest_type));
            if (depleted > 0)
                Game1.addHUDMessage(new HUDMessage($"Agritech: {depleted} sensor zone(s) low on nutrients.", HUDMessage.newQuest_type));
            int inHeat = State.Collars.Keys.Count(id => animals.TryGetValue(id, out var a) && FarmData.InHeat(a));
            if (inHeat > 0)
                Game1.addHUDMessage(new HUDMessage($"Agritech: {inHeat} collared animal(s) in heat today.", HUDMessage.newQuest_type));
            int unwell = State.Collars.Keys.Count(id => animals.TryGetValue(id, out var a) && FarmData.HealthScore(a) < 40);
            if (unwell > 0)
                Game1.addHUDMessage(new HUDMessage($"Agritech: {unwell} collared animal(s) unwell.", HUDMessage.error_type));
        }

        /*********
        ** Ticks: fence herding (needs a herding drone), horse training
        *********/
        private bool herdingCached;

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            if (e.IsMultipleOf(10))
                Livestock.TickRiding();
            if (e.IsMultipleOf(15))
                if (Config.TractorNeedsFuel)
                    Polish.TickTractor();
            if (!Context.IsMainPlayer)
                return;
            if (e.IsMultipleOf(180))
                Visitors.Wander(Game1.random);
            if (e.IsMultipleOf(600))
                herdingCached = Drones.HerdingActive();
            if (herdingCached && e.IsMultipleOf(120))
                Polish.Herd(State, Game1.random);
        }

        /*********
        ** Input
        *********/
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (drawingFence && e.Button.IsUseToolButton())
            {
                Helper.Input.Suppress(e.Button);
                HandleFenceClick(e.Cursor.Tile);
                return;
            }
            if (!Context.IsPlayerFree)
                return;

            if (e.Button == Config.TerminalKey) { OpenTerminal(); return; }
            if (e.Button == Config.ContractsKey) { OpenBoard(); return; }
            if (e.Button == Config.OverlayKey)
            {
                if (RequireTier(3, "the heat-map overlay"))
                    overlayOn = !overlayOn;
                return;
            }
            if (e.Button == Config.FenceKey)
            {
                if (!RequireTier(3, "virtual fences"))
                    return;
                if (Game1.currentLocation is not Farm)
                {
                    Game1.addHUDMessage(new HUDMessage("Virtual fences can only be drawn on the farm.", HUDMessage.error_type));
                    return;
                }
                drawingFence = true;
                fenceCorner = null;
                Game1.addHUDMessage(new HUDMessage("Fence mode: click the first corner of the zone.", HUDMessage.newQuest_type));
                return;
            }
            if (e.Button == Config.CycleZoneKey) { CycleZone(); return; }

            if (!e.Button.IsActionButton() && !e.Button.IsUseToolButton())
                return;

            Vector2 pixel = e.Cursor.AbsolutePixels;
            string held = Game1.player.ActiveObject?.QualifiedItemId;

            // visitors
            NPC visitor = Visitors.VisitorAt(pixel);
            if (visitor != null && e.Button.IsActionButton())
            {
                Helper.Input.Suppress(e.Button);
                string filter = visitor.modData.TryGetValue("Ali.Agritech/Visitor", out string who) && Buyer.Get(who) != null ? who : null;
                Visitors.Talk(visitor, () => OpenBoard(filter));
                return;
            }

            // hand delivery to Gus or Emily
            if (e.Button.IsActionButton() && Polish.TryHandDeliver(e.Cursor.GrabTile))
            {
                Helper.Input.Suppress(e.Button);
                return;
            }

            // held tools
            if (held == ContentRegistry.CollarQualified || held == Ids.O(Ids.ButcherTag))
            {
                FarmAnimal target = AnimalAt(pixel);
                if (target != null)
                {
                    Helper.Input.Suppress(e.Button);
                    if (held == ContentRegistry.CollarQualified)
                        ApplyCollar(target);
                    else
                        Livestock.AskRetire(target);
                    return;
                }
            }
            // herd horses: oat feed, brush, saddle
            if (held == Ids.O(Ids.OatFeed) || held == Ids.O(Ids.Brush) || held == Ids.O(Ids.Saddle))
            {
                FarmAnimal target = AnimalAt(pixel);
                if (target != null && Herd.TryInteract(target, held))
                {
                    Helper.Input.Suppress(e.Button);
                    return;
                }
            }
            if (held == Ids.O(Ids.Brush))
            {
                Horse h = Livestock.HorseAt(pixel);
                if (h != null)
                {
                    Helper.Input.Suppress(e.Button);
                    Livestock.Groom(h);
                    return;
                }
            }

            // clicking our buildings
            if (e.Button.IsActionButton() && Game1.currentLocation is Farm farm)
            {
                Building b = farm.getBuildingAt(e.Cursor.Tile);
                if (b != null && !b.isUnderConstruction())
                {
                    string type = b.buildingType.Value ?? "";
                    if (type.StartsWith(Ids.HubPrefix)) { Helper.Input.Suppress(e.Button); OpenTerminal(); return; }
                    if (type == Ids.FreightDepot) { Helper.Input.Suppress(e.Button); OpenBoard(); return; }
                }
            }
        }

        private static bool RequireTier(int tier, string feature)
        {
            if (FarmData.HubTier() >= tier)
                return true;
            Game1.addHUDMessage(new HUDMessage($"Needs an Agritech Hub of tier {tier} for {feature}.", HUDMessage.error_type));
            return false;
        }

        private static FarmAnimal AnimalAt(Vector2 pixel)
        {
            foreach (var pair in Game1.currentLocation.animals.Pairs)
            {
                Rectangle box = pair.Value.GetBoundingBox();
                box.Inflate(16, 32);
                if (box.Contains((int)pixel.X, (int)pixel.Y))
                    return pair.Value;
            }
            return null;
        }

        private static void ApplyCollar(FarmAnimal a)
        {
            long id = a.myID.Value;
            if (State.Collars.ContainsKey(id))
            {
                Game1.addHUDMessage(new HUDMessage($"{a.displayName} is already collared.", HUDMessage.error_type));
                return;
            }
            State.Collars[id] = new CollarRecord { AnimalId = id, Name = a.displayName, Type = a.type.Value, CollaredOnDay = Game1.Date.TotalDays };
            Game1.player.reduceActiveItemByOne();
            Game1.playSound("newArtifact");
            Skill.AddXp(25);
            Game1.addHUDMessage(new HUDMessage($"{a.displayName} is now collared.", HUDMessage.achievement_type));
        }

        private void HandleFenceClick(Vector2 tile)
        {
            if (fenceCorner == null)
            {
                fenceCorner = tile;
                Game1.addHUDMessage(new HUDMessage("Now click the opposite corner.", HUDMessage.newQuest_type));
                return;
            }
            Vector2 a = fenceCorner.Value;
            var zone = new FenceZone
            {
                Name = $"Zone {State.Zones.Count + 1}",
                X = (int)Math.Min(a.X, tile.X), Y = (int)Math.Min(a.Y, tile.Y),
                Width = (int)Math.Abs(a.X - tile.X) + 1, Height = (int)Math.Abs(a.Y - tile.Y) + 1
            };
            State.Zones.Add(zone);
            State.ActiveZone = State.Zones.Count - 1;
            drawingFence = false;
            fenceCorner = null;
            Game1.playSound("drumkit6");
            string drone = Drones.HerdingActive() ? "" : " Herding starts once a Herding Drone is docked.";
            Game1.addHUDMessage(new HUDMessage($"{zone.Name} drawn ({zone.Width}x{zone.Height}) and set active.{drone}", HUDMessage.achievement_type));
        }

        private static void CycleZone()
        {
            if (State.Zones.Count == 0)
            {
                Game1.addHUDMessage(new HUDMessage("No fence zones drawn yet.", HUDMessage.error_type));
                return;
            }
            State.ActiveZone++;
            if (State.ActiveZone >= State.Zones.Count)
                State.ActiveZone = -1;
            Game1.addHUDMessage(new HUDMessage(State.ActiveZone < 0 ? "Herding off." : $"Herding into {State.Zones[State.ActiveZone].Name}.", HUDMessage.newQuest_type));
        }

        private static void OpenBoard(string buyer = null)
        {
            if (FarmData.HubTier() < 1)
            {
                Game1.addHUDMessage(new HUDMessage("Build an Agritech Hub to start taking contracts.", HUDMessage.error_type));
                return;
            }
            Game1.activeClickableMenu = new ContractsMenu(buyer);
        }

        /*********
        ** Terminal
        *********/
        internal static void OpenTerminal()
        {
            int tier = FarmData.HubTier();
            var lines = new List<(string, Color)>();
            Color head = new(60, 40, 140);
            Color text = Game1.textColor;
            Color warn = new(170, 40, 30);
            Color muted = new(110, 90, 70);

            int level = Skill.Level();
            string next = level >= 10 ? "max level" : $"{State.SkillXp}/{Skill.XpForNext(level)} xp";
            lines.Add(($"Hub tier {tier}   Agritech skill {level} ({next})   Collared {State.Collars.Count}", head));
            if (tier == 0)
            {
                lines.Add(("No Agritech Hub on the farm yet. Build one at Robin's to connect sensors, collars and buyers.", warn));
                Game1.activeClickableMenu = new TerminalMenu(lines);
                return;
            }
            lines.Add(("", text));

            // sensors
            var posts = FarmData.SensorPosts();
            lines.Add(($"SENSOR POSTS ({posts.Count}), radius {FarmData.SensorRadius(tier)}{(Drones.SurveyActive() ? ", survey drone covering the whole farm" : "")}", head));
            if (posts.Count == 0)
                lines.Add(("  None placed.", muted));
            foreach (var (loc, tile) in posts)
            {
                SensorReading r = FarmData.Read(State, loc, tile, tier);
                string where = $"  {loc.DisplayName} ({(int)tile.X},{(int)tile.Y}): {r.TilledTiles} tilled";
                if (tier >= 2)
                {
                    where += $", moisture {r.Moisture:P0}, nutrients {r.AverageNutrients:P0}";
                    bool bad = (r.TilledTiles > 0 && r.Moisture < 0.5f) || r.AverageNutrients < FarmData.LowNutrient;
                    lines.Add((where, bad ? warn : text));
                    if (r.LowNutrientTiles > 0)
                        lines.Add(($"    {r.LowNutrientTiles} tile(s) depleted", warn));
                }
                else
                    lines.Add((where, text));
            }
            lines.Add(("", text));

            // horse herd
            var herd = Herd.AllHerd().ToList();
            if (herd.Count > 0)
            {
                lines.Add(($"HORSE HERD ({herd.Count}, {herd.Count(Herd.IsTrained)} saddle-trained)", head));
                foreach (FarmAnimal h in herd.OrderBy(h => h.displayName))
                {
                    string age = h.isAdult() ? "adult" : "foal";
                    lines.Add(($"  {h.displayName} ({h.type.Value.Substring(Ids.HorsePrefix.Length).ToLower()} {age}): {h.friendshipTowardFarmer.Value / 200} hearts{(Herd.IsTrained(h) ? ", trained" : "")}, worth {h.getSellPrice()}g", text));
                }
                lines.Add(("", text));
            }

            // collars
            var animals = FarmData.AllAnimals().ToDictionary(p => p.animal.myID.Value, p => p);
            lines.Add(($"COLLARED ANIMALS ({State.Collars.Count})", head));
            if (State.Collars.Count == 0)
                lines.Add(("  None yet. Craft a Smart Collar and click an animal while holding it.", muted));
            foreach (CollarRecord rec in State.Collars.Values.OrderBy(r => r.Type).ThenBy(r => r.Name))
            {
                if (!animals.TryGetValue(rec.AnimalId, out var found))
                    continue;
                FarmAnimal a = found.animal;
                string line = $"  {rec.Name} the {a.displayType}: {found.loc.DisplayName} ({(int)a.Tile.X},{(int)a.Tile.Y})";
                Color c = text;
                if (tier >= 2)
                {
                    int health = FarmData.HealthScore(a);
                    int toHeat = FarmData.DaysToHeat(a);
                    line += $", health {health}, " + (!a.isAdult() ? "not yet adult" : toHeat == 0 ? "IN HEAT" : $"heat in {toHeat}d");
                    if (health < 40) c = warn;
                }
                lines.Add((line, c));
                if (tier >= 4)
                {
                    string trend = rec.HealthHistory.Count >= 2 ? (rec.HealthHistory[^1] - rec.HealthHistory[0] >= 0 ? "steady or rising" : "falling") : "not enough data";
                    lines.Add(($"    {rec.ProductsLogged} products in {rec.DaysTracked} days, forecast {FarmData.WeeklyForecast(rec):0.0}/week, health {trend}", muted));
                }
            }

            // drones and fences
            if (tier >= 3)
            {
                lines.Add(("", text));
                var fleet = Drones.Types.Where(Drones.Has).Select(d => d.Replace(Ids.P, "").Replace("Drone", "")).ToList();
                lines.Add(($"DRONES: {(fleet.Count == 0 ? "none docked" : string.Join(", ", fleet))}   actions this morning {State.DroneActionsToday}", head));
                lines.Add(($"VIRTUAL FENCES ({State.Zones.Count}){(Drones.HerdingActive() ? "" : "  - dock a Herding Drone to enforce them")}", head));
                for (int i = 0; i < State.Zones.Count; i++)
                {
                    FenceZone z = State.Zones[i];
                    lines.Add(($"  {z.Name}: ({z.X},{z.Y}) {z.Width}x{z.Height}" + (i == State.ActiveZone ? "  ACTIVE" : ""), text));
                }
            }

            // horses
            var horses = Livestock.AllHorses().Select(Livestock.Record).ToList();
            if (horses.Count > 0)
            {
                lines.Add(("", text));
                lines.Add(($"HORSES ({horses.Count}){(State.PendingFoals > 0 ? $"  - {State.PendingFoals} foal(s) waiting for a stable" : "")}", head));
                foreach (HorseRecord h in horses)
                    lines.Add(($"  {h.Name}: bond {h.Bond}/100, training {h.TrainingLevel}/5, foals {h.Foals}{(h.LastGroomedDay == Game1.Date.TotalDays ? ", groomed today" : "")}", text));
            }

            // trade
            lines.Add(("", text));
            int open = State.Contracts.Count(c => c.Status == "Open");
            lines.Add(($"TRADE: {open} open contract(s), {State.Shipments.Count} in transit. Freight Depot: {(Logistics.HasFreightDepot() ? "yes" : "no")}", head));
            foreach (Buyer b in Buyer.All.Where(b => b.Unlocked()))
                lines.Add(($"  {b.Name}: {ContractBook.RelLabel(ContractBook.Rel(b.Id))} ({ContractBook.Rel(b.Id)}){(b.VisitDay > 0 ? $", visits on day {b.VisitDay}" : "")}", text));
            var locked = Buyer.All.Where(b => !b.Unlocked()).ToList();
            if (locked.Count > 0)
                lines.Add(($"  Not yet trading with: {string.Join(", ", locked.Select(b => b.Name))}", muted));

            // provenance
            if (State.ProvenanceLedger.Count > 0)
            {
                lines.Add(("", text));
                lines.Add(("PROVENANCE LEDGER (latest)", head));
                foreach (string cert in State.ProvenanceLedger.AsEnumerable().Reverse().Take(10))
                    lines.Add(($"  {cert}", muted));
            }

            Game1.activeClickableMenu = new TerminalMenu(lines);
        }

        /*********
        ** Drawing: heat map, fence outlines, drones
        *********/
        private void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            var b = e.SpriteBatch;
            GameLocation here = Game1.currentLocation;

            if (overlayOn)
            {
                int tier = FarmData.HubTier();
                var tiles = new List<Vector2>();
                if (here is Farm && Drones.SurveyActive())
                    tiles.AddRange(here.terrainFeatures.Pairs.Where(p => p.Value is StardewValley.TerrainFeatures.HoeDirt).Select(p => p.Key));
                foreach (var (loc, post) in FarmData.SensorPosts())
                    if (loc == here)
                        tiles.AddRange(FarmData.TilesAround(post, FarmData.SensorRadius(tier)));
                foreach (Vector2 tile in tiles.Distinct())
                {
                    if (!here.terrainFeatures.TryGetValue(tile, out var tf) || tf is not StardewValley.TerrainFeatures.HoeDirt dirt)
                        continue;
                    float n = FarmData.GetNutrients(State, here, tile);
                    bool wet = dirt.state.Value == StardewValley.TerrainFeatures.HoeDirt.watered;
                    Color c = Color.Lerp(Color.Red, Color.LimeGreen, n);
                    if (!wet)
                        c = Color.Lerp(c, Color.DodgerBlue, 0.45f);
                    DrawTile(b, tile, c * 0.35f);
                }
            }

            if (here is Farm && State.Zones.Count > 0 && (overlayOn || drawingFence))
                for (int i = 0; i < State.Zones.Count; i++)
                    DrawZone(b, State.Zones[i], i == State.ActiveZone ? Color.Gold : Color.White);
            if (drawingFence && fenceCorner != null)
                DrawTile(b, fenceCorner.Value, Color.Gold * 0.6f);

            if (Config.ShowDockedDrones)
                Drones.Draw(b, itemsTexture);
        }

        private static void DrawTile(SpriteBatch b, Vector2 tile, Color color)
        {
            Vector2 pos = Game1.GlobalToLocal(Game1.viewport, tile * 64f);
            b.Draw(Game1.staminaRect, new Rectangle((int)pos.X, (int)pos.Y, 64, 64), color);
        }

        private static void DrawZone(SpriteBatch b, FenceZone z, Color color)
        {
            Vector2 pos = Game1.GlobalToLocal(Game1.viewport, new Vector2(z.X * 64f, z.Y * 64f));
            int w = z.Width * 64, h = z.Height * 64, t = 4;
            var tex = Game1.staminaRect;
            b.Draw(tex, new Rectangle((int)pos.X, (int)pos.Y, w, t), color);
            b.Draw(tex, new Rectangle((int)pos.X, (int)pos.Y + h - t, w, t), color);
            b.Draw(tex, new Rectangle((int)pos.X, (int)pos.Y, t, h), color);
            b.Draw(tex, new Rectangle((int)pos.X + w - t, (int)pos.Y, t, h), color);
        }
    }
}
