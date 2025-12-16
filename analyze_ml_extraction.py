#!/usr/bin/env python3
"""
Analyze ML-Optimized Extraction with View Transformations
"""
import json

# Read JSONL file with BOM handling
extraction_file = "Server/TeklaMCP/extractions/tekla_extraction_ml_20251215_134606.jsonl"

with open(extraction_file, 'r', encoding='utf-8-sig') as f:
    line = f.readline()
    drawing_data = json.loads(line)

print("=" * 80)
print("ML-OPTIMIZED EXTRACTION ANALYSIS - PHASE 1.1")
print("=" * 80)

# Check views extraction
view_count = drawing_data.get('view_count', 0)
views = drawing_data.get('views', [])

print(f"\n1. VIEWS EXTRACTION:")
print(f"   Total views: {view_count}")

if views:
    sample_view = views[0]
    print(f"\n   Sample View Structure:")
    print(f"   - View ID: {sample_view.get('view_id')}")
    print(f"   - Name: {sample_view.get('name')}")
    print(f"   - Type: {sample_view.get('view_type')}")
    print(f"   - Keys: {list(sample_view.keys())}")

    if 'coordinate_system_3d' in sample_view:
        cs3d = sample_view['coordinate_system_3d']
        print(f"\n   3D Coordinate System (SUCCESS!):")
        print(f"   - Origin: {cs3d.get('origin')}")
        print(f"   - Axis X: {cs3d.get('axis_x')}")
        print(f"   - Axis Y: {cs3d.get('axis_y')}")
        print(f"   - Normal (Z): {cs3d.get('normal')}")
    else:
        print(f"\n   ERROR: No 3D coordinate system extracted!")

    if 'coordinate_system_2d' in sample_view:
        cs2d = sample_view['coordinate_system_2d']
        print(f"\n   2D Display Coordinate System:")
        print(f"   - Origin X: {cs2d.get('origin_x')}")
        print(f"   - Origin Y: {cs2d.get('origin_y')}")
    else:
        print(f"\n   WARNING: No 2D coordinate system extracted!")

# Check parent view tracking
print(f"\n2. PARENT VIEW TRACKING:")
objects = drawing_data.get('objects', [])
objects_with_parent = [obj for obj in objects if 'parent_view_id' in obj]
print(f"   Total objects: {len(objects)}")
print(f"   Objects with parent_view_id: {len(objects_with_parent)}")
print(f"   Coverage: {len(objects_with_parent)/len(objects)*100:.1f}%" if objects else "   N/A")

if objects_with_parent:
    sample = objects_with_parent[0]
    print(f"\n   Sample object with parent view:")
    print(f"   - Type: {sample.get('type_name')}")
    print(f"   - Parent View: {sample.get('parent_view_id')}")

# Analyze dimensions
print(f"\n3. DIMENSIONS WITH PARENT VIEW:")
dims = [obj for obj in objects if 'Dimension' in obj.get('type_name', '')]
dims_with_parent = [dim for dim in dims if 'parent_view_id' in dim]
print(f"   Total dimensions: {len(dims)}")
print(f"   Dimensions with parent view: {len(dims_with_parent)}")

if dims_with_parent:
    sample_dim = dims_with_parent[0]
    print(f"\n   Sample dimension:")
    print(f"   - Type: {sample_dim.get('type_name')}")
    print(f"   - Parent View: {sample_dim.get('parent_view_id')}")
    if 'geometric_data' in sample_dim:
        print(f"   - Geometric data keys: {list(sample_dim['geometric_data'].keys())}")

# Analyze parts with model linkage
print(f"\n4. PARTS WITH 3D MODEL LINKAGE:")
parts = [obj for obj in objects if obj.get('type_name') == 'Part']
parts_with_model = [part for part in parts if part.get('has_model_link')]
print(f"   Total parts: {len(parts)}")
print(f"   Parts with model link: {len(parts_with_model)}")
print(f"   Coverage: {len(parts_with_model)/len(parts)*100:.1f}%" if parts else "   N/A")

print("\n" + "=" * 80)
print("PHASE 1.1 STATUS: VIEW TRANSFORMATION EXTRACTION")
print("=" * 80)
print(f"Status:")
print(f"  - View extraction: {'SUCCESS' if view_count > 0 else 'FAILED'}")
print(f"  - 3D transformation matrices: {'SUCCESS' if views and 'coordinate_system_3d' in views[0] else 'FAILED'}")
print(f"  - 2D display coordinates: {'SUCCESS' if views and 'coordinate_system_2d' in views[0] else 'FAILED'}")
print(f"  - Parent view tracking: {'SUCCESS' if objects_with_parent else 'FAILED'}")
print(f"  - Model object linkage: {'SUCCESS' if parts_with_model else 'FAILED'}")

print("\nNext Steps:")
print("  - PHASE 1.2: Reorganize data by views (view-centric structure)")
print("  - PHASE 1.3: Implement 2D->3D dimension transformation")
print("  - PHASE 1.4: Build geometric relationships")
