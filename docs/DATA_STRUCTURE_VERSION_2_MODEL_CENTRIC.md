# Data Structure Version 2: Model-Centric with 3D Projection

## Overview

Version 2 treats the 3D Model as the primary source of truth. Drawing annotations
(dimensions, marks) are projected back into 3D space and attached to model objects.
This creates a unified 3D representation where dimensions exist as 3D objects.

```
Drawing (2D)                    Model (3D) ← PRIMARY SOURCE
┌─────────────────────────┐     ┌─────────────────────────┐
│  StraightDimension ─────┼────►│  DimensionPlane3D       │
│  Mark ──────────────────┼────►│  Annotation3D           │
│  LeaderLine ────────────┼────►│  Leader3D               │
│                         │     │                         │
│  Drawing.Part ◄─────────┼─────│  Model.Part (origin)    │
│  Drawing.Bolt ◄─────────┼─────│  Model.BoltGroup        │
└─────────────────────────┘     └─────────────────────────┘
```

## Data Flow

```
┌────────────────────────────────────────────────────────────────────────┐
│                         EXTRACTION PROCESS                              │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  STEP 1: Get Model Objects from Drawing                                 │
│          Drawing.GetSheet() → Get all DrawingObjects with ModelLinks    │
│          Collect: Part IDs, Bolt IDs, Connection IDs, Weld IDs          │
│                        ↓                                                │
│  STEP 2: Get Drawing Objects (Annotations)                              │
│          Dimensions, Marks, LeaderLines                                 │
│          Collect: 2D coordinates, measured parts, properties            │
│                        ↓                                                │
│  STEP 3: Get View Transformation Matrices                               │
│          For each View: DisplayCoordinateSystem                         │
│          Origin, AxisX, AxisY, Normal                                   │
│                        ↓                                                │
│  STEP 4: Get Full Model Objects from Model Database                     │
│          Using collected IDs → Model.SelectModelObject()                │
│          Get complete 3D geometry, relationships                        │
│                        ↓                                                │
│  STEP 5: Project 2D Annotations to 3D                                   │
│          2D Point → 3D Point using view transformation                  │
│          P_3d = Origin + x_2d * AxisX + y_2d * AxisY                    │
│                        ↓                                                │
│  STEP 6: Create Unified 3D Training Data                                │
│          One coordinate system, all relationships visible               │
│                                                                         │
└────────────────────────────────────────────────────────────────────────┘
```

## Complete Data Structure

