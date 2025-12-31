using System;
using System.Collections.Generic;

namespace TeklaDrawingExtractor.Extraction.DataModels
{
    /// <summary>
    /// Configuration for drawing extraction process
    /// </summary>
    public class ExtractionConfig
    {
        // Output settings
        public string OutputDirectory { get; set; } = @"C:\Tekla\Extractions";
        public bool UseHierarchicalJson { get; set; } = true;
        public bool PrettyFormat { get; set; } = true;
        public bool UseJsonL { get; set; } = false;

        // Filters
        public List<string> DrawingMarks { get; set; } = new List<string>();
        public string DrawingType { get; set; } = "AssemblyDrawing";
        public List<string> ViewTypes { get; set; } = new List<string> { "FrontView" };
        public int MaxDrawings { get; set; } = 10;

        // Extraction options
        public bool ExtractProfiles { get; set; } = true;
        public bool ExtractDimensionValues { get; set; } = true;
        public bool ExtractBoltPositions { get; set; } = true;
        public bool ExtractChamfers { get; set; } = true;
        public bool ExtractSolidVertices { get; set; } = false;
        public bool IncludeDebugInfo { get; set; } = false;
        public bool IncludeSecondaryParts { get; set; } = false;

        // Organization options
        public bool GroupByViews { get; set; } = true;
        public bool GroupByObjectType { get; set; } = true;
        public bool GroupByImportance { get; set; } = true;
        public bool MainBeamSeparate { get; set; } = true;
    }
}
