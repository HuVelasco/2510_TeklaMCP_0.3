# Raw Extraction Data - Quick Access Guide

## 📁 Location of Raw JSON Data

### Primary Dataset File (Used for Training)
```
Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_FINAL.jsonl
```
- **Size:** 1.9 MB
- **Format:** JSONL (JSON Lines - one JSON object per line)
- **Contains:** 95 drawings with full geometry and dimension data
- **Created:** 2025-12-20

### Alternative/Related Files
```
Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_ML_READY.jsonl   (1.5 MB)
Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_111510.jsonl    (4.7 MB - unfiltered)
```

---

## 🔍 How to View the Raw Data

### Method 1: Quick Peek (First Drawing)
```bash
# View first drawing (first line of JSONL)
head -1 Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_FINAL.jsonl | python -m json.tool
```

### Method 2: Count Total Drawings
```bash
# Count lines (each line = 1 drawing)
wc -l Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_FINAL.jsonl
```

### Method 3: Open in VS Code
```bash
code Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_FINAL.jsonl
```

### Method 4: Python Script to Explore
```python
import json
from pathlib import Path

# Load the extraction file
extraction_file = Path('Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_FINAL.jsonl')

# Read all drawings
drawings = []
with open(extraction_file, 'r', encoding='utf-8') as f:
    for line in f:
        if line.strip():
            drawings.append(json.loads(line))

print(f"Total drawings: {len(drawings)}")

# Explore first drawing
first_drawing = drawings[0]
print(f"\nFirst drawing mark: {first_drawing.get('mark')}")
print(f"Number of objects: {len(first_drawing.get('objects', []))}")
print(f"Number of views: {len(first_drawing.get('views', []))}")

# Count object types
from collections import Counter
object_types = Counter(obj.get('type_name') for obj in first_drawing.get('objects', []))
print(f"\nObject types in first drawing:")
for obj_type, count in object_types.items():
    print(f"  {obj_type}: {count}")
```

---

## 📊 File Format: JSONL (JSON Lines)

**What is JSONL?**
- Each line is a complete JSON object (one drawing)
- NOT a JSON array (no `[` `]` wrapping all drawings)
- Easier to process large files (can read line-by-line)

**Structure:**
```
{"drawing_1_data...}\n
{"drawing_2_data...}\n
{"drawing_3_data...}\n
...
```

---

## 📋 Data Structure Overview

### Top Level (Per Drawing)
```json
{
  "mark": "[EP1001]",
  "drawing_type": "AssemblyDrawing",
  "tekla_id": "1234567",
  "objects": [...],      // Parts, dimensions, etc.
  "views": [...]         // View coordinate systems
}
```

### Views Array
```json
"views": [
  {
    "coordinate_system_3d": {
      "origin": {"x": 53393.2, "y": 20751.4, "z": 3048.9},
      "axis_x": {"x": 1.0, "y": 0.0, "z": 0.0},
      "axis_y": {"x": 0.0, "y": 1.0, "z": 0.0},
      "normal": {"x": 0.0, "y": 0.0, "z": 1.0}
    },
    "frame_origin": {"x": -500.0, "y": -300.0, "z": 0.0},
    "name": "Front"
  }
]
```

### Objects Array
```json
"objects": [
  {
    "type_name": "Part",
    "tekla_id": 12345,
    "properties": {
      "Name": "BEAM",
      "Profile": "W21X50",
      "Material": "A992",
      "Class": "1"
    },
    "geometric_data": {
      "vertices": [
        {"x": 53393.2, "y": 20751.4, "z": 3048.9},
        {"x": 53545.6, "y": 20751.4, "z": 3048.9},
        ...
      ],
      "bounding_box": {
        "min": {"x": 53393.2, "y": 20751.4, "z": 3048.9},
        "max": {"x": 53545.6, "y": 20853.0, "z": 3150.5}
      }
    }
  },
  {
    "type_name": "StraightDimension",
    "tekla_id": 67890,
    "properties": {
      "StartPoint": "(-54.372, -138.113, 0.000)",
      "EndPoint": "(-54.372, -36.513, 0.000)",
      "Distance": "101.6"
    },
    "geometric_data": {
      "dimension_points": []  // Often empty, use properties instead
    }
  }
]
```

