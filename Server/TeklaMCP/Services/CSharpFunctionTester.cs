using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Tekla.Structures.Model;
using Newtonsoft.Json;
using System.CodeDom.Compiler;
using Microsoft.CSharp;

namespace TeklaMCP.Services
{
    /// <summary>
    /// C# Functions File Tester - Tests C# code templates directly in Tekla environment
    /// Author: Hugo Velasco Pizano
    /// </summary>
    public class CSharpFunctionTester
    {
        private readonly string _testedCodePath;
        private readonly Dictionary<string, string> _templateCache;
        
        public CSharpFunctionTester()
        {
            // Find the correct path to tested_csharp - it's at Server level
            var baseDir = AppDomain.CurrentDomain.BaseDirectory; // bin/Debug
            var teklaMcpDir = Directory.GetParent(baseDir).Parent.Parent.FullName; // Go up to TeklaMCP project dir
            var serverDir = Directory.GetParent(teklaMcpDir).FullName; // Go up to Server dir
            _testedCodePath = Path.Combine(serverDir, "tested_csharp"); // Server/tested_csharp
            _templateCache = new Dictionary<string, string>();
            
            Console.WriteLine($"📁 Base directory: {baseDir}");
            Console.WriteLine($"📁 Server directory: {serverDir}");
            Console.WriteLine($"📁 Tested code path: {_testedCodePath}");
            Console.WriteLine($"📁 Path exists: {Directory.Exists(_testedCodePath)}");
            
            if (Directory.Exists(_testedCodePath))
            {
                var dirs = Directory.GetDirectories(_testedCodePath);
                Console.WriteLine($"📂 Available categories: {string.Join(", ", dirs.Select(d => Path.GetFileName(d)))}");
            }
        }
        
        /// <summary>
        /// Test a specific C# function template with parameters
        /// </summary>
        public TestResult TestFunction(string category, string functionName, Dictionary<string, object> parameters = null)
        {
            try
            {
                Console.WriteLine($"🧪 Testing function: {category}/{functionName}");
                
                // Load template
                string template = LoadTemplate(category, functionName);
                if (template == null)
                {
                    return new TestResult 
                    { 
                        Success = false, 
                        Error = $"Template not found: {category}/{functionName}.cs",
                        FunctionName = functionName,
                        Category = category
                    };
                }
                
                // Substitute parameters if provided
                string code = template;
                if (parameters != null)
                {
                    code = SubstituteParameters(template, parameters);
                }
                
                // Test compilation
                var compilationResult = TestCompilation(code);
                if (!compilationResult.Success)
                {
                    return new TestResult
                    {
                        Success = false,
                        Error = $"Compilation failed: {compilationResult.Error}",
                        FunctionName = functionName,
                        Category = category,
                        Code = code
                    };
                }
                
                // Test execution (if Tekla is connected)
                var executionResult = TestExecution(code);
                
                return new TestResult
                {
                    Success = executionResult.Success,
                    Result = executionResult.Result,
                    Error = executionResult.Error,
                    FunctionName = functionName,
                    Category = category,
                    Code = code,
                    CompilationSuccess = true
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    Success = false,
                    Error = $"Test failed: {ex.Message}",
                    FunctionName = functionName,
                    Category = category
                };
            }
        }
        
        /// <summary>
        /// Test all functions in a category
        /// </summary>
        public List<TestResult> TestCategory(string category)
        {
            var results = new List<TestResult>();
            
            try
            {
                string categoryPath = Path.Combine(_testedCodePath, category);
                if (!Directory.Exists(categoryPath))
                {
                    results.Add(new TestResult 
                    { 
                        Success = false, 
                        Error = $"Category not found: {category}",
                        Category = category
                    });
                    return results;
                }
                
                var csFiles = Directory.GetFiles(categoryPath, "*.cs");
                Console.WriteLine($"🔍 Found {csFiles.Length} functions in {category}");
                
                foreach (var file in csFiles)
                {
                    string functionName = Path.GetFileNameWithoutExtension(file);
                    var result = TestFunction(category, functionName);
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                results.Add(new TestResult
                {
                    Success = false,
                    Error = $"Category test failed: {ex.Message}",
                    Category = category
                });
            }
            
            return results;
        }
        
        /// <summary>
        /// Test all available functions
        /// </summary>
        public Dictionary<string, List<TestResult>> TestAllFunctions()
        {
            var allResults = new Dictionary<string, List<TestResult>>();
            
            try
            {
                if (!Directory.Exists(_testedCodePath))
                {
                    Console.WriteLine($"❌ Tested code directory not found: {_testedCodePath}");
                    return allResults;
                }
                
                var categories = Directory.GetDirectories(_testedCodePath)
                    .Select(d => Path.GetFileName(d))
                    .ToList();
                
                Console.WriteLine($"📂 Found categories: {string.Join(", ", categories)}");
                
                foreach (var category in categories)
                {
                    var categoryResults = TestCategory(category);
                    allResults[category] = categoryResults;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test all functions failed: {ex.Message}");
            }
            
            return allResults;
        }
        
        private string LoadTemplate(string category, string functionName)
        {
            string cacheKey = $"{category}/{functionName}";
            
            if (_templateCache.ContainsKey(cacheKey))
            {
                return _templateCache[cacheKey];
            }
            
            string filePath = Path.Combine(_testedCodePath, category, $"{functionName}.cs");
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"❌ Template file not found: {filePath}");
                return null;
            }
            
            try
            {
                string content = File.ReadAllText(filePath);
                _templateCache[cacheKey] = content;
                Console.WriteLine($"✅ Loaded template: {cacheKey}");
                return content;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading template {filePath}: {ex.Message}");
                return null;
            }
        }
        
