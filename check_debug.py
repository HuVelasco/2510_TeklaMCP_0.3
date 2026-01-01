import json

with open('Server/TeklaMCP/extractions/tekla_extraction_ml_20251228_103321.jsonl', 'r', encoding='utf-8-sig') as f:
    data = json.loads(f.readline())

print(f"Drawing: {data.get('mark')}")

parts = [o for o in data['objects'] if o.get('type_name') == 'Part']
contours = [p for p in parts if p.get('geometric_data', {}).get('part_geometry', {}).get('part_type_model') == 'ContourPlate']

print(f"ContourPlates found: {len(contours)}")

for i, p in enumerate(contours[:3]):
    geo = p.get('geometric_data', {}).get('part_geometry', {})
    print(f"\nContourPlate {i+1}:")
    print(f"  Debug: {geo.get('contour_debug', 'none')}")
    print(f"  Iter count: {geo.get('contour_iter_count', 'missing')}")
    if 'contour_points' in geo:
        print(f"  SUCCESS Contour points: {len(geo['contour_points'])}")
        print(f"     Sample: {geo['contour_points'][0] if geo['contour_points'] else 'none'}")
