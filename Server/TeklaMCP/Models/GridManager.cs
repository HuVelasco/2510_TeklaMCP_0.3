using System;
using System.Collections.Generic;
using System.Linq;
using Tekla.Structures.Model;
using Tekla.Structures.Geometry3d;
using TeklaMCP.HttpServer.Utils;

namespace TeklaMCP.HttpServer.Models
{
    public class GridManager
    {
        private Dictionary<string, Point> gridPoints;

        public GridManager()
        {
            gridPoints = new Dictionary<string, Point>();
        }

        public void CacheGridPoints(Model teklaModel)
        {
            try
            {
                gridPoints.Clear();

                var modelObjects = teklaModel.GetModelObjectSelector().GetAllObjects();
                while (modelObjects.MoveNext())
                {
                    if (modelObjects.Current is Grid grid)
                    {
                        // Parse grid coordinates and labels to create intersection points
                        ParseGridIntersections(grid);
                    }
                }

                Console.WriteLine($"✅ Cached {gridPoints.Count} grid intersection points");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error caching grid points: {ex.Message}");
            }
        }

        private void ParseGridIntersections(Grid grid)
        {
            try
            {
                string[] xLabels = grid.LabelX?.Split(' ') ?? new string[0];
                string[] yLabels = grid.LabelY?.Split(' ') ?? new string[0];

                List<double> xPositions = CoordinateParser.ParseCoordinateString(grid.CoordinateX);
                List<double> yPositions = CoordinateParser.ParseCoordinateString(grid.CoordinateY);

                for (int i = 0; i < xLabels.Length && i < xPositions.Count; i++)
                {
                    for (int j = 0; j < yLabels.Length && j < yPositions.Count; j++)
                    {
                        string intersection = $"{xLabels[i]}-{yLabels[j]}";
                        gridPoints[intersection] = new Point(xPositions[i], yPositions[j], 0.0);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing grid intersections: {ex.Message}");
            }
        }

        public Dictionary<string, Point> GetAllGridPoints()
        {
            return new Dictionary<string, Point>(gridPoints);
        }

        public Point GetGridPoint(string intersection)
        {
            gridPoints.TryGetValue(intersection, out Point point);
            return point;
        }

        public bool HasGridPoint(string intersection)
        {
            return gridPoints.ContainsKey(intersection);
        }

        public int GetGridPointCount()
        {
            return gridPoints.Count;
        }

        public List<string> GetAllIntersections()
        {
            return gridPoints.Keys.ToList();
        }
    }
}