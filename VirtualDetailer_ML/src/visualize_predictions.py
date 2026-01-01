#!/usr/bin/env python3
"""
Visual Comparison: Ground Truth vs Model Predictions
"""
import cv2
import numpy as np
from pathlib import Path
from ultralytics import YOLO
import matplotlib.pyplot as plt


def load_yolo_annotations(label_file: Path, img_width: int, img_height: int):
    """Load YOLO format annotations and convert to pixel coordinates"""
    annotations = []

    if not label_file.exists():
        return annotations

    with open(label_file, 'r') as f:
        for line in f:
            parts = line.strip().split()
            if len(parts) >= 5:
                class_id = int(parts[0])
                center_x = float(parts[1]) * img_width
                center_y = float(parts[2]) * img_height
                width = float(parts[3]) * img_width
                height = float(parts[4]) * img_height

                # Convert to corner coordinates
                x1 = int(center_x - width/2)
                y1 = int(center_y - height/2)
                x2 = int(center_x + width/2)
                y2 = int(center_y + height/2)

                annotations.append({
                    'class_id': class_id,
                    'bbox': (x1, y1, x2, y2)
                })

    return annotations


def draw_boxes(img, boxes, color, label_prefix="", thickness=2):
    """Draw bounding boxes on image"""
    img_copy = img.copy()

    for i, box in enumerate(boxes):
        x1, y1, x2, y2 = box['bbox']
        cv2.rectangle(img_copy, (x1, y1), (x2, y2), color, thickness)

        # Add label
        label = f"{label_prefix}{i+1}"
        cv2.putText(img_copy, label, (x1, y1-5),
                   cv2.FONT_HERSHEY_SIMPLEX, 0.5, color, 1)

    return img_copy


def create_comparison():
    """Create side-by-side comparison of ground truth vs predictions"""
    print("\n" + "="*60)
    print("VISUAL COMPARISON: GROUND TRUTH vs PREDICTIONS")
    print("="*60 + "\n")

    base_dir = Path(__file__).parent.parent
    model_path = base_dir / 'models' / 'dimension_detector_v1' / 'weights' / 'best.pt'
    data_dir = base_dir / 'data'
    test_images_dir = data_dir / 'images' / 'test'
    test_labels_dir = data_dir / 'labels' / 'test'
    output_dir = base_dir / 'comparison_results'
    output_dir.mkdir(exist_ok=True)

    # Class names
    class_names = {
        0: "StraightDimension",
        1: "AngleDimension",
        2: "RadiusDimension",
        3: "DiameterDimension",
        4: "ArcLengthDimension"
    }

    print(f"Loading model: {model_path}")
    model = YOLO(str(model_path))

    # Get first 5 test images
    test_images = sorted(test_images_dir.glob('*.png'))[:5]
    print(f"Processing {len(test_images)} test images\n")

    comparison_stats = []

    for img_path in test_images:
        print(f"Processing: {img_path.name}")

        # Load image
        img = cv2.imread(str(img_path))
        h, w = img.shape[:2]

        # Load ground truth
        label_path = test_labels_dir / f"{img_path.stem}.txt"
        gt_boxes = load_yolo_annotations(label_path, w, h)

        # Get predictions at conf=0.01 (reasonable threshold from diagnosis)
        results = model.predict(
            source=str(img_path),
            conf=0.01,
            save=False,
            verbose=False
        )

        pred_boxes = []
        if results and len(results) > 0:
            result = results[0]
            boxes = result.boxes

            for box in boxes:
                bbox = box.xyxy[0].cpu().numpy()
                pred_boxes.append({
                    'class_id': int(box.cls[0]),
                    'confidence': float(box.conf[0]),
                    'bbox': tuple(map(int, bbox))
                })

        # Create visualizations
        # 1. Original image
        img_orig = img.copy()

        # 2. Ground truth (green boxes)
        img_gt = draw_boxes(img, gt_boxes, (0, 255, 0), "GT", thickness=2)

        # 3. Predictions (blue boxes)
        img_pred = draw_boxes(img, pred_boxes, (255, 0, 0), "P", thickness=2)

        # 4. Combined (green=GT, blue=Pred)
        img_combined = img.copy()
        img_combined = draw_boxes(img_combined, gt_boxes, (0, 255, 0), "GT", thickness=2)
        img_combined = draw_boxes(img_combined, pred_boxes, (255, 0, 0), "P", thickness=1)

        # Create 2x2 grid
        top_row = np.hstack([img_orig, img_gt])
        bottom_row = np.hstack([img_pred, img_combined])
        grid = np.vstack([top_row, bottom_row])

        # Add labels to grid
        label_height = 30
        labeled_grid = np.zeros((grid.shape[0] + label_height, grid.shape[1], 3), dtype=np.uint8)
        labeled_grid[label_height:, :] = grid

        # Add title
        title = f"{img_path.stem} | GT: {len(gt_boxes)} | Pred: {len(pred_boxes)} (conf>0.01)"
        cv2.putText(labeled_grid, title, (10, 20),
                   cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255, 255, 255), 2)

        # Save comparison
        output_path = output_dir / f"{img_path.stem}_comparison.png"
        cv2.imwrite(str(output_path), labeled_grid)

        # Statistics
        print(f"  Ground Truth: {len(gt_boxes):3d} dimensions")
        print(f"  Predictions:  {len(pred_boxes):3d} dimensions (max conf: {max([b['confidence'] for b in pred_boxes], default=0.0):.4f})")
        print(f"  Saved: {output_path.name}\n")

        comparison_stats.append({
            'image': img_path.name,
            'ground_truth': len(gt_boxes),
            'predictions': len(pred_boxes),
            'max_confidence': max([b['confidence'] for b in pred_boxes], default=0.0)
        })

    # Summary
    print("="*60)
    print("COMPARISON SUMMARY")
    print("="*60)

    total_gt = sum(s['ground_truth'] for s in comparison_stats)
    total_pred = sum(s['predictions'] for s in comparison_stats)

    print(f"\nTotal Ground Truth dimensions: {total_gt}")
    print(f"Total Predicted dimensions:     {total_pred}")
    print(f"Prediction Rate:                {total_pred/total_gt*100:.1f}% (at conf>0.01)")

    print(f"\nPer-image breakdown:")
    for stat in comparison_stats:
        print(f"  {stat['image']:20s}: GT={stat['ground_truth']:2d}, Pred={stat['predictions']:3d}, MaxConf={stat['max_confidence']:.4f}")

    print(f"\nComparison images saved to: {output_dir}")
    print("  Layout: [Original] [Ground Truth]")
    print("          [Predictions] [Combined]")
    print("  Colors: Green=Ground Truth, Blue=Predictions")

    print("\n" + "="*60)
    print("KEY FINDINGS")
    print("="*60)
    print(f"\n1. Model IS detecting dimensions (avg {total_pred/len(comparison_stats):.1f} per image)")
    print(f"2. Confidence is LOW (max {max(s['max_confidence'] for s in comparison_stats):.2%})")
    print(f"3. Detection rate: {total_pred/total_gt*100:.1f}% of ground truth")
    print(f"\nThis suggests:")
    print("  - Model has learned some dimension patterns")
    print("  - Needs more training to improve confidence")
    print("  - Consider: longer training, larger model, data augmentation")

    print("\n" + "="*60)


if __name__ == '__main__':
    create_comparison()
