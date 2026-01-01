#!/usr/bin/env python3
"""
COMPREHENSIVE ML TRAINING DATA ANALYSIS
Analyze extraction data to determine ML model viability and design
"""
import json
import math
from collections import defaultdict, Counter

# Load the most complete extraction file
extraction_file = "Server/TeklaMCP/extractions/tekla_extraction_20251213_180910.jsonl"

print("=" * 100)
print(" COMPREHENSIVE ML TRAINING DATA ANALYSIS - VIRTUAL DETAILER FEASIBILITY")
print("=" * 100)

with open(extraction_file, 'r', encoding='utf-8-sig') as f:
    line = f.readline()
    data = json.loads(line)

# ==================== 1. DATA INVENTORY ====================
print("\n" + "=" * 100)
print("1. DATA INVENTORY - What We Have")
print("=" * 100)

objects = data.get('objects', [])
views = data.get('views', [])

# Count by type
type_counts = Counter([obj.get('type_name', 'Unknown') for obj in objects])

print(f"\nDrawing: {data.get('mark', 'Unknown')}")
print(f"Total Objects: {len(objects)}")
print(f"Total Views: {len(views)}")
print(f"\nObject Type Breakdown:")
for obj_type, count in sorted(type_counts.items(), key=lambda x: x[1], reverse=True):
    print(f"  - {obj_type:30s}: {count:4d}")

# ==================== 2. GEOMETRIC DATA ANALYSIS ====================
print("\n" + "=" * 100)
print("2. GEOMETRIC DATA ANALYSIS - Input Features for ML")
print("=" * 100)

# Analyze Parts (primary geometric objects)
parts = [obj for obj in objects if obj.get('type_name') == 'Part']
print(f"\nPARTS (Structural Members): {len(parts)}")

# Check for geometric data in parts
parts_with_geom = [p for p in parts if 'geometric_data' in p]
print(f"  - Parts with geometric_data: {len(parts_with_geom)}")

if parts_with_geom:
    sample_part = parts_with_geom[0]
    geom_data = sample_part['geometric_data']
    print(f"\n  Sample Part Geometric Data:")
    print(f"    - Keys available: {list(geom_data.keys())}")

    if 'bounding_box' in geom_data:
        bbox = geom_data['bounding_box']
        print(f"    - Bounding Box: {bbox}")

    if 'vertices' in geom_data:
        vertices = geom_data['vertices']
        print(f"    - Vertices count: {len(vertices)}")
        print(f"    - Sample vertex: {vertices[0] if vertices else 'None'}")

# Analyze Dimensions (our target labels)
dimensions = [obj for obj in objects if 'Dimension' in obj.get('type_name', '')]
straight_dims = [obj for obj in objects if obj.get('type_name') == 'StraightDimension']
dim_sets = [obj for obj in objects if obj.get('type_name') == 'StraightDimensionSet']
angle_dims = [obj for obj in objects if obj.get('type_name') == 'AngleDimension']

print(f"\nDIMENSIONS (Target Labels): {len(dimensions)}")
print(f"  - StraightDimension: {len(straight_dims)}")
print(f"  - StraightDimensionSet: {len(dim_sets)}")
print(f"  - AngleDimension: {len(angle_dims)}")

# Check dimension geometric data
dims_with_geom = [d for d in dimensions if 'geometric_data' in d]
print(f"  - Dimensions with geometric_data: {len(dims_with_geom)}")

if dims_with_geom:
    sample_dim = dims_with_geom[0]
    dim_geom = sample_dim['geometric_data']
    print(f"\n  Sample Dimension Geometric Data:")
    print(f"    - Keys available: {list(dim_geom.keys())}")
    if 'dimension_points' in dim_geom:
        print(f"    - Dimension points: {dim_geom['dimension_points']}")

# Analyze Views (transformation context)
print(f"\nVIEWS (Coordinate Transformation): {len(views)}")
views_with_3d = [v for v in views if 'coordinate_system_3d' in v]
views_with_2d = [v for v in views if 'coordinate_system_2d' in v]
print(f"  - Views with 3D coordinate system: {len(views_with_3d)}")
print(f"  - Views with 2D coordinate system: {len(views_with_2d)}")

# ==================== 3. RELATIONSHIPS ANALYSIS ====================
print("\n" + "=" * 100)
print("3. RELATIONSHIPS ANALYSIS - Object Connections")
print("=" * 100)

