#!/usr/bin/env python3
"""
Analyze All Extraction Files
- Generate summaries for each drawing
- Create comparison tables
- Visualize data and relationships
"""
import json
import os
import glob
from collections import defaultdict, Counter
from datetime import datetime

def load_extraction(jsonl_file):
    """Load extraction JSONL file"""
    with open(jsonl_file, 'r', encoding='utf-8-sig') as f:
        return json.loads(f.readline())

def analyze_single_drawing(data):
    """Analyze a single drawing extraction"""

    mark = data.get('mark', 'Unknown')
    drawing_type = data.get('drawing_type', 'unknown')
    assembly_prefix = data.get('assembly_prefix', 'unknown')
    part_type = data.get('part_type', 'unknown')
    unique_parts = data.get('unique_part_count', 0)

    objects = data.get('objects', [])
    views = data.get('views', [])
    metadata = data.get('metadata', {})

    # Count by type
    type_counts = Counter([obj.get('type_name', 'Unknown') for obj in objects])

    # Extract parts
    parts = [obj for obj in objects if obj.get('type_name') == 'Part']

    # Count parts with classification
    classified_parts = [p for p in parts if 'part_classification' in p]
    part_categories = Counter([
        p['part_classification']['category']
        for p in classified_parts
    ])

    # Count dimensions
    dimensions = [obj for obj in objects if 'Dimension' in obj.get('type_name', '')]
    dims_with_3d = [d for d in dimensions if 'dimension_points_3d' in d.get('geometric_data', {})]

    # Count objects with parent_view_id
    with_parent_view = [obj for obj in objects if 'parent_view_id' in obj]

    summary = {
        'mark': mark,
        'drawing_type': drawing_type,
        'assembly_prefix': assembly_prefix,
        'part_type': part_type,
        'unique_parts': unique_parts,
        'total_objects': len(objects),
        'views': len(views),
        'parts': {
            'total_instances': len(parts),
            'unique_count': unique_parts,
            'duplication_ratio': round(len(parts) / unique_parts, 1) if unique_parts > 0 else 0,
            'with_classification': len(classified_parts),
            'categories': dict(part_categories)
        },
        'dimensions': {
            'total': len(dimensions),
            'with_3d_coords': len(dims_with_3d),
            'types': dict(Counter([d.get('type_name') for d in dimensions]))
        },
        'metadata': {
            'title': metadata.get('drawing_title', 'Unknown'),
            'extraction_timestamp': metadata.get('extraction_timestamp', 'Unknown'),
            'total_dimensions': metadata.get('total_dimensions', 0)
        },
        'views': {
            'count': len(views),
            'objects_with_parent_view': len(with_parent_view),
            'coverage_percent': round(len(with_parent_view) / len(objects) * 100, 1) if objects else 0
        },
        'object_types': dict(type_counts.most_common(10))
    }

    return summary

def print_drawing_summary(summary):
    """Print formatted summary for a drawing"""
    print("=" * 80)
    print(f"DRAWING: {summary['mark']}")
    print("=" * 80)
    print(f"\nType: {summary['drawing_type']}")
    print(f"Prefix: {summary['assembly_prefix']}")
    print(f"Part Type: {summary['part_type']}")
    print(f"\nUnique Parts: {summary['unique_parts']}")
    print(f"Total Objects: {summary['total_objects']}")
    print(f"Views: {summary['views']['count']}")

    print(f"\n--- PARTS ---")
    print(f"Part Instances: {summary['parts']['total_instances']}")
    print(f"Unique Parts: {summary['parts']['unique_count']}")
    print(f"Duplication Ratio: {summary['parts']['duplication_ratio']}x (parts appear in ~{summary['parts']['duplication_ratio']} views)")
    print(f"With Classification: {summary['parts']['with_classification']}/{summary['parts']['total_instances']}")

    if summary['parts']['categories']:
        print(f"\nPart Categories:")
        for cat, count in sorted(summary['parts']['categories'].items(), key=lambda x: x[1], reverse=True):
            print(f"  - {cat}: {count}")

    print(f"\n--- DIMENSIONS ---")
    print(f"Total: {summary['dimensions']['total']}")
    print(f"With 3D Coordinates: {summary['dimensions']['with_3d_coords']}/{summary['dimensions']['total']}")

    if summary['dimensions']['types']:
        print(f"\nDimension Types:")
        for dtype, count in sorted(summary['dimensions']['types'].items(), key=lambda x: x[1], reverse=True):
            print(f"  - {dtype}: {count}")

    print(f"\n--- VIEWS & TRACKING ---")
    print(f"Total Views: {summary['views']['count']}")
    print(f"Objects with parent_view_id: {summary['views']['objects_with_parent_view']}")
    print(f"Coverage: {summary['views']['coverage_percent']}%")

    print(f"\n--- METADATA ---")
    print(f"Title: {summary['metadata']['title']}")
    print(f"Extracted: {summary['metadata']['extraction_timestamp']}")

    print("\n")

