#!/usr/bin/env python3
"""
OPTIMIZED DATA EXTRACTION RESTRUCTURING
Deduplicate and restructure extraction data for ML training

Key Concepts:
1. DrawingObject (2D): Dimensions, marks, leader lines (view-specific representations)
2. ModelObject (3D): Parts, bolts, connections (single 3D model objects)
3. Each ModelObject appears in multiple views as different DrawingObjects
4. Need to deduplicate using ModelIdentifier to get unique 3D objects
"""
import json
import hashlib
from collections import defaultdict
from typing import Dict, List, Any

def load_extraction(jsonl_file: str) -> dict:
    """Load extraction JSONL with UTF-8 BOM handling"""
    with open(jsonl_file, 'r', encoding='utf-8-sig') as f:
        return json.loads(f.readline())

def extract_unique_model_objects(objects: List[dict]) -> Dict[str, dict]:
    """
    Extract unique 3D model objects by deduplicating using ModelIdentifier

    Each ModelObject (part, bolt, connection) appears in multiple views but
    represents a single 3D object in the model.
    """
    unique_objects = {}

    for obj in objects:
        model_id = obj.get('model_object_id')
        if not model_id:
            continue  # Skip drawing-only objects (dimensions, marks, etc.)

        obj_type = obj.get('type_name')

        if model_id not in unique_objects:
            # First occurrence of this model object
            unique_objects[model_id] = {
                'model_id': model_id,
                'type': obj_type,
                'geometric_data': obj.get('geometric_data', {}),
                'properties': obj.get('properties', {}),
                'drawing_representations': []  # Track which views show this object
            }

        # Add this drawing representation
        unique_objects[model_id]['drawing_representations'].append({
            'drawing_object_id': obj.get('object_id'),
            'parent_view_id': obj.get('parent_view_id'),
            'view_specific_data': {
                # Store any view-specific rendering data here
            }
        })

    return unique_objects

def extract_drawing_objects_2d(objects: List[dict]) -> Dict[str, List[dict]]:
    """
    Extract pure 2D drawing objects (dimensions, marks, etc.)
    Group by parent view for context
    """
    drawing_objects_by_view = defaultdict(list)

    for obj in objects:
        # Skip model objects (already handled)
        if obj.get('has_model_link', False):
            continue

        obj_type = obj.get('type_name')

        # These are pure drawing objects (2D annotations)
        if obj_type in ['StraightDimension', 'StraightDimensionSet', 'AngleDimension',
                        'Mark', 'MarkSet', 'LeaderLine', 'Text', 'SectionMark',
                        'DetailMark', 'WeldMark']:

            view_id = obj.get('parent_view_id', 'unknown_view')

            drawing_obj = {
                'object_id': obj.get('object_id'),
                'type': obj_type,
                'geometric_data_2d': obj.get('geometric_data', {}),
                'related_model_objects': [],  # Will populate from relationships
                'properties': obj.get('properties', {})
            }

            # Link to related model objects
            related = obj.get('related_objects', [])
            if related:
                drawing_obj['related_model_objects'] = related

            drawing_objects_by_view[view_id].append(drawing_obj)

    return dict(drawing_objects_by_view)

