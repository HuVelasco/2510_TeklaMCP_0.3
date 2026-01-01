import json
import os
from pathlib import Path

extractions_dir = Path('Server/TeklaMCP/extractions')
files = sorted(extractions_dir.glob('*.jsonl'), key=os.path.getmtime, reverse=True)

print(f"Checking {len(files)} extraction files for geometry data...\n")

for file in files:
    if file.stat().st_size == 0:
        continue

    try:
        with open(file, 'r', encoding='utf-8-sig') as f:
            # Check first drawing only
            first_line = f.readline()
            if not first_line.strip():
                continue
            data = json.loads(first_line)

        parts = [obj for obj in data.get('objects', []) if obj.get('type_name') == 'Part']
        if not parts:
            continue

        # Check for geometry data
        has_start_end = False
        has_contour = False

        for part in parts[:10]:  # Check first 10 parts
            geo = part.get('geometric_data', {}).get('part_geometry', {})
            if geo.get('start_point'):
                has_start_end = True
            if geo.get('contour_points') and len(geo.get('contour_points', [])) > 0:
                has_contour = True

        if has_start_end or has_contour:
            print(f"✅ {file.name}")
            print(f"   start/end points: {has_start_end}")
            print(f"   contour points: {has_contour}")
            # Show sample
            for part in parts[:2]:
                geo = part.get('geometric_data', {}).get('part_geometry', {})
                print(f"   - {geo.get('part_type_model')}: start={geo.get('start_point', 'none')}, contour={len(geo.get('contour_points', []))}")
            print()

    except Exception as e:
        continue

print("\nDone.")
