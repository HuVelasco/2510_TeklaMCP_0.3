using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using Tekla.Structures.Model;
using Tekla.Structures.Geometry3d;
using Newtonsoft.Json;
using Microsoft.CSharp;
using System.CodeDom.Compiler;

namespace TeklaMCP
{
    /// <summary>
    /// TeklaMCP 0.3 - CLEAN VERSION with Legacy Dynamic Execution
    /// 
    /// ARCHITECTURE OVERVIEW:
    /// - Uses legacy CSharpCodeProvider (built into .NET Framework)
    /// - No external dependencies that conflict with Tekla
    /// - Automatic assembly discovery from AppDomain
    /// - Direct Tekla API integration without abstraction layers
    /// 
    /// EXECUTION REQUIREMENTS:
    /// - Assumes Tekla Structures is installed and running
    /// - Requires active Tekla model connection
    /// - Uses .NET Framework 4.8 compiler (C# 5.0/6.0 syntax)
    /// 
    /// C# SYNTAX RESTRICTIONS (Legacy Compiler):
    /// ❌ NO string interpolation: $"{variable}" 
    /// ✅ USE string concatenation: "text " + variable
    /// ❌ NO C# 6.0+ features (null-conditional operators ?., etc.)
    /// ✅ USE traditional C# 5.0 syntax
    /// ❌ NO var in implicit contexts that are ambiguous
    /// ✅ USE explicit types or clear var declarations
    /// 
    /// TEKLA API CAPABILITIES:
    /// ✅ Create beams, columns, plates, bolts
    /// ✅ Create connections (EndPlateConnection, ShearTabConnection, etc.)
    /// ✅ Modify positions, profiles, materials
    /// ✅ Access model info, grid lines, existing objects
    /// ✅ Full geometry and coordinate manipulation
    /// 
    /// TEKLA CONNECTION CREATION (PROVEN V2 PATTERN):
    /// 
    /// CRITICAL SEQUENCE (must be exact for ALL connection types):
    /// 1. connection.SetPrimaryObject(mainElement);        // Set main/primary element
    /// 2. connection.SetSecondaryObjects(secondaryArray);  // Can be multiple objects
    /// 3. connection.Insert();                              // Insert AFTER setting objects
    /// 4. connection.LoadAttributesFromFile("standard");    // Load attributes AFTER insert
    /// 5. connection.Modify();                              // Apply all changes
    /// 
    /// PRIMARY/SECONDARY RULES (connection-specific):
    /// - Column-to-Beam: Column = PRIMARY, Beam(s) = SECONDARY
    /// - Beam-to-Beam: Supporting beam = PRIMARY, Supported beam(s) = SECONDARY  
    /// - Rafter Apex: Left rafter = PRIMARY, Right rafter = SECONDARY
    /// - Base Plate: Column = PRIMARY (Detail class, single object)
    /// - Bracing: Main member = PRIMARY, Braces = SECONDARY
    /// 
    /// COMMON CONNECTION NUMBERS:
    /// - 144: End plate (beam-to-column)
    /// - 106: Apex bolted (rafter-to-rafter)
    /// - 141: Clip angle (beam-to-beam)
    /// - 1014: Base plate (column base)
    /// - 11: Shear plate simple
    /// 
    /// IMPORTANT NOTES:
    /// - Use Connection class for multi-object connections
    /// - Use Detail class for single-object components (base plates, stiffeners)
    /// - Cannot create duplicate connections between same objects
    /// - Some connections accept multiple secondary objects (e.g., multiple braces)
    /// 
    /// EXECUTION FLOW:
    /// 1. Check Tekla connection
    /// 2. Wrap user code in DynamicTeklaScript class
    /// 3. Auto-discover Tekla assemblies from AppDomain
    /// 4. Compile using CSharpCodeProvider
    /// 5. Execute compiled assembly
    /// 6. Commit changes to Tekla model
    /// 
    /// PROVEN STABILITY: Based on working V2 implementation with dependency conflicts resolved
    /// </summary>
    class ProgramClean
    {
        private static HttpListener listener;
        private static string logFile = @"C:\temp\tekla_mcp_clean.log";