```json
{
  "assembly": {
    "id": "ST1006",
    "type": "AssemblyDrawing",
    "name": "STAIR ASSEMBLY",
    "extraction_timestamp": "2025-12-17T10:00:00Z",

    "assembly_bounds": {
      "min": {"x": 7000, "y": 3300, "z": 3000},
      "max": {"x": 8000, "y": 5000, "z": 4000}
    },

    "coordinate_system": {
      "type": "model_global",
      "units": "mm"
    }
  },

  "model_objects_3d": {
    "parts": {
      "773641573": {
        "type": "Beam",
        "name": "STRINGER",
        "piece_mark": "c",

        "profile": {
          "string": "MC12X31",
          "type": "channel",
          "depth": 304.8,
          "width": 88.9,
          "web_thickness": 7.5,
          "flange_thickness": 12.7
        },

        "material": {
          "string": "A36",
          "yield_strength": 250,
          "density": 7850
        },

        "geometry_3d": {
          "axis": {
            "start": {"x": 7077.08, "y": 3409.95, "z": 3004.46},
            "end": {"x": 7077.08, "y": 3409.95, "z": 3461.99},
            "length": 457.53,
            "direction": {"x": 0, "y": 0, "z": 1}
          },
          "bounding_box": {
            "min": {"x": 7077.07, "y": 3317.88, "z": 3004.46},
            "max": {"x": 7381.88, "y": 3409.95, "z": 3461.99}
          },
          "centroid": {"x": 7229.47, "y": 3363.91, "z": 3233.22}
        },

        "position": {
          "depth": "BEHIND",
          "plane": "MIDDLE",
          "rotation": "TOP",
          "offset": {"dx": 0, "dy": 0, "dz": 0}
        },

        "relationships": {
          "parent_assembly": "ST1006",
          "connected_to": [
            {"part_id": "773481986", "connection_type": "welded"},
            {"part_id": "773555111", "connection_type": "bolted"}
          ],
          "intersecting_parts": ["773481986"]
        },

        "dimensions_attached": ["dim3d_001", "dim3d_002", "dim3d_003"]
      },

      "773481986": {
        "type": "PolyBeam",
        "name": "BENT PLATE",

        "profile": {
          "string": "PL3.175*1270",
          "type": "plate",
          "thickness": 3.175,
          "width": 1270
        },

        "geometry_3d": {
          "contour_points": [
            {"x": 7500.93, "y": 3409.95, "z": 3619.50, "chamfer": null},
            {"x": 7600.00, "y": 3500.00, "z": 3700.00, "chamfer": {"type": "Line", "x": 10, "y": 10}},
            {"x": 7700.00, "y": 4000.00, "z": 3750.00, "chamfer": null},
            {"x": 7836.10, "y": 4679.95, "z": 3831.77, "chamfer": null}
          ],
          "bounding_box": {
            "min": {"x": 7500.93, "y": 3409.95, "z": 3619.50},
            "max": {"x": 7836.10, "y": 4679.95, "z": 3831.77}
          },
          "path_length": 1450.32
        },

        "relationships": {
          "connected_to": [{"part_id": "773641573", "connection_type": "welded"}]
        },

        "dimensions_attached": ["dim3d_010", "dim3d_011"]
      },

      "plate_003": {
        "type": "ContourPlate",
        "name": "BASE PLATE",

        "geometry_3d": {
          "contour_points": [
            {"x": 7000, "y": 3300, "z": 3000},
            {"x": 7200, "y": 3300, "z": 3000},
            {"x": 7200, "y": 3500, "z": 3000},
            {"x": 7000, "y": 3500, "z": 3000}
          ],
          "normal": {"x": 0, "y": 0, "z": 1},
          "thickness": 12.7,
          "area": 40000
        }
      }
    },

    "bolts": {
      "789123456": {
        "type": "BoltArray",
        "standard": "AISC",
        "size": 19.05,
        "grade": "A325",

        "geometry_3d": {
          "first_position": {"x": 7100, "y": 3350, "z": 3200},
          "second_position": {"x": 7100, "y": 3350, "z": 3400},

          "pattern": {
            "type": "linear",
            "count_x": 1,
            "count_y": 4,
            "spacing_x": 0,
            "spacing_y": 75
          },

          "bolt_positions_3d": [
            {"x": 7100, "y": 3350, "z": 3200, "bolt_index": 0},
            {"x": 7100, "y": 3350, "z": 3275, "bolt_index": 1},
            {"x": 7100, "y": 3350, "z": 3350, "bolt_index": 2},
            {"x": 7100, "y": 3350, "z": 3400, "bolt_index": 3}
          ],

          "holes": [
            {
              "position": {"x": 7100, "y": 3350, "z": 3200},
              "diameter": 21.0,
              "in_part": "773641573"
            },
            {
              "position": {"x": 7100, "y": 3350, "z": 3200},
              "diameter": 21.0,
              "in_part": "773481986"
            }
          ]
        },

        "connections": {
          "part_to_bolt_through": "773641573",
          "part_to_be_bolted": "773481986"
        },

        "dimensions_attached": ["dim3d_020", "dim3d_021"]
      }
    },

    "connections": {
      "conn_001": {
        "type": "Connection",
        "name": "Clip angle (144)",
        "number": 144,

        "geometry_3d": {
          "origin": {"x": 7100, "y": 3400, "z": 3300},
          "bounding_box": {
            "min": {"x": 7050, "y": 3300, "z": 3150},
            "max": {"x": 7150, "y": 3500, "z": 3450}
          }
        },

        "connected_objects": {
          "primary": "773641573",
          "secondary": ["773481986"]
        },

        "created_objects": {
          "bolts": ["789123456"],
          "welds": ["weld_001"],
          "additional_parts": ["angle_002"]
        }
      }
    },

    "welds": {
      "weld_001": {
        "type": "FilletWeld",
        "size": 6.0,
        "length": 150.0,

        "geometry_3d": {
          "weld_line": {
            "start": {"x": 7100, "y": 3350, "z": 3200},
            "end": {"x": 7100, "y": 3350, "z": 3350}
          },
          "weld_plane_normal": {"x": 1, "y": 0, "z": 0}
        },

        "connected_parts": {
          "main": "773641573",
          "secondary": "773481986"
        },

        "dimensions_attached": ["dim3d_030"]
      }
    }
  },

  "dimensions_3d": {
    "dim3d_001": {
      "type": "LinearDimension",
      "measures": "overall_length",

      "geometry_3d": {
        "measured_points": {
          "start": {"x": 7077.08, "y": 3409.95, "z": 3004.46},
          "end": {"x": 7077.08, "y": 3409.95, "z": 3461.99}
        },
        "dimension_line": {
          "start": {"x": 7200.0, "y": 3409.95, "z": 3004.46},
          "end": {"x": 7200.0, "y": 3409.95, "z": 3461.99}
        },
        "text_position": {"x": 7200.0, "y": 3409.95, "z": 3233.22},
        "offset_from_part": 122.92,
        "offset_direction": {"x": 1, "y": 0, "z": 0}
      },

      "value": 457.53,
      "unit": "mm",

      "attached_to": {
        "part_id": "773641573",
        "measures_property": "axis_length"
      },

      "source_views": ["view_0", "view_2"],

      "projection_info": {
        "original_view": "view_0",
        "original_2d": {
          "start": {"x": 100, "y": 50},
          "end": {"x": 250, "y": 50}
        },
        "transformation_matrix": {
          "origin": {"x": 7200, "y": 3500, "z": 3300},
          "axis_x": {"x": 1, "y": 0, "z": 0},
          "axis_y": {"x": 0, "y": 0, "z": 1}
        }
      }
    },

    "dim3d_002": {
      "type": "LinearDimension",
      "measures": "hole_spacing",

      "geometry_3d": {
        "measured_points": {
          "start": {"x": 7100, "y": 3350, "z": 3200},
          "end": {"x": 7100, "y": 3350, "z": 3275}
        },
        "dimension_line": {
          "start": {"x": 7150.0, "y": 3350, "z": 3200},
          "end": {"x": 7150.0, "y": 3350, "z": 3275}
        },
        "offset_from_part": 50.0,
        "offset_direction": {"x": 1, "y": 0, "z": 0}
      },

      "value": 75.0,

      "attached_to": {
        "bolt_id": "789123456",
        "measures_property": "bolt_spacing"
      }
    },

    "dim3d_010": {
      "type": "LinearDimension",
      "measures": "segment_length",

      "geometry_3d": {
        "measured_points": {
          "start": {"x": 7500.93, "y": 3409.95, "z": 3619.50},
          "end": {"x": 7600.00, "y": 3500.00, "z": 3700.00}
        }
      },

      "value": 164.35,

      "attached_to": {
        "part_id": "773481986",
        "segment_index": 0
      }
    },

    "dim3d_030": {
      "type": "LinearDimension",
      "measures": "weld_length",

      "geometry_3d": {
        "measured_points": {
          "start": {"x": 7100, "y": 3350, "z": 3200},
          "end": {"x": 7100, "y": 3350, "z": 3350}
        }
      },

      "value": 150.0,

      "attached_to": {
        "weld_id": "weld_001",
        "measures_property": "weld_length"
      }
    }
  },

  "annotations_3d": {
    "mark_001": {
      "type": "PartMark",
      "text": "MC12X31",

      "position_3d": {"x": 7229.47, "y": 3363.91, "z": 3233.22},
      "attached_to": "773641573",

      "leader_line_3d": {
        "from": {"x": 7229.47, "y": 3363.91, "z": 3233.22},
        "to": {"x": 7150.0, "y": 3400.0, "z": 3300.0}
      }
    }
  },

  "drawing_views_reference": {
    "view_0": {
      "type": "FrontView",
      "transformation": {
        "origin": {"x": 7200, "y": 3500, "z": 3300},
        "axis_x": {"x": 1, "y": 0, "z": 0},
        "axis_y": {"x": 0, "y": 0, "z": 1},
        "normal": {"x": 0, "y": -1, "z": 0}
      },
      "scale": 0.1,
      "objects_visible": ["773641573", "773481986", "789123456"]
    },
    "view_1": {
      "type": "TopView",
      "transformation": {
        "origin": {"x": 7200, "y": 3500, "z": 3300},
        "axis_x": {"x": 1, "y": 0, "z": 0},
        "axis_y": {"x": 0, "y": 1, "z": 0},
        "normal": {"x": 0, "y": 0, "z": -1}
      }
    }
  },

  "spatial_relationships": {
    "part_proximities": [
      {
        "part_a": "773641573",
        "part_b": "773481986",
        "min_distance": 0,
        "relationship": "intersecting",
        "connection_id": "conn_001"
      }
    ],

    "part_alignment_groups": [
      {
        "alignment_type": "colinear_axes",
        "parts": ["773641573", "stringer_002", "stringer_003"]
      }
    ]
  },

  "metadata": {
    "parts_count": 42,
    "bolts_count": 15,
    "connections_count": 8,
    "welds_count": 12,
    "dimensions_3d_count": 211,
    "annotations_3d_count": 53
  }
}
```

