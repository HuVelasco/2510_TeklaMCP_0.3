#!/usr/bin/env python3
"""Quick inspection of extraction data"""
import json
import sys

jsonl_file = sys.argv[1] if len(sys.argv) > 1 else 'Server/TeklaMCP/extractions/tekla_extraction_ml_20251216_133837.jsonl'

with open(jsonl_file, 'r', encoding='utf-8-sig') as f:
    data = json.loads(f.readline())

print(f"Drawing: {data.get('mark')}")
print(f"Objects: {len(data.get('objects', []))}")
print(f"Views: {len(data.get('views', []))}")
print(f"Has assembly_prefix: {('assembly_prefix' in data)}")
print(f"Has drawing_type: {('drawing_type' in data)}")
print(f"Has part_type: {('part_type' in data)}")

print("\nFirst 10 object types:")
objs = data.get('objects', [])
for i, obj in enumerate(objs[:10]):
    obj_type = obj.get('type_name', '?')
    has_geom = 'geometric_data' in obj
    has_parent = 'parent_view_id' in obj
    print(f"  {i+1}. {obj_type:30s} geom={has_geom} parent_view={has_parent}")

# Count object types
type_counts = {}
for obj in objs:
    t = obj.get('type_name', 'Unknown')
    type_counts[t] = type_counts.get(t, 0) + 1

print("\nObject type distribution:")
for t, count in sorted(type_counts.items(), key=lambda x: -x[1])[:15]:
    print(f"  {t:30s}: {count:4d}")

# Check for dimensions with 3D points
dim_count = 0
dim_with_3d = 0
for obj in objs:
    if 'Dimension' in obj.get('type_name', ''):
        dim_count += 1
        geom = obj.get('geometric_data', {})
        if 'dimension_points_3d' in geom:
            dim_with_3d += 1

print(f"\nDimension analysis:")
print(f"  Total dimensions: {dim_count}")
print(f"  With 3D points: {dim_with_3d}")

# Check for parts with classification
part_count = 0
part_with_class = 0
for obj in objs:
    if obj.get('type_name') == 'Part':
        part_count += 1
        if 'part_classification' in obj:
            part_with_class += 1

print(f"\nPart analysis:")
print(f"  Total parts: {part_count}")
print(f"  With classification: {part_with_class}")