---

## 🔎 Quick Analysis Commands

### Count Drawings
```bash
wc -l Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_FINAL.jsonl
```

### Find Specific Drawing
```bash
# Find drawing by mark (e.g., EP1001)
grep -n "EP1001" Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_FINAL.jsonl
```

### Extract Single Drawing to Pretty JSON
```bash
# Get line 5 (drawing #5) formatted nicely
sed -n '5p' Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_FINAL.jsonl | python -m json.tool > drawing_5_pretty.json
```

### Count Object Types Across All Drawings
```python
import json
from collections import Counter

extraction_file = 'Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_FINAL.jsonl'

all_types = Counter()

with open(extraction_file, 'r', encoding='utf-8') as f:
    for line in f:
        if line.strip():
            drawing = json.loads(line)
            for obj in drawing.get('objects', []):
                all_types[obj.get('type_name')] += 1

print("Object Type Distribution:")
for obj_type, count in all_types.most_common():
    print(f"  {obj_type:25s}: {count:5d}")
```

---

## 🎯 Common Exploration Tasks

### Task 1: View a Specific Drawing
```python
import json

# Find drawing EP1001
with open('Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_FINAL.jsonl', 'r') as f:
    for line in f:
        drawing = json.loads(line)
        if 'EP1001' in drawing.get('mark', ''):
            # Pretty print
            print(json.dumps(drawing, indent=2))
            break
```

### Task 2: List All Drawing Marks
```python
import json

marks = []
with open('Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_FINAL.jsonl', 'r') as f:
    for line in f:
        drawing = json.loads(line)
        marks.append(drawing.get('mark', 'unknown').strip('[]'))

print(f"Total drawings: {len(marks)}")
print("\nDrawing marks:")
for i, mark in enumerate(sorted(marks), 1):
    print(f"  {i:2d}. {mark}")
```

### Task 3: Analyze Dimension Types
```python
import json
from collections import Counter

dimension_types = Counter()

with open('Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_FINAL.jsonl', 'r') as f:
    for line in f:
        drawing = json.loads(line)
        for obj in drawing.get('objects', []):
            type_name = obj.get('type_name', '')
            if 'Dimension' in type_name:
                dimension_types[type_name] += 1

print("Dimension Type Distribution:")
for dim_type, count in dimension_types.most_common():
    print(f"  {dim_type:25s}: {count:5d}")
```

### Task 4: Extract All Part Profiles
```python
import json
from collections import Counter

profiles = Counter()

with open('Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_FINAL.jsonl', 'r') as f:
    for line in f:
        drawing = json.loads(line)
        for obj in drawing.get('objects', []):
            if obj.get('type_name') == 'Part':
                profile = obj.get('properties', {}).get('Profile', 'Unknown')
                profiles[profile] += 1

print("Part Profile Distribution:")
for profile, count in profiles.most_common(20):
    print(f"  {profile:25s}: {count:5d}")
```

---

## 📦 File Size Comparison

```
Original extraction (unfiltered):    4.7 MB  (includes all objects)
Filtered for ML:                     1.9 MB  (only relevant objects)
ML-ready (further processed):        1.5 MB  (optimized structure)
```

**Why multiple versions?**
- **Unfiltered:** Everything extracted from Tekla (including UI elements, etc.)
- **Filtered:** Only parts, dimensions, and relevant objects
- **FINAL:** Best quality, ready for ML pipeline

---

## 🛠️ Interactive Exploration Tool

Save this as `explore_extraction.py`:

