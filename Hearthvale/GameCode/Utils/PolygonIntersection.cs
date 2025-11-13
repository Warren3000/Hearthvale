using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hearthvale.GameCode.Utils
{
    /// <summary>
    /// Utility class for polygon collision detection using Separating Axis Theorem (SAT)
    /// </summary>
    public static class PolygonIntersection
    {
        /// <summary>
        /// Checks if two polygons intersect using the Separating Axis Theorem
        /// </summary>
        public static bool DoPolygonsIntersect(List<Vector2> poly1, List<Vector2> poly2)
        {
            if (poly1.Count < 3 || poly2.Count < 3)
                return false;

            // Get all potential separating axes
            var axes = new List<Vector2>();
            axes.AddRange(GetPolygonAxes(poly1));
            axes.AddRange(GetPolygonAxes(poly2));

            // Test each axis for separation
            foreach (var axis in axes)
            {
                if (axis.LengthSquared() == 0) continue;

                var proj1 = ProjectPolygonOntoAxis(poly1, axis);
                var proj2 = ProjectPolygonOntoAxis(poly2, axis);

                // If projections don't overlap, polygons are separated
                if (proj1.max < proj2.min || proj2.max < proj1.min)
                    return false;
            }

            return true; // No separating axis found, polygons intersect
        }

        /// <summary>
        /// Gets the perpendicular axes for a polygon (normals to each edge)
        /// </summary>
        private static List<Vector2> GetPolygonAxes(List<Vector2> polygon)
        {
            var axes = new List<Vector2>();

            for (int i = 0; i < polygon.Count; i++)
            {
                var edge = polygon[(i + 1) % polygon.Count] - polygon[i];
                var perpendicular = new Vector2(-edge.Y, edge.X);

                if (perpendicular.LengthSquared() > 0)
                {
                    axes.Add(Vector2.Normalize(perpendicular));
                }
            }

            return axes;
        }

        /// <summary>
        /// Projects a polygon onto an axis and returns the min/max projections
        /// </summary>
        private static (float min, float max) ProjectPolygonOntoAxis(List<Vector2> polygon, Vector2 axis)
        {
            float min = Vector2.Dot(polygon[0], axis);
            float max = min;

            for (int i = 1; i < polygon.Count; i++)
            {
                float projection = Vector2.Dot(polygon[i], axis);
                min = Math.Min(min, projection);
                max = Math.Max(max, projection);
            }

            return (min, max);
        }

        /// <summary>
        /// Checks if a point is inside a polygon using ray casting algorithm
        /// </summary>
        public static bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
        {
            if (polygon.Count < 3) return false;

            bool inside = false;
            int j = polygon.Count - 1;

            for (int i = 0; i < polygon.Count; j = i++)
            {
                if (((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y)) &&
                    (point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X))
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        public static List<Vector2> CreateRectanglePolygon(Rectangle rect)
        {
            return new List<Vector2>
            {
                new Vector2(rect.Left, rect.Top),
                new Vector2(rect.Right, rect.Top),
                new Vector2(rect.Right, rect.Bottom),
                new Vector2(rect.Left, rect.Bottom)
            };
        }
    }
}