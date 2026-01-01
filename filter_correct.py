#!/usr/bin/env python3
"""Filter to keep ONLY assembly and part drawings (exclude stairs ST* and erection EP*)"""
import json

input_file = 'Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_111510.jsonl'
output_file = 'Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_ML_READY.jsonl'

kept_drawings = []
excluded_stairs = []
excluded_erection = []

with open(input_file, 'r', encoding='utf-8-sig') as f:
    for line in f:
        if line.strip():
            d = json.loads(line)
            mark = d.get('mark', '').strip('[]').upper()

            # Exclude ST* (stairs)
            if mark.startswith('ST'):
                excluded_stairs.append(mark)
                continue

            # Exclude EP* (erection plans)
            if mark.startswith('EP'):
                excluded_erection.append(mark)
                continue

            # Keep everything else (assembly + part drawings)
            kept_drawings.append(d)

# Write ML-ready dataset
with open(output_file, 'w', encoding='utf-8') as f:
    for d in kept_drawings:
        f.write(json.dumps(d) + '\n')

print("="*80)
print("CORRECTED ML DATASET FILTERING")
print("="*80)
print()
print(f"Input:  {input_file}")
print(f"Output: {output_file}")
print()
print(f"Total drawings read:     {len(kept_drawings) + len(excluded_stairs) + len(excluded_erection)}")
print(f"Excluded - ST* (Stairs): {len(excluded_stairs)}")
print(f"Excluded - EP* (Erection): {len(excluded_erection)}")
print(f"{'─'*80}")
print(f"ML TRAINING DATASET:     {len(kept_drawings)} drawings")
print()

# Analyze kept drawings by type
assembly_count = 0
part_count = 0

for d in kept_drawings:
    dtype = d.get('drawing_type', 'unknown')
    if dtype == 'AssemblyDrawing':
        assembly_count += 1
    elif dtype == 'SinglePartDrawing':
        part_count += 1

print(f"Breakdown:")
print(f"  Assembly drawings:   {assembly_count}")
print(f"  Part drawings:       {part_count}")
print()

# Count objects
total_objects = sum(len(d.get('objects', [])) for d in kept_drawings)
total_parts = 0
total_dims = 0

for d in kept_drawings:
    for obj in d.get('objects', []):
        if obj.get('type_name') == 'Part':
            total_parts += 1
        elif 'Dimension' in obj.get('type_name', ''):
            total_dims += 1

print(f"Dataset Statistics:")
print(f"  Total objects:       {total_objects:,}")
print(f"  Total parts:         {total_parts:,}")
print(f"  Total dimensions:    {total_dims:,}")
print(f"  Avg objects/drawing: {total_objects/len(kept_drawings):.1f}")
print(f"  Avg dims/drawing:    {total_dims/len(kept_drawings):.1f}")
print()

if excluded_stairs:
    print(f"Excluded ST* (Stairs):")
    for mark in sorted(excluded_stairs):
        print(f"  - {mark}")
    print()

if excluded_erection:
    print(f"Excluded EP* (Erection Plans): {len(excluded_erection)} drawings")
    print(f"  (EP2000, EP2001, EP2003, ... and {len(excluded_erection)-3} more)")
    print()

print("="*80)
print("READY FOR ML MODEL TRAINING!")
print("="*80)
