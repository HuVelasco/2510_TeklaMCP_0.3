# Extraction Fixes - Implementation Plan
**Corrected Goal: Train ML to Auto-Detail Drawings**

**Date:** 2025-12-16
**Status:** PLANNING COMPLETE - READY TO IMPLEMENT

---

## Corrected Problem Statement

### What We're Actually Training

**INPUT:** Un-detailed drawing from Tekla
- Front view created (default Tekla template)
- Geometry visible (parts, connections, bolts)
- NO dimensions, marks, or annotations

**OUTPUT:** Fully detailed drawing (fabrication-ready)
- All dimensions placed correctly
- All marks added (part marks, assembly marks)
- All leader lines and annotations
- Ready for fabrication shop

**TRAINING DATA:**
- Each drawing = 1 complete training sample
- ST1006 example = 42 parts + 254 dimensions + 83 marks + 200 leader lines
- Learn: "Given geometry → Place all detailing"

### Why This Is Better

**Original (Wrong) Understanding:**
- "Train to classify/dimension individual parts"
- Need 200+ parts minimum
- Complex part-by-part prediction

**Corrected Understanding:**
- "Train to detail complete drawings"
- Each drawing is full sample (ALL drawings in model!)
- More realistic: detailers work on whole drawings, not isolated parts

---

## Required Fixes

### Fix 1: Extraction Bugs (Critical)

**Bug 1.1: Views Array Empty**
- Location: `ProgramClean.cs` line ~1389
- Issue: Views extracted but not written to output array
- Fix: Ensure proper serialization

**Bug 1.2: parent_view_id Not Set**
- Location: `ProgramClean.cs` line ~1427
- Issue: ReferenceEquals doesn't work for view objects
- Fix: Use View.Identifier instead

**Bug 1.3: 2D→3D Transformation Missing**
- Location: Need new function
- Issue: Dimensions only have 2D coordinates
- Fix: Transform using view matrix

### Fix 2: Drawing Metadata (Critical)

**Add Drawing-Level Metadata:**
```json
{
  "drawing_mark": "ST1006",
  "drawing_type": "assembly",  // NEW: assembly, part, fabrication
  "assembly_prefix": "ST",      // NEW: ST, B, C, etc.
  "part_type": "stair",         // NEW: stair, beam, column, plate, etc.
  "drawing_purpose": "fabrication",  // NEW: fabrication, erection, shop
  "metadata": {
    "drawing_title": "STAIR ASSEMBLY TYPICAL",
    "drawing_size": "A1",
    "scale": "1:20",
    "revision": "2"
  }
}
```

**Why Needed:**
- Drawing type affects detailing rules
- Assembly prefix helps categorize
- Part type affects dimension placement patterns
- ML model can learn type-specific rules

### Fix 3: Part Classification (Important)

**Add Part Type Classification:**
```json
{
  "model_object_id": "673629780",
  "type": "Part",
  "part_classification": {
    "category": "beam",           // NEW: beam, column, plate, stair_component, etc.
    "inferred_from": "part_name", // NEW: How we determined this
    "part_name": "ST-B1",         // NEW: Actual part name from model
    "assembly_mark": "ST1006",    // NEW: Which assembly this belongs to
    "piece_mark": "B1"            // NEW: Individual piece mark
  }
}
```

**Classification Rules:**
```
Part Name Prefix → Category
- C*, COL*       → column
- B*, BM*        → beam
- PL*, P*        → plate
- ST-*           → stair_component
- BR*, BRACE*    → brace
- GIRT*, G*      → girt
- HP*, HSS*      → shape_identifier
```

### Fix 4: Complete Relationships (Important)

**Ensure All Relationship Types Captured:**
```json
{
  "relationships": {
    "explicit_links": [...],        // From GetRelatedObjects()
    "spatial_proximity": [...],     // Parts near each other
    "dimension_targets": [...],     // What each dimension measures
    "mark_references": [...],       // Which part each mark refers to
    "leader_line_targets": [...]    // What each leader line points to
  }
}
```

---

## Implementation Steps

### STEP 1: Fix Extraction Bugs

**File:** `Server/TeklaMCP/ProgramClean.cs`

**Changes Needed:**

#### 1.1: Fix Views Array (lines 1389-1391)

**BEFORE:**
```csharp
drawingData["views"] = viewsList;
drawingData["view_count"] = viewsList.Count;
```

**AFTER:**
```csharp
// Ensure views are in SEPARATE top-level array
drawingData["views"] = viewsList;
drawingData["view_count"] = viewsList.Count;

// DEBUG: Print to verify
System.Console.WriteLine("Views extracted: " + viewsList.Count);
foreach (var v in viewsList)
{
    var vDict = v as System.Collections.Generic.Dictionary<string, object>;
    if (vDict != null)
    {
        System.Console.WriteLine("  - View: " + vDict.get("view_id", "unknown"));
    }
}
```

