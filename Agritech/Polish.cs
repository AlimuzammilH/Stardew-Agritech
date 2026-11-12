using System;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Pathfinding;
using SObject = StardewValley.Object;

namespace Agritech
{
    /// <summary>Polish pass: item tooltips, real herding, tractor fuel, hand delivery to Gus and Emily.</summary>
    internal static class Polish
    {
        /*********
        ** Tooltips: provenance certificate and freshness on the item itself
        *********/
        public static void PatchTooltips(string modId)
        {
            var harmony = new Harmony(modId);
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), nameof(SObject.getDescription)),
                postfix: new HarmonyMethod(typeof(Polish), nameof(DescriptionPostfix)));
        }

        private static void DescriptionPostfix(SObject __instance, ref string __result)
        {
            try
            {
                string extra = "";
                if (__instance.modData.TryGetValue(ModEntry.ProvenanceKey, out string cert))
                    extra += "\n\nProvenance: " + cert;
                string fresh = Logistics.Freshness(__instance);
                if (fresh != null)
                    extra += "\n\n" + fresh[..1].ToUpper() + fresh[1..] + ".";
                if (extra.Length > 0)
                    __result += Game1.parseText(extra, Game1.smallFont, 300);
            }
            catch
            {
                // never break a tooltip
            }
        }

        /*********
        ** Herding: collared animals outside the active zone walk to a spot inside it
        *********/
        public static void Herd(SaveModel save, Random rng)
        {
            if (save.ActiveZone < 0 || save.ActiveZone >= save.Zones.Count)
                return;
            FenceZone zone = save.Zones[save.ActiveZone];
            Farm farm = Game1.getFarm();
            foreach (var pair in farm.animals.Pairs)
            {
                FarmAnimal a = pair.Value;
                if (!save.Collars.ContainsKey(a.myID.Value) || zone.Contains(a.Tile.X, a.Tile.Y) || a.controller != null)
                    continue;

                Point target = new(zone.X + rng.Next(zone.Width), zone.Y + rng.Next(zone.Height));
                var controller = new PathFindController(a, farm, target, 2);
                // no path means a fence or building is in the way: leave it, like a real herd would
                if (controller.pathToEndPoint != null && controller.pathToEndPoint.Count > 0)
                    a.controller = controller;
            }
        }

        /*********
        ** Tractor fuel: one synthetic fuel per 100 tiles, or coal per 40 tiles as a fallback
        *********/
        private static Vector2 lastTractorPos;
        private static float tractorDistance;

        public static void TickTractor()
        {
            Horse mount = Game1.player.mount;
            if (!Livestock.IsTractor(mount))
            {
                lastTractorPos = Vector2.Zero;
                return;
            }
            if (lastTractorPos != Vector2.Zero)
            {
                float moved = Vector2.Distance(lastTractorPos, Game1.player.Position);
                if (moved < 64)
                    tractorDistance += moved;
            }
            lastTractorPos = Game1.player.Position;

            bool hasFuel = Game1.player.Items.Any(i => i?.QualifiedItemId == Ids.O(Ids.Fuel));
            float budget = (hasFuel ? 100 : 40) * 64f;
            if (tractorDistance < budget)
                return;

            tractorDistance = 0;
            if (hasFuel)
                Game1.player.Items.ReduceId(Ids.O(Ids.Fuel), 1);
            else if (Game1.player.Items.Any(i => i?.QualifiedItemId == "(O)382"))
                Game1.player.Items.ReduceId("(O)382", 1);
            else
            {
                Game1.addHUDMessage(new HUDMessage("The tractor is out of fuel. Carry synthetic fuel (or coal).", HUDMessage.error_type));
                mount.dismount();
            }
        }

        /*********
        ** Hand delivery: give Gus or Emily the goods for their open contract in person
        *********/
        private static readonly (string npc, string line)[] Thanks =
        {
            ("Gus", "Now that's a delivery! These go straight on tonight's menu. Thanks, friend."),
            ("Emily", "Oh, this has such good energy. I can already feel what I'm going to make with it. Thank you!")
        };

        /// <summary>Returns true if the click was a hand delivery.</summary>
        public static bool TryHandDeliver(Vector2 tile)
        {
            NPC npc = Game1.currentLocation.isCharacterAtTile(tile) ?? Game1.currentLocation.isCharacterAtTile(tile + new Vector2(0, 1));
            if (npc == null || Game1.player.ActiveObject == null)
                return false;
            string buyer = npc.Name;
            if (buyer != "Gus" && buyer != "Emily")
                return false;

            Item held = Game1.player.ActiveObject;
            Contract c = ModEntry.State.Contracts
                .Where(x => x.BuyerId == buyer && x.Status == "Open" && Goods.Get(x.MaterialId).Accepts(held, x.MinTier))
                .OrderBy(x => x.DeadlineDay).FirstOrDefault();
            if (c == null)
                return false;

            if (ContractBook.CountMatching(Game1.player.Items, c) < c.Quantity)
            {
                Game1.DrawDialogue(new Dialogue(npc, null, $"That's for my order, right? I need {ContractBook.Describe(c)}. Bring me the rest when you have it."));
                return true;
            }

            ContractBook.Ship(c, Game1.player.Items, "Hand");
            ContractBook.SettleArrivals();
            npc.doEmote(Character.happyEmote);
            Game1.DrawDialogue(new Dialogue(npc, null, Thanks.First(t => t.npc == buyer).line));
            return true;
        }
    }
}
