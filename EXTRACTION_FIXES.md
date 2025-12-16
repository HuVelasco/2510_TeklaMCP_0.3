# Drawing Extraction Enhancement Fixes

## Test Results Summary

**Test Date:** 2025-12-11
**Extraction File:** `tekla_extraction_20251211_214922.jsonl`
**Drawings Tested:** 2 drawings (ST1006, ST1000)
**Total Objects:** 1,981 objects

### Enhancement Results:

1. ✅ **Model Object Linkage** - WORKING (934/1981 = 47.1%)
2. ❌ **Bounding Boxes (AABB)** - FAILED (0/1981 = 0%)
3. ❌ **Dimension Text Positions** - FAILED (0/1981 = 0%)

---

## Issue 1: Bounding Box Extraction

### Root Cause
The current code (lines 1659-1700) attempts to get `MinimumPoint` and `MaximumPoint` properties from the **drawing** Part's solid. However, these properties only exist on **MODEL** object solids, not drawing solids.

### Solution from TeklaDrawingExamples.cs

Looking at lines 2148-2171 in the examples, the correct approach is to use the drawing object's bounding box **interfaces**:

```csharp
// From TeklaDrawingExamples.cs lines 2148-2171
private static void DrawBBForObject(DrawingObject drawingObject)
{
    if (drawingObject is IAxisAlignedBoundingBox)
    {
        drawingObject.Select();
        RectangleBoundingBox currentBB = (drawingObject as IAxisAlignedBoundingBox)!.GetAxisAlignedBoundingBox();
        Console.WriteLine("Drawing AABB for {0}", drawingObject.GetType().Name);
    }

    if (drawingObject is IObjectAlignedBoundingBox)
    {
        drawingObject.Select();
        RectangleBoundingBox currentBB = (drawingObject as IObjectAlignedBoundingBox)!.GetObjectAlignedBoundingBox();
        Console.WriteLine("Drawing OABB for {0}: Width: {1}, Height: {2}, Angle: {3}",
            drawingObject.GetType().Name,
            Distance.PointToPoint(currentBB.LowerRight, currentBB.LowerLeft),
            Distance.PointToPoint(currentBB.UpperLeft, currentBB.LowerLeft),
            currentBB.AngleToAxis);
    }
}
```

### Fixed Code for ProgramClean.cs (lines 1659-1700)

**REPLACE THIS:**
```csharp
// ENHANCEMENT: Extract bounding box (AABB) data inspired by tekladrawingexamples.cs
// MOVED INSIDE solid scope to fix compilation error
try
{
    var minPointProp = solid.GetType().GetProperty("MinimumPoint");
    var maxPointProp = solid.GetType().GetProperty("MaximumPoint");
    // ... rest of old code that doesn't work
}
catch { /* Skip bounding box if extraction fails */ }
```

**WITH THIS:**
```csharp
// ENHANCEMENT: Extract bounding box using drawing interfaces (tekladrawingexamples.cs:2148-2171)
// FIX: Use IAxisAlignedBoundingBox and IObjectAlignedBoundingBox interfaces
try
{
    // Check if this Part implements IAxisAlignedBoundingBox
    if (obj is Tekla.Structures.Drawing.IAxisAlignedBoundingBox)
    {
        var aabbInterface = obj as Tekla.Structures.Drawing.IAxisAlignedBoundingBox;
        var bbox = aabbInterface.GetAxisAlignedBoundingBox();
        if (bbox != null)
        {
            partGeometry["bounding_box_aabb"] = new {
                lower_left = new { x = bbox.LowerLeft.X, y = bbox.LowerLeft.Y, z = bbox.LowerLeft.Z },
                upper_right = new { x = bbox.UpperRight.X, y = bbox.UpperRight.Y, z = bbox.UpperRight.Z },
                lower_right = new { x = bbox.LowerRight.X, y = bbox.LowerRight.Y, z = bbox.LowerRight.Z },
                upper_left = new { x = bbox.UpperLeft.X, y = bbox.UpperLeft.Y, z = bbox.UpperLeft.Z },
                type = "axis_aligned"
            };
        }
    }

    // Also extract Object-Aligned Bounding Box for rotated parts
    if (obj is Tekla.Structures.Drawing.IObjectAlignedBoundingBox)
    {
        var oabbInterface = obj as Tekla.Structures.Drawing.IObjectAlignedBoundingBox;
        var obbox = oabbInterface.GetObjectAlignedBoundingBox();
        if (obbox != null)
        {
            partGeometry["bounding_box_oabb"] = new {
                lower_left = new { x = obbox.LowerLeft.X, y = obbox.LowerLeft.Y, z = obbox.LowerLeft.Z },
                upper_right = new { x = obbox.UpperRight.X, y = obbox.UpperRight.Y, z = obbox.UpperRight.Z },
                lower_right = new { x = obbox.LowerRight.X, y = obbox.LowerRight.Y, z = obbox.LowerRight.Z },
                upper_left = new { x = obbox.UpperLeft.X, y = obbox.UpperLeft.Y, z = obbox.UpperLeft.Z },
                angle_to_axis = obbox.AngleToAxis,
                type = "object_aligned"
            };
        }
    }
}
catch (Exception ex)
{
    LogMessage($"BBOX_EXTRACTION_ERROR: {ex.Message}");
}
```

---

## Issue 2: Dimension Text Position Extraction

### Root Cause
The current code (lines 1526-1551) attempts to extract an `InsertionPoint` property from dimension objects. However, Tekla dimension objects don't have this property.

