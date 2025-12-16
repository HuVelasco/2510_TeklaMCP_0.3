using System;
using System.Collections.Generic;
using System.Linq;
using Tekla.Structures;
using Tekla.Structures.Model;
using Tekla.Structures.Drawing;
using Tekla.Structures.Geometry3d;

namespace VirtualFitter
{
    /// <summary>
    /// Analyzes assemblies to determine required dimensions
    /// </summary>
    public class AssemblyAnalyzer
    {
        private Tekla.Structures.Model.Model model;
        public Tekla.Structures.Drawing.Drawing Drawing { get; private set; }
        public AnalysisResult LastAnalysis { get; private set; }

        public AssemblyAnalyzer(Tekla.Structures.Model.Model model, Tekla.Structures.Drawing.Drawing drawing)
        {
            this.model = model;
            this.Drawing = drawing;
        }

        public AnalysisResult Analyze()
        {
            var result = new AnalysisResult();

            try
            {
                // Get all views
                var views = GetAllViews();
                result.AvailableViews = views.Count;

                // Get all parts in drawing
                var parts = GetAllParts();
                result.TotalParts = parts.Count;

                // Get existing dimensions
                var existing Dims = GetExistingDimensions();
                result.ExistingDimensions = existingDims.Count;

                // Analyze what dimensions are needed
                var missingDims = FindMissingDimensions(parts, existingDims, views);
                result.MissingDimensions = missingDims;

                // Check if we have sufficient views
                var viewAnalysis = AnalyzeViewSufficiency(missingDims, views);
                result.SufficientViews = viewAnalysis.Sufficient;
                result.SuggestedViews = viewAnalysis.SuggestedViews;

                LastAnalysis = result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Analysis failed: {ex.Message}", ex);
            }

            return result;
        }

        private List<Tekla.Structures.Drawing.View> GetAllViews()
        {
            var views = new List<Tekla.Structures.Drawing.View>();
            var sheet = Drawing.GetSheet();

            var objectEnum = sheet.GetAllObjects();
            while (objectEnum.MoveNext())
            {
                if (objectEnum.Current is Tekla.Structures.Drawing.View view)
                {
                    views.Add(view);
                }
            }

            return views;
        }