def create_comparison_table(all_summaries):
    """Create comparison table across all drawings"""

    print("\n" + "=" * 120)
    print("DRAWING COMPARISON TABLE")
    print("=" * 120)
    print()

    # Header
    print(f"{'Drawing':<15} {'Type':<15} {'Prefix':<8} {'Unique':<8} {'Dims':<6} {'Views':<6} {'3D%':<6} {'Parent%':<8} {'Classif%':<10}")
    print("-" * 120)

    # Data rows
    for s in all_summaries:
        dim_3d_pct = round(s['dimensions']['with_3d_coords'] / s['dimensions']['total'] * 100, 1) if s['dimensions']['total'] > 0 else 0
        classif_pct = round(s['parts']['with_classification'] / s['parts']['total_instances'] * 100, 1) if s['parts']['total_instances'] > 0 else 0

        print(f"{s['mark']:<15} {s['drawing_type']:<15} {s['assembly_prefix']:<8} {s['unique_parts']:<8} {s['dimensions']['total']:<6} {s['views']['count']:<6} {dim_3d_pct:<6.1f} {s['views']['coverage_percent']:<8.1f} {classif_pct:<10.1f}")

    print()

    # Aggregates
    print("\nAGGREGATE STATISTICS:")
    print("-" * 120)

    total_unique_parts = sum(s['unique_parts'] for s in all_summaries)
    total_dimensions = sum(s['dimensions']['total'] for s in all_summaries)

    assemblies = [s for s in all_summaries if s['drawing_type'] == 'assembly']
    parts = [s for s in all_summaries if s['drawing_type'] == 'part']

    print(f"Total Drawings: {len(all_summaries)}")
    print(f"  - Assemblies: {len(assemblies)}")
    print(f"  - Parts: {len(parts)}")
    print(f"  - Sub-Assemblies: {len([s for s in all_summaries if s['drawing_type'] == 'sub_assembly'])}")
    print(f"\nTotal Unique Parts: {total_unique_parts}")
    print(f"Total Dimensions: {total_dimensions}")
    print(f"\nPart Types Distribution:")

    all_part_types = Counter([s['part_type'] for s in all_summaries])
    for ptype, count in sorted(all_part_types.items(), key=lambda x: x[1], reverse=True):
        print(f"  - {ptype}: {count} drawings")

    print(f"\nPart Categories Distribution:")
    all_categories = Counter()
    for s in all_summaries:
        all_categories.update(s['parts']['categories'])

    for cat, count in sorted(all_categories.items(), key=lambda x: x[1], reverse=True):
        print(f"  - {cat}: {count} parts")

