import json

# Check FINAL file
with open('Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_FINAL.jsonl', 'r', encoding='utf-8-sig') as f:
    data = json.loads(f.readlines()[0])

parts = [obj for obj in data['objects'] if obj.get('type_name') == 'Part']
contours = [p for p in parts if p.get('geometric_data', {}).get('part_geometry', {}).get('part_type_model') == 'ContourPlate']

print(f"FINAL file: Found {len(contours)} ContourPlates")
if contours:
    geo = contours[0].get('geometric_data', {}).get('part_geometry', {})
    print(f"  contour_points: {len(geo.get('contour_points', []))}")
    print(json.dumps(geo, indent=2)[:1000])

# Check NEW file
with open('Server/TeklaMCP/extractions/tekla_extraction_ml_20251228_083716.jsonl', 'r', encoding='utf-8-sig') as f:
    data = json.loads(f.readlines()[1])

parts = [obj for obj in data['objects'] if obj.get('type_name') == 'Part']
contours = [p for p in parts if p.get('geometric_data', {}).get('part_geometry', {}).get('part_type_model') == 'ContourPlate']

print(f"\nNEW file: Found {len(contours)} ContourPlates")
if contours:
    geo = contours[0].get('geometric_data', {}).get('part_geometry', {})
    print(f"  contour_points: {len(geo.get('contour_points', []))}")
    print(json.dumps(geo, indent=2)[:1000])
