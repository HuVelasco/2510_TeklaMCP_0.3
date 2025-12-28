#!/usr/bin/env python3
"""
Quick Raw Data Explorer
View the extracted Tekla drawing data
"""
import json
import sys
from pathlib import Path
from collections import Counter

def main():
    # Path to raw extraction data - accept from command line or auto-detect latest
    if len(sys.argv) > 1:
        extraction_file = Path(sys.argv[1])
    else:
        # Auto-detect: find the most recent extraction file
        extractions_dir = Path(__file__).parent.parent / 'Server' / 'TeklaMCP' / 'extractions'
        if extractions_dir.exists():
            extraction_files = sorted(extractions_dir.glob('*.jsonl'), key=lambda p: p.stat().st_mtime, reverse=True)
            if extraction_files:
                extraction_file = extraction_files[0]
                print(f"Auto-detected latest extraction: {extraction_file.name}")
            else:
                print(f"Error: No extraction files found in {extractions_dir}")
                print(f"Usage: python explore_raw_data.py <path_to_extraction_file.jsonl>")
                return
        else:
            print(f"Error: Extractions directory not found: {extractions_dir}")
            print(f"Usage: python explore_raw_data.py <path_to_extraction_file.jsonl>")
            return

    if not extraction_file.exists():
        print(f"Error: Extraction file not found at {extraction_file}")
        return

    print("\n" + "="*80)
    print("RAW EXTRACTION DATA EXPLORER")
    print("="*80)
    print(f"\nFile: {extraction_file.name}")
    print(f"Size: {extraction_file.stat().st_size / 1024 / 1024:.2f} MB")

    # Load all drawings
    print("\nLoading drawings...")
    drawings = []
    with open(extraction_file, 'r', encoding='utf-8-sig') as f:
        for line in f:
            if line.strip():
                drawings.append(json.loads(line))

    print(f"Loaded: {len(drawings)} drawings")

    # Quick statistics
    print("\n" + "="*80)
    print("QUICK STATISTICS")
    print("="*80)

    # Count objects and views
    total_objects = 0
    total_views = 0
    object_types = Counter()
    dimension_types = Counter()

    for drawing in drawings:
        objects = drawing.get('objects', [])
        views = drawing.get('views', [])

        total_objects += len(objects)
        total_views += len(views)

        for obj in objects:
            type_name = obj.get('type_name', 'Unknown')
            object_types[type_name] += 1

            if 'Dimension' in type_name:
                dimension_types[type_name] += 1

    print(f"\nTotal drawings:         {len(drawings)}")
    print(f"Total objects:          {total_objects}")
    print(f"Total views:            {total_views}")
    print(f"Avg objects/drawing:    {total_objects/len(drawings):.1f}")
    print(f"Avg views/drawing:      {total_views/len(drawings):.1f}")

    # Object types
    print("\n" + "-"*80)
    print("OBJECT TYPE DISTRIBUTION")
    print("-"*80)

    for obj_type, count in object_types.most_common():
        pct = (count / total_objects) * 100
        print(f"  {obj_type:30s}: {count:5d} ({pct:5.1f}%)")

    # Dimension types specifically
    print("\n" + "-"*80)
    print("DIMENSION TYPE DISTRIBUTION")
    print("-"*80)

    if dimension_types:
        total_dims = sum(dimension_types.values())
        for dim_type, count in dimension_types.most_common():
            pct = (count / total_dims) * 100
            print(f"  {dim_type:30s}: {count:5d} ({pct:5.1f}%)")
    else:
        print("  No dimensions found")

    # List all drawing marks
    print("\n" + "="*80)
    print("ALL DRAWING MARKS")
    print("="*80)

    for i, drawing in enumerate(drawings, 1):
        mark = drawing.get('mark', 'unknown').strip('[]')
        obj_count = len(drawing.get('objects', []))
        view_count = len(drawing.get('views', []))

        # Count dimensions in this drawing
        dim_count = sum(1 for obj in drawing.get('objects', [])
                       if 'Dimension' in obj.get('type_name', ''))

        print(f"  {i:2d}. {mark:15s} | {obj_count:3d} objects | {view_count} views | {dim_count:2d} dimensions")

    # Interactive mode
    print("\n" + "="*80)
    print("INTERACTIVE MODE")
    print("="*80)

    while True:
        print("\nOptions:")
        print("  1. View specific drawing (full JSON)")
        print("  2. View drawing summary")
        print("  3. Search for drawing by mark")
        print("  4. Show dimension details")
        print("  5. Export drawing(s) to JSON file")
        print("  6. Exit")

        choice = input("\nEnter choice (1-6): ").strip()

        if choice == '1':
            mark = input("Enter drawing mark (e.g., EP1001): ").strip()
            found = False
            for drawing in drawings:
                if mark.upper() in drawing.get('mark', '').upper():
                    print("\n" + "="*80)
                    print(f"DRAWING: {drawing.get('mark')}")
                    print("="*80)
                    print(json.dumps(drawing, indent=2))
                    found = True
                    break

            if not found:
                print(f"\nDrawing '{mark}' not found")

        elif choice == '2':
            mark = input("Enter drawing mark (e.g., EP1001): ").strip()
            found = False
            for drawing in drawings:
                if mark.upper() in drawing.get('mark', '').upper():
                    print("\n" + "="*80)
                    print(f"DRAWING SUMMARY: {drawing.get('mark')}")
                    print("="*80)

                    objects = drawing.get('objects', [])
                    views = drawing.get('views', [])

                    print(f"\nDrawing Type: {drawing.get('drawing_type')}")
                    print(f"Total Objects: {len(objects)}")
                    print(f"Total Views: {len(views)}")

                    # Object breakdown
                    obj_types = Counter(obj.get('type_name') for obj in objects)
                    print("\nObject breakdown:")
                    for obj_type, count in obj_types.most_common():
                        print(f"  {obj_type:25s}: {count:3d}")

                    # View details
                    print("\nViews:")
                    for i, view in enumerate(views):
                        print(f"  View {i}: {view.get('view_type', 'Unknown')}")

                    found = True
                    break

            if not found:
                print(f"\nDrawing '{mark}' not found")

        elif choice == '3':
            search = input("Enter search term: ").strip().upper()
            matches = []
            for drawing in drawings:
                mark = drawing.get('mark', '').upper()
                if search in mark:
                    matches.append(drawing)

            print(f"\nFound {len(matches)} matches:")
            for i, drawing in enumerate(matches, 1):
                mark = drawing.get('mark', '').strip('[]')
                obj_count = len(drawing.get('objects', []))
                print(f"  {i}. {mark:15s} ({obj_count} objects)")

        elif choice == '4':
            mark = input("Enter drawing mark (e.g., EP1001): ").strip()
            found = False
            for drawing in drawings:
                if mark.upper() in drawing.get('mark', '').upper():
                    print("\n" + "="*80)
                    print(f"DIMENSIONS IN: {drawing.get('mark')}")
                    print("="*80)

                    dimensions = [obj for obj in drawing.get('objects', [])
                                 if 'Dimension' in obj.get('type_name', '')]

                    print(f"\nTotal dimensions: {len(dimensions)}\n")

                    for i, dim in enumerate(dimensions, 1):
                        print(f"Dimension {i}:")
                        print(f"  Type: {dim.get('type_name')}")
                        props = dim.get('properties', {})
                        if 'StartPoint' in props:
                            print(f"  Start: {props.get('StartPoint')}")
                        if 'EndPoint' in props:
                            print(f"  End: {props.get('EndPoint')}")
                        if 'Distance' in props:
                            print(f"  Distance: {props.get('Distance')}")
                        print()

                    found = True
                    break

            if not found:
                print(f"\nDrawing '{mark}' not found")

        elif choice == '5':
            # Export drawing(s) to JSON file
            print("\n" + "="*80)
            print("EXPORT TO JSON FILE")
            print("="*80)
            print("\nExport options:")
            print("  1. Export specific drawing by mark")
            print("  2. Export all drawings")
            print("  3. Cancel")

            export_choice = input("\nEnter choice (1-3): ").strip()

            if export_choice == '1':
                mark = input("Enter drawing mark to export (e.g., B2): ").strip()
                found = False
                for drawing in drawings:
                    if mark.upper() in drawing.get('mark', '').upper():
                        output_file = f"exported_{mark}.json"
                        with open(output_file, 'w', encoding='utf-8') as f:
                            json.dump(drawing, f, indent=2, ensure_ascii=False)
                        print(f"\n✓ Exported to: {output_file}")
                        found = True
                        break

                if not found:
                    print(f"\nDrawing '{mark}' not found")

            elif export_choice == '2':
                output_file = "exported_all_drawings.json"
                with open(output_file, 'w', encoding='utf-8') as f:
                    json.dump(drawings, f, indent=2, ensure_ascii=False)
                print(f"\n✓ Exported {len(drawings)} drawings to: {output_file}")

            elif export_choice == '3':
                print("\nExport cancelled")
            else:
                print("\nInvalid export choice")

        elif choice == '6':
            print("\nExiting...")
            break

        else:
            print("\nInvalid choice")

    print("\n" + "="*80)
    print("DONE")
    print("="*80 + "\n")


if __name__ == '__main__':
    main()
