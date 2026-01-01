#!/usr/bin/env python3
"""
Test Extraction Fixes - Verify all fixes are working
"""
import json
import sys

def test_extraction(jsonl_file):
    """Test extraction file for all required fixes"""

    print("="*80)
    print("EXTRACTION FIXES VERIFICATION")
    print("="*80)

    # Load extraction
    with open(jsonl_file, 'r', encoding='utf-8-sig') as f:
        data = json.loads(f.readline())

    drawing_mark = data.get('mark', 'Unknown')
    print(f"\nDrawing: {drawing_mark}")

    # TEST 1: Drawing Metadata
    print("\n[TEST 1] Drawing Metadata")
    print("-" * 80)

    has_assembly_prefix = 'assembly_prefix' in data or 'assemblyPrefix' in data
    has_part_type = 'part_type' in data or 'partType' in data
    has_drawing_type = 'drawing_type' in data or 'drawingType' in data

    print(f"  assembly_prefix: {'✓ PASS' if has_assembly_prefix else '✗ FAIL (MISSING)'}")
    if has_assembly_prefix:
        prefix = data.get('assembly_prefix', data.get('assemblyPrefix', 'N/A'))
        print(f"    Value: {prefix}")

    print(f"  part_type: {'✓ PASS' if has_part_type else '✗ FAIL (MISSING)'}")
    if has_part_type:
        ptype = data.get('part_type', data.get('partType', 'N/A'))
        print(f"    Value: {ptype}")

    print(f"  drawing_type: {'✓ PASS' if has_drawing_type else '✗ FAIL (MISSING)'}")
    if has_drawing_type:
        dtype = data.get('drawing_type', data.get('drawingType', 'N/A'))
        print(f"    Value: {dtype}")

    # TEST 2: Views Array
    print("\n[TEST 2] Views Array")
    print("-" * 80)

    views = data.get('views', [])
    views_count = len(views)

    if views_count > 0:
        print(f"  ✓ PASS: {views_count} views extracted")

        # Check first view has transformation matrices
        if views:
            sample_view = views[0]
            has_3d_cs = 'coordinate_system_3d' in sample_view
            has_2d_cs = 'coordinate_system_2d' in sample_view

            print(f"  3D coordinate system: {'✓ PASS' if has_3d_cs else '✗ FAIL'}")
            print(f"  2D coordinate system: {'✓ PASS' if has_2d_cs else '✗ FAIL'}")

            if has_3d_cs:
                cs3d = sample_view['coordinate_system_3d']
                print(f"    Sample origin: {cs3d.get('origin', 'N/A')}")
    else:
        print(f"  ✗ FAIL: Views array is empty")

    # TEST 3: parent_view_id
    print("\n[TEST 3] parent_view_id Tracking")
    print("-" * 80)

    objects = data.get('objects', [])
    objects_with_parent = [obj for obj in objects if 'parent_view_id' in obj]

    total_objects = len(objects)
    with_parent = len(objects_with_parent)
    coverage = (with_parent / total_objects * 100) if total_objects > 0 else 0

    print(f"  Total objects: {total_objects}")
    print(f"  With parent_view_id: {with_parent}")
    print(f"  Coverage: {coverage:.1f}%")

    if coverage > 50:
        print(f"  ✓ PASS: Good coverage")
    elif coverage > 0:
        print(f"  ⚠ WARNING: Low coverage (expected >50%)")
    else:
        print(f"  ✗ FAIL: No objects have parent_view_id")

    # TEST 4: Part Classification
    print("\n[TEST 4] Part Classification")
    print("-" * 80)

    parts = [obj for obj in objects if obj.get('type_name') == 'Part']
    parts_with_classification = [p for p in parts if 'part_classification' in p]

    print(f"  Total parts: {len(parts)}")
    print(f"  With classification: {len(parts_with_classification)}")

    if parts_with_classification:
        print(f"  ✓ PASS: Part classification present")
        # Show sample
        sample_class = parts_with_classification[0]['part_classification']
        print(f"    Sample category: {sample_class.get('category', 'N/A')}")
        print(f"    Sample part_name: {sample_class.get('part_name', 'N/A')}")
    else:
        print(f"  ⚠ WARNING: Part classification not added yet")

    # TEST 5: Deduplication Data
    print("\n[TEST 5] Deduplication (ModelIdentifier)")
    print("-" * 80)

    parts_with_model_id = [p for p in parts if 'model_object_id' in p]
    unique_model_ids = set(p['model_object_id'] for p in parts_with_model_id)

    print(f"  Part drawing objects: {len(parts)}")
    print(f"  With model_object_id: {len(parts_with_model_id)}")
    print(f"  Unique model IDs: {len(unique_model_ids)}")

    if len(unique_model_ids) > 0:
        print(f"  ✓ PASS: Deduplication data present")
        print(f"    Ratio: {len(parts) / len(unique_model_ids):.1f}x duplication (expected ~10-12x for assembly)")
    else:
        print(f"  ✗ FAIL: No model_object_id found")

    # TEST 6: 2D→3D Transformation
    print("\n[TEST 6] 2D→3D Transformation")
    print("-" * 80)

    dimensions = [obj for obj in objects if 'Dimension' in obj.get('type_name', '')]
    dims_with_3d = [d for d in dimensions if 'dimension_points_3d' in d.get('geometric_data', {})]

    print(f"  Total dimensions: {len(dimensions)}")
    print(f"  With 3D coordinates: {len(dims_with_3d)}")

    if len(dims_with_3d) > 0:
        print(f"  ✓ PASS: 2D→3D transformation working")
        # Show sample
        sample_dim = dims_with_3d[0]
        geom = sample_dim.get('geometric_data', {})
        pts_3d = geom.get('dimension_points_3d', [])
        if pts_3d:
            print(f"    Sample 3D point: {pts_3d[0]}")
    else:
        print(f"  ⚠ WARNING: No 3D coordinates found (not implemented yet)")

    # SUMMARY
    print("\n" + "="*80)
    print("SUMMARY")
    print("="*80)

    tests_passed = []
    tests_failed = []

    if has_assembly_prefix and has_part_type:
        tests_passed.append("Drawing metadata")
    else:
        tests_failed.append("Drawing metadata")

    if views_count > 0:
        tests_passed.append("Views array")
    else:
        tests_failed.append("Views array")

    if coverage > 50:
        tests_passed.append("parent_view_id tracking")
    elif coverage > 0:
        tests_passed.append("parent_view_id tracking (partial)")
    else:
        tests_failed.append("parent_view_id tracking")

    if len(unique_model_ids) > 0:
        tests_passed.append("Deduplication data")
    else:
        tests_failed.append("Deduplication data")

    print(f"\n✓ PASSED ({len(tests_passed)}): {', '.join(tests_passed) if tests_passed else 'None'}")
    print(f"✗ FAILED ({len(tests_failed)}): {', '.join(tests_failed) if tests_failed else 'None'}")

    print(f"\n⚠ OPTIONAL (not critical for MVP):")
    print(f"  - Part classification: {'✓ Added' if parts_with_classification else '⚠ Not added'}")
    print(f"  - 2D→3D transformation: {'✓ Working' if dims_with_3d else '⚠ Not added'}")

    # Overall status
    critical_passed = len(tests_failed) == 0
    if critical_passed:
        print(f"\n✓ OVERALL: READY FOR ML TRAINING")
    else:
        print(f"\n✗ OVERALL: NEEDS FIXES")

    return critical_passed

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python test_extraction_fixes.py <extraction_file.jsonl>")
        sys.exit(1)

    jsonl_file = sys.argv[1]
    passed = test_extraction(jsonl_file)
    sys.exit(0 if passed else 1)
