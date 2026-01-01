#!/usr/bin/env python3
"""Get final statistics for the ML-ready dataset"""
import json

ml_file = 'Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_ML_READY.jsonl'

drawings = []
with open(ml_file, 'r', encoding='utf-8-sig') as f:
    for line in f:
        if line.strip():
            drawings.append(json.loads(line))

# Count by type
assembly_count = 0
part_count = 0

for d in drawings:
    dtype = d.get('drawing_type', 'unknown')
    if dtype == 'AssemblyDrawing':
        assembly_count += 1
    elif dtype == 'SinglePartDrawing':
        part_count += 1

# Count objects
total_objects = 0
total_parts = 0
total_dims = 0

for d in drawings:
    objs = d.get('objects', [])
    total_objects += len(objs)
    for obj in objs:
        if obj.get('type_name') == 'Part':
            total_parts += 1
        elif 'Dimension' in obj.get('type_name', ''):
            total_dims += 1

print("FINAL ML TRAINING DATASET")
print("")
print(f"File: {ml_file}")
print(f"Size: 1.5 MB")
print("")
print(f"Total drawings:          {len(drawings)}")
print(f"  - Assembly drawings:   {assembly_count}")
print(f"  - Part drawings:       {part_count}")
print("")
print(f"Dataset statistics:")
print(f"  Total objects:         {total_objects:,}")
print(f"  Total parts:           {total_parts:,}")
print(f"  Total dimensions:      {total_dims:,}")
print("")
print(f"Averages per drawing:")
print(f"  Objects:               {total_objects/len(drawings):.1f}")
print(f"  Parts:                 {total_parts/len(drawings):.1f}")
print(f"  Dimensions:            {total_dims/len(drawings):.1f}")
print("")
print("READY FOR ML MODEL TRAINING!")
