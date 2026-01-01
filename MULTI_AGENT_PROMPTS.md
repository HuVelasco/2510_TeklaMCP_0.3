# Multi-Agent Prompt System for Virtual Detailer Development
**Phased AI-Assisted Implementation Workflow**

This document contains isolated, executable prompts for AI agents to implement the Virtual Detailer ML system step by step. Each prompt is self-contained and builds on previous steps.

---

## How to Use This Document

1. **Execute prompts sequentially** - Each depends on previous completions
2. **One agent per prompt** - Spawn dedicated agent for each task
3. **Save outputs** - Store results in specified files for next agent
4. **Review between steps** - Validate before proceeding
5. **Iterate as needed** - Re-run with feedback if results unsatisfactory

---

## Agent Execution Template

```
Agent Name: [From prompt header]
Description: [From prompt]
Prompt: [Full prompt text]
Input Files: [Files to read]
Output Files: [Files to create]
Success Metrics: [How to verify completion]
```

---

# PHASE 1: DATA PREPARATION

---

## PROMPT 1.1: Data Pipeline Engineer

**Agent Name:** `data_pipeline_engineer`
**Role:** Build data processing pipeline to transform extraction data into ML-ready format

### Agent Description
```
You are a data pipeline engineer specializing in preparing geometric data for machine learning.
Your task is to parse Tekla extraction JSONL files and create clean, structured training datasets.
```

### Prompt

```
I need you to build a data processing pipeline for ML training on Tekla dimensioning data.

CONTEXT:
- I have extracted drawing data in JSONL format: Server/TeklaMCP/extractions/tekla_extraction_20251213_180910.jsonl
- This file contains 462 parts with geometric data and 254 dimensions
- I need to prepare this for supervised learning: part geometry (input) → dimension placements (output)

YOUR TASK:

1. CREATE: data_processing_pipeline.py with these functions:

   a) load_extraction_data(jsonl_file) -> dict
      - Load and parse JSONL file
      - Handle UTF-8 BOM encoding
      - Return parsed JSON data

   b) extract_part_features(part_obj) -> dict
      - Input: Part object from extraction
      - Extract: bounding box (min_x, min_y, min_z, max_x, max_y, max_z)
      - Calculate: width, height, depth
      - Extract: vertex_count
      - Return: Dict with 10 features

   c) extract_dimension_labels(dimension_obj) -> dict
      - Input: Dimension object from extraction
      - Extract: dimension_points (start, end coordinates)
      - Extract: dimension type (StraightDimension, etc.)
      - Return: Dict with dimension placement data

   d) build_training_dataset(extraction_data) -> (X, Y)
      - Match dimensions to parts using 'related_objects'
      - Build X: List of part features (462 samples)
      - Build Y: List of dimension labels per part
      - Handle parts with multiple dimensions
      - Return: (X, Y) as numpy arrays or lists

   e) save_training_data(X, Y, output_dir)
      - Save X as 'X_train.npy' or 'X_train.json'
      - Save Y as 'Y_train.npy' or 'Y_train.json'
      - Save metadata (sample counts, feature names)

2. CREATE: process_extraction.py (main script)
   - Load extraction file
   - Process into X, Y
   - Save to training_data/ directory
   - Print summary statistics

3. VALIDATION:
   - Verify 462 parts processed
   - Verify 254 dimensions extracted
   - Check for missing/null values
   - Print data shapes and sample

4. DELIVERABLES:
   - data_processing_pipeline.py
   - process_extraction.py
   - training_data/X_train.json
   - training_data/Y_train.json
   - training_data/metadata.json
   - data_processing_report.txt

RUN the scripts and verify output.
```

### Input Files
- `Server/TeklaMCP/extractions/tekla_extraction_20251213_180910.jsonl`

### Output Files
- `data_processing_pipeline.py`
- `process_extraction.py`
- `training_data/X_train.json`
- `training_data/Y_train.json`
- `training_data/metadata.json`
- `data_processing_report.txt`

### Success Metrics
- ✓ X shape: (462, 10) - 462 parts, 10 features each
- ✓ Y shape: (462, N) - 462 parts, N dimensions each
- ✓ No missing/null values
- ✓ All 254 dimensions accounted for
- ✓ Report shows feature distributions

---

## PROMPT 1.2: Baseline Model Engineer

**Agent Name:** `baseline_model_engineer`
**Role:** Build rule-based baseline model for dimension placement