objects_with_relations = [obj for obj in objects if 'related_objects' in obj and obj['related_objects']]
print(f"\nObjects with relationships: {len(objects_with_relations)} / {len(objects)} ({len(objects_with_relations)/len(objects)*100:.1f}%)")

# Analyze dimension-part relationships
dims_with_relations = [d for d in dimensions if 'related_objects' in d and d['related_objects']]
print(f"Dimensions with relationships: {len(dims_with_relations)} / {len(dimensions)} ({len(dims_with_relations)/len(dimensions)*100:.1f}%)")

if dims_with_relations:
    rel_counts = [len(d['related_objects']) for d in dims_with_relations]
    avg_rels = sum(rel_counts) / len(rel_counts)
    print(f"  - Average related objects per dimension: {avg_rels:.1f}")

# ==================== 4. ML TRAINING DATA QUALITY ====================
print("\n" + "=" * 100)
print("4. ML TRAINING DATA QUALITY ASSESSMENT")
print("=" * 100)

print("\n[+] STRENGTHS:")
if len(parts_with_geom) > 0:
    print(f"  [+] {len(parts_with_geom)} parts with geometric data (input features)")
if len(dims_with_geom) > 0:
    print(f"  [+] {len(dims_with_geom)} dimensions with geometric data (target labels)")
if len(views_with_3d) > 0:
    print(f"  [+] {len(views_with_3d)} views with 3D coordinate systems (transformation)")
if len(objects_with_relations) > 0:
    print(f"  [+] {len(objects_with_relations)} objects with relationships (context)")

print("\n[!] WEAKNESSES:")
if len(parts) - len(parts_with_geom) > 0:
    print(f"  [!] {len(parts) - len(parts_with_geom)} parts WITHOUT geometric data")
if len(dimensions) - len(dims_with_geom) > 0:
    print(f"  [!] {len(dimensions) - len(dims_with_geom)} dimensions WITHOUT geometric data")

# Check if we have parent view tracking
objects_with_parent_view = [obj for obj in objects if 'parent_view_id' in obj]
if len(objects_with_parent_view) == 0:
    print(f"  [!] NO parent view tracking (objects not linked to views)")

# Check if we have model linkage
objects_with_model_link = [obj for obj in objects if 'has_model_link' in obj and obj['has_model_link']]
if len(objects_with_model_link) == 0:
    print(f"  [!] NO model linkage (drawing objects not linked to 3D model)")

# ==================== 5. MINIMUM VIABLE MODEL ANALYSIS ====================
print("\n" + "=" * 100)
print("5. MINIMUM VIABLE MODEL (MVP) ANALYSIS")
print("=" * 100)

# Calculate training samples
training_pairs = 0
if len(dims_with_relations) > 0:
    # Each dimension-part relationship is a potential training sample
    training_pairs = sum(len(d['related_objects']) for d in dims_with_relations)

print(f"\nTraining Data Availability:")
print(f"  - Parts (input): {len(parts_with_geom)}")
print(f"  - Dimensions (target): {len(dims_with_geom)}")
print(f"  - Dimension-Part pairs: {training_pairs}")
print(f"  - Views (context): {len(views_with_3d)}")

# Assess MVP feasibility
print(f"\nMVP FEASIBILITY:")
if len(parts_with_geom) >= 100 and len(dims_with_geom) >= 50:
    print(f"  [+] SUFFICIENT data for MVP (>100 parts, >50 dimensions)")
    print(f"  [+] Can train: Dimension placement classifier")
elif len(parts_with_geom) >= 50 and len(dims_with_geom) >= 20:
    print(f"  [!] MARGINAL data for MVP (50-100 parts, 20-50 dimensions)")
    print(f"  [!] May need: Data augmentation or transfer learning")
else:
    print(f"  [-] INSUFFICIENT data for MVP (<50 parts, <20 dimensions)")
    print(f"  [-] Need: More drawings or better extraction")

# ==================== 6. ML MODEL DESIGN RECOMMENDATIONS ====================
print("\n" + "=" * 100)
print("6. ML MODEL DESIGN RECOMMENDATIONS")
print("=" * 100)

