#!/usr/bin/env python3
"""
Drawing Rasterizer for VirtualDetailer ML
Converts Tekla drawing geometry (parts, vertices) into 2D images for training
"""
import numpy as np
import cv2
from typing import Dict, List, Tuple, Optional
from pathlib import Path
import json
from coordinate_transform import CoordinateTransformer


class DrawingRasterizer:
    """Converts drawing JSON data to 2D images"""

    def __init__(self, image_size: int = 1024, margin: int = 50):
        """
        Initialize rasterizer

        Args:
            image_size: Output image size (square)
            margin: Margin around drawing in pixels
        """
        self.image_size = image_size
        self.margin = margin
        self.drawing_size = image_size - 2 * margin

    def rasterize_drawing(self, drawing: Dict) -> Tuple[np.ndarray, Dict]:
        """
        Convert a drawing to a 2D image

        Args:
            drawing: Drawing data from JSON

        Returns:
            Tuple of (image, metadata)
            - image: numpy array (H, W, 3) BGR format
            - metadata: Transform info, bounds, etc.
        """
        # Create blank white canvas
        image = np.ones((self.image_size, self.image_size, 3), dtype=np.uint8) * 255

        # Get primary view for coordinate transformation
        views = drawing.get('views', [])
        if not views:
            print(f"Warning: No views found in drawing {drawing.get('mark', 'unknown')}")
            return image, {'error': 'no_views'}

        # Use first view (typically the main view)
        view = views[0]
        transformer = CoordinateTransformer(view)

        # Extract parts and transform to 2D
        parts = self._extract_and_transform_parts(drawing, transformer)

        if not parts:
            print(f"Warning: No parts found in drawing {drawing.get('mark', 'unknown')}")
            return image, {'error': 'no_parts'}

        # Calculate bounding box for all parts (now in 2D drawing space)
        bbox = self._calculate_global_bbox(parts)

        if bbox is None:
            print(f"Warning: Could not calculate bounding box for {drawing.get('mark', 'unknown')}")
            return image, {'error': 'no_bbox'}

        # Calculate scaling to fit drawing in image
        scale, offset = self._calculate_transform(bbox)

        # Draw each part
        for part in parts:
            self._draw_part(image, part, scale, offset)

        metadata = {
            'bbox': bbox,
            'scale': scale,
            'offset': offset,
            'part_count': len(parts),
            'drawing_mark': drawing.get('mark', 'unknown'),
            'drawing_type': drawing.get('drawing_type', 'unknown')
        }

        return image, metadata

    def _extract_and_transform_parts(self, drawing: Dict, transformer: CoordinateTransformer) -> List[Dict]:
        """
        Extract all parts and transform from 3D model space to 2D drawing space

        Args:
            drawing: Drawing data
            transformer: Coordinate transformer

        Returns:
            List of parts with vertices in 2D drawing coordinates
        """
        parts = []

        for obj in drawing.get('objects', []):
            if obj.get('type_name') == 'Part':
                # Extract geometric data
                geom = obj.get('geometric_data', {})
                part_geom = geom.get('part_geometry', {})

                # Get vertices in 3D model space
                vertices_3d = part_geom.get('vertices', [])

                # Transform vertices to 2D
                vertices_2d = []
                for v in vertices_3d:
                    if isinstance(v, dict) and 'x' in v and 'y' in v:
                        x_2d, y_2d = transformer.transform_3d_to_2d(v)
                        vertices_2d.append({'x': x_2d, 'y': y_2d})

                # Get bounding box and transform
                bbox_model_3d = part_geom.get('bounding_box_model')
                bbox_2d = None
                if bbox_model_3d:
                    bbox_2d = transformer.transform_bbox(bbox_model_3d)

                if vertices_2d or bbox_2d:
                    parts.append({
                        'vertices': vertices_2d,
                        'bbox': bbox_2d,
                        'id': obj.get('id', 'unknown')
                    })

        return parts

    def _calculate_global_bbox(self, parts: List[Dict]) -> Optional[Dict]:
        """Calculate bounding box encompassing all parts"""
        all_points = []

        for part in parts:
            # Collect vertices
            vertices = part.get('vertices', [])
            for v in vertices:
                if isinstance(v, dict) and 'x' in v and 'y' in v:
                    all_points.append((v['x'], v['y']))

            # Collect bbox points if available (transformed format)
            bbox = part.get('bbox')
            if bbox and isinstance(bbox, dict):
                # Check for transformed bbox format (min_x, max_x, min_y, max_y)
                if 'min_x' in bbox and 'max_x' in bbox:
                    all_points.append((bbox['min_x'], bbox['min_y']))
                    all_points.append((bbox['max_x'], bbox['max_y']))
                # Check for old format (min/max points)
                else:
                    min_pt = bbox.get('min', {})
                    max_pt = bbox.get('max', {})

                    if 'x' in min_pt and 'y' in min_pt:
                        all_points.append((min_pt['x'], min_pt['y']))
                    if 'x' in max_pt and 'y' in max_pt:
                        all_points.append((max_pt['x'], max_pt['y']))

        if not all_points:
            return None

        # Calculate min/max
        xs = [p[0] for p in all_points]
        ys = [p[1] for p in all_points]

        return {
            'min_x': min(xs),
            'max_x': max(xs),
            'min_y': min(ys),
            'max_y': max(ys),
            'width': max(xs) - min(xs),
            'height': max(ys) - min(ys)
        }

    def _calculate_transform(self, bbox: Dict) -> Tuple[float, Tuple[float, float]]:
        """
        Calculate scale and offset to fit drawing in image

        Args:
            bbox: Global bounding box

        Returns:
            Tuple of (scale, (offset_x, offset_y))
        """
        # Calculate scale to fit in drawing area
        width = bbox['width']
        height = bbox['height']

        if width == 0 or height == 0:
            scale = 1.0
        else:
            scale_x = self.drawing_size / width
            scale_y = self.drawing_size / height
            scale = min(scale_x, scale_y)

        # Calculate offset to center drawing
        scaled_width = width * scale
        scaled_height = height * scale

        offset_x = self.margin + (self.drawing_size - scaled_width) / 2 - bbox['min_x'] * scale
        offset_y = self.margin + (self.drawing_size - scaled_height) / 2 - bbox['min_y'] * scale

        return scale, (offset_x, offset_y)

    def _draw_part(self, image: np.ndarray, part: Dict, scale: float, offset: Tuple[float, float]):
        """
        Draw a single part on the image

        Args:
            image: Image to draw on
            part: Part data
            scale: Scaling factor
            offset: (offset_x, offset_y)
        """
        vertices = part.get('vertices', [])

        if not vertices:
            # If no vertices, draw bounding box
            bbox = part.get('bbox')
            if bbox:
                self._draw_bbox(image, bbox, scale, offset)
            return

        # Convert vertices to image coordinates
        points = []
        for v in vertices:
            if isinstance(v, dict) and 'x' in v and 'y' in v:
                x = int(v['x'] * scale + offset[0])
                y = int(v['y'] * scale + offset[1])

                # Flip Y axis (OpenCV origin is top-left)
                y = self.image_size - y

                points.append([x, y])

        if len(points) < 2:
            return

        # Draw part outline
        points_array = np.array(points, dtype=np.int32)

        # Draw filled polygon (light gray)
        cv2.fillPoly(image, [points_array], color=(220, 220, 220))

        # Draw outline (dark gray)
        cv2.polylines(image, [points_array], isClosed=True, color=(80, 80, 80), thickness=2)

    def _draw_bbox(self, image: np.ndarray, bbox: Dict, scale: float, offset: Tuple[float, float]):
        """Draw bounding box as rectangle"""
        min_pt = bbox.get('min', {})
        max_pt = bbox.get('max', {})

        if 'x' not in min_pt or 'x' not in max_pt:
            return

        # Convert to image coordinates
        x1 = int(min_pt['x'] * scale + offset[0])
        y1 = int(min_pt['y'] * scale + offset[1])
        x2 = int(max_pt['x'] * scale + offset[0])
        y2 = int(max_pt['y'] * scale + offset[1])

        # Flip Y axis
        y1 = self.image_size - y1
        y2 = self.image_size - y2

        # Draw rectangle
        cv2.rectangle(image, (x1, y1), (x2, y2), color=(80, 80, 80), thickness=2)


