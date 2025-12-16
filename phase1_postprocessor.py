#!/usr/bin/env python3
"""
PHASE 1.2-1.4: ML Data Post-Processor
- Assigns parent views based on spatial containment
- Computes 2D→3D dimension transformations
- Builds geometric relationships
- Restructures to view-centric format
"""
import json
import numpy as np
from typing import Dict, List, Tuple, Optional
import re


def parse_point(point_str: str) -> Optional[Tuple[float, float, float]]:
    """Parse point string like '(x, y, z)' into tuple."""
    if not point_str:
        return None
    match = re.search(r'\(([-\d.]+),\s*([-\d.]+),\s*([-\d.]+)\)', point_str)
    if match:
        return (float(match.group(1)), float(match.group(2)), float(match.group(3)))
    return None


def get_object_2d_center(obj: Dict) -> Optional[Tuple[float, float]]:
    """Extract 2D center point from object properties."""
    # Handle Part objects - they have geometry in geometric_data.part_geometry
    if obj.get('type_name') == 'Part':
        geom_data = obj.get('geometric_data', {})
        part_geom = geom_data.get('part_geometry', {})

        start_pt = part_geom.get('start_point')
        end_pt = part_geom.get('end_point')

        if start_pt and end_pt and isinstance(start_pt, dict) and isinstance(end_pt, dict):
            # Use midpoint of start and end for part center in 2D
            return ((start_pt['x'] + end_pt['x']) / 2, (start_pt['y'] + end_pt['y']) / 2)
        elif start_pt and isinstance(start_pt, dict):
            return (start_pt['x'], start_pt['y'])

    # Handle other objects - they have points in properties as strings
    props = obj.get('properties', {})

    # Try different property names
    for prop_name in ['InsertionPoint', 'StartPoint', 'CenterPoint', 'Origin']:
        if prop_name in props:
            point_3d = parse_point(props[prop_name])
            if point_3d:
                return (point_3d[0], point_3d[1])

    # For dimensions, use midpoint of start/end
    if 'StartPoint' in props and 'EndPoint' in props:
        start = parse_point(props['StartPoint'])
        end = parse_point(props['EndPoint'])
        if start and end:
            return ((start[0] + end[0]) / 2, (start[1] + end[1]) / 2)

    return None


def point_in_view_bounds(point_2d: Tuple[float, float], view: Dict) -> bool:
    """Check if 2D point is within view bounds."""
    frame_origin = view.get('frame_origin', {})
    width = view.get('width', 0)
    height = view.get('height', 0)

    # Skip if bounds data is missing or zero
    if not frame_origin or width == 0 or height == 0:
        # Fallback: always return True for first view as default
        return view.get('view_index', -1) == 0

    x_min = frame_origin.get('x', 0)
    y_min = frame_origin.get('y', 0)
    x_max = x_min + width
    y_max = y_min + height

    return (x_min <= point_2d[0] <= x_max) and (y_min <= point_2d[1] <= y_max)


def assign_parent_views(drawing_data: Dict) -> Dict:
    """PHASE 1.2: Assign parent views based on spatial containment."""
    views = drawing_data.get('views', [])
    objects = drawing_data.get('objects', [])

    assigned_count = 0
    unassigned_count = 0

    for obj in objects:
        # Skip if already has parent_view_id
        if 'parent_view_id' in obj:
            assigned_count += 1
            continue

        # Get object's 2D center point
        center_2d = get_object_2d_center(obj)
        if not center_2d:
            unassigned_count += 1
            continue

        # Find which view contains this point
        for view in views:
            if point_in_view_bounds(center_2d, view):
                obj['parent_view_id'] = view['view_id']
                assigned_count += 1
                break
        else:
            unassigned_count += 1

    return {
        'assigned': assigned_count,
        'unassigned': unassigned_count,
        'total': len(objects),
        'coverage': assigned_count / len(objects) * 100 if objects else 0
    }


def transform_2d_to_3d(point_2d: Tuple[float, float], view: Dict, depth_strategy: str = 'centerline') -> Tuple[float, float, float]:
    """PHASE 1.3: Transform 2D point to 3D using view transformation matrix."""
    cs3d = view.get('coordinate_system_3d', {})
    origin = cs3d.get('origin', {})
    axis_x = cs3d.get('axis_x', {})
    axis_y = cs3d.get('axis_y', {})
    normal = cs3d.get('normal', {})

    if not all([origin, axis_x, axis_y, normal]):
        # Fallback: assume identity transformation
        return (point_2d[0], point_2d[1], 0.0)

    # Compute 3D point: P_3d = origin + (x_2d * axis_x) + (y_2d * axis_y)
    point_3d = np.array([
        origin['x'] + point_2d[0] * axis_x['x'] + point_2d[1] * axis_y['x'],
        origin['y'] + point_2d[0] * axis_x['y'] + point_2d[1] * axis_y['y'],
        origin['z'] + point_2d[0] * axis_x['z'] + point_2d[1] * axis_y['z']
    ])

    # Apply depth strategy (for now, use point as-is on view plane)
    # TODO: Adjust depth based on measured geometry

    return tuple(point_3d)