### Agent Description
```
You are an ML engineer building baseline models. Your task is to create a simple rule-based
dimension placer that will serve as the performance baseline for ML models.
```

### Prompt

```
I need you to build a rule-based baseline model for automatic dimension placement on parts.

CONTEXT:
- I have training data ready: training_data/X_train.json, training_data/Y_train.json
- X contains part features: bounding box coordinates (min_x, min_y, min_z, max_x, max_y, max_z), width, height, depth, vertex_count
- Y contains actual human-placed dimension positions
- I need a baseline to compare ML models against

YOUR TASK:

1. CREATE: baseline_model.py with class BaselineDimensionPlacer:

   a) Method: place_dimensions(part_features) -> List[Dimension]
      RULE: Place 4 dimensions at bounding box edges
      - Top dimension: horizontal line at (min_x, max_y) to (max_x, max_y)
      - Bottom dimension: horizontal line at (min_x, min_y) to (max_x, min_y)
      - Left dimension: vertical line at (min_x, min_y) to (min_x, max_y)
      - Right dimension: vertical line at (max_x, min_y) to (max_x, max_y)

   b) Method: predict_batch(X) -> List[List[Dimension]]
      Apply place_dimensions to all parts in X

2. CREATE: evaluate_model.py with functions:

   a) calculate_position_accuracy(pred_dims, true_dims, tolerance_mm=50)
      - Match predicted dimensions to true dimensions (by position)
      - Count matches within tolerance (50mm)
      - Return: accuracy percentage

   b) calculate_type_accuracy(pred_dims, true_dims)
      - Check if dimension type matches (horizontal vs vertical)
      - Return: accuracy percentage

   c) calculate_coverage(pred_dims, true_dims)
      - Check if all required dimensions are placed
      - Return: coverage percentage

   d) evaluate_model(model, X, Y) -> Dict[str, float]
      - Run model on X
      - Compare with Y
      - Calculate all metrics
      - Return: {'position_acc': 0.XX, 'type_acc': 0.XX, 'coverage': 0.XX, 'overall': 0.XX}

3. CREATE: run_baseline.py (main script):
   - Load training data
   - Create BaselineDimensionPlacer
   - Run predictions
   - Evaluate against ground truth
   - Print results

4. EXPECTED RESULTS:
   - Position Accuracy: 40-60% (simple rules won't match human exactly)
   - Type Accuracy: 80-90% (easy to get orientation right)
   - Coverage: 100% (always place 4 dimensions)
   - Overall Score: 50-70%

5. DELIVERABLES:
   - baseline_model.py
   - evaluate_model.py
   - run_baseline.py
   - baseline_results.json (with metrics)
   - baseline_report.txt

RUN the scripts and report baseline performance.
```

### Input Files
- `training_data/X_train.json`
- `training_data/Y_train.json`

### Output Files
- `baseline_model.py`
- `evaluate_model.py`
- `run_baseline.py`
- `baseline_results.json`
- `baseline_report.txt`

### Success Metrics
- ✓ Baseline runs without errors
- ✓ Produces predictions for all 462 parts
- ✓ Position accuracy: 40-60%
- ✓ Type accuracy: 80-90%
- ✓ Overall score: 50-70%

---

# PHASE 2: ML MODEL DEVELOPMENT

---

## PROMPT 2.1: Neural Network Engineer

**Agent Name:** `neural_network_engineer`
**Role:** Build and train PyTorch neural network for dimension placement

### Agent Description
```
You are a deep learning engineer specializing in PyTorch. Your task is to design and train
a neural network that outperforms the baseline model for dimension placement.
```

### Prompt

