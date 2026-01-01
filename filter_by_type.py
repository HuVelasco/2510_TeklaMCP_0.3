#!/usr/bin/env python3
"""Filter by actual drawing TYPE, not prefix"""
import json

input_file = 'Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_111510.jsonl'
output_file = 'Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_FINAL.jsonl'

assembly_drawings = []
part_drawings = []
stairs_excluded = []
other_types = []

with open(input_file, 'r', encoding='utf-8-sig') as f:
    for line in f:
        if line.strip():
            d = json.loads(line)
            mark = d.get('mark', '').strip('[]').upper()
            dtype = d.get('drawing_type', 'unknown')

            # Exclude stairs (ST* prefix)
            if mark.startswith('ST'):
                stairs_excluded.append((mark, dtype))
                continue

            # Keep AssemblyDrawing and SinglePartDrawing
            if dtype == 'AssemblyDrawing':
                assembly_drawings.append(d)
            elif dtype == 'SinglePartDrawing':
                part_drawings.append(d)
            else:
                other_types.append((mark, dtype))

# Combine assembly + part drawings
all_drawings = assembly_drawings + part_drawings

# Write to file
with open(output_file, 'w', encoding='utf-8') as f:
    for d in all_drawings:
        f.write(json.dumps(d) + '\n')

print("CORRECT FILTERING BY DRAWING TYPE")
print("")
print(f"Total drawings processed: {len(all_drawings) + len(stairs_excluded) + len(other_types)}")
print("")
print(f"KEPT:")
print(f"  AssemblyDrawing:     {len(assembly_drawings)}")
print(f"  SinglePartDrawing:   {len(part_drawings)}")
print(f"  TOTAL KEPT:          {len(all_drawings)}")
print("")
print(f"EXCLUDED:")
print(f"  ST* (Stairs):        {len(stairs_excluded)}")
if other_types:
    print(f"  Other types:         {len(other_types)}")
print("")

# Show which ST drawings were excluded
if stairs_excluded:
    print("Stairs drawings excluded:")
    for mark, dtype in stairs_excluded:
        print(f"  {mark:20s} ({dtype})")
    print("")

# Show other types if any
if other_types:
    print("Other drawing types found:")
    for mark, dtype in other_types:
        print(f"  {mark:20s} ({dtype})")
    print("")

# Statistics
total_objects = sum(len(d.get('objects', [])) for d in all_drawings)
total_parts = 0
total_dims = 0

for d in all_drawings:
    for obj in d.get('objects', []):
        if obj.get('type_name') == 'Part':
            total_parts += 1
        elif 'Dimension' in obj.get('type_name', ''):
            total_dims += 1

print(f"ML Dataset Statistics:")
print(f"  Total drawings:      {len(all_drawings)}")
print(f"  Total objects:       {total_objects:,}")
print(f"  Total parts:         {total_parts:,}")
print(f"  Total dimensions:    {total_dims:,}")
print(f"  Avg dims/drawing:    {total_dims/len(all_drawings):.1f}")
print("")
print(f"Output: {output_file}")
print("READY FOR ML TRAINING!")
