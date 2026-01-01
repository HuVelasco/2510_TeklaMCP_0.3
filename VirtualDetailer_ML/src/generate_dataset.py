#!/usr/bin/env python3
"""
Complete Dataset Generation Pipeline
Generates images and YOLO annotations for all train/val/test splits
"""
import json
import cv2
from pathlib import Path
from tqdm import tqdm
from rasterizer import DrawingRasterizer
from annotator import YOLOAnnotator


class DatasetGenerator:
    """Complete pipeline for generating YOLO training dataset"""

    def __init__(self,
                 image_size: int = 1024,
                 margin: int = 50):
        """
        Initialize dataset generator

        Args:
            image_size: Output image size
            margin: Margin around drawings
        """
        self.rasterizer = DrawingRasterizer(image_size=image_size, margin=margin)
        self.annotator = YOLOAnnotator(image_size=image_size)
        self.stats = {
            'total_drawings': 0,
            'total_dimensions': 0,
            'errors': [],
            'splits': {}
        }

    def process_split(self,
                      split_name: str,
                      drawings: list,
                      output_dir: Path):
        """
        Process a single split (train/val/test)

        Args:
            split_name: 'train', 'val', or 'test'
            drawings: List of drawing dicts
            output_dir: Output directory for this split
        """
        print(f"\n{'='*60}")
        print(f"Processing {split_name.upper()} split ({len(drawings)} drawings)")
        print(f"{'='*60}\n")

        # Create directories
        images_dir = output_dir / 'images' / split_name
        labels_dir = output_dir / 'labels' / split_name
        images_dir.mkdir(parents=True, exist_ok=True)
        labels_dir.mkdir(parents=True, exist_ok=True)

        split_stats = {
            'processed': 0,
            'dimensions': 0,
            'errors': 0
        }

        # Process each drawing
        for drawing in tqdm(drawings, desc=f"{split_name:>5s}"):
            mark = drawing.get('mark', 'unknown').strip('[]')

            try:
                # Rasterize drawing
                image, metadata = self.rasterizer.rasterize_drawing(drawing)

                # Check for errors
                if 'error' in metadata:
                    split_stats['errors'] += 1
                    self.stats['errors'].append({
                        'split': split_name,
                        'mark': mark,
                        'error': metadata['error']
                    })
                    continue

                # Generate annotations
                annotations = self.annotator.generate_annotations(drawing, metadata)

                # Save image
                image_path = images_dir / f"{mark}.png"
                cv2.imwrite(str(image_path), image)

                # Save annotations
                label_path = labels_dir / f"{mark}.txt"
                self.annotator.save_yolo_annotation(annotations, label_path)

                split_stats['processed'] += 1
                split_stats['dimensions'] += len(annotations)

            except Exception as e:
                split_stats['errors'] += 1
                self.stats['errors'].append({
                    'split': split_name,
                    'mark': mark,
                    'error': str(e)
                })
                print(f"\nError processing {mark}: {e}")

        # Update global stats
        self.stats['total_drawings'] += split_stats['processed']
        self.stats['total_dimensions'] += split_stats['dimensions']
        self.stats['splits'][split_name] = split_stats

        print(f"\n{split_name.upper()} Results:")
        print(f"  Processed:   {split_stats['processed']:3d} drawings")
        print(f"  Dimensions:  {split_stats['dimensions']:4d} annotations")
        print(f"  Errors:      {split_stats['errors']:3d}")

    def print_summary(self):
        """Print generation summary"""
        print("\n" + "="*60)
        print("DATASET GENERATION SUMMARY")
        print("="*60)
        print(f"\nTotal drawings:    {self.stats['total_drawings']}")
        print(f"Total dimensions:  {self.stats['total_dimensions']}")
        print(f"Total errors:      {len(self.stats['errors'])}")

        print("\nSplit breakdown:")
        for split_name, split_stats in self.stats['splits'].items():
            print(f"  {split_name:>5s}: {split_stats['processed']:3d} drawings, "
                  f"{split_stats['dimensions']:4d} dimensions")

        if self.stats['errors']:
            print(f"\nErrors ({len(self.stats['errors'])}):")
            for error in self.stats['errors'][:10]:  # Show first 10
                print(f"  {error['split']:>5s}/{error['mark']:20s}: {error['error']}")
            if len(self.stats['errors']) > 10:
                print(f"  ... and {len(self.stats['errors']) - 10} more")

        print("="*60 + "\n")


def main():
    """Main execution"""
    print("\n" + "="*60)
    print("YOLO DATASET GENERATION - VIRTUALDETAILER ML")
    print("="*60)

    # Paths
    data_dir = Path(__file__).parent.parent / 'data'
    processed_dir = data_dir / 'processed'
    output_dir = data_dir

    # Load splits
    splits = {}
    for split_name in ['train', 'val', 'test']:
        split_file = processed_dir / f'{split_name}.json'

        if not split_file.exists():
            print(f"Error: {split_file} not found")
            print("Run data_loader.py first to create splits")
            return

        with open(split_file, 'r', encoding='utf-8') as f:
            splits[split_name] = json.load(f)

        print(f"Loaded {split_name:>5s}: {len(splits[split_name]):3d} drawings")

    # Create generator
    generator = DatasetGenerator(image_size=1024, margin=50)

    # Process each split
    for split_name, drawings in splits.items():
        generator.process_split(split_name, drawings, output_dir)

    # Print summary
    generator.print_summary()

    # Create classes file
    classes_path = output_dir / 'classes.txt'
    generator.annotator.create_class_file(classes_path)
    print(f"Created classes file: {classes_path}")

    # Create YOLO dataset config
    create_yolo_config(output_dir)

    print("\n" + "="*60)
    print("DATASET GENERATION COMPLETE!")
    print("="*60)
    print("\nNext steps:")
    print("  1. Review generated images in data/images/")
    print("  2. Verify annotations in data/labels/")
    print("  3. Train YOLOv8: python src/train.py")
    print("="*60 + "\n")


def create_yolo_config(output_dir: Path):
    """Create YOLO dataset configuration YAML file"""
    config = {
        'path': str(output_dir.absolute()),
        'train': 'images/train',
        'val': 'images/val',
        'test': 'images/test',
        'names': {
            0: 'StraightDimension',
            1: 'AngleDimension',
            2: 'RadiusDimension',
            3: 'DiameterDimension',
            4: 'ArcLengthDimension'
        }
    }

    config_path = output_dir / 'dataset.yaml'

    with open(config_path, 'w') as f:
        f.write(f"# VirtualDetailer ML Dataset Configuration\n")
        f.write(f"# Generated: {Path(__file__).name}\n\n")
        f.write(f"path: {config['path']}\n")
        f.write(f"train: {config['train']}\n")
        f.write(f"val: {config['val']}\n")
        f.write(f"test: {config['test']}\n\n")
        f.write(f"names:\n")
        for class_id, class_name in config['names'].items():
            f.write(f"  {class_id}: {class_name}\n")

    print(f"Created YOLO config:  {config_path}")


if __name__ == '__main__':
    main()
