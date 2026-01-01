# Virtual Detailer ML Implementation Plan
**10-Iteration Roadmap to 90% Accurate Automated Dimensioning**

**Date:** 2025-12-16
**Target:** Achieve 90% correct shop drawings with automated dimension placement
**Approach:** Fast iteration, incremental complexity, production-ready at each step

---

## Plan Overview

### Strategic Approach

**Philosophy:** Start simple, validate fast, iterate based on real results

1. **Build narrow, deep** - Master one dimension type before adding complexity
2. **Test on production** - Use real drawings, real feedback from Day 1
3. **Measure obsessively** - Every iteration has clear success metrics
4. **Ship incrementally** - Each phase delivers value, even at 70% accuracy

### Success Criteria

- **Phase 1 Complete:** 80% accuracy on overall dimensions (straight beams)
- **Phase 3 Complete:** 85% accuracy including hole/cut features
- **Phase 5 Complete:** 90% accuracy on single-part drawings
- **Phase 8 Complete:** 90% accuracy on assembly drawings
- **Phase 10 Complete:** Production-ready virtual detailer

---

## 10-Iteration Roadmap

---

## ✓ ITERATION 1: Data Preparation & Baseline Model

**Duration:** 3-5 days
**Complexity:** Low
**Goal:** Transform extraction data into ML-ready format and build simplest possible model

### Objectives

1. Parse JSONL extraction into structured training data
2. Extract features from part geometry (bounding boxes)
3. Extract labels from dimension positions
4. Build baseline rule-based model (no ML yet!)
5. Establish evaluation metrics

### Input Data

- File: `tekla_extraction_20251213_180910.jsonl`
- 462 parts with geometric_data
- 254 dimensions with positions
- 1,176 dimension-part relationships

### Technical Tasks

**Data Processing:**
```python
# 1. Load and parse JSONL
def load_extraction_data(jsonl_file):
    with open(jsonl_file) as f:
        return json.loads(f.readline())

# 2. Extract part features
def extract_part_features(part_obj):
    geom = part_obj['geometric_data']['part_geometry']
    bbox = geom['bounding_box']

    features = {
        'part_id': part_obj.get('object_id'),
        'min_x': bbox['min']['x'],
        'min_y': bbox['min']['y'],
        'min_z': bbox['min']['z'],
        'max_x': bbox['max']['x'],
        'max_y': bbox['max']['y'],
        'max_z': bbox['max']['z'],
        'width': bbox['max']['x'] - bbox['min']['x'],
        'height': bbox['max']['y'] - bbox['min']['y'],
        'depth': bbox['max']['z'] - bbox['min']['z'],
        'vertex_count': len(geom.get('vertices', []))
    }
    return features

# 3. Extract dimension labels
def extract_dimension_labels(dimension_obj, part_id):
    dim_points = dimension_obj['geometric_data']['dimension_points']
    return {
        'part_id': part_id,
        'dim_type': dimension_obj['type_name'],
        'start_x': dim_points[0]['x'] if dim_points else None,
        'start_y': dim_points[0]['y'] if dim_points else None,
        'end_x': dim_points[1]['x'] if len(dim_points) > 1 else None,
        'end_y': dim_points[1]['y'] if len(dim_points) > 1 else None,
    }

# 4. Build training dataset
def build_training_data(extraction_data):
    parts = [obj for obj in extraction_data['objects']
             if obj['type_name'] == 'Part']
    dimensions = [obj for obj in extraction_data['objects']
                  if 'Dimension' in obj['type_name']]

    X = []  # Features
    Y = []  # Labels

    for part in parts:
        features = extract_part_features(part)
        related_dims = find_related_dimensions(part, dimensions)
        labels = [extract_dimension_labels(dim, part['object_id'])
                  for dim in related_dims]

        X.append(features)
        Y.append(labels)

    return X, Y
```