def compute_3d_from_2d(dimension_2d: dict, view_transform: dict, model_objects: dict) -> dict:
    """
    Transform 2D dimension to 3D using view transformation matrix

    Key insight: Dimensions are drawn in 2D view space but measure 3D geometry
    We need both representations for ML:
    - 2D: What the human sees and places
    - 3D: What is actually being measured
    """
    dim_points_2d = dimension_2d.get('geometric_data_2d', {}).get('dimension_points', [])

    if not dim_points_2d or not view_transform:
        return {}

    # Extract transformation matrix from view
    cs_3d = view_transform.get('coordinate_system_3d', {})
    origin = cs_3d.get('origin', {})
    axis_x = cs_3d.get('axis_x', {})
    axis_y = cs_3d.get('axis_y', {})
    normal = cs_3d.get('normal', {})

    if not all([origin, axis_x, axis_y]):
        return {}

    # Transform each 2D point to 3D
    # 2D point (x_2d, y_2d) -> 3D point = origin + x_2d*axis_x + y_2d*axis_y
    dim_points_3d = []
    for pt_2d in dim_points_2d:
        if not pt_2d:
            continue

        x_2d = pt_2d.get('x', 0)
        y_2d = pt_2d.get('y', 0)

        # 3D transformation
        x_3d = origin['x'] + x_2d * axis_x['x'] + y_2d * axis_y['x']
        y_3d = origin['y'] + x_2d * axis_x['y'] + y_2d * axis_y['y']
        z_3d = origin['z'] + x_2d * axis_x['z'] + y_2d * axis_y['z']

        dim_points_3d.append({'x': x_3d, 'y': y_3d, 'z': z_3d})

    return {
        'dimension_points_3d': dim_points_3d,
        'view_normal': normal,  # Which plane the dimension is in
        'transformation_used': view_transform.get('view_id')
    }

def create_geometry_hash(geom_data: dict) -> str:
    """
    Create hash of 3D geometry for duplicate detection
    Helps identify if two drawing objects represent same geometric element
    """
    if not geom_data:
        return None

    # Use bounding box or vertices for hash
    bbox = geom_data.get('bounding_box', {})
    if bbox:
        hash_input = f"{bbox.get('min', {})}{bbox.get('max', {})}"
        return hashlib.md5(hash_input.encode()).hexdigest()[:16]

    return None

def restructure_extraction_data(extraction_data: dict) -> dict:
    """
    Main restructuring function

    Output structure:
    {
        "drawing_mark": "ST1006",
        "model_objects": {
            "42_unique_parts": {...},  # Deduplicated by ModelIdentifier
            "geometric_hashes": {...}   # For additional deduplication
        },
        "views": [
            {
                "view_id": "view_0",
                "transformation_matrix": {...},  # For 2D↔3D conversion
                "drawing_objects": [
                    {
                        "dimensions": [...],  # With both 2D and 3D coords
                        "marks": [...],
                        "leader_lines": [...]
                    }
                ]
            }
        ],
        "metadata": {
            "unique_part_count": 42,
            "total_drawing_objects": 1086,
            "views_count": 12
        }
    }
    """

    print("Restructuring extraction data...")

    objects = extraction_data.get('objects', [])
    views_array = extraction_data.get('views', [])

    # If views array is empty, extract View objects from objects list
    if not views_array:
        print("WARNING: views array is empty, extracting from objects list")
        views_array = [obj for obj in objects if obj.get('type_name') == 'View']

    print(f"Found {len(views_array)} views")

    # Create view transformation lookup
    view_transforms = {}
    for view in views_array:
        view_id = view.get('view_id', view.get('object_id'))
        view_transforms[view_id] = view

    # Step 1: Deduplicate model objects (3D)
    unique_model_objects = extract_unique_model_objects(objects)
    print(f"Unique model objects: {len(unique_model_objects)}")

    # Count by type
    parts_count = sum(1 for obj in unique_model_objects.values() if obj['type'] == 'Part')
    print(f"  - Unique Parts: {parts_count}")

    # Step 2: Extract drawing objects (2D) grouped by view
    drawing_objects_by_view = extract_drawing_objects_2d(objects)
    print(f"Drawing objects grouped into {len(drawing_objects_by_view)} views")

    # Step 3: Transform dimensions to 3D
    for view_id, view_objects in drawing_objects_by_view.items():
        view_transform = view_transforms.get(view_id, {})

        for obj in view_objects:
            if 'Dimension' in obj['type']:
                # Compute 3D representation
                obj['geometric_data_3d'] = compute_3d_from_2d(obj, view_transform, unique_model_objects)

    # Step 4: Create geometry hashes for deduplication
    for model_id, obj_data in unique_model_objects.items():
        geom_hash = create_geometry_hash(obj_data.get('geometric_data', {}))
        if geom_hash:
            obj_data['geometry_hash'] = geom_hash

    # Build final structure
    restructured = {
        'drawing_mark': extraction_data.get('mark', 'Unknown'),
        'model_objects': unique_model_objects,
        'views': [
            {
                'view_id': view_id,
                'view_data': view_transforms.get(view_id, {}),
                'drawing_objects': view_objects
            }
            for view_id, view_objects in drawing_objects_by_view.items()
        ],
        'metadata': {
            'unique_part_count': parts_count,
            'unique_model_object_count': len(unique_model_objects),
            'total_drawing_object_count': len(objects),
            'views_count': len(view_transforms),
            'dimension_count': sum(
                len([o for o in view_objs if 'Dimension' in o['type']])
                for view_objs in drawing_objects_by_view.values()
            )
        }
    }

    return restructured