## 3D Projection Algorithm

### 2D to 3D Transformation

```python
def project_2d_to_3d(point_2d, view_transformation):
    """
    Transform 2D drawing point to 3D model coordinates.

    2D point (x_2d, y_2d) in view space
    3D point = Origin + x_2d * AxisX + y_2d * AxisY
    """
    origin = view_transformation['origin']
    axis_x = view_transformation['axis_x']
    axis_y = view_transformation['axis_y']

    x_3d = origin['x'] + point_2d['x'] * axis_x['x'] + point_2d['y'] * axis_y['x']
    y_3d = origin['y'] + point_2d['x'] * axis_x['y'] + point_2d['y'] * axis_y['y']
    z_3d = origin['z'] + point_2d['x'] * axis_x['z'] + point_2d['y'] * axis_y['z']

    return {'x': x_3d, 'y': y_3d, 'z': z_3d}

def project_dimension_to_3d(dimension_2d, view_transformation, measured_parts):
    """
    Project a 2D dimension to its 3D representation.
    """
    dim_3d = {
        'type': dimension_2d['type'],
        'value': dimension_2d['value']
    }

    # Project dimension line endpoints
    start_3d = project_2d_to_3d(dimension_2d['start_point'], view_transformation)
    end_3d = project_2d_to_3d(dimension_2d['end_point'], view_transformation)

    dim_3d['geometry_3d'] = {
        'dimension_line': {
            'start': start_3d,
            'end': end_3d
        }
    }

    # Calculate offset direction (perpendicular to measured direction in view plane)
    # offset_direction = view_transformation['normal'] x dimension_direction

    # Attach to nearest part
    dim_3d['attached_to'] = find_nearest_part(start_3d, end_3d, measured_parts)

    return dim_3d
```

