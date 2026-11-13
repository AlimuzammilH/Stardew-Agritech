using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace Agritech
{
    /// <summary>A buyer: a fashion house, a restaurant house, or a local.</summary>
    internal class Buyer
    {
        public string Id;
        public string Name;
        public string Kind;          // Fashion, Restaurant, Local
        public string City;
        public string[] Wants;       // goods ids
        public int SkillNeeded;
        public int HubNeeded;
        public int VisitDay;         // day of month the buyer visits the farm (0 = never)
        public string Greeting;

        public static readonly List<Buyer> All = new()
        {
            new() { Id = "Vellier", Name = "Maison Vellier", Kind = "Fashion", City = "Paris", Wants = new[] { "Cashmere", "Silk", "Merino", "Leather" }, SkillNeeded = 4, HubNeeded = 2, VisitDay = 8,
                Greeting = "Maison Vellier sends its regards. We buy only what we can trace to the animal. Show me your herd's records and we will talk." },
            new() { Id = "Ardenti", Name = "Casa Ardenti", Kind = "Fashion", City = "Milan", Wants = new[] { "Leather", "Merino", "Wool", "Alpaca" }, SkillNeeded = 4, HubNeeded = 2, VisitDay = 15,
                Greeting = "Casa Ardenti. Leather and fine wool are our whole house. If the hide is right, the price is right." },
            new() { Id = "Halvorsen", Name = "Halvorsen & Lund", Kind = "Fashion", City = "Stockholm", Wants = new[] { "Alpaca", "Llama", "Angora", "Wool", "Merino" }, SkillNeeded = 4, HubNeeded = 2, VisitDay = 22,
                Greeting = "Halvorsen & Lund. Our winters need honest fibre. Warm, light, traceable. That's the brief." },
            new() { Id = "Koen", Name = "Kōen", Kind = "Restaurant", City = "Zuzu City", Wants = new[] { "Wagyu", "AgedWagyu", "WagyuCut", "WagyuSteak", "WagyuTasting" }, SkillNeeded = 3, HubNeeded = 1, VisitDay = 12,
                Greeting = "Kōen, from Zuzu City. Twelve seats, one grill. We serve your Wagyu or we serve nothing." },
            new() { Id = "BrassPheasant", Name = "The Brass Pheasant", Kind = "Restaurant", City = "Zuzu City", Wants = new[] { "BeefCut", "WagyuCut", "Cheese", "Milk", "BeefStew", "WagyuSteak" }, SkillNeeded = 3, HubNeeded = 1, VisitDay = 19,
                Greeting = "The Brass Pheasant. A big room and a long menu. We need steady supply more than anything." },
            new() { Id = "Gus", Name = "Gus (Stardrop Saloon)", Kind = "Local", City = "Pelican Town", Wants = new[] { "BeefCut", "WagyuCut", "Cheese", "Milk", "BeefStew" }, SkillNeeded = 0, HubNeeded = 1, VisitDay = 0 },
            new() { Id = "Emily", Name = "Emily", Kind = "Local", City = "Pelican Town", Wants = new[] { "Wool", "Cloth", "Angora", "Merino" }, SkillNeeded = 0, HubNeeded = 1, VisitDay = 0 }
        };

        public static Buyer Get(string id) => All.Find(b => b.Id == id);

        public bool Unlocked() => Skill.Level() >= SkillNeeded && FarmData.HubTier() >= HubNeeded;
    }

    /// <summary>Something a contract can ask for.</summary>
    internal class Goods
    {
        public string Id;
        public string Name;
        public int BasePrice;
        public bool Perishable;
        public Material Material;    // set for graded materials
        public string[] Accepted;    // qualified ids for ungraded goods

        public static readonly List<Goods> All = BuildAll();

        private static List<Goods> BuildAll()
        {
            var list = Material.All.Select(m => new Goods { Id = m.Id, Name = m.Name, BasePrice = m.BasePrice, Perishable = m.Perishable, Material = m }).ToList();
            list.Add(new Goods { Id = "WagyuCut", Name = "Wagyu Cut", BasePrice = 900, Perishable = true, Accepted = new[] { Ids.O(Ids.WagyuCut) } });
            list.Add(new Goods { Id = "AgedWagyu", Name = "Dry-Aged Wagyu", BasePrice = 1400, Perishable = true, Accepted = new[] { Ids.O(Ids.AgedWagyu) } });
            list.Add(new Goods { Id = "BeefCut", Name = "Beef Cut", BasePrice = 320, Perishable = true, Accepted = new[] { Ids.O(Ids.BeefCut) } });
            list.Add(new Goods { Id = "Cheese", Name = "Cheese", BasePrice = 230, Accepted = new[] { Ids.VCheese, Ids.VGoatCheese } });
            list.Add(new Goods { Id = "Milk", Name = "Milk", BasePrice = 125, Perishable = true, Accepted = new[] { Ids.VMilk, Ids.VLargeMilk, Ids.VGoatMilk, Ids.VLargeGoatMilk } });
            list.Add(new Goods { Id = "Cloth", Name = "Cloth", BasePrice = 470, Accepted = new[] { Ids.VCloth } });
            list.Add(new Goods { Id = "WagyuSteak", Name = "Wagyu Steak", BasePrice = 1800, Accepted = new[] { Ids.O(Ids.WagyuSteak) } });
            list.Add(new Goods { Id = "BeefStew", Name = "Beef Stew", BasePrice = 650, Accepted = new[] { Ids.O(Ids.BeefStew) } });
            list.Add(new Goods { Id = "WagyuTasting", Name = "Wagyu Tasting", BasePrice = 3200, Accepted = new[] { Ids.O(Ids.WagyuTasting) } });
            return list;
        }

        public static Goods Get(string id) => All.Find(g => g.Id == id);

        /// <summary>Whether an item satisfies a contract for this good at the given minimum tier.</summary>
        public bool Accepts(Item item, int minTier)
        {
            if (item == null)
                return false;
            string q = item.QualifiedItemId;
            if (Material != null)
            {
                if (Material.TryParseGraded(q, out Material m, out int tier))
                    return m == Material && tier >= Math.Max(1, minTier);
                // raw inputs only count for untiered contracts
                return minTier == 0 && Material.ForInput(q, item.Name) == Material;
            }
            return Array.IndexOf(Accepted, q) >= 0;
        }
    }

    /// <summary>Transport options for shipping a contract.</summary>
    internal static class Transport
    {
        public static readonly string[] Modes = { "Cart", "Rail", "Truck", "Air" };

        public static int Days(string mode) => mode switch { "Cart" => 3, "Rail" => 2, "Truck" => 1, "Air" => 1, "Drone" => 1, "Hand" => 0, _ => 3 };

        /// <summary>Cost as a fraction of contract value.</summary>
        public static float CostFraction(string mode) => mode switch { "Rail" => 0.02f, "Air" => 0.10f, _ => 0f };

        public static bool Available(string mode, out string why)
        {
            why = null;
            switch (mode)
            {
                case "Cart":
                    return true;
                case "Rail":
                case "Truck":
                    if (!Logistics.HasFreightDepot()) { why = "Needs a Freight Depot"; return false; }
                    if (mode == "Truck" && !Game1.player.Items.Any(i => i?.QualifiedItemId == Ids.O(Ids.Fuel))) { why = "Needs 1 Synthetic Fuel"; return false; }
                    return true;
                case "Air":
                    if (FarmData.HubTier() < 4) { why = "Needs Hub tier 4"; return false; }
                    return true;
            }
            return false;
        }
    }

    internal static class ContractBook
    {
        private static SaveModel S => ModEntry.State;

        public static int Rel(string buyer) => S.Relationships.TryGetValue(buyer, out int r) ? r : 0;

        public static void AddRel(string buyer, int delta)
            => S.Relationships[buyer] = Math.Clamp(Rel(buyer) + delta, 0, 1000);

        public static string RelLabel(int r) => r switch { >= 800 => "Partner", >= 600 => "Trusted", >= 300 => "Regular", >= 100 => "Acquainted", _ => "New" };

        /// <summary>Weekly (Mondays): each unlocked buyer posts contracts. Trusted buyers post two, partners add a standing order.</summary>
        public static void PostWeekly(Random rng)
        {
            int today = Game1.Date.TotalDays;
            int posted = 0;
            foreach (Buyer b in Buyer.All.Where(b => b.Unlocked()))
            {
                int rel = Rel(b.Id);
                int count = rel >= 600 ? 2 : 1;
                for (int i = 0; i < count; i++)
                {
                    S.Contracts.Add(Make(b, rel, rng, today, standing: false));
                    posted++;
                }
                if (rel >= 800 && !S.Contracts.Any(c => c.BuyerId == b.Id && c.Standing && c.Status == "Open"))
                {
                    S.Contracts.Add(Make(b, rel, rng, today, standing: true));
                    posted++;
                }
            }
            if (posted > 0)
            {
                Game1.player.mailForTomorrow.Remove(ModEntry.ContractsMail);
                if (!Game1.player.mailbox.Contains(ModEntry.ContractsMail))
                    Game1.player.mailbox.Add(ModEntry.ContractsMail);
                Game1.addHUDMessage(new HUDMessage($"{posted} new contract(s) posted. Open the board with {ModEntry.Config.ContractsKey}.", HUDMessage.newQuest_type));
            }
        }

        private static Contract Make(Buyer b, int rel, Random rng, int today, bool standing)
        {
            Goods g = Goods.Get(b.Wants[rng.Next(b.Wants.Length)]);
            int minTier = 0;
            if (g.Material != null)
            {
                // buyers ask for higher tiers as the relationship deepens; fashion houses never take ungraded
                int ceiling = rel >= 800 ? 4 : rel >= 600 ? 3 : rel >= 300 ? 2 : 1;
                int floor = b.Kind == "Fashion" ? 1 : 0;
                minTier = rng.Next(floor, ceiling + 1);
                if (FarmData.HubTier() < 4)
                    minTier = Math.Min(minTier, 2);
            }
            int qty = b.Kind switch { "Local" => rng.Next(2, 6), "Restaurant" => rng.Next(4, 11), _ => rng.Next(3, 9) };
            int unitPrice = g.Material != null ? g.Material.GradedPrice(Math.Max(1, minTier)) : g.BasePrice;
            float premium = 1.25f + rel / 2000f + (b.Kind == "Fashion" ? 0.25f : 0f);
            return new Contract
            {
                Id = S.NextContractId++,
                BuyerId = b.Id,
                MaterialId = g.Id,
                MinTier = minTier,
                Quantity = qty,
                PostedDay = today,
                DeadlineDay = today + (standing ? 14 : 7 + rng.Next(0, 8)),
                Reward = (int)(unitPrice * qty * premium),
                Standing = standing
            };
        }

        /// <summary>Count matching items across the given inventories.</summary>
        public static int CountMatching(IEnumerable<Item> items, Contract c)
        {
            Goods g = Goods.Get(c.MaterialId);
            return items.Where(i => g.Accepts(i, c.MinTier)).Sum(i => i.Stack);
        }

        /// <summary>Remove the contract quantity from the inventory; returns the oldest made-day among perishables, or -1.</summary>
        public static int? TakeItems(IList<Item> items, Contract c)
        {
            Goods g = Goods.Get(c.MaterialId);
            int need = c.Quantity;
            int? oldest = null;
            for (int i = 0; i < items.Count && need > 0; i++)
            {
                Item item = items[i];
                if (!g.Accepts(item, c.MinTier))
                    continue;
                if (item.modData.TryGetValue(ModEntry.MadeKey, out string made) && int.TryParse(made, out int day))
                    oldest = oldest == null ? day : Math.Min(oldest.Value, day);
                int take = Math.Min(need, item.Stack);
                need -= take;
                item.Stack -= take;
                if (item.Stack <= 0)
                    items[i] = null;
            }
            return oldest;
        }

        /// <summary>Ship a contract from the given inventory with the given mode. Returns an error message or null.</summary>
        public static string Ship(Contract c, IList<Item> source, string mode, bool chargeFuel = true)
        {
            if (CountMatching(source, c) < c.Quantity)
                return "Not enough matching goods.";

            if (mode == "Truck" && chargeFuel)
            {
                Item fuel = Game1.player.Items.FirstOrDefault(i => i?.QualifiedItemId == Ids.O(Ids.Fuel));
                if (fuel == null)
                    return "Needs 1 Synthetic Fuel.";
                Game1.player.Items.ReduceId(Ids.O(Ids.Fuel), 1);
            }

            int? oldest = TakeItems(source, c);
            Goods g = Goods.Get(c.MaterialId);
            int arrive = Game1.Date.TotalDays + Transport.Days(mode);
            bool spoiled = false;
            if (g.Perishable && oldest != null)
            {
                int shelf = g.Material != null ? 6 : Ids.ShelfLife.TryGetValue(g.Accepted?[0] ?? "", out int s) ? s : 4;
                spoiled = arrive - oldest.Value > shelf;
            }

            int cost = (int)(c.Reward * Transport.CostFraction(mode));
            if (cost > 0)
                Game1.player.Money = Math.Max(0, Game1.player.Money - cost);

            S.Shipments.Add(new Shipment { ContractId = c.Id, Mode = mode, ArriveDay = arrive, Value = c.Reward, Spoiled = spoiled });
            c.Status = "Shipped";
            Game1.playSound(mode == "Hand" ? "give_gift" : "Ship");
            return null;
        }

        /// <summary>Morning: settle arrivals, expire overdue contracts, renew standing orders.</summary>
        public static void DailyUpdate()
        {
            int today = Game1.Date.TotalDays;
            SettleArrivals();

            foreach (Contract c in S.Contracts.Where(c => c.Status == "Open" && c.DeadlineDay < today).ToList())
            {
                c.Status = "Expired";
                AddRel(c.BuyerId, -60);
                Game1.addHUDMessage(new HUDMessage($"Contract for {Buyer.Get(c.BuyerId)?.Name} expired.", HUDMessage.error_type));
            }

            // keep the list small
            S.Contracts.RemoveAll(c => (c.Status == "Done" || c.Status == "Expired") && today - c.DeadlineDay > 14);
        }

        /// <summary>Pay out every shipment that has arrived (also used for hand deliveries, which arrive at once).</summary>
        public static void SettleArrivals()
        {
            int today = Game1.Date.TotalDays;
            foreach (Shipment s in S.Shipments.Where(s => s.ArriveDay <= today).ToList())
            {
                S.Shipments.Remove(s);
                Contract c = S.Contracts.FirstOrDefault(x => x.Id == s.ContractId);
                if (c == null)
                    continue;
                Buyer b = Buyer.Get(c.BuyerId);
                bool late = s.ArriveDay > c.DeadlineDay;
                int pay = s.Value;
                if (s.Spoiled) pay /= 2;
                if (late) pay = pay * 7 / 10;
                Game1.player.Money += pay;
                BalanceDiary.ContractIncome += pay;
                AddRel(c.BuyerId, s.Spoiled || late ? 10 : 45);
                Skill.AddXp(40);
                c.Status = "Done";
                string note = s.Spoiled ? " (arrived past its shelf life, half pay)" : late ? " (late, 70% pay)" : "";
                Game1.addHUDMessage(new HUDMessage($"{b?.Name ?? c.BuyerId} paid {pay}g for {c.Quantity} {Goods.Get(c.MaterialId)?.Name}{note}.", HUDMessage.achievement_type));

                if (c.Standing)
                {
                    // standing orders renew
                    c.Status = "Open";
                    c.PostedDay = today;
                    c.DeadlineDay = today + 14;
                }
            }

        }

        public static string Describe(Contract c)
        {
            Goods g = Goods.Get(c.MaterialId);
            string tier = g.Material != null && c.MinTier > 0 ? $"{Ids.TierNames[c.MinTier]}+ " : "";
            return $"{c.Quantity} x {tier}{g.Name}";
        }
    }

    /// <summary>Freight depot, cold storage and perishables.</summary>
    internal static class Logistics
    {
        public static bool HasFreightDepot()
            => Game1.getFarm()?.buildings.Any(b => b.buildingType.Value == Ids.FreightDepot && !b.isUnderConstruction()) == true;

        /// <summary>Stamp perishables that have no made-day yet (milk from the barn, cuts from retiring animals).</summary>
        public static void StampPerishables(IEnumerable<Item> items)
        {
            foreach (Item i in items)
            {
                if (i == null)
                    continue;
                if (Ids.ShelfLife.ContainsKey(i.QualifiedItemId) && !i.modData.ContainsKey(ModEntry.MadeKey))
                    i.modData[ModEntry.MadeKey] = Game1.Date.TotalDays.ToString();
            }
        }

        /// <summary>Cold store: perishables in chests next to a Cold Store unit don't age (their made-day moves forward each night).</summary>
        public static void ColdStoreTick()
        {
            Utility.ForEachLocation(loc =>
            {
                foreach (var pair in loc.objects.Pairs)
                {
                    if (pair.Value?.QualifiedItemId != Ids.BC(Ids.ColdStore))
                        continue;
                    foreach (var tile in FarmData.TilesAround(pair.Key, 1))
                    {
                        if (loc.objects.TryGetValue(tile, out SObject o) && o is Chest chest)
                        {
                            foreach (Item i in chest.Items)
                            {
                                if (i != null && i.modData.TryGetValue(ModEntry.MadeKey, out string made) && int.TryParse(made, out int d))
                                    i.modData[ModEntry.MadeKey] = (d + 1).ToString();
                            }
                        }
                    }
                }
                return true;
            });

            // the Cold Warehouse's built-in storage
            foreach (var building in Game1.getFarm()?.buildings.ToList() ?? new List<StardewValley.Buildings.Building>())
            {
                if (building.buildingType.Value != Ids.Warehouse || building.isUnderConstruction())
                    continue;
                Chest storage = building.GetBuildingChest("Storage");
                if (storage == null)
                    continue;
                foreach (Item i in storage.Items)
                {
                    if (i != null && i.modData.TryGetValue(ModEntry.MadeKey, out string made) && int.TryParse(made, out int d))
                        i.modData[ModEntry.MadeKey] = (d + 1).ToString();
                }
            }
        }

        public static string Freshness(Item i)
        {
            if (i == null || !i.modData.TryGetValue(ModEntry.MadeKey, out string made) || !int.TryParse(made, out int d))
                return null;
            int shelf = Ids.ShelfLife.TryGetValue(i.QualifiedItemId, out int s) ? s : 6;
            int left = shelf - (Game1.Date.TotalDays - d);
            return left >= 0 ? $"{left} day(s) of shelf life left" : "past its shelf life";
        }
    }
}