def compute_dimension_3d(dim_obj: Dict, view: Dict) -> Dict:
    """PHASE 1.3: Compute 3D interpretation of 2D dimension."""
    props = dim_obj.get('properties', {})

    start_2d_str = props.get('StartPoint')
    end_2d_str = props.get('EndPoint')

    if not (start_2d_str and end_2d_str):
        return {}

    start_3d_full = parse_point(start_2d_str)
    end_3d_full = parse_point(end_2d_str)

    if not (start_3d_full and end_3d_full):
        return {}

    # Use 2D projection (x, y) for transformation
    start_2d = (start_3d_full[0], start_3d_full[1])
    end_2d = (end_3d_full[0], end_3d_full[1])

    # Transform to 3D using view matrix
    start_3d = transform_2d_to_3d(start_2d, view)
    end_3d = transform_2d_to_3d(end_2d, view)

    # Compute measured value
    measured_value = np.linalg.norm(np.array(end_3d) - np.array(start_3d))

    return {
        'start_point_3d': {'x': start_3d[0], 'y': start_3d[1], 'z': start_3d[2]},
        'end_point_3d': {'x': end_3d[0], 'y': end_3d[1], 'z': end_3d[2]},
        'measured_value': round(measured_value, 3),
        'depth_placement': 'centerline',  # Default strategy
        'projection_plane': get_plane_from_normal(view.get('coordinate_system_3d', {}).get('normal', {}))
    }


def get_plane_from_normal(normal: Dict) -> str:
    """Determine projection plane from normal vector."""
    if not normal:
        return 'unknown'

    nx = abs(normal.get('x', 0))
    ny = abs(normal.get('y', 0))
    nz = abs(normal.get('z', 0))

    # Normal points perpendicular to plane
    if nx > ny and nx > nz:
        return 'YZ'  # Normal along X
    elif ny > nx and ny > nz:
        return 'XZ'  # Normal along Y
    else:
        return 'XY'  # Normal along Z


def build_relationships(drawing_data: Dict) -> Dict:
    """PHASE 1.4: Build geometric relationships."""
    objects = drawing_data.get('objects', [])

    relationships = {
        'dimension_to_parts': {},
        'part_to_dimensions': {},
        'view_to_parts': {},
        'view_to_dimensions': {}
    }

    # Build dimension→part relationships from related_objects
    for obj in objects:
        obj_id = obj.get('object_id', '')
        obj_type = obj.get('type_name', '')
        parent_view = obj.get('parent_view_id', '')

        if 'Dimension' in obj_type and obj_id:
            related = obj.get('related_objects', [])
            parts = [r for r in related if 'Part' in r]
            if parts:
                relationships['dimension_to_parts'][obj_id] = parts

        if obj_type == 'Part' and obj_id and parent_view:
            if parent_view not in relationships['view_to_parts']:
                relationships['view_to_parts'][parent_view] = []
            relationships['view_to_parts'][parent_view].append(obj_id)

        if 'Dimension' in obj_type and obj_id and parent_view:
            if parent_view not in relationships['view_to_dimensions']:
                relationships['view_to_dimensions'][parent_view] = []
            relationships['view_to_dimensions'][parent_view].append(obj_id)

    # Invert: part→dimensions
    for dim_id, parts in relationships['dimension_to_parts'].items():
        for part in parts:
            if part not in relationships['part_to_dimensions']:
                relationships['part_to_dimensions'][part] = []
            relationships['part_to_dimensions'][part].append(dim_id)

    return relationships