**Baseline Model (Rule-Based):**
```python
def baseline_dimension_placer(part_features):
    """
    Rule: Place 4 dimensions at bbox edges
    - Top: horizontal line at max_y
    - Bottom: horizontal line at min_y
    - Left: vertical line at min_x
    - Right: vertical line at max_x
    """
    dims = []

    # Top dimension
    dims.append({
        'type': 'horizontal',
        'start': (part_features['min_x'], part_features['max_y']),
        'end': (part_features['max_x'], part_features['max_y']),
        'location': 'top'
    })

    # Bottom dimension
    dims.append({
        'type': 'horizontal',
        'start': (part_features['min_x'], part_features['min_y']),
        'end': (part_features['max_x'], part_features['min_y']),
        'location': 'bottom'
    })

    # Left dimension
    dims.append({
        'type': 'vertical',
        'start': (part_features['min_x'], part_features['min_y']),
        'end': (part_features['min_x'], part_features['max_y']),
        'location': 'left'
    })

    # Right dimension
    dims.append({
        'type': 'vertical',
        'start': (part_features['max_x'], part_features['min_y']),
        'end': (part_features['max_x'], part_features['max_y']),
        'location': 'right'
    })

    return dims
```

**Evaluation Metrics:**
```python
def evaluate_dimension_placement(predicted_dims, actual_dims, tolerance_mm=50):
    """
    Metric 1: Position Accuracy (within 50mm)
    Metric 2: Type Accuracy (horizontal vs vertical)
    Metric 3: Coverage (% of required dimensions placed)
    """
    correct_positions = 0
    correct_types = 0

    for pred, actual in zip(predicted_dims, actual_dims):
        # Position accuracy
        dist = distance(pred['start'], actual['start'])
        if dist < tolerance_mm:
            correct_positions += 1

        # Type accuracy
        if pred['type'] == actual['type']:
            correct_types += 1

    position_acc = correct_positions / len(actual_dims)
    type_acc = correct_types / len(actual_dims)
    coverage = len(predicted_dims) / len(actual_dims)

    return {
        'position_accuracy': position_acc,
        'type_accuracy': type_acc,
        'coverage': coverage,
        'overall_score': (position_acc + type_acc + coverage) / 3
    }
```

### Deliverables

1. ✓ Data processing pipeline (Python scripts)
2. ✓ Training dataset (X, Y arrays)
3. ✓ Baseline model implementation
4. ✓ Evaluation framework
5. ✓ Initial accuracy report

### Success Metrics

- **Data Processing:** Successfully parse 462 parts, 254 dimensions
- **Baseline Accuracy:** 40-60% (rule-based is simple but surprisingly effective)
- **Runtime:** <1 second per drawing
- **Deliverable:** `baseline_results.json` with accuracy scores

### Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Missing dimension data | Filter out incomplete samples |
| Coordinate system issues | Normalize to part-local coordinates |
| Large file size | Process in chunks |

---

## ✓ ITERATION 2: Simple ML Model (Overall Dimensions Only)

**Duration:** 5-7 days
**Complexity:** Medium
**Goal:** Replace rule-based with ML model, target 80% accuracy on overall dimensions

### Objectives

1. Train simple neural network on overall dimensions
2. Outperform baseline by 20-30 percentage points
3. Identify failure modes
4. Establish training/validation/test split

### Model Architecture

**Approach: Feedforward Neural Network**

```python
import torch
import torch.nn as nn

class DimensionPlacer(nn.Module):
    def __init__(self, input_dim=10, hidden_dim=64, output_dim=16):
        super().__init__()

        self.encoder = nn.Sequential(
            nn.Linear(input_dim, hidden_dim),
            nn.ReLU(),
            nn.Dropout(0.2),
            nn.Linear(hidden_dim, hidden_dim),
            nn.ReLU(),
            nn.Dropout(0.2),
            nn.Linear(hidden_dim, output_dim)
        )

    def forward(self, x):
        # x: [batch, 10] - part features
        # output: [batch, 16] - 4 dimensions × 4 values (x1,y1,x2,y2)
        return self.encoder(x)

# Input features (10-dimensional)
# 1-3:   min_x, min_y, min_z
# 4-6:   max_x, max_y, max_z
# 7-9:   width, height, depth
# 10:    vertex_count

# Output (16-dimensional)
# 0-3:   top_dim (start_x, start_y, end_x, end_y)
# 4-7:   bottom_dim (start_x, start_y, end_x, end_y)
# 8-11:  left_dim (start_x, start_y, end_x, end_y)
# 12-15: right_dim (start_x, start_y, end_x, end_y)
```

### Training Process

