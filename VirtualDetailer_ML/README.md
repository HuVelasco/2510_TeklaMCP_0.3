# VirtualDetailer ML - Automated Dimension Placement

**Deep Learning system for automatic dimension placement on Tekla shop drawings**

## Project Overview

This ML system learns from existing detailed drawings to automatically place dimensions on new un-dimensioned drawings.

**Approach**: YOLOv8-based object detection adapted for dimension placement (MVP)

## Dataset

- **Source**: `tekla_extraction_ml_20251220_FINAL.jsonl`
- **Total Drawings**: 95 (14 assembly + 81 part)
- **Dimensions**: 2,474 (training targets)
- **Parts**: 218 (input geometry)

## Project Structure

```
VirtualDetailer_ML/
├── data/
│   ├── raw/                    # Original JSONL file
│   ├── processed/              # Train/val/test splits
│   └── images/                 # Rasterized drawings
├── models/                     # Trained model weights
├── notebooks/                  # Jupyter notebooks for exploration
├── src/
│   ├── data_loader.py          # Parse JSONL
│   ├── rasterizer.py           # Convert geometry to images
│   ├── annotator.py            # Generate YOLO annotations
│   ├── train.py                # Training script
│   └── inference.py            # Inference pipeline
├── requirements.txt            # Python dependencies
└── README.md                   # This file
```

## Installation

```bash
pip install -r requirements.txt
```

## Usage

### 1. Data Preparation
```bash
python src/data_loader.py
```

### 2. Training
```bash
python src/train.py --config config/yolo_v1.yaml
```

### 3. Inference
```bash
python src/inference.py --drawing B1-1 --output results/
```

## Success Metrics

| Metric | Target |
|--------|--------|
| Dimension Detection mAP | >70% |
| Placement Accuracy | <50mm |
| Type Accuracy | >90% |
| User Acceptance | >80% |
| Inference Time | <5 sec/drawing |

## Implementation Timeline

- **Week 1**: Data preparation + baseline model
- **Week 2**: Post-processing + Tekla integration
- **Week 3**: Evaluation + fine-tuning

## References

- Architecture: `../ML_MODEL_ARCHITECTURE.md`
- Training Data: `../Server/TeklaMCP/extractions/tekla_extraction_ml_20251220_FINAL.jsonl`
