#!/usr/bin/env python3
"""Debug annotation generation"""
import json
from pathlib import Path
from rasterizer import DrawingRasterizer
from annotator import YOLOAnnotator

# Load first drawing
data_dir = Path(__file__).parent.parent / 'data/processed'
with open(data_dir / 'train.json', 'r') as f:
    drawings = json.load(f)

drawing = drawings[0]  # Should be p.94 with 35 dimensions
mark = drawing.get('mark', 'unknown')
print(f'\nDrawing: {mark}')
print(f'Objects: {len(drawing["objects"])}')

# Count dimensions
dims_in_drawing = [o for o in drawing['objects'] if 'Dimension' in o.get('type_name', '')]
print(f'Dimensions in drawing: {len(dims_in_drawing)}')

# Rasterize
print('\n--- RASTERIZING ---')
rasterizer = DrawingRasterizer()
image, metadata = rasterizer.rasterize_drawing(drawing)
print(f'Metadata: {metadata}')

# Extract dimensions using annotator
print('\n--- EXTRACTING DIMENSIONS ---')
annotator = YOLOAnnotator()
dims_extracted = annotator._extract_dimensions(drawing)
print(f'Dimensions extracted by annotator: {len(dims_extracted)}')

if dims_extracted:
    first_dim = dims_extracted[0]
    print(f'First dimension type: {first_dim["type"]}')
    print(f'First dimension points: {first_dim["points"]}')

    # Try converting to YOLO
    print('\n--- CONVERTING TO YOLO ---')
    scale = metadata.get('scale', 1.0)
    offset = metadata.get('offset', (0, 0))
    print(f'Using scale={scale}, offset={offset}')

    yolo_ann = annotator._dimension_to_yolo(first_dim, scale, offset)
    print(f'YOLO annotation: {yolo_ann}')

# Generate all annotations
print('\n--- GENERATING ALL ANNOTATIONS ---')
annotations = annotator.generate_annotations(drawing, metadata)
print(f'Total annotations generated: {len(annotations)}')

if annotations:
    print(f'Sample annotation: {annotations[0]}')
else:
    print('NO ANNOTATIONS GENERATED!')
    print('This is the problem we need to fix.')