```python
def train_model(X_train, Y_train, X_val, Y_val, epochs=100):
    model = DimensionPlacer()
    optimizer = torch.optim.Adam(model.parameters(), lr=0.001)
    criterion = nn.MSELoss()  # Mean squared error for coordinates

    for epoch in range(epochs):
        # Training
        model.train()
        optimizer.zero_grad()

        predictions = model(X_train)
        loss = criterion(predictions, Y_train)

        loss.backward()
        optimizer.step()

        # Validation
        if epoch % 10 == 0:
            model.eval()
            with torch.no_grad():
                val_predictions = model(X_val)
                val_loss = criterion(val_predictions, Y_val)
                val_acc = evaluate_dimension_placement(val_predictions, Y_val)

            print(f"Epoch {epoch}: Train Loss={loss:.4f}, Val Loss={val_loss:.4f}, Val Acc={val_acc['overall_score']:.2%}")

    return model
```

### Data Split

```python
# Split strategy
train_ratio = 0.70  # 323 parts
val_ratio = 0.15    # 69 parts
test_ratio = 0.15   # 70 parts

# Ensure stratification by part type
from sklearn.model_selection import train_test_split

X_train, X_temp, Y_train, Y_temp = train_test_split(
    X, Y, test_size=0.30, stratify=part_types, random_state=42
)

X_val, X_test, Y_val, Y_test = train_test_split(
    X_temp, Y_temp, test_size=0.50, random_state=42
)
```

### Deliverables

1. ✓ Trained ML model (`dimension_placer_v1.pth`)
2. ✓ Training logs and curves
3. ✓ Validation results report
4. ✓ Error analysis (failure modes)
5. ✓ Comparison: Baseline vs ML model

### Success Metrics

- **Position Accuracy:** 75-80% (within 50mm)
- **Type Accuracy:** 90%+ (horizontal vs vertical)
- **Coverage:** 95%+ (most parts get dimensions)
- **Improvement:** +20-30 points over baseline

### Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Overfitting | Use dropout, early stopping |
| Poor generalization | Ensure diverse training data |
| Slow training | Use GPU if available |

---

## ✓ ITERATION 3: Production Integration & Real-World Testing

**Duration:** 5-7 days
**Complexity:** Medium
**Goal:** Test model on NEW drawings (not in training set), integrate into Tekla workflow

### Objectives

1. Create test drawings (drawing-1 postfix)
2. Run model predictions on test drawings
3. Compare AI dimensions vs human dimensions
4. Build Tekla integration tool
5. Collect user feedback

### Test Drawing Creation

**Procedure:**
1. Select 10 representative production drawings
2. Copy each drawing with '-1' postfix (e.g., ST1006 → ST1006-1)
3. Remove all dimensions from copies (keep only front view)
4. Run ML model to auto-dimension
5. Compare against original human-dimensioned drawings

### Tekla Integration Tool

```csharp
// VirtualFitter Integration (Phase 1)
public class DimensionPlacer
{
    public void PlaceDimensionsOnDrawing(string drawingMark)
    {
        // 1. Extract part geometry from drawing
        var parts = ExtractPartsFromDrawing(drawingMark);

        // 2. Call ML model via Python
        var predictions = CallMLModel(parts);

        // 3. Create dimensions in Tekla
        foreach (var pred in predictions)
        {
            CreateStraightDimension(pred.StartPoint, pred.EndPoint, pred.Distance);
        }

        // 4. Commit changes
        model.CommitChanges();
    }

    private List<DimensionPrediction> CallMLModel(List<Part> parts)
    {
        // Call Python ML model via HTTP or subprocess
        var pythonScript = "predict_dimensions.py";
        var input = JsonConvert.SerializeObject(parts);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"{pythonScript} --input {input}",
                RedirectStandardOutput = true,
                UseShellExecute = false
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return JsonConvert.DeserializeObject<List<DimensionPrediction>>(output);
    }
}
```

### Evaluation Process

```python
def test_on_production_drawings(model, test_drawings):
    """
    Test model on drawings not seen during training
    """
    results = []

    for drawing in test_drawings:
        # Extract parts from test drawing
        parts = extract_parts_from_drawing(drawing)

        # Get human dimensions (ground truth)
        human_dims = get_human_dimensions(drawing.replace('-1', ''))

        # Get AI predictions
        ai_dims = model.predict(parts)

        # Compare
        accuracy = evaluate_dimension_placement(ai_dims, human_dims)

        results.append({
            'drawing': drawing,
            'accuracy': accuracy,
            'human_dim_count': len(human_dims),
            'ai_dim_count': len(ai_dims)
        })

    return results
```

### Deliverables

