# Data Structure Version 1: Drawing-Centric Approach

## Overview

Version 1 extracts ALL data from the Drawing API, accessing the Model only to retrieve
additional properties for objects that have model links. The drawing is the primary
source of truth.

```
Drawing (2D)                    Model (3D)
┌─────────────────────────┐     ┌─────────────────────────┐
│  Drawing.Part ──────────┼────►│  Model.Part             │
│  Drawing.Bolt ──────────┼────►│  Model.BoltGroup        │
│  Drawing.Connection ────┼────►│  Model.BaseComponent    │
│  Drawing.Weld ──────────┼────►│  Model.BaseWeld         │
│                         │     │                         │
│  StraightDimension      │     │  (No model equivalent)  │
│  Mark                   │     │  (No model equivalent)  │
│  LeaderLine             │     │  (No model equivalent)  │
└─────────────────────────┘     └─────────────────────────┘
```

## Data Flow

```
┌────────────────────────────────────────────────────────────────────────┐
│                         EXTRACTION PROCESS                              │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  1. Open Drawing (ST1006)                                               │
│         ↓                                                               │
│  2. Iterate Drawing.GetSheet().GetAllObjects()                          │
│         ↓                                                               │
│  3. For each DrawingObject:                                             │
│      ├── If Part/Bolt/Connection/Weld:                                  │
│      │     └── Get ModelIdentifier → SelectModelObject() → Enrich       │
│      └── If Dimension/Mark/LeaderLine:                                  │
│            └── Extract 2D properties directly                           │
│         ↓                                                               │
│  4. Group by View (preserve view context)                               │
│         ↓                                                               │
│  5. Output structured JSON                                              │
│                                                                         │
└────────────────────────────────────────────────────────────────────────┘
```

## Complete Data Structure

