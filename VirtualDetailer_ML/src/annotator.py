#!/usr/bin/env python3
"""
YOLO Annotator for VirtualDetailer ML
Extracts dimension locations and converts to YOLO format annotations
"""
import numpy as np
from typing import Dict, List, Tuple, Optional
from pathlib import Path
import json


class YOLOAnnotator:
    """Generates YOLO format annotations from drawing data"""

    # Dimension type to class mapping
    DIMENSION_CLASSES = {
        'StraightDimension': 0,
        'AngleDimension': 1,
        'RadiusDimension': 2,
        'DiameterDimension': 3,
        'ArcLengthDimension': 4
    }

    def __init__(self, image_size: int = 1024):
        """
        Initialize annotator

        Args:
            image_size: Image size (must match rasterizer)
        """
        self.image_size = image_size

    def generate_annotations(self,
                             drawing: Dict,
                             transform_metadata: Dict) -> List[Dict]:
        """
        Generate YOLO annotations for a drawing

        Args:
            drawing: Drawing data from JSON
            transform_metadata: Transform info from rasterizer

        Returns:
            List of annotation dicts with:
            - class_id: Dimension type
            - bbox: [center_x, center_y, width, height] normalized
            - confidence: 1.0 (ground truth)
        """
        annotations = []

        # Extract dimension objects
        dimensions = self._extract_dimensions(drawing)

        if not dimensions:
            return annotations

        # Get transform parameters
        scale = transform_metadata.get('scale', 1.0)
        offset = transform_metadata.get('offset', (0, 0))

        # Process each dimension
        for dim in dimensions:
            annotation = self._dimension_to_yolo(dim, scale, offset)
            if annotation:
                annotations.append(annotation)

        return annotations

    def _extract_dimensions(self, drawing: Dict) -> List[Dict]:
        """Extract all dimension objects from drawing"""
        dimensions = []

        for obj in drawing.get('objects', []):
            obj_type = obj.get('type_name', '')

            if 'Dimension' in obj_type:
                # Try geometric_data first
                geom = obj.get('geometric_data', {})
                dim_points = geom.get('dimension_points', [])

                # If no points in geometric_data, extract from properties
                if not dim_points:
                    props = obj.get('properties', {})
                    dim_points = self._extract_points_from_properties(props)

                if dim_points:
                    dimensions.append({
                        'type': obj_type,
                        'points': dim_points,
                        'id': obj.get('id', 'unknown')
                    })

        return dimensions

    def _extract_points_from_properties(self, properties: Dict) -> List[Dict]:
        """Extract dimension points from properties (StartPoint, EndPoint, etc.)"""
        points = []

        # Common point fields in dimension properties
        point_fields = ['StartPoint', 'EndPoint', 'Point', 'CenterPoint', 'MidPoint']

        for field in point_fields:
            point_str = properties.get(field, '')

            if point_str:
                # Parse point string like "(-54.372, -138.113, 0.000)"
                point = self._parse_point_string(point_str)
                if point:
                    points.append(point)

        return points

    def _parse_point_string(self, point_str: str) -> Optional[Dict]:
        """
        Parse point string like "(-54.372, -138.113, 0.000)" to dict

        Args:
            point_str: Point string from properties

        Returns:
            Dict with x, y, z or None if invalid
        """
        if not isinstance(point_str, str):
            return None

        # Remove parentheses and split by comma
        clean_str = point_str.strip().strip('()').replace(' ', '')
        parts = clean_str.split(',')

        if len(parts) >= 2:
            try:
                x = float(parts[0])
                y = float(parts[1])
                z = float(parts[2]) if len(parts) > 2 else 0.0

                return {'x': x, 'y': y, 'z': z}
            except (ValueError, IndexError):
                return None

        return None

    def _dimension_to_yolo(self,
                           dimension: Dict,
                           scale: float,
                           offset: Tuple[float, float]) -> Optional[Dict]:
        """
        Convert a dimension to YOLO format annotation

        Args:
            dimension: Dimension data
            scale: Scaling factor
            offset: (offset_x, offset_y)

        Returns:
            Annotation dict or None if invalid
        """
        dim_type = dimension['type']
        points = dimension['points']

        # Get class ID
        class_id = self.DIMENSION_CLASSES.get(dim_type, 0)

        if not points:
            return None

        # Convert points to image coordinates
        img_points = []
        for pt in points:
            if isinstance(pt, dict) and 'x' in pt and 'y' in pt:
                x = pt['x'] * scale + offset[0]
                y = pt['y'] * scale + offset[1]

                # Flip Y axis
                y = self.image_size - y

                img_points.append([x, y])

        if not img_points:
            return None

        # Calculate bounding box
        xs = [p[0] for p in img_points]
        ys = [p[1] for p in img_points]

        min_x = min(xs)
        max_x = max(xs)
        min_y = min(ys)
        max_y = max(ys)

        # Add padding around dimension (dimensions are typically small)
        padding = 20  # pixels
        min_x = max(0, min_x - padding)
        min_y = max(0, min_y - padding)
        max_x = min(self.image_size, max_x + padding)
        max_y = min(self.image_size, max_y + padding)

        # Calculate center and size
        width = max_x - min_x
        height = max_y - min_y
        center_x = (min_x + max_x) / 2
        center_y = (min_y + max_y) / 2

        # Normalize to [0, 1]
        norm_center_x = center_x / self.image_size
        norm_center_y = center_y / self.image_size
        norm_width = width / self.image_size
        norm_height = height / self.image_size

        # Validate bounds
        if not (0 <= norm_center_x <= 1 and 0 <= norm_center_y <= 1):
            return None

        if norm_width <= 0 or norm_height <= 0:
            return None

        return {
            'class_id': class_id,
            'bbox': [norm_center_x, norm_center_y, norm_width, norm_height],
            'confidence': 1.0,
            'type': dim_type,
            'point_count': len(img_points)
        }

    def save_yolo_annotation(self,
                             annotations: List[Dict],
                             output_path: Path):
        """
        Save annotations in YOLO format (.txt file)

        Args:
            annotations: List of annotation dicts
            output_path: Path to save .txt file

        YOLO format: class_id center_x center_y width height
        """
        with open(output_path, 'w') as f:
            for ann in annotations:
                class_id = ann['class_id']
                bbox = ann['bbox']

                # Write: class_id center_x center_y width height
                f.write(f"{class_id} {bbox[0]:.6f} {bbox[1]:.6f} {bbox[2]:.6f} {bbox[3]:.6f}\n")

    def create_class_file(self, output_path: Path):
        """
        Create classes.txt file with dimension type names

        Args:
            output_path: Path to save classes.txt
        """
        # Sort by class ID
        classes = sorted(self.DIMENSION_CLASSES.items(), key=lambda x: x[1])

        with open(output_path, 'w') as f:
            for class_name, class_id in classes:
                f.write(f"{class_name}\n")


