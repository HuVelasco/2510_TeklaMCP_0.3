using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tekla.Structures.Model;
using Tekla.Structures.Drawing;
using Tekla.Structures.Geometry3d;

namespace FitterAnalyzer
{
    /// <summary>
    /// Core algorithm to analyze assemblies and identify required dimensions
    /// Simplified version focusing on dimension detection logic
    /// </summary>
    public class FitterAnalyzer
    {
        private Tekla.Structures.Model.Model model;
        private Tekla.Structures.Drawing.Drawing drawing;

        public FitterAnalyzer(Tekla.Structures.Model.Model model, Tekla.Structures.Drawing.Drawing drawing)
        {
            this.model = model;
            this.drawing = drawing;
        }

        /// <summary>
        /// Analyze assembly and return summary of required dimensions
        /// </summary>
        public AnalysisSummary Analyze()
        {
            var summary = new AnalysisSummary();

            try
            {
                // Get all parts in drawing
                var parts = GetAllParts();
                summary.TotalParts = parts.Count;

                // Get existing dimensions
                var existingDims = GetExistingDimensions();
                summary.ExistingDimensions = existingDims.Count;

                // Analyze what dimensions are needed
                var requiredDims = FindRequiredDimensions(parts);
                summary.RequiredDimensions = requiredDims;

                summary.Success = true;
            }
            catch (Exception ex)
            {
                summary.Success = false;
                summary.ErrorMessage = ex.Message;
            }

            return summary;
        }

        private List<Tekla.Structures.Drawing.Part> GetAllParts()
        {
            var parts = new List<Tekla.Structures.Drawing.Part>();
            var sheet = drawing.GetSheet();

            var objectEnum = sheet.GetAllObjects();
            while (objectEnum.MoveNext())
            {
                if (objectEnum.Current is Tekla.Structures.Drawing.Part part)
                {
                    parts.Add(part);
                }
            }

            return parts;
        }

        private List<DatabaseObject> GetExistingDimensions()
        {
            var dimensions = new List<DatabaseObject>();
            var sheet = drawing.GetSheet();

            var objectEnum = sheet.GetAllObjects();
            while (objectEnum.MoveNext())
            {
                var obj = objectEnum.Current;
                if (obj != null && obj.GetType().Name.Contains("Dimension"))
                {
                    dimensions.Add(obj);
                }
            }

            return dimensions;
        }

        private List<RequiredDimension> FindRequiredDimensions(List<Tekla.Structures.Drawing.Part> parts)
        {
            var required = new List<RequiredDimension>();

            // Identify main part
            var mainPart = IdentifyMainPart(parts);
            if (mainPart == null) return required;

            var mainModelPart = GetModelPart(mainPart);
            if (mainModelPart == null) return required;

            var mainPartAxis = GetMainPartLocalAxis(mainModelPart);

            // Analyze each secondary part
            foreach (var part in parts)
            {
                var modelPart = GetModelPart(part);
                if (modelPart == null) continue;

                // Skip main part
                if (modelPart.Identifier.Equals(mainModelPart.Identifier))
                    continue;

                // Check if it's a secondary part
                if (IsSecondaryPart(modelPart))
                {
                    var dims = AnalyzeSecondaryPartDimensions(part, modelPart, mainModelPart, mainPartAxis);
                    required.AddRange(dims);
                }
            }

            return required;
        }

        private Tekla.Structures.Model.Part GetModelPart(Tekla.Structures.Drawing.Part drawingPart)
        {
            try
            {
                var modelObject = model.SelectModelObject(drawingPart.ModelIdentifier);
                return modelObject as Tekla.Structures.Model.Part;
            }
            catch
            {
                return null;
            }
        }

        private bool IsSecondaryPart(Tekla.Structures.Model.Part part)
        {
            var assembly = part.GetAssembly();
            if (assembly == null) return false;

            var mainPart = assembly.GetMainPart();
            return mainPart != null && !mainPart.Identifier.Equals(part.Identifier);
        }

        private Tekla.Structures.Drawing.Part IdentifyMainPart(List<Tekla.Structures.Drawing.Part> parts)
        {
            // Main part is typically the longest part
            Tekla.Structures.Drawing.Part mainPart = null;
            double maxLength = 0;

            foreach (var part in parts)
            {
                var modelPart = GetModelPart(part);
                if (modelPart == null) continue;

                var beam = modelPart as Beam;
                if (beam != null)
                {
                    var length = Distance.PointToPoint(beam.StartPoint, beam.EndPoint);
                    if (length > maxLength)
                    {
                        maxLength = length;
                        mainPart = part;
                    }
                }
            }

            return mainPart ?? parts.FirstOrDefault();
        }