```json
{
  "drawing": {
    "mark": "ST1006",
    "type": "AssemblyDrawing",
    "name": "STAIR ASSEMBLY",
    "extraction_timestamp": "2025-12-17T10:00:00Z"
  },

  "model_objects": {
    "PARTS": {
      "773641573": {
        "drawing_type": "Tekla.Structures.Drawing.Part",
        "model_type": "Beam",
        "model_id": "773641573",

        "classification": {
          "category": "stair_component",
          "part_name": "STRINGER",
          "assembly_mark": "ST",
          "piece_mark": "c",
          "profile": "MC12X31",
          "material": "A36"
        },

        "geometry": {
          "defining_points": {
            "type": "beam_axis",
            "start_point": {"x": 7077.08, "y": 3409.95, "z": 3004.46},
            "end_point": {"x": 7077.08, "y": 3409.95, "z": 3461.99}
          },
          "bounding_box": {
            "min": {"x": 7077.07, "y": 3317.88, "z": 3004.46},
            "max": {"x": 7381.88, "y": 3409.95, "z": 3461.99}
          },
          "solid_vertices": ["... optional detailed vertices ..."]
        },

        "position": {
          "depth": "BEHIND",
          "plane": "MIDDLE",
          "rotation": "TOP"
        },

        "drawing_appearances": [
          {
            "view_id": "view_0",
            "view_type": "FrontView",
            "bounding_box_2d": {"min": {"x": 100, "y": 50}, "max": {"x": 250, "y": 300}},
            "visibility": "full"
          },
          {
            "view_id": "view_1",
            "view_type": "SectionView",
            "bounding_box_2d": {"min": {"x": 400, "y": 100}, "max": {"x": 450, "y": 200}},
            "visibility": "partial"
          }
        ]
      },

      "773481986": {
        "drawing_type": "Tekla.Structures.Drawing.Part",
        "model_type": "PolyBeam",
        "model_id": "773481986",

        "classification": {
          "part_name": "BENT PLATE",
          "profile": "PL3.175*1270",
          "material": "A36"
        },

        "geometry": {
          "defining_points": {
            "type": "contour_points",
            "points": [
              {"x": 7500.93, "y": 3409.95, "z": 3619.50, "chamfer": null},
              {"x": 7600.00, "y": 3500.00, "z": 3700.00, "chamfer": {"type": "Line", "x": 10, "y": 10}},
              {"x": 7836.10, "y": 4679.95, "z": 3831.77, "chamfer": null}
            ]
          },
          "bounding_box": {
            "min": {"x": 7500.93, "y": 3409.95, "z": 3619.50},
            "max": {"x": 7836.10, "y": 4679.95, "z": 3831.77}
          }
        },

        "drawing_appearances": ["..."]
      },

      "... ContourPlate example ...": {
        "model_type": "ContourPlate",
        "geometry": {
          "defining_points": {
            "type": "contour_points",
            "points": [
              {"x": 0, "y": 0, "z": 0},
              {"x": 500, "y": 0, "z": 0},
              {"x": 500, "y": 300, "z": 0},
              {"x": 0, "y": 300, "z": 0}
            ]
          },
          "thickness": "10mm"
        }
      }
    },

    "BOLTS": {
      "789123456": {
        "drawing_type": "Tekla.Structures.Drawing.Bolt",
        "model_type": "BoltArray",
        "model_id": "789123456",

        "bolt_properties": {
          "standard": "AISC",
          "size": "3/4",
          "length": 50,
          "hole_type": "STD"
        },

        "geometry": {
          "first_position": {"x": 7100, "y": 3350, "z": 3200},
          "second_position": {"x": 7100, "y": 3350, "z": 3400},
          "bolt_positions": [
            {"x": 7100, "y": 3350, "z": 3200},
            {"x": 7100, "y": 3350, "z": 3275},
            {"x": 7100, "y": 3350, "z": 3350},
            {"x": 7100, "y": 3350, "z": 3400}
          ],
          "hole_positions": [
            {"x": 7100, "y": 3350, "z": 3200, "diameter": 21.0},
            {"x": 7100, "y": 3350, "z": 3275, "diameter": 21.0},
            {"x": 7100, "y": 3350, "z": 3350, "diameter": 21.0},
            {"x": 7100, "y": 3350, "z": 3400, "diameter": 21.0}
          ]
        },

        "connected_parts": ["773641573", "773481986"],

        "drawing_appearances": ["..."]
      }
    },

    "CONNECTIONS": {
      "connection_12345": {
        "drawing_type": "Tekla.Structures.Drawing.Connection",
        "model_type": "Connection",
        "model_id": "connection_12345",

        "connection_info": {
          "name": "Clip angle (144)",
          "number": 144,
          "code": "Clip angle"
        },

        "connected_parts": {
          "primary": "773641573",
          "secondary": ["773481986"]
        },

        "sub_objects": {
          "bolts": ["789123456"],
          "welds": ["weld_001"],
          "secondary_parts": ["plate_002"]
        },

        "drawing_appearances": ["..."]
      }
    },

    "WELDS": {
      "weld_001": {
        "drawing_type": "Tekla.Structures.Drawing.Weld",
        "model_type": "Weld",
        "model_id": "weld_001",

        "weld_properties": {
          "type": "WELD_TYPE_FILLET",
          "size_above": 6.0,
          "size_below": 0.0,
          "length_above": 100.0,
          "intermittent_type": "WELD_INTERMITTENT_NONE"
        },

        "geometry": {
          "main_weld_polygon": [
            {"x": 7100, "y": 3350, "z": 3200},
            {"x": 7100, "y": 3350, "z": 3400}
          ]
        },

        "connected_parts": {
          "main_object": "773641573",
          "secondary_object": "773481986"
        },

        "drawing_appearances": ["..."]
      }
    }
  },

  "views": {
    "view_0": {
      "view_id": "view_0",
      "view_type": "FrontView",
      "view_name": "FRONT",

      "coordinate_system": {
        "origin": {"x": 7200, "y": 3500, "z": 3300},
        "axis_x": {"x": 1, "y": 0, "z": 0},
        "axis_y": {"x": 0, "y": 0, "z": 1},
        "normal": {"x": 0, "y": -1, "z": 0}
      },

      "scale": 0.1,
      "frame_origin_2d": {"x": 50, "y": 100},

      "visible_parts": ["773641573", "773481986", "..."],

      "drawing_objects": {
        "dimensions": [
          {
            "type": "StraightDimension",
            "object_id": "dim_001",

            "geometry_2d": {
              "start_point": {"x": 100, "y": 50},
              "end_point": {"x": 250, "y": 50},
              "text_position": {"x": 175, "y": 70}
            },

            "placement": {
              "distance": 360.44,
              "up_direction": {"x": 0, "y": 1, "z": 0},
              "side": "TOP"
            },

            "measured_value": 1270.0,
            "measured_parts": ["773641573"],
            "dimension_type": "overall_length"
          },
          {
            "type": "StraightDimensionSet",
            "object_id": "dimset_001",

            "child_dimensions": [
              {"start": 100, "end": 150, "value": 635.0},
              {"start": 150, "end": 200, "value": 317.5},
              {"start": 200, "end": 250, "value": 317.5}
            ],

            "placement": {
              "distance": 287.70,
              "up_direction": {"x": 0, "y": 1, "z": 0}
            },

            "measured_parts": ["773641573", "773481986"]
          }
        ],

        "marks": [
          {
            "type": "Mark",
            "object_id": "mark_001",
            "text_content": "MC12X31",
            "position_2d": {"x": 175, "y": 150},
            "attached_to_part": "773641573"
          }
        ],

        "leader_lines": [
          {
            "type": "LeaderLine",
            "start_point": {"x": 175, "y": 150},
            "end_point": {"x": 200, "y": 175},
            "elbow_points": [{"x": 190, "y": 160}]
          }
        ]
      }
    },

    "view_1": {
      "view_type": "SectionView",
      "section_cut_origin": {"x": 7200, "y": 3500, "z": 3300},
      "section_cut_direction": {"x": 1, "y": 0, "z": 0},
      "drawing_objects": {"...": "..."}
    }
  },

  "metadata": {
    "unique_parts_count": 42,
    "unique_bolts_count": 15,
    "unique_connections_count": 8,
    "unique_welds_count": 12,
    "total_dimensions": 211,
    "total_marks": 53,
    "views_count": 12
  }
}
```

