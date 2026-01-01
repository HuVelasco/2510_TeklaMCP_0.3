# TeklaMCP ML Training Data Extraction - Project Summary

## Overview

This project implements a comprehensive data extraction system for Tekla Structures shop drawings to generate machine learning training data for automated dimension placement.

## What Data We Can Extract

### 1. Part Geometry (Complete Part Hierarchy)

| Part Type | Geometry Data Extracted |
|-----------|------------------------|
| **Beam** | `start_point`, `end_point`, `axis_length`, `axis_direction` |
| **PolyBeam** | `contour_points[]` with chamfer data (x, y, z, chamfer type) |
| **ContourPlate** | `contour_points[]` with chamfer data (polygon outline) |
| **BentPlate** | `leg_sections_count` |
| **Brep** | `start_point`, `end_point` |
| **LoftedPlate** | `base_curves[]` with start/end points |
| **SpiralBeam** | `start_point`, `end_point`, `total_rise`, `rotation_angle` |

**Additional Part Data:**
- `part_type_model` - Specific type from Model API (Beam, PolyBeam, etc.)
- `profile` - Profile string (e.g., "W21X50")
- `material` - Material specification
- `bounding_box` - 3D min/max coordinates

### 2. BoltGroup Data

| Property | Description |
|----------|-------------|
| `bolt_type` | BoltGroup subtype |
| `bolt_size` | Diameter in mm |
| `first_position` | First bolt position (x, y, z) |
| `second_position` | Second bolt position |
| `bolt_positions[]` | All bolt positions with index |
| `bolt_count` | Number of bolts |
| `hole_diameter` | Calculated based on hole type |
| `part_to_bolt_through` | Primary part ID |
| `part_to_be_bolted` | Secondary part ID |
| `other_bolted_parts[]` | Additional connected parts |

### 3. Connection Data

| Property | Description |
|----------|-------------|
| `connection_type` | Type (Connection, Component, Detail, Seam) |
| `name` | Connection name |
| `number` | Connection number |
| `code` | Connection code |
| `primary_object` | Main part ID |
| `secondary_objects[]` | Connected part IDs |
| `sub_objects[]` | Child objects (bolts, welds, plates) with type and ID |

### 4. Weld Data

| Property | Description |
|----------|-------------|
| `weld_type` | Weld/PolygonWeld |
| `type_above` | Weld type above line |
| `type_below` | Weld type below line |
| `size_above` | Size specification |
| `size_below` | Size specification |
| `main_object` | Primary connected part |
| `secondary_object` | Secondary connected part |
| `weld_polygon[]` | Polygon points (for PolygonWeld) |

### 5. Dimension Data

| Property | Description |
|----------|-------------|
| `start_point`, `end_point` | Dimension line endpoints |
| `value` | Dimension value |
| `distance` | Measured distance |
| `up_direction` | Dimension orientation vector |
| `insertion_point` | Text placement location |
| `dimension_points[]` | All measurement points |

### 6. Drawing Object Data

| Object Type | Count Example | Key Properties |
|-------------|---------------|----------------|
| Part | 462 | ModelIdentifier, profile, material |
| StraightDimension | 157 | StartPoint, EndPoint, Value, Distance, UpDirection |
| LeaderLine | 143 | StartPoint, EndPoint, ElbowPoints |
| Mark | 53 | InsertionPoint, Placing |
| View | 12 | ViewType, ViewCoordinateSystem, RestrictionBox |
| StraightDimensionSet | 50 | Distance, child dimensions |
| Connection | 11 | ModelIdentifier |
| AngleDimension | 4 | Origin, Point1, Point2, Distance |

## Data Structure Approach

Two approaches documented (see `/docs/DATA_STRUCTURE_*.md`):

### Version 1: Drawing-Centric (Recommended for Initial ML)
- Primary source: Drawing API
- Enrich with Model API data
- 2D dimension coordinates preserved as-is
- 3D model coordinates for part geometry
- Simpler implementation, easier debugging

### Version 2: Model-Centric
- Primary source: Model API
- Project 2D annotations to 3D
- Unified coordinate system
- Better for 3D spatial pattern learning
- More complex, requires view transformation

## How to Use for ML Training

### Feature Engineering

**Input Features:**
```python
# Part features
part_type           # "Beam", "PolyBeam", "ContourPlate", etc.
profile_category    # W, HSS, L, C, etc.
axis_length         # For beams
axis_direction      # Unit vector
bounding_box        # Min/max 3D coordinates
contour_points      # For plates/polybeams

# Context features
neighboring_parts   # Connected parts via bolts/welds
connection_types    # Types of connections to part
view_type           # "FrontView", "SectionView", etc.

# Bolt/connection features
bolt_pattern        # Positions relative to part
hole_count          # Number of holes
```

**Output Features (Dimension Placement):**
```python
dimension_type      # "length", "height", "edge_distance", etc.
offset_from_part    # Distance from part edge
offset_direction    # UP, DOWN, LEFT, RIGHT
snap_points         # Which geometry points to dimension
```

### Training Pipeline

1. **Extract drawings** using `tekla_extract_drawing_objects`
2. **Process JSON output** to normalize coordinates
3. **Generate training pairs** (part + context) -> (dimension placement)
4. **Train model** (e.g., random forest, neural network)
5. **Validate** against held-out drawings

## Current Status

### Working
- Discovery mode: Scans drawings, catalogs object types
- Part type identification (Beam, PolyBeam, etc.)
- Model API data enrichment (profiles, materials)
- Dimension extraction with UpDirection and Distance
- View coordinate system extraction

### In Progress
- Full extraction mode: C# 5.0 compatibility fixes for some collection types
- ArrayList iteration patterns need refinement for Tekla runtime

### Verified Discovery Output
```
Drawings Scanned: 1
Total Objects: 941
Unique Types Found: 16

Type breakdown:
- Part: 462
- StraightDimension: 157
- LeaderLine: 143
- Mark: 53
- StraightDimensionSet: 50
- View: 12
- Connection: 11
- And more...
```

## Technical Notes

### C# 5.0 Compatibility (Tekla Runtime)
Tekla's script execution uses C# 5.0 compiler. Key restrictions:
- No pattern matching (`is Type variable`)
- No string interpolation (`$"{var}"`)
- Use `as Type` + null check instead
- Use string concatenation instead
- ArrayList requires `.Count` and `[index]` instead of `foreach`

### API Methods and Return Types
```csharp
sheet.GetAllObjects()        -> DrawingObjectEnumerator (has MoveNext)
boltGroup.GetOtherPartsToBolt() -> ModelObjectEnumerator (has MoveNext)
polyBeam.Contour.ContourPoints  -> ArrayList (use indexing)
```

## Files Modified

- `Server/TeklaMCP/ProgramClean.cs` - Main extraction code
- `docs/DATA_STRUCTURE_VERSION_1_DRAWING_CENTRIC.md`
- `docs/DATA_STRUCTURE_VERSION_2_MODEL_CENTRIC.md`
- `docs/DATA_STRUCTURE_COMPARISON.md`
- `docs/DATA_STRUCTURE_RECOMMENDATION.md`

## Next Steps

1. Complete C# 5.0 ArrayList compatibility fixes
2. Test full extraction on multiple drawings
3. Generate training dataset
4. Develop feature extraction pipeline
5. Train initial ML model for dimension placement
6. Validate and iterate

---
*Generated: 2025-12-19*
*Branch: improvements_ml_optimization_01_data_structure*
