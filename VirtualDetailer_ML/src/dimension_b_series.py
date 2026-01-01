#!/usr/bin/env python3
"""
Complete Pipeline: Extract B-series drawings and create dimensions
"""
import json
import time
from pathlib import Path
from inference import DimensionPredictor


def load_b_series_drawings():
    """Load B-series drawings from latest extraction"""
    extraction_dir = Path(__file__).parent.parent.parent / 'Server' / 'TeklaMCP' / 'extractions'

    # Find latest extraction file
    extraction_files = sorted(extraction_dir.glob('tekla_extraction_ml_202512*.jsonl'), reverse=True)

    if not extraction_files:
        print("No extraction files found")
        return []

    latest_extraction = extraction_files[0]
    print(f"Loading from: {latest_extraction.name}")

    drawings = []
    with open(latest_extraction, 'r', encoding='utf-8') as f:
        content = f.read().strip()

        # Try parsing as single JSON array
        try:
            all_drawings = json.loads(content)
            if not isinstance(all_drawings, list):
                all_drawings = [all_drawings]
        except json.JSONDecodeError:
            # Try JSONL format (one JSON per line)
            all_drawings = []
            for line in content.split('\n'):
                if line.strip():
                    try:
                        all_drawings.append(json.loads(line))
                    except json.JSONDecodeError as e:
                        print(f"Warning: Failed to parse line: {e}")
                        continue

        # Filter for B-series
        for drawing in all_drawings:
            mark = drawing.get('mark', '').strip('[]')
            if mark.startswith('B') and '-1' in mark:
                drawings.append(drawing)

    return drawings


def analyze_drawing(drawing):
    """Analyze drawing structure"""
    mark = drawing.get('mark', '').strip('[]')
    objects = drawing.get('objects', [])

    parts = [o for o in objects if o.get('type_name') == 'Part']
    dims = [o for o in objects if 'Dimension' in o.get('type_name', '')]
    views = drawing.get('views', [])

    return {
        'mark': mark,
        'parts': len(parts),
        'existing_dimensions': len(dims),
        'views': len(views),
        'total_objects': len(objects)
    }


def main():
    """Main pipeline"""
    print("\n" + "="*60)
    print("B-SERIES DIMENSION CREATION PIPELINE")
    print("="*60 + "\n")

    # Load B-series drawings
    print("Loading B-series test drawings...")
    b_drawings = load_b_series_drawings()

    if not b_drawings:
        print("No B-series drawings found in extraction.")
        print("Expected: B1-1, B2-1, B3-1, ..., B10-1")
        print("\nTo extract them:")
        print("  1. Open Tekla with the model containing B1-1 through B10-1")
        print("  2. Run extraction for each drawing")
        print("  3. Re-run this script")
        return

    print(f"Found {len(b_drawings)} B-series drawings\n")

    # Initialize predictor
    predictor = DimensionPredictor(use_ml_model=False)

    # Process each drawing
    results = []

    for i, drawing in enumerate(b_drawings):
        print(f"\n{i+1}. Processing drawing...")

        # Analyze
        analysis = analyze_drawing(drawing)
        print(f"   Mark: {analysis['mark']}")
        print(f"   Parts: {analysis['parts']}")
        print(f"   Existing dimensions: {analysis['existing_dimensions']}")
        print(f"   Views: {analysis['views']}")

        # Predict dimensions
        predictions = predictor.predict_dimensions(drawing)
        print(f"   Predicted dimensions: {len(predictions)}")

        # Show sample predictions
        if predictions:
            print(f"   Sample predictions:")
            for pred in predictions[:3]:
                print(f"     - {pred['type']} ({pred['axis']}): {pred['distance']:.1f}mm")

            if len(predictions) > 3:
                print(f"     ... and {len(predictions) - 3} more")

        results.append({
            'drawing': analysis['mark'],
            'analysis': analysis,
            'predictions': len(predictions),
            'prediction_details': predictions
        })

    # Summary
    print("\n" + "="*60)
    print("PIPELINE SUMMARY")
    print("="*60)

    total_predictions = sum(r['predictions'] for r in results)

    print(f"\nDrawings processed:      {len(results)}")
    print(f"Total dimensions predicted: {total_predictions}")
    print(f"Average per drawing:     {total_predictions/len(results):.1f}")

    print("\nPer-drawing breakdown:")
    for r in results:
        print(f"  {r['drawing']:15s}: {r['predictions']:3d} dimensions predicted")

    # Save results
    output_file = Path(__file__).parent.parent / 'data' / 'b_series_results.json'
    with open(output_file, 'w') as f:
        json.dump(results, f, indent=2)

    print(f"\nResults saved to: {output_file}")

    print("\n" + "="*60)
    print("NEXT STEPS")
    print("="*60)
    print("\nTo create dimensions in Tekla:")
    print("  1. Implement Tekla MCP dimension creation endpoint")
    print("  2. For each prediction, call MCP to create dimension")
    print("  3. Verify dimensions in Tekla drawings")
    print("\nCurrent status: Predictions ready, MCP integration needed")
    print("="*60 + "\n")


if __name__ == '__main__':
    main()