**Root Cause Investigation:**
- Check if JSONL serialization skips empty nested structures
- Verify views are written BEFORE objects in output
- Test with explicit JSON writer

#### 1.2: Fix parent_view_id (lines 1416-1445)

**BEFORE (BROKEN):**
```csharp
// Try using ReferenceEquals for object comparison
for (int i = 0; i < viewObjectMap.Count; i++)
{
    if (System.Object.ReferenceEquals(viewObjectMap[i], parentView))
    {
        objData["parent_view_id"] = "view_" + i.ToString();
        debugInfo.Add("found_match_at_" + i.ToString());
        break;
    }
}
```

**AFTER (FIXED):**
```csharp
// Use View.Identifier for reliable matching
try
{
    var identifierProp = parentView.GetType().GetProperty("Identifier");
    if (identifierProp != null)
    {
        var viewIdentifier = identifierProp.GetValue(parentView, null);
        if (viewIdentifier != null)
        {
            var viewIdValue = viewIdentifier.GetType().GetProperty("ID");
            if (viewIdValue != null)
            {
                var id = viewIdValue.GetValue(viewIdentifier, null);
                objData["parent_view_id"] = "view_" + id.ToString();
                debugInfo.Add("matched_by_identifier_" + id.ToString());
            }
        }
    }
}
catch (Exception ex)
{
    debugInfo.Add("identifier_error:" + ex.Message);
}
```

#### 1.3: Add 2D→3D Transformation (NEW FUNCTION)

**Add after line 1100:**
```csharp
private static object Transform2DPointTo3D(
    Tekla.Structures.Geometry3d.Point point2D,
    Tekla.Structures.Geometry3d.CoordinateSystem viewCS)
{
    // 3D = origin + x_2d*axis_x + y_2d*axis_y
    double x_3d = viewCS.Origin.X + point2D.X * viewCS.AxisX.X + point2D.Y * viewCS.AxisY.X;
    double y_3d = viewCS.Origin.Y + point2D.X * viewCS.AxisX.Y + point2D.Y * viewCS.AxisY.Y;
    double z_3d = viewCS.Origin.Z + point2D.X * viewCS.AxisX.Z + point2D.Y * viewCS.AxisY.Z;

    return new
    {
        x = System.Math.Round(x_3d, 2),
        y = System.Math.Round(y_3d, 2),
        z = System.Math.Round(z_3d, 2)
    };
}
```

**Apply to Dimensions (around line 1600 where dimensions are processed):**
```csharp
// For StraightDimension objects
if (obj.GetType().Name == "StraightDimension")
{
    var dim = obj as Tekla.Structures.Drawing.StraightDimension;
    if (dim != null)
    {
        // Get parent view for transformation
        var dimView = dim.GetView();
        if (dimView != null)
        {
            var viewCS = dimView.ViewCoordinateSystem;

            // Transform start/end points to 3D
            try
            {
                var dimPoints2D = new System.Collections.Generic.List<object>();
                var dimPoints3D = new System.Collections.Generic.List<object>();

                // Start point
                dimPoints2D.Add(new { x = dim.StartPoint.X, y = dim.StartPoint.Y, z = 0.0 });
                dimPoints3D.Add(Transform2DPointTo3D(dim.StartPoint, viewCS));

                // End point
                dimPoints2D.Add(new { x = dim.EndPoint.X, y = dim.EndPoint.Y, z = 0.0 });
                dimPoints3D.Add(Transform2DPointTo3D(dim.EndPoint, viewCS));

                geomData["dimension_points_2d"] = dimPoints2D;
                geomData["dimension_points_3d"] = dimPoints3D;
            }
            catch (Exception ex)
            {
                geomData["transform_error"] = ex.Message;
            }
        }
    }
}
```

### STEP 2: Add Drawing Metadata

**Add at beginning of extraction (after line 1280):**

```csharp
// Extract drawing-level metadata
var drawingMark = drawing.Mark;
var drawingTitle = drawing.Title;

// Infer drawing type from mark
var drawingType = InferDrawingType(drawingMark, drawing);
var assemblyPrefix = ExtractAssemblyPrefix(drawingMark);
var partType = InferPartType(drawingMark, assemblyPrefix);

drawingData["drawing_mark"] = drawingMark;
drawingData["drawing_type"] = drawingType;
drawingData["assembly_prefix"] = assemblyPrefix;
drawingData["part_type"] = partType;

drawingData["metadata"] = new System.Collections.Generic.Dictionary<string, object>
{
    ["drawing_title"] = drawingTitle ?? "Unknown",
    ["drawing_size"] = "A1",  // Could extract from layout if available
    ["scale"] = "Unknown",    // Could extract if available
    ["revision"] = "0"        // Could extract if available
};
```

