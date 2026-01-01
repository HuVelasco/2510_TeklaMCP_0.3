# Deep Learning Model for Automated Dimension Placement

**Project:** VirtualDetailer - Automated Drawing Dimensioning
**Date:** 2025-12-20
**Status:** Design Phase

---

## 1. Problem Statement

**Goal:** Automatically place dimensions on shop drawings (assembly and part drawings) based on learned patterns from existing detailed drawings.

**Input:** Un-dimensioned shop drawing (geometry visible, no dimensions)
- Parts with vertices, bounding boxes
- Connections, bolts, welds
- View transformations (2D coordinates)

**Output:** Fully dimensioned drawing
- Dimension placements (x, y coordinates)
- Dimension types (straight, angle, radius)
- Dimension targets (which parts/features to measure)

---

## 2. Training Data

**Dataset:** `tekla_extraction_ml_20251220_FINAL.jsonl`
- **95 drawings** (14 assembly + 81 part)
- **2,474 dimensions** (training targets)
- **218 parts** (input geometry)

**Data Structure per Drawing:**
```json
{
  "mark": "[P1234]",
  "drawing_type": "SinglePartDrawing",
  "objects": [
    {
      "type_name": "Part",
      "geometric_data": {
        "part_geometry": {
          "bounding_box_model": {"min": {...}, "max": {...}},
          "vertices": [...]
        }
      }
    },
    {
      "type_name": "StraightDimension",
      "geometric_data": {
        "dimension_points": [{"x": 100, "y": 200, "z": 0}]
      }
    }
  ],
  "views": [...]
}
```

---

## 3. Model Architecture Options

### Option A: Object Detection Approach (Recommended for MVP)

**Architecture:** YOLOv8 / Faster R-CNN adapted for dimension placement

**Concept:** Treat dimension placement as "object detection" where dimensions are "detected" on the drawing.

**Input:**
- Rasterized drawing image (2D projection of parts)
- 1024x1024 pixels

**Output:**
- Bounding boxes for dimension placement areas
- Classification: dimension type (straight/angle/radius)

**Pros:**
- Well-established architecture
- Fast inference
- Works with image representations
- Can leverage pre-trained weights

**Cons:**
- Requires rasterizing vector geometry
- May miss precise geometric relationships

---

### Option B: Graph Neural Network (GNN)

**Architecture:** Graph Attention Network (GAT) or GraphSAGE

**Concept:** Model drawing as a graph where nodes = parts/features, edges = spatial relationships

**Input:**
- Node features: Part geometry (bbox, vertices, type)
- Edge features: Distance, orientation, connections

**Output:**
- Dimension placements per node/edge
- Dimension properties

**Pros:**
- Preserves geometric relationships
- More interpretable
- Works with vector data directly

**Cons:**
- More complex to implement
- Requires careful graph construction
- Less established for this problem

---

### Option C: Transformer-based Sequence Model

**Architecture:** Transformer encoder-decoder

**Concept:** Treat dimension placement as sequence generation: Part features → Dimension sequence

**Input:**
- Sequence of part features (serialized geometry)
- Positional encodings

**Output:**
- Sequence of dimension specifications
- Attention maps showing part-dimension relationships

**Pros:**
- Can capture long-range dependencies
- Attention mechanism is interpretable
- Flexible output format

**Cons:**
- Requires large dataset (we have only 95 drawings)
- Computationally expensive
- Order-dependence issues

---

## 4. Recommended Approach: Hybrid Model

**Phase 1 (MVP):** Object Detection + Rule-based Post-processing

1. **Preprocessing:**
   - Convert JSONL → 2D images (rasterize parts as polygons)
   - Annotate with dimension locations from training data
   - Data augmentation (rotation, scale, noise)

2. **Model:**
   - YOLOv8 for dimension detection
   - Input: 1024x1024 grayscale image
   - Output: Bounding boxes + dimension type + confidence

3. **Post-processing:**
   - Snap dimensions to part edges/vertices (rule-based)
   - Remove duplicates
   - Enforce dimension constraints (parallel, perpendicular, etc.)

**Phase 2 (Production):** GNN Refinement

