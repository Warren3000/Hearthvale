using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hearthvale.GameCode.Utils
{
    public enum HitboxPivotMode
    {
        BottomCenter,  // matches Sprite.Origin = (region.Width/2, region.Height)
        Centroid,      // centroid of opaque pixels
        Custom         // custom pivot (pixels, region-local)
    }

    public sealed class WeaponHitboxOptions
    {
        public byte AlphaThreshold { get; set; } = 1;         // 1 = any non-zero alpha
        public int InflatePixels { get; set; } = 0;           // expands the bounds outward
        public bool UseOrientedBoundingBox { get; set; } = false; // PCA OBB (better for slanted/diagonal weapons)
        public HitboxPivotMode PivotMode { get; set; } = HitboxPivotMode.BottomCenter;
        public Vector2 CustomPivot { get; set; } = Vector2.Zero;   // used when PivotMode=Custom; in region-local pixels
        public bool NormalizeYUp { get; set; } = true;             // output local Y negative upward (matches your current HitPolygon)
    }

    public static class WeaponHitboxGenerator
    {

        public static List<Vector2> GenerateBoundingPolygon(Texture2D texture, Rectangle sourceRect, WeaponHitboxOptions options, out Rectangle opaqueBounds)
        {
            if (texture == null) throw new ArgumentNullException(nameof(texture));
            if (sourceRect.Width <= 0 || sourceRect.Height <= 0) throw new ArgumentException("Invalid sourceRect");

            // Extract pixel data only for the region
            var pixels = new Color[sourceRect.Width * sourceRect.Height];
            texture.GetData(0, sourceRect, pixels, 0, pixels.Length);

            // Compute tight bounds of opaque pixels
            if (!TryComputeOpaqueBounds(pixels, sourceRect.Width, sourceRect.Height, options.AlphaThreshold, out opaqueBounds))
            {
                // No opaque pixels; return a tiny box around pivot
                var pivot = ComputePivot(sourceRect, pixels, options);
                var small = MakeAabbPolygon(new Rectangle((int)pivot.X - 2, (int)pivot.Y - 2, 4, 4));
                return ToWeaponLocal(small, sourceRect, options, pivot);
            }

            // Inflate if requested
            if (options.InflatePixels != 0)
            {
                opaqueBounds.Inflate(options.InflatePixels, options.InflatePixels);
                opaqueBounds = ClampTo(sourceRect.Size, opaqueBounds);
            }

            List<Vector2> polygon;
            if (options.UseOrientedBoundingBox)
            {
                polygon = ComputeObbPolygon(pixels, sourceRect.Width, sourceRect.Height, options.AlphaThreshold);
                if (polygon == null || polygon.Count < 3)
                {
                    polygon = MakeAabbPolygon(opaqueBounds);
                }
            }
            else
            {
                polygon = MakeAabbPolygon(opaqueBounds);
            }

            // Convert to weapon-local using chosen pivot
            var pivotPoint = ComputePivot(sourceRect, pixels, options);
            return ToWeaponLocal(polygon, sourceRect, options, pivotPoint);
        }

        // Debug draw helpers
        //public static void DrawPolygonOutline(SpriteBatch spriteBatch, Texture2D pixel, IReadOnlyList<Vector2> polygon, Vector2 worldOrigin, float rotation, Color color)
        //{
        //    if (polygon == null || polygon.Count < 2 || pixel == null) return;

        //    // transform each local point to world
        //    for (int i = 0; i < polygon.Count; i++)
        //    {
        //        var a = LocalToWorld(polygon[i], worldOrigin, rotation);
        //        var b = LocalToWorld(polygon[(i + 1) % polygon.Count], worldOrigin, rotation);
        //        DrawLine(spriteBatch, pixel, a, b, color);
        //    }
        //}

        public static void DrawRectangleOutline(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color)
        {
            if (pixel == null) return;
            // Top
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
            // Left
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
            // Right
            spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color);
            // Bottom
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color);
        }

        // --- Internals ---

        private static bool TryComputeOpaqueBounds(Color[] pixels, int w, int h, byte alphaThreshold, out Rectangle bounds)
        {
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            bool any = false;

            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    if (pixels[row + x].A >= alphaThreshold)
                    {
                        any = true;
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (!any)
            {
                bounds = Rectangle.Empty;
                return false;
            }

            // +1 on width/height to include the max pixel
            bounds = new Rectangle(minX, minY, (maxX - minX + 1), (maxY - minY + 1));
            return true;
        }

        private static Rectangle ClampTo(Point size, Rectangle rect)
        {
            int x = Math.Clamp(rect.X, 0, size.X);
            int y = Math.Clamp(rect.Y, 0, size.Y);
            int r = Math.Clamp(rect.Right, 0, size.X);
            int b = Math.Clamp(rect.Bottom, 0, size.Y);
            return new Rectangle(x, y, Math.Max(0, r - x), Math.Max(0, b - y));
        }

        private static List<Vector2> MakeAabbPolygon(Rectangle rect)
        {
            // In region-local pixel coordinates (top-left origin)
            return new List<Vector2>
            {
                new Vector2(rect.Left,  rect.Top),
                new Vector2(rect.Right, rect.Top),
                new Vector2(rect.Right, rect.Bottom),
                new Vector2(rect.Left,  rect.Bottom),
            };
        }

        private static List<Vector2> ComputeObbPolygon(Color[] pixels, int w, int h, byte alphaThreshold)
        {
            // Collect opaque points
            var pts = new List<Vector2>(w * h / 8);
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    if (pixels[row + x].A >= alphaThreshold)
                    {
                        pts.Add(new Vector2(x + 0.5f, y + 0.5f)); // center of pixel for better stability
                    }
                }
            }
            if (pts.Count == 0) return null;

            // PCA
            var mean = Vector2.Zero;
            foreach (var p in pts) mean += p;
            mean /= pts.Count;

            float sxx = 0, sxy = 0, syy = 0;
            foreach (var p in pts)
            {
                var d = p - mean;
                sxx += d.X * d.X;
                sxy += d.X * d.Y;
                syy += d.Y * d.Y;
            }
            sxx /= pts.Count;
            sxy /= pts.Count;
            syy /= pts.Count;

            // Eigen decomposition of 2x2 covariance
            // trace = sxx + syy, det = sxx*syy - sxy*sxy
            float trace = sxx + syy;
            float det = sxx * syy - sxy * sxy;
            float term = MathF.Sqrt(MathF.Max(0, trace * trace / 4f - det));
            float l1 = trace / 2f + term; // largest eigenvalue
            // Eigenvector for l1: (sxy, l1 - sxx) unless sxy ~ 0
            Vector2 u = (MathF.Abs(sxy) > 1e-5f) ? new Vector2(sxy, l1 - sxx) : new Vector2(1, 0);
            if (u.LengthSquared() < 1e-6f) u = new Vector2(1, 0);
            u.Normalize();
            Vector2 v = new Vector2(-u.Y, u.X);

            // Project to u,v to get extents
            float minU = float.MaxValue, maxU = float.MinValue;
            float minV = float.MaxValue, maxV = float.MinValue;
            foreach (var p in pts)
            {
                var d = p - mean;
                float du = Vector2.Dot(d, u);
                float dv = Vector2.Dot(d, v);
                if (du < minU) minU = du;
                if (du > maxU) maxU = du;
                if (dv < minV) minV = dv;
                if (dv > maxV) maxV = dv;
            }

            // Rebuild OBB corners in region-local coords
            var c0 = mean + u * minU + v * minV;
            var c1 = mean + u * maxU + v * minV;
            var c2 = mean + u * maxU + v * maxV;
            var c3 = mean + u * minU + v * maxV;

            return new List<Vector2> { c0, c1, c2, c3 };
        }

        private static Vector2 ComputePivot(Rectangle sourceRect, Color[] pixels, WeaponHitboxOptions options)
        {
            switch (options.PivotMode)
            {
                case HitboxPivotMode.BottomCenter:
                    // region-local (0..W),(0..H); bottom = H
                    return new Vector2(sourceRect.Width / 2f, sourceRect.Height);
                case HitboxPivotMode.Centroid:
                    // centroid of opaque pixels
                    int w = sourceRect.Width, h = sourceRect.Height;
                    long count = 0;
                    double sumX = 0, sumY = 0;
                    for (int y = 0; y < h; y++)
                    {
                        int row = y * w;
                        for (int x = 0; x < w; x++)
                        {
                            if (pixels[row + x].A >= options.AlphaThreshold)
                            {
                                count++;
                                sumX += x + 0.5;
                                sumY += y + 0.5;
                            }
                        }
                    }
                    if (count == 0) return new Vector2(sourceRect.Width / 2f, sourceRect.Height);
                    return new Vector2((float)(sumX / count), (float)(sumY / count));
                case HitboxPivotMode.Custom:
                    return options.CustomPivot;
                default:
                    return new Vector2(sourceRect.Width / 2f, sourceRect.Height);
            }
        }

        private static List<Vector2> ToWeaponLocal(List<Vector2> regionLocalPoints, Rectangle sourceRect, WeaponHitboxOptions options, Vector2 pivotRegionLocal)
        {
            // Convert from region-local (top-left origin, +Y down) to weapon-local around pivot.
            // Weapon-local convention: X right, Y up negative (NormalizeYUp=true).
            var list = new List<Vector2>(regionLocalPoints.Count);
            foreach (var p in regionLocalPoints)
            {
                float lx = p.X - pivotRegionLocal.X;
                float ly = p.Y - pivotRegionLocal.Y;
                if (options.NormalizeYUp) ly = -ly; // invert Y to match existing HitPolygon style
                list.Add(new Vector2(lx, ly));
            }
            return list;
        }

        private static void DrawLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 a, Vector2 b, Color color)
        {
            var diff = b - a;
            float len = diff.Length();
            if (len <= 0.0001f) return;
            float angle = MathF.Atan2(diff.Y, diff.X);
            spriteBatch.Draw(pixel, a, null, color, angle, Vector2.Zero, new Vector2(len, 1f), SpriteEffects.None, 0f);
        }

        private static Vector2 LocalToWorld(Vector2 local, Vector2 origin, float rotation)
        {
            var rotated = Vector2.Transform(local, Matrix.CreateRotationZ(rotation));
            return origin + rotated;
        }

        // Optional helper: try extract Texture and SourceRect from a TextureRegion via reflection (best-effort).
        //public static bool TryGetRegionData(object textureRegion, out Texture2D texture, out Rectangle sourceRect)
        //{
        //    texture = null;
        //    sourceRect = Rectangle.Empty;
        //    if (textureRegion == null) return false;

        //    var type = textureRegion.GetType();

        //    // Common property names to try
        //    var texProp = type.GetProperty("Texture") ?? type.GetProperty("AtlasTexture") ?? type.GetProperty("Sheet");
        //    if (texProp != null)
        //    {
        //        texture = texProp.GetValue(textureRegion) as Texture2D;
        //    }

        //    // Rect-like property names
        //    var rectProp = type.GetProperty("Bounds") ?? type.GetProperty("SourceRectangle") ?? type.GetProperty("Region") ?? type.GetProperty("Source");
        //    if (rectProp != null)
        //    {
        //        var val = rectProp.GetValue(textureRegion);
        //        if (val is Rectangle r) sourceRect = r;
        //    }
        //    else
        //    {
        //        // Some regions store X,Y,Width,Height separately
        //        var xProp = type.GetProperty("X") ?? type.GetProperty("Left");
        //        var yProp = type.GetProperty("Y") ?? type.GetProperty("Top");
        //        var wProp = type.GetProperty("Width");
        //        var hProp = type.GetProperty("Height");
        //        if (xProp != null && yProp != null && wProp != null && hProp != null)
        //        {
        //            int x = Convert.ToInt32(xProp.GetValue(textureRegion));
        //            int y = Convert.ToInt32(yProp.GetValue(textureRegion));
        //            int w = Convert.ToInt32(wProp.GetValue(textureRegion));
        //            int h = Convert.ToInt32(hProp.GetValue(textureRegion));
        //            sourceRect = new Rectangle(x, y, w, h);
        //        }
        //    }

        //    return texture != null && sourceRect.Width > 0 && sourceRect.Height > 0;
        //}
    }
}