**Add helper functions (after line 1000):**

```csharp
private static string InferDrawingType(string drawingMark, Tekla.Structures.Drawing.Drawing drawing)
{
    // Check if it's an assembly drawing (multiple main parts)
    var sheet = drawing.GetSheet();
    if (sheet == null) return "unknown";

    var allParts = new System.Collections.Generic.HashSet<string>();
    var allObjects = sheet.GetAllObjects();

    while (allObjects.MoveNext())
    {
        var obj = allObjects.Current;
        if (obj != null && obj.GetType().Name == "Part")
        {
            var part = obj as Tekla.Structures.Drawing.Part;
            if (part != null && part.ModelIdentifier != null)
            {
                allParts.Add(part.ModelIdentifier.ToString());
            }
        }
    }

    // If >10 unique parts, likely assembly
    if (allParts.Count > 10)
    {
        return "assembly";
    }
    else if (allParts.Count == 1)
    {
        return "part";
    }
    else if (allParts.Count > 1)
    {
        return "sub_assembly";
    }

    return "unknown";
}

private static string ExtractAssemblyPrefix(string drawingMark)
{
    // Extract prefix before numbers or hyphen
    // Examples: ST1006 → ST, B-123 → B, C12A → C
    if (string.IsNullOrEmpty(drawingMark))
        return "unknown";

    var match = System.Text.RegularExpressions.Regex.Match(
        drawingMark,
        @"^([A-Z]+)",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase
    );

    return match.Success ? match.Groups[1].Value.ToUpper() : "unknown";
}

private static string InferPartType(string drawingMark, string prefix)
{
    // Map prefix to part type
    var typeMap = new System.Collections.Generic.Dictionary<string, string>
    {
        {"C", "column"},
        {"COL", "column"},
        {"B", "beam"},
        {"BM", "beam"},
        {"PL", "plate"},
        {"P", "plate"},
        {"ST", "stair"},
        {"BR", "brace"},
        {"BRACE", "brace"},
        {"GIRT", "girt"},
        {"G", "girt"},
        {"HP", "shape_identifier"},
        {"HSS", "shape_identifier"},
        {"W", "shape_identifier"}
    };

    return typeMap.ContainsKey(prefix) ? typeMap[prefix] : "unknown";
}
```

### STEP 3: Add Part Classification

**Add when extracting Part objects (around line 1500):**

```csharp
if (obj.GetType().Name == "Part")
{
    var part = obj as Tekla.Structures.Drawing.Part;
    if (part != null)
    {
        // Get model part for more information
        var modelId = part.ModelIdentifier;
        if (modelId != null)
        {
            try
            {
                var model = new Tekla.Structures.Model.Model();
                var modelPart = model.SelectModelObject(modelId) as Tekla.Structures.Model.Part;

                if (modelPart != null)
                {
                    var partName = modelPart.Name ?? "UNKNOWN";
                    var assemblyMark = modelPart.AssemblyNumber?.Prefix ?? "UNKNOWN";
                    var pieceMark = modelPart.PartNumber?.Prefix ?? "UNKNOWN";

                    // Classify based on name
                    var category = ClassifyPart(partName, modelPart);

                    objData["part_classification"] = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["category"] = category,
                        ["inferred_from"] = "part_name",
                        ["part_name"] = partName,
                        ["assembly_mark"] = assemblyMark,
                        ["piece_mark"] = pieceMark,
                        ["profile"] = modelPart.Profile.ProfileString,
                        ["material"] = modelPart.Material.MaterialString
                    };
                }
            }
            catch (Exception ex)
            {
                objData["classification_error"] = ex.Message;
            }
        }
    }
}

private static string ClassifyPart(string partName, Tekla.Structures.Model.Part modelPart)
{
    var nameLower = partName.ToLower();

    // Check common naming patterns
    if (nameLower.Contains("column") || nameLower.StartsWith("c-") || nameLower.StartsWith("col"))
        return "column";
    if (nameLower.Contains("beam") || nameLower.StartsWith("b-") || nameLower.StartsWith("bm"))
        return "beam";
    if (nameLower.Contains("plate") || nameLower.StartsWith("pl") || nameLower.StartsWith("p-"))
        return "plate";
    if (nameLower.Contains("stair") || nameLower.StartsWith("st-"))
        return "stair_component";
    if (nameLower.Contains("brace") || nameLower.StartsWith("br-"))
        return "brace";
    if (nameLower.Contains("girt") || nameLower.StartsWith("g-"))
        return "girt";

    // Check orientation (vertical = column, horizontal = beam)
    try
    {
        var startPoint = modelPart.StartPoint;
        var endPoint = modelPart.EndPoint;
        var deltaZ = System.Math.Abs(endPoint.Z - startPoint.Z);
        var deltaXY = System.Math.Sqrt(
            System.Math.Pow(endPoint.X - startPoint.X, 2) +
            System.Math.Pow(endPoint.Y - startPoint.Y, 2)
        );

        if (deltaZ > deltaXY * 2)
            return "column";  // Mostly vertical
        else if (deltaXY > deltaZ * 2)
            return "beam";    // Mostly horizontal
    }
    catch { }

    return "unknown";
}
```

