#!/usr/bin/env python3
"""Analyze the full extracted dataset"""
import json
import sys

jsonl_file = sys.argv[1] if len(sys.argv) > 1 else 'Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_111510.jsonl'

drawings = []
with open(jsonl_file, 'r', encoding='utf-8-sig') as f:
    for line in f:
        if line.strip():
            drawings.append(json.loads(line))

print(f"="*80)
print(f"FULL DATASET ANALYSIS")
print(f"="*80)
print(f"\nTotal drawings: {len(drawings)}")

# Analyze by drawing mark/prefix
marks = [d.get('mark', 'Unknown') for d in drawings]
prefixes = {}
for mark in marks:
    # Extract prefix (letters before numbers)
    import re
    match = re.match(r'^([A-Z]+)', mark.strip('[]'), re.IGNORECASE)
    prefix = match.group(1).upper() if match else 'Unknown'
    prefixes[prefix] = prefixes.get(prefix, 0) + 1

print(f"\nDrawings by prefix:")
for prefix, count in sorted(prefixes.items(), key=lambda x: -x[1]):
    print(f"  {prefix:15s}: {count:3d} drawings")

# Count stairs
stairs_count = sum(1 for m in marks if m.strip('[]').upper().startswith('ST'))
non_stairs_count = len(drawings) - stairs_count

print(f"\n{'─'*80}")
print(f"Stairs drawings (ST*): {stairs_count}")
print(f"Non-stairs drawings:   {non_stairs_count}")
print(f"{'─'*80}")

# Count objects and dimensions
total_objects = sum(len(d.get('objects', [])) for d in drawings)
total_dimensions = 0
total_parts = 0

for d in drawings:
    objs = d.get('objects', [])
    for obj in objs:
        if 'Dimension' in obj.get('type_name', ''):
            total_dimensions += 1
        elif obj.get('type_name') == 'Part':
            total_parts += 1

print(f"\nDataset totals:")
print(f"  Total objects:     {total_objects:,}")
print(f"  Total parts:       {total_parts:,}")
print(f"  Total dimensions:  {total_dimensions:,}")
print(f"  Avg per drawing:   {total_objects/len(drawings):.1f} objects")

# List stairs drawings
print(f"\nStairs drawings to exclude:")
stairs_marks = [m for m in marks if m.strip('[]').upper().startswith('ST')]
for mark in stairs_marks[:10]:  # Show first 10
    print(f"  - {mark}")
if len(stairs_marks) > 10:
    print(f"  ... and {len(stairs_marks) - 10} more")

print(f"\n{'='*80}")
print(f"RECOMMENDATION: Filter out {stairs_count} stairs drawings")
print(f"ML training dataset: {non_stairs_count} drawings")
print(f"{'='*80}")