def validate_restructured_data(data: dict, expected_parts: int = 42):
    """
    Validate that restructuring worked correctly
    """
    print("\n" + "="*80)
    print("VALIDATION REPORT")
    print("="*80)

    metadata = data['metadata']
    print(f"\nDrawing: {data['drawing_mark']}")
    print(f"Unique Parts: {metadata['unique_part_count']} (expected: {expected_parts})")

    if metadata['unique_part_count'] == expected_parts:
        print("[PASS] Part count matches expected")
    else:
        print(f"[FAIL] Expected {expected_parts}, got {metadata['unique_part_count']}")

    print(f"\nTotal Model Objects: {metadata['unique_model_object_count']}")
    print(f"Total Drawing Objects: {metadata['total_drawing_object_count']}")
    print(f"Views: {metadata['views_count']}")
    print(f"Dimensions: {metadata['dimension_count']}")

    # Check for 3D transformations
    dims_with_3d = 0
    for view in data['views']:
        for obj in view['drawing_objects']:
            if 'Dimension' in obj['type'] and obj.get('geometric_data_3d'):
                dims_with_3d += 1

    print(f"\nDimensions with 3D transformation: {dims_with_3d}/{metadata['dimension_count']}")

    if dims_with_3d > 0:
        print("[PASS] 2D->3D transformation working")
    else:
        print("[WARNING] No 3D transformations applied")

    print("\n" + "="*80)

def main():
    """Main execution"""

    # Load original extraction
    print("Loading extraction data...")
    extraction_file = "Server/TeklaMCP/extractions/tekla_extraction_20251213_180910.jsonl"
    extraction_data = load_extraction(extraction_file)

    print(f"Loaded drawing: {extraction_data.get('mark', 'Unknown')}")
    print(f"Total objects: {len(extraction_data.get('objects', []))}")

    # Restructure
    restructured_data = restructure_extraction_data(extraction_data)

    # Validate
    validate_restructured_data(restructured_data, expected_parts=42)

    # Save restructured data
    output_file = "training_data/ST1006_restructured.json"
    import os
    os.makedirs("training_data", exist_ok=True)

    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(restructured_data, f, indent=2)

    print(f"\n[SUCCESS] Restructured data saved to: {output_file}")

    # Also save summary for easy inspection
    summary = {
        'drawing': restructured_data['drawing_mark'],
        'metadata': restructured_data['metadata'],
        'unique_parts': [
            {
                'model_id': model_id,
                'type': obj_data['type'],
                'views_appearing_in': len(obj_data['drawing_representations'])
            }
            for model_id, obj_data in restructured_data['model_objects'].items()
            if obj_data['type'] == 'Part'
        ][:5]  # Show first 5 parts as example
    }

    with open("training_data/ST1006_summary.json", 'w', encoding='utf-8') as f:
        json.dump(summary, f, indent=2)

    print(f"[SUCCESS] Summary saved to: training_data/ST1006_summary.json")

if __name__ == "__main__":
    main()