def restructure_to_view_centric(drawing_data: Dict) -> Dict:
    """PHASE 1.2: Restructure to view-centric format for ML training."""
    views = drawing_data.get('views', [])
    objects = drawing_data.get('objects', [])

    # Group objects by parent view
    view_centric = {
        'drawing': {
            'mark': drawing_data.get('mark'),
            'type': drawing_data.get('drawing_type'),
            'view_count': drawing_data.get('view_count')
        },
        'views': []
    }

    for view in views:
        view_id = view['view_id']

        # Get all objects in this view
        view_objects = [obj for obj in objects if obj.get('parent_view_id') == view_id]

        # Separate by type
        parts = [obj for obj in view_objects if obj.get('type_name') == 'Part']
        dimensions = [obj for obj in view_objects if 'Dimension' in obj.get('type_name', '')]
        others = [obj for obj in view_objects if obj not in parts and obj not in dimensions]

        view_data = {
            'view_id': view_id,
            'name': view.get('name'),
            'type': view.get('view_type'),
            'coordinate_system_3d': view.get('coordinate_system_3d'),
            'coordinate_system_2d': view.get('coordinate_system_2d'),
            'bounds': {
                'width': view.get('width'),
                'height': view.get('height'),
                'frame_origin': view.get('frame_origin')
            },
            'objects': {
                'parts': parts,
                'dimensions': dimensions,
                'other': others
            },
            'statistics': {
                'total_objects': len(view_objects),
                'parts_count': len(parts),
                'dimensions_count': len(dimensions),
                'other_count': len(others)
            }
        }

        view_centric['views'].append(view_data)

    # Add unassigned objects
    unassigned = [obj for obj in objects if 'parent_view_id' not in obj]
    if unassigned:
        view_centric['unassigned_objects'] = {
            'count': len(unassigned),
            'objects': unassigned
        }

    return view_centric


def process_extraction(input_file: str, output_file: str):
    """Main processing pipeline."""
    print(f"Loading extraction from: {input_file}")
    with open(input_file, 'r', encoding='utf-8-sig') as f:
        drawing_data = json.load(f)

    print("\n" + "=" * 80)
    print("PHASE 1 POST-PROCESSING PIPELINE")
    print("=" * 80)

    # PHASE 1.2: Assign parent views
    print("\nPHASE 1.2: Assigning parent views...")
    assignment_stats = assign_parent_views(drawing_data)
    print(f"  Assigned: {assignment_stats['assigned']}/{assignment_stats['total']} ({assignment_stats['coverage']:.1f}%)")
    print(f"  Unassigned: {assignment_stats['unassigned']}")

    # PHASE 1.3: Compute 3D interpretations for dimensions
    print("\nPHASE 1.3: Computing 2D to 3D dimension transformations...")
    views_by_id = {v['view_id']: v for v in drawing_data.get('views', [])}
    dimensions_3d = 0

    for obj in drawing_data.get('objects', []):
        if 'Dimension' in obj.get('type_name', ''):
            parent_view_id = obj.get('parent_view_id')
            if parent_view_id and parent_view_id in views_by_id:
                view = views_by_id[parent_view_id]
                measurement_3d = compute_dimension_3d(obj, view)
                if measurement_3d:
                    obj['measurement_3d'] = measurement_3d
                    dimensions_3d += 1

    print(f"  Dimensions with 3D interpretation: {dimensions_3d}")

    # PHASE 1.4: Build relationships
    print("\nPHASE 1.4: Building geometric relationships...")
    relationships = build_relationships(drawing_data)
    print(f"  Dimension-to-Part links: {len(relationships['dimension_to_parts'])}")
    print(f"  Part-to-Dimension links: {len(relationships['part_to_dimensions'])}")
    print(f"  View-to-Parts mapping: {len(relationships['view_to_parts'])} views")
    print(f"  View-to-Dimensions mapping: {len(relationships['view_to_dimensions'])} views")

    # Add relationships to output
    drawing_data['relationships'] = relationships

    # PHASE 1.2: Restructure to view-centric
    print("\nPHASE 1.2: Restructuring to view-centric format...")
    view_centric = restructure_to_view_centric(drawing_data)
    print(f"  Views with objects: {len(view_centric['views'])}")
    print(f"  Unassigned objects: {view_centric.get('unassigned_objects', {}).get('count', 0)}")

    # Save both formats
    flat_output = output_file.replace('.json', '_flat.json')
    view_output = output_file.replace('.json', '_view_centric.json')

    print(f"\nSaving outputs...")
    with open(flat_output, 'w', encoding='utf-8') as f:
        json.dump(drawing_data, f, indent=2)
    print(f"  Flat format: {flat_output}")

    with open(view_output, 'w', encoding='utf-8') as f:
        json.dump(view_centric, f, indent=2)
    print(f"  View-centric format: {view_output}")

    print("\n" + "=" * 80)
    print("PHASE 1 POST-PROCESSING COMPLETE!")
    print("=" * 80)

    return drawing_data, view_centric


if __name__ == '__main__':
    input_file = 'Server/TeklaMCP/extractions/tekla_extraction_ml_20251215_145213.jsonl'
    output_file = 'ml_training_data.json'

    process_extraction(input_file, output_file)
