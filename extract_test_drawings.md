# Test Extraction Guide
**Extract these drawings from your Tekla model**

## Assembly Drawings (6 total)

From Tekla: Tools → Custom Tools → Extract Drawing Objects

### 1. Stair Assembly (ST prefix)
- **Drawing:** ST1006 or any ST* drawing
- **Expected:** assembly, stair type, 30-50 unique parts

### 2. Beam Assembly (B prefix)
- **Drawing:** Find a B* or BM* assembly drawing
- **Expected:** assembly, beam type, 20-40 unique parts

### 3. Column Assembly (C prefix)
- **Drawing:** Find a C* or COL* assembly drawing
- **Expected:** assembly, column type, 10-30 unique parts

### 4. General Assembly (A prefix)
- **Drawing:** Find an A* assembly drawing
- **Expected:** assembly, mixed types, 30-60 unique parts

### 5. Connection/Detail Assembly
- **Drawing:** Any connection or detail assembly
- **Expected:** sub_assembly, 5-15 unique parts

### 6. Bracing Assembly (BR prefix)
- **Drawing:** Find a BR* or BRACE* drawing (if available)
- **Expected:** assembly, brace type

## Part Drawings (5 total)

### 7. Single Beam
- **Drawing:** Individual beam piece mark
- **Expected:** part type, beam category, 1 unique part

### 8. Single Column
- **Drawing:** Individual column piece mark
- **Expected:** part type, column category, 1 unique part

### 9. Base Plate
- **Drawing:** Individual plate piece mark
- **Expected:** part type, plate category, 1 unique part

### 10. Stair Component
- **Drawing:** Individual stair part (stringer, pan, etc.)
- **Expected:** part type, stair_component category, 1 unique part

### 11. Connection Plate/Gusset
- **Drawing:** Small connection part
- **Expected:** part type, plate category, 1 unique part

## Extraction Process

For EACH drawing above:
1. In Tekla: Tools → Custom Tools → Extract Drawing Objects
2. Enter drawing mark
3. Discovery Mode: **False**
4. Max Drawings: 1
5. Click Execute
6. Wait for completion

Files will be saved to: `Server/TeklaMCP/extractions/`

## After Extraction

Run analysis:
```bash
python analyze_all_extractions.py
```

This will generate:
- Individual summaries for each drawing
- Comparison table
- Visualizations
- Data relationship diagrams
