#!/usr/bin/env python3
"""
Coordinate Transformation for Tekla Drawings
Transforms 3D model coordinates to 2D drawing coordinates using view transformations
"""
import numpy as np
from typing import Dict, Tuple, Optional


class CoordinateTransformer:
    """Transforms 3D model coordinates to 2D drawing coordinates"""

    def __init__(self, view: Dict):
        """
        Initialize transformer with view data

        Args:
            view: View dictionary containing coordinate_system_3d and coordinate_system_2d
        """
        self.view = view

        # Extract coordinate systems
        cs3d = view.get('coordinate_system_3d', {})
        cs2d = view.get('coordinate_system_2d', {})

        # 3D coordinate system
        self.origin_3d = self._parse_point(cs3d.get('origin', {}))
        self.axis_x = self._parse_point(cs3d.get('axis_x', {}))
        self.axis_y = self._parse_point(cs3d.get('axis_y', {}))
        self.normal = self._parse_point(cs3d.get('normal', {}))

        # 2D origin (in model space)
        self.origin_2d = np.array([
            cs2d.get('origin_x', 0),
            cs2d.get('origin_y', 0),
            cs2d.get('origin_z', 0)
        ])

        # Frame origin (drawing space offset)
        frame_origin = view.get('frame_origin', {})
        self.frame_offset = np.array([
            frame_origin.get('x', 0),
            frame_origin.get('y', 0)
        ])

    def _parse_point(self, point: Dict) -> np.ndarray:
        """Parse point dict to numpy array"""
        return np.array([
            point.get('x', 0),
            point.get('y', 0),
            point.get('z', 0)
        ])

    def transform_3d_to_2d(self, point_3d: Dict) -> Tuple[float, float]:
        """
        Transform 3D model point to 2D drawing coordinates

        Args:
            point_3d: Dict with x, y, z keys

        Returns:
            Tuple of (x_2d, y_2d) in drawing space
        """
        # Convert to numpy array
        p = np.array([
            point_3d.get('x', 0),
            point_3d.get('y', 0),
            point_3d.get('z', 0)
        ])

        # Translate to view origin
        p_relative = p - self.origin_3d

        # Project onto view plane using dot products
        # axis_x represents the horizontal direction in the view
        # axis_y represents the vertical direction in the view
        x_2d = np.dot(p_relative, self.axis_x)
        y_2d = np.dot(p_relative, self.axis_y)

        # Apply frame offset (drawing space translation)
        x_2d += self.frame_offset[0]
        y_2d += self.frame_offset[1]

        return float(x_2d), float(y_2d)

    def transform_bbox(self, bbox: Dict) -> Optional[Dict]:
        """
        Transform 3D bounding box to 2D

        Args:
            bbox: Bounding box with min/max points

        Returns:
            2D bounding box dict
        """
        min_pt = bbox.get('min', {})
        max_pt = bbox.get('max', {})

        if not min_pt or not max_pt:
            return None

        # Transform corners
        corners_3d = [
            {'x': min_pt['x'], 'y': min_pt['y'], 'z': min_pt.get('z', 0)},
            {'x': max_pt['x'], 'y': min_pt['y'], 'z': min_pt.get('z', 0)},
            {'x': min_pt['x'], 'y': max_pt['y'], 'z': min_pt.get('z', 0)},
            {'x': max_pt['x'], 'y': max_pt['y'], 'z': min_pt.get('z', 0)},
            {'x': min_pt['x'], 'y': min_pt['y'], 'z': max_pt.get('z', 0)},
            {'x': max_pt['x'], 'y': min_pt['y'], 'z': max_pt.get('z', 0)},
            {'x': min_pt['x'], 'y': max_pt['y'], 'z': max_pt.get('z', 0)},
            {'x': max_pt['x'], 'y': max_pt['y'], 'z': max_pt.get('z', 0)},
        ]

        # Transform all corners
        corners_2d = [self.transform_3d_to_2d(c) for c in corners_3d]

        # Calculate 2D bounding box
        xs = [c[0] for c in corners_2d]
        ys = [c[1] for c in corners_2d]

        return {
            'min_x': min(xs),
            'max_x': max(xs),
            'min_y': min(ys),
            'max_y': max(ys),
            'width': max(xs) - min(xs),
            'height': max(ys) - min(ys)
        }


def test_transformation():
    """Test coordinate transformation"""
    print("\n" + "="*60)
    print("TESTING COORDINATE TRANSFORMATION")
    print("="*60 + "\n")

    import json
    from pathlib import Path

    # Load test drawing
    data_dir = Path(__file__).parent.parent / 'data/processed'
    with open(data_dir / 'train.json', 'r') as f:
        drawings = json.load(f)

    drawing = drawings[0]
    mark = drawing.get('mark', 'unknown')
    print(f"Drawing: {mark}\n")

    # Get first view
    views = drawing.get('views', [])
    if not views:
        print("No views found!")
        return

    view = views[0]
    print(f"View type: {view.get('view_type')}")

    # Create transformer
    transformer = CoordinateTransformer(view)

    # Test with part bounding box
    parts = [o for o in drawing['objects'] if o.get('type_name') == 'Part']
    if parts:
        part = parts[0]
        geom = part.get('geometric_data', {})
        bbox_3d = geom.get('part_geometry', {}).get('bounding_box_model')

        if bbox_3d:
            print(f"\n3D Bounding Box:")
            print(f"  min: ({bbox_3d['min']['x']:.2f}, {bbox_3d['min']['y']:.2f}, {bbox_3d['min'].get('z', 0):.2f})")
            print(f"  max: ({bbox_3d['max']['x']:.2f}, {bbox_3d['max']['y']:.2f}, {bbox_3d['max'].get('z', 0):.2f})")

            bbox_2d = transformer.transform_bbox(bbox_3d)
            print(f"\n2D Bounding Box (transformed):")
            print(f"  min: ({bbox_2d['min_x']:.2f}, {bbox_2d['min_y']:.2f})")
            print(f"  max: ({bbox_2d['max_x']:.2f}, {bbox_2d['max_y']:.2f})")
            print(f"  size: {bbox_2d['width']:.2f} x {bbox_2d['height']:.2f}")

    # Test with dimension points
    dims = [o for o in drawing['objects'] if 'Dimension' in o.get('type_name', '')]
    if dims:
        dim = dims[0]
        props = dim.get('properties', {})
        start_str = props.get('StartPoint', '')
        end_str = props.get('EndPoint', '')

        print(f"\nDimension points (already in 2D drawing space):")
        print(f"  StartPoint: {start_str}")
        print(f"  EndPoint: {end_str}")

    print("\n" + "="*60)
    print("TEST COMPLETE")
    print("="*60 + "\n")


if __name__ == '__main__':
    test_transformation()
