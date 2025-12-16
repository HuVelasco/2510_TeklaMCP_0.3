# Vertex Extraction - Next Steps

## Current Status (Completed)

### ✅ Issue 1: "Investigate the hang"
**Resolution:** Not a hang - extraction takes 7 minutes for 1086 objects

**Evidence:**
- Server logs show successful completion
- File created: `Server/TeklaMCP/extractions/tekla_extraction_*.jsonl` (594KB)
- MCP tool errors due to timeout, but extraction succeeds

**Performance Metrics:**
- Time: 7 minutes 17 seconds
- Objects: 1086 total (462 Parts)
- File size: 594KB
- Throughput: ~2.5 objects/second

### ✅ Issue 2: "Get vertices from solid"
**Implementation:** ProgramClean.cs lines 1685-1724

**Method Used:**
- Direct enumerator approach from TeklaDrawingExamples.cs (lines 814-903)
- Pattern: `FaceEnumerator → LoopEnumerator → VertexEnumerator`
- Extracts from MODEL Part solid (not drawing Part)

**Current Configuration:**
- Limit: 10 vertices per part (testing)
- Coordinates: Global model coordinates
- Storage: `partGeometry["vertices_model"]`
- Count tracking: `partGeometry["vertices_count"]`

**Code Structure:**
```csharp
var faceEnum = modelSolid.GetFaceEnumerator();
while (faceEnum.MoveNext() && modelVertices.Count < vertexLimit)
{
    var face = faceEnum.Current as Tekla.Structures.Solid.Face;
    // ... loop enumerator
    //     ... vertex enumerator
    //         modelVertices.Add(new { x, y, z });
}
```

## User Requirements - To Be Implemented

### 📋 Requirement 1: Remove Duplicate Vertices
**User Quote:** "make sure you remove the duplicate vertices as they are a projection"

**Reasoning:**
- Vertices appear multiple times when projected to 2D drawing views
- ML training needs unique vertices only (in global coords)
- Duplicates waste storage and processing time

**Implementation Plan:**
1. Add tolerance-based duplicate check (0.01mm)
2. Use pattern from TeklaDrawingExamples.cs lines 858-864:
```csharp
bool isDuplicate = false;
foreach (var existingVertex in uniqueVertices)
{
    if (Math.Abs(existingVertex.X - vertex.X) < 0.01 &&
        Math.Abs(existingVertex.Y - vertex.Y) < 0.01 &&
        Math.Abs(existingVertex.Z - vertex.Z) < 0.01)
    {
        isDuplicate = true;
        break;
    }
}
if (!isDuplicate) uniqueVertices.Add(vertex);
```

**Location:** ProgramClean.cs line ~1710
**Status:** Ready to implement

### 📋 Requirement 2: View Local Coordinates
**User Quote:** "we need them in the view coordinates for each view and once more without duplicates for the global coordinates"

**Two Outputs Required:**
1. **Global coordinates** (current) - unique vertices, no duplicates
2. **View coordinates** (new) - per-view transformation, can have duplicates

**Implementation Approaches:**

**Option A: Transform global to view** (User suggested)
- Extract once in global coordinates
- Transform using existing view transformation matrix
- More efficient (single extraction)
- User: "you can save some resources by converting the global coordinates to view workplane"

**Option B: Extract in view coordinates** (From examples)
- Use WorkPlaneHandler to set view plane
- Extract vertices in view coordinate system
- Pattern from TeklaDrawingExamples.cs lines 820-828

**User Requirement:** "make sure and assert that the local vertices are exactly the same as the transformed ones before implementing this way"

**Implementation Plan:**
1. Create verification test (`test_coordinate_transformation.cs` - already exists)
2. Extract BOTH ways and compare results
3. If results match → use transformation (more efficient)
4. If results differ → use WorkPlaneHandler extraction

**Test File:** `test_coordinate_transformation.cs` lines 1-217
**Status:** Test file ready, needs execution

### 📋 Requirement 3: Increase Vertex Limit
**Current:** 10 vertices per part (testing only)
**Recommended:** 100-500 vertices per part for ML training

**Considerations:**
- 462 parts × 500 vertices = 231,000 vertices
- File size impact: ~10-50MB additional
- Processing time: May increase extraction time significantly
- Memory: Need to ensure no memory issues

