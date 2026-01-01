#!/usr/bin/env python3
"""Filter out stairs drawings and create ML-ready dataset"""
import json
import sys

input_file = sys.argv[1] if len(sys.argv) > 1 else 'Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_111510.jsonl'
output_file = input_file.replace('.jsonl', '_filtered.jsonl')

drawings = []
stairs_drawings = []
excluded_count = 0

with open(input_file, 'r', encoding='utf-8-sig') as f:
    for line in f:
        if line.strip():
            d = json.loads(line)
            mark = d.get('mark', '').strip('[]').upper()

            if mark.startswith('ST'):
                stairs_drawings.append(mark)
                excluded_count += 1
            else:
                drawings.append(d)

# Write filtered dataset
with open(output_file, 'w', encoding='utf-8') as f:
    for d in drawings:
        f.write(json.dumps(d) + '\n')

print(f"Dataset Filtering Complete")
print(f"")
print(f"Input:  {input_file}")
print(f"Output: {output_file}")
print(f"")
print(f"Total drawings read:      {len(drawings) + excluded_count}")
print(f"Stairs excluded (ST*):    {excluded_count}")
print(f"Final training dataset:   {len(drawings)}")
print(f"")

if stairs_drawings:
    print(f"Excluded stairs drawings:")
    for mark in stairs_drawings:
        print(f"  - {mark}")

# Quick stats
total_objects = sum(len(d.get('objects', [])) for d in drawings)
total_parts = 0
total_dims = 0

for d in drawings:
    for obj in d.get('objects', []):
        if obj.get('type_name') == 'Part':
            total_parts += 1
        elif 'Dimension' in obj.get('type_name', ''):
            total_dims += 1

print(f"")
print(f"ML Training Dataset Stats:")
print(f"  Drawings:    {len(drawings)}")
print(f"  Objects:     {total_objects:,}")
print(f"  Parts:       {total_parts:,}")
print(f"  Dimensions:  {total_dims:,}")
print(f"  Avg dims/drawing: {total_dims/len(drawings):.1f}")
print(f"")
print(f"READY FOR ML MODEL TRAINING!")
