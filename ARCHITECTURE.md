# 🏗️ TeklaMCP Architecture Documentation

## 🎯 Dynamic Code Execution Solutions Comparison

### ❌ **Failed Approach: Modern Roslyn Compiler**
```csharp
using Microsoft.CodeAnalysis.CSharp; // External package dependency
var compilation = CSharpCompilation.Create();
```

**Problems:**
- `Microsoft.CodeAnalysis.CSharp` v4.0.1 conflicts with Tekla dependencies  
- `System.Runtime.CompilerServices.Unsafe` version conflicts (v5.0 vs v6.0)
- Tekla connection fails: "Could not load assembly" errors
- Complex manual reference management

### ✅ **Successful Approach: Legacy CSharpCodeProvider** 

```csharp
using Microsoft.CSharp; // Built into .NET Framework
using System.CodeDom.Compiler; // Built into .NET Framework

using (var provider = new CSharpCodeProvider())
{
    var parameters = new CompilerParameters();
    
    // Smart automatic reference discovery
    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
        if (assembly.GetName().Name.StartsWith("Tekla.Structures"))
        {
            parameters.ReferencedAssemblies.Add(assembly.Location);
        }
    }
}
```

**Advantages:**
- ✅ **No external dependencies** - uses built-in .NET Framework compiler
- ✅ **No version conflicts** - doesn't introduce conflicting packages  
- ✅ **Tekla connection works** - no assembly loading issues
- ✅ **Automatic reference discovery** - scans loaded assemblies
- ✅ **Proven stability** - was working in previous versions

## 🔧 **Implementation Strategy**

### **Phase 1: Replace Roslyn with Legacy Compiler**
Remove problematic dependencies:
```xml
<!-- REMOVE -->
<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.0.1" />

<!-- KEEP MINIMAL -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

Add built-in references:
```csharp
using Microsoft.CSharp;
using System.CodeDom.Compiler;
```

### **Phase 2: Implement Smart Reference Discovery**
```csharp
static CompilerParameters SetupCompilerParameters()
{
    var parameters = new CompilerParameters();
    
    // Basic .NET Framework references
    parameters.ReferencedAssemblies.Add("System.dll");
    parameters.ReferencedAssemblies.Add("System.Core.dll");
    
    // Auto-discover Tekla assemblies
    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
        if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
        {
            var name = assembly.GetName().Name;
            if (name.StartsWith("Tekla.Structures") || name.Equals("Newtonsoft.Json"))
            {
                parameters.ReferencedAssemblies.Add(assembly.Location);
                Console.WriteLine($"📚 Added reference: {name}");
            }
        }
    }
    
    parameters.GenerateInMemory = true;
    parameters.GenerateExecutable = false;
    
    return parameters;
}
```

### **Phase 3: Code Wrapper Template**
```csharp
static string WrapUserCode(string userCode)
{
    return $@"
using System;
using Tekla.Structures.Model;
using Tekla.Structures.Geometry3d;

public class DynamicTeklaScript 
{{
    public object Execute() 
    {{
        var model = new Model();
        if (!model.GetConnectionStatus()) 
            throw new Exception(""Tekla not connected"");

        object result = null;
        try 
        {{
            {userCode}
        }}
        catch (Exception ex) 
        {{
            throw new Exception(""Code execution error: "" + ex.Message);
        }}

        // Always commit changes
        model.CommitChanges();
        return result ?? ""Success - changes committed"";
    }}
}}";
}
```

## 🎯 **Expected Results**

**Before (Roslyn):**
```
❌ Tekla connection fails
❌ System.Runtime.CompilerServices.Unsafe conflicts  
❌ Complex dependency management
❌ External package requirements
```

**After (Legacy):**
```  
✅ Tekla connection works
✅ No version conflicts
✅ Automatic reference discovery
✅ Built-in .NET Framework tools only
✅ Proven stability from previous working version
```

## 🔬 **Test Cases**

### **Test 1: Basic Tekla Connection**
```csharp
var model = new Model();
Console.WriteLine("Connected: " + model.GetConnectionStatus());
```

### **Test 2: Simple Beam Creation**
```csharp  
var beam = new Beam();
beam.Profile.ProfileString = "W12X26";
beam.StartPoint = new Point(0, 0, 0);
beam.EndPoint = new Point(6000, 0, 0);
beam.Insert();
Console.WriteLine("Created beam ID: " + beam.Identifier.ID);
```

### **Test 3: Complex Operations**
```csharp
var beam = new Beam();
beam.Profile.ProfileString = "W21X50"; 
beam.StartPoint = new Point(2000, 0, 0);
beam.EndPoint = new Point(8000, 0, 0);
beam.Material.MaterialString = "A992";
beam.Class = "5";
beam.Name = "LEGACY_DYNAMIC_BEAM";
beam.Insert();

Console.WriteLine($"Created {beam.Name} with ID: {beam.Identifier.ID}");
```

## 💡 **Architecture Benefits**

1. **Dependency Isolation**: Uses only built-in .NET Framework compiler
2. **Proven Stability**: Based on previously working implementation  
3. **Smart Discovery**: Automatically finds and references Tekla assemblies
4. **Clean Separation**: HTTP Server → Code Wrapper → Legacy Compiler → Tekla API
5. **Future Proof**: No external package version conflicts

**This approach gives us the power of dynamic execution with the stability of the clean architecture!**