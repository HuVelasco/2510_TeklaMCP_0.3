using System;
using System.Collections.Generic;
using Tekla.Structures.Model;
using Tekla.Structures.Geometry3d;

namespace TeklaMCP.HttpServer.Utils
{
    public static class TeklaApiHelpers
    {
        public static bool InsertAndCommit(ModelObject obj, Model model)
        {
            try
            {
                bool success = obj.Insert();
                if (success)
                {
                    model.CommitChanges();
                }
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting object: {ex.Message}");
                return false;
            }
        }

        public static bool ModifyAndCommit(ModelObject obj, Model model)
        {
            try
            {
                bool success = obj.Modify();
                if (success)
                {
                    model.CommitChanges();
                }
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error modifying object: {ex.Message}");
                return false;
            }
        }

        public static bool DeleteAndCommit(ModelObject obj, Model model)
        {
            try
            {
                bool success = obj.Delete();
                if (success)
                {
                    model.CommitChanges();
                }
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting object: {ex.Message}");
                return false;
            }
        }

        public static ModelObject GetObjectById(int id, Model model)
        {
            try
            {
                var identifier = new Tekla.Structures.Identifier(id);
                return model.SelectModelObject(identifier);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting object by ID {id}: {ex.Message}");
                return null;
            }
        }

        public static List<ModelObject> GetAllObjectsOfType<T>(Model model) where T : ModelObject
        {
            var objects = new List<ModelObject>();
            
            try
            {
                var modelObjects = model.GetModelObjectSelector().GetAllObjects();
                while (modelObjects.MoveNext())
                {
                    if (modelObjects.Current is T)
                    {
                        objects.Add(modelObjects.Current);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting objects of type {typeof(T).Name}: {ex.Message}");
            }

            return objects;
        }

        public static double CalculateDistance(Point p1, Point p2)
        {
            return Math.Sqrt(
                Math.Pow(p2.X - p1.X, 2) +
                Math.Pow(p2.Y - p1.Y, 2) +
                Math.Pow(p2.Z - p1.Z, 2)
            );
        }

        public static double CalculateAngle(Point p1, Point p2)
        {
            double deltaX = p2.X - p1.X;
            double deltaY = p2.Y - p1.Y;
            return Math.Atan2(deltaY, deltaX) * (180.0 / Math.PI);
        }

        public static Point MidPoint(Point p1, Point p2)
        {
            return new Point(
                (p1.X + p2.X) / 2,
                (p1.Y + p2.Y) / 2,
                (p1.Z + p2.Z) / 2
            );
        }

        public static bool IsVerticalElement(Point startPoint, Point endPoint, double tolerance = 2.0)
        {
            double deltaZ = Math.Abs(endPoint.Z - startPoint.Z);
            double deltaXY = Math.Sqrt(
                Math.Pow(endPoint.X - startPoint.X, 2) +
                Math.Pow(endPoint.Y - startPoint.Y, 2)
            );
            return deltaZ > deltaXY * tolerance;
        }

        public static bool IsHorizontalElement(Point startPoint, Point endPoint, double tolerance = 2.0)
        {
            double deltaZ = Math.Abs(endPoint.Z - startPoint.Z);
            double deltaXY = Math.Sqrt(
                Math.Pow(endPoint.X - startPoint.X, 2) +
                Math.Pow(endPoint.Y - startPoint.Y, 2)
            );
            return deltaXY > deltaZ * tolerance;
        }

        public static string GenerateUniqueName(string baseName, Model model)
        {
            int counter = 1;
            string uniqueName = baseName;

            // Simple check - in production you might want to check against actual model objects
            while (counter < 1000) // Prevent infinite loop
            {
                try
                {
                    // For now, just append timestamp and counter
                    uniqueName = $"{baseName}_{DateTime.Now:HHmmss}_{counter}";
                    break;
                }
                catch
                {
                    counter++;
                }
            }

            return uniqueName;
        }

        public static bool ValidateProfile(string profileString)
        {
            // Basic profile validation - extend as needed
            if (string.IsNullOrEmpty(profileString)) return false;
            
            // Common profile prefixes
            string[] validPrefixes = { "W", "C", "L", "WT", "MC", "S", "M", "HP", "HSS", "PIPE" };
            
            foreach (string prefix in validPrefixes)
            {
                if (profileString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static bool ValidateMaterial(string materialString)
        {
            // Basic material validation - extend as needed
            if (string.IsNullOrEmpty(materialString)) return false;
            
            string[] validMaterials = { "A992", "A36", "A572", "A588", "A514" };
            
            return Array.Exists(validMaterials, m => 
                string.Equals(m, materialString, StringComparison.OrdinalIgnoreCase));
        }
    }
}