## Object Type Hierarchy and Required Data

### Part Hierarchy

| Model Type | Defining Geometry | Key Properties |
|------------|-------------------|----------------|
| **Beam** | StartPoint, EndPoint | Profile, Material, Position |
| **PolyBeam** | Contour.ContourPoints[] | Profile, Material, Position |
| **ContourPlate** | Contour.ContourPoints[] | Profile (thickness), Material |
| **BentPlate** | Contour + Bend info | Profile, BendRadius |
| **LoftedPlate** | LoftedContours[] | Profile |
| **SpiralBeam** | Start/End + TwistAngle | Profile, TotalRise |
| **Brep** | Solid vertices | Material |

### BoltGroup Hierarchy

| Model Type | Defining Geometry | Key Properties |
|------------|-------------------|----------------|
| **BoltArray** | FirstPosition, SecondPosition, Distances | Size, Standard, HoleType |
| **BoltCircle** | CenterPoint, Radius, Count | Size, Standard |
| **BoltXYList** | BoltPositions[] | Size, Standard |

### Connection/Component Hierarchy

| Model Type | Key Data | Sub-Objects |
|------------|----------|-------------|
| **Connection** | Name, Number, Code | Bolts, Welds, Secondary Parts |
| **Component** | Name, Number | Created Objects |
| **Detail** | Name, Number | Created Objects |
| **Seam** | Name | Weld info |

### Weld Hierarchy

| Model Type | Defining Geometry | Key Properties |
|------------|-------------------|----------------|
| **Weld** | MainObject, SecondaryObject polygon | Type, Size, Length |
| **PolygonWeld** | Polygon points | Type, Size |
| **LogicalWeld** | Logical grouping | Type |

## Extraction Code Requirements

### For Parts (Priority 1)

