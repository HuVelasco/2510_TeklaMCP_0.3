#!/usr/bin/env python3
"""
Visualize YOLO Annotations on Images
Shows dimension bounding boxes overlaid on rasterized drawings
"""
import cv2
import numpy as np
from pathlib import Path


def visualize_annotations(image_path: Path, label_path: Path, output_path: Path):
    """
    Draw YOLO annotations on image

    Args:
        image_path: Path to image file
        label_path: Path to YOLO annotation file
        output_path: Path to save annotated image
    """
    # Load image
    image = cv2.imread(str(image_path))
    if image is None:
        print(f"Error: Could not load image {image_path}")
        return

    h, w = image.shape[:2]

    # Load annotations
    if not label_path.exists():
        print(f"No annotations for {image_path.name}")
        return

    with open(label_path, 'r') as f:
        annotations = f.readlines()

    # Draw each annotation
    for ann in annotations:
        parts = ann.strip().split()
        if len(parts) < 5:
            continue

        class_id = int(parts[0])
        center_x = float(parts[1])
        center_y = float(parts[2])
        bbox_w = float(parts[3])
        bbox_h = float(parts[4])

        # Convert normalized to pixel coordinates
        x1 = int((center_x - bbox_w/2) * w)
        y1 = int((center_y - bbox_h/2) * h)
        x2 = int((center_x + bbox_w/2) * w)
        y2 = int((center_y + bbox_h/2) * h)

        # Draw bounding box (red for dimensions)
        cv2.rectangle(image, (x1, y1), (x2, y2), (0, 0, 255), 2)

        # Draw center point
        center_px = int(center_x * w)
        center_py = int(center_y * h)
        cv2.circle(image, (center_px, center_py), 3, (0, 255, 0), -1)

    # Save annotated image
    cv2.imwrite(str(output_path), image)
    print(f"Saved: {output_path.name} ({len(annotations)} dimensions)")


def main():
    """Visualize annotations for sample drawings"""
    print("\n" + "="*60)
    print("VISUALIZING YOLO ANNOTATIONS")
    print("="*60 + "\n")

    data_dir = Path(__file__).parent.parent / 'data'
    images_dir = data_dir / 'images' / 'train'
    labels_dir = data_dir / 'labels' / 'train'
    output_dir = data_dir / 'annotated_samples'
    output_dir.mkdir(exist_ok=True)

    # Get first 5 images
    image_files = sorted(images_dir.glob('*.png'))[:5]

    for img_path in image_files:
        label_path = labels_dir / f"{img_path.stem}.txt"
        output_path = output_dir / f"{img_path.stem}_annotated.png"

        visualize_annotations(img_path, label_path, output_path)

    print(f"\n{'='*60}")
    print(f"Annotated images saved to: {output_dir}")
    print(f"{'='*60}\n")


if __name__ == '__main__':
    main()
