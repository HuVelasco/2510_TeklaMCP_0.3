using System;
using System.Collections.Generic;

namespace TeklaDrawingExtractor.Extraction.DataModels
{
    /// <summary>
    /// RESTRUCTURED: Drawing data for ML training
    /// Model objects are unique with view-specific data
    /// Drawing objects are organized by view
    /// </summary>
    public class DrawingData
    {
        public string DrawingMark { get; set; }
        public string DrawingType { get; set; }
        public DateTime Timestamp { get; set; }

        // Views metadata
        public List<ViewInfo> Views { get; set; } = new List<ViewInfo>();

        // Model objects (unique, no repetition across views)
        public List<PartInfo> Parts { get; set; } = new List<PartInfo>();
        public List<BoltGroupInfo> BoltGroups { get; set; } = new List<BoltGroupInfo>();

        // Drawing objects (view-specific)
        public List<DimensionInfo> Dimensions { get; set; } = new List<DimensionInfo>();
        public List<MarkInfo> Marks { get; set; } = new List<MarkInfo>();
        public List<TextInfo> TextObjects { get; set; } = new List<TextInfo>();

        // Statistics
        public int ModelObjectsCount => Parts.Count + BoltGroups.Count;
        public int DimensionsCount => Dimensions.Count;
    }

    /// <summary>
    /// View metadata
    /// </summary>
    public class ViewInfo
    {
        public string ViewId { get; set; }
        public int ViewIndex { get; set; }
        public string Name { get; set; }
        public string ViewType { get; set; }
        public object CoordinateSystem3D { get; set; }
        public object CoordinateSystem2D { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    /// <summary>
    /// Part with view-specific data
    /// </summary>
    public class PartInfo
    {
        // Model properties (static)
        public int ModelId { get; set; }
        public string Guid { get; set; }
        public string PartType { get; set; } // Beam, ContourPlate, PolyBeam
        public string Profile { get; set; }
        public string Material { get; set; }
        public string Name { get; set; }
        public string Finish { get; set; }
        public string Class { get; set; }
        public double Length { get; set; }
        public string PartPos { get; set; }
        public string AssemblyPos { get; set; }
        public bool IsMainPart { get; set; } // From GetReportProperty("MAIN_PART")

        // Geometry (global coordinates)
        public object GeometryGlobal { get; set; }

        // View-specific data
        public List<PartViewInfo> ViewData { get; set; } = new List<PartViewInfo>();
    }

    /// <summary>
    /// Part data in a specific view
    /// </summary>
    public class PartViewInfo
    {
        public string ViewId { get; set; }
        public bool Visible { get; set; }
        public object GeometryLocal { get; set; } // Local 2D coordinates
        public List<string> RelatedDrawingObjects { get; set; } = new List<string>(); // Dimensions, marks in this view
    }

    /// <summary>
    /// Bolt group with view-specific data
    /// </summary>
    public class BoltGroupInfo
    {
        // Model properties (static)
        public int ModelId { get; set; }
        public string Guid { get; set; }
        public string BoltSize { get; set; }
        public string BoltStandard { get; set; }
        public string BoltType { get; set; }
        public string ThreadInMaterial { get; set; }
        public double CutLength { get; set; }
        public double ExtraLength { get; set; }

        // Individual bolt positions (global 3D)
        public List<Position3D> PositionsGlobal { get; set; } = new List<Position3D>();

        // View-specific data
        public List<BoltViewInfo> ViewData { get; set; } = new List<BoltViewInfo>();
    }

    /// <summary>
    /// Bolt data in a specific view
    /// </summary>
    public class BoltViewInfo
    {
        public string ViewId { get; set; }
        public bool Visible { get; set; }
        public List<Position2D> PositionsLocal { get; set; } = new List<Position2D>(); // Local 2D coordinates
        public List<string> RelatedDrawingObjects { get; set; } = new List<string>();
    }

    /// <summary>
    /// 3D position
    /// </summary>
    public class Position3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }

    /// <summary>
    /// 2D position
    /// </summary>
    public class Position2D
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    /// <summary>
    /// Dimension with enhanced data
    /// </summary>
    public class DimensionInfo
    {
        public string ViewId { get; set; }
        public string DimensionId { get; set; }
        public string Type { get; set; }

        // Value information
        public string ValueText { get; set; } // Full text
        public double ValueMm { get; set; } // Numeric value in mm
        public string Prefix { get; set; }
        public string Postfix { get; set; }

        // Direction
        public string DirectionString { get; set; }
        public DirectionVector DirectionVector { get; set; }

        // Geometry
        public object StartPoint { get; set; }
        public object EndPoint { get; set; }
        public double DistanceToLine { get; set; }

        // Related objects
        public List<string> RelatedModelObjects { get; set; } = new List<string>();
        public List<string> RelatedDrawingObjects { get; set; } = new List<string>();
    }

    /// <summary>
    /// Direction vector
    /// </summary>
    public class DirectionVector
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }

    /// <summary>
    /// Mark with identification
    /// </summary>
    public class MarkInfo
    {
        public string ViewId { get; set; }
        public string MarkId { get; set; }
        public string MarkType { get; set; }
        public object InsertionPoint { get; set; }
        public int? ModelIdentifier { get; set; }
        public string Text { get; set; }
        public List<string> RelatedModelObjects { get; set; } = new List<string>();
        public List<string> RelatedDrawingObjects { get; set; } = new List<string>();
    }

    /// <summary>
    /// Text object
    /// </summary>
    public class TextInfo
    {
        public string ViewId { get; set; }
        public string TextId { get; set; }
        public string Text { get; set; }
        public object InsertionPoint { get; set; }
        public List<string> RelatedModelObjects { get; set; } = new List<string>();
        public List<string> RelatedDrawingObjects { get; set; } = new List<string>();
    }

    /// <summary>
    /// Progress information during extraction
    /// </summary>
    public class ExtractionProgress
    {
        public int DrawingsProcessed { get; set; }
        public int TotalDrawings { get; set; }
        public int ObjectsExtracted { get; set; }
        public string CurrentDrawing { get; set; }
        public string CurrentView { get; set; }
        public TimeSpan Elapsed { get; set; }
        public TimeSpan EstimatedRemaining { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Result of extraction operation
    /// </summary>
    public class ExtractionResult
    {
        public bool Success { get; set; }
        public List<DrawingData> DrawingDataList { get; set; } = new List<DrawingData>();
        public int TotalObjectsExtracted { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public string OutputFilePath { get; set; }
    }
}