        private List<Tekla.Structures.Drawing.Part> GetAllParts()
        {
            var parts = new List<Tekla.Structures.Drawing.Part>();
            var sheet = Drawing.GetSheet();

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
            var sheet = Drawing.GetSheet();

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

        private List<RequiredDimension> FindMissingDimensions(
            List<Tekla.Structures.Drawing.Part> parts,
            List<DatabaseObject> existingDims,
            List<Tekla.Structures.Drawing.View> views)
        {
            var required = new List<RequiredDimension>();

            // For each part, determine required dimensions
            foreach (var part in parts)
            {
                // Get model part for geometry
                var modelPart = GetModelPart(part);
                if (modelPart == null) continue;

                // Check if it's a secondary part (not the main assembly part)
                if (IsSecondaryPart(modelPart))
                {
                    // Secondary parts need positioning dimensions
                    required.AddRange(GetSecondaryPartDimensions(part, modelPart, views));
                }
            }

            // Filter out dimensions that already exist
            var filtered = FilterExistingDimensions(required, existingDims);

            return filtered;
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
            // Simple heuristic: parts with assembly GUID are secondary
            // In practice, you'd check if it's in an assembly and not the main part
            var assembly = part.GetAssembly();
            if (assembly == null) return false;

            var mainPart = assembly.GetMainPart();
            return mainPart != null && !mainPart.Identifier.Equals(part.Identifier);
        }

        private List<RequiredDimension> GetSecondaryPartDimensions(
            Tekla.Structures.Drawing.Part drawingPart,
            Tekla.Structures.Model.Part modelPart,
            List<Tekla.Structures.Drawing.View> views)
        {
            var dimensions = new List<RequiredDimension>();

            // Each secondary part typically needs:
            // 1. Horizontal position (X)
            // 2. Vertical position (Z)
            // 3. Depth position (Y)
            // 4. If rotated: angle dimension

            var beam = modelPart as Beam;
            if (beam != null)
            {
                // Length dimension
                dimensions.Add(new RequiredDimension
                {
                    Type = "Length",
                    Description = $"Length of {beam.Profile.ProfileString}",
                    Part = drawingPart,
                    ModelPart = modelPart,
                    PreferredView = FindBestView(beam, views, "length")
                });

                // Position from main part - horizontal
                dimensions.Add(new RequiredDimension
                {
                    Type = "Horizontal Position",
                    Description = $"Horizontal position of {beam.Profile.ProfileString}",
                    Part = drawingPart,
                    ModelPart = modelPart,
                    PreferredView = FindBestView(beam, views, "horizontal")
                });

                // Position from main part - vertical
                dimensions.Add(new RequiredDimension
                {
                    Type = "Vertical Position",
                    Description = $"Vertical position of {beam.Profile.ProfileString}",
                    Part = drawingPart,
                    ModelPart = modelPart,
                    PreferredView = FindBestView(beam, views, "vertical")
                });
            }

            return dimensions;
        }

        private Tekla.Structures.Drawing.View FindBestView(
            Beam beam,
            List<Tekla.Structures.Drawing.View> views,
            string dimensionType)
        {
            // Simple heuristic: find view where beam is most visible
            // In practice, analyze view normal vs beam direction

            foreach (var view in views)
            {
                // Prefer front/side views for length
                // Prefer plan view for horizontal positioning
                // Prefer elevation for vertical positioning

                if (dimensionType == "length" && view.ViewType == View.ViewTypes.FrontView)
                    return view;
                if (dimensionType == "horizontal" && view.ViewType == View.ViewTypes.TopView)
                    return view;
                if (dimensionType == "vertical" && view.ViewType == View.ViewTypes.FrontView)
                    return view;
            }

            return views.FirstOrDefault();
        }

        private List<RequiredDimension> FilterExistingDimensions(
            List<RequiredDimension> required,
            List<DatabaseObject> existing)
        {
            // For now, return all required dimensions
            // In practice, analyze existing dimensions to avoid duplicates
            return required;
        }

        private (bool Sufficient, List<string> SuggestedViews) AnalyzeViewSufficiency(
            List<RequiredDimension> requiredDims,
            List<Tekla.Structures.Drawing.View> existingViews)
        {
            var suggested = new List<string>();
            var hasTopView = existingViews.Any(v => v.ViewType == View.ViewTypes.TopView);
            var hasFrontView = existingViews.Any(v => v.ViewType == View.ViewTypes.FrontView);
            var hasSideView = existingViews.Any(v => v.ViewType == View.ViewTypes.SideView);

            // Check if we need more views for the required dimensions
            var needsTopView = requiredDims.Any(d => d.Type.Contains("Horizontal")) && !hasTopView;
            var needsFrontView = requiredDims.Any(d => d.Type.Contains("Vertical") || d.Type.Contains("Length")) && !hasFrontView;

            if (needsTopView)
                suggested.Add("Top View (for horizontal positioning)");
            if (needsFrontView)
                suggested.Add("Front View (for vertical positioning and lengths)");

            return (suggested.Count == 0, suggested);
        }
    }

    public class AnalysisResult
    {
        public int TotalParts { get; set; }
        public int ExistingDimensions { get; set; }
        public int AvailableViews { get; set; }
        public List<RequiredDimension> MissingDimensions { get; set; } = new List<RequiredDimension>();
        public bool SufficientViews { get; set; }
        public List<string> SuggestedViews { get; set; } = new List<string>();
    }

    public class RequiredDimension
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public Tekla.Structures.Drawing.Part Part { get; set; }
        public Tekla.Structures.Model.Part ModelPart { get; set; }
        public Tekla.Structures.Drawing.View PreferredView { get; set; }
    }
}
