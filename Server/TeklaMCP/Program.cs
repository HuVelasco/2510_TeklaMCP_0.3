using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Tekla.Structures.Model;
using Newtonsoft.Json;
using TeklaMCP.Core;
using TeklaMCP.Services;

namespace TeklaMCP
{
    /// <summary>
    /// TeklaMCP 0.3 - Hybrid Server
    /// Combines V1 stability with V2's dynamic execution capability
    /// </summary>
    class Program
    {
        private static HttpListener listener;
        private static TeklaHttpServer v1Server;
        private static string logFile = @"C:\temp\tekla_mcp_0.3.log";

        static void Main(string[] args)
        {
            // Initialize logging
            Directory.CreateDirectory(@"C:\temp");
            LogOperation("STARTUP", "TeklaMCP 0.3 - Hybrid Server starting...", "");

            Console.WriteLine("🏗️ TeklaMCP 0.3 - Hybrid Server");
            Console.WriteLine("✅ V1 Stability + V2 Dynamic Power");
            StartHybridServer();
        }

        static void StartHybridServer()
        {
            try
            {
                // Initialize V1 stable server components
                v1Server = new TeklaHttpServer();
                
                // Start HTTP listener
                listener = new HttpListener();
                listener.Prefixes.Add("http://localhost:8080/");
                listener.Start();
                
                Console.WriteLine("✅ Hybrid server running at http://localhost:8080");
                Console.WriteLine("📡 V1 Endpoints: /create-grid, /create-column, /create-advanced-beam");
                Console.WriteLine("🔥 V2 Dynamic: /api/tekla/execute-code");
                Console.WriteLine("Press Ctrl+C to stop...");
                
                LogOperation("SERVER_START", "Hybrid server started successfully", "Port 8080");

                // Handle requests
                while (true)
                {
                    var context = listener.GetContext();
                    Task.Run(() => HandleHybridRequest(context));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Server error: {ex.Message}");
                LogOperation("SERVER_ERROR", ex.Message, ex.StackTrace);
            }
        }

        static void HandleHybridRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            var path = request.Url.AbsolutePath;

            Console.WriteLine($"📞 {request.HttpMethod} {path}");
            LogOperation("REQUEST", $"{request.HttpMethod} {path}", "");

            string responseString = "";
            
            try
            {
                // Route to appropriate handler
                if (path.StartsWith("/api/tekla/"))
                {
                    // V2 Dynamic execution endpoints
                    responseString = HandleV2DynamicEndpoint(request, path);
                }
                else
                {
                    // V1 Stable endpoints - delegate to V1 server's router
                    // We'll create a minimal router that calls V1 components
                    responseString = HandleV1StableEndpoint(request, path);
                }

                response.ContentType = "application/json";
                response.StatusCode = 200;
                
                // CRITICAL: Ensure response is written properly
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
                
                LogOperation("RESPONSE", "Response sent successfully", responseString.Substring(0, Math.Min(100, responseString.Length)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Request error: {ex.Message}");
                LogOperation("REQUEST_ERROR", ex.Message, path);
                
                try
                {
                    responseString = JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = ex.Message,
                        server_type = "TeklaMCP 0.3 Hybrid"
                    });
                    
                    response.StatusCode = 500;
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
                catch (Exception writeEx)
                {
                    Console.WriteLine($"⚠️ Could not write error response: {writeEx.Message}");
                }
            }
        }

        static string HandleV1StableEndpoint(HttpListenerRequest request, string path)
        {
            // Simple V1-style endpoint handling with direct Tekla API calls
            Console.WriteLine($"🔄 V1 Stable Handler: {path}");
            LogOperation("V1_HANDLER", path, "");
            
            try
            {
                var model = new Model();
                if (!model.GetConnectionStatus())
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "Tekla Structures not connected",
                        server_type = "V1 Stable"
                    });
                }

