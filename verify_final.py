import json

# Check the latest extraction
with open('Server/TeklaMCP/extractions/tekla_extraction_ml_20251228_103819.jsonl', 'r', encoding='utf-8-sig') as f:
    lines = f.readlines()

print(f"Total drawings extracted: {len(lines)}")
print()

for i, line in enumerate(lines):
    data = json.loads(line)
    parts = [o for o in data['objects'] if o.get('type_name') == 'Part']

    # Count geometry types
    beams = [p for p in parts if p.get('geometric_data', {}).get('part_geometry', {}).get('part_type_model') == 'Beam']
    contours = [p for p in parts if p.get('geometric_data', {}).get('part_geometry', {}).get('part_type_model') == 'ContourPlate']

    print(f"Drawing {i+1}: {data.get('mark')}")
    print(f"  Total objects: {len(data.get('objects', []))}")
    print(f"  Total parts: {len(parts)}")
    print(f"  Beams: {len(beams)}")
    print(f"  ContourPlates: {len(contours)}")

    # Check Beam geometry
    if beams:
        beam_geo = beams[0].get('geometric_data', {}).get('part_geometry', {})
        has_start_end = 'start_point' in beam_geo and 'end_point' in beam_geo
        print(f"  Beam has start/end points: {has_start_end}")
        if has_start_end:
            print(f"    Sample start_point: {beam_geo['start_point']}")

    # Check ContourPlate geometry
    if contours:
        contour_geo = contours[0].get('geometric_data', {}).get('part_geometry', {})
        has_contour_pts = 'contour_points' in contour_geo and len(contour_geo['contour_points']) > 0
        print(f"  ContourPlate has contour_points: {has_contour_pts}")
        if has_contour_pts:
            print(f"    Contour points count: {len(contour_geo['contour_points'])}")
            print(f"    Sample point: {contour_geo['contour_points'][0]}")

    print()