```
I need you to build a PyTorch neural network to predict dimension placements from part geometry.

CONTEXT:
- Training data ready: training_data/X_train.json, Y_train.json
- Baseline performance: 50-70% overall accuracy
- Target: 75-80% accuracy (beat baseline by 20+ points)
- Input: 10 features (bounding box + dimensions)
- Output: 16 values (4 dimensions × 4 coordinates each)

YOUR TASK:

1. CREATE: models/dimension_placer.py with class DimensionPlacer(nn.Module):

   ARCHITECTURE:
   - Input layer: 10 features
   - Hidden layer 1: 64 neurons, ReLU, Dropout(0.2)
   - Hidden layer 2: 64 neurons, ReLU, Dropout(0.2)
   - Output layer: 16 values (4 dims × [x1, y1, x2, y2])

   Methods:
   - __init__(input_dim=10, hidden_dim=64, output_dim=16)
   - forward(x) -> predictions

2. CREATE: train_model.py with functions:

   a) prepare_data(X, Y):
      - Split: 70% train, 15% validation, 15% test
      - Convert to PyTorch tensors
      - Normalize features (StandardScaler)
      - Return: train_loader, val_loader, test_loader

   b) train_epoch(model, train_loader, optimizer, criterion):
      - One epoch of training
      - Batch processing
      - Return: average loss

   c) validate(model, val_loader, criterion):
      - Validation without gradient updates
      - Return: validation loss and accuracy

   d) train_model(model, train_loader, val_loader, epochs=100):
      - Training loop with validation
      - Early stopping (patience=15)
      - Save best model checkpoint
      - Return: trained model, training history

3. CREATE: run_training.py (main script):
   - Load and prepare data
   - Create model
   - Train with Adam optimizer (lr=0.001)
   - Use MSE loss function
   - Save trained model: models/dimension_placer_v1.pth
   - Plot training curves
   - Evaluate on test set

4. EXPECTED RESULTS:
   - Training loss: Decreasing over epochs
   - Validation loss: Similar to training (no overfitting)
   - Test accuracy: 75-80% position accuracy
   - Improvement: +20-30 points over baseline

5. DELIVERABLES:
   - models/dimension_placer.py
   - train_model.py
   - run_training.py
   - models/dimension_placer_v1.pth (trained weights)
   - training_curves.png
   - training_results.json
   - training_report.txt

TRAIN the model and report results compared to baseline.
```

### Input Files
- `training_data/X_train.json`
- `training_data/Y_train.json`
- `baseline_results.json` (for comparison)

### Output Files
- `models/dimension_placer.py`
- `train_model.py`
- `run_training.py`
- `models/dimension_placer_v1.pth`
- `training_curves.png`
- `training_results.json`
- `training_report.txt`

### Success Metrics
- ✓ Model trains without errors
- ✓ Training loss decreases consistently
- ✓ No overfitting (val loss close to train loss)
- ✓ Test accuracy: 75-80% position accuracy
- ✓ Beats baseline by 20+ percentage points

---

## PROMPT 2.2: Model Evaluation Engineer

**Agent Name:** `model_evaluation_engineer`
**Role:** Comprehensive evaluation and comparison of baseline vs ML model

### Agent Description
```
You are an ML evaluation specialist. Your task is to perform comprehensive evaluation
of the trained model and create detailed comparison reports.
```

### Prompt

```
I need you to perform comprehensive evaluation of the ML model and compare it with the baseline.

CONTEXT:
- Baseline model: baseline_model.py with 50-70% accuracy
- ML model: models/dimension_placer_v1.pth with 75-80% target accuracy
- Test data: training_data/X_test.json, Y_test.json (from train/test split)

YOUR TASK:

1. CREATE: comprehensive_evaluation.py with functions:

   a) load_models():
      - Load baseline model (BaselineDimensionPlacer)
      - Load ML model (DimensionPlacer from checkpoint)
      - Return both models

   b) run_comprehensive_evaluation(model, X_test, Y_test):
      - Position accuracy (tolerance: 25mm, 50mm, 100mm)
      - Type accuracy (horizontal vs vertical)
      - Coverage (% parts with all dims)
      - Precision/Recall per dimension type
      - Error distribution analysis
      - Return: detailed metrics dict

   c) compare_models(baseline_results, ml_results):
      - Side-by-side comparison
      - Improvement calculations
      - Statistical significance tests
      - Return: comparison dict

   d) visualize_results(baseline_results, ml_results):
      - Bar charts comparing metrics
      - Error distribution plots
      - Confusion matrices
      - Save: evaluation_visualizations.png

   e) analyze_failure_cases(model, X_test, Y_test):
      - Find worst predictions (top 10)
      - Categorize errors (position vs type vs coverage)
      - Identify patterns in failures
      - Return: failure analysis report

2. CREATE: run_evaluation.py (main script):
   - Load both models
   - Run comprehensive evaluation
   - Compare results
   - Generate visualizations
   - Create detailed report

3. EXPECTED RESULTS:
   Baseline:
   - Position (50mm): 50-70%
   - Type: 80-90%
   - Coverage: 100%
   - Overall: 50-70%

   ML Model:
   - Position (50mm): 75-80%
   - Type: 90-95%
   - Coverage: 95-100%
   - Overall: 75-80%

   Improvement: +20-30 percentage points

4. DELIVERABLES:
   - comprehensive_evaluation.py
   - run_evaluation.py
   - evaluation_results.json (detailed metrics)
   - evaluation_visualizations.png
   - comparison_report.md (formatted report)
   - failure_cases_analysis.txt

RUN evaluation and create comprehensive comparison report.
```