### View-Aware Offset Calculation

```python
def calculate_dimension_offset(dim_3d, part_geometry, view_normal):
    """
    Calculate how far the dimension line is offset from the part.
    This is crucial for ML training - learning optimal offset distances.
    """
    # Get nearest point on part to dimension line
    part_surface = get_nearest_surface(part_geometry, dim_3d['dimension_line'])

    # Calculate perpendicular distance
    offset = distance_point_to_line(
        part_surface['point'],
        dim_3d['dimension_line']['start'],
        dim_3d['dimension_line']['end']
    )

    # Determine offset direction relative to view
    offset_direction = normalize(
        cross_product(view_normal, dim_3d['measured_direction'])
    )

    return {
        'offset_distance': offset,
        'offset_direction': offset_direction,
        'relative_to_view': view_normal
    }
```

## Advantages of Version 2

1. **Single coordinate system**: Everything in model global coordinates
2. **Clear spatial relationships**: Easy to see part-to-part and part-to-dimension relationships
3. **View-independent learning**: ML model learns 3D patterns, not view-specific 2D patterns
4. **Transferable knowledge**: Patterns learned apply to any view orientation
5. **Assembly context**: Full assembly visible with all relationships
6. **Dimension offset analysis**: Can analyze optimal offset distances in 3D

## Disadvantages of Version 2

1. **Projection complexity**: 2D→3D projection can introduce errors
2. **View context lost**: Why a dimension was placed in a specific view may be unclear
3. **Multiple extractions**: Need both Drawing API and Model API queries
4. **Projection ambiguity**: Multiple 3D points can project to same 2D point
5. **Dimension plane ambiguity**: Dimensions exist on infinite planes perpendicular to view

## Use Cases

- Training ML to understand 3D spatial relationships for dimensioning
- Learning assembly-wide dimension patterns
- Understanding dimension placement relative to part geometry
- Transfer learning across different view orientations
