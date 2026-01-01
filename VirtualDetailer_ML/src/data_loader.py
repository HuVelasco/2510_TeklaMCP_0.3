#!/usr/bin/env python3
"""
Data Loader for VirtualDetailer ML
Parses JSONL extraction data and creates train/val/test splits
"""
import json
import random
from pathlib import Path
from typing import List, Dict, Tuple
from collections import defaultdict


class DrawingDataset:
    """Handles loading and splitting of Tekla drawing extraction data"""

    def __init__(self, jsonl_path: str):
        """
        Initialize dataset from JSONL file

        Args:
            jsonl_path: Path to tekla_extraction_ml_20251220_FINAL.jsonl
        """
        self.jsonl_path = Path(jsonl_path)
        self.drawings = []
        self.stats = defaultdict(int)

        self._load_data()
        self._compute_stats()

    def _load_data(self):
        """Load all drawings from JSONL file"""
        print(f"Loading data from: {self.jsonl_path}")

        with open(self.jsonl_path, 'r', encoding='utf-8') as f:
            for line_num, line in enumerate(f, 1):
                if line.strip():
                    try:
                        drawing = json.loads(line)
                        self.drawings.append(drawing)
                    except json.JSONDecodeError as e:
                        print(f"Warning: Failed to parse line {line_num}: {e}")

        print(f"Loaded {len(self.drawings)} drawings")

    def _compute_stats(self):
        """Compute dataset statistics"""
        self.stats['total_drawings'] = len(self.drawings)

        assembly_count = 0
        part_count = 0
        total_parts = 0
        total_dims = 0
        total_objects = 0

        for drawing in self.drawings:
            dtype = drawing.get('drawing_type', 'unknown')

            if dtype == 'AssemblyDrawing':
                assembly_count += 1
            elif dtype == 'SinglePartDrawing':
                part_count += 1

            objects = drawing.get('objects', [])
            total_objects += len(objects)

            for obj in objects:
                obj_type = obj.get('type_name', '')
                if obj_type == 'Part':
                    total_parts += 1
                elif 'Dimension' in obj_type:
                    total_dims += 1

        self.stats['assembly_drawings'] = assembly_count
        self.stats['part_drawings'] = part_count
        self.stats['total_parts'] = total_parts
        self.stats['total_dimensions'] = total_dims
        self.stats['total_objects'] = total_objects
        self.stats['avg_dims_per_drawing'] = total_dims / len(self.drawings) if self.drawings else 0

    def print_stats(self):
        """Print dataset statistics"""
        print("\n" + "="*60)
        print("DATASET STATISTICS")
        print("="*60)
        print(f"Total drawings:        {self.stats['total_drawings']}")
        print(f"  Assembly drawings:   {self.stats['assembly_drawings']}")
        print(f"  Part drawings:       {self.stats['part_drawings']}")
        print(f"\nTotal objects:         {self.stats['total_objects']:,}")
        print(f"Total parts:           {self.stats['total_parts']:,}")
        print(f"Total dimensions:      {self.stats['total_dimensions']:,}")
        print(f"Avg dims/drawing:      {self.stats['avg_dims_per_drawing']:.1f}")
        print("="*60 + "\n")

    def split_data(self,
                   train_ratio: float = 0.70,
                   val_ratio: float = 0.15,
                   test_ratio: float = 0.15,
                   random_seed: int = 42) -> Tuple[List[Dict], List[Dict], List[Dict]]:
        """
        Split data into train/val/test sets

        Args:
            train_ratio: Proportion for training (default 0.70)
            val_ratio: Proportion for validation (default 0.15)
            test_ratio: Proportion for testing (default 0.15)
            random_seed: Random seed for reproducibility

        Returns:
            Tuple of (train_data, val_data, test_data)
        """
        # Validate ratios
        total_ratio = train_ratio + val_ratio + test_ratio
        if abs(total_ratio - 1.0) > 1e-6:
            raise ValueError(f"Ratios must sum to 1.0, got {total_ratio}")

        # Set random seed
        random.seed(random_seed)

        # Shuffle drawings
        shuffled = self.drawings.copy()
        random.shuffle(shuffled)

        # Calculate split indices
        n = len(shuffled)
        train_end = int(n * train_ratio)
        val_end = train_end + int(n * val_ratio)

        # Split data
        train_data = shuffled[:train_end]
        val_data = shuffled[train_end:val_end]
        test_data = shuffled[val_end:]

        print(f"\nData Split (seed={random_seed}):")
        print(f"  Training:   {len(train_data):3d} drawings ({len(train_data)/n*100:.1f}%)")
        print(f"  Validation: {len(val_data):3d} drawings ({len(val_data)/n*100:.1f}%)")
        print(f"  Test:       {len(test_data):3d} drawings ({len(test_data)/n*100:.1f}%)")
        print(f"  Total:      {len(shuffled):3d} drawings\n")

        return train_data, val_data, test_data

    def save_splits(self,
                    train_data: List[Dict],
                    val_data: List[Dict],
                    test_data: List[Dict],
                    output_dir: str):
        """
        Save train/val/test splits to JSON files

        Args:
            train_data: Training drawings
            val_data: Validation drawings
            test_data: Test drawings
            output_dir: Directory to save files
        """
        output_path = Path(output_dir)
        output_path.mkdir(parents=True, exist_ok=True)

        # Save each split
        splits = {
            'train': train_data,
            'val': val_data,
            'test': test_data
        }

        for split_name, split_data in splits.items():
            file_path = output_path / f"{split_name}.json"

            with open(file_path, 'w', encoding='utf-8') as f:
                json.dump(split_data, f, indent=2)

            print(f"Saved {split_name:>5s} split: {file_path} ({len(split_data)} drawings)")

        # Save split metadata
        metadata = {
            'total_drawings': len(self.drawings),
            'train_count': len(train_data),
            'val_count': len(val_data),
            'test_count': len(test_data),
            'random_seed': 42,
            'source_file': str(self.jsonl_path),
            'statistics': dict(self.stats)
        }

        metadata_path = output_path / 'split_metadata.json'
        with open(metadata_path, 'w', encoding='utf-8') as f:
            json.dump(metadata, f, indent=2)

        print(f"Saved metadata:  {metadata_path}\n")


def main():
    """Main execution function"""
    # Path to the ML-ready dataset
    jsonl_file = Path(__file__).parent.parent.parent / \
                 'Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_FINAL.jsonl'

    # Output directory for processed data
    output_dir = Path(__file__).parent.parent / 'data/processed'

    print("\n" + "="*60)
    print("VIRTUALDETAILER ML - DATA PREPARATION")
    print("="*60 + "\n")

    # Load dataset
    dataset = DrawingDataset(jsonl_file)
    dataset.print_stats()

    # Create train/val/test splits
    train_data, val_data, test_data = dataset.split_data(
        train_ratio=0.70,
        val_ratio=0.15,
        test_ratio=0.15,
        random_seed=42
    )

    # Save splits
    dataset.save_splits(train_data, val_data, test_data, output_dir)

    print("="*60)
    print("DATA PREPARATION COMPLETE")
    print("="*60)
    print("\nNext steps:")
    print("  1. Implement rasterizer (src/rasterizer.py)")
    print("  2. Implement YOLO annotator (src/annotator.py)")
    print("  3. Generate training images and annotations")
    print("="*60 + "\n")


if __name__ == '__main__':
    main()
