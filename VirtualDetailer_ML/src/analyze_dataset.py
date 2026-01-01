#!/usr/bin/env python3
"""
Comprehensive Dataset Analysis Script
Generates detailed statistics and insights about the training data
"""
import json
import cv2
import numpy as np
from pathlib import Path
from collections import defaultdict, Counter
import matplotlib.pyplot as plt
import matplotlib.patches as patches


def analyze_dataset():
    """Comprehensive analysis of the ML dataset"""
    print("\n" + "="*80)
    print("COMPREHENSIVE DATASET ANALYSIS")
    print("="*80 + "\n")

    base_dir = Path(__file__).parent.parent
    data_dir = base_dir / 'data'

    # Load data
    splits = ['train', 'val', 'test']

    analysis_results = {
        'summary': {},
        'class_distribution': {},
        'spatial_analysis': {},
        'annotation_quality': {},
        'split_statistics': {}
    }

    # Class names
    class_names = {
        0: "StraightDimension",
        1: "AngleDimension",
        2: "RadiusDimension",
        3: "DiameterDimension",
        4: "ArcLengthDimension"
    }

    print("1. DATASET OVERVIEW")
    print("-" * 80)

    total_images = 0
    total_annotations = 0

    for split in splits:
        images_dir = data_dir / 'images' / split
        labels_dir = data_dir / 'labels' / split

        images = list(images_dir.glob('*.png'))

        split_annotations = 0
        for label_file in labels_dir.glob('*.txt'):
            with open(label_file, 'r') as f:
                split_annotations += len(f.readlines())

        total_images += len(images)
        total_annotations += split_annotations

        print(f"  {split.upper():5s}: {len(images):3d} images, {split_annotations:4d} annotations")

        analysis_results['split_statistics'][split] = {
            'images': len(images),
            'annotations': split_annotations,
            'avg_per_image': split_annotations / len(images) if images else 0
        }

    print(f"\n  TOTAL: {total_images:3d} images, {total_annotations:4d} annotations")
    print(f"  Average: {total_annotations/total_images:.1f} annotations per image")

    analysis_results['summary'] = {
        'total_images': total_images,
        'total_annotations': total_annotations,
        'avg_per_image': total_annotations / total_images
    }

    # 2. Class Distribution Analysis
    print("\n2. CLASS DISTRIBUTION")
    print("-" * 80)

    class_counts = defaultdict(int)

    for split in splits:
        labels_dir = data_dir / 'labels' / split

        for label_file in labels_dir.glob('*.txt'):
            with open(label_file, 'r') as f:
                for line in f:
                    parts = line.strip().split()
                    if parts:
                        class_id = int(parts[0])
                        class_counts[class_id] += 1

    print(f"\n  {'Class':<25s} {'Count':>6s} {'Percentage':>12s}")
    print("  " + "-" * 45)

    for class_id in sorted(class_counts.keys()):
        count = class_counts[class_id]
        percentage = (count / total_annotations) * 100
        print(f"  {class_names[class_id]:<25s} {count:6d} {percentage:11.1f}%")

    analysis_results['class_distribution'] = {
        class_names[cid]: {
            'count': count,
            'percentage': (count / total_annotations) * 100
        }
        for cid, count in class_counts.items()
    }

    # 3. Bounding Box Size Analysis
    print("\n3. BOUNDING BOX ANALYSIS")
    print("-" * 80)

    bbox_widths = defaultdict(list)
    bbox_heights = defaultdict(list)
    bbox_areas = defaultdict(list)

    for split in splits:
        labels_dir = data_dir / 'labels' / split

        for label_file in labels_dir.glob('*.txt'):
            with open(label_file, 'r') as f:
                for line in f:
                    parts = line.strip().split()
                    if len(parts) >= 5:
                        class_id = int(parts[0])
                        width = float(parts[3])
                        height = float(parts[4])
                        area = width * height

                        bbox_widths[class_id].append(width)
                        bbox_heights[class_id].append(height)
                        bbox_areas[class_id].append(area)

    print(f"\n  {'Class':<25s} {'Avg Width':>12s} {'Avg Height':>12s} {'Avg Area':>12s}")
    print("  " + "-" * 65)

    for class_id in sorted(bbox_widths.keys()):
        avg_w = np.mean(bbox_widths[class_id])
        avg_h = np.mean(bbox_heights[class_id])
        avg_a = np.mean(bbox_areas[class_id])

        print(f"  {class_names[class_id]:<25s} {avg_w:12.4f} {avg_h:12.4f} {avg_a:12.6f}")

    # 4. Annotation Distribution per Image
    print("\n4. ANNOTATIONS PER IMAGE DISTRIBUTION")
    print("-" * 80)

    annotations_per_image = []

    for split in splits:
        labels_dir = data_dir / 'labels' / split

        for label_file in labels_dir.glob('*.txt'):
            with open(label_file, 'r') as f:
                count = len(f.readlines())
                annotations_per_image.append(count)

    annotations_per_image = np.array(annotations_per_image)

    print(f"\n  Minimum:    {annotations_per_image.min():3d} annotations")
    print(f"  Maximum:    {annotations_per_image.max():3d} annotations")
    print(f"  Mean:       {annotations_per_image.mean():6.2f} annotations")
    print(f"  Median:     {np.median(annotations_per_image):6.2f} annotations")
    print(f"  Std Dev:    {annotations_per_image.std():6.2f} annotations")

    # Distribution bins
    bins = [0, 1, 5, 10, 15, 20, 30, 50]
    hist, _ = np.histogram(annotations_per_image, bins=bins)

    print(f"\n  Distribution:")
    for i in range(len(bins) - 1):
        print(f"    {bins[i]:2d}-{bins[i+1]:2d} annotations: {hist[i]:3d} images ({hist[i]/len(annotations_per_image)*100:5.1f}%)")

    analysis_results['annotation_quality'] = {
        'per_image_stats': {
            'min': int(annotations_per_image.min()),
            'max': int(annotations_per_image.max()),
            'mean': float(annotations_per_image.mean()),
            'median': float(np.median(annotations_per_image)),
            'std': float(annotations_per_image.std())
        }
    }

    # 5. Sample Drawing Analysis
    print("\n5. SAMPLE DRAWING ANALYSIS")
    print("-" * 80)

    # Analyze first 5 train images in detail
    train_images_dir = data_dir / 'images' / 'train'
    train_labels_dir = data_dir / 'labels' / 'train'

    sample_images = sorted(train_images_dir.glob('*.png'))[:5]

    print(f"\n  Analyzing {len(sample_images)} sample drawings:\n")

    for img_path in sample_images:
        label_path = train_labels_dir / f"{img_path.stem}.txt"

        # Load image to get dimensions
        img = cv2.imread(str(img_path))
        h, w = img.shape[:2] if img is not None else (0, 0)

        # Count annotations
        annotations = []
        if label_path.exists():
            with open(label_path, 'r') as f:
                for line in f:
                    parts = line.strip().split()
                    if len(parts) >= 5:
                        annotations.append({
                            'class_id': int(parts[0]),
                            'center_x': float(parts[1]),
                            'center_y': float(parts[2]),
                            'width': float(parts[3]),
                            'height': float(parts[4])
                        })

        # Class distribution in this image
        class_dist = Counter(a['class_id'] for a in annotations)

        print(f"  {img_path.name}")
        print(f"    Image Size: {w}x{h} pixels")
        print(f"    Annotations: {len(annotations)}")
        if class_dist:
            for class_id, count in sorted(class_dist.items()):
                print(f"      - {class_names[class_id]}: {count}")
        print()

    # 6. Image Quality Check
    print("6. IMAGE QUALITY ANALYSIS")
    print("-" * 80)

    image_sizes = []

    for split in splits:
        images_dir = data_dir / 'images' / split

        for img_path in images_dir.glob('*.png'):
            img = cv2.imread(str(img_path))
            if img is not None:
                h, w = img.shape[:2]
                image_sizes.append((w, h))

    image_sizes = np.array(image_sizes)

    print(f"\n  Total images analyzed: {len(image_sizes)}")
    print(f"  Image dimensions:")
    print(f"    Width:  {image_sizes[:, 0].min()}-{image_sizes[:, 0].max()} (all {image_sizes[:, 0].mean():.0f})")
    print(f"    Height: {image_sizes[:, 1].min()}-{image_sizes[:, 1].max()} (all {image_sizes[:, 1].mean():.0f})")

    unique_sizes = np.unique(image_sizes, axis=0)
    print(f"  Unique sizes: {len(unique_sizes)}")
    if len(unique_sizes) == 1:
        print(f"    [OK] All images same size: {unique_sizes[0][0]}x{unique_sizes[0][1]}")
    else:
        print(f"    [WARNING] Multiple sizes found:")
        for size in unique_sizes:
            count = np.sum(np.all(image_sizes == size, axis=1))
            print(f"      {size[0]}x{size[1]}: {count} images")

    # Save results
    output_file = base_dir / 'data_analysis_results.json'
    with open(output_file, 'w') as f:
        json.dump(analysis_results, f, indent=2)

    print(f"\n{'='*80}")
    print("ANALYSIS COMPLETE")
    print(f"{'='*80}")
    print(f"\nResults saved to: {output_file}")

    return analysis_results


