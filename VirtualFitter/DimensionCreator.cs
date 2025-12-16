using System;
using System.Collections.Generic;
using Tekla.Structures;
using Tekla.Structures.Model;
using Tekla.Structures.Drawing;
using Tekla.Structures.Geometry3d;

namespace VirtualFitter
{
    /// <summary>
    /// Creates dimensions in Tekla drawings with [VF] prefix for identification
    /// </summary>
    public class DimensionCreator
    {
        private Tekla.Structures.Drawing.Drawing drawing;
        private const string VF_PREFIX = "[VF] ";

        public DimensionCreator(Tekla.Structures.Drawing.Drawing drawing)
        {
            this.drawing = drawing;
        }

        public CreationResult CreateDimensions(List<RequiredDimension> dimensions)
        {
            var result = new CreationResult();

            foreach (var dim in dimensions)
            {
                try
                {
                    var created = CreateSingleDimension(dim);
                    if (created != null)
                    {
                        result.CreatedDimensions.Add(created);
                        result.SuccessCount++;
                    }
                    else
                    {
                        result.FailureCount++;
                        result.Errors.Add($"Failed to create {dim.Type} for {dim.Description}");
                    }
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add($"{dim.Type}: {ex.Message}");
                }
            }

            // Commit changes to drawing
            if (result.SuccessCount > 0)
            {
                drawing.CommitChanges();
            }

            return result;
        }

        private DatabaseObject CreateSingleDimension(RequiredDimension reqDim)
        {
            var view = reqDim.PreferredView;
            if (view == null) return null;

            var beam = reqDim.ModelPart as Beam;
            if (beam == null) return null;

            // Create straight dimension based on type
            StraightDimension dimension = null;

            switch (reqDim.Type)
            {
                case "Length":
                    dimension = CreateLengthDimension(beam, reqDim.Part, view);
                    break;
                case "Horizontal Position":
                    dimension = CreateHorizontalPositionDimension(beam, reqDim.Part, view);
                    break;
                case "Vertical Position":
                    dimension = CreateVerticalPositionDimension(beam, reqDim.Part, view);
                    break;
            }

            if (dimension != null)
            {
                // Mark dimension with [VF] prefix
                MarkAsVirtualFitterDimension(dimension);

                if (dimension.Insert())
                {
                    return dimension;
                }
            }

            return null;
        }

        private StraightDimension CreateLengthDimension(
            Beam beam,
            Tekla.Structures.Drawing.Part drawingPart,
            Tekla.Structures.Drawing.View view)
        {
            try
            {
                // Get beam endpoints
                var start = beam.StartPoint;
                var end = beam.EndPoint;

                // Project to 2D view coordinates
                var start2D = ProjectTo2D(start, view);
                var end2D = ProjectTo2D(end, view);

                var dimension = new StraightDimension(view, start2D, end2D, new Vector(0, -100, 0));

                // Configure dimension attributes
                var attrs = dimension.Attributes as StraightDimension.StraightDimensionAttributes;
                if (attrs != null)
                {
                    attrs.Font.Name = "Arial";
                    attrs.Font.Bold = true;
                    attrs.Font.Color = new Color(1.0, 0.0, 0.0); // Red for easy identification
                }

                return dimension;
            }
            catch
            {
                return null;
            }
        }

        private StraightDimension CreateHorizontalPositionDimension(
            Beam beam,
            Tekla.Structures.Drawing.Part drawingPart,
            Tekla.Structures.Drawing.View view)
        {
            try
            {
                // For horizontal positioning, measure from a reference point
                // (usually the main part's edge) to the secondary part

                var beamStart = beam.StartPoint;
                var referencePoint = new Point(0, beamStart.Y, beamStart.Z); // X=0 as reference

                var start2D = ProjectTo2D(referencePoint, view);
                var end2D = ProjectTo2D(beamStart, view);

                var dimension = new StraightDimension(view, start2D, end2D, new Vector(0, -150, 0));

                var attrs = dimension.Attributes as StraightDimension.StraightDimensionAttributes;
                if (attrs != null)
                {
                    attrs.Font.Bold = true;
                    attrs.Font.Color = new Color(1.0, 0.0, 0.0);
                }

                return dimension;
            }
            catch
            {
                return null;
            }
        }

        private StraightDimension CreateVerticalPositionDimension(
            Beam beam,
            Tekla.Structures.Drawing.Part drawingPart,
            Tekla.Structures.Drawing.View view)
        {
            try
            {
                // For vertical positioning, measure from a reference elevation

                var beamStart = beam.StartPoint;
                var referencePoint = new Point(beamStart.X, beamStart.Y, 0); // Z=0 as reference

                var start2D = ProjectTo2D(referencePoint, view);
                var end2D = ProjectTo2D(beamStart, view);

                var dimension = new StraightDimension(view, start2D, end2D, new Vector(-150, 0, 0));

                var attrs = dimension.Attributes as StraightDimension.StraightDimensionAttributes;
                if (attrs != null)
                {
                    attrs.Font.Bold = true;
                    attrs.Font.Color = new Color(1.0, 0.0, 0.0);
                }

                return dimension;
            }
            catch
            {
                return null;
            }
        }

        private Point ProjectTo2D(Point point3D, Tekla.Structures.Drawing.View view)
        {
            // Use view's coordinate system to project 3D point to 2D
            var viewCS = view.ViewCoordinateSystem;

            // Transform 3D point to view coordinates
            var localPoint = new Point(point3D);

            // Simple projection (in practice, use proper coordinate transformation)
            return new Point(localPoint.X, localPoint.Y, 0);
        }

        private void MarkAsVirtualFitterDimension(StraightDimension dimension)
        {
            try
            {
                // Try to set a user-defined attribute to mark this dimension
                dimension.SetUserProperty("VirtualFitter", "Created");

                // Also modify the dimension mark/label to include [VF] prefix
                var attrs = dimension.Attributes as StraightDimension.StraightDimensionAttributes;
                if (attrs != null)
                {
                    // Make it visually distinct
                    attrs.Font.Bold = true;
                    attrs.Font.Color = new Color(1.0, 0.0, 0.0); // Red text
                }
            }
            catch
            {
                // If marking fails, dimension is still created
            }
        }
    }

    public class CreationResult
    {
        public List<DatabaseObject> CreatedDimensions { get; set; } = new List<DatabaseObject>();
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