### Input Files
- `baseline_model.py`
- `models/dimension_placer_v1.pth`
- `training_data/X_test.json`
- `training_data/Y_test.json`

### Output Files
- `comprehensive_evaluation.py`
- `run_evaluation.py`
- `evaluation_results.json`
- `evaluation_visualizations.png`
- `comparison_report.md`
- `failure_cases_analysis.txt`

### Success Metrics
- ✓ Both models evaluated on same test set
- ✓ ML model shows clear improvement
- ✓ Statistical significance confirmed
- ✓ Failure modes documented
- ✓ Professional report generated

---

# PHASE 3: PRODUCTION INTEGRATION

---

## PROMPT 3.1: Tekla Integration Engineer

**Agent Name:** `tekla_integration_engineer`
**Role:** Build C# integration tool to use ML model from Tekla Structures

### Agent Description
```
You are a Tekla API developer. Your task is to create a C# application that loads the trained
ML model and applies dimension predictions to Tekla drawings.
```

### Prompt

```
I need you to build a Tekla Structures integration tool that uses the trained ML model to automatically place dimensions on drawings.

CONTEXT:
- Trained ML model: models/dimension_placer_v1.pth
- Model API: Takes part features (10 values) → Returns dimension positions (16 values)
- Existing extraction code: Server/TeklaMCP/ProgramClean.cs
- Goal: Create tool that detailers can run on drawings

YOUR TASK:

1. CREATE: VirtualFitter/DimensionPlacer.cs with class:

   Methods:
   a) List<PartFeatures> ExtractPartsFromDrawing(string drawingMark)
      - Use Tekla API to get all parts from drawing
      - Extract bounding box for each part
      - Return list of part features (same format as training data)

   b) List<DimensionPrediction> CallMLModel(List<PartFeatures> parts)
      - Call Python ML model via subprocess or HTTP
      - Pass part features as JSON
      - Receive dimension predictions as JSON
      - Return predictions

   c) void PlaceDimensionsOnDrawing(string drawingMark, List<DimensionPrediction> predictions)
      - For each prediction:
        * Create StraightDimension in Tekla
        * Set start point, end point
        * Set distance, orientation
      - Commit changes to model

   d) void RunVirtualFitter(string drawingMark)
      - Main workflow:
        1. Extract parts from drawing
        2. Call ML model
        3. Place dimensions
        4. Report results

2. CREATE: VirtualFitter/PythonModelCaller.cs:
   - Helper class to call Python model
   - Handle subprocess execution
   - Handle JSON serialization
   - Error handling

3. CREATE: VirtualFitter/predict_dimensions.py:
   - Load trained model
   - Accept JSON input from C#
   - Run predictions
   - Return JSON output

4. CREATE: VirtualFitter/Program.cs (main entry point):
   - Parse command-line arguments
   - Call RunVirtualFitter
   - Print results

5. TESTING:
   - Create test drawing (ST1006-1)
   - Copy ST1006, remove dimensions
   - Run Virtual Fitter
   - Verify dimensions are placed

6. DELIVERABLES:
   - VirtualFitter/DimensionPlacer.cs
   - VirtualFitter/PythonModelCaller.cs
   - VirtualFitter/predict_dimensions.py
   - VirtualFitter/Program.cs
   - VirtualFitter/VirtualFitter.csproj
   - VirtualFitter/README.md (usage instructions)
   - integration_test_results.txt

BUILD the tool and TEST on ST1006-1 drawing.
```

### Input Files
- `models/dimension_placer_v1.pth`
- `Server/TeklaMCP/ProgramClean.cs` (reference)
- Tekla drawing: ST1006

