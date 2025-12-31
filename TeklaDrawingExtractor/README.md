# Tekla Drawing Extractor

**Standalone Windows application for extracting Tekla shop drawing data for ML training**

## Overview

This application extracts comprehensive data from Tekla Structures shop drawings, specifically designed to generate high-quality training datasets for machine learning models that predict dimension placement on drawings.

## Key Features

### ✅ Complete Data Extraction
- **Part Properties**: Profile, material, length, class, position codes
- **Dimension Values**: Actual text values with prefix/postfix
- **Bolt Positions**: X,Y,Z coordinates for all bolt positions
- **Chamfer Data**: Chamfer dimensions for beams
- **View Information**: Coordinate systems and view metadata
- **Related Objects**: Object relationships with IDs

### 📊 Organized JSON Output
- **Hierarchical Structure**: Easy to navigate and visualize
- **Grouped by Category**: Model objects, drawing objects, views
- **Statistics Summary**: Object counts and metadata
- **JSONL Support**: Line-delimited format for ML processing

### 🎯 Flexible Filtering
- Filter by drawing marks (e.g., B1, B2, B3)
- Select specific view types (Front, Top, End)
- Limit number of drawings to process
- Include/exclude secondary parts

### ⚙️ Configurable Extraction
- Toggle extraction of profiles, dimensions, bolts, chamfers
- Optional solid vertex extraction (detailed geometry)
- Debug information inclusion
- Pretty or compact JSON formatting

## Installation

### Prerequisites
- Tekla Structures 2024.0 or later
- .NET Framework 4.7.2
- Windows 10/11

### Build from Source
```bash
cd TeklaDrawingExtractor
dotnet build
```

### Run
```bash
dotnet run
```

Or use the compiled executable: `TeklaDrawingExtractor.exe`

## Usage Guide

### Basic Workflow

1. **Open Tekla Structures** with a model containing shop drawings

2. **Launch TeklaDrawingExtractor.exe**

3. **Configure Output Settings**:
   - Select output directory
   - Choose JSON or JSONL format
   - Set indentation (Pretty for viewing, Compact for file size)

4. **Set Extraction Filters**:
   - Enter drawing marks (comma-separated): `B1,B2,B3`
   - Select drawing type: `AssemblyDrawing`
   - Check view types to extract: `FrontView`, `TopView`, `EndView`
   - Set max drawings to process

5. **Enable Extraction Options**:
   - ✅ Extract part profiles & materials (RECOMMENDED)
   - ✅ Extract dimension values (CRITICAL for ML)
   - ✅ Extract bolt positions
   - ✅ Extract chamfer values
   - ☐ Extract solid vertices (only if needed - slow)
   - ☐ Include debug information (for troubleshooting)
   - ☐ Include secondary parts (plates, polybeams)

6. **Choose JSON Organization**:
   - **Hierarchical** (recommended for viewing in editors)
   - **Flat** (better for ML data pipelines)

7. **Click "START EXTRACTION"**

8. **Monitor Progress**:
   - Progress bar shows completion
   - Log shows detailed extraction steps
   - Estimated time remaining displayed

9. **Review Output**:
   - Output file saved to configured directory
   - Filename: `extraction_YYYYMMDD_HHMMSS.json`

### Example Output Structure (Hierarchical)

```json
{
  "drawing_info": {
    "mark": "[B3]",
    "type": "AssemblyDrawing",
    "timestamp": "2024-12-30T10:38:19"
  },
  "views": [
    {
      "view_id": "view_0",
      "name": "FrontView",
      "type": "FrontView"
    }
  ],
  "model_objects": {
    "main_parts": [
      {
        "type": "Part",
        "model_identifier": 768221423,
        "model_object": {
          "id": 768221423,
          "profile": "W21X50",
          "material": "A992",
          "length": 4013.2,
          "class_value": "1"
        },
        "geometry": {
          "part_type_model": "Beam",
          "start_point": {"x": 279.4, "y": 13131.8, "z": 10795.0},
          "end_point": {"x": 279.4, "y": 17145.0, "z": 10795.0},
          "chamfer": {"x": 12.7, "y": 12.7, "dz1": 0, "dz2": 0}
        }
      }
    ],
    "secondary_parts": [],
    "bolts": [
      {
        "type": "Bolt",
        "model_identifier": 769813992,
        "positions": [
          {"x": 0, "y": 0, "z": 0},
          {"x": 0, "y": 76.2, "z": 0}
        ],
        "bolt_size": 19.05,
        "bolt_standard": "A325"
      }
    ]
  },
  "drawing_objects": {
    "dimension_sets": [
      {
        "type": "StraightDimensionSet",
        "distance_to_line": 148.64,
        "direction": "horizontal",
        "dimensions": [
          {
            "type": "StraightDimension",
            "start_point": {"x": 0, "y": 0},
            "end_point": {"x": 152, "y": 0},
            "value_text": "152",
            "value_calculated": 152.0,
            "direction": "horizontal"
          }
        ]
      }
    ],
    "individual_dimensions": [],
    "text_objects": [],
    "marks": []
  },
  "statistics": {
    "model_objects_count": 1,
    "main_parts_count": 1,
    "dimension_sets_count": 6,
    "individual_dimensions_count": 11
  }
}
```

