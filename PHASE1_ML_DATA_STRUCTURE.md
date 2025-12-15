# PHASE 1: Optimized ML Data Structure Design

**Date:** 2025-12-15
**Branch:** improvements_ml_optimization_01_data_structure
**Goal:** Restructure extraction data for Deep Learning dimension placement training

---

## 1. Current State Analysis

### Current Extraction Structure
```json
{
  "mark": "[ST1006]",
  "drawing_type": "AssemblyDrawing",
  "sheet_coordinate_system": {},  // ❌ EMPTY - Critical issue!
  "objects": [
    {
      "type": "Tekla.Structures.Drawing.StraightDimension",
      "type_name": "StraightDimension",
      "related_objects": ["Part: Part", "Part: Part"],
      "geometric_data": {
        "dimension_points": []  // ❌ EMPTY - Not extracting points!
      },
      "properties": {
        "StartPoint": "(-235.415, -26.988, 0.000)",  // ✓ String format
        "EndPoint": "(2767.334, -33.338, 0.000)",
        "Distance": "360.437133275036"
      }
    },
    {
      "type": "Tekla.Structures.Drawing.Part",
      "model_object_id": "773641573",  // ✓ Has model linkage
      "geometric_data": {
        "part_geometry": {}  // Nested structure
      }
    }
  ]
}
```

### Problems Identified

1. **❌ Missing View Context**: Objects don't know which view they belong to
2. **❌ Empty Sheet Coordinate System**: No transformation matrix
3. **❌ Missing View Transformation Matrices**: Can't transform 2D→3D
4. **❌ Dimension Points Not Extracted**: GetPoints() returns empty
5. **❌ String Coordinates**: Properties have string coords, not structured data
6. **❌ No Parent-Child Hierarchy**: Parts and dimensions not linked geometrically

---

## 2. Tekla Drawing Object Hierarchy

### From Tekla API Documentation
```
DrawingObject (abstract base)
├─ View (2D container)
│  └─ Has: ViewCoordinateSystem (3D), DisplayCoordinateSystem (2D)
├─ Part (2D representation of 3D model part) ⭐ HYBRID 2D/3D
│  └─ ModelIdentifier → links to Tekla.Structures.Model.Part
├─ Dimension (2D annotation) ⭐ EXTRACTED AS 2D, COMPUTED AS 3D
│  ├─ StraightDimension
│  ├─ AngleDimension
│  └─ DimensionSet
├─ Mark (2D text annotation)
├─ Text (2D text)
├─ Line (2D graphic)
└─ LeaderLine (2D connector)
```

### Classification for ML Training

| Object Type | 2D/3D Nature | ML Training Role |
|-------------|--------------|------------------|
| **View** | Container with 2D↔3D transformation | Provides coordinate transformation |
| **Part** | **HYBRID**: 2D drawing + 3D model link | **Primary 3D geometry** - ground truth |
| **Dimension** | **2D placed, 3D computed** | **Target to predict** - needs 3D interpretation |
| Mark, Text | Pure 2D annotation | Secondary features (labels) |
| Line, LeaderLine | Pure 2D graphics | Visual helpers (ignore for fitting) |

**ANSWER TO USER'S QUESTION:**
> "Is there any other drawing object that we should treat as 3D as well?"

**YES: Only `Part` objects should be treated as 3D**, because:
1. Parts have `ModelIdentifier` linking to 3D model geometry
2. Parts represent actual steel members (beams, plates, bolts)
3. Parts have 3D solid geometry (vertices, bounding boxes)

**Dimensions are NOT 3D objects** - they are 2D annotations that *measure* 3D geometry. We extract them as 2D and compute their 3D interpretation using view transformations.

---

## 3. Proposed Optimized Data Structure

### Design Principles

1. **Single Source of Truth**: Model objects (3D) are authoritative
2. **2D→3D Transformation**: Dimensions stored as 2D with computed 3D projection
3. **View-Centric Organization**: Group objects by parent view
4. **Geometric Linkage**: Explicit dimension→part relationships
5. **ML-Ready Format**: Direct tensors/arrays, no string parsing

### New Structure Schema