### Output Files
- `VirtualFitter/DimensionPlacer.cs`
- `VirtualFitter/PythonModelCaller.cs`
- `VirtualFitter/predict_dimensions.py`
- `VirtualFitter/Program.cs`
- `VirtualFitter/VirtualFitter.csproj`
- `VirtualFitter/README.md`
- `integration_test_results.txt`

### Success Metrics
- ✓ Tool compiles without errors
- ✓ Successfully calls Python model
- ✓ Places dimensions on test drawing
- ✓ 70-75% accuracy on real drawings
- ✓ Zero crashes or Tekla errors

---

## PROMPT 3.2: Production Validation Engineer

**Agent Name:** `production_validation_engineer`
**Role:** Test ML system on real production drawings and collect metrics

### Agent Description
```
You are a QA engineer specializing in ML systems. Your task is to validate the Virtual Fitter
on real production drawings and measure its performance.
```

### Prompt

```
I need you to perform comprehensive production validation of the Virtual Fitter tool.

CONTEXT:
- Virtual Fitter tool built: VirtualFitter/
- Trained ML model: models/dimension_placer_v1.pth
- Target: 10 test drawings with human dimensions for comparison
- Success criteria: 70-75% accuracy on unseen drawings

YOUR TASK:

1. CREATE: validation/create_test_drawings.py:
   - Select 10 production drawings (diverse part types)
   - For each drawing:
     * Copy as [Drawing]-1 (e.g., ST1006 → ST1006-1)
     * Remove all dimensions (keep front view only)
     * Save as test drawing
   - Document: original vs test drawing mapping

2. CREATE: validation/run_production_tests.py:

   a) run_virtual_fitter_on_test_set(test_drawings):
      - For each test drawing:
        * Run Virtual Fitter: VirtualFitter.exe --drawing [Drawing]-1
        * Capture output and errors
        * Save AI-generated dimensions
      - Return: results per drawing

   b) compare_with_human_dimensions(test_drawing, original_drawing):
      - Extract human dimensions from original
      - Extract AI dimensions from test drawing
      - Compare using evaluation metrics:
        * Position accuracy (50mm tolerance)
        * Type accuracy
        * Coverage
        * False positives
      - Return: accuracy scores

   c) aggregate_results(all_results):
      - Average accuracy across all test drawings
      - Identify best/worst performing drawings
      - Categorize by part type/complexity
      - Return: aggregate metrics

3. CREATE: validation/analyze_production_failures.py:
   - Load all test results
   - Find failure patterns:
     * Which part types perform worst?
     * Which dimension types are problematic?
     * Are errors systematic or random?
   - Generate recommendations for improvement

4. CREATE: run_validation.py (main script):
   - Create test drawings
   - Run Virtual Fitter on all test drawings
   - Compare with human dimensions
   - Aggregate results
   - Analyze failures
   - Generate report

5. EXPECTED RESULTS:
   - Position accuracy: 70-75% (slightly lower than test set due to new data)
   - Type accuracy: 90%+
   - Coverage: 90-95%
   - Critical errors: 0 (no dimensions on wrong parts)
   - Time savings: 50-60% vs manual dimensioning

6. DELIVERABLES:
   - validation/create_test_drawings.py
   - validation/run_production_tests.py
   - validation/analyze_production_failures.py
   - run_validation.py
   - validation_results.json (detailed metrics per drawing)
   - production_validation_report.md
   - failure_patterns_analysis.txt
   - test_drawings_documentation.txt

RUN validation on 10 production drawings and report results.
```

### Input Files
- `VirtualFitter/` (built tool)
- `models/dimension_placer_v1.pth`
- 10 production drawings (to be selected)

### Output Files
- `validation/create_test_drawings.py`
- `validation/run_production_tests.py`
- `validation/analyze_production_failures.py`
- `run_validation.py`
- `validation_results.json`
- `production_validation_report.md`
- `failure_patterns_analysis.txt`
- `test_drawings_documentation.txt`

### Success Metrics
- ✓ 10 test drawings created successfully
- ✓ Virtual Fitter runs on all test drawings
- ✓ Average accuracy: 70-75%
- ✓ Zero critical errors
- ✓ Clear improvement recommendations

---

# PHASE 4: ITERATION & IMPROVEMENT

---

## PROMPT 4.1: Feature Engineering Specialist

**Agent Name:** `feature_engineering_specialist`
**Role:** Analyze failures and engineer new features to improve model performance

