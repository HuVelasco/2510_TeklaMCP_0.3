#!/usr/bin/env python3
"""
Create Visualizations from Extraction Data
- Bar charts showing distributions
- Relationship diagrams
- Comparison charts
"""
import json
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
from collections import Counter

# Load visualization data
with open('visualization_data.json', 'r') as f:
    data = json.load(f)

# Create figure with subplots
fig = plt.figure(figsize=(16, 12))

# 1. Drawing Types Distribution
ax1 = plt.subplot(2, 3, 1)
drawing_types = Counter([d['type'] for d in data['drawings']])
colors = {'assembly': '#3498db', 'part': '#e74c3c', 'sub_assembly': '#f39c12', 'unknown': '#95a5a6'}
bars1 = ax1.bar(drawing_types.keys(), drawing_types.values(),
                color=[colors.get(k, '#95a5a6') for k in drawing_types.keys()])
ax1.set_title('Drawing Types Distribution', fontweight='bold')
ax1.set_ylabel('Count')
ax1.grid(axis='y', alpha=0.3)
for bar in bars1:
    height = bar.get_height()
    ax1.text(bar.get_x() + bar.get_width()/2., height,
             f'{int(height)}', ha='center', va='bottom')

# 2. Part Categories Distribution
ax2 = plt.subplot(2, 3, 2)
part_cats = dict(sorted(data['part_distribution'].items(), key=lambda x: x[1], reverse=True)[:8])
bars2 = ax2.barh(list(part_cats.keys()), list(part_cats.values()),
                 color='#27ae60')
ax2.set_title('Part Categories Distribution', fontweight='bold')
ax1.set_xlabel('Count')
ax2.grid(axis='x', alpha=0.3)
for i, (cat, count) in enumerate(part_cats.items()):
    ax2.text(count, i, f' {count}', va='center')

# 3. Dimension Types Distribution
ax3 = plt.subplot(2, 3, 3)
dim_types = dict(sorted(data['dimension_distribution'].items(), key=lambda x: x[1], reverse=True))
bars3 = ax3.bar(range(len(dim_types)), list(dim_types.values()),
                color='#9b59b6')
ax3.set_xticks(range(len(dim_types)))
ax3.set_xticklabels([k.replace('Dimension', '\nDim') for k in dim_types.keys()], rotation=0, ha='center', fontsize=8)
ax3.set_title('Dimension Types Distribution', fontweight='bold')
ax3.set_ylabel('Count')
ax3.grid(axis='y', alpha=0.3)
for i, count in enumerate(dim_types.values()):
    ax3.text(i, count, f'{count}', ha='center', va='bottom')

# 4. Unique Parts per Drawing
ax4 = plt.subplot(2, 3, 4)
drawings_sorted = sorted(data['drawings'], key=lambda x: x['unique_parts'], reverse=True)
marks = [d['mark'][:10] for d in drawings_sorted]
unique_parts = [d['unique_parts'] for d in drawings_sorted]
drawing_type_colors = [colors.get(d['type'], '#95a5a6') for d in drawings_sorted]

bars4 = ax4.bar(range(len(marks)), unique_parts, color=drawing_type_colors)
ax4.set_xticks(range(len(marks)))
ax4.set_xticklabels(marks, rotation=45, ha='right', fontsize=8)
ax4.set_title('Unique Parts per Drawing', fontweight='bold')
ax4.set_ylabel('Unique Parts')
ax4.grid(axis='y', alpha=0.3)

# Legend for drawing types
legend_patches = [mpatches.Patch(color=colors[t], label=t.replace('_', ' ').title())
                  for t in ['assembly', 'part', 'sub_assembly']]
ax4.legend(handles=legend_patches, loc='upper right', fontsize=8)

# 5. Dimensions per Drawing
ax5 = plt.subplot(2, 3, 5)
dimensions = [d['dimensions'] for d in drawings_sorted]
bars5 = ax5.bar(range(len(marks)), dimensions, color='#e67e22')
ax5.set_xticks(range(len(marks)))
ax5.set_xticklabels(marks, rotation=45, ha='right', fontsize=8)
ax5.set_title('Dimensions per Drawing', fontweight='bold')
ax5.set_ylabel('Dimension Count')
ax5.grid(axis='y', alpha=0.3)

