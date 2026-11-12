using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace Agritech
{
    /// <summary>The contract board: open orders with ship buttons per transport mode, shipments in transit, buyer standings.</summary>
    internal class ContractsMenu : IClickableMenu
    {
        private const int RowHeight = 92;
        private readonly string buyerFilter;
        private int scroll;
        private string flash;
        private int flashTimer;

        private List<Contract> Rows => ModEntry.State.Contracts
            .Where(c => c.Status == "Open" && (buyerFilter == null || c.BuyerId == buyerFilter))
            .OrderBy(c => c.DeadlineDay).ToList();

        public ContractsMenu(string buyerFilter = null)
            : base(Game1.uiViewport.Width / 2 - 620, Game1.uiViewport.Height / 2 - 380, 1240, 760, showUpperRightCloseButton: true)
        {
            this.buyerFilter = buyerFilter;
        }

        private int Visible => (height - 330) / RowHeight;

        private Rectangle ButtonRect(int rowOnScreen, int modeIndex)
        {
            int y = yPositionOnScreen + 120 + rowOnScreen * RowHeight + 40;
            int x = xPositionOnScreen + width - 48 - (Transport.Modes.Length - modeIndex) * 112;
            return new Rectangle(x, y, 104, 40);
        }

        public override void receiveScrollWheelAction(int direction)
            => scroll = Math.Clamp(scroll + (direction > 0 ? -1 : 1), 0, Math.Max(0, Rows.Count - Visible));

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);
            var rows = Rows;
            for (int r = 0; r < Visible && r + scroll < rows.Count; r++)
            {
                Contract c = rows[r + scroll];
                for (int m = 0; m < Transport.Modes.Length; m++)
                {
                    if (!ButtonRect(r, m).Contains(x, y))
                        continue;
                    string mode = Transport.Modes[m];
                    if (!Transport.Available(mode, out string why))
                    {
                        Flash(why);
                        Game1.playSound("cancel");
                        return;
                    }
                    string error = ContractBook.Ship(c, Game1.player.Items, mode);
                    if (error != null)
                    {
                        Flash(error);
                        Game1.playSound("cancel");
                    }
                    else
                        Flash($"Shipped by {mode}. Arrives in {Transport.Days(mode)} day(s).");
                    return;
                }
            }
        }

        private void Flash(string text)
        {
            flash = text;
            flashTimer = 180;
        }

        public override void update(GameTime time)
        {
            base.update(time);
            if (flashTimer > 0)
                flashTimer--;
        }

        private static void Pin(SpriteBatch b, int x, int y)
        {
            b.Draw(Game1.staminaRect, new Rectangle(x + 2, y + 2, 12, 12), Color.Black * 0.3f);
            b.Draw(Game1.staminaRect, new Rectangle(x, y, 12, 12), new Color(200, 40, 40));
            b.Draw(Game1.staminaRect, new Rectangle(x + 2, y + 2, 4, 4), new Color(255, 150, 150));
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.5f);
            // corkboard in a wooden frame
            drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), xPositionOnScreen, yPositionOnScreen, width, height, new Color(170, 110, 60), 1f, true);
            var cork = new Rectangle(xPositionOnScreen + 28, yPositionOnScreen + 28, width - 56, height - 56);
            b.Draw(Game1.staminaRect, cork, new Color(196, 148, 96));
            var speck = new Random(4242);
            for (int n = 0; n < 900; n++)
            {
                int sx = cork.X + speck.Next(cork.Width - 4), sy = cork.Y + speck.Next(cork.Height - 4);
                b.Draw(Game1.staminaRect, new Rectangle(sx, sy, 4, 4), speck.Next(3) == 0 ? new Color(150, 104, 62) : new Color(214, 170, 118));
            }
            string title = buyerFilter == null ? "Contract Board" : Buyer.Get(buyerFilter)?.Name ?? "Contracts";
            int titleW = SpriteText.getWidthOfString(title) + 64;
            b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + width / 2 - titleW / 2, yPositionOnScreen + 24, titleW, 64), new Color(245, 236, 210));
            Pin(b, xPositionOnScreen + width / 2 - titleW / 2 + 12, yPositionOnScreen + 32);
            Pin(b, xPositionOnScreen + width / 2 + titleW / 2 - 24, yPositionOnScreen + 32);
            SpriteText.drawStringHorizontallyCenteredAt(b, title, xPositionOnScreen + width / 2, yPositionOnScreen + 36);

            var font = Game1.smallFont;
            Color ink = Game1.textColor;
            Color muted = new(110, 90, 70);
            Color warn = new(170, 40, 30);
            int today = Game1.Date.TotalDays;
            var rows = Rows;

            if (rows.Count == 0)
            {
                b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + 36, yPositionOnScreen + 116, 760, 60), new Color(250, 244, 222));
                Pin(b, xPositionOnScreen + 776, yPositionOnScreen + 124);
            }
            if (rows.Count == 0)
                Utility.drawTextWithShadow(b, "No open contracts. New ones are posted every Monday.", font, new Vector2(xPositionOnScreen + 48, yPositionOnScreen + 130), muted);

            for (int r = 0; r < Visible && r + scroll < rows.Count; r++)
            {
                Contract c = rows[r + scroll];
                Buyer buyer = Buyer.Get(c.BuyerId);
                int y = yPositionOnScreen + 120 + r * RowHeight;
                int have = ContractBook.CountMatching(Game1.player.Items, c);
                int daysLeft = c.DeadlineDay - today;

                // the order as a pinned note
                var note = new Rectangle(xPositionOnScreen + 36, y - 10, width - 72, RowHeight - 8);
                b.Draw(Game1.staminaRect, new Rectangle(note.X + 6, note.Y + 6, note.Width, note.Height), Color.Black * 0.18f);
                b.Draw(Game1.staminaRect, note, (r + scroll) % 2 == 0 ? new Color(250, 244, 222) : new Color(244, 236, 206));
                Pin(b, note.Right - 24, note.Y + 8);

                string head = $"{buyer?.Name} ({buyer?.City}){(c.Standing ? "  - standing order" : "")}";
                Utility.drawTextWithShadow(b, head, font, new Vector2(xPositionOnScreen + 48, y), new Color(60, 40, 140));
                string body = $"{ContractBook.Describe(c)}   {c.Reward}g   {daysLeft} day(s) left   you have {have}";
                Utility.drawTextWithShadow(b, body, font, new Vector2(xPositionOnScreen + 48, y + 40), have >= c.Quantity ? ink : daysLeft <= 1 ? warn : muted);

                for (int m = 0; m < Transport.Modes.Length; m++)
                {
                    Rectangle rect = ButtonRect(r, m);
                    bool ok = Transport.Available(Transport.Modes[m], out _) && have >= c.Quantity;
                    drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9), rect.X, rect.Y, rect.Width, rect.Height, ok ? Color.White : Color.Gray * 0.7f, 4f, false);
                    string label = Transport.Modes[m];
                    Vector2 size = font.MeasureString(label);
                    b.DrawString(font, label, new Vector2(rect.Center.X - size.X / 2, rect.Center.Y - size.Y / 2 + 2), ok ? ink : muted);
                }
            }

            // footer: shipments in transit and buyer standings
            int fy = yPositionOnScreen + height - 200;
            b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + 42, fy - 18, width - 72, 170), Color.Black * 0.18f);
            b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + 36, fy - 24, width - 72, 170), new Color(236, 230, 214));
            Pin(b, xPositionOnScreen + width / 2 - 6, fy - 18);
            b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + 40, fy - 12, width - 80, 2), muted * 0.6f);
            var transit = ModEntry.State.Shipments.OrderBy(s => s.ArriveDay).Take(3).ToList();
            string transitText = transit.Count == 0 ? "In transit: nothing." :
                "In transit: " + string.Join("   ", transit.Select(s =>
                {
                    Contract c = ModEntry.State.Contracts.FirstOrDefault(x => x.Id == s.ContractId);
                    return $"{(c != null ? ContractBook.Describe(c) : "?")} by {s.Mode}, {Math.Max(0, s.ArriveDay - today)}d{(s.Spoiled ? " (spoiling)" : "")}";
                }));
            Utility.drawTextWithShadow(b, transitText, font, new Vector2(xPositionOnScreen + 48, fy), ink);

            var standings = Buyer.All.Where(x => x.Unlocked()).Select(x => $"{x.Name}: {ContractBook.RelLabel(ContractBook.Rel(x.Id))}").ToList();
            string line1 = string.Join("   ", standings.Take(4));
            string line2 = string.Join("   ", standings.Skip(4));
            Utility.drawTextWithShadow(b, line1, font, new Vector2(xPositionOnScreen + 48, fy + 44), muted);
            Utility.drawTextWithShadow(b, line2, font, new Vector2(xPositionOnScreen + 48, fy + 84), muted);
            string modes = "Cart 3d free  ·  Rail 2d, 2%, needs Freight Depot  ·  Truck 1d, 1 fuel  ·  Air 1d, 10%, Hub tier 4";
            Utility.drawTextWithShadow(b, modes, font, new Vector2(xPositionOnScreen + 48, fy + 124), muted * 0.8f);

            if (flashTimer > 0 && flash != null)
            {
                Vector2 size = font.MeasureString(flash);
                Utility.drawTextWithShadow(b, flash, font, new Vector2(xPositionOnScreen + width / 2 - size.X / 2, yPositionOnScreen + 84), warn);
            }

            base.draw(b);
            drawMouse(b);
        }
    }
}