### What Dimensions Actually Have
From the test extraction output:
```json
{
  "type": "Tekla.Structures.Drawing.StraightDimension",
  "properties": {
    "StartPoint": "(-235.415, -26.988, 0.000)",
    "EndPoint": "(2767.334, -33.338, 0.000)",
    "Distance": "360.437133275036",
    "UpDirection": "(0.000, 1.000, 0.000)"
  }
}
```

### Solution
Extract the **actual available properties**: StartPoint, EndPoint, Distance, UpDirection.

Dimensions also implement the bounding box interfaces, which can give us the text frame position.

### Fixed Code for ProgramClean.cs (lines 1526-1551)

**REPLACE THIS:**
```csharp
// ENHANCEMENT: Extract InsertionPoint for dimension text placement (ML training feature)
try
{
    var insertionPointProp = obj.GetType().GetProperty("InsertionPoint");
    // ... rest of old code that doesn't work
}
catch { /* Skip if InsertionPoint not available */ }
```

**WITH THIS:**
```csharp
// ENHANCEMENT: Extract dimension placement data (ML training feature)
// FIX: Dimensions don't have InsertionPoint - use bounding box for text position
try
{
    // Extract dimension bounding box (text frame position)
    if (obj is Tekla.Structures.Drawing.IObjectAlignedBoundingBox)
    {
        var dimBBox = (obj as Tekla.Structures.Drawing.IObjectAlignedBoundingBox).GetObjectAlignedBoundingBox();
        if (dimBBox != null)
        {
            // The dimension text is positioned within this bounding box
            geometricData["dimension_text_frame"] = new {
                lower_left = new { x = dimBBox.LowerLeft.X, y = dimBBox.LowerLeft.Y },
                upper_right = new { x = dimBBox.UpperRight.X, y = dimBBox.UpperRight.Y },
                center = new {
                    x = (dimBBox.LowerLeft.X + dimBBox.UpperRight.X) / 2,
                    y = (dimBBox.LowerLeft.Y + dimBBox.UpperRight.Y) / 2
                },
                angle = dimBBox.AngleToAxis,
                type = "dimension_text_bounding_box"
            };
        }
    }
}
catch { /* Skip if dimension bounding box not available */ }
```

---

## Issue 3: RAM Usage Risk Analysis

### Current Implementation Risk: **MEDIUM to HIGH**

The current extraction code builds the entire dataset in memory before writing to file:

```csharp
var allDrawings = new System.Collections.Generic.List<object>();
// ... for each drawing ...
allDrawings.Add(drawingData);  // Accumulates in memory
// ... at the end ...
File.WriteAllText(outputFile, JsonConvert.SerializeObject(allDrawings));
```

### Risk Assessment

| Drawing Count | Objects/Drawing | Total Objects | Approx RAM Usage | Risk Level |
|--------------|-----------------|---------------|------------------|------------|
| 2 drawings | ~1,000 | 2,000 | ~10 MB | ✅ LOW |
| 10 drawings | ~1,000 | 10,000 | ~50 MB | ⚠️ MEDIUM |
| 50 drawings | ~1,000 | 50,000 | ~250 MB | ⚠️ MEDIUM |
| 100 drawings | ~1,000 | 100,000 | ~500 MB | 🔴 HIGH |
| 500 drawings | ~1,000 | 500,000 | ~2.5 GB | 🔴 CRITICAL |

**Test Results:** 2 drawings with 1,981 objects created a 922KB JSONL file. Extrapolating:
- 100 drawings ≈ 46 MB JSONL file, ~500 MB RAM during extraction
- 500 drawings ≈ 230 MB JSONL file, ~2.5 GB RAM during extraction

### Solution: Streaming JSONL Writer

**Current problematic pattern:**
```csharp
var allDrawings = new List<object>();  // Accumulates in RAM
foreach (var drawing in drawings)
{
    var drawingData = ExtractDrawing(drawing);
    allDrawings.Add(drawingData);  // Growing memory footprint
}
File.WriteAllText(outputFile, JsonConvert.SerializeObject(allDrawings));
```

**Fixed streaming pattern:**
```csharp
using (var writer = new System.IO.StreamWriter(outputFile, false, System.Text.Encoding.UTF8))
{
    foreach (var drawing in drawings)
    {
        var drawingData = ExtractDrawing(drawing);
        // Write immediately, then discard from memory
        writer.WriteLine(JsonConvert.SerializeObject(drawingData));
        drawingData = null;  // Explicit disposal hint
    }
}
// Memory footprint: Only ONE drawing at a time
```

### Benefits of Streaming Approach:
1. **Constant Memory:** ~50 MB regardless of drawing count
2. **Crash Recovery:** Partial files are still valid JSONL
3. **Early Access:** Can start processing data before extraction completes
4. **Scalability:** Can handle 10,000+ drawings without issues

---

## Implementation Priority

1. **CRITICAL:** Fix bounding box extraction (affects 462 Part objects per drawing)
2. **HIGH:** Fix dimension position extraction (affects 190+ dimension objects per drawing)
3. **MEDIUM:** Implement streaming JSONL writer (prevents future crashes)

## Testing Plan

After implementing fixes:

1. **Unit Test:** Extract 2 drawings, verify all 3 enhancements present
2. **Stress Test:** Extract 10+ drawings, monitor RAM usage
3. **Type Test:** Test with assembly drawings, GA drawings, and part drawings
4. **Validation:** Run `verify_enhancements.py` script to confirm data quality

---

## Expected Results After Fixes

| Enhancement | Current Coverage | Expected Coverage |
|------------|------------------|-------------------|
| Model Linkage | 47.1% | 47.1% (no change - already working) |
| Bounding Boxes | 0.0% | ~23% (462 Part objects) |
| Dimension Positions | 0.0% | ~10% (190 dimension objects) |

**Total Enhanced Objects:** From 47.1% to ~80% coverage
