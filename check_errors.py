#!/usr/bin/env python3
"""Check for errors in extraction data"""
import json
import sys

jsonl_file = sys.argv[1] if len(sys.argv) > 1 else 'Server/TeklaMCP/extractions/tekla_extraction_ml_20251216_133837.jsonl'

with open(jsonl_file, 'r', encoding='utf-8-sig') as f:
    data = json.loads(f.readline())

print("="*80)
print("ERROR ANALYSIS")
print("="*80)

# Check for errors in dimensions
dims = [obj for obj in data.get('objects', []) if 'Dimension' in obj.get('type_name', '')]
print(f"\nTotal dimensions: {len(dims)}")

# Sample a few dimensions and check their geometric_data
print("\nFirst 3 dimensions details:")
for i, dim in enumerate(dims[:3]):
    print(f"\n--- Dimension {i+1}: {dim.get('type_name')} ---")
    geom = dim.get('geometric_data', {})
    print(f"  Has geometric_data: {bool(geom)}")
    if geom:
        print(f"  Keys: {list(geom.keys())}")
        if 'dimension_points' in geom:
            pts = geom['dimension_points']
            print(f"  dimension_points count: {len(pts) if isinstance(pts, list) else '?'}")
        if 'dimension_points_3d' in geom:
            print(f"  dimension_points_3d: PRESENT")
        if 'transform_error' in geom:
            print(f"  transform_error: {geom['transform_error']}")
        if 'dimension_extraction_error' in geom:
            print(f"  dimension_extraction_error: {geom['dimension_extraction_error']}")

    if 'debug_info' in dim:
        print(f"  Debug info: {dim['debug_info'][:200]}")

# Check parent_view_id in objects
objs_with_parent = [obj for obj in data.get('objects', []) if 'parent_view_id' in obj]
print(f"\n\nObjects with parent_view_id: {len(objs_with_parent)}/{len(data.get('objects', []))}")

if objs_with_parent:
    print("\nSample parent_view_ids:")
    for obj in objs_with_parent[:5]:
        print(f"  {obj.get('type_name')}: {obj.get('parent_view_id')}")

# Check for debug_info in objects
objs_with_debug = [obj for obj in data.get('objects', []) if 'debug_info' in obj]
print(f"\nObjects with debug_info: {len(objs_with_debug)}")

if objs_with_debug:
    print("\nSample debug info (first object):")
    print(f"  {objs_with_debug[0].get('debug_info')}")
