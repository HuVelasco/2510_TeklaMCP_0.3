# TeklaMCP Development Guidelines
*Proven methodology from successful Phase 1-2.7 development*

## 1. File Management (CRITICAL)
- **ONE server file:** `Server/TeklaMCP/server.py` - NEVER create duplicates
- **ONE HTTP backend:** `Server/TeklaMCP/ProgramClean.cs`
- **NO file renaming** - Both Claude Code & Desktop use same paths

## 2. Version Control Methodology

### **Phased Branch Strategy**
```bash
# Pattern: improvements_[topic]_[phase]_[description]  
git checkout -b improvements_deduplication_01_code_cleanup
git checkout -b improvements_crud_02_operations
git checkout -b improvements_orphan_cleanup_025
git checkout -b improvements_tool_modernization_027
git checkout -b improvements_architecture_03_handlers
```

### **Commit Standards**
- **Detailed commit messages** with impact assessment
- **Document what changed** and why
- **Include test results** and verification
- **Never commit untested code**

### **Safety Practices**
- **Create branch before major changes** - always
- **Commit frequently** during development phases
- **Test before commit** - verify all functionality
- **NEVER force push or rebase main branches**

## 3. Server Architecture Requirements

### **CRUD Completeness**
- ✅ **Create:** Full object creation with all parameters
- ✅ **Read:** List, search, filter, detailed object info
- ✅ **Update:** Modify existing object properties  
- ✅ **Delete:** Safe removal with verification
- ✅ **Bulk:** Batch operations for efficiency

### **Agent-Friendly Design**
- **Direct parameters:** `start_x: float` not `start_point: str`
- **Minimal complexity:** Smart defaults, fewer required params
- **Clear naming:** Intuitive names AI agents understand
- **Consistent patterns:** Similar tools use same structure
- **Rich docs:** Examples and usage in docstrings

### **Response Standardization**
```python
# Use these helpers consistently
BuildSuccessResponse(data, message)
BuildErrorResponse(error, operation, details)
```

## 4. Code Quality Standards

### **Eliminate Repetition**
- **Extract helpers:** Common patterns into functions
- **Standardize handling:** Consistent parameter processing  
- **Remove duplicates:** Delete unused/orphan code immediately
- **Architectural cleanup:** Phase 3 achieved 66% reduction (1587→535 lines)

### **Maintainability Principles** 
- **Single responsibility** per method/endpoint
- **Endpoint-specific handlers** - eliminated monolithic switch statements
- **Configuration management** - centralized settings, no hard-coded values
- **Direct API calls** - no unnecessary abstraction
- **Comprehensive error handling** with context and operation details
- **Type safety** with proper Tekla types and input validation

## 5. Testing Protocol

### **Development Testing**
```bash
# 1. Start server
cd Server/TeklaMCP && dotnet run

# 2. Test HTTP endpoints directly  
curl -X GET "http://localhost:8080/test"
curl -X POST "http://localhost:8080/create-beam" -d '{...}'

# 3. Test MCP tools
# Use actual MCP tools to verify functionality

# 4. Document results
# Record what was tested and outcomes
```

### **Integration Testing**
- **Restart Claude Code** after tool signature changes
- **Test real workflows** with actual usage scenarios  
- **Verify round-trip** - create then read objects
- **Both Claude Code AND Desktop** if available

## 6. Tekla API Integration & C# Version Limitations

### **CRITICAL: Dynamic Code Uses C# 5.0 Compiler** ⚠️

**Generated code strings** (in `ProgramClean.cs`) run with **C# 5.0 legacy compiler**.

**See `docs/GENERATED_CODE_LIMITATIONS.md` for full details**

#### Quick Reference - C# 5.0 Restrictions:

❌ **NOT ALLOWED:**
```csharp
$"{variable}"           // String interpolation - C# 6.0
obj?.Property           // Null-conditional - C# 6.0
using Tekla.Structures  // Using directives in dynamic code
```

✅ **MUST USE:**
```csharp
"text " + variable                              // String concatenation
(obj != null) ? obj.Property : null            // Explicit null check
Tekla.Structures.Model.Model                   // Fully qualified names
```

#### ArrayList Collection Issue (CRITICAL):

❌ **WRONG:**
```csharp
var children = obj.GetChildren();  // Returns ArrayList
while (children.MoveNext())        // ERROR: ArrayList has no MoveNext
```

✅ **CORRECT:**
```csharp
var children = obj.GetChildren();
var enumerable = children as System.Collections.IEnumerable;
if (enumerable != null) {
    foreach (var child in enumerable) { /* process */ }
}
```

#### Where This Applies:
- `GenerateFullExtractionCode()` - Lines 1314+
- `GenerateDiscoveryModeCode()` - Lines 1229+
- All code inside `@"..."` string literals

### **Tekla API Integration**
- **Key namespaces:** `Tekla.Structures.Model`, `Tekla.Structures.Geometry3d`
- **Always commit changes:** `Model.CommitChanges()` after Insert/Modify
- **Reference guide:** See `@HugoNotes\07.Model-Object-Tekla-Info.md`
- **Priority objects:** Look for `*` markers in inheritance trees

## 7. Development Session Workflow
*The proven 5-phase methodology:*

1. **Analysis** - TodoWrite to plan, identify issues/improvements
2. **Implementation** - Focused changes with helper functions
3. **Testing** - HTTP + MCP verification before commit
4. **Documentation** - Detailed commit with impact assessment  
5. **Cleanup** - Remove orphan code, eliminate repetition

### **Phase Results Achieved:**
- **Phase 1:** Code deduplication (54 lines eliminated)
- **Phase 2:** CRUD operations implementation  
- **Phase 2.5:** Orphan code cleanup (880+ lines removed)
- **Phase 2.7:** Tool modernization (agent-friendly parameters)
- **Phase 3:** Clean architecture (1052 lines eliminated, endpoint handlers)

### **Communication Style**
- **Direct & technical** - clear problem identification
- **Immediate implementation** - solve as discussed
- **Comprehensive testing** - verify everything works
- **Document everything** - commits tell full story

## 8. Critical Anti-Patterns
❌ **Never Do:**
- Create multiple server file versions
- Complex string parsing (use direct numeric params)
- Skip `Model.CommitChanges()` after Tekla operations
- Test only partial functionality  
- Commit without testing
- Large monolithic code blocks
- Inconsistent tool parameter patterns
- Missing documentation of changes

## 9. Documentation Requirements
- **All changes documented** in commit messages
- **Test results recorded** - what worked/failed
- **Tool signatures updated** when parameters change
- **Architecture decisions** captured with rationale
- **Change impact** clearly communicated

## 10. 🌉 Bridge Coordination (If Used)

### **Specialized Command File**
For bridge operations with external systems:
```
.claude/bridge_coordination.command
```

### **General Rule**
- When user mentions bridge/import/export operations → Read relevant .claude/command file
- Keep general methodology in CLAUDE.md
- Keep specific implementations in specialized command files

---
*This methodology successfully delivered **5 major phases** with complete architectural transformation:*
- **90% CRUD completeness** 
- **66% code reduction** (1587→535 lines)
- **Agent-friendly modernization** with direct parameters
- **Clean endpoint-handler architecture** 
- **Configuration management** and input validation
- **Zero technical debt** - eliminated all orphan code