```json
{
  "drawing": {
    "mark": "[ST1006]",
    "type": "AssemblyDrawing",
    "timestamp": "20251215_111533"
  },

  "views": [
    {
      "view_id": "view_0",
      "name": "FRONT",
      "type": "FrontView",

      // ⭐ TRANSFORMATION MATRICES (2D ↔ 3D)
      "coordinate_system": {
        "origin_3d": {"x": 0.0, "y": 0.0, "z": 0.0},
        "axis_x": {"x": 1.0, "y": 0.0, "z": 0.0},
        "axis_y": {"x": 0.0, "y": 0.0, "z": 1.0},
        "normal": {"x": 0.0, "y": 1.0, "z": 0.0}
      },

      "display_coordinate_system": {
        "origin_2d": {"x": 100.0, "y": 200.0},
        "scale": 1.0,
        "rotation": 0.0
      },

      // View bounding box
      "bounds": {
        "width": 1200.0,
        "height": 800.0,
        "frame_origin": {"x": -100.0, "y": -50.0}
      }
    }
  ],

  "model_objects": [
    {
      "object_id": "773641573",
      "object_type": "Beam",
      "class": "3d_model_part",

      // ⭐ 3D GEOMETRY (Ground Truth)
      "geometry_3d": {
        "start_point": {"x": 0.0, "y": 0.0, "z": 0.0},
        "end_point": {"x": 5000.0, "y": 0.0, "z": 3000.0},
        "bounding_box": {
          "min": {"x": -106.0, "y": -127.0, "z": 0.0},
          "max": {"x": 5106.0, "y": 127.0, "z": 3000.0}
        },
        "length": 5830.95,
        "profile": "W21X50",
        "material": "A992"
      },

      // ⭐ 2D REPRESENTATIONS (per view)
      "representations_2d": [
        {
          "view_id": "view_0",
          "drawing_object_id": "drawing_part_42",
          "visible": true,
          "projection": {
            "outline_2d": [
              {"x": 50.0, "y": 100.0},
              {"x": 550.0, "y": 400.0},
              {"x": 550.0, "y": 450.0},
              {"x": 50.0, "y": 150.0}
            ]
          }
        }
      ]
    }
  ],

  "dimensions": [
    {
      "dimension_id": "dim_0",
      "dimension_type": "StraightDimension",
      "class": "2d_annotation",

      // ⭐ 2D PLACEMENT (as placed on drawing)
      "placement_2d": {
        "view_id": "view_0",
        "start_point_2d": {"x": -235.415, "y": -26.988},
        "end_point_2d": {"x": 2767.334, "y": -33.338},
        "text_position_2d": {"x": 1265.96, "y": -390.425},
        "dimension_line_offset": 360.437
      },

      // ⭐ 3D INTERPRETATION (computed from view transformation)
      "measurement_3d": {
        "measured_geometry": "edge",  // edge, face, distance
        "start_point_3d": {"x": -235.415, "y": 0.0, "z": -26.988},
        "end_point_3d": {"x": 2767.334, "y": 0.0, "z": -33.338},
        "measured_value": 3002.749,
        "depth_placement": "centerline",  // centerline | nearest | farthest
        "projection_plane": "XZ"  // which 3D plane
      },

      // ⭐ GEOMETRIC RELATIONSHIPS
      "measures": [
        {
          "model_object_id": "773641573",
          "measured_feature": "length",
          "attachment_points": [
            {"type": "endpoint", "location_3d": {"x": -235.415, "y": 0.0, "z": -26.988}},
            {"type": "endpoint", "location_3d": {"x": 2767.334, "y": 0.0, "z": -33.338}}
          ]
        }
      ],

      "text_content": "3'-0\"",
      "distance_value": 3002.749
    }
  ],

  "relationships": {
    "dimension_to_parts": {
      "dim_0": ["773641573", "773641574"]
    },
    "view_to_parts": {
      "view_0": ["773641573", "773641574", "773641575"]
    }
  }
}
```

---

## 4. Dimension 2D→3D Transformation Logic

### User's Suggestion (Correct!)
> "The dimension can be transformed to 3D using the transformation matrix from the view.
> For the depth, we can place the dim in the more convenient plane, either in the
> centerline of the main part or at the nearest or farthest point of the secondary part."

### Implementation Strategy