## Data Quality Fixes

This application implements critical fixes for data extraction:

### 1. ✅ Dimension Values
- **Before**: `"Value": "Tekla.Structures.Drawing.ContainerElement"`
- **After**: `"value_text": "152", "value_calculated": 152.0`

### 2. ✅ Part Properties
- **Before**: `"model_object": {}`
- **After**: Complete profile, material, length, class data

### 3. ✅ Bolt Positions
- **Before**: Missing positions
- **After**: `"positions": [{"x": 0, "y": 0, "z": 0}, ...]`

### 4. ✅ Chamfer Values
- **Before**: `"chamfer": "Tekla.Structures.Model.Chamfer"`
- **After**: `"chamfer": {"x": 12.7, "y": 12.7}`

### 5. ✅ DimensionSet Hierarchy
- **Before**: Flat list of dimensions
- **After**: Nested structure with parent-child relationships

## Architecture

### Project Structure
```
TeklaDrawingExtractor/
├── TeklaDrawingExtractor.csproj
├── Program.cs                    # Entry point
├── MainForm.cs                   # GUI
├── Extraction/
│   ├── DrawingExtractor.cs       # Main extraction engine
│   ├── ObjectExtractors/
│   │   ├── PartExtractor.cs      # Part data extraction
│   │   ├── DimensionExtractor.cs # Dimension extraction
│   │   ├── BoltExtractor.cs      # Bolt extraction
│   │   └── ViewExtractor.cs      # View extraction
│   └── DataModels/
│       ├── ExtractionConfig.cs   # Configuration
│       ├── DrawingData.cs        # Data models
│       └── ExtractionResult.cs
└── Utilities/
    ├── GeometryHelper.cs         # Geometry calculations
    ├── JsonOrganizer.cs          # JSON structuring
    └── Logger.cs                 # Progress logging
```

### Key Classes

**DrawingExtractor**: Main orchestrator
- Connects to Tekla
- Iterates through drawings and views
- Coordinates specialized extractors
- Reports progress
- Saves output

**PartExtractor**: Extracts part data
- Uses `GetAllReportProperties()` for complete data
- Handles Beam, ContourPlate, PolyBeam
- Extracts chamfer values
- Optional solid vertex extraction

**DimensionExtractor**: Extracts dimensions
- Handles StraightDimension, RadiusDimension, AngleDimension
- Extracts dimension values from ContainerElement
- Supports DimensionSet hierarchy
- Calculates backup values from coordinates

**BoltExtractor**: Extracts bolt data
- Gets BoltGroup from model
- Extracts all bolt positions
- Includes bolt size, standard, type

**JsonOrganizer**: Structures output
- Hierarchical organization (for viewing)
- Flat organization (for ML processing)
- Groups by category and importance

## Performance

### Benchmarks
- **10 drawings**: ~2 minutes
- **100 drawings**: ~15-20 minutes
- **Memory usage**: < 500 MB

### Optimization Tips
- Disable solid vertex extraction unless needed (10x faster)
- Use compact JSON for large datasets (50% smaller files)
- Filter to specific view types to reduce processing

## Troubleshooting

### "Not connected to Tekla Structures"
- Ensure Tekla is running
- Open a model with shop drawings
- Check Tekla API connection

### "No drawings found"
- Verify drawings exist in the model
- Check drawing marks match exactly
- Try removing mark filter to process all drawings

### Extraction is slow
- Disable "Extract solid vertices"
- Reduce max drawings
- Filter to specific view types

### Output file is huge
- Use compact formatting instead of pretty
- Disable debug information
- Don't include secondary parts
- Disable solid vertices extraction

## License

Internal use only

## Author

Hugo Valente - TeklaMCP Project

## Version

1.0.0 - Initial release
- Complete extraction engine
- GUI with all parameters
- Critical data quality fixes
- Hierarchical JSON organization
- Progress reporting and logging