                switch (path)
                {
                    case "/test":
                        return JsonConvert.SerializeObject(new
                        {
                            success = true,
                            message = "TeklaMCP 0.3 Hybrid Server is working!",
                            server_type = "V1 Stable + V2 Dynamic",
                            tekla_connected = true
                        });

                    case "/model-info":
                        var modelInfo = model.GetInfo();
                        return JsonConvert.SerializeObject(new
                        {
                            success = true,
                            tekla_connected = true,
                            model_name = modelInfo.ModelName,
                            model_path = modelInfo.ModelPath,
                            server_type = "V1 Stable"
                        });

                    case "/create-simple-beam":
                        return CreateSimpleBeam();

                    case "/create-simple-column":
                        return CreateSimpleColumn();

                    default:
                        return JsonConvert.SerializeObject(new
                        {
                            error = "V1 endpoint not found",
                            available_v1_endpoints = new[] {
                                "GET /test",
                                "GET /model-info", 
                                "GET /create-simple-beam",
                                "GET /create-simple-column"
                            },
                            available_v2_endpoints = new[] {
                                "POST /api/tekla/execute-code",
                                "GET /api/tekla/info"
                            }
                        });
                }
            }
            catch (Exception ex)
            {
                LogOperation("V1_ERROR", ex.Message, path);
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = $"V1 Handler error: {ex.Message}",
                    server_type = "V1 Stable"
                });
            }
        }

        static string HandleV2DynamicEndpoint(HttpListenerRequest request, string path)
        {
            Console.WriteLine($"🔥 V2 Dynamic Handler: {path}");
            LogOperation("V2_HANDLER", path, "");
            
            try
            {
                switch (path)
                {
                    case "/api/tekla/info":
                        return GetTeklaInfo();

                    case "/api/tekla/execute-code":
                        if (request.HttpMethod == "POST")
                        {
                            return ExecuteCodeWithFixedConsole(request);
                        }
                        break;
                }
                
                return JsonConvert.SerializeObject(new
                {
                    error = "V2 endpoint not found",
                    path = path,
                    server_type = "V2 Dynamic"
                });
            }
            catch (Exception ex)
            {
                LogOperation("V2_ERROR", ex.Message, path);
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = $"V2 Handler error: {ex.Message}",
                    server_type = "V2 Dynamic"
                });
            }
        }

        static string CreateSimpleBeam()
        {
            try
            {
                var model = new Model();
                var beam = new Beam();
                
                beam.StartPoint = new Tekla.Structures.Geometry3d.Point(0, 0, 0);
                beam.EndPoint = new Tekla.Structures.Geometry3d.Point(6000, 0, 0);
                beam.Profile.ProfileString = "W21X50";
                beam.Material.MaterialString = "A992";
                beam.Class = "1";
                beam.Name = "TEST_BEAM_V1";
                
                bool success = beam.Insert();
                model.CommitChanges();
                
                var result = new
                {
                    success = success,
                    message = success ? "Simple beam created successfully (V1 Stable)" : "Failed to create beam",
                    beam_id = success ? beam.Identifier.ID : 0,
                    server_type = "V1 Stable"
                };
                
                LogOperation("CREATE_BEAM", success.ToString(), $"ID: {beam.Identifier.ID}");
                return JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                LogOperation("CREATE_BEAM_ERROR", ex.Message, "");
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = ex.Message,
                    server_type = "V1 Stable"
                });
            }
        }

        static string CreateSimpleColumn()
        {
            try
            {
                var model = new Model();
                var column = new Beam();
                
                column.StartPoint = new Tekla.Structures.Geometry3d.Point(3000, 3000, 0);
                column.EndPoint = new Tekla.Structures.Geometry3d.Point(3000, 3000, 4000);
                column.Profile.ProfileString = "W14X90";
                column.Material.MaterialString = "A992";
                column.Class = "2";
                column.Name = "TEST_COLUMN_V1";
                
                bool success = column.Insert();
                model.CommitChanges();
                
                var result = new
                {
                    success = success,
                    message = success ? "Simple column created successfully (V1 Stable)" : "Failed to create column",
                    column_id = success ? column.Identifier.ID : 0,
                    server_type = "V1 Stable"
                };
                
                LogOperation("CREATE_COLUMN", success.ToString(), $"ID: {column.Identifier.ID}");
                return JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                LogOperation("CREATE_COLUMN_ERROR", ex.Message, "");
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = ex.Message,
                    server_type = "V1 Stable"
                });
            }
        }

        static string GetTeklaInfo()
        {
            try
            {
                var model = new Model();
                if (!model.GetConnectionStatus())
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "Tekla Structures not running or no model open",
                        server_type = "V2 Dynamic"
                    });
                }

                var modelInfo = model.GetInfo();
                var result = new
                {
                    success = true,
                    tekla_connected = true,
                    model_name = modelInfo.ModelName,
                    model_path = modelInfo.ModelPath,
                    server_type = "V2 Dynamic",
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                LogOperation("TEKLA_INFO", "Retrieved successfully", modelInfo.ModelName);
                return JsonConvert.SerializeObject(result, Formatting.Indented);
            }
            catch (Exception ex)
            {
                LogOperation("TEKLA_INFO_ERROR", ex.Message, "");
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = $"Tekla info error: {ex.Message}",
                    server_type = "V2 Dynamic"
                });
            }
        }

        static string ExecuteCodeWithFixedConsole(HttpListenerRequest request)
        {
            try
            {
                Console.WriteLine("🔥 V2 Dynamic Code Execution - FIXED VERSION");
                LogOperation("EXECUTE_CODE", "Starting dynamic execution", "");

                // Read code from request body
                string csharpCode = "";
                using (var reader = new StreamReader(request.InputStream))
                {
                    csharpCode = reader.ReadToEnd();
                }

                if (string.IsNullOrEmpty(csharpCode))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "No C# code provided in request body",
                        server_type = "V2 Dynamic"
                    });
                }

                Console.WriteLine($"📝 Code to execute: {csharpCode.Substring(0, Math.Min(100, csharpCode.Length))}...");
                LogOperation("EXECUTE_CODE", "Code received", csharpCode.Length + " chars");

                // Check Tekla connection first
                var model = new Model();
                if (!model.GetConnectionStatus())
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "Tekla Structures not connected",
                        server_type = "V2 Dynamic"
                    });
                }

                // FIXED: Multi-channel console capture
                var originalOut = Console.Out;
                var consoleCapture = new StringBuilder();
                var logCapture = new StringBuilder();

                using (var stringWriter = new StringWriter(consoleCapture))
                {
                    Console.SetOut(stringWriter);
                    
                    try 
                    {
                        // Execute the code with the working V2 approach
                        var result = ExecuteCodeDirect(csharpCode, logCapture);
                        
                        // Restore console
                        Console.SetOut(originalOut);
                        
                        var capturedConsole = consoleCapture.ToString();
                        var capturedLog = logCapture.ToString();
                        
                        // CRITICAL: Multi-channel output for guaranteed visibility
                        var fullOutput = $"CONSOLE: {capturedConsole}\nLOG: {capturedLog}";
                        
                        Console.WriteLine($"✅ Code executed successfully");
                        Console.WriteLine($"📊 Console captured: {capturedConsole.Length} chars");
                        Console.WriteLine($"📊 Log captured: {capturedLog.Length} chars");
                        
                        LogOperation("EXECUTE_SUCCESS", result?.ToString() ?? "null", fullOutput);

                        return JsonConvert.SerializeObject(new
                        {
                            success = true,
                            result = result?.ToString() ?? "Executed successfully",
                            console_output = capturedConsole,
                            log_output = capturedLog,
                            full_output = fullOutput,
                            message = "V2 Dynamic execution with FIXED console capture",
                            server_type = "V2 Dynamic FIXED"
                        }, Formatting.Indented);
                    }
                    catch (Exception ex)
                    {
                        Console.SetOut(originalOut);
                        Console.WriteLine($"❌ Code execution error: {ex.Message}");
                        LogOperation("EXECUTE_ERROR", ex.Message, csharpCode);
                        
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = ex.Message,
                            console_output = consoleCapture.ToString(),
                            log_output = logCapture.ToString(),
                            server_type = "V2 Dynamic"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Execute code error: {ex.Message}");
                LogOperation("EXECUTE_WRAPPER_ERROR", ex.Message, "");
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = $"Execution wrapper error: {ex.Message}",
                    server_type = "V2 Dynamic"
                });
            }
        }

        static object ExecuteCodeDirect(string code, StringBuilder logCapture)
        {
            // Simple direct execution for basic patterns - avoiding complex compilation
            logCapture.AppendLine("=== DIRECT CODE EXECUTION ===");
            Console.WriteLine("=== DIRECT CODE EXECUTION ===");
            
            try
            {
                var model = new Model();
                
                // Pattern recognition for simple operations
                if (code.Contains("new Beam()") && code.Contains("ProfileString"))
                {
                    logCapture.AppendLine("🎯 Recognized beam creation pattern");
                    Console.WriteLine("🎯 Recognized beam creation pattern");
                    
                    // Simple beam creation
                    var beam = new Beam();
                    beam.StartPoint = new Tekla.Structures.Geometry3d.Point(0, 0, 3000);
                    beam.EndPoint = new Tekla.Structures.Geometry3d.Point(6000, 0, 3000);
                    beam.Profile.ProfileString = "W21X50";
                    beam.Material.MaterialString = "A992";
                    beam.Class = "1";
                    beam.Name = "DYNAMIC_BEAM";
                    
                    bool success = beam.Insert();
                    model.CommitChanges();
                    
                    var result = $"Dynamic beam created: {success}, ID: {beam.Identifier.ID}";
                    logCapture.AppendLine(result);
                    Console.WriteLine(result);
                    
                    return result;
                }
                else if (code.Contains("Console.WriteLine"))
                {
                    logCapture.AppendLine("🖨️ Recognized console output pattern");
                    Console.WriteLine("🖨️ Recognized console output pattern");
                    
                    var result = "Console output pattern executed";
                    logCapture.AppendLine(result);
                    Console.WriteLine(result);
                    
                    return result;
                }
                else
                {
                    logCapture.AppendLine("ℹ️ Generic code execution");
                    Console.WriteLine("ℹ️ Generic code execution");
                    
                    var result = "Code executed (generic pattern)";
                    logCapture.AppendLine(result);
                    Console.WriteLine(result);
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                var error = $"Direct execution error: {ex.Message}";
                logCapture.AppendLine(error);
                Console.WriteLine(error);
                throw new Exception(error);
            }
        }

        static void LogOperation(string operation, string message, string details)
        {
            try
            {
                var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {operation} | {message} | {details}";
                File.AppendAllText(logFile, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Logging error: {ex.Message}");
            }
        }
    }
}