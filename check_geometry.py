import json

# Check old successful extraction
with open('Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_FINAL.jsonl', 'r', encoding='utf-8-sig') as f:
    old_data = json.loads(f.readlines()[0])

print("=== OLD SUCCESSFUL EXTRACTION ===")
parts = [obj for obj in old_data['objects'] if obj.get('type_name') == 'Part'][:2]
for p in parts:
    geo = p.get('geometric_data', {}).get('part_geometry', {})
    print(f"\nPart type: {geo.get('part_type_model')}")
    print(f"  start_point: {geo.get('start_point', 'MISSING')}")
    print(f"  end_point: {geo.get('end_point', 'MISSING')}")
    print(f"  contour_points: {len(geo.get('contour_points', []))} points")
    print(f"  vertices: {len(geo.get('vertices', []))} vertices")

# Check new broken extraction
with open('Server/TeklaMCP/extractions/tekla_extraction_ml_20251228_083716.jsonl', 'r', encoding='utf-8-sig') as f:
    new_data = json.loads(f.readlines()[1])

print("\n\n=== NEW EXTRACTION (BROKEN) ===")
parts = [obj for obj in new_data['objects'] if obj.get('type_name') == 'Part'][:2]
for p in parts:
    geo = p.get('geometric_data', {}).get('part_geometry', {})
    print(f"\nPart type: {geo.get('part_type_model')}")
    print(f"  start_point: {geo.get('start_point', 'MISSING')}")
    print(f"  end_point: {geo.get('end_point', 'MISSING')}")
    print(f"  contour_points: {len(geo.get('contour_points', []))} points")
    print(f"  vertices: {len(geo.get('vertices', []))} vertices")