1. ✓ 10 test drawings (with -1 postfix)
2. ✓ Tekla integration tool (VirtualFitter v0.1)
3. ✓ Production test results report
4. ✓ User feedback collection
5. ✓ Error case documentation

### Success Metrics

- **Real-World Accuracy:** 70-75% on unseen drawings
- **User Satisfaction:** "Better than starting from scratch"
- **Time Savings:** 50% reduction in dimensioning time
- **Zero Critical Errors:** No dimensions on wrong parts

### Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Lower accuracy on new data | Expected - document gaps |
| Integration bugs | Thorough testing in dev environment |
| User resistance | Emphasize time savings, not replacement |

---

## ✓ ITERATION 4: Feature Engineering & Model Improvement

**Duration:** 5-7 days
**Complexity:** Medium
**Goal:** Analyze failures, add features, improve to 85% accuracy

### Objectives

1. Analyze error cases from Iteration 3
2. Engineer new features based on failure modes
3. Retrain model with enhanced features
4. Validate improvement on test set

### Error Analysis

```python
def analyze_failure_modes(predictions, ground_truth):
    """
    Categorize errors to guide feature engineering
    """
    errors = {
        'position_off': [],      # Correct type, wrong position
        'type_wrong': [],        # Wrong dimension type
        'missing': [],           # Should have dimension, didn't place
        'extra': [],             # Placed dimension, shouldn't have
        'orientation': []        # Dimension flipped/rotated wrong
    }

    for pred, truth in zip(predictions, ground_truth):
        if not pred:
            errors['missing'].append(truth)
        elif not truth:
            errors['extra'].append(pred)
        elif pred['type'] != truth['type']:
            errors['type_wrong'].append((pred, truth))
        elif distance(pred['position'], truth['position']) > 50:
            errors['position_off'].append((pred, truth))

    return errors
```

### Enhanced Feature Engineering

```python
def extract_enhanced_features(part_obj):
    """
    Add more sophisticated features based on error analysis
    """
    basic_features = extract_part_features(part_obj)  # From Iteration 1

    # NEW FEATURES
    geom = part_obj['geometric_data']['part_geometry']
    vertices = geom.get('vertices', [])

    enhanced = {
        **basic_features,

        # Geometric complexity
        'vertex_count': len(vertices),
        'bbox_volume': basic_features['width'] * basic_features['height'] * basic_features['depth'],
        'aspect_ratio_xy': basic_features['width'] / basic_features['height'] if basic_features['height'] > 0 else 0,
        'aspect_ratio_xz': basic_features['width'] / basic_features['depth'] if basic_features['depth'] > 0 else 0,

        # Part orientation
        'is_horizontal': 1 if basic_features['width'] > basic_features['depth'] else 0,
        'is_vertical': 1 if basic_features['height'] > max(basic_features['width'], basic_features['depth']) else 0,

        # Shape classification
        'is_beam': 1 if len(vertices) == 12 else 0,
        'is_complex': 1 if len(vertices) > 20 else 0,

        # Contextual features (if relationships available)
        'has_connections': 1 if part_obj.get('related_objects') else 0,
        'connection_count': len(part_obj.get('related_objects', []))
    }

    return enhanced
```

### Model Architecture v2

```python
class DimensionPlacerV2(nn.Module):
    def __init__(self, input_dim=20, hidden_dim=128):
        super().__init__()

        # Deeper network with more capacity
        self.encoder = nn.Sequential(
            nn.Linear(input_dim, hidden_dim),
            nn.BatchNorm1d(hidden_dim),
            nn.ReLU(),
            nn.Dropout(0.3),

            nn.Linear(hidden_dim, hidden_dim),
            nn.BatchNorm1d(hidden_dim),
            nn.ReLU(),
            nn.Dropout(0.3),

            nn.Linear(hidden_dim, hidden_dim // 2),
            nn.BatchNorm1d(hidden_dim // 2),
            nn.ReLU(),
            nn.Dropout(0.2),

            nn.Linear(hidden_dim // 2, 16)  # 4 dims × 4 coords
        )

        # Separate heads for dimension type classification
        self.type_classifier = nn.Sequential(
            nn.Linear(hidden_dim // 2, 4),  # 4 dimension types
            nn.Softmax(dim=1)
        )

    def forward(self, x):
        features = self.encoder[:-1](x)  # Get features before final layer
        positions = self.encoder[-1](features)  # Dimension positions
        types = self.type_classifier(features)   # Dimension types

        return positions, types
```

