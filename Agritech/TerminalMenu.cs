using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace Agritech
{
    /// <summary>The farm data terminal: a scrollable read-out of sensors and collared animals.</summary>
    internal class TerminalMenu : IClickableMenu
    {
        private readonly List<(string text, Color color)> lines;
        private int scroll;
        private const int LineHeight = 40;

        public TerminalMenu(List<(string text, Color color)> lines)
            : base(Game1.uiViewport.Width / 2 - 520, Game1.uiViewport.Height / 2 - 340, 1040, 680, showUpperRightCloseButton: true)
        {
            this.lines = lines;
        }

        private int VisibleLines => (height - 140) / LineHeight;

        public override void receiveScrollWheelAction(int direction)
        {
            scroll = Math.Clamp(scroll + (direction > 0 ? -1 : 1), 0, Math.Max(0, lines.Count - VisibleLines));
        }

        public override void receiveKeyPress(Microsoft.Xna.Framework.Input.Keys key)
        {
            if (key == Microsoft.Xna.Framework.Input.Keys.Down)
                receiveScrollWheelAction(-1);
            else if (key == Microsoft.Xna.Framework.Input.Keys.Up)
                receiveScrollWheelAction(1);
            else
                base.receiveKeyPress(key);
        }

        /// <summary>Map the line's colour (chosen for a light page) onto a green-screen palette, keeping its hue as a tint.</summary>
        private static Color Phosphor(Color c)
        {
            Color light = Color.Lerp(c, Color.White, 0.6f);
            return Color.Lerp(light, new Color(120, 255, 150), 0.55f);
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.5f);
            // monitor: grey bezel, dark phosphor screen, scanlines, blinking cursor
            drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), xPositionOnScreen, yPositionOnScreen, width, height, new Color(150, 156, 166), 1f, true);
            var screen = new Rectangle(xPositionOnScreen + 28, yPositionOnScreen + 28, width - 56, height - 56);
            b.Draw(Game1.staminaRect, screen, new Color(8, 20, 12));
            for (int sy = screen.Top; sy < screen.Bottom; sy += 4)
                b.Draw(Game1.staminaRect, new Rectangle(screen.X, sy, screen.Width, 1), new Color(30, 80, 45) * 0.35f);
            b.Draw(Game1.staminaRect, new Rectangle(screen.X, screen.Y, screen.Width, 6), new Color(120, 255, 160) * 0.08f);

            var font = Game1.smallFont;
            Color phosphor = new(120, 255, 150);
            b.DrawString(font, "AGRITECH OS  //  FARM DATA TERMINAL", new Vector2(screen.X + 24, screen.Y + 16), phosphor);
            b.Draw(Game1.staminaRect, new Rectangle(screen.X + 20, screen.Y + 54, screen.Width - 40, 2), phosphor * 0.5f);

            int y = yPositionOnScreen + 110;
            for (int i = scroll; i < lines.Count && i < scroll + VisibleLines; i++)
            {
                b.DrawString(font, lines[i].text, new Vector2(xPositionOnScreen + 56, y), Phosphor(lines[i].color));
                y += LineHeight;
            }
            if (Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 1000 < 500)
                b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + 56, Math.Min(y, screen.Bottom - 60) + 6, 14, 24), phosphor);

            if (lines.Count > VisibleLines)
            {
                string hint = $"> scroll  [{scroll + 1}-{Math.Min(lines.Count, scroll + VisibleLines)} / {lines.Count}]";
                b.DrawString(font, hint, new Vector2(xPositionOnScreen + 56, yPositionOnScreen + height - 76), phosphor * 0.6f);
            }

            base.draw(b);
            drawMouse(b);
        }
    }
}
