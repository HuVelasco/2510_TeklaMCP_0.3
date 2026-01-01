#!/usr/bin/env python3
"""Check what drawing types we have in the dataset"""
import json

jsonl_file = 'Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_111510.jsonl'

drawings = []
with open(jsonl_file, 'r', encoding='utf-8-sig') as f:
    for line in f:
        if line.strip():
            drawings.append(json.loads(line))

print(f"Total drawings: {len(drawings)}\n")

# Check drawing_type field
type_counts = {}
prefix_to_type = {}

for d in drawings:
    mark = d.get('mark', 'Unknown').strip('[]')
    dtype = d.get('drawing_type', 'unknown')

    type_counts[dtype] = type_counts.get(dtype, 0) + 1

    # Track prefix -> type mapping
    prefix = mark.split('-')[0] if '-' in mark else (mark[:2] if len(mark) >= 2 else mark)
    if prefix not in prefix_to_type:
        prefix_to_type[prefix] = {}
    prefix_to_type[prefix][dtype] = prefix_to_type[prefix].get(dtype, 0) + 1

print("Drawing types found:")
for dtype, count in sorted(type_counts.items()):
    print(f"  {dtype:20s}: {count:3d} drawings")

print("\nPrefix -> Type mapping:")
for prefix, types in sorted(prefix_to_type.items()):
    print(f"  {prefix:10s}: {types}")

print("\nSample drawings with their types:")
for i, d in enumerate(drawings[:10]):
    mark = d.get('mark', 'Unknown')
    dtype = d.get('drawing_type', 'unknown')
    print(f"  {mark:20s} -> {dtype}")