def test_rasterizer():
    """Test rasterizer on sample drawings"""
    print("\n" + "="*60)
    print("TESTING DRAWING RASTERIZER")
    print("="*60 + "\n")

    # Load a sample drawing
    data_dir = Path(__file__).parent.parent / 'data/processed'
    train_file = data_dir / 'train.json'

    if not train_file.exists():
        print(f"Error: {train_file} not found")
        print("Run data_loader.py first to create train/val/test splits")
        return

    with open(train_file, 'r', encoding='utf-8') as f:
        train_data = json.load(f)

    if not train_data:
        print("Error: No training data found")
        return

    # Create output directory
    output_dir = Path(__file__).parent.parent / 'data/images/test'
    output_dir.mkdir(parents=True, exist_ok=True)

    # Rasterize first 5 drawings
    rasterizer = DrawingRasterizer(image_size=1024, margin=50)

    for i, drawing in enumerate(train_data[:5]):
        mark = drawing.get('mark', f'drawing_{i}').strip('[]')
        print(f"Rasterizing: {mark}")

        image, metadata = rasterizer.rasterize_drawing(drawing)

        # Save image
        output_path = output_dir / f"{mark}.png"
        cv2.imwrite(str(output_path), image)

        print(f"  Saved: {output_path}")
        print(f"  Parts: {metadata.get('part_count', 0)}")
        print(f"  Scale: {metadata.get('scale', 0):.2f}")
        print()

    print("="*60)
    print(f"TEST COMPLETE - Images saved to {output_dir}")
    print("="*60 + "\n")


if __name__ == '__main__':
    test_rasterizer()
