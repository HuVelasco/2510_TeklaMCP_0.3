#!/usr/bin/env python3
"""
Model Validation and Proof Script
Runs trained model on test set and generates comprehensive proof
"""
import json
import cv2
import numpy as np
from pathlib import Path
from ultralytics import YOLO
import matplotlib.pyplot as plt


class ModelValidator:
    """Validates trained model and generates proof"""

    def __init__(self, model_path: Path, data_dir: Path):
        """
        Initialize validator

        Args:
            model_path: Path to trained model weights
            data_dir: Path to data directory
        """
        self.model_path = model_path
        self.data_dir = data_dir
        self.model = None
        self.results = []

        # Load model
        if model_path.exists():
            print(f"Loading model: {model_path}")
            self.model = YOLO(str(model_path))
        else:
            raise FileNotFoundError(f"Model not found: {model_path}")

    def validate_on_test_set(self):
        """Run validation on test set"""
        print("\n" + "="*60)
        print("VALIDATING MODEL ON TEST SET")
        print("="*60 + "\n")

        # Run validation
        metrics = self.model.val(
            data=str(self.data_dir / 'dataset.yaml'),
            split='test',
            save_json=True,
            save_hybrid=True
        )

        print("\nValidation Metrics:")
        print(f"  mAP50:      {metrics.box.map50:.3f}")
        print(f"  mAP50-95:   {metrics.box.map:.3f}")
        print(f"  Precision:  {metrics.box.mp:.3f}")
        print(f"  Recall:     {metrics.box.mr:.3f}")

        return metrics

    def predict_on_images(self, image_paths: list, save_dir: Path):
        """
        Run predictions on specific images

        Args:
            image_paths: List of image paths
            save_dir: Directory to save results

        Returns:
            List of results
        """
        save_dir.mkdir(exist_ok=True)
        results = []

        print("\n" + "="*60)
        print("RUNNING PREDICTIONS")
        print("="*60 + "\n")

        for img_path in image_paths:
            print(f"Processing: {img_path.name}")

            # Run prediction
            pred_results = self.model.predict(
                source=str(img_path),
                conf=0.25,  # Confidence threshold
                save=False,
                verbose=False
            )

            # Extract predictions
            if pred_results and len(pred_results) > 0:
                result = pred_results[0]
                boxes = result.boxes

                predictions = []
                for box in boxes:
                    predictions.append({
                        'bbox': box.xyxy[0].cpu().numpy().tolist(),
                        'confidence': float(box.conf[0]),
                        'class': int(box.cls[0]),
                        'class_name': result.names[int(box.cls[0])]
                    })

                print(f"  Found {len(predictions)} dimensions")

                # Save annotated image
                annotated = result.plot()
                output_path = save_dir / f"{img_path.stem}_predicted.png"
                cv2.imwrite(str(output_path), annotated)
                print(f"  Saved: {output_path.name}")

                results.append({
                    'image': img_path.name,
                    'predictions': predictions,
                    'count': len(predictions)
                })
            else:
                print(f"  No predictions")
                results.append({
                    'image': img_path.name,
                    'predictions': [],
                    'count': 0
                })

        return results

    def compare_with_ground_truth(self, image_paths: list, labels_dir: Path):
        """
        Compare predictions with ground truth labels

        Args:
            image_paths: List of image paths
            labels_dir: Directory containing ground truth labels

        Returns:
            Comparison results
        """
        print("\n" + "="*60)
        print("COMPARING WITH GROUND TRUTH")
        print("="*60 + "\n")

        comparisons = []

        for img_path in image_paths:
            # Get ground truth
            label_path = labels_dir / f"{img_path.stem}.txt"

            gt_count = 0
            if label_path.exists():
                with open(label_path, 'r') as f:
                    gt_count = len(f.readlines())

            # Get predictions
            pred_results = self.model.predict(
                source=str(img_path),
                conf=0.25,
                verbose=False
            )

            pred_count = 0
            if pred_results and len(pred_results) > 0:
                pred_count = len(pred_results[0].boxes)

            comparison = {
                'image': img_path.name,
                'ground_truth': gt_count,
                'predicted': pred_count,
                'difference': pred_count - gt_count
            }

            comparisons.append(comparison)

            print(f"{img_path.name:20s}: GT={gt_count:2d}, Pred={pred_count:2d}, Diff={comparison['difference']:+3d}")

        return comparisons

    def generate_report(self, metrics, predictions, comparisons, output_file: Path):
        """Generate comprehensive validation report"""
        report = {
            'validation_metrics': {
                'mAP50': float(metrics.box.map50),
                'mAP50-95': float(metrics.box.map),
                'precision': float(metrics.box.mp),
                'recall': float(metrics.box.mr)
            },
            'predictions': predictions,
            'comparisons': comparisons,
            'summary': {
                'total_images': len(comparisons),
                'total_gt_dimensions': sum(c['ground_truth'] for c in comparisons),
                'total_pred_dimensions': sum(c['predicted'] for c in comparisons),
                'avg_gt_per_image': sum(c['ground_truth'] for c in comparisons) / len(comparisons) if comparisons else 0,
                'avg_pred_per_image': sum(c['predicted'] for c in comparisons) / len(comparisons) if comparisons else 0
            }
        }

        with open(output_file, 'w') as f:
            json.dump(report, f, indent=2)

        print(f"\nReport saved: {output_file}")
        return report