### Training Improvements

```python
# Multi-task learning: Position + Type
def train_model_v2(X_train, Y_positions, Y_types, epochs=150):
    model = DimensionPlacerV2()
    optimizer = torch.optim.Adam(model.parameters(), lr=0.001, weight_decay=1e-5)

    # Two loss functions
    position_loss_fn = nn.MSELoss()
    type_loss_fn = nn.CrossEntropyLoss()

    # Learning rate scheduler
    scheduler = torch.optim.lr_scheduler.ReduceLROnPlateau(
        optimizer, mode='min', factor=0.5, patience=10
    )

    for epoch in range(epochs):
        model.train()
        optimizer.zero_grad()

        positions_pred, types_pred = model(X_train)

        # Combined loss
        loss_pos = position_loss_fn(positions_pred, Y_positions)
        loss_type = type_loss_fn(types_pred, Y_types)
        loss = loss_pos + 0.5 * loss_type  # Weight type loss less

        loss.backward()
        optimizer.step()
        scheduler.step(loss)

    return model
```

### Deliverables

1. ✓ Error analysis report
2. ✓ Enhanced feature set (20 features)
3. ✓ Model v2 implementation
4. ✓ Improved accuracy results
5. ✓ Feature importance analysis

### Success Metrics

- **Position Accuracy:** 80-85% (within 50mm)
- **Type Accuracy:** 95%+
- **Improvement:** +5-10 points over v1
- **Generalization:** Similar accuracy on train/val/test

---

## ✓ ITERATION 5: Data Augmentation & Robustness

**Duration:** 3-5 days
**Complexity:** Medium
**Goal:** Increase training data through augmentation, improve robustness

### Objectives

1. Extract 5-10 additional drawings
2. Apply data augmentation techniques
3. Retrain with augmented dataset
4. Test robustness to variations

### Data Extraction

```bash
# Extract more drawings
for drawing in [ST1007, ST1008, ST1009, ST1010, ST1011]; do
    python extract_drawing.py --drawing $drawing --output extractions/
done

# Combine datasets
python combine_extractions.py --input extractions/*.jsonl --output combined_training_data.jsonl
```

**Target:** 800-1000 parts, 500-600 dimensions

### Data Augmentation

```python
def augment_training_data(X, Y, augmentation_factor=3):
    """
    Augmentation strategies for geometric data:
    1. Rotation (0, 90, 180, 270 degrees)
    2. Scaling (±10% size variation)
    3. Translation (shift part position)
    4. Mirroring (flip horizontally/vertically)
    """
    X_aug = []
    Y_aug = []

    for x, y in zip(X, Y):
        # Original
        X_aug.append(x)
        Y_aug.append(y)

        # Rotation 90°
        x_rot90 = rotate_part_features(x, 90)
        y_rot90 = rotate_dimensions(y, 90)
        X_aug.append(x_rot90)
        Y_aug.append(y_rot90)

        # Scaling +10%
        x_scale = scale_part_features(x, 1.1)
        y_scale = scale_dimensions(y, 1.1)
        X_aug.append(x_scale)
        Y_aug.append(y_scale)

        # Mirroring (horizontal flip)
        x_mirror = mirror_part_features(x, axis='x')
        y_mirror = mirror_dimensions(y, axis='x')
        X_aug.append(x_mirror)
        Y_aug.append(y_mirror)

    return X_aug, Y_aug

def rotate_part_features(features, angle_degrees):
    """Rotate part coordinates by angle"""
    rad = np.radians(angle_degrees)
    cos, sin = np.cos(rad), np.sin(rad)

    # Rotate bounding box corners
    min_x, min_y = features['min_x'], features['min_y']
    max_x, max_y = features['max_x'], features['max_y']

    # Apply rotation matrix
    min_x_rot = min_x * cos - min_y * sin
    min_y_rot = min_x * sin + min_y * cos
    max_x_rot = max_x * cos - max_y * sin
    max_y_rot = max_x * sin + max_y * cos

    features_rot = {**features}
    features_rot['min_x'], features_rot['min_y'] = min_x_rot, min_y_rot
    features_rot['max_x'], features_rot['max_y'] = max_x_rot, max_y_rot

    return features_rot
```

### Robustness Testing

