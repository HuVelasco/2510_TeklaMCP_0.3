#!/usr/bin/env python3
"""
Diagnostic Script - Inspect model predictions at various confidence levels
"""
import json
import cv2
import numpy as np
from pathlib import Path
from ultralytics import YOLO


def diagnose_model_predictions():
    """Diagnose what the model is predicting at different confidence thresholds"""
    print("\n" + "="*60)
    print("MODEL PREDICTION DIAGNOSIS")
    print("="*60 + "\n")

    base_dir = Path(__file__).parent.parent
    model_path = base_dir / 'models' / 'dimension_detector_v1' / 'weights' / 'best.pt'
    data_dir = base_dir / 'data'
    test_images_dir = data_dir / 'images' / 'test'
    output_dir = base_dir / 'diagnosis_results'
    output_dir.mkdir(exist_ok=True)

    # Check if model exists
    if not model_path.exists():
        print(f"Error: Model not found at {model_path}")
        return

    print(f"Loading model: {model_path}")
    model = YOLO(str(model_path))

    # Get first 5 test images
    test_images = sorted(test_images_dir.glob('*.png'))[:5]
    print(f"Testing on {len(test_images)} images\n")

    # Test different confidence thresholds
    confidence_thresholds = [0.001, 0.01, 0.05, 0.1, 0.25, 0.5]

    results_summary = []

    for img_path in test_images:
        print(f"\n{'='*60}")
        print(f"Image: {img_path.name}")
        print('='*60)

        img_results = {'image': img_path.name, 'thresholds': {}}

        for conf_thresh in confidence_thresholds:
            # Run prediction
            results = model.predict(
                source=str(img_path),
                conf=conf_thresh,
                save=False,
                verbose=False
            )

            if results and len(results) > 0:
                result = results[0]
                boxes = result.boxes

                detections = []
                for box in boxes:
                    class_id = int(box.cls[0])
                    confidence = float(box.conf[0])
                    class_name = result.names[class_id]

                    detections.append({
                        'class': class_name,
                        'confidence': confidence,
                        'bbox': box.xyxy[0].cpu().numpy().tolist()
                    })

                print(f"  conf={conf_thresh:.3f}: {len(detections):3d} detections", end="")

                if detections:
                    max_conf = max(d['confidence'] for d in detections)
                    print(f" (max conf: {max_conf:.4f})")

                    # Show top 3 detections
                    sorted_dets = sorted(detections, key=lambda x: x['confidence'], reverse=True)
                    for i, det in enumerate(sorted_dets[:3]):
                        print(f"      #{i+1}: {det['class']:20s} conf={det['confidence']:.4f}")
                else:
                    print()

                img_results['thresholds'][conf_thresh] = {
                    'count': len(detections),
                    'detections': detections
                }

                # Save annotated image for lowest threshold that has detections
                if conf_thresh == 0.001 and len(detections) > 0:
                    annotated = result.plot()
                    output_path = output_dir / f"{img_path.stem}_conf{conf_thresh}_annotated.png"
                    cv2.imwrite(str(output_path), annotated)
            else:
                print(f"  conf={conf_thresh:.3f}: 0 detections")
                img_results['thresholds'][conf_thresh] = {'count': 0, 'detections': []}

        results_summary.append(img_results)

    # Save detailed results
    results_file = output_dir / 'diagnosis_results.json'
    with open(results_file, 'w') as f:
        json.dump(results_summary, f, indent=2)

    # Print summary
    print("\n" + "="*60)
    print("DIAGNOSIS SUMMARY")
    print("="*60)

    for img_result in results_summary:
        print(f"\n{img_result['image']}:")
        for conf_thresh in confidence_thresholds:
            count = img_result['thresholds'][conf_thresh]['count']
            if count > 0:
                dets = img_result['thresholds'][conf_thresh]['detections']
                max_conf = max(d['confidence'] for d in dets)
                print(f"  conf={conf_thresh:.3f}: {count:3d} detections (max: {max_conf:.4f})")

    # Overall statistics
    print("\n" + "="*60)
    print("OVERALL STATISTICS")
    print("="*60)

    for conf_thresh in confidence_thresholds:
        total_dets = sum(
            img['thresholds'][conf_thresh]['count']
            for img in results_summary
        )
        images_with_dets = sum(
            1 for img in results_summary
            if img['thresholds'][conf_thresh]['count'] > 0
        )

        print(f"conf={conf_thresh:.3f}: {total_dets:4d} total detections across {images_with_dets}/{len(test_images)} images")

    print(f"\nResults saved to: {output_dir}")
    print(f"  - {results_file.name}")
    print(f"  - Annotated images (if detections found)")

    print("\n" + "="*60)
    print("DIAGNOSIS COMPLETE")
    print("="*60 + "\n")


if __name__ == '__main__':
    diagnose_model_predictions()
