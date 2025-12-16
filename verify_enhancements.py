#!/usr/bin/env python3
"""Verify that extraction enhancements are present in the output data."""

import json
import sys

extraction_file = "C:/Users/hugov/ClaudeProjects/2510_TeklaMCP_0.3/Server/TeklaMCP/extractions/tekla_extraction_20251211_214922.jsonl"

print("Verifying Extraction Enhancements\n")
print("=" * 70)

# Read all drawings
drawings = []
with open(extraction_file, 'r', encoding='utf-8') as f:
    for line in f:
        drawings.append(json.loads(line))

print(f"Loaded {len(drawings)} drawings\n")

# Analyze across all drawings
total_objects = 0
enhancement_stats = {
    'model_linkage': 0,
    'bounding_boxes': 0,
    'dimension_positions': 0
}

for drawing in drawings:
    print(f"\n📐 Drawing: {drawing['mark']}")
    print(f"   Objects: {drawing['object_count']}")

    total_objects += drawing['object_count']

    for obj in drawing.get('objects', []):
        # Enhancement 1: Model linkage
        if 'has_model_link' in obj:
            enhancement_stats['model_linkage'] += 1

        # Enhancement 2: Bounding boxes
        if obj.get('geometric_data', {}).get('bounding_box_aabb'):
            enhancement_stats['bounding_boxes'] += 1

        # Enhancement 3: Dimension text positions
        if obj.get('geometric_data', {}).get('dimension_text_position'):
            enhancement_stats['dimension_positions'] += 1

print("\n" + "=" * 70)
print("\n ENHANCEMENT VERIFICATION RESULTS:\n")

print(f"[1]  Model Object Linkage:")
print(f"    Objects with has_model_link: {enhancement_stats['model_linkage']}")
print(f"    Coverage: {enhancement_stats['model_linkage']/total_objects*100:.1f}%")

print(f"\n[2]  Bounding Boxes (AABB):")
print(f"    Objects with bounding_box_aabb: {enhancement_stats['bounding_boxes']}")
print(f"    Coverage: {enhancement_stats['bounding_boxes']/total_objects*100:.1f}%")

print(f"\n[3]  Dimension Text Positions:")
print(f"    Objects with dimension_text_position: {enhancement_stats['dimension_positions']}")
print(f"    Coverage: {enhancement_stats['dimension_positions']/total_objects*100:.1f}%")

# Show examples
print("\n" + "=" * 70)
print("\n📋 SAMPLE DATA:\n")

for drawing in drawings[:1]:  # First drawing only
    objs = drawing['objects']

    # Find example with model linkage
    model_link_ex = next((o for o in objs if o.get('has_model_link')), None)
    if model_link_ex:
        print("[1]  Model Linkage Example:")
        print(f"    Type: {model_link_ex.get('type')}")
        print(f"    has_model_link: {model_link_ex.get('has_model_link')}")
        print(f"    model_object_id: {str(model_link_ex.get('model_object_id', 'N/A'))[:60]}")

    # Find example with bounding box
    bbox_ex = next((o for o in objs if o.get('geometric_data', {}).get('bounding_box_aabb')), None)
    if bbox_ex:
        bb = bbox_ex['geometric_data']['bounding_box_aabb']
        print(f"\n[2]  Bounding Box Example:")
        print(f"    Type: {bbox_ex.get('type')}")
        print(f"    Min Point: {bb['min']}")
        print(f"    Max Point: {bb['max']}")
        print(f"    AABB Type: {bb.get('type')}")

    # Find example with dimension position
    dim_pos_ex = next((o for o in objs if o.get('geometric_data', {}).get('dimension_text_position')), None)
    if dim_pos_ex:
        dp = dim_pos_ex['geometric_data']['dimension_text_position']
        print(f"\n[3]  Dimension Text Position Example:")
        print(f"    Type: {dim_pos_ex.get('type')}")
        print(f"    Position: {dp}")

print("\n" + "=" * 70)
print("\n VERIFICATION COMPLETE\n")

# Summary
if all(v > 0 for v in enhancement_stats.values()):
    print("🎉 SUCCESS: All three enhancements are present in the extraction data!")
else:
    print("  WARNING: Some enhancements may not be working:")
    if enhancement_stats['model_linkage'] == 0:
        print("    - Model linkage not found")
    if enhancement_stats['bounding_boxes'] == 0:
        print("    - Bounding boxes not found")
    if enhancement_stats['dimension_positions'] == 0:
        print("    - Dimension text positions not found")
