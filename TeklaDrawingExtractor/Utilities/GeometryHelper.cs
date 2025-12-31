using System;
using Tekla.Structures.Geometry3d;
using Tekla.Structures.Drawing;

namespace TeklaDrawingExtractor.Utilities
{
    /// <summary>
    /// Helper methods for geometry calculations
    /// </summary>
    public static class GeometryHelper
    {
        /// <summary>
        /// Calculate distance between two 3D points
        /// </summary>
        public static double Distance3D(Point p1, Point p2)
        {
            return Math.Sqrt(
                Math.Pow(p2.X - p1.X, 2) +
                Math.Pow(p2.Y - p1.Y, 2) +
                Math.Pow(p2.Z - p1.Z, 2)
            );
        }

        /// <summary>
        /// Calculate distance between two 2D points
        /// </summary>
        public static double Distance2D(object p1, object p2)
        {
            // Handle dynamic point types from Tekla Drawing API
            dynamic dp1 = p1;
            dynamic dp2 = p2;

            return Math.Sqrt(
                Math.Pow(dp2.X - dp1.X, 2) +
                Math.Pow(dp2.Y - dp1.Y, 2)
            );
        }

        /// <summary>
        /// Parse point to object (handles both 2D and 3D)
        /// </summary>
        public static object ParsePoint(object point)
        {
            if (point == null) return null;

            // Use dynamic to handle different point types
            dynamic dp = point;

            return new
            {
                x = Math.Round((double)dp.X, 2),
                y = Math.Round((double)dp.Y, 2)
            };
        }

        /// <summary>
        /// Parse 3D point to object
        /// </summary>
        public static object ParsePoint3D(Point point)
        {
            if (point == null) return null;

            return new
            {
                x = Math.Round(point.X, 2),
                y = Math.Round(point.Y, 2),
                z = Math.Round(point.Z, 2)
            };
        }

        /// <summary>
        /// Determine direction from two points
        /// </summary>
        public static string DetermineDirection(object start, object end)
        {
            dynamic dStart = start;
            dynamic dEnd = end;

            double dx = Math.Abs(dEnd.X - dStart.X);
            double dy = Math.Abs(dEnd.Y - dStart.Y);

            return dx > dy ? "horizontal" : "vertical";
        }

        /// <summary>
        /// Calculate bounding box from list of points
        /// </summary>
        public static object CalculateBoundingBox(System.Collections.Generic.List<Point> points)
        {
            if (points == null || points.Count == 0)
                return null;

            double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;

            foreach (var p in points)
            {
                minX = Math.Min(minX, p.X);
                minY = Math.Min(minY, p.Y);
                minZ = Math.Min(minZ, p.Z);
                maxX = Math.Max(maxX, p.X);
                maxY = Math.Max(maxY, p.Y);
                maxZ = Math.Max(maxZ, p.Z);
            }

            return new
            {
                min = new { x = Math.Round(minX, 2), y = Math.Round(minY, 2), z = Math.Round(minZ, 2) },
                max = new { x = Math.Round(maxX, 2), y = Math.Round(maxY, 2), z = Math.Round(maxZ, 2) }
            };
        }
    }
}