```python
def test_robustness(model, test_set):
    """
    Test model on variations:
    - Different drawing scales
    - Different part orientations
    - Different part sizes
    """
    results = {}

    # Test on rotated parts
    for angle in [0, 45, 90, 135, 180]:
        X_rotated = [rotate_part_features(x, angle) for x in test_set]
        accuracy = evaluate_model(model, X_rotated)
        results[f'rotation_{angle}'] = accuracy

    # Test on scaled parts
    for scale in [0.5, 0.75, 1.0, 1.25, 1.5]:
        X_scaled = [scale_part_features(x, scale) for x in test_set]
        accuracy = evaluate_model(model, X_scaled)
        results[f'scale_{scale}'] = accuracy

    return results
```

### Deliverables

1. ✓ 5-10 new drawings extracted
2. ✓ Augmented training dataset (2000+ samples)
3. ✓ Retrained model v2.1
4. ✓ Robustness test results
5. ✓ Updated accuracy report

### Success Metrics

- **Training Data:** 800-1000 parts (vs 462 before)
- **Augmented Data:** 2000-3000 samples
- **Accuracy:** Maintain or improve 85%
- **Robustness:** <5% accuracy drop on rotations

---

## ⚠ ITERATION 6: Feature Detection (Holes, Cuts, Notches)

**Duration:** 7-10 days
**Complexity:** High
**Goal:** Extend model to detect and dimension specific features, not just overall dimensions

### Objectives

1. Enhance extraction to identify geometric features
2. Build feature detector model
3. Place dimensions on features
4. Achieve 80% accuracy on feature dimensions

### Feature Extraction Enhancement

```python
def extract_geometric_features(part_vertices):
    """
    Identify features from vertex analysis:
    - Holes: Circular patterns in vertices
    - Cuts: Sharp edges, missing vertices
    - Notches: Rectangular recesses
    """
    features = []

    # Detect holes (circular patterns)
    holes = detect_circular_patterns(part_vertices)
    for hole in holes:
        features.append({
            'type': 'hole',
            'center': hole['center'],
            'radius': hole['radius'],
            'requires_dimension': True
        })

    # Detect cuts (edge analysis)
    cuts = detect_edge_discontinuities(part_vertices)
    for cut in cuts:
        features.append({
            'type': 'cut',
            'start': cut['start'],
            'end': cut['end'],
            'depth': cut['depth'],
            'requires_dimension': True
        })

    # Detect notches (rectangular recesses)
    notches = detect_rectangular_recesses(part_vertices)
    for notch in notches:
        features.append({
            'type': 'notch',
            'bbox': notch['bbox'],
            'requires_dimension': True
        })

    return features
```

### Feature Detection Model

```python
class FeatureDimensioner(nn.Module):
    def __init__(self):
        super().__init__()

        # Feature encoder (processes vertex data)
        self.feature_encoder = nn.Sequential(
            nn.Conv1d(3, 32, kernel_size=5, padding=2),  # 3D vertices
            nn.ReLU(),
            nn.MaxPool1d(2),
            nn.Conv1d(32, 64, kernel_size=5, padding=2),
            nn.ReLU(),
            nn.MaxPool1d(2),
        )

        # Feature classifier
        self.classifier = nn.Sequential(
            nn.Linear(64 * 8, 128),  # Adjust based on vertex count
            nn.ReLU(),
            nn.Dropout(0.3),
            nn.Linear(128, 32),
            nn.ReLU(),
            nn.Linear(32, 4)  # 4 feature types: hole, cut, notch, none
        )

        # Dimension placer (for detected features)
        self.dim_placer = nn.Sequential(
            nn.Linear(64 * 8 + 4, 128),  # Features + type
            nn.ReLU(),
            nn.Linear(128, 4)  # x, y, width/radius, height/depth
        )

    def forward(self, vertices):
        # Encode vertices
        features = self.feature_encoder(vertices)
        features_flat = features.view(features.size(0), -1)

        # Classify feature type
        feature_type = self.classifier(features_flat)

        # Place dimension based on feature
        feature_input = torch.cat([features_flat, feature_type], dim=1)
        dimension = self.dim_placer(feature_input)

        return feature_type, dimension
```

### Training on Feature Data

