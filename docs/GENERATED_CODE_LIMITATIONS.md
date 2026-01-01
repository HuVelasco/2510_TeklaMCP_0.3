# Generated Code C# Version Limitations

**CRITICAL: Dynamic Code Generation Uses C# 5.0 Compiler**

## Overview

The TeklaMCP server generates C# code dynamically that runs inside Tekla Structures. This code is **compiled with C# 5.0 (legacy compiler)** which has strict syntax limitations.

## C# 5.0 Limitations (MUST FOLLOW)

### ❌ NOT SUPPORTED (Will Cause Compilation Errors)

1. **String Interpolation**
   ```csharp
   // ❌ WRONG - C# 6.0 feature
   var msg = $"Drawing: {drawingMark}";

   // ✅ CORRECT - Use concatenation
   var msg = "Drawing: " + drawingMark;
   ```

2. **Null-Conditional Operators**
   ```csharp
   // ❌ WRONG - C# 6.0 feature
   var name = drawing?.Name;

   // ✅ CORRECT - Use explicit null check
   var name = (drawing != null) ? drawing.Name : null;
   ```

3. **Using Statements**
   ```csharp
   // ❌ WRONG - Not allowed in dynamic code
   using Tekla.Structures.Model;
   using Tekla.Structures.Geometry3d;

   // ✅ CORRECT - Use fully qualified names
   var model = new Tekla.Structures.Model.Model();
   var point = new Tekla.Structures.Geometry3d.Point(0, 0, 0);
   ```

4. **Expression-Bodied Members**
   ```csharp
   // ❌ WRONG - C# 6.0 feature
   public int GetCount() => items.Count;

   // ✅ CORRECT - Use traditional method body
   public int GetCount() { return items.Count; }
   ```

5. **Auto-Property Initializers**
   ```csharp
   // ❌ WRONG - C# 6.0 feature
   public string Name { get; set; } = "Default";

   // ✅ CORRECT - Initialize in constructor
   public string Name { get; set; }
   // Name = "Default"; // in constructor
   ```

## Collection Iteration (CRITICAL)

### Problem: Tekla API Returns Different Collection Types

Tekla API methods return various collection types:
- `DrawingObjectEnumerator` (has MoveNext/Current)
- `ArrayList` (does NOT have MoveNext/Current in C# 5.0)
- `IEnumerable` (generic interface)

### ❌ WRONG - Assuming ArrayList Has MoveNext

```csharp
var children = obj.GetChildren();  // Returns ArrayList
while (children.MoveNext())  // ❌ ERROR: ArrayList doesn't have MoveNext
{
    var child = children.Current;  // ❌ ERROR: ArrayList doesn't have Current
}
```

### ✅ CORRECT - Cast to IEnumerable and Use foreach

```csharp
var children = obj.GetChildren();  // Returns ArrayList
var enumerable = children as System.Collections.IEnumerable;
if (enumerable != null)
{
    foreach (var child in enumerable)  // ✅ Works in C# 5.0
    {
        // Process child
    }
}
```

### ✅ ALTERNATIVE - Direct ArrayList Indexing

```csharp
var children = obj.GetChildren();  // Returns ArrayList
var arrayList = children as System.Collections.ArrayList;
if (arrayList != null && arrayList.Count > 0)
{
    for (int i = 0; i < arrayList.Count; i++)
    {
        var child = arrayList[i];
        // Process child
    }
}
```

## Where This Applies

### Files That Generate Dynamic Code:

1. **Server/TeklaMCP/ProgramClean.cs**
   - `GenerateFullExtractionCode()` - Lines 1314+
   - `GenerateDiscoveryModeCode()` - Lines 1229+
   - All code inside `@"..."` strings

### Files That Run Directly (Can Use Modern C#):

1. **Server/TeklaMCP/ProgramClean.cs** - Outside string literals
2. **VirtualFitter/** - C# 7.3+ supported

## Testing Generated Code

### Quick Test:
```bash
# This will show compilation errors if syntax is wrong
curl -X POST http://localhost:5050/extract-drawing-objects \
  -H "Content-Type: application/json" \
  -d '{"drawing_mark":"B18","max_drawings":1,"discovery_mode":false}'
```

### Error Messages to Watch For:
```
Line XXX: '...' does not contain a definition for 'MoveNext'
Line XXX: Invalid expression term ':'  (string interpolation)
Line XXX: using directive expected  (null-conditional operator)
```

## Code Review Checklist

Before modifying generated code strings:

- [ ] No `$"{variable}"` - use `"text " + variable`
- [ ] No `obj?.Property` - use `(obj != null) ? obj.Property : null`
- [ ] No `using` statements - use fully qualified names
- [ ] ArrayList: Cast to `IEnumerable` + `foreach`, OR index with `[i]`
- [ ] Test extraction after changes

## Why This Limitation Exists

Tekla Structures uses an embedded scripting engine with C# 5.0 compiler for dynamic code execution. This cannot be upgraded without Tekla changing their API architecture.

## Workarounds

1. **Keep generated code simple** - Complex logic should be in ProgramClean.cs
2. **Use reflection** - Access properties/methods dynamically
3. **Test frequently** - Compilation errors only show when code runs

---

**Last Updated:** 2025-12-20
**Applies To:** TeklaMCP 0.3+ (all versions using dynamic code generation)
