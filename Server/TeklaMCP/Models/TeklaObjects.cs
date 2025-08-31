using System;
using Tekla.Structures.Model;
using Tekla.Structures.Geometry3d;

namespace TeklaMCP.HttpServer.Models
{
    public static class TeklaObjects
    {
        public static Beam CreateBeam(BeamCreateRequest request)
        {
            var beam = new Beam();

            // Set geometry
            if (request.StartPoint != null && request.EndPoint != null)
            {
                beam.StartPoint = new Point(request.StartPoint.X, request.StartPoint.Y, request.StartPoint.Z);
                beam.EndPoint = new Point(request.EndPoint.X, request.EndPoint.Y, request.EndPoint.Z);
            }

            // Set properties
            beam.Profile.ProfileString = request.Profile ?? "W12X50";
            beam.Material.MaterialString = request.Material ?? "A992";
            beam.Class = request.Class ?? "1";
            beam.Finish = request.Finish ?? "PAINT";
            beam.Name = request.Name ?? "BEAM";

            return beam;
        }

        public static Beam CreateColumn(string location, Point gridPoint, string profile = "W12X50", double height = 12000.0)
        {
            var column = new Beam();
            
            column.StartPoint = new Point(gridPoint.X, gridPoint.Y, gridPoint.Z);
            column.EndPoint = new Point(gridPoint.X, gridPoint.Y, gridPoint.Z + height);
            column.Profile.ProfileString = profile;
            column.Material.MaterialString = "A992";
            column.Class = "1";
            column.Name = $"COL-{location}";

            return column;
        }

        public static Beam CreateBrace(Point startPoint, Point endPoint, string profile = "L4X4X1/2")
        {
            var brace = new Beam();
            
            brace.StartPoint = startPoint;
            brace.EndPoint = endPoint;
            brace.Profile.ProfileString = profile;
            brace.Material.MaterialString = "A992";
            brace.Class = "1";
            brace.Name = "BRACE";

            return brace;
        }

        public static Grid CreateGrid(GridCreateRequest request)
        {
            var grid = new Grid();

            grid.Name = request.Name ?? $"Grid_{DateTime.Now:HHmm-ss}";
            grid.CoordinateX = request.XCoordinates ?? "0.00 2*6096.00";
            grid.CoordinateY = request.YCoordinates ?? "0.00 2*4572.00";
            grid.CoordinateZ = request.ZCoordinates ?? "0.00";
            grid.LabelX = request.XLabels ?? "A B C";
            grid.LabelY = request.YLabels ?? "1 2 3";
            grid.LabelZ = request.ZLabels ?? "+0";

            grid.ExtensionLeftX = request.ExtensionLeftX > 0 ? request.ExtensionLeftX : 3048.0;
            grid.ExtensionRightX = request.ExtensionRightX > 0 ? request.ExtensionRightX : 3048.0;
            grid.ExtensionLeftY = request.ExtensionLeftY > 0 ? request.ExtensionLeftY : 3048.0;
            grid.ExtensionRightY = request.ExtensionRightY > 0 ? request.ExtensionRightY : 3048.0;
            grid.IsMagnetic = request.IsMagnetic;

            return grid;
        }

        public static bool ModifyBeam(Beam beam, BeamModifyRequest request)
        {
            try
            {
                if (!string.IsNullOrEmpty(request.NewProfile))
                    beam.Profile.ProfileString = request.NewProfile;
                
                if (!string.IsNullOrEmpty(request.NewMaterial))
                    beam.Material.MaterialString = request.NewMaterial;
                
                if (!string.IsNullOrEmpty(request.NewClass))
                    beam.Class = request.NewClass;
                
                if (!string.IsNullOrEmpty(request.NewFinish))
                    beam.Finish = request.NewFinish;

                return beam.Modify();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error modifying beam: {ex.Message}");
                return false;
            }
        }

        public static ElementType GetElementType(Beam beam)
        {
            try
            {
                double deltaZ = Math.Abs(beam.EndPoint.Z - beam.StartPoint.Z);
                double deltaXY = Math.Sqrt(Math.Pow(beam.EndPoint.X - beam.StartPoint.X, 2) +
                                         Math.Pow(beam.EndPoint.Y - beam.StartPoint.Y, 2));
                
                if (deltaZ > deltaXY * 2)
                    return ElementType.Column;
                else if (deltaXY > deltaZ * 2)
                    return ElementType.Beam;
                else
                    return ElementType.Brace;
            }
            catch
            {
                return ElementType.Unknown;
            }
        }
    }

    public enum ElementType
    {
        Beam,
        Column,
        Brace,
        Unknown
    }
}