1. Use Phase 1 predictions as initialization
2. GNN refines placements based on geometric constraints
3. Learn optimal dimension spacing, leader line placement

---

## 5. Implementation Plan

### Step 1: Data Preparation (Week 1)

**Tasks:**
1. Parse JSONL → structured dataset
2. Extract part geometries + dimension annotations
3. Create train/val/test split (70/15/15)
4. Implement visualization to verify data quality

**Deliverables:**
- `data/train.json`, `data/val.json`, `data/test.json`
- Visualization notebook showing samples

---

### Step 2: MVP Model (Week 1-2)

**Tasks:**
1. Rasterize drawings → images
2. Annotate dimension locations in YOLO format
3. Train YOLOv8-small model
4. Evaluate on validation set

**Target Metrics:**
- **mAP@0.5:** >0.70 (dimension detection accuracy)
- **Precision:** >0.75 (correct dimensions / predicted)
- **Recall:** >0.65 (detected dimensions / total)

**Deliverables:**
- Trained model weights `models/yolo_v1.pt`
- Training curves and metrics

---

### Step 3: Post-processing & Refinement (Week 2)

**Tasks:**
1. Implement snap-to-edge logic
2. Remove duplicate/overlapping dimensions
3. Add dimension text positioning
4. Integrate with Tekla API for actual dimension creation

**Deliverables:**
- `inference/dimension_placer.py` - End-to-end pipeline
- Test on B1-1, B2-1, ... B10-1 drawings

---

### Step 4: Evaluation & Fine-tuning (Week 3)

**Tasks:**
1. Visual review of dimensioned drawings
2. Collect feedback on failures
3. Fine-tune model with hard negative mining
4. Iterate

**Success Criteria:**
- **User acceptance:** 80%+ of dimensions placed correctly
- **Manual adjustment needed:** <20% of dimensions
- **Zero catastrophic errors** (dimensions on wrong parts)

---

## 6. Technical Stack

**Framework:**
```python
- PyTorch 2.0+
- Ultralytics YOLOv8
- OpenCV (image rendering)
- NumPy, Pandas (data processing)
- Matplotlib, Plotly (visualization)
```

**Integration:**
```csharp
- TeklaMCP C# (dimension creation in Tekla)
- HTTP API (model inference server)
```

**Infrastructure:**
```bash
- GPU: CUDA 11.8+ (for training)
- CPU inference: ONNX Runtime (for production)
```

---

## 7. Dataset Split Strategy

**Training Set (67 drawings):**
- Mix of assembly (10) and part (57) drawings
- Used for model training

**Validation Set (14 drawings):**
- Used for hyperparameter tuning
- Monitor overfitting

**Test Set (14 drawings):**
- Final evaluation
- Never seen during training

**User Test Set (B1-1 through B10-1):**
- Real-world test on undimensioned copies
- Visual review by user

---

## 8. Success Metrics

| Metric | Target | Description |
|--------|--------|-------------|
| **Dimension Detection mAP** | >70% | % of dimensions detected correctly |
| **Placement Accuracy** | <50mm | Avg distance from ideal position |
| **Type Accuracy** | >90% | % of correct dimension types |
| **User Acceptance** | >80% | % of dimensions needing no adjustment |
| **Inference Time** | <5 sec/drawing | Time to dimension one drawing |

---

## 9. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Small dataset (95 drawings)** | High | Data augmentation, transfer learning |
| **Variable drawing complexity** | Medium | Separate models for assembly vs part |
| **Rasterization loss of precision** | Medium | High-resolution rendering (2048x2048) |
| **Overfitting** | High | Cross-validation, regularization |

---

## 10. Next Steps (Immediate)

1. ✅ Set up project structure
2. Create data loading pipeline
3. Implement drawing → image converter
4. Train baseline YOLOv8 model
5. Test on B18 drawing (validate end-to-end)

---

**Estimated Timeline:** 3 weeks to working MVP
**Required Resources:** GPU with 8GB+ VRAM for training

---

*Document prepared for ML model development phase*
*See SESSION_COMPLETE_20251220.md for data extraction summary*