```csharp
// Cast to specific type for proper geometry extraction
var modelPart = modelObj as Tekla.Structures.Model.Part;

if (modelPart is Tekla.Structures.Model.Beam beam)
{
    geometry["type"] = "beam_axis";
    geometry["start_point"] = PointToDict(beam.StartPoint);
    geometry["end_point"] = PointToDict(beam.EndPoint);
}
else if (modelPart is Tekla.Structures.Model.PolyBeam polyBeam)
{
    geometry["type"] = "contour_points";
    geometry["points"] = ExtractContourPoints(polyBeam.Contour.ContourPoints);
}
else if (modelPart is Tekla.Structures.Model.ContourPlate plate)
{
    geometry["type"] = "contour_points";
    geometry["points"] = ExtractContourPoints(plate.Contour.ContourPoints);
}
// ... handle BentPlate, LoftedPlate, SpiralBeam, Brep
```

### For Bolts (Priority 1)

```csharp
var modelBolt = modelObj as Tekla.Structures.Model.BoltGroup;

boltData["first_position"] = PointToDict(modelBolt.FirstPosition);
boltData["second_position"] = PointToDict(modelBolt.SecondPosition);

// Get individual bolt positions
var boltPositions = new List<object>();
foreach (var point in modelBolt.BoltPositions)
{
    boltPositions.Add(PointToDict(point as Point));
}
boltData["bolt_positions"] = boltPositions;

// Get hole info
boltData["hole_type"] = modelBolt.HoleType.ToString();
boltData["hole_diameter"] = modelBolt.HoleType == BoltGroup.BoltHoleTypeEnum.HOLE_TYPE_OVERSIZED
    ? modelBolt.BoltSize + 3.0
    : modelBolt.BoltSize + 1.5;
```

### For Connections (Priority 1)

```csharp
var modelConnection = modelObj as Tekla.Structures.Model.Connection;

connectionData["name"] = modelConnection.Name;
connectionData["number"] = modelConnection.Number;
connectionData["code"] = modelConnection.Code;

// Get connected parts
connectionData["primary"] = modelConnection.GetPrimaryObject()?.Identifier.ID;
var secondaries = new List<int>();
foreach (var sec in modelConnection.GetSecondaryObjects())
{
    secondaries.Add(sec.Identifier.ID);
}
connectionData["secondary"] = secondaries;

// Get sub-objects created by connection
var subObjects = new List<object>();
foreach (var child in modelConnection.GetChildren())
{
    subObjects.Add(new { type = child.GetType().Name, id = child.Identifier.ID });
}
connectionData["sub_objects"] = subObjects;
```

### For Welds (Priority 1)

```csharp
var modelWeld = modelObj as Tekla.Structures.Model.BaseWeld;

weldData["type"] = modelWeld.TypeAbove.ToString();
weldData["size_above"] = modelWeld.SizeAbove;
weldData["size_below"] = modelWeld.SizeBelow;
weldData["length_above"] = modelWeld.LengthAbove;

weldData["main_object_id"] = modelWeld.MainObject?.Identifier.ID;
weldData["secondary_object_id"] = modelWeld.SecondaryObject?.Identifier.ID;

// Get weld polygon if available
if (modelWeld is Tekla.Structures.Model.Weld weld)
{
    weldData["polygon"] = ExtractPolygon(weld.Polygon);
}
```

## Advantages of Version 1

1. **Direct relationship**: Drawing objects directly link to model objects
2. **View context preserved**: Know exactly which view each object appears in
3. **2D placement data**: Dimensions have exact 2D coordinates for placement
4. **Single extraction pass**: One iteration through drawing objects
5. **Drawing-specific data**: Captures visibility, hidden lines, etc.

## Disadvantages of Version 1

1. **Duplicate processing**: Same model object processed multiple times (per view)
2. **Coordinate systems**: Mix of 2D drawing coords and 3D model coords
3. **View-dependent**: ML model needs to understand view projections
4. **Missing relationships**: Harder to see part-to-part spatial relationships

## Use Cases

- Training ML to place dimensions on drawings
- Learning view-specific annotation placement
- Understanding human dimensioning patterns per view type
