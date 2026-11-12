using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Agritech
{
    /// <summary>
    /// Builds the herd horse sheets at load time from the game's own horse sprite (Animals/horse), so the mod ships
    /// no game art: takes the saddle off, recolours the coat (bay, black, grey) and rearranges the frames into the
    /// farm-animal layout (4 columns x 5 rows of 32x32: walk down, right, up, left, then grazing).
    /// </summary>
    internal static class HorseArt
    {
        public const int SrcWidth = 224, SrcHeight = 128;

        private static readonly Color Out = new(81, 32, 0);
        private static readonly Color[] Coat = { new(249, 201, 144), new(236, 173, 97), new(198, 133, 53), new(163, 99, 19), new(127, 71, 3) };
        private static readonly Color[] Mane = { new(99, 40, 22), new(142, 75, 39) };
        private static readonly Color SaddleStitch = new(153, 72, 5);
        private static readonly (Color from, Color to)[] Saddle = { (new(142, 120, 39), new(198, 133, 53)), (new(112, 83, 31), new(163, 99, 19)) };
        private static readonly Color[] Shoes = { new(157, 177, 183), new(96, 116, 122) };
        private static readonly Color FoalShadow = new(38, 26, 21, 136); // premultiplied, like the game's own shadow

        private class Palette { public Color[] Coat; public Color[] Mane; public Color Out; }

        private static readonly Dictionary<string, Palette> Coats = new()
        {
            ["Bay"] = new() { Coat = new Color[] { new(222, 150, 104), new(194, 112, 66), new(158, 80, 44), new(122, 56, 28), new(90, 38, 18) }, Mane = new Color[] { new(34, 26, 28), new(58, 46, 46) }, Out = new(44, 18, 10) },
            ["Black"] = new() { Coat = new Color[] { new(132, 128, 142), new(96, 92, 106), new(68, 64, 78), new(48, 44, 56), new(32, 30, 40) }, Mane = new Color[] { new(18, 16, 20), new(34, 32, 38) }, Out = new(12, 8, 12) },
            ["Grey"] = new() { Coat = new Color[] { new(250, 250, 252), new(228, 228, 232), new(198, 200, 206), new(162, 164, 172), new(126, 128, 136) }, Mane = new Color[] { new(96, 96, 104), new(140, 140, 148) }, Out = new(60, 56, 62) }
        };

        private static bool Same(Color a, Color b) => a.R == b.R && a.G == b.G && a.B == b.B;
        private static int IndexIn(Color[] set, Color c) { for (int i = 0; i < set.Length; i++) if (Same(set[i], c)) return i; return -1; }
        private static bool IsSaddle(Color c) => Saddle.Any(s => Same(s.from, c));
        private static Color Unsaddle(Color c) { foreach (var s in Saddle) if (Same(s.from, c)) return s.to; return c; }

        /// <summary>Build one texture's pixels. kind: "adult", "baby" or "shop". Returns the pixels and their size.</summary>
        public static (Color[] pixels, int width, int height) Build(Color[] src, string coat, string kind)
        {
            Color[] sheet = Recolour(Desaddle(src), Coats[coat]);
            if (kind == "shop")
            {
                // the horse's front-facing head, like the vanilla shop icons
                var icon = new Color[32 * 16];
                for (int y = 0; y < 16; y++)
                    for (int x = 0; x < 16; x++)
                        icon[y * 32 + 8 + x] = sheet[(96 + y) * SrcWidth + 160 + x];
                return (icon, 32, 16);
            }

            var outPx = new Color[128 * 160];
            int[] walk = { 0, 2, 4, 6 };
            for (int i = 0; i < 4; i++)
            {
                Blit(sheet, walk[i], 0, outPx, i, 0, false);
                Blit(sheet, walk[i], 1, outPx, i, 1, false);
                Blit(sheet, walk[i], 2, outPx, i, 2, false);
                Blit(sheet, walk[i], 1, outPx, i, 3, true);
                Blit(sheet, i, 3, outPx, i, 4, false); // grazing: head going down
            }
            if (kind == "baby")
                outPx = Foal(outPx, Coats[coat]);
            return (outPx, 128, 160);
        }

        private static void Blit(Color[] src, int sc, int sr, Color[] dst, int dc, int dr, bool flip)
        {
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    int sx = sc * 32 + (flip ? 31 - x : x);
                    dst[(dr * 32 + y) * 128 + dc * 32 + x] = src[(sr * 32 + y) * SrcWidth + sx];
                }
        }

        /// <summary>Take the saddle off: saddle pixels become coat, and the saddle's outline inside the body is filled in.</summary>
        private static Color[] Desaddle(Color[] src)
        {
            var p = (Color[])src.Clone();
            int W = SrcWidth, H = SrcHeight;
            var sad = new List<int>();
            for (int i = 0; i < p.Length; i++)
                if (p[i].A == 255 && (IsSaddle(p[i]) || Same(p[i], SaddleStitch)))
                    sad.Add(i);
            var near = new HashSet<int>();
            foreach (int i in sad)
            {
                int x0 = i % W, y0 = i / W;
                for (int dx = -3; dx <= 3; dx++)
                    for (int dy = -3; dy <= 3; dy++)
                    {
                        int x = x0 + dx, y = y0 + dy;
                        if (x > 0 && y > 0 && x < W - 1 && y < H - 1)
                            near.Add(y * W + x);
                    }
            }
            for (int pass = 0; pass < 4; pass++)
            {
                foreach (int i in near)
                {
                    if (p[i].A != 255 || !Same(p[i], Out))
                        continue;
                    Color[] nb = { p[i + 1], p[i - 1], p[i + W], p[i - W] };
                    if (nb.Any(n => n.A != 255))
                        continue;
                    var coat = nb.Where(n => IndexIn(Coat, n) >= 0 || IsSaddle(n)).ToList();
                    var notOutline = nb.Where(n => !Same(n, Out)).ToList();
                    if (coat.Count >= 2 || notOutline.Count >= 3)
                    {
                        Color c = (coat.Count > 0 ? coat : notOutline)[0];
                        p[i] = Unsaddle(c);
                    }
                }
            }
            foreach (int i in sad)
                p[i] = Same(p[i], SaddleStitch) ? Coat[3] : Unsaddle(p[i]);
            return p;
        }

        private static Color[] Recolour(Color[] src, Palette pal)
        {
            var p = (Color[])src.Clone();
            for (int i = 0; i < p.Length; i++)
            {
                Color c = p[i];
                if (c.A != 255)
                    continue;
                c = Unsaddle(c);
                int k;
                if (Same(c, Out)) c = pal.Out;
                else if ((k = IndexIn(Coat, c)) >= 0) c = pal.Coat[k];
                else if ((k = IndexIn(Mane, c)) >= 0) c = pal.Mane[k];
                p[i] = new Color(c.R, c.G, c.B, (byte)255);
            }
            return p;
        }

        /// <summary>Foals: each frame shrunk to 24x24 with an area average, then snapped back to the coat's palette.</summary>
        private static Color[] Foal(Color[] sheet, Palette pal)
        {
            var palette = new List<Color> { pal.Out };
            palette.AddRange(pal.Coat);
            palette.AddRange(pal.Mane);
            palette.AddRange(Shoes);
            var outPx = new Color[sheet.Length];
            const int S = 24;
            const float scale = 32f / S;
            for (int r = 0; r < 5; r++)
                for (int c = 0; c < 4; c++)
                    for (int y = 0; y < S; y++)
                        for (int x = 0; x < S; x++)
                        {
                            // area-weighted average over the source footprint
                            float x0 = x * scale, x1 = x0 + scale, y0 = y * scale, y1 = y0 + scale;
                            float r_ = 0, g_ = 0, b_ = 0, a_ = 0, wsum = 0;
                            for (int sy = (int)y0; sy < Math.Ceiling(y1); sy++)
                                for (int sx = (int)x0; sx < Math.Ceiling(x1); sx++)
                                {
                                    float w = (Math.Min(x1, sx + 1) - Math.Max(x0, sx)) * (Math.Min(y1, sy + 1) - Math.Max(y0, sy));
                                    if (w <= 0) continue;
                                    Color s = sheet[(r * 32 + sy) * 128 + c * 32 + sx];
                                    r_ += s.R * w; g_ += s.G * w; b_ += s.B * w; a_ += s.A * w; wsum += w;
                                }
                            float a = a_ / wsum;
                            Color o;
                            if (a < 60) o = Color.Transparent;
                            else if (a < 200) o = FoalShadow;
                            else
                            {
                                // colours are premultiplied: divide by alpha to compare with the opaque palette
                                float k = 255f / a_;
                                float R = r_ * k, G = g_ * k, B = b_ * k;
                                o = palette.OrderBy(q => (q.R - R) * (q.R - R) + (q.G - G) * (q.G - G) + (q.B - B) * (q.B - B)).First();
                                o.A = 255;
                            }
                            outPx[(r * 32 + 8 + y) * 128 + c * 32 + 4 + x] = o;
                        }
            return outPx;
        }
    }
}