**Strategy:**
1. Test with 50 vertices per part first
2. Monitor file size and extraction time
3. Gradually increase if performance acceptable
4. Add vertex limit as configurable parameter

## Implementation Priority

### Phase 1: Verify Current Implementation ✅
- [x] Confirm vertex extraction works
- [x] Check extraction file for vertices
- [ ] **IN PROGRESS:** Waiting for extraction to complete (7 min)

### Phase 2: Add Duplicate Removal (High Priority)
**Why:** User explicitly requested, improves data quality
**Time:** 15 minutes
**Impact:** Reduces file size, cleaner ML data

**Steps:**
1. Add `uniqueVertices` list with tolerance check
2. Test with 10 vertex limit
3. Verify duplicates are removed
4. Commit changes

### Phase 3: Coordinate Transformation Testing (High Priority)
**Why:** User wants verification before implementation
**Time:** 30 minutes
**Impact:** Determines best approach for view coordinates

**Steps:**
1. Compile `test_coordinate_transformation.cs`
2. Run in Tekla with active drawing
3. Compare global vs view vertices
4. Document results
5. Choose implementation approach

### Phase 4: Implement View Coordinates (Medium Priority)
**Why:** Required for ML training (view-specific data)
**Time:** 1-2 hours depending on approach
**Impact:** Complete two-coordinate requirement

**Steps:**
1. If transformation works → add matrix transformation
2. If not → implement WorkPlaneHandler extraction
3. Store in `partGeometry["vertices_view"]`
4. Test with sample drawing
5. Commit changes

### Phase 5: Increase Limits & Optimize (Low Priority)
**Why:** Performance and completeness
**Time:** 1-2 hours
**Impact:** Full-resolution ML dataset

**Steps:**
1. Increase to 50 vertices, test performance
2. Increase to 100 vertices if acceptable
3. Increase to 500 vertices if needed
4. Add configurable limits
5. Optimize duplicate checking if slow

## Testing Strategy

### Unit Tests
- [ ] Verify vertices are extracted (count > 0)
- [ ] Verify duplicate removal works
- [ ] Verify coordinate transformation accuracy
- [ ] Verify performance with high limits

### Integration Tests
- [ ] Extract from assembly drawing
- [ ] Extract from GA drawing
- [ ] Extract from part/shop drawing
- [ ] Extract multiple drawings

### Performance Tests
- [ ] Measure extraction time vs vertex limit
- [ ] Measure file size vs vertex limit
- [ ] Monitor memory usage during extraction

## Expected Outcomes

### After Phase 2 (Duplicate Removal)
```json
{
  "vertices_model": [{x, y, z}, ...],
  "vertices_count": 234,  // Reduced from ~462*10 due to duplicates
  "vertices_unique": true
}
```

### After Phase 4 (View Coordinates)
```json
{
  "vertices_global": [{x, y, z}, ...],
  "vertices_global_count": 234,
  "vertices_view": [{x, y, z}, ...],
  "vertices_view_count": 462,  // More due to view projections
  "coordinate_systems": "both"
}
```

### After Phase 5 (Full Limits)
```json
{
  "vertices_global": [...],  // ~10,000-50,000 unique
  "vertices_global_count": 23456,
  "vertices_view": [...],     // ~100,000-250,000 with duplicates
  "vertices_view_count": 123456,
  "vertex_limit_per_part": 500
}
```

## Success Criteria

✅ **Phase 1:** Vertex extraction confirmed working
✅ **Phase 2:** No duplicate vertices in global coordinates
✅ **Phase 3:** Coordinate transformation verified
✅ **Phase 4:** Both global and view coordinates available
✅ **Phase 5:** Full-resolution dataset with 100-500 vertices/part

## Notes

**MCP Tool Error:**
- Ignore "ERROR: Unexpected error -" from MCP tool
- Check extraction files directly in `Server/TeklaMCP/extractions/`
- File naming: `tekla_extraction_YYYYMMDD_HHMMSS.jsonl`

**Performance:**
- Current: 7 minutes for 1086 objects
- With 500 vertices/part: Estimate 10-15 minutes
- File size: Current 594KB → Expect 5-10MB with full vertices

**User's Efficiency Suggestion:**
- Transform global → view using matrix (more efficient)
- But verify first that results are identical