def test_annotator():
    """Test annotator on sample drawings"""
    print("\n" + "="*60)
    print("TESTING YOLO ANNOTATOR")
    print("="*60 + "\n")

    # Load a sample drawing
    data_dir = Path(__file__).parent.parent / 'data/processed'
    train_file = data_dir / 'train.json'

    if not train_file.exists():
        print(f"Error: {train_file} not found")
        return

    with open(train_file, 'r', encoding='utf-8') as f:
        train_data = json.load(f)

    if not train_data:
        print("Error: No training data found")
        return

    # Create output directory
    output_dir = Path(__file__).parent.parent / 'data/images/test/labels'
    output_dir.mkdir(parents=True, exist_ok=True)

    # Create annotator
    annotator = YOLOAnnotator(image_size=1024)

    # Create classes file
    classes_path = output_dir.parent / 'classes.txt'
    annotator.create_class_file(classes_path)
    print(f"Created classes file: {classes_path}\n")

    # Mock transform metadata (in real usage, this comes from rasterizer)
    mock_metadata = {
        'scale': 4.0,
        'offset': (50, 50)
    }

    total_annotations = 0

    for i, drawing in enumerate(train_data[:5]):
        mark = drawing.get('mark', f'drawing_{i}').strip('[]')

        # Generate annotations
        annotations = annotator.generate_annotations(drawing, mock_metadata)

        # Save to file
        output_path = output_dir / f"{mark}.txt"
        annotator.save_yolo_annotation(annotations, output_path)

        print(f"{mark:20s}: {len(annotations):3d} dimensions annotated")
        total_annotations += len(annotations)

        # Show breakdown
        if annotations:
            type_counts = {}
            for ann in annotations:
                dim_type = ann['type']
                type_counts[dim_type] = type_counts.get(dim_type, 0) + 1

            for dim_type, count in sorted(type_counts.items()):
                print(f"  {dim_type:25s}: {count:3d}")

        print()

    print("="*60)
    print(f"Total annotations: {total_annotations}")
    print(f"Annotations saved to: {output_dir}")
    print("="*60 + "\n")


if __name__ == '__main__':
    test_annotator()