### Agent Description
```
You are an ML feature engineering expert. Your task is to analyze model failures, identify
patterns, and engineer new features that will improve accuracy.
```

### Prompt

```
I need you to analyze model failures and engineer enhanced features to improve accuracy from 75% to 85%.

CONTEXT:
- Current model accuracy: 75-80% on test set, 70-75% on production
- Failure analysis available: failure_patterns_analysis.txt
- Current features (10): min_x, min_y, min_z, max_x, max_y, max_z, width, height, depth, vertex_count

YOUR TASK:

1. CREATE: feature_engineering/analyze_failures.py:

   a) load_failure_cases(failure_file):
      - Load documented failures
      - Categorize by error type:
        * Position errors (wrong location)
        * Type errors (wrong orientation)
        * Coverage errors (missing dimensions)
      - Return categorized failures

   b) identify_failure_patterns(failures):
      - Statistical analysis of failures:
        * Do certain part types fail more?
        * Do large/small parts fail more?
        * Do complex geometries (high vertex count) fail more?
      - Correlation analysis: features vs errors
      - Return: pattern report

   c) suggest_new_features(pattern_report):
      - Based on patterns, suggest features like:
        * Aspect ratios (width/height, width/depth)
        * Part orientation (horizontal vs vertical)
        * Geometric complexity (vertex_count bins)
        * Part type classification (beam, plate, etc.)
      - Return: list of proposed features

2. CREATE: feature_engineering/enhanced_features.py:

   Implement enhanced feature extraction:

   def extract_enhanced_features(part_obj):
       basic = extract_part_features(part_obj)  # Original 10 features

       enhanced = {
           **basic,
           # Geometric complexity
           'bbox_volume': basic['width'] * basic['height'] * basic['depth'],
           'aspect_ratio_xy': basic['width'] / basic['height'] if basic['height'] > 0 else 0,
           'aspect_ratio_xz': basic['width'] / basic['depth'] if basic['depth'] > 0 else 0,
           'aspect_ratio_yz': basic['height'] / basic['depth'] if basic['depth'] > 0 else 0,

           # Part orientation
           'is_horizontal_beam': 1 if basic['width'] > max(basic['height'], basic['depth']) else 0,
           'is_vertical_column': 1 if basic['height'] > max(basic['width'], basic['depth']) else 0,

           # Shape classification
           'is_rectangular_beam': 1 if basic['vertex_count'] == 12 else 0,
           'is_simple_shape': 1 if basic['vertex_count'] <= 8 else 0,
           'is_complex_shape': 1 if basic['vertex_count'] >= 20 else 0,

           # Contextual (if available)
           'has_connections': 1 if part_obj.get('related_objects') else 0,
           'connection_count': len(part_obj.get('related_objects', []))
       }

       return enhanced  # Now 20 features total

3. CREATE: feature_engineering/retrain_with_enhanced_features.py:
   - Reload training data
   - Extract enhanced features (20 instead of 10)
   - Rebuild train/val/test split with new features
   - Save: training_data/X_train_enhanced.json, Y_train.json

4. CREATE: models/dimension_placer_v2.py:
   - Same architecture as v1 but:
     * Input layer: 20 features (was 10)
     * Hidden layers: 128 neurons (was 64) - more capacity needed
     * Batch normalization for stability
     * Deeper network (3 hidden layers instead of 2)

5. CREATE: retrain_model_v2.py:
   - Load enhanced training data
   - Train DimensionPlacer V2
   - Compare with V1 results
   - Save: models/dimension_placer_v2.pth

6. EXPECTED RESULTS:
   - Feature count: 10 → 20 features
   - Model accuracy: 75-80% → 80-85%
   - Improvement: +5-10 percentage points
   - Especially better on complex parts

7. DELIVERABLES:
   - feature_engineering/analyze_failures.py
   - feature_engineering/enhanced_features.py
   - feature_engineering/retrain_with_enhanced_features.py
   - models/dimension_placer_v2.py
   - retrain_model_v2.py
   - training_data/X_train_enhanced.json
   - models/dimension_placer_v2.pth
   - feature_engineering_report.md (analysis + new features + results)

RUN feature engineering and retrain model V2.
```

### Input Files
- `failure_patterns_analysis.txt`
- `training_data/X_train.json`
- `training_data/Y_train.json`
- `models/dimension_placer.py` (v1)

