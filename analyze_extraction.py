#!/usr/bin/env python3
"""
Analyze Tekla Drawing Extraction Data Structure
"""
import json
import sys

# Read JSONL file (one drawing per line)
extraction_file = "Server/TeklaMCP/extractions/tekla_extraction_20251213_180910.jsonl"

with open(extraction_file, 'r', encoding='utf-8-sig') as f:  # utf-8-sig handles BOM
    line = f.readline()
    drawing_data = json.loads(line)

print("=" * 80)
print("EXTRACTION DATA STRUCTURE ANALYSIS")
print("=" * 80)

print(f"\n1. DRAWING-LEVEL DATA:")
print(f"   - Mark: {drawing_data.get('mark')}")
print(f"   - Drawing Type: {drawing_data.get('drawing_type')}")
print(f"   - Sheet Coordinate System: {drawing_data.get('sheet_coordinate_system')}")
print(f"   - Total Objects: {len(drawing_data.get('objects', []))}")

# Analyze object types
object_types = {}
for obj in drawing_data.get('objects', []):
    obj_type = obj.get('type_name', 'Unknown')
    if obj_type not in object_types:
        object_types[obj_type] = {'count': 0, 'sample': obj}
    object_types[obj_type]['count'] += 1

print(f"\n2. OBJECT TYPES ({len(object_types)} unique types):")
for obj_type, data in sorted(object_types.items(), key=lambda x: x[1]['count'], reverse=True):
    print(f"   - {obj_type}: {data['count']} objects")

# Analyze dimensions specifically
print(f"\n3. DIMENSION ANALYSIS:")
dims = [obj for obj in drawing_data.get('objects', []) if 'Dimension' in obj.get('type_name', '')]
if dims:
    sample_dim = dims[0]
    print(f"   - Total dimensions: {len(dims)}")
    print(f"   - Sample dimension structure:")
    print(f"     Type: {sample_dim.get('type_name')}")
    print(f"     Has geometric_data: {'geometric_data' in sample_dim}")
    if 'geometric_data' in sample_dim:
        geo = sample_dim['geometric_data']
        print(f"     Dimension points: {len(geo.get('dimension_points', []))}")
        if geo.get('dimension_points'):
            print(f"     Sample point: {geo['dimension_points'][0]}")
        print(f"     Geometric data keys: {list(geo.keys())}")
    print(f"     Related objects: {sample_dim.get('related_objects', [])}")
    print(f"     Properties keys: {list(sample_dim.get('properties', {}).keys())}")

# Analyze parts
print(f"\n4. PART ANALYSIS:")
parts = [obj for obj in drawing_data.get('objects', []) if obj.get('type_name') == 'Part']
if parts:
    sample_part = parts[0]
    print(f"   - Total parts: {len(parts)}")
    print(f"   - Sample part structure:")
    print(f"     Type: {sample_part.get('type_name')}")
    print(f"     Has model_object_id: {'model_object_id' in sample_part}")
    print(f"     Has geometric_data: {'geometric_data' in sample_part}")
    if 'geometric_data' in sample_part:
        geo = sample_part['geometric_data']
        print(f"     Geometric data keys: {list(geo.keys())}")
        if 'start_point' in geo:
            print(f"     Start point: {geo['start_point']}")
        if 'end_point' in geo:
            print(f"     End point: {geo['end_point']}")
        if 'bounding_box_model' in geo:
            print(f"     BBox: {geo['bounding_box_model']}")

# Analyze views
print(f"\n5. VIEW ANALYSIS:")
views = [obj for obj in drawing_data.get('objects', []) if obj.get('type_name') == 'View']
if views:
    sample_view = views[0]
    print(f"   - Total views: {len(views)}")
    print(f"   - Sample view structure:")
    print(f"     Properties keys: {list(sample_view.get('properties', {}).keys())}")

print("\n" + "=" * 80)
print("KEY FINDINGS:")
print("=" * 80)
print("Current structure:")
print("  ✓ Drawing-level metadata (mark, type)")
print("  ✓ Sheet coordinate system (currently empty)")
print("  ✓ Object-level related_objects")
print("  ✓ Geometric data for dimensions and parts")
print("  ✓ Model object linkage (model_object_id)")
print("\nMissing for ML training (Phase 1 needed):")
print("  ✗ View transformation matrices (for 2D→3D)")
print("  ✗ Parent view for each object")
print("  ✗ 3D coordinates for dimension points")
print("  ✗ Dimension→part geometric relationships")
