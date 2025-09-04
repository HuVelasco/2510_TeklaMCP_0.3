# TeklaMCP v0.3.0 - MCP Tools Reference

## Overview
This document provides comprehensive documentation for all 15 MCP tools available in TeklaMCP v0.3.0. These tools provide complete CRUD operations for Tekla Structures through Claude Code IDE.

## Tool Categories

### 🔧 Core Operations
Essential tools for basic Tekla integration

### 📊 CRUD Operations  
Complete Create, Read, Update, Delete functionality

### 🔄 Bulk Operations
Efficient handling of multiple objects

---

## 🔧 Core Operations

### 1. tekla_get_model_info
**Purpose**: Get information about the current Tekla Structures model

**Parameters**: None

**Returns**: JSON with model details
```json
{
  "success": true,
  "tekla_connected": true,
  "model_name": "New model",
  "model_path": "C:\\TeklaStructuresModels\\New model",
  "server_architecture": "Phase 3 Clean Handlers"
}
```

**Example Usage**:
```
Use tekla_get_model_info to check if Tekla is running and connected
```

**Error Cases**:
- Tekla Structures not running
- No model currently open
- Connection timeout

---

### 2. tekla_create_beam  
**Purpose**: Create structural beams with full parameter control

**Parameters**:
- `start_point` (string): Start coordinates "x y z" (required)
- `end_point` (string): End coordinates "x y z" (required)  
- `profile` (string): Steel profile designation (default: "W21X50")
- `material` (string): Material specification (default: "A992")
- `class_type` (string): Object class (default: "1")
- `name` (string): Object name (default: "BEAM")
- `position_depth` (string): Depth position "FRONT/MIDDLE/BEHIND" (default: "BEHIND")
- `position_plane` (string): Plane position "LEFT/MIDDLE/RIGHT" (default: "MIDDLE")
- `position_rotation` (string): Rotation position "TOP/MIDDLE/BOTTOM" (default: "TOP")

**Returns**: Beam creation confirmation with ID
```json
{
  "success": true,
  "message": "Beam created successfully",
  "beam_id": 3305582
}
```

**Example Usage**:
```
Use tekla_create_beam with:
- start_point="0 0 0" 
- end_point="6000 0 0"
- profile="W21X50"
- material="A992"
```

**Error Cases**:
- Invalid coordinate format
- Unknown profile designation
- Invalid material specification
- Tekla model not accessible

---

### 3. tekla_execute_code
**Purpose**: Execute custom C# code in Tekla environment for advanced operations

**Parameters**:
- `code` (string): C# code to execute (required)

**Returns**: Execution result
```json
{
  "success": true,
  "result": "Hello from Tekla",
  "compilation_successful": true
}
```

**Example Usage**:
```
Use tekla_execute_code with code:
"var model = new Model(); 
var beam = new Beam();
beam.StartPoint = new Point(0,0,0);
beam.EndPoint = new Point(3000,0,0);
beam.Profile.ProfileString = \"W14X90\";
beam.Insert();
model.CommitChanges();
return \"Beam created successfully\";"
```

**Important Notes**:
- Uses legacy C# compiler (C# 5.0 syntax)
- No string interpolation - use concatenation
- No null-conditional operators
- Full Tekla API access available

**Error Cases**:
- Compilation errors in provided code
- Runtime exceptions
- Tekla API access failures

---

## 📊 CRUD Operations (Phase 4)

### 4. tekla_list_objects
**Purpose**: List and filter structural objects in the model

**Parameters**:
- `object_type` (string): Filter by type "BEAM/COLUMN/ALL" (default: "ALL")
- `limit` (int): Maximum objects to return (default: 100)
- `offset` (int): Skip objects for pagination (default: 0)
- `filter_criteria` (string): Additional filter "profile:W18" (default: "")

**Returns**: Array of objects with basic info
```json
{
  "success": true,
  "objects": [
    {
      "id": 3305582,
      "type": "Beam",
      "profile": "W21X50",
      "material": "A992"
    }
  ],
  "total_count": 1
}
```

**Example Usage**:
```
Use tekla_list_objects with object_type="BEAM" limit=50
```

---

### 5. tekla_search_objects
**Purpose**: Advanced search with multiple criteria

**Parameters**:
- `query` (string): Search query (required)
- `object_types` (string): Comma-separated types (default: "BEAM,COLUMN")
- `search_properties` (string): Properties to search (default: "profile,material")
- `limit` (int): Maximum results (default: 50)

**Returns**: Array of matching objects
```json
{
  "success": true,
  "results": [
    {
      "id": 3305582,
      "type": "Beam", 
      "profile": "W21X50",
      "material": "A992",
      "match_property": "profile"
    }
  ],
  "search_query": "W21",
  "results_count": 1
}
```

**Example Usage**:
```
Use tekla_search_objects with:
- query="W21" 
- object_types="BEAM"
- search_properties="profile"
```

---

### 6. tekla_get_object_details
**Purpose**: Get comprehensive details about a specific object

**Parameters**:
- `object_id` (int): Tekla object ID (required)

**Returns**: Complete object information
```json
{
  "success": true,
  "object": {
    "id": 3305582,
    "type": "Beam",
    "profile": "W21X50",
    "material": "A992",
    "start_point": [0, 0, 0],
    "end_point": [6000, 0, 0],
    "class": "1",
    "name": "BEAM",
    "position": {
      "depth": "BEHIND",
      "plane": "MIDDLE", 
      "rotation": "TOP"
    }
  }
}
```

**Example Usage**:
```
Use tekla_get_object_details with object_id=3305582
```

**Error Cases**:
- Object ID not found
- Object no longer exists
- Insufficient permissions

