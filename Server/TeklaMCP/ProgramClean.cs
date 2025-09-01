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
    /// BeamRequest - Type-safe data model for beam creation
    /// Follows engineered architecture from old commits
    /// </summary>
    public class BeamRequest
    {
        // Core geometry
        public double StartPointX { get; set; }
        public double StartPointY { get; set; }
        public double StartPointZ { get; set; }
        public double EndPointX { get; set; }
        public double EndPointY { get; set; }
        public double EndPointZ { get; set; }

        // Material and profile
        public string Profile { get; set; }
        public string Material { get; set; } = "A992";

        // Properties  
        public string Class { get; set; } = "1";        // User-defined organizational number
        public string Name { get; set; } = "BEAM";      // Display name
        public string Finish { get; set; } = "";

        // Position parameters (strings converted to Tekla enums)
        public string PositionDepth { get; set; } = "MIDDLE";      // FRONT, MIDDLE, BEHIND
        public string PositionPlane { get; set; } = "MIDDLE";      // LEFT, MIDDLE, RIGHT
        public string PositionRotation { get; set; } = "FRONT";    // TOP, FRONT, BACK, BELOW

        // Point offsets (optional)
        public double StartPointOffsetDx { get; set; } = 0.0;
        public double StartPointOffsetDy { get; set; } = 0.0;
        public double StartPointOffsetDz { get; set; } = 0.0;
        public double EndPointOffsetDx { get; set; } = 0.0;
        public double EndPointOffsetDy { get; set; } = 0.0;
        public double EndPointOffsetDz { get; set; } = 0.0;

        // Position offsets (optional)
        public double PositionDepthOffset { get; set; } = 0.0;
        public double PositionPlaneOffset { get; set; } = 0.0;
        public double PositionRotationOffset { get; set; } = 0.0;

        // Advanced properties
        public int PourPhase { get; set; } = 0;
    }
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
    /// 
    /// PHASE 1 IMPROVEMENTS:
    /// - Standardized error response builder eliminates duplication
    /// - Modularized connection verification code
    /// - Cleaned up monolithic patterns
    /// </summary>
    class ProgramClean
    {
        private static HttpListener listener;
        private static string logFile = @"C:\temp\tekla_mcp_clean.log";

        // ==================== STANDARDIZED RESPONSE BUILDERS ====================
        
        /// <summary>
        /// Standardized success response builder - eliminates 25+ lines of duplication
        /// </summary>
        private static string BuildSuccessResponse(object data, string message = null)
        {
            return JsonConvert.SerializeObject(new
            {
                success = true,
                message = message ?? "Operation completed successfully",
                data = data,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            }, Formatting.Indented);
        }
        
        /// <summary>
        /// Standardized error response builder - eliminates duplication and ensures consistency
        /// </summary>
        private static string BuildErrorResponse(string error, string operation = null, object details = null)
        {
            return JsonConvert.SerializeObject(new
            {
                success = false,
                error = error,
                operation = operation,
                details = details,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            }, Formatting.Indented);
        }
        
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
                        responseString = HandleCreateBeam(request);
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
            LogMessage("TEST_SUCCESS", "Test endpoint called successfully");
            return BuildSuccessResponse(new {
                server_type = "Clean - Direct Tekla API - Phase 1",
                phase = "Code Deduplication & Cleanup",
                improvements = new[] {
                    "Eliminated HTTP request duplication",
                    "Standardized error responses", 
                    "Modularized connection verification"
                }
            }, "TeklaMCP 0.3 CLEAN Server is working!");
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
                    LogMessage("TEKLA_INFO_SUCCESS", $"Retrieved model info: {modelInfo.ModelName}");
                    return BuildSuccessResponse(new {
                        tekla_connected = true,
                        model_name = modelInfo.ModelName,
                        model_path = modelInfo.ModelPath,
                        server_type = "Clean - Direct API - Phase 1"
                    });
                }
                else
                {
                    LogMessage("TEKLA_NOT_CONNECTED", "Tekla not connected");
                    return BuildErrorResponse("Tekla Structures not running or no model open", "tekla-info");
                }
            }
            catch (Exception ex)
            {
                LogMessage("TEKLA_INFO_ERROR", $"Error: {ex.Message}");
                return BuildErrorResponse($"Tekla info error: {ex.Message}", "tekla-info", new { exception = ex.GetType().Name });
            }
        }

        static string HandleCreateBeam(HttpListenerRequest request)
        {
            try
            {
                var model = new Model();
                if (!model.GetConnectionStatus())
                {
                    return BuildErrorResponse("Tekla not connected", "create-beam");
                }

                // Read JSON request body
                string requestBody = "";
                using (var reader = new StreamReader(request.InputStream))
                {
                    requestBody = reader.ReadToEnd();
                }

                if (string.IsNullOrEmpty(requestBody))
                {
                    return BuildErrorResponse("No request body provided. Expected JSON with BeamRequest parameters.", "create-beam");
                }

                // Parse to type-safe BeamRequest
                var beamRequest = JsonConvert.DeserializeObject<BeamRequest>(requestBody);
                
                // Create Tekla Beam object
                var beam = new Beam();
                
                // Core geometry using Tekla Point directly
                beam.StartPoint = new Point(beamRequest.StartPointX, beamRequest.StartPointY, beamRequest.StartPointZ);
                beam.EndPoint = new Point(beamRequest.EndPointX, beamRequest.EndPointY, beamRequest.EndPointZ);
                
                // Point offsets using existing Tekla offset properties
                beam.StartPointOffset.Dx = beamRequest.StartPointOffsetDx;
                beam.StartPointOffset.Dy = beamRequest.StartPointOffsetDy;
                beam.StartPointOffset.Dz = beamRequest.StartPointOffsetDz;
                beam.EndPointOffset.Dx = beamRequest.EndPointOffsetDx;
                beam.EndPointOffset.Dy = beamRequest.EndPointOffsetDy;
                beam.EndPointOffset.Dz = beamRequest.EndPointOffsetDz;
                
                // Material and profile
                beam.Profile.ProfileString = beamRequest.Profile;
                beam.Material.MaterialString = beamRequest.Material;
                
                // Properties
                beam.Class = beamRequest.Class;
                beam.Name = beamRequest.Name;
                beam.Finish = beamRequest.Finish;
                beam.PourPhase = beamRequest.PourPhase;
                
                // Position parameters - convert strings to Tekla enums (following old commits pattern)
                if (!string.IsNullOrEmpty(beamRequest.PositionDepth))
                {
                    beam.Position.Depth = (Position.DepthEnum)Enum.Parse(typeof(Position.DepthEnum), beamRequest.PositionDepth.ToUpper());
                }
                
                if (!string.IsNullOrEmpty(beamRequest.PositionPlane))
                {
                    beam.Position.Plane = (Position.PlaneEnum)Enum.Parse(typeof(Position.PlaneEnum), beamRequest.PositionPlane.ToUpper());
                }
                
                if (!string.IsNullOrEmpty(beamRequest.PositionRotation))
                {
                    beam.Position.Rotation = (Position.RotationEnum)Enum.Parse(typeof(Position.RotationEnum), beamRequest.PositionRotation.ToUpper());
                }
                
                // Position offsets
                beam.Position.DepthOffset = beamRequest.PositionDepthOffset;
                beam.Position.PlaneOffset = beamRequest.PositionPlaneOffset;
                beam.Position.RotationOffset = beamRequest.PositionRotationOffset;
                
                // Insert beam into model
                bool success = beam.Insert();
                model.CommitChanges();
                
                if (success)
                {
                    LogMessage("BEAM_CREATED", $"Type-safe beam created, ID: {beam.Identifier.ID}");
                    return BuildSuccessResponse(new {
                        beam_id = beam.Identifier.ID,
                        profile = beam.Profile.ProfileString,
                        material = beam.Material.MaterialString,
                        class_value = beam.Class,
                        name = beam.Name,
                        position = new {
                            depth = beam.Position.Depth.ToString(),
                            plane = beam.Position.Plane.ToString(),
                            rotation = beam.Position.Rotation.ToString(),
                            depth_offset = beam.Position.DepthOffset,
                            plane_offset = beam.Position.PlaneOffset,
                            rotation_offset = beam.Position.RotationOffset
                        },
                        start_point = new { x = beam.StartPoint.X, y = beam.StartPoint.Y, z = beam.StartPoint.Z },
                        end_point = new { x = beam.EndPoint.X, y = beam.EndPoint.Y, z = beam.EndPoint.Z }
                    }, "Beam created with type-safe BeamRequest");
                }
                else
                {
                    return BuildErrorResponse("Failed to insert beam into model", "create-beam");
                }
            }
            catch (Exception ex)
            {
                LogMessage("BEAM_ERROR", $"Error: {ex.Message}");
                return BuildErrorResponse($"Beam creation error: {ex.Message}", "create-beam", new { exception = ex.GetType().Name });
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
                    return BuildErrorResponse("No code provided", "execute-code");
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
                        
                        return BuildSuccessResponse(new {
                            result = result,
                            console_output = capturedOutput,
                            code_length = code.Length
                        }, "Clean execution with direct Tekla API");
                    }
                    catch (Exception ex)
                    {
                        Console.SetOut(originalOut);
                        LogMessage("EXECUTE_ERROR", $"Error: {ex.Message}");
                        
                        return BuildErrorResponse(ex.Message, "execute-code", new { console_output = consoleOutput.ToString() });
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage("EXECUTE_WRAPPER_ERROR", $"Error: {ex.Message}");
                return BuildErrorResponse($"Execute wrapper error: {ex.Message}", "execute-code", new { exception = ex.GetType().Name });
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

        static ModelObject FindObjectById(Model model, int objectId)
        {
            try
            {
                var selector = model.GetModelObjectSelector();
                var enumerator = selector.GetAllObjects();
                
                while (enumerator.MoveNext())
                {
                    var obj = enumerator.Current as ModelObject;
                    if (obj != null && obj.Identifier.ID == objectId)
                    {
                        return obj;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                LogMessage("FIND_OBJECT_ERROR", $"Error finding object {objectId}: {ex.Message}");
                return null;
            }
        }

        // ==================== MODULAR CONNECTION MANAGEMENT ====================
        
        /// <summary>
        /// Main connection creation with verification - now modular and clean
        /// </summary>
        static string CreateConnectionWithVerification(Model model, int connectionNumber, string connectionName, ModelObject primaryObject, System.Collections.ArrayList secondaryObjects)
        {
            try
            {
                // Check for existing connections
                var existingCheck = CheckExistingConnections(model, primaryObject, secondaryObjects) as System.Collections.Generic.Dictionary<string, object>;
                if (HasExistingConnection(existingCheck))
                {
                    return BuildErrorResponse(
                        "Connection already exists between these objects",
                        "create-connection",
                        new {
                            existing_connections = GetExistingConnectionsData(existingCheck),
                            message = "Cannot create duplicate connection. Most connection types allow only one connection between the same objects."
                        }
                    );
                }
                
                // Record initial state
                var initialState = RecordInitialState(model, primaryObject);
                
                // Create and configure connection
                var connection = CreateAndConfigureConnection(connectionNumber, connectionName, primaryObject, secondaryObjects);
                
                if (!connection.Insert())
                {
                    return BuildErrorResponse("Connection.Insert() returned false - connection rejected by Tekla", "create-connection");
                }
                
                // Finalize connection
                var finalized = FinalizeConnection(connection, model);
                
                // Verify creation
                var verification = VerifyConnectionCreation(model, connection, primaryObject, secondaryObjects, initialState.connectionCount, initialState.childrenCount);
                var connectionExists = IsConnectionVerified(verification);
                
                LogMessage("CONNECTION_VERIFICATION", $"Connection {connection.Identifier.ID} verification: {connectionExists}");
                
                return BuildSuccessResponse(new {
                    connection_created = connectionExists,
                    connection_id = connection.Identifier.ID,
                    connection_number = connectionNumber,
                    connection_name = connection.Name,
                    primary_object = new {
                        id = primaryObject.Identifier.ID,
                        type = primaryObject.GetType().Name
                    },
                    secondary_objects = GetSecondaryObjectsInfo(secondaryObjects),
                    verification = verification,
                    finalization = finalized
                });
            }
            catch (Exception ex)
            {
                LogMessage("CREATE_CONNECTION_ERROR", $"Error: {ex.Message}");
                return BuildErrorResponse($"Connection creation error: {ex.Message}", "create-connection", new { exception = ex.GetType().Name });
            }
        }
        
        /// <summary>
        /// Helper to check if existing connections were found
        /// </summary>
        private static bool HasExistingConnection(System.Collections.Generic.Dictionary<string, object> existingCheck)
        {
            return existingCheck != null && existingCheck.ContainsKey("hasExisting") && Convert.ToBoolean(existingCheck["hasExisting"]);
        }
        
        /// <summary>
        /// Helper to extract existing connections data
        /// </summary>
        private static object GetExistingConnectionsData(System.Collections.Generic.Dictionary<string, object> existingCheck)
        {
            return existingCheck != null && existingCheck.ContainsKey("existingConnections") ? existingCheck["existingConnections"] : new object[0];
        }
        
        /// <summary>
        /// Record initial model state for comparison
        /// </summary>
        private static (int connectionCount, int childrenCount) RecordInitialState(Model model, ModelObject primaryObject)
        {
            return (CountConnections(model), GetObjectChildren(primaryObject));
        }
        
        /// <summary>
        /// Create and configure connection object
        /// </summary>
        private static Tekla.Structures.Model.Connection CreateAndConfigureConnection(int connectionNumber, string connectionName, ModelObject primaryObject, System.Collections.ArrayList secondaryObjects)
        {
            var connection = new Tekla.Structures.Model.Connection();
            connection.Number = connectionNumber;
            connection.Name = !string.IsNullOrEmpty(connectionName) ? connectionName : GetDefaultConnectionName(connectionNumber);
            
            // V2 PROVEN SEQUENCE
            connection.SetPrimaryObject(primaryObject);
            
            // Handle secondary objects
            if (secondaryObjects.Count == 1)
            {
                connection.SetSecondaryObject(secondaryObjects[0] as ModelObject);
            }
            else
            {
                // Multiple secondaries - first one only for now (API limitation)
                connection.SetSecondaryObject(secondaryObjects[0] as ModelObject);
            }
            
            return connection;
        }
        
        /// <summary>
        /// Finalize connection with attributes and modifications
        /// </summary>
        private static object FinalizeConnection(Tekla.Structures.Model.Connection connection, Model model)
        {
            bool attributesLoaded = connection.LoadAttributesFromFile("standard");
            bool modified = connection.Modify();
            model.CommitChanges();
            
            return new {
                attributes_loaded = attributesLoaded,
                modified = modified,
                committed = true
            };
        }
        
        /// <summary>
        /// Helper to check if connection verification passed
        /// </summary>
        private static bool IsConnectionVerified(object verification)
        {
            var verificationDict = verification as System.Collections.Generic.Dictionary<string, object>;
            return verificationDict != null && verificationDict.ContainsKey("connection_exists") && Convert.ToBoolean(verificationDict["connection_exists"]);
        }
        
        static object VerifyConnectionCreation(Model model, Tekla.Structures.Model.Connection connection, ModelObject primaryObject, System.Collections.ArrayList secondaryObjects, int initialConnectionCount, int initialChildrenCount)
        {
            try
            {
                // 1. Verify connection exists in model
                bool connectionExists = false;
                var connectionId = connection.Identifier.ID;
                
                var selector = model.GetModelObjectSelector();
                var connections = selector.GetAllObjectsWithType(ModelObject.ModelObjectEnum.CONNECTION);
                
                while (connections.MoveNext())
                {
                    var conn = connections.Current as Tekla.Structures.Model.Connection;
                    if (conn != null && conn.Identifier.ID == connectionId)
                    {
                        connectionExists = true;
                        break;
                    }
                }
                
                // 2. Check if new objects were created (bolts, plates, etc.)
                var finalConnectionCount = CountConnections(model);
                var connectionCountChanged = finalConnectionCount > initialConnectionCount;
                
                // 3. Check if primary object has new children (cuts, modifications)
                var finalChildrenCount = GetObjectChildren(primaryObject);
                var primaryObjectModified = finalChildrenCount > initialChildrenCount;
                
                // 4. Try to get connection children to see what was actually created
                var connectionChildren = new System.Collections.ArrayList();
                string connectionChildrenInfo = "";
                
                if (connectionExists)
                {
                    try
                    {
                        var childEnum = connection.GetChildren();
                        int childCount = 0;
                        while (childEnum.MoveNext())
                        {
                            childCount++;
                            var child = childEnum.Current as ModelObject;
                            if (child != null)
                            {
                                connectionChildren.Add(child.GetType().Name + ":" + child.Identifier.ID);
                            }
                        }
                        connectionChildrenInfo = $"Found {childCount} connection children";
                    }
                    catch (Exception ex)
                    {
                        connectionChildrenInfo = $"Could not get children: {ex.Message}";
                    }
                }
                
                var result = new System.Collections.Generic.Dictionary<string, object>();
                result["connection_exists"] = connectionExists;
                result["connection_id_valid"] = connectionId > 0;
                result["model_connection_count_changed"] = connectionCountChanged;
                result["primary_object_modified"] = primaryObjectModified;
                result["connection_children_info"] = connectionChildrenInfo;
                result["connection_children"] = connectionChildren.ToArray();
                result["initial_connections"] = initialConnectionCount;
                result["final_connections"] = finalConnectionCount;
                result["initial_primary_children"] = initialChildrenCount;
                result["final_primary_children"] = finalChildrenCount;
                return result;
            }
            catch (Exception ex)
            {
                var errorResult = new System.Collections.Generic.Dictionary<string, object>();
                errorResult["connection_exists"] = false;
                errorResult["verification_error"] = ex.Message;
                return errorResult;
            }
        }
        
        static int CountConnections(Model model)
        {
            try
            {
                var selector = model.GetModelObjectSelector();
                var connections = selector.GetAllObjectsWithType(ModelObject.ModelObjectEnum.CONNECTION);
                int count = 0;
                while (connections.MoveNext()) count++;
                return count;
            }
            catch
            {
                return 0;
            }
        }
        
        static int GetObjectChildren(ModelObject obj)
        {
            try
            {
                var children = obj.GetChildren();
                int count = 0;
                while (children.MoveNext()) count++;
                return count;
            }
            catch
            {
                return 0;
            }
        }
        
        static object[] GetSecondaryObjectsInfo(System.Collections.ArrayList secondaryObjects)
        {
            var info = new object[secondaryObjects.Count];
            for (int i = 0; i < secondaryObjects.Count; i++)
            {
                var obj = secondaryObjects[i] as ModelObject;
                info[i] = new {
                    id = obj.Identifier.ID,
                    type = obj.GetType().Name
                };
            }
            return info;
        }
        
        static object CheckExistingConnections(Model model, ModelObject primaryObject, System.Collections.ArrayList secondaryObjects)
        {
            try
            {
                var existingConnections = new System.Collections.ArrayList();
                
                var selector = model.GetModelObjectSelector();
                var allConnections = selector.GetAllObjectsWithType(ModelObject.ModelObjectEnum.CONNECTION);
                
                while (allConnections.MoveNext())
                {
                    var connection = allConnections.Current as Tekla.Structures.Model.Connection;
                    if (connection != null)
                    {
                        try
                        {
                            // Get primary and secondary objects from existing connection
                            var connPrimary = connection.GetPrimaryObject();
                            var connSecondaries = connection.GetSecondaryObjects();
                            
                            // Check if this connection involves our objects
                            bool involvesPrimary = (connPrimary != null && connPrimary.Identifier.ID == primaryObject.Identifier.ID);
                            bool involvesSecondary = false;
                            
                            if (connSecondaries != null)
                            {
                                var secondaryEnumerator = connSecondaries.GetEnumerator();
                                while (secondaryEnumerator.MoveNext())
                                {
                                    var secObj = secondaryEnumerator.Current as ModelObject;
                                    if (secObj != null)
                                    {
                                        foreach (ModelObject ourSecondary in secondaryObjects)
                                        {
                                            if (secObj.Identifier.ID == ourSecondary.Identifier.ID)
                                            {
                                                involvesSecondary = true;
                                                break;
                                            }
                                        }
                                        if (involvesSecondary) break;
                                    }
                                }
                            }
                            
                            // If connection involves both our primary and at least one secondary
                            if (involvesPrimary && involvesSecondary)
                            {
                                existingConnections.Add(new {
                                    id = connection.Identifier.ID,
                                    name = connection.Name,
                                    number = connection.Number
                                });
                            }
                        }
                        catch
                        {
                            // Skip connections we can't access
                            continue;
                        }
                    }
                }
                
                var result = new System.Collections.Generic.Dictionary<string, object>();
                result["hasExisting"] = existingConnections.Count > 0;
                result["existingConnections"] = existingConnections.ToArray();
                return result;
            }
            catch (Exception ex)
            {
                // If we can't check, assume no existing connections (safer to proceed)
                var errorResult = new System.Collections.Generic.Dictionary<string, object>();
                errorResult["hasExisting"] = false;
                errorResult["existingConnections"] = new object[0];
                errorResult["checkError"] = ex.Message;
                return errorResult;
            }
        }
        
        static string GetDefaultConnectionName(int connectionNumber)
        {
            switch (connectionNumber)
            {
                case 144: return "End plate";
                case 106: return "Apex bolted";
                case 141: return "Clip angle";
                case 1014: return "Base plate";
                case 11: return "Shear plate";
                default: return $"Connection {connectionNumber}";
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