```python
#!/usr/bin/env python3
"""
Interactive Extraction Data Explorer
"""
import json
from pathlib import Path
from collections import Counter

def load_drawings(file_path):
    """Load all drawings from JSONL file"""
    drawings = []
    with open(file_path, 'r', encoding='utf-8') as f:
        for line in f:
            if line.strip():
                drawings.append(json.loads(line))
    return drawings

def main():
    extraction_file = Path('Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_FINAL.jsonl')

    print("Loading extraction data...")
    drawings = load_drawings(extraction_file)

    print(f"\n{'='*60}")
    print(f"EXTRACTION DATA EXPLORER")
    print(f"{'='*60}")
    print(f"\nTotal drawings: {len(drawings)}")

    while True:
        print(f"\n{'='*60}")
        print("Options:")
        print("  1. List all drawing marks")
        print("  2. View specific drawing (by mark)")
        print("  3. Count object types")
        print("  4. Count dimension types")
        print("  5. Show drawing statistics")
        print("  6. Exit")
        print(f"{'='*60}")

        choice = input("\nEnter choice (1-6): ").strip()

        if choice == '1':
            print("\nDrawing Marks:")
            for i, drawing in enumerate(drawings, 1):
                mark = drawing.get('mark', 'unknown').strip('[]')
                obj_count = len(drawing.get('objects', []))
                print(f"  {i:2d}. {mark:15s} ({obj_count:3d} objects)")

        elif choice == '2':
            mark = input("\nEnter drawing mark (e.g., EP1001): ").strip()
            for drawing in drawings:
                if mark in drawing.get('mark', ''):
                    print(json.dumps(drawing, indent=2))
                    break
            else:
                print(f"Drawing '{mark}' not found")

        elif choice == '3':
            object_types = Counter()
            for drawing in drawings:
                for obj in drawing.get('objects', []):
                    object_types[obj.get('type_name')] += 1

            print("\nObject Type Distribution:")
            for obj_type, count in object_types.most_common():
                print(f"  {obj_type:25s}: {count:5d}")

        elif choice == '4':
            dimension_types = Counter()
            for drawing in drawings:
                for obj in drawing.get('objects', []):
                    type_name = obj.get('type_name', '')
                    if 'Dimension' in type_name:
                        dimension_types[type_name] += 1

            print("\nDimension Type Distribution:")
            for dim_type, count in dimension_types.most_common():
                print(f"  {dim_type:25s}: {count:5d}")

        elif choice == '5':
            total_objects = sum(len(d.get('objects', [])) for d in drawings)
            total_views = sum(len(d.get('views', [])) for d in drawings)

            print("\nStatistics:")
            print(f"  Total drawings:     {len(drawings)}")
            print(f"  Total objects:      {total_objects}")
            print(f"  Total views:        {total_views}")
            print(f"  Avg objects/drawing: {total_objects/len(drawings):.1f}")
            print(f"  Avg views/drawing:   {total_views/len(drawings):.1f}")

        elif choice == '6':
            print("\nExiting...")
            break

        else:
            print("\nInvalid choice. Please enter 1-6.")

if __name__ == '__main__':
    main()
```

**Run it:**
```bash
cd VirtualDetailer_ML
python explore_extraction.py
```

---

## 📍 Quick Reference

| Task | Command/File |
|------|--------------|
| **Raw data location** | `Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_FINAL.jsonl` |
| **File format** | JSONL (one JSON per line) |
| **Number of drawings** | 95 |
| **File size** | 1.9 MB |
| **View in editor** | `code Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_FINAL.jsonl` |
| **Pretty print first** | `head -1 <file> \| python -m json.tool` |
| **Count drawings** | `wc -l <file>` |
| **Interactive explorer** | `python explore_extraction.py` |

---

**Created:** 2025-12-26
**Dataset:** tekla_extraction_ml_20251220_FINAL.jsonl
**Status:** Ready for exploration