print("\nRECOMMENDED APPROACH: Phased Iterative Development")
print("\n┌─────────────────────────────────────────────────────────────────────────┐")
print("│ PHASE 1: Overall Dimension Detection (Bounding Box Dimensions)         │")
print("├─────────────────────────────────────────────────────────────────────────┤")
print("│ INPUT:  Part bounding box (min/max x,y,z)                              │")
print("│ OUTPUT: 4 dimension placements (top, bottom, left, right)              │")
print("│ MODEL:  Simple classifier or rule-based                                │")
print("│ TARGET: 90% accuracy on overall dimensions                             │")
print("└─────────────────────────────────────────────────────────────────────────┘")

print("\n┌─────────────────────────────────────────────────────────────────────────┐")
print("│ PHASE 2: Feature Dimension Detection (Holes, Cuts, Notches)            │")
print("├─────────────────────────────────────────────────────────────────────────┤")
print("│ INPUT:  Part vertices + edges                                          │")
print("│ OUTPUT: Feature-specific dimension placements                          │")
print("│ MODEL:  Graph neural network or attention mechanism                    │")
print("│ TARGET: 80% accuracy on feature dimensions                             │")
print("└─────────────────────────────────────────────────────────────────────────┘")

print("\n┌─────────────────────────────────────────────────────────────────────────┐")
print("│ PHASE 3: Multi-Part Assembly Dimensions                                │")
print("├─────────────────────────────────────────────────────────────────────────┤")
print("│ INPUT:  Multiple part geometries + assembly context                    │")
print("│ OUTPUT: Assembly dimension placements                                  │")
print("│ MODEL:  Relational network with part interactions                      │")
print("│ TARGET: 70% accuracy on assembly dimensions                            │")
print("└─────────────────────────────────────────────────────────────────────────┘")

# ==================== 7. DATA REQUIREMENTS vs CURRENT STATE ====================
print("\n" + "=" * 100)
print("7. DATA REQUIREMENTS vs CURRENT STATE")
print("=" * 100)

requirements = {
    "Part bounding boxes": (len(parts_with_geom), len(parts), "REQUIRED for Phase 1"),
    "Part vertices": (len([p for p in parts_with_geom if 'vertices' in p.get('geometric_data', {})]), len(parts), "REQUIRED for Phase 2"),
    "Dimension positions": (len(dims_with_geom), len(dimensions), "REQUIRED for all phases"),
    "Part-Dimension relationships": (len(dims_with_relations), len(dimensions), "REQUIRED for supervised learning"),
    "View transformations": (len(views_with_3d), len(views), "REQUIRED for 2D/3D mapping"),
    "Model linkage (3D parts)": (len(objects_with_model_link), len(parts), "OPTIONAL but helpful"),
    "Parent view tracking": (len(objects_with_parent_view), len(objects), "OPTIONAL but helpful")
}

print("\nData Feature          | Have | Need | Status | Purpose")
print("-" * 95)
for feature, (have, need, purpose) in requirements.items():
    coverage = have/need*100 if need > 0 else 0
    status = "[+]" if coverage >= 80 else "[!]" if coverage >= 50 else "[-]"
    print(f"{feature:25s} | {have:4d} | {need:4d} | {status} {coverage:3.0f}% | {purpose}")

# ==================== 8. NEXT STEPS ====================
print("\n" + "=" * 100)
print("8. NEXT STEPS TO ACHIEVE VIRTUAL DETAILER")
print("=" * 100)

print("\nIMMEDIATE ACTIONS (Week 1-2):")
print("  1. [+] Extract data from more drawings (target: 10-20 drawings)")
print("  2. [!] Fix parent view tracking in extraction code")
print("  3. [!] Fix model linkage in extraction code")
print("  4. [+] Build Phase 1 MVP: Overall dimension classifier")

print("\nSHORT-TERM (Week 3-4):")
print("  5. Test Phase 1 model on actual drawings")
print("  6. Collect human feedback on dimension quality")
print("  7. Improve model based on feedback")
print("  8. Start Phase 2: Feature dimension detection")

print("\nMEDIUM-TERM (Month 2-3):")
print("  9. Integrate Phases 1+2 into production workflow")
print("  10. Collect more training data from production use")
print("  11. Build Phase 3: Assembly dimensions")
print("  12. Achieve 90% overall accuracy target")

print("\n" + "=" * 100)
print("ANALYSIS COMPLETE")
print("=" * 100)