def main():
    """Main validation pipeline"""
    print("\n" + "="*60)
    print("MODEL VALIDATION - PROOF OF CONCEPT")
    print("="*60)

    # Paths
    base_dir = Path(__file__).parent.parent
    model_path = base_dir / 'models' / 'dimension_detector_v1' / 'weights' / 'best.pt'
    data_dir = base_dir / 'data'
    test_images_dir = data_dir / 'images' / 'test'
    test_labels_dir = data_dir / 'labels' / 'test'
    output_dir = base_dir / 'validation_results'
    output_dir.mkdir(exist_ok=True)

    # Check if model exists
    if not model_path.exists():
        print(f"\nError: Model not found at {model_path}")
        print("Train the model first: python src/train.py")
        return

    # Initialize validator
    validator = ModelValidator(model_path, data_dir)

    # 1. Validate on test set
    metrics = validator.validate_on_test_set()

    # 2. Get test images (first 10)
    test_images = sorted(test_images_dir.glob('*.png'))[:10]
    print(f"\nSelected {len(test_images)} test images for detailed analysis")

    # 3. Run predictions
    predictions = validator.predict_on_images(test_images, output_dir)

    # 4. Compare with ground truth
    comparisons = validator.compare_with_ground_truth(test_images, test_labels_dir)

    # 5. Generate report
    report_file = output_dir / 'validation_report.json'
    report = validator.generate_report(metrics, predictions, comparisons, report_file)

    # 6. Print summary
    print("\n" + "="*60)
    print("VALIDATION SUMMARY")
    print("="*60)
    print(f"\nModel Performance:")
    print(f"  mAP@0.5:    {report['validation_metrics']['mAP50']:.1%}")
    print(f"  mAP@0.5-0.95: {report['validation_metrics']['mAP50-95']:.1%}")
    print(f"  Precision:  {report['validation_metrics']['precision']:.1%}")
    print(f"  Recall:     {report['validation_metrics']['recall']:.1%}")

    print(f"\nPrediction Analysis:")
    print(f"  Images tested:       {report['summary']['total_images']}")
    print(f"  Total GT dimensions: {report['summary']['total_gt_dimensions']}")
    print(f"  Total predictions:   {report['summary']['total_pred_dimensions']}")
    print(f"  Avg per image (GT):  {report['summary']['avg_gt_per_image']:.1f}")
    print(f"  Avg per image (Pred): {report['summary']['avg_pred_per_image']:.1f}")

    print(f"\nResults saved to: {output_dir}")
    print("  - Annotated images with predictions")
    print("  - validation_report.json")

    print("\n" + "="*60)
    print("MODEL VALIDATION COMPLETE!")
    print("="*60 + "\n")


if __name__ == '__main__':
    main()