```python
def prepare_feature_training_data(extraction_data):
    """
    Build dataset with feature-level annotations
    """
    X_features = []
    Y_feature_types = []
    Y_feature_dims = []

    for part in extraction_data['parts']:
        vertices = part['geometric_data']['vertices']

        # Extract features
        features = extract_geometric_features(vertices)

        for feature in features:
            # Input: vertex neighborhood around feature
            vertex_patch = get_vertex_neighborhood(vertices, feature['center'], radius=100)
            X_features.append(vertex_patch)

            # Label: feature type
            Y_feature_types.append(feature['type'])

            # Label: dimension for this feature
            feature_dim = find_dimension_for_feature(feature, part['dimensions'])
            Y_feature_dims.append(feature_dim)

    return X_features, Y_feature_types, Y_feature_dims
```

### Deliverables

1. ✓ Enhanced extraction with feature detection
2. ✓ Feature dimensioner model
3. ✓ Training data with feature annotations
4. ✓ Feature dimension accuracy report
5. ✓ Combined overall + feature model

### Success Metrics

- **Feature Detection:** 85% precision/recall
- **Feature Dimensioning:** 75-80% accuracy
- **Combined Accuracy:** 85% overall (overall + feature dims)
- **Coverage:** 90% of features get dimensions

---

## Iterations 7-10: Coming Soon

**ITERATION 7:** Assembly Dimensions (multi-part relationships)
**ITERATION 8:** Leader Lines and Marks (annotations)
**ITERATION 9:** Production Deployment and Monitoring
**ITERATION 10:** Continuous Learning Pipeline

*Full details will be added after successful completion of Iterations 1-6*

---

## Resource Requirements

### Hardware

- **Development:** Modern laptop with GPU (RTX 3060+ recommended)
- **Training:** GPU with 8GB+ VRAM (can use CPU but slower)
- **Production:** Standard workstation (model inference is fast)

### Software

- Python 3.8+
- PyTorch 1.10+
- Tekla Structures 2023+
- .NET Framework 4.7+
- Development tools: VS Code, Git

### Data

- **Current:** 1 drawing (ST1006) with 462 parts ✓
- **Iteration 1-2:** Use current data
- **Iteration 3-4:** Add 5-10 more drawings
- **Iteration 5+:** Continuous data collection

### Team

- **AI Engineer:** 1 FTE (ML model development)
- **Tekla Developer:** 0.5 FTE (integration)
- **QA/Tester:** 0.25 FTE (validation)
- **Domain Expert (Detailer):** 0.25 FTE (feedback)

---

## Risk Management

### Technical Risks

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Insufficient training data | High | Medium | Extract more drawings, use augmentation |
| Poor generalization | High | Medium | Rigorous train/test split, diverse data |
| Integration bugs | Medium | High | Extensive testing, phased rollout |
| Slow inference | Low | Low | Model optimization, caching |

### Business Risks

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| User resistance | Medium | Medium | Emphasize time savings, involve early |
| Inaccurate dimensions | High | Low | Human review step, confidence scores |
| Maintenance burden | Medium | Low | Clean code, documentation, monitoring |

---

## Success Tracking

### KPIs per Iteration

| Iteration | Accuracy Target | Time Savings | User Satisfaction |
|-----------|----------------|--------------|------------------|
| 1 (Baseline) | 40-60% | 0% | N/A |
| 2 (ML v1) | 75-80% | 30-40% | "Helpful start" |
| 3 (Production) | 70-75% | 50% | "Saves time" |
| 4 (Improved) | 80-85% | 60% | "Very useful" |
| 5 (Robust) | 85% | 70% | "Reliable" |
| 6 (Features) | 85% | 75% | "Excellent" |

### Overall Timeline

```
Week 1-2:  Iteration 1-2 (Data prep + ML v1)
Week 3-4:  Iteration 3 (Production testing)
Week 5-6:  Iteration 4 (Improvements)
Week 7-8:  Iteration 5 (Data augmentation)
Week 9-12: Iteration 6 (Feature detection)
─────────────────────────────────────────
12 weeks to achieve 85% accuracy on overall + feature dimensions
```

---

## Next Steps

**IMMEDIATE (This Week):**
1. Review and approve this plan
2. Set up development environment
3. Start Iteration 1: Data preparation

**WEEK 1-2:**
1. Execute Iterations 1-2
2. Achieve baseline + ML v1
3. Report initial results

**ONGOING:**
1. Weekly progress reviews
2. Adjust plan based on results
3. Collect user feedback continuously

---

**Generated:** 2025-12-16
**Plan Owner:** AI Engineer (with Technical Director oversight)
**Status:** APPROVED FOR EXECUTION ✓
**Next Review:** After Iteration 2 completion
