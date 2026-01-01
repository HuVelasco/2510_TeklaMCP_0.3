#!/usr/bin/env python3
"""
Extract B1-1 through B10-1 test drawings from Tekla
"""
import json
from pathlib import Path


# Use the existing extraction to find B drawings
extraction_file = Path(__file__).parent.parent.parent / 'Server' / 'TeklaMCP' / 'extractions' / 'tekla_extraction_ml_20251220_FINAL.jsonl'

if not extraction_file.exists():
    print(f"Error: {extraction_file} not found")
    exit(1)

print("\n" + "="*60)
print("SEARCHING FOR B-SERIES TEST DRAWINGS")
print("="*60 + "\n")

b_drawings = []

with open(extraction_file, 'r') as f:
    for line in f:
        if line.strip():
            drawing = json.loads(line)
            mark = drawing.get('mark', '').strip('[]')

            # Look for B1-1, B2-1, etc.
            if mark.startswith('B') and '-1' in mark:
                dims = [o for o in drawing['objects'] if 'Dimension' in o.get('type_name', '')]
                parts = [o for o in drawing['objects'] if o.get('type_name') == 'Part']

                b_drawings.append({
                    'mark': mark,
                    'dimensions': len(dims),
                    'parts': len(parts),
                    'drawing': drawing
                })

if not b_drawings:
    print("No B-series (-1 suffix) drawings found in extraction.")
    print("\nSearching for all B-series drawings:")

    with open(extraction_file, 'r') as f:
        for line in f:
            if line.strip():
                drawing = json.loads(line)
                mark = drawing.get('mark', '').strip('[]')

                if mark.startswith('B'):
                    dims = [o for o in drawing['objects'] if 'Dimension' in o.get('type_name', '')]
                    parts = [o for o in drawing['objects'] if o.get('type_name') == 'Part']

                    print(f"  {mark:15s}: {len(parts):2d} parts, {len(dims):3d} dimensions")
                    b_drawings.append({
                        'mark': mark,
                        'dimensions': len(dims),
                        'parts': len(parts),
                        'drawing': drawing
                    })
else:
    print(f"Found {len(b_drawings)} B-series test drawings:")
    for bd in b_drawings:
        print(f"  {bd['mark']:15s}: {bd['parts']:2d} parts, {bd['dimensions']:3d} dimensions")

if b_drawings:
    # Save for inference
    output_file = Path(__file__).parent.parent / 'data' / 'b_series_test_drawings.json'
    with open(output_file, 'w') as f:
        json.dump([bd['drawing'] for bd in b_drawings], f, indent=2)

    print(f"\n Saved {len(b_drawings)} drawings to: {output_file}")
else:
    print("\nNo B-series drawings found.")
    print("These may need to be extracted from Tekla separately.")

print("="*60 + "\n")