### STEP 4: Enhance Relationships

**Already mostly done, but ensure dimension targets are clear:**

```csharp
// When processing dimensions, explicitly link to measured parts
if (obj.GetType().Name == "StraightDimension")
{
    var dim = obj as Tekla.Structures.Drawing.StraightDimension;
    var relatedParts = new System.Collections.Generic.List<string>();

    // Get related objects
    var relatedEnum = dim.GetRelatedObjects();
    while (relatedEnum != null && relatedEnum.MoveNext())
    {
        var relObj = relatedEnum.Current;
        if (relObj != null && relObj.GetType().Name == "Part")
        {
            var relPart = relObj as Tekla.Structures.Drawing.Part;
            if (relPart != null && relPart.ModelIdentifier != null)
            {
                relatedParts.Add(relPart.ModelIdentifier.ToString());
            }
        }
    }

    objData["dimension_targets"] = new System.Collections.Generic.Dictionary<string, object>
    {
        ["measures_parts"] = relatedParts,
        ["dimension_type"] = dim.GetType().Name,
        ["is_overall_dimension"] = relatedParts.Count == 1,  // True if measures single part
        ["is_assembly_dimension"] = relatedParts.Count > 1   // True if between multiple parts
    };
}
```

---

## Testing Plan

### Test 1: Verify Fixes on ST1006

```bash
# Re-extract with fixes
cd Server/TeklaMCP
dotnet run

# In Tekla: Tools → Custom Tools → Extract Drawing Objects
# Drawing: ST1006

# Verify output
python verify_extraction.py extractions/tekla_extraction_*.jsonl
```

**Expected Results:**
```
✓ views array: 12 views with transformation matrices
✓ parent_view_id: Set on all objects
✓ dimension_points_3d: Present on all dimensions
✓ drawing_type: "assembly"
✓ assembly_prefix: "ST"
✓ part_type: "stair"
✓ part_classification: Present on all parts (beam, column, plate, etc.)
```

### Test 2: Extract Multiple Drawings

```bash
# Extract variety of drawing types
ST1006 (stair assembly)
B-123 (beam part drawing)
C-456 (column part drawing)
A-789 (general assembly)

# Verify diversity
python analyze_drawings.py extractions/*.jsonl
```

**Expected Results:**
```
Drawing Type Distribution:
- assembly: 2 drawings
- part: 2 drawings

Part Type Distribution:
- stair_component: 15 parts
- beam: 8 parts
- column: 4 parts
- plate: 3 parts

All drawings have:
✓ Complete metadata
✓ View transformations
✓ Part classifications
✓ 2D and 3D coordinates
```

### Test 3: Relationship Verification

```python
# Verify dimension → part relationships
python verify_relationships.py extractions/tekla_extraction_ST1006.jsonl

# Should show:
# - All dimensions linked to parts
# - Overall dimensions vs assembly dimensions identified
# - Marks linked to correct parts
# - Leader lines pointing to correct objects
```

---

## Next Steps After Fixes

### 1. Count Total Available Drawings

```python
# In Tekla model
drawing_handler = Tekla.Structures.Drawing.DrawingHandler()
all_drawings = drawing_handler.GetDrawings()

count = 0
while all_drawings.MoveNext():
    count += 1

print(f"Total drawings in model: {count}")
```

**This tells us our TRUE training dataset size!**

### 2. Extract All (or Sample)

**Strategy:**
- If <100 drawings: Extract ALL
- If 100-500 drawings: Extract 50-100 representative sample
- If >500 drawings: Extract 100 stratified by type

### 3. Build Training Pipeline

With correct data:
- Input: Drawing geometry (parts + connections + bolts)
- Target: Complete detailing (dimensions + marks + leader lines)
- Model learns: "How would human detailer detail this drawing?"

---

## Files to Create/Modify

1. **Server/TeklaMCP/ProgramClean.cs** - Apply all fixes
2. **verify_extraction.py** - Test script for verification
3. **analyze_drawings.py** - Analyze dataset diversity
4. **verify_relationships.py** - Check relationship completeness

Ready to implement?
