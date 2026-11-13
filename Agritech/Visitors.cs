using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace Agritech
{
    /// <summary>
    /// Buyers from the fashion and restaurant houses visit the farm on their day each season; tourists come on weekends.
    /// They're temporary characters: spawned in the morning, removed before the game saves.
    /// </summary>
    internal static class Visitors
    {
        private const string Marker = "Ali.Agritech/Visitor";
        private static readonly string[] TouristNames = { "Tourist", "Day-tripper" };

        /// <summary>Visitors currently on the farm: npc -> buyer id (or "tourist").</summary>
        private static readonly Dictionary<NPC, string> Active = new();
        private static readonly HashSet<NPC> PaidTours = new();
        private static readonly Dictionary<NPC, Vector2> Homes = new();

        public static string SpriteAsset(string who) => $"Mods/Ali.Agritech/Characters/{who}";
        public static string PortraitAsset(string who) => $"Mods/Ali.Agritech/Portraits/{who}";

        public static void MorningSpawn(IModHelper helper, Random rng)
        {
            Despawn();
            Farm farm = Game1.getFarm();
            Point door = farm.GetMainFarmHouseEntry();
            int slot = 0;

            foreach (Buyer b in Buyer.All.Where(b => b.VisitDay == Game1.dayOfMonth && b.Unlocked()))
            {
                Spawn(helper, farm, b.Id, b.Name, new Vector2(door.X - 2 + slot * 2, door.Y + 3));
                slot++;
                Game1.addHUDMessage(new HUDMessage($"{b.Name} is visiting the farm today.", HUDMessage.newQuest_type));
            }

            // weekend tourists, once the Hub is up
            bool weekend = Game1.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            if (weekend && FarmData.HubTier() >= 1 && !Game1.IsRainingHere(farm))
            {
                int count = 1 + rng.Next(0, 3);
                for (int i = 0; i < count; i++)
                {
                    string who = "tourist" + (i % 2 + 1);
                    Spawn(helper, farm, who, TouristNames[i % 2], new Vector2(door.X - 6 + i * 3 + rng.Next(0, 2), door.Y + 6 + rng.Next(0, 3)));
                }
            }
        }

        internal static void Spawn(IModHelper helper, Farm farm, string who, string displayName, Vector2 tile)
        {
            Texture2D portrait = helper.GameContent.Load<Texture2D>(PortraitAsset(who));
            var npc = new NPC(new AnimatedSprite(SpriteAsset(who), 0, 16, 32), tile * 64f, "Farm", 2, displayName, false, portrait);
            npc.modData[Marker] = who;
            npc.displayName = displayName;
            npc.Breather = true;
            farm.characters.Add(npc);
            Active[npc] = who;
            Homes[npc] = tile;
        }

        /// <summary>Remove all visitors (before saving, or when the day ends).</summary>
        public static void Despawn()
        {
            Farm farm = Game1.getFarm();
            if (farm != null)
            {
                foreach (NPC npc in farm.characters.ToList())
                    if (npc.modData.ContainsKey(Marker))
                        farm.characters.Remove(npc);
            }
            Active.Clear();
            PaidTours.Clear();
            Homes.Clear();
        }

        /// <summary>Visitors stroll around the yard near where they arrived; buyers pause to look at the animals.</summary>
        public static void Wander(Random rng)
        {
            if (Game1.dialogueUp || Game1.activeClickableMenu != null)
                return;
            foreach (NPC npc in Active.Keys.ToList())
            {
                if (npc.controller != null || rng.NextDouble() < 0.5)
                    continue;
                Vector2 home = Homes[npc];
                var target = new Point((int)home.X + rng.Next(-6, 7), (int)home.Y + rng.Next(-4, 5));
                if (!npc.currentLocation.isTilePassable(new Vector2(target.X, target.Y)) || npc.currentLocation.IsTileOccupiedBy(new Vector2(target.X, target.Y)))
                    continue;
                var controller = new StardewValley.Pathfinding.PathFindController(npc, npc.currentLocation, target, rng.Next(4));
                if (controller.pathToEndPoint != null && controller.pathToEndPoint.Count > 0)
                    npc.controller = controller;
            }
        }

        public static NPC VisitorAt(Vector2 pixel)
        {
            foreach (NPC npc in Active.Keys)
            {
                if (npc.currentLocation != Game1.currentLocation)
                    continue;
                Rectangle box = npc.GetBoundingBox();
                box.Inflate(16, 64);
                box.Y -= 48;
                if (box.Contains((int)pixel.X, (int)pixel.Y))
                    return npc;
            }
            return null;
        }

        /// <summary>Talk to a visitor.</summary>
        public static void Talk(NPC npc, Action openBoard)
        {
            string who = Active[npc];
            npc.controller = null;
            npc.Halt();
            npc.faceTowardFarmerForPeriod(4000, 3, false, Game1.player);

            if (who.StartsWith("tourist"))
            {
                if (PaidTours.Contains(npc))
                {
                    Say(npc, "Lovely place. I'm telling everyone about the animals with the little collars.");
                    return;
                }
                PaidTours.Add(npc);
                int fee = 60 + 40 * FarmData.HubTier() + 15 * ModEntry.State.Collars.Count;
                Game1.player.Money += fee;
                BalanceDiary.TourIncome += fee;
                Say(npc, $"We came for the farm tour! The drones, the fences you can't see... here, {fee}g for the visit.");
                return;
            }

            Buyer b = Buyer.Get(who);
            string key = $"{b.Id}|{Game1.Date.TotalDays}";
            if (ModEntry.State.HostedVisits.Add(key))
            {
                // hosting the buyer: they inspect the collared herd
                var collared = FarmData.AllAnimals().Where(p => ModEntry.State.Collars.ContainsKey(p.animal.myID.Value)).Select(p => p.animal).ToList();
                float avg = collared.Count > 0 ? (float)collared.Average(FarmData.HealthScore) : 0;
                int gain = 20 + (avg >= 80 ? 30 : avg >= 60 ? 15 : 0) + (FarmData.HubTier() >= 4 ? 15 : 0);
                ContractBook.AddRel(b.Id, gain);
                Skill.AddXp(20);
                string verdict = collared.Count == 0
                    ? "No collared animals to inspect? Then I'm buying stories, not provenance."
                    : avg >= 80 ? "Your herd records are excellent. This is what we pay for."
                    : avg >= 60 ? "Healthy enough. Keep the records coming."
                    : "Your animals need care before we talk about the top grades.";
                Say(npc, $"{b.Greeting} {verdict}", openBoard);
                return;
            }
            Say(npc, "Shall we look at the contracts?", openBoard);
        }

        private static void Say(NPC npc, string text, Action after = null)
        {
            Game1.DrawDialogue(new Dialogue(npc, null, text));
            if (after != null)
                Game1.afterDialogues = () => after();
        }
    }
}
