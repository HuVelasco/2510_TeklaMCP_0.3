#!/usr/bin/env python3
"""
Process Tekla drawing extraction JSONL files for ML dataset analysis.

This script demonstrates how external tools can read the extraction data
WITHOUT Claude processing it, avoiding context saturation.
"""

import json
import sys
from pathlib import Path
from collections import Counter
from typing import Dict, List, Any


def load_extraction_file(jsonl_path: str) -> List[Dict[str, Any]]:
    """Load JSONL extraction file line by line (memory efficient)."""
    drawings = []
    with open(jsonl_path, 'r', encoding='utf-8') as f:
        for line_num, line in enumerate(f, 1):
            try:
                drawing = json.loads(line.strip())
                drawings.append(drawing)
            except json.JSONDecodeError as e:
                print(f"⚠️ Error parsing line {line_num}: {e}", file=sys.stderr)
    return drawings


def analyze_object_types(drawings: List[Dict[str, Any]]) -> Counter:
    """Analyze distribution of object types across all drawings."""
    type_counts = Counter()
    for drawing in drawings:
        for obj in drawing.get('objects', []):
            obj_type = obj.get('type', 'Unknown')
            type_counts[obj_type] += 1
    return type_counts


def analyze_dimensions(drawings: List[Dict[str, Any]]) -> Dict[str, Any]:
    """Extract and analyze dimension objects for ML training."""
    dimension_objects = []

    for drawing in drawings:
        for obj in drawing.get('objects', []):
            obj_type = obj.get('type', '')
            if 'Dimension' in obj_type:
                dimension_objects.append({
                    'drawing': drawing.get('mark'),
                    'type': obj_type,
                    'properties': obj.get('properties', {})
                })

    return {
        'total_dimensions': len(dimension_objects),
        'dimension_types': Counter(d['type'] for d in dimension_objects),
        'sample_dimensions': dimension_objects[:5]  # First 5 for inspection
    }


def generate_ml_summary(jsonl_path: str) -> None:
    """Generate comprehensive ML dataset summary."""
    print(f"📊 Processing extraction file: {jsonl_path}\n")

    # Load data
    drawings = load_extraction_file(jsonl_path)
    total_objects = sum(d.get('object_count', 0) for d in drawings)

    print(f"✅ Loaded {len(drawings)} drawings with {total_objects:,} objects\n")

    # Analyze object types
    type_counts = analyze_object_types(drawings)
    print("📋 Object Type Distribution:")
    for obj_type, count in type_counts.most_common(10):
        percentage = (count / total_objects) * 100
        print(f"  {obj_type:50s}: {count:6,} ({percentage:5.2f}%)")

    # Analyze dimensions specifically
    dim_analysis = analyze_dimensions(drawings)
    print(f"\n📐 Dimension Analysis:")
    print(f"  Total dimension objects: {dim_analysis['total_dimensions']:,}")
    print(f"\n  Dimension type breakdown:")
    for dim_type, count in dim_analysis['dimension_types'].items():
        print(f"    {dim_type:40s}: {count:6,}")

    # File size
    file_size_mb = Path(jsonl_path).stat().st_size / (1024 * 1024)
    print(f"\n💾 File size: {file_size_mb:.2f} MB")
    print(f"📊 Average objects per drawing: {total_objects / len(drawings):.1f}")


def extract_for_ml_training(jsonl_path: str, output_csv: str = None) -> None:
    """
    Extract dimension placement patterns for ML training.

    This would typically output CSV or structured format for ML frameworks.
    """
    import csv

    if output_csv is None:
        output_csv = jsonl_path.replace('.jsonl', '_ml_dataset.csv')

    drawings = load_extraction_file(jsonl_path)

    # Extract features relevant to dimension placement
    with open(output_csv, 'w', newline='', encoding='utf-8') as f:
        writer = csv.DictWriter(f, fieldnames=[
            'drawing_mark', 'object_type', 'property_count',
            'has_position', 'has_angle', 'dimension_type'
        ])
        writer.writeheader()

        for drawing in drawings:
            for obj in drawing.get('objects', []):
                obj_type = obj.get('type', '')
                props = obj.get('properties', {})

                # Filter for dimension-related objects
                if 'Dimension' in obj_type or 'Mark' in obj_type:
                    writer.writerow({
                        'drawing_mark': drawing.get('mark'),
                        'object_type': obj_type,
                        'property_count': len(props),
                        'has_position': 'InsertionPoint' in props or 'Position' in props,
                        'has_angle': 'Angle' in props or 'Rotation' in props,
                        'dimension_type': obj_type
                    })

    print(f"✅ ML training dataset exported to: {output_csv}")


if __name__ == '__main__':
    if len(sys.argv) < 2:
        print("Usage: python process_extraction.py <extraction_file.jsonl>")
        print("\nExample:")
        print("  python process_extraction.py C:\\Temp\\tekla_extraction_20251211_083345.jsonl")
        sys.exit(1)

    extraction_file = sys.argv[1]

    if not Path(extraction_file).exists():
        print(f"❌ File not found: {extraction_file}", file=sys.stderr)
        sys.exit(1)

    # Generate summary
    generate_ml_summary(extraction_file)

    # Extract ML training dataset
    print("\n" + "="*70)
    extract_for_ml_training(extraction_file)