        static void Main(string[] args)
        {
            Directory.CreateDirectory(@"C:\temp");
            LogMessage("STARTUP", "TeklaMCP 0.3 - CLEAN Server starting...");

            Console.WriteLine("🏗️ TeklaMCP 0.3 - CLEAN Server");
            Console.WriteLine("✅ Direct Tekla API - No nonsense");
            StartCleanServer();
        }

        static void StartCleanServer()
        {
            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add("http://localhost:8080/");
                listener.Start();
                
                Console.WriteLine("✅ Clean server running at http://localhost:8080");
                Console.WriteLine("📡 Endpoints:");
                Console.WriteLine("  GET  /test");
                Console.WriteLine("  GET  /tekla-info"); 
                Console.WriteLine("  POST /create-beam");
                Console.WriteLine("  POST /create-connection");
                Console.WriteLine("  POST /execute-code");
                Console.WriteLine("Press Ctrl+C to stop...");
                
                LogMessage("SERVER_START", "Clean server started successfully on port 8080");

                while (true)
                {
                    var context = listener.GetContext();
                    System.Threading.Tasks.Task.Run(() => HandleRequest(context));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Server error: {ex.Message}");
                LogMessage("SERVER_ERROR", $"Error: {ex.Message}");
            }
        }

        static void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            var path = request.Url.AbsolutePath;

            Console.WriteLine($"📞 {request.HttpMethod} {path}");
            LogMessage("REQUEST", $"{request.HttpMethod} {path}");

            string responseString = "";
            
            try
            {
                switch ($"{request.HttpMethod} {path}")
                {
                    case "GET /test":
                        responseString = HandleTest();
                        break;

                    case "GET /tekla-info":
                        responseString = HandleTeklaInfo();
                        break;

                    case "POST /create-beam":
                        responseString = HandleCreateBeam();
                        break;

                    case "POST /create-connection":
                        responseString = HandleCreateConnection();
                        break;

                    case "POST /execute-code":
                        responseString = HandleExecuteCode(request);
                        break;

                    default:
                        responseString = JsonConvert.SerializeObject(new
                        {
                            error = "Endpoint not found",
                            available = new[] { "GET /test", "GET /tekla-info", "POST /create-beam", "POST /execute-code" }
                        });
                        break;
                }

                // Send response
                response.ContentType = "application/json";
                response.StatusCode = 200;
                
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
                
                LogMessage("RESPONSE_SENT", "Response sent successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Request error: {ex.Message}");
                LogMessage("REQUEST_ERROR", $"Error: {ex.Message}");
                
                try
                {
                    var errorResponse = JsonConvert.SerializeObject(new { success = false, error = ex.Message });
                    response.StatusCode = 500;
                    byte[] buffer = Encoding.UTF8.GetBytes(errorResponse);
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
                catch
                {
                    LogMessage("CRITICAL_ERROR", "Could not send error response");
                }
            }
        }