        private string SubstituteParameters(string template, Dictionary<string, object> parameters)
        {
            string result = template;
            
            foreach (var param in parameters)
            {
                string placeholder = "{" + param.Key + "}";
                string value = param.Value?.ToString() ?? "null";
                result = result.Replace(placeholder, value);
            }
            
            return result;
        }
        
        private TestResult TestCompilation(string code)
        {
            try
            {
                // Create wrapper code for compilation test
                var fullCode = $@"
                using System;
                using Tekla.Structures.Model;
                using Tekla.Structures.Geometry3d;
                
                public class TestWrapper 
                {{
                    public object TestFunction() 
                    {{
                        object result = null;
                        {code}
                        return result ?? ""Test compiled successfully"";
                    }}
                }}";
                
                using (var provider = new CSharpCodeProvider())
                {
                    var parameters = new CompilerParameters();
                    parameters.ReferencedAssemblies.Add("System.dll");
                    parameters.ReferencedAssemblies.Add("System.Core.dll");
                    
                    // Add Tekla references
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                            {
                                var name = assembly.GetName().Name;
                                if (name.StartsWith("Tekla.Structures"))
                                {
                                    parameters.ReferencedAssemblies.Add(assembly.Location);
                                }
                            }
                        }
                        catch { /* Skip problematic assemblies */ }
                    }
                    
                    parameters.GenerateInMemory = true;
                    parameters.GenerateExecutable = false;
                    
                    var results = provider.CompileAssemblyFromSource(parameters, fullCode);
                    
                    if (results.Errors.HasErrors)
                    {
                        var errors = string.Join("\n", results.Errors.Cast<CompilerError>()
                            .Select(e => $"Line {e.Line}: {e.ErrorText}"));
                        return new TestResult { Success = false, Error = errors };
                    }
                    
                    return new TestResult { Success = true, Result = "Compilation successful" };
                }
            }
            catch (Exception ex)
            {
                return new TestResult { Success = false, Error = $"Compilation test failed: {ex.Message}" };
            }
        }
        
        private TestResult TestExecution(string code)
        {
            try
            {
                var model = new Model();
                if (!model.GetConnectionStatus())
                {
                    return new TestResult 
                    { 
                        Success = false, 
                        Error = "Tekla not connected - execution test skipped",
                        Result = "Compilation OK, execution not tested"
                    };
                }
                
                // For now, we'll just validate that Tekla is connected
                // Actual execution would be done through the main HTTP server
                return new TestResult 
                { 
                    Success = true, 
                    Result = "Ready for execution - Tekla connected" 
                };
            }
            catch (Exception ex)
            {
                return new TestResult 
                { 
                    Success = false, 
                    Error = $"Execution test failed: {ex.Message}" 
                };
            }
        }
    }
    
    /// <summary>
    /// Test result structure
    /// </summary>
    public class TestResult
    {
        public bool Success { get; set; }
        public string Result { get; set; }
        public string Error { get; set; }
        public string FunctionName { get; set; }
        public string Category { get; set; }
        public string Code { get; set; }
        public bool CompilationSuccess { get; set; }
        public DateTime TestTime { get; set; } = DateTime.Now;
        
        public override string ToString()
        {
            string status = Success ? "✅ PASS" : "❌ FAIL";
            string location = !string.IsNullOrEmpty(Category) && !string.IsNullOrEmpty(FunctionName) 
                ? $"{Category}/{FunctionName}" 
                : "Unknown";
                
            if (Success)
            {
                return $"{status} {location}: {Result}";
            }
            else
            {
                return $"{status} {location}: {Error}";
            }
        }
    }
}