        private Vector GetMainPartLocalAxis(Tekla.Structures.Model.Part mainPart)
        {
            var beam = mainPart as Beam;
            if (beam != null)
            {
                var direction = new Vector(
                    beam.EndPoint.X - beam.StartPoint.X,
                    beam.EndPoint.Y - beam.StartPoint.Y,
                    beam.EndPoint.Z - beam.StartPoint.Z
                );
                direction.Normalize();
                return direction;
            }

            return new Vector(1, 0, 0); // Default to global X axis
        }

        private List<RequiredDimension> AnalyzeSecondaryPartDimensions(
            Tekla.Structures.Drawing.Part drawingPart,
            Tekla.Structures.Model.Part modelPart,
            Tekla.Structures.Model.Part mainPart,
            Vector mainPartAxis)
        {
            var dimensions = new List<RequiredDimension>();

            var beam = modelPart as Beam;
            if (beam == null) return dimensions;

            var mainBeam = mainPart as Beam;
            if (mainBeam == null) return dimensions;

            // Get position of secondary part relative to main part
            var secondaryPos = beam.StartPoint;
            var mainPos = mainBeam.StartPoint;

            var offset = new Vector(
                secondaryPos.X - mainPos.X,
                secondaryPos.Y - mainPos.Y,
                secondaryPos.Z - mainPos.Z
            );

            // Dimension 1: Along main part's local X axis
            var alongAxis = Vector.Dot(offset, mainPartAxis);
            if (Math.Abs(alongAxis) > 1.0)
            {
                dimensions.Add(new RequiredDimension
                {
                    Type = "Along Main Axis (X)",
                    Description = $"Position along main axis: {beam.Profile.ProfileString}",
                    PartProfile = beam.Profile.ProfileString,
                    Value = Math.Abs(alongAxis)
                });
            }

            // Dimension 2: Normal to main axis - Y direction
            var yAxis = new Vector(-mainPartAxis.Y, mainPartAxis.X, 0);
            yAxis.Normalize();
            var normalY = Vector.Dot(offset, yAxis);
            if (Math.Abs(normalY) > 1.0)
            {
                dimensions.Add(new RequiredDimension
                {
                    Type = "Normal to Axis (Y)",
                    Description = $"Perpendicular offset: {beam.Profile.ProfileString}",
                    PartProfile = beam.Profile.ProfileString,
                    Value = Math.Abs(normalY)
                });
            }

            // Dimension 3: Vertical Z direction
            var zAxis = new Vector(0, 0, 1);
            var normalZ = Vector.Dot(offset, zAxis);
            if (Math.Abs(normalZ) > 1.0)
            {
                dimensions.Add(new RequiredDimension
                {
                    Type = "Vertical (Z)",
                    Description = $"Vertical offset: {beam.Profile.ProfileString}",
                    PartProfile = beam.Profile.ProfileString,
                    Value = Math.Abs(normalZ)
                });
            }

            return dimensions;
        }
    }

    /// <summary>
    /// Summary of analysis results
    /// </summary>
    public class AnalysisSummary
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int TotalParts { get; set; }
        public int ExistingDimensions { get; set; }
        public List<RequiredDimension> RequiredDimensions { get; set; } = new List<RequiredDimension>();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== FITTER ANALYSIS SUMMARY ===");
            sb.AppendLine();

            if (!Success)
            {
                sb.AppendLine($"ERROR: {ErrorMessage}");
                return sb.ToString();
            }

            sb.AppendLine($"Total Parts: {TotalParts}");
            sb.AppendLine($"Existing Dimensions: {ExistingDimensions}");
            sb.AppendLine($"Required Dimensions: {RequiredDimensions.Count}");
            sb.AppendLine();

            if (RequiredDimensions.Count > 0)
            {
                sb.AppendLine("DIMENSION BREAKDOWN:");
                var byType = RequiredDimensions.GroupBy(d => d.Type);
                foreach (var group in byType)
                {
                    sb.AppendLine($"  {group.Key}: {group.Count()} dimensions");
                }
                sb.AppendLine();

                sb.AppendLine("DETAILS:");
                foreach (var dim in RequiredDimensions.Take(20))
                {
                    sb.AppendLine($"  - {dim.Type}: {dim.Value:F1}mm ({dim.PartProfile})");
                }

                if (RequiredDimensions.Count > 20)
                {
                    sb.AppendLine($"  ... and {RequiredDimensions.Count - 20} more");
                }
            }
            else
            {
                sb.AppendLine("No missing dimensions detected.");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Represents a dimension that should be added
    /// </summary>
    public class RequiredDimension
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public string PartProfile { get; set; }
        public double Value { get; set; }
    }
}
