# Tekla MCP Inter-Agent Communication Requirements
*Version 1.0 - ETABS-Tekla Bridge Protocol*

## Executive Summary
This document defines the Tekla MCP Server's requirements for establishing robust communication protocols with the ETABS MCP agent. These requirements ensure seamless data exchange, validation, and synchronization between both systems.

## 1. UBIM JSON Data Format Requirements

### 1.1 Required Frame Properties
The Tekla MCP Server requires the following enhanced properties in the UBIM JSON format:

```json
{
  "frames": [{
    "id": "frame_unique_id",
    "guid": "ETABS_GUID",  // CRITICAL: For change tracking
    "type": "beam|column|brace",  // From GetDesignOrientation
    "geometry": {
      "start": {"x": 0, "y": 0, "z": 0, "point_id": "1"},
      "end": {"x": 5000, "y": 0, "z": 0, "point_id": "2"},
      "length": 5000
    },
    "properties": {
      "profile": "W21X50",
      "material": "A992",  // From GetMaterial, not guessed
      "story": "STORY1",
      "insertion_point": {
        "cardinal_point": 5,  // 1-11 mapping
        "mirror_2": false,
        "stiff_transform": true,
        "offset_i": [0, 0, 0],  // mm
        "offset_j": [0, 0, 0]   // mm
      }
    },
    "orientation": {
      "local_angle": 0,  // Frame rotation angle
      "design_orientation": "beam",  // Classification
      "rotation_x": 0,
      "rotation_y": 0,
      "rotation_z": 0
    }
  }]
}
```

### 1.2 Material Extraction Requirements
- **MUST** extract actual material from section properties using `GetMaterial()`
- **NEVER** guess material based on section naming conventions
- **FALLBACK** to "UNKNOWN" if material cannot be determined

### 1.3 Design Orientation Mapping
```python
DESIGN_ORIENTATION_MAP = {
    1: "column",
    2: "beam",
    3: "brace",
    4: "null",
    5: "other"
}
```

## 2. API Method Requirements

### 2.1 Required ETABS API Calls
The ETABS agent MUST implement proper extraction using:
- `cFrameObj.GetGUID()` - For unique identification
- `cFrameObj.GetInsertionPoint()` - For accurate positioning
- `cFrameObj.GetDesignOrientation()` - For frame classification
- `cPropFrame.GetMaterial()` - For material properties
- `cFrameObj.GetSection()` - For section properties

### 2.2 Coordinate System Standards
- **ALWAYS** export coordinates in mm (millimeters)
- **ALWAYS** use Global coordinate system for export
- **DOCUMENT** any local coordinate transformations

## 3. Communication File Structure

### 3.1 Command File Location
```
.claude/
├── etabs_tekla_bridge.command     # Main protocol file
├── last_export_manifest.json      # Export tracking
└── validation_log.json            # Validation results
```

### 3.2 Export Manifest Format
```json
{
  "export_id": "uuid",
  "timestamp": "ISO8601",
  "etabs_version": "v21.0.0",
  "tekla_compatible": "2024i",
  "model_hash": "md5_hash",
  "statistics": {
    "total_frames": 150,
    "beams": 100,
    "columns": 40,
    "braces": 10
  },
  "validation": {
    "passed": true,
    "warnings": [],
    "errors": []
  }
}
```

## 4. Validation Protocol

### 4.1 Pre-Export Validation (ETABS Side)
1. **Model State Check**
   - Verify model is saved
   - Check for analysis results if needed
   - Confirm units are set correctly

2. **Data Completeness**
   - All frames have valid profiles
   - All materials are defined
   - GUIDs are present for tracking

### 4.2 Import Validation (Tekla Side)
1. **Data Structure Validation**
   - JSON schema compliance
   - Required fields present
   - Data type verification

2. **Geometry Validation**
   - Non-zero length frames
   - Valid coordinate ranges
   - Profile availability in Tekla

### 4.3 Validation Response Format
```json
{
  "validation_result": {
    "status": "success|warning|error",
    "frame_count": 150,
    "issues": [
      {
        "frame_id": "B123",
        "type": "warning",
        "message": "Profile W36X300 mapped to W36X302"
      }
    ],
    "profile_mapping": {
      "unmapped": ["CUSTOM1"],
      "mapped": {"W36X300": "W36X302"}
    }
  }
}
```

## 5. Error Handling Protocol

### 5.1 Error Categories
- **CRITICAL**: Model connection failures, file corruption
- **ERROR**: Missing required data, invalid geometry
- **WARNING**: Profile mapping, material substitution
- **INFO**: Statistics, successful mappings

### 5.2 Error Communication Format
```json
{
  "error": {
    "code": "ETABS_001",
    "severity": "ERROR",
    "message": "Failed to extract material for frame B123",
    "context": {
      "frame_id": "B123",
      "profile": "W21X50",
      "operation": "GetMaterial"
    },
    "suggestion": "Check frame section property definition"
  }
}
```

