# ETABS-Tekla Bridge Coordination Commands
# Specialized command file for bridge operations only

## BRIDGE STATUS COORDINATION

### Read Current Status Before Development
```python
# Read live status of both agents
status_path = "../2509_EtabsMCP_0.3/.claude/bridge_status.json"
with open(status_path, 'r') as f:
    bridge_status = json.load(f)

etabs_status = bridge_status['etabs_agent']
current_export_format = etabs_status['latest_export_sample']
```

### Update Your Status When Implementing
```python
# Update Tekla agent status
bridge_status['tekla_agent']['status'] = "IMPLEMENTING"
bridge_status['tekla_agent']['capabilities']['import_frames']['implemented'] = True
```

## CURRENT ETABS MCP EXPORT FORMAT (v3.0)

Based on live status, ETABS MCP now exports:
- **GetGUID()** for change tracking
- **GetMaterial()** for actual materials (A992Fy50, not guessed)
- **GetDesignOrientation()** for frame classification (column/beam/brace)
- **GetInsertionPoint()** for positioning with cardinal points 1-11
- **Local rotation angles** export
- **Units in mm** (exact coordinates)

### Sample Current Export Structure
```json
{
  "id": "21",
  "guid": "a7bda08c-7156-46e8-8587-f83867fb0cd6",
  "type": "column",
  "geometry": {
    "start": {"x": 0.0, "y": 0.0, "z": 11887.2},
    "end": {"x": 0.0, "y": 0.0, "z": 15697.2}
  },
  "properties": {
    "profile": "W36X182",
    "material": "A992Fy50", 
    "story": "Story4",
    "insertion_point": {
      "cardinal_point": 10,
      "mirror_2": false,
      "offset_i": [0, 0, 0],
      "offset_j": [0, 0, 0]
    }
  },
  "orientation": {
    "local_angle": 0.0,
    "design_orientation": "column"
  }
}
```

## IMPLEMENTATION REQUIREMENTS

1. **Support GUID tracking** - Use ETABS GUIDs for change detection
2. **Handle insertion_point data** - Cardinal points 1-11 with offsets
3. **Apply local_angle** - Use rotation from ETABS orientation
4. **Use design_orientation** - ETABS classification (not geometric guessing)
5. **Process actual materials** - A992Fy50 format from GetMaterial()
6. **Create response reporting** - Update bridge_status.json after imports

## BRIDGE COORDINATION TRIGGERS

- When user mentions "ETABS", "transfer", or "bridge" → Read this command file
- When implementing import tools → Check latest_export_sample for exact format  
- When testing bridge → Update status with implementation progress
- When modifying tools → Update capabilities in bridge_status.json

## BRIDGE PARTNER STATUS

- **ETABS MCP v3.0** (Unified Bridge) - ONLINE  
- **Export capability**: 300 frames tested with 100% quality
- **Features implemented**: GUID, GetMaterial, GetDesignOrientation, GetInsertionPoint, Local angles
- **Tekla MCP Status**: AWAITING_IMPLEMENTATION

## NO COMPLEX MAPPINGS NEEDED

✅ **Both systems use same profiles** - W36X182 works in both
✅ **Materials are extractable** - A992Fy50 → A992 (simple conversion)  
✅ **Step-by-step development** - Only implement what ETABS MCP currently exports