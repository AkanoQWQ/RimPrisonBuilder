using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimPrison.UI
{
    // [UNREVIEWED] Haven't reviewed carefully
    public static class RPR_UiStyle
    {
        public static readonly Color PanelBg = new Color(0.12f, 0.125f, 0.13f, 0.62f);
        public static readonly Color SubPanelBg = new Color(0.18f, 0.185f, 0.19f, 0.38f);
        public static readonly Color MutedPanelBg = new Color(1f, 1f, 1f, 0.035f);
        public static readonly Color BorderColor = new Color(1f, 1f, 1f, 0.075f);
        public static readonly Color TextMuted = new Color(0.72f, 0.74f, 0.76f, 1f);
        public static readonly Color SelectionColor = new Color(0.28f, 0.38f, 0.46f, 0.58f);

        public static void DrawPanel(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, PanelBg);
            DrawSoftBorder(rect);
        }

        public static void DrawSubPanel(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, SubPanelBg);
            DrawSoftBorder(rect);
        }

        public static void DrawSectionTitle(Rect rect, string label)
        {
            var oldFont = Text.Font;
            Text.Font = GameFont.Small;
            Widgets.Label(rect, label);
            Text.Font = oldFont;
        }

        public static void DrawMutedLabel(Rect rect, string label)
        {
            var oldColor = GUI.color;
            GUI.color = TextMuted;
            Widgets.Label(rect, label);
            GUI.color = oldColor;
        }

        public static void DrawSoftBorder(Rect rect)
        {
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, 1f), BorderColor);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), BorderColor);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 1f, rect.height), BorderColor);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), BorderColor);
        }

        // Draw a suppression ring chart. suppressedPct is 0-1.
        // Blue arc = suppressed portion, red arc = unsuppressed.
        public static void DrawSuppressionRing(Rect rect, float suppressedPct)
        {
            suppressedPct = Mathf.Clamp01(suppressedPct);
            int size = Mathf.FloorToInt(Mathf.Min(rect.width, rect.height));
            if (size < 32) return;
            size = Mathf.Min(size, 256);

            string cacheKey = $"SuppRing_{size}_{suppressedPct:F2}";
            var tex = GetCachedTexture(cacheKey, size, suppressedPct);

            float drawX = rect.x + (rect.width - size) / 2f;
            float drawY = rect.y + (rect.height - size) / 2f;
            GUI.DrawTexture(new Rect(drawX, drawY, size, size), tex, ScaleMode.StretchToFill, alphaBlend: true);
        }

        private static readonly Dictionary<string, CachedTex> texCache = new Dictionary<string, CachedTex>();
        private const int MaxTexCache = 8;

        private struct CachedTex { public Texture2D tex; public int frame; }

        private static Texture2D GetCachedTexture(string key, int size, float suppressedPct)
        {
            if (texCache.TryGetValue(key, out var cached))
            {
                cached.frame = Time.frameCount;
                texCache[key] = cached;
                return cached.tex;
            }

            if (texCache.Count >= MaxTexCache)
            {
                string oldestKey = null;
                int oldestFrame = int.MaxValue;
                foreach (var kv in texCache)
                {
                    if (kv.Value.frame < oldestFrame)
                    {
                        oldestFrame = kv.Value.frame;
                        oldestKey = kv.Key;
                    }
                }
                if (oldestKey != null)
                {
                    Object.Destroy(texCache[oldestKey].tex);
                    texCache.Remove(oldestKey);
                }
            }

            var newTex = GenSuppressionRing(size, suppressedPct);
            texCache[key] = new CachedTex { tex = newTex, frame = Time.frameCount };
            return newTex;
        }

        private static Texture2D GenSuppressionRing(int size, float suppressedPct)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float cx = size / 2f, cy = size / 2f;
            float outerR = size / 2f - 2f;
            float innerR = outerR * 0.6f;

            Color suppressedColor = new Color(0.18f, 0.38f, 0.65f, 1f);
            Color unsuppressedColor = new Color(0.65f, 0.15f, 0.15f, 1f);
            Color bgColor = new Color(0f, 0f, 0f, 0f);

            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist < innerR || dist > outerR)
                    {
                        pixels[y * size + x] = bgColor;
                        continue;
                    }
                    // Angle from top (12 o'clock position), clockwise
                    float angle = Mathf.Atan2(dx, -dy);
                    if (angle < 0f) angle += Mathf.PI * 2f;
                    float frac = angle / (Mathf.PI * 2f);
                    pixels[y * size + x] = frac <= suppressedPct ? suppressedColor : unsuppressedColor;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply(false);
            return tex;
        }

        public static void ClearTexCache()
        {
            foreach (var kv in texCache)
                Object.Destroy(kv.Value.tex);
            texCache.Clear();
        }
    }
}