## 6. Incremental Update Protocol

### 6.1 Change Detection
- Use GUID comparison for frame identification
- Track model hash for global changes
- Maintain change log between exports

### 6.2 Delta Export Format
```json
{
  "update_type": "incremental",
  "base_export_id": "previous_uuid",
  "changes": {
    "added": ["frame_ids"],
    "modified": ["frame_ids"],
    "deleted": ["frame_ids"]
  },
  "frames": [/* Only changed frames */]
}
```

## 7. Profile and Material Mapping

### 7.1 Profile Mapping Table
```json
{
  "profile_mappings": {
    "ETABS_to_Tekla": {
      "W36X300": "W36X302",
      "HSS4X4X1/4": "HSS4X4X.25"
    }
  }
}
```

### 7.2 Material Mapping Table
```json
{
  "material_mappings": {
    "A992Fy50": "A992",
    "A36": "A36",
    "CONC4000": "C30/37"
  }
}
```

## 8. Performance Requirements

### 8.1 Export Performance
- Support models with 10,000+ frames
- Chunk large exports (1000 frames per file)
- Provide progress reporting for long operations

### 8.2 Memory Management
- Stream large JSON files instead of loading entirely
- Clear temporary data after successful export
- Implement garbage collection for COM objects

## 9. Logging and Debugging

### 9.1 Log Entry Format
```json
{
  "timestamp": "ISO8601",
  "level": "INFO|WARNING|ERROR",
  "source": "ETABS|TEKLA",
  "operation": "export|import|validate",
  "message": "descriptive message",
  "data": {}
}
```

### 9.2 Debug Information
- Frame extraction statistics
- API call timings
- Memory usage metrics
- Validation results

## 10. Version Compatibility

### 10.1 Supported Versions
- ETABS: v19, v20, v21
- Tekla Structures: 2023, 2024, 2024i
- UBIM Format: 1.0.0

### 10.2 Version Negotiation
```json
{
  "compatibility_check": {
    "etabs_version": "21.0.0",
    "tekla_version": "2024i",
    "ubim_version": "1.0.0",
    "compatible": true
  }
}
```

## 11. Testing Protocol

### 11.1 Test Cases Required
1. **Basic Export**: 10 frames (beams, columns, braces)
2. **Complex Geometry**: Sloped frames, offsets
3. **Material Variety**: Steel, concrete, custom
4. **Large Model**: 1000+ frames
5. **Incremental Update**: Add/modify/delete operations

### 11.2 Test Result Format
```json
{
  "test_suite": "ETABS_TEKLA_BRIDGE",
  "timestamp": "ISO8601",
  "results": {
    "passed": 45,
    "failed": 2,
    "skipped": 3
  },
  "details": []
}
```

## 12. Documentation Requirements

### 12.1 Required Documentation
- API method usage examples
- Error code reference
- Profile/material mapping guide
- Troubleshooting guide

### 12.2 Change Log Format
```markdown
## [Version] - Date
### Added
- New features
### Changed
- Modifications
### Fixed
- Bug fixes
### Deprecated
- Features to be removed
```

## 13. Security and Data Integrity

### 13.1 Data Validation
- Validate JSON against schema
- Check for SQL injection in strings
- Sanitize file paths

### 13.2 File Permissions
- Read-only access to ETABS model
- Write permissions for export directory
- Temp file cleanup after processing

## 14. Future Enhancements

### 14.1 Planned Features
- Connection design export
- Load combination transfer
- Analysis results mapping
- Rebar information for concrete

### 14.2 API Extensions
- Bi-directional sync
- Real-time update notifications
- Cloud storage integration

---

## Implementation Checklist for ETABS Agent

### Priority 1 (Critical)
- [ ] Implement `GetGUID()` for all frames
- [ ] Extract actual materials using `GetMaterial()`
- [ ] Use `GetDesignOrientation()` instead of geometric guessing
- [ ] Implement `GetInsertionPoint()` for accurate positioning

### Priority 2 (Important)
- [ ] Add validation before export
- [ ] Implement error handling with proper codes
- [ ] Create export manifest file
- [ ] Add profile/material mapping tables

### Priority 3 (Enhancement)
- [ ] Implement incremental updates
- [ ] Add progress reporting
- [ ] Create comprehensive test suite
- [ ] Add debug logging

---

## Notes for ETABS Agent Implementation

1. **NEVER** guess materials - always extract from model
2. **ALWAYS** include GUIDs for change tracking
3. **VALIDATE** data before export to prevent import failures
4. **LOG** all operations for debugging
5. **TEST** with various model complexities

This protocol ensures robust, reliable data exchange between ETABS and Tekla systems while maintaining data integrity and providing clear debugging capabilities.