### Output Files
- `feature_engineering/analyze_failures.py`
- `feature_engineering/enhanced_features.py`
- `feature_engineering/retrain_with_enhanced_features.py`
- `models/dimension_placer_v2.py`
- `retrain_model_v2.py`
- `training_data/X_train_enhanced.json`
- `models/dimension_placer_v2.pth`
- `feature_engineering_report.md`

### Success Metrics
- ✓ 20 enhanced features extracted
- ✓ Model V2 trains successfully
- ✓ Accuracy improvement: +5-10 points
- ✓ Better performance on complex parts
- ✓ No degradation on simple parts

---

# ADDITIONAL PHASES (PROMPTS 5.1 - 10.1)

**Note:** Additional prompts for Phases 5-10 will be created after successful completion of Phases 1-4.

**Preview of upcoming prompts:**
- **5.1:** Data Augmentation Specialist (increase training data)
- **6.1:** Feature Detection Engineer (holes, cuts, notches)
- **7.1:** Assembly Dimension Specialist (multi-part relationships)
- **8.1:** Production Deployment Engineer (monitoring, logging)
- **9.1:** Continuous Learning Engineer (feedback loop)
- **10.1:** System Optimization Engineer (performance tuning)

---

# COORDINATION & WORKFLOW

## Inter-Agent Communication

Agents communicate via files:

```
Agent 1 (Data Pipeline) → Creates: training_data/
                       ↓
Agent 2 (Baseline Model) → Creates: baseline_results.json
                         ↓
Agent 3 (Neural Network) → Creates: models/dimension_placer_v1.pth
                         ↓
Agent 4 (Evaluation) → Creates: comparison_report.md
                     ↓
Agent 5 (Tekla Integration) → Creates: VirtualFitter/
                             ↓
Agent 6 (Validation) → Creates: validation_results.json
                     ↓
Agent 7 (Feature Engineering) → Creates: models/dimension_placer_v2.pth
```

## File Naming Conventions

- **Data:** `training_data/X_*.json`, `training_data/Y_*.json`
- **Models:** `models/dimension_placer_v{N}.pth`
- **Results:** `*_results.json`
- **Reports:** `*_report.{md|txt}`
- **Code:** `{function}_{agent}.py`

## Execution Order

1. **Sequential Execution:** Prompts must be executed in order (1.1 → 1.2 → 2.1 → ...)
2. **Validate Before Proceeding:** Check success metrics before next prompt
3. **Save All Outputs:** Keep all files for traceability
4. **Iterate If Needed:** Re-run prompt with adjustments if metrics not met

## Progress Tracking

Create `PROGRESS_LOG.md`:

```markdown
# Virtual Detailer Development Progress

## Phase 1: Data Preparation
- [ ] Prompt 1.1: Data Pipeline (Status: Not Started)
- [ ] Prompt 1.2: Baseline Model (Status: Not Started)

## Phase 2: ML Model Development
- [ ] Prompt 2.1: Neural Network (Status: Not Started)
- [ ] Prompt 2.2: Evaluation (Status: Not Started)

## Phase 3: Production Integration
- [ ] Prompt 3.1: Tekla Integration (Status: Not Started)
- [ ] Prompt 3.2: Production Validation (Status: Not Started)

## Phase 4: Iteration & Improvement
- [ ] Prompt 4.1: Feature Engineering (Status: Not Started)
```

---

# GETTING STARTED

## Step 1: Set Up Environment

```bash
# Create project structure
mkdir -p training_data models validation feature_engineering VirtualFitter

# Install dependencies
pip install torch numpy pandas scikit-learn matplotlib seaborn

# Verify extraction data exists
ls -lh Server/TeklaMCP/extractions/tekla_extraction_20251213_180910.jsonl
```

## Step 2: Execute First Prompt

```bash
# Copy Prompt 1.1 to Claude Code or your AI agent
# Wait for completion
# Verify outputs exist:
ls training_data/X_train.json
ls training_data/Y_train.json
ls data_processing_report.txt

# Review report
cat data_processing_report.txt
```

## Step 3: Continue Sequential Execution

Repeat for each prompt:
1. Read prompt
2. Provide to AI agent
3. Verify outputs
4. Review success metrics
5. Proceed to next prompt

---

**Generated:** 2025-12-16
**Document Owner:** Technical Director
**Status:** READY FOR EXECUTION
**First Agent:** data_pipeline_engineer (Prompt 1.1)