        static string HandleTest()
        {
            var result = new
            {
                success = true,
                message = "TeklaMCP 0.3 CLEAN Server is working!",
                server_type = "Clean - Direct Tekla API",
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            LogMessage("TEST_SUCCESS", "Test endpoint called successfully");
            return JsonConvert.SerializeObject(result, Formatting.Indented);
        }

        static string HandleTeklaInfo()
        {
            try
            {
                var model = new Model();
                bool isConnected = model.GetConnectionStatus();
                
                if (isConnected)
                {
                    var modelInfo = model.GetInfo();
                    var result = new
                    {
                        success = true,
                        tekla_connected = true,
                        model_name = modelInfo.ModelName,
                        model_path = modelInfo.ModelPath,
                        server_type = "Clean - Direct API"
                    };
                    
                    LogMessage("TEKLA_INFO_SUCCESS", $"Retrieved model info: {modelInfo.ModelName}");
                    return JsonConvert.SerializeObject(result, Formatting.Indented);
                }
                else
                {
                    LogMessage("TEKLA_NOT_CONNECTED", "Tekla not connected");
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "Tekla Structures not running or no model open"
                    });
                }
            }
            catch (Exception ex)
            {
                LogMessage("TEKLA_INFO_ERROR", $"Error: {ex.Message}");
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = $"Tekla info error: {ex.Message}"
                });
            }
        }

        static string HandleCreateBeam()
        {
            try
            {
                var model = new Model();
                if (!model.GetConnectionStatus())
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "Tekla not connected"
                    });
                }

                var beam = new Beam();
                beam.StartPoint = new Point(0, 0, 0);
                beam.EndPoint = new Point(6000, 0, 0);
                beam.Profile.ProfileString = "W21X50";
                beam.Material.MaterialString = "A992";
                beam.Class = "1";
                beam.Name = "CLEAN_BEAM";
                
                bool success = beam.Insert();
                model.CommitChanges();
                
                if (success)
                {
                    LogMessage("BEAM_CREATED", $"Beam created successfully, ID: {beam.Identifier.ID}");
                    return JsonConvert.SerializeObject(new
                    {
                        success = true,
                        message = "Beam created successfully with direct Tekla API",
                        beam_id = beam.Identifier.ID,
                        profile = beam.Profile.ProfileString,
                        material = beam.Material.MaterialString
                    });
                }
                else
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "Failed to insert beam into model"
                    });
                }
            }
            catch (Exception ex)
            {
                LogMessage("BEAM_ERROR", $"Error: {ex.Message}");
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = $"Beam creation error: {ex.Message}"
                });
            }
        }

        static string HandleCreateConnection()
        {
            try
            {
                var model = new Model();
                if (!model.GetConnectionStatus())
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "Tekla not connected"
                    });
                }

                // Get selected objects from Tekla UI
                var selector = new Tekla.Structures.Model.UI.ModelObjectSelector();
                var selectedObjects = selector.GetSelectedObjects();
                
                var beams = new System.Collections.ArrayList();
                while (selectedObjects.MoveNext())
                {
                    var obj = selectedObjects.Current as ModelObject;
                    if (obj is Beam)
                    {
                        beams.Add(obj);
                    }
                }

                if (beams.Count < 2)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "Please select 2 objects (column and beam) in Tekla",
                        selected_count = beams.Count
                    });
                }

                var beam1 = beams[0] as Beam;
                var beam2 = beams[1] as Beam;

                // Auto-identify column vs beam (vertical > horizontal = column)
                double beam1Vertical = System.Math.Abs(beam1.EndPoint.Z - beam1.StartPoint.Z);
                double beam1Horizontal = System.Math.Sqrt(
                    System.Math.Pow(beam1.EndPoint.X - beam1.StartPoint.X, 2) + 
                    System.Math.Pow(beam1.EndPoint.Y - beam1.StartPoint.Y, 2));
                bool beam1IsColumn = beam1Vertical > beam1Horizontal;

                double beam2Vertical = System.Math.Abs(beam2.EndPoint.Z - beam2.StartPoint.Z);
                double beam2Horizontal = System.Math.Sqrt(
                    System.Math.Pow(beam2.EndPoint.X - beam2.StartPoint.X, 2) + 
                    System.Math.Pow(beam2.EndPoint.Y - beam2.StartPoint.Y, 2));
                bool beam2IsColumn = beam2Vertical > beam2Horizontal;

                // Assign primary (column) and secondary (beam) correctly
                Beam primaryColumn = beam1IsColumn ? beam1 : (beam2IsColumn ? beam2 : beam1);
                Beam secondaryBeam = beam1IsColumn ? beam2 : (beam2IsColumn ? beam1 : beam2);

                // Create connection using PROVEN V2 SEQUENCE
                var connection = new Tekla.Structures.Model.Connection();
                connection.Name = "End plate";
                connection.Number = 144;  // Standard End plate connection

                // V2 PROVEN SEQUENCE - MUST BE EXACT:
                connection.SetPrimaryObject(primaryColumn);     // Column as primary
                connection.SetSecondaryObject(secondaryBeam);   // Beam as secondary
                
                bool inserted = connection.Insert();            // Insert first
                
                if (inserted)
                {
                    connection.LoadAttributesFromFile("standard"); // Load attributes AFTER insert
                    connection.Modify();                           // Apply changes
                    
                    model.CommitChanges();
                    
                    LogMessage("CONNECTION_CREATED", $"Connection created successfully, ID: {connection.Identifier.ID}");
                    
                    return JsonConvert.SerializeObject(new
                    {
                        success = true,
                        message = "Connection created using V2 proven pattern",
                        connection_id = connection.Identifier.ID,
                        connection_name = connection.Name,
                        primary = new { 
                            id = primaryColumn.Identifier.ID, 
                            profile = primaryColumn.Profile.ProfileString,
                            type = beam1IsColumn ? "COLUMN" : "BEAM"
                        },
                        secondary = new { 
                            id = secondaryBeam.Identifier.ID, 
                            profile = secondaryBeam.Profile.ProfileString,
                            type = beam1IsColumn ? "BEAM" : "COLUMN"
                        }
                    });
                }
                else
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "Failed to insert connection - may already exist between these objects"
                    });
                }
            }
            catch (Exception ex)
            {
                LogMessage("CONNECTION_ERROR", $"Error: {ex.Message}");
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = $"Connection creation error: {ex.Message}"
                });
            }
        }

        static string HandleExecuteCode(HttpListenerRequest request)
        {
            try
            {
                // Read code from request
                string code = "";
                using (var reader = new StreamReader(request.InputStream))
                {
                    code = reader.ReadToEnd();
                }

                if (string.IsNullOrEmpty(code))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "No code provided"
                    });
                }

                Console.WriteLine($"📝 Code to execute: {code.Substring(0, Math.Min(100, code.Length))}...");
                LogMessage("EXECUTE_CODE_START", $"Code received: {code.Length} chars");

                // Simple console capture
                var originalOut = Console.Out;
                var consoleOutput = new StringBuilder();

                using (var stringWriter = new StringWriter(consoleOutput))
                {
                    Console.SetOut(stringWriter);
                    
                    try
                    {
                        string result = ExecuteCodeDirect(code);
                        
                        Console.SetOut(originalOut);
                        var capturedOutput = consoleOutput.ToString();
                        
                        Console.WriteLine($"✅ Code executed successfully");
                        Console.WriteLine($"📊 Console captured: {capturedOutput.Length} chars");
                        
                        LogMessage("EXECUTE_SUCCESS", $"Code executed: {result}");
                        
                        return JsonConvert.SerializeObject(new
                        {
                            success = true,
                            result = result,
                            console_output = capturedOutput,
                            message = "Clean execution with direct Tekla API",
                            code_length = code.Length
                        }, Formatting.Indented);
                    }
                    catch (Exception ex)
                    {
                        Console.SetOut(originalOut);
                        LogMessage("EXECUTE_ERROR", $"Error: {ex.Message}");
                        
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = ex.Message,
                            console_output = consoleOutput.ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage("EXECUTE_WRAPPER_ERROR", $"Error: {ex.Message}");
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = $"Execute wrapper error: {ex.Message}"
                });
            }
        }

        static string ExecuteCodeDirect(string code)
        {
            Console.WriteLine("=== LEGACY C# EXECUTION (NO CONFLICTS) ===");
            Console.WriteLine("🔧 SYNTAX REMINDERS:");
            Console.WriteLine("❌ DON'T USE: $\"{variable}\" (string interpolation)");
            Console.WriteLine("✅ USE: \"text \" + variable (concatenation)");
            Console.WriteLine("❌ DON'T USE: obj?.Property (null-conditional)");
            Console.WriteLine("✅ USE: (obj != null) ? obj.Property : defaultValue");
            Console.WriteLine("");
            
            // Check Tekla connection first
            var model = new Model();
            if (!model.GetConnectionStatus())
            {
                throw new Exception("Tekla Structures not connected");
            }
            
            try
            {
                // Wrap user code using the proven template
                var wrappedCode = $@"
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
            {code}
        }}
        catch (Exception ex) 
        {{
            throw new Exception(""Code execution error: "" + ex.Message);
        }}

        // Always commit changes
        try 
        {{
            model.CommitChanges();
            Console.WriteLine(""✅ CommitChanges executed successfully"");
        }}
        catch (Exception ex)
        {{
            throw new Exception(""CommitChanges failed: "" + ex.Message);
        }}

        return result ?? ""Success - changes committed"";
    }}
}}";

                Console.WriteLine("🔨 Compiling with legacy CSharpCodeProvider...");
                
                return CompileAndExecuteLegacy(wrappedCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Execution error: {ex.Message}");
                return $"Execution error: {ex.Message}";
            }
        }

        static string CompileAndExecuteLegacy(string code)
        {
            try
            {
                using (var provider = new CSharpCodeProvider())
                {
                    var parameters = new CompilerParameters();

                    // Add basic .NET Framework references
                    parameters.ReferencedAssemblies.Add("System.dll");
                    parameters.ReferencedAssemblies.Add("System.Core.dll");

                    // Smart auto-discovery of Tekla assemblies
                    foreach (var loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            if (!loadedAssembly.IsDynamic && !string.IsNullOrEmpty(loadedAssembly.Location))
                            {
                                var name = loadedAssembly.GetName().Name;
                                if (name.StartsWith("Tekla.Structures") ||
                                    name.Equals("Newtonsoft.Json"))
                                {
                                    parameters.ReferencedAssemblies.Add(loadedAssembly.Location);
                                    Console.WriteLine($"📚 Added reference: {name}");
                                }
                            }
                        }
                        catch
                        {
                            // Skip problematic assemblies
                        }
                    }

                    parameters.GenerateInMemory = true;
                    parameters.GenerateExecutable = false;

                    Console.WriteLine("🔨 Compiling code with legacy compiler...");

                    // Compile using legacy CSharpCodeProvider
                    var results = provider.CompileAssemblyFromSource(parameters, code);

                    if (results.Errors.HasErrors)
                    {
                        var errors = string.Join("\n", results.Errors.Cast<CompilerError>()
                            .Select(e => $"Line {e.Line}: {e.ErrorText}"));
                        Console.WriteLine($"❌ Compilation errors:\n{errors}");
                        throw new Exception($"Compilation errors:\n{errors}");
                    }

                    Console.WriteLine("✅ Code compiled successfully with legacy compiler");

                    // Execute
                    var compiledAssembly = results.CompiledAssembly;
                    var type = compiledAssembly.GetType("DynamicTeklaScript");
                    var instance = Activator.CreateInstance(type);
                    var method = type.GetMethod("Execute");

                    Console.WriteLine("🚀 Executing compiled code...");
                    var result = method.Invoke(instance, null);
                    
                    Console.WriteLine($"✅ Legacy execution completed: {result}");
                    return result?.ToString() ?? "Executed successfully";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Legacy compilation/execution error: {ex.Message}");
                throw new Exception($"Legacy compilation/execution error: {ex.Message}");
            }
        }

        static void LogMessage(string category, string message)
        {
            try
            {
                var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {category} | {message}";
                File.AppendAllText(logFile, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Logging error: {ex.Message}");
            }
        }
    }
}