def find_common_patterns(all_summaries):
    """Find common patterns across all drawings"""

    print("\n" + "=" * 80)
    print("COMMON PATTERNS & RELATIONSHIPS")
    print("=" * 80)

    # Pattern 1: Drawing types vs unique parts
    print("\n1. Drawing Type vs Unique Part Count:")
    type_to_parts = defaultdict(list)
    for s in all_summaries:
        type_to_parts[s['drawing_type']].append(s['unique_parts'])

    for dtype, parts_list in sorted(type_to_parts.items()):
        avg = sum(parts_list) / len(parts_list)
        min_val = min(parts_list)
        max_val = max(parts_list)
        print(f"  {dtype:15s}: {avg:5.1f} avg parts (range: {min_val}-{max_val})")

    # Pattern 2: Prefix vs part categories
    print("\n2. Assembly Prefix vs Part Categories:")
    prefix_to_categories = defaultdict(lambda: Counter())
    for s in all_summaries:
        if s['assembly_prefix'] != 'unknown':
            prefix_to_categories[s['assembly_prefix']].update(s['parts']['categories'])

    for prefix, categories in sorted(prefix_to_categories.items()):
        print(f"  {prefix:8s}:")
        for cat, count in categories.most_common(3):
            print(f"    - {cat}: {count}")

    # Pattern 3: Views vs duplication
    print("\n3. View Count vs Part Duplication:")
    for s in sorted(all_summaries, key=lambda x: x['views']['count'], reverse=True)[:5]:
        print(f"  {s['mark']:15s}: {s['views']['count']:2d} views, {s['parts']['duplication_ratio']}x duplication")

    # Pattern 4: Dimensions per unique part
    print("\n4. Dimensions per Unique Part:")
    for s in sorted(all_summaries, key=lambda x: x['unique_parts']):
        if s['unique_parts'] > 0:
            ratio = s['dimensions']['total'] / s['unique_parts']
            print(f"  {s['mark']:15s}: {ratio:5.1f} dims/part ({s['dimensions']['total']} dims / {s['unique_parts']} parts)")

def generate_visualization_data(all_summaries):
    """Generate data for visualization"""

    viz_data = {
        'drawings': [],
        'part_distribution': {},
        'dimension_distribution': {},
        'relationships': []
    }

    for s in all_summaries:
        viz_data['drawings'].append({
            'mark': s['mark'],
            'type': s['drawing_type'],
            'unique_parts': s['unique_parts'],
            'dimensions': s['dimensions']['total'],
            'views': s['views']['count']
        })

        # Accumulate part categories
        for cat, count in s['parts']['categories'].items():
            viz_data['part_distribution'][cat] = viz_data['part_distribution'].get(cat, 0) + count

        # Accumulate dimension types
        for dtype, count in s['dimensions']['types'].items():
            viz_data['dimension_distribution'][dtype] = viz_data['dimension_distribution'].get(dtype, 0) + count

    # Save visualization data
    with open('visualization_data.json', 'w') as f:
        json.dump(viz_data, f, indent=2)

    print("\n" + "=" * 80)
    print("VISUALIZATION DATA GENERATED")
    print("=" * 80)
    print(f"Saved to: visualization_data.json")
    print(f"\nTo create visualizations, run:")
    print(f"  python create_visualizations.py")

def main():
    """Main analysis function"""

    print("="*80)
    print(" EXTRACTION DATA ANALYSIS")
    print("="*80)
    print()

    # Find all extraction files
    extraction_files = glob.glob("Server/TeklaMCP/extractions/tekla_extraction_ml_*.jsonl")

    if not extraction_files:
        print("ERROR: No extraction files found!")
        print("Please extract drawings first using Tekla.")
        print("See extract_test_drawings.md for instructions.")
        return

    print(f"Found {len(extraction_files)} extraction files\n")

    # Analyze each drawing
    all_summaries = []

    for extraction_file in sorted(extraction_files):
        try:
            data = load_extraction(extraction_file)
            summary = analyze_single_drawing(data)
            all_summaries.append(summary)
            print_drawing_summary(summary)
        except Exception as e:
            print(f"ERROR processing {extraction_file}: {e}\n")

    if not all_summaries:
        print("ERROR: No valid extractions found!")
        return

    # Create comparison table
    create_comparison_table(all_summaries)

    # Find common patterns
    find_common_patterns(all_summaries)

    # Generate visualization data
    generate_visualization_data(all_summaries)

    # Save detailed summaries
    with open('extraction_summaries.json', 'w') as f:
        json.dump(all_summaries, f, indent=2)

    print("\n" + "=" * 80)
    print("ANALYSIS COMPLETE")
    print("=" * 80)
    print(f"\nFiles generated:")
    print(f"  - extraction_summaries.json (detailed summaries)")
    print(f"  - visualization_data.json (for visualization)")
    print(f"\nNext: Run create_visualizations.py to generate charts")

if __name__ == "__main__":
    main()
