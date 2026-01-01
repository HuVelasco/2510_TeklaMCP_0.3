#!/usr/bin/env python3
"""
YOLOv8 Training Script for VirtualDetailer ML
Trains dimension detection model on Tekla shop drawings
"""
from pathlib import Path
from ultralytics import YOLO
import torch


def train_model():
    """Train YOLOv8 model for dimension detection"""
    print("\n" + "="*60)
    print("YOLO TRAINING - VIRTUALDETAILER ML")
    print("="*60 + "\n")

    # Check for GPU
    device = 'cuda' if torch.cuda.is_available() else 'cpu'
    print(f"Device: {device}")
    if device == 'cpu':
        print("WARNING: Training on CPU will be slow. GPU recommended.\n")

    # Paths
    data_yaml = Path(__file__).parent.parent / 'data' / 'dataset.yaml'
    model_dir = Path(__file__).parent.parent / 'models'
    model_dir.mkdir(exist_ok=True)

    # Initialize YOLOv8 nano model (fastest for quick training)
    print("Initializing YOLOv8 Nano model...")
    model = YOLO('yolov8n.pt')  # Nano model - fastest

    # Training configuration
    print("\nTraining Configuration:")
    print(f"  Data:        {data_yaml}")
    print(f"  Model:       YOLOv8 Nano")
    print(f"  Image size:  1024x1024")
    print(f"  Batch size:  4 (small for CPU compatibility)")
    print(f"  Epochs:      50 (quick baseline)")
    print(f"  Device:      {device}")
    print()

    # Train the model
    print("Starting training...")
    print("-"*60)

    results = model.train(
        data=str(data_yaml),
        epochs=50,              # Quick training for demonstration
        imgsz=1024,             # Match our dataset
        batch=4,                # Small batch for CPU
        device=device,
        project=str(model_dir),
        name='dimension_detector_v1',
        exist_ok=True,
        patience=10,            # Early stopping
        save=True,
        save_period=10,         # Save checkpoint every 10 epochs
        plots=True,             # Generate plots
        verbose=True
    )

    print("\n" + "="*60)
    print("TRAINING COMPLETE")
    print("="*60)

    # Print results
    best_model = model_dir / 'dimension_detector_v1' / 'weights' / 'best.pt'
    last_model = model_dir / 'dimension_detector_v1' / 'weights' / 'last.pt'

    print(f"\nModel saved:")
    print(f"  Best:  {best_model}")
    print(f"  Last:  {last_model}")

    print(f"\nTraining metrics:")
    print(f"  mAP50:     {results.results_dict.get('metrics/mAP50(B)', 'N/A')}")
    print(f"  mAP50-95:  {results.results_dict.get('metrics/mAP50-95(B)', 'N/A')}")
    print(f"  Precision: {results.results_dict.get('metrics/precision(B)', 'N/A')}")
    print(f"  Recall:    {results.results_dict.get('metrics/recall(B)', 'N/A')}")

    print("\n" + "="*60)
    print("Next: Run inference on test drawings")
    print("  python src/inference.py")
    print("="*60 + "\n")

    return best_model


if __name__ == '__main__':
    train_model()