---

### 7. tekla_update_object
**Purpose**: Modify properties of an existing object

**Parameters**:
- `object_id` (int): Object to update (required)
- `updates` (string): JSON string with properties (required)

**Updates JSON Format**:
```json
{
  "profile": "W24X68",
  "material": "A572",
  "class": "2",
  "name": "MAIN_BEAM"
}
```

**Returns**: Update confirmation
```json
{
  "success": true,
  "message": "Object updated successfully",
  "updated_properties": ["profile", "material"]
}
```

**Example Usage**:
```
Use tekla_update_object with:
- object_id=3305582
- updates='{"profile": "W24X68", "material": "A572"}'
```

---

### 8. tekla_delete_object
**Purpose**: Remove an object from the model

**Parameters**:
- `object_id` (int): Object to delete (required)

**Returns**: Deletion confirmation
```json
{
  "success": true,
  "message": "Object deleted successfully",
  "deleted_id": 3305582
}
```

**Example Usage**:
```
Use tekla_delete_object with object_id=3305582
```

**Error Cases**:
- Object ID not found
- Object cannot be deleted (dependencies)
- Model is locked/read-only

---

## 🔄 Bulk Operations

### 9. tekla_bulk_operations  
**Purpose**: Execute multiple operations in a single transaction

**Parameters**:
- `operations` (string): JSON array of operations (required)

**Operations Array Format**:
```json
[
  {
    "operation": "create_beam",
    "parameters": {
      "start_point": "0 0 0",
      "end_point": "6000 0 0",
      "profile": "W21X50"
    }
  },
  {
    "operation": "update_object", 
    "parameters": {
      "object_id": 3305582,
      "updates": {"profile": "W24X68"}
    }
  },
  {
    "operation": "delete_object",
    "parameters": {
      "object_id": 3305583
    }
  }
]
```

**Returns**: Bulk operation results
```json
{
  "success": true,
  "operations_completed": 3,
  "results": [
    {"operation": 0, "success": true, "result": "Beam created - ID: 3305584"},
    {"operation": 1, "success": true, "result": "Object updated"},
    {"operation": 2, "success": true, "result": "Object deleted"}
  ],
  "transaction_id": "bulk_20250903_211652"
}
```

**Example Usage**:
```
Use tekla_bulk_operations to create multiple beams and update existing ones in one transaction
```

**Benefits**:
- Atomic operations (all succeed or all fail)
- Improved performance for multiple changes
- Single model commit for all operations
- Transactional integrity

---

## 🔧 Legacy Tools (Modernized)

### 10. tekla_create_column
**Purpose**: Create vertical structural elements (specialized beam creation)

**Parameters**:
- `base_point` (string): Base coordinates "x y z" (required)
- `height` (string): Column height (default: "3000")
- `profile` (string): Column profile (default: "W14X90")  
- `material` (string): Material specification (default: "A992")
- `class_type` (string): Object class (default: "1")
- `name` (string): Object name (default: "COLUMN")

**Returns**: Column creation confirmation
```json
{
  "success": true,
  "message": "Column created successfully",
  "column_id": 3305585,
  "height": 3000
}
```

**Example Usage**:
```
Use tekla_create_column with:
- base_point="3000 3000 0"
- height="4000"
- profile="W14X90"
```

**Implementation Note**: Creates vertical beam from base_point to base_point + height

---

## 📋 HTTP Endpoint Mapping

| MCP Tool | HTTP Endpoint | Method |
|----------|---------------|---------|
| tekla_get_model_info | /tekla-info | GET |
| tekla_create_beam | /create-beam | POST |
| tekla_execute_code | /execute-code | POST |
| tekla_list_objects | /list-parts | GET |
| tekla_search_objects | /search-parts | POST |
| tekla_get_object_details | /get-part | GET |
| tekla_update_object | /update-part | POST |
| tekla_delete_object | /delete-part | POST |
| tekla_bulk_operations | /execute-code | POST |
| tekla_create_column | /execute-code | POST |

## 🔍 Error Handling Patterns

### Standard Error Response
```json
{
  "success": false,
  "error": "Error description",
  "operation": "operation_name",
  "details": {
    "error_type": "ValidationError",
    "context": "Additional context"
  }
}
```

### Common Error Types
- **ValidationError**: Invalid parameters or format
- **ConnectionError**: Tekla not accessible  
- **NotFoundError**: Object ID doesn't exist
- **ExecutionError**: Operation failed in Tekla
- **CompilationError**: C# code compilation failed

## 🧪 Testing Strategy

### Individual Tool Testing
1. Start Tekla Structures with open model
2. Start HTTP server (`dotnet run`)
3. Use MCP tools in Claude Code
4. Verify results in Tekla model

### Integration Testing  
1. Use tekla_create_beam to create object
2. Use tekla_list_objects to find created object
3. Use tekla_get_object_details to inspect object
4. Use tekla_update_object to modify object
5. Use tekla_delete_object to remove object

### Error Testing
1. Test with Tekla closed (connection errors)
2. Test with invalid parameters (validation errors)
3. Test with non-existent IDs (not found errors)
4. Test with malformed JSON (parsing errors)

## 📈 Performance Guidelines

### Best Practices
- Use bulk operations for multiple changes
- Limit list operations with reasonable limits
- Use specific object types for better filtering
- Cache object IDs for repeated operations

### Performance Metrics
- Simple operations: < 100ms
- Bulk operations: < 500ms for 10 objects
- Search operations: < 200ms for 100 objects
- Memory usage: < 50MB additional overhead

---

**MCP Tools Reference v0.3.0**  
*Complete CRUD Operations with Part-Based Tools*  
*Generated: 2025-09-03*