# 6. Parts vs Dimensions Scatter
ax6 = plt.subplot(2, 3, 6)
for dtype in set(d['type'] for d in data['drawings']):
    filtered = [d for d in data['drawings'] if d['type'] == dtype]
    x = [d['unique_parts'] for d in filtered]
    y = [d['dimensions'] for d in filtered]
    ax6.scatter(x, y, s=100, alpha=0.6, color=colors.get(dtype, '#95a5a6'),
                label=dtype.replace('_', ' ').title())

ax6.set_xlabel('Unique Parts')
ax6.set_ylabel('Total Dimensions')
ax6.set_title('Parts vs Dimensions Relationship', fontweight='bold')
ax6.grid(alpha=0.3)
ax6.legend()

# Add a best-fit line
all_x = [d['unique_parts'] for d in data['drawings']]
all_y = [d['dimensions'] for d in data['drawings']]
if len(all_x) > 1:
    z = np.polyfit(all_x, all_y, 1)
    p = np.poly1d(z)
    ax6.plot(sorted(all_x), p(sorted(all_x)), "r--", alpha=0.5, linewidth=2)

plt.tight_layout()
plt.savefig('extraction_visualizations.png', dpi=150, bbox_inches='tight')
print("✓ Saved: extraction_visualizations.png")

# Create second figure: Detailed comparison
fig2, axes = plt.subplots(2, 2, figsize=(14, 10))

# Drawing-level detail
for idx, drawing in enumerate(data['drawings'][:4]):  # Show first 4 drawings
    ax = axes[idx//2, idx%2]

    # Get drawing details from summaries
    mark = drawing['mark']

    # Sample data structure (simplified)
    metrics = {
        'Unique\nParts': drawing['unique_parts'],
        'Total\nDimensions': drawing['dimensions'],
        'Views': drawing['views']
    }

    bars = ax.bar(range(len(metrics)), list(metrics.values()),
                  color=['#3498db', '#e74c3c', '#f39c12'])
    ax.set_xticks(range(len(metrics)))
    ax.set_xticklabels(list(metrics.keys()), fontsize=9)
    ax.set_title(f'{mark} - {drawing["type"]}', fontweight='bold')
    ax.grid(axis='y', alpha=0.3)

    for bar in bars:
        height = bar.get_height()
        ax.text(bar.get_x() + bar.get_width()/2., height,
                f'{int(height)}', ha='center', va='bottom')

plt.tight_layout()
plt.savefig('drawing_details.png', dpi=150, bbox_inches='tight')
print("✓ Saved: drawing_details.png")

# Create third figure: Data relationships diagram
fig3 = plt.figure(figsize=(14, 10))
ax = fig3.add_subplot(111)

# Summary statistics text
stats_text = f"""
EXTRACTION DATA SUMMARY

Drawings: {len(data['drawings'])}
Total Unique Parts: {sum(d['unique_parts'] for d in data['drawings'])}
Total Dimensions: {sum(d['dimensions'] for d in data['drawings'])}

Part Categories: {len(data['part_distribution'])}
Dimension Types: {len(data['dimension_distribution'])}

Drawing Types:
"""

for dtype, count in sorted(Counter([d['type'] for d in data['drawings']]).items()):
    stats_text += f"  • {dtype}: {count}\n"

stats_text += f"\nTop Part Categories:\n"
for cat, count in sorted(data['part_distribution'].items(), key=lambda x: x[1], reverse=True)[:5]:
    stats_text += f"  • {cat}: {count}\n"

stats_text += f"\nDimension Types:\n"
for dtype, count in sorted(data['dimension_distribution'].items(), key=lambda x: x[1], reverse=True)[:5]:
    stats_text += f"  • {dtype}: {count}\n"

ax.text(0.05, 0.95, stats_text, transform=ax.transAxes,
        fontsize=12, verticalalignment='top', fontfamily='monospace',
        bbox=dict(boxstyle='round', facecolor='wheat', alpha=0.5))
ax.axis('off')

plt.savefig('data_summary.png', dpi=150, bbox_inches='tight')
print("✓ Saved: data_summary.png")

print("\n✓ All visualizations created successfully!")
print("\nGenerated files:")
print("  1. extraction_visualizations.png - Main charts")
print("  2. drawing_details.png - Per-drawing comparison")
print("  3. data_summary.png - Summary statistics")