def create_visualizations(analysis_results):
    """Create visualization plots for the analysis"""
    print("\n" + "="*80)
    print("CREATING VISUALIZATIONS")
    print("="*80 + "\n")

    base_dir = Path(__file__).parent.parent
    output_dir = base_dir / 'data_visualizations'
    output_dir.mkdir(exist_ok=True)

    # 1. Class Distribution Pie Chart
    class_data = analysis_results['class_distribution']

    fig, ax = plt.subplots(figsize=(10, 8))

    classes = list(class_data.keys())
    counts = [class_data[c]['count'] for c in classes]
    colors = plt.cm.Set3(range(len(classes)))

    wedges, texts, autotexts = ax.pie(
        counts,
        labels=classes,
        autopct='%1.1f%%',
        colors=colors,
        startangle=90
    )

    # Make percentage text bold
    for autotext in autotexts:
        autotext.set_color('white')
        autotext.set_fontweight('bold')
        autotext.set_fontsize(10)

    ax.set_title('Dimension Type Distribution', fontsize=14, fontweight='bold', pad=20)

    plt.tight_layout()
    plt.savefig(output_dir / 'class_distribution.png', dpi=300, bbox_inches='tight')
    print("  [OK] Created: class_distribution.png")
    plt.close()

    # 2. Split Statistics Bar Chart
    splits_data = analysis_results['split_statistics']

    fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(14, 6))

    splits = list(splits_data.keys())
    images = [splits_data[s]['images'] for s in splits]
    annotations = [splits_data[s]['annotations'] for s in splits]

    x = np.arange(len(splits))
    width = 0.35

    ax1.bar(x, images, width, label='Images', color='steelblue')
    ax1.set_xlabel('Split', fontweight='bold')
    ax1.set_ylabel('Count', fontweight='bold')
    ax1.set_title('Images per Split', fontweight='bold')
    ax1.set_xticks(x)
    ax1.set_xticklabels([s.upper() for s in splits])
    ax1.grid(axis='y', alpha=0.3)

    for i, v in enumerate(images):
        ax1.text(i, v + 1, str(v), ha='center', fontweight='bold')

    ax2.bar(x, annotations, width, label='Annotations', color='coral')
    ax2.set_xlabel('Split', fontweight='bold')
    ax2.set_ylabel('Count', fontweight='bold')
    ax2.set_title('Annotations per Split', fontweight='bold')
    ax2.set_xticks(x)
    ax2.set_xticklabels([s.upper() for s in splits])
    ax2.grid(axis='y', alpha=0.3)

    for i, v in enumerate(annotations):
        ax2.text(i, v + 10, str(v), ha='center', fontweight='bold')

    plt.tight_layout()
    plt.savefig(output_dir / 'split_statistics.png', dpi=300, bbox_inches='tight')
    print("  [OK] Created: split_statistics.png")
    plt.close()

    # 3. Annotations per Image Distribution
    base_dir = Path(__file__).parent.parent
    data_dir = base_dir / 'data'

    annotations_per_image = []

    for split in ['train', 'val', 'test']:
        labels_dir = data_dir / 'labels' / split

        for label_file in labels_dir.glob('*.txt'):
            with open(label_file, 'r') as f:
                count = len(f.readlines())
                annotations_per_image.append(count)

    fig, ax = plt.subplots(figsize=(12, 6))

    bins = range(0, max(annotations_per_image) + 2)
    ax.hist(annotations_per_image, bins=bins, color='mediumseagreen', edgecolor='black', alpha=0.7)

    ax.set_xlabel('Number of Annotations per Image', fontweight='bold', fontsize=12)
    ax.set_ylabel('Frequency (Number of Images)', fontweight='bold', fontsize=12)
    ax.set_title('Distribution of Annotations per Image', fontweight='bold', fontsize=14, pad=15)
    ax.grid(axis='y', alpha=0.3)

    # Add statistics text
    stats_text = f"Mean: {np.mean(annotations_per_image):.1f}\n"
    stats_text += f"Median: {np.median(annotations_per_image):.0f}\n"
    stats_text += f"Std: {np.std(annotations_per_image):.1f}"

    ax.text(0.95, 0.95, stats_text,
            transform=ax.transAxes,
            fontsize=11,
            verticalalignment='top',
            horizontalalignment='right',
            bbox=dict(boxstyle='round', facecolor='wheat', alpha=0.5))

    plt.tight_layout()
    plt.savefig(output_dir / 'annotations_distribution.png', dpi=300, bbox_inches='tight')
    print("  [OK] Created: annotations_distribution.png")
    plt.close()

    print(f"\nVisualizations saved to: {output_dir}")


if __name__ == '__main__':
    results = analyze_dataset()
    create_visualizations(results)
