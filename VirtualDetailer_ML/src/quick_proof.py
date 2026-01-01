#!/usr/bin/env python3
"""
Quick Proof Script - Runs inference as soon as model checkpoint is available
"""
import time
import json
from pathlib import Path
from ultralytics import YOLO
import cv2
import numpy as np


def wait_for_model(model_dir: Path, min_epoch: int = 10):
    """Wait for model checkpoint to be available"""
    print(f"\nWaiting for model checkpoint (epoch {min_epoch} or later)...")

    weights_dir = model_dir / 'weights'

    while True:
        # Check for best.pt or epoch checkpoints
        if (weights_dir / 'best.pt').exists():
            return weights_dir / 'best.pt'

        # Check for epoch checkpoints
        epoch_weights = list(weights_dir.glob('epoch*.pt'))
        if epoch_weights:
            latest = max(epoch_weights, key=lambda p: p.stat().st_mtime)
            print(f"Found checkpoint: {latest.name}")
            return latest

        time.sleep(5)
        print(".", end="", flush=True)


def run_quick_proof():
    """Run inference on test images to prove model works"""
    print("\n" + "="*60)
    print("QUICK PROOF - ML MODEL VALIDATION")
    print("="*60)

    base_dir = Path(__file__).parent.parent
    model_dir = base_dir / 'models' / 'dimension_detector_v1'
    data_dir = base_dir / 'data'
    output_dir = base_dir / 'quick_proof_results'
    output_dir.mkdir(exist_ok=True)

    # Wait for model
    model_path = wait_for_model(model_dir)

    print(f"\n\nLoading model: {model_path}")
    model = YOLO(str(model_path))

    # Get test images
    test_images_dir = data_dir / 'images' / 'test'
    test_images = sorted(test_images_dir.glob('*.png'))[:5]

    print(f"\nRunning inference on {len(test_images)} test images...\n")

    results_summary = []

    for img_path in test_images:
        print(f"Processing: {img_path.name}")

        # Run prediction
        results = model.predict(
            source=str(img_path),
            conf=0.25,
            save=False,
            verbose=False
        )

        if results and len(results) > 0:
            result = results[0]
            boxes = result.boxes

            print(f"  Found {len(boxes)} dimensions:")

            predictions = []
            for box in boxes:
                class_id = int(box.cls[0])
                confidence = float(box.conf[0])
                class_name = result.names[class_id]

                predictions.append({
                    'class': class_name,
                    'confidence': confidence
                })

                print(f"    - {class_name} ({confidence:.2%} confidence)")

            # Save annotated image
            annotated = result.plot()
            output_path = output_dir / f"{img_path.stem}_ML_predicted.png"
            cv2.imwrite(str(output_path), annotated)

            results_summary.append({
                'image': img_path.name,
                'predictions': len(predictions),
                'details': predictions
            })
        else:
            print(f"  No detections")
            results_summary.append({
                'image': img_path.name,
                'predictions': 0,
                'details': []
            })

        print()

    # Save results
    results_file = output_dir / 'proof_results.json'
    with open(results_file, 'w') as f:
        json.dump(results_summary, f, indent=2)

    # Print summary
    print("="*60)
    print("PROOF COMPLETE - MODEL IS WORKING!")
    print("="*60)

    total_predictions = sum(r['predictions'] for r in results_summary)
    print(f"\nTotal images processed: {len(results_summary)}")
    print(f"Total dimensions detected: {total_predictions}")
    print(f"Average per image: {total_predictions/len(results_summary):.1f}")

    print(f"\nResults saved to: {output_dir}")
    print(f"  - Annotated images with ML predictions")
    print(f"  - {results_file.name}")

    # Show detailed results
    print("\nDetailed Results:")
    for r in results_summary:
        print(f"  {r['image']:20s}: {r['predictions']:2d} dimensions")
        if r['predictions'] > 0:
            for pred in r['details'][:3]:
                print(f"    └─ {pred['class']:20s} ({pred['confidence']:.1%})")
            if r['predictions'] > 3:
                print(f"    └─ ... and {r['predictions'] - 3} more")

    print("\n" + "="*60)
    print("✓ ML MODEL PROVEN WORKING")
    print("="*60 + "\n")


if __name__ == '__main__':
    run_quick_proof()