```python
def transform_dimension_to_3d(dimension_2d, view):
    """
    Transform 2D dimension to 3D interpretation

    INPUT:
      - dimension_2d: {start_2d, end_2d} in drawing coordinates
      - view: {coordinate_system, axis_x, axis_y, normal, origin}

    OUTPUT:
      - dimension_3d: {start_3d, end_3d, plane, depth_placement}
    """
    # Step 1: Project 2D points to 3D plane
    # Using view's coordinate system:
    # point_3d = origin_3d + (x_2d * axis_x) + (y_2d * axis_y)

    start_3d_projected = (
        view.origin_3d +
        dimension_2d.start_x * view.axis_x +
        dimension_2d.start_y * view.axis_y
    )

    end_3d_projected = (
        view.origin_3d +
        dimension_2d.end_x * view.axis_x +
        dimension_2d.end_y * view.axis_y
    )

    # Step 2: Determine depth (normal direction)
    # Options:
    #   a) Centerline of main part (most common)
    #   b) Nearest surface of secondary part
    #   c) Farthest surface of secondary part

    # For a dimension measuring part length:
    depth_strategy = "centerline"  # Place on part centerline

    # For a dimension measuring clearance:
    depth_strategy = "nearest"  # Place at nearest surface

    # Adjust depth based on measured geometry
    depth_offset = get_depth_offset(dimension, parts, depth_strategy)

    start_3d_final = start_3d_projected + depth_offset * view.normal
    end_3d_final = end_3d_projected + depth_offset * view.normal

    return {
        "start_point_3d": start_3d_final,
        "end_point_3d": end_3d_final,
        "projection_plane": get_plane_from_normal(view.normal),
        "depth_placement": depth_strategy
    }
```

### Depth Placement Strategies

| Strategy | Use Case | Example |
|----------|----------|---------|
| **centerline** | Length dimensions along beam axis | Member length, spacing between columns |
| **nearest** | Clearance, gap dimensions | Edge distance for bolts, weld gaps |
| **farthest** | Overall dimensions, envelopes | Total assembly width, overall height |

### Solving 2D→3D Ambiguity

**Problem:** 2D dimension line has no depth information.

**Solutions:**
1. ✅ **Geometric inference** from measured parts (recommended)
   - Find which part features the dimension measures
   - Place dimension on part's centerline/surface

2. ✅ **Heuristic rules** based on dimension type
   - Horizontal/vertical dims → centerline
   - Bolt edge distance → nearest surface
   - Overall dims → farthest extent

3. ✅ **Multiple 3D interpretations** (for ML training)
   - Generate candidate 3D placements
   - ML model learns which placement is typical
   - Ground truth from similar assemblies

---

## 5. Implementation Plan

### Phase 1.1: Extract View Transformations ✅ NEXT
```csharp
// In GenerateFullExtractionCode():
// Already extracting ViewCoordinateSystem and DisplayCoordinateSystem
// Need to: Extract ALL views and their transformations properly
```

### Phase 1.2: Reorganize Data by Views
- Group objects by parent view
- Track view_id for each object
- Build view→objects mapping

### Phase 1.3: Implement 2D→3D Dimension Transformation
- Extract dimension 2D points properly (fix empty dimension_points)
- Compute 3D interpretation using view matrix
- Determine depth placement strategy
- Link dimensions to measured parts

### Phase 1.4: Build Geometric Relationships
- dimension→parts mapping (which parts does this dimension measure?)
- part→dimensions mapping (which dimensions constrain this part?)
- view→parts visibility

---

## 6. ML Training Use Cases

### Use Case 1: Dimension Placement Prediction
**Input:**
- 3D assembly geometry (parts)
- View configuration (camera angle, scale)

**Output:**
- 2D dimension placements
- Dimension types needed
- Text positioning

**Training Data Needed:**
- ✅ Part 3D geometry
- ✅ View transformations
- ✅ Dimension 2D placements
- ✅ Dimension→part relationships

### Use Case 2: Virtual Fitter (Phase 2)
**Input:**
- Part 3D geometries (without positions)
- Dimension values from drawing

**Output:**
- Part positions that satisfy all dimensions
- Missing dimension detection

**Training Data Needed:**
- ✅ 3D part geometries
- ✅ Dimension constraints (3D interpretation)
- ✅ Ground truth assembly configuration

---

## 7. Next Steps

### Immediate (Phase 1.1)
1. Fix view coordinate system extraction (currently empty)
2. Extract ALL views in drawing (not just sheet)
3. Store view transformation matrices
4. Test view extraction with sample drawing

### Short-term (Phase 1.2-1.4)
1. Fix dimension point extraction (GetPoints() not working)
2. Implement dimension→part linkage
3. Add 2D→3D transformation computation
4. Validate against known assemblies

### Phase 2 (Virtual Fitter Tool)
1. Design constraint system from dimensions
2. Implement OR solver (PuLP, OR-Tools, Gurobi)
3. Build assembly reconstruction algorithm
4. Test on simple→complex assemblies

---

**Status:** Design Complete ✅
**Next:** Implement Phase 1.1 - View Transformation Extraction
