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
using System.Collections.Generic;

namespace TeklaMCP
{
    /// <summary>
    /// BeamRequest - Type-safe data model for beam creation
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
        public string Class { get; set; } = "1";
        public string Name { get; set; } = "BEAM";

        // Position parameters
        public string PositionDepth { get; set; } = "BEHIND";
        public string PositionPlane { get; set; } = "MIDDLE";
        public string PositionRotation { get; set; } = "TOP";
        
        // Rotation parameters (UBIM enhancement)
        public string RotationEnum { get; set; } = "TOP";
        public double RotationOffset { get; set; } = 0.0;
    }

    /// <summary>
    /// PartFilter - Filter criteria for searching parts
    /// </summary>
    public class PartFilter
    {
        public string PartType { get; set; }  // "Beam", "ContourPlate", "PolyBeam"
        public string Material { get; set; }
        public string Profile { get; set; }
        public string Class { get; set; }
        public string Name { get; set; }
        public int? MaxResults { get; set; } = 100;
    }

    /// <summary>
    /// UpdatePartRequest - Request model for updating part properties
    /// </summary>
    public class UpdatePartRequest
    {
        public string Material { get; set; }
        public string Profile { get; set; }
        public string Class { get; set; }
        public string Name { get; set; }
        public string PositionDepth { get; set; }
        public string PositionPlane { get; set; }
        public string PositionRotation { get; set; }
    }

    /// <summary>
    /// MovePartRequest - Request model for moving/repositioning parts
    /// </summary>
    public class MovePartRequest
    {
        public double? NewStartX { get; set; }
        public double? NewStartY { get; set; }
        public double? NewStartZ { get; set; }
        public double? NewEndX { get; set; }
        public double? NewEndY { get; set; }
        public double? NewEndZ { get; set; }
    }

    /// <summary>
    /// BulkDeleteRequest - Request model for bulk deletion
    /// </summary>
    public class BulkDeleteRequest
    {
        public List<int> PartIds { get; set; }
        public bool VerifyBeforeDelete { get; set; } = true;
    }

    /// <summary>
    /// ETABS-Tekla Bridge Data Models
    /// </summary>
    public class UBIMImportRequest
    {
        public UBIMData ubim_data { get; set; }
        public int frame_count { get; set; }
    }

    public class UBIMData
    {
        public string version { get; set; }
        public string source { get; set; }
        public UBIMUnits units { get; set; }
        public List<UBIMFrame> frames { get; set; }
    }

    public class UBIMUnits
    {
        public string length { get; set; }
        public string force { get; set; }
        public string angle { get; set; }
    }

    public class UBIMFrame
    {
        public string id { get; set; }
        public string guid { get; set; }  // ETABS GUID for change tracking
        public string type { get; set; }  // column|beam|brace|other
        public string design_orientation { get; set; }  // ETABS classification
        public double local_angle { get; set; }  // Local rotation angle in degrees
        public UBIMGeometry geometry { get; set; }
        public UBIMProperties properties { get; set; }
    }

    public class UBIMGeometry
    {
        public UBIMPoint start { get; set; }
        public UBIMPoint end { get; set; }
        public double length { get; set; }
    }

    public class UBIMPoint
    {
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }
        public string point_id { get; set; }
    }

    public class UBIMProperties
    {
        public string profile { get; set; }
        public string material { get; set; }
        public string story { get; set; }
        public int cardinal_point { get; set; }  // 1-11 from ETABS
        public string frame_class { get; set; }  // Optional user-defined subtype (e.g., "MAIN", "SECONDARY")
    }


    public class ConnectTeklaRequest
    {
        public bool auto_start { get; set; } = false;
    }

    /// <summary>
    /// Drawing extraction request for ML dataset generation
    /// </summary>
    public class DrawingExtractionRequest
    {
        public string DrawingMark { get; set; }  // Null = process all drawings
        public int MaxDrawings { get; set; } = 10;
        public bool DiscoveryMode { get; set; } = true;
    }

    /// <summary>
    /// TeklaMCP 0.3 PHASE 4 - Part-Based CRUD Operations
    /// 
    /// PHASE 4 IMPROVEMENTS:
    /// - Complete CRUD operations for Part objects (Beam, ContourPlate, PolyBeam)
    /// - READ: List, Get, Search with filtering
    /// - UPDATE: Modify properties and positions
    /// - DELETE: Single and bulk deletion with verification
    /// - Agent-friendly direct parameters
    /// - Builds on Phase 3 clean architecture
    /// 
    /// ARCHITECTURE:
    /// - Smart Part-based polymorphic operations
    /// - Type-safe request/response models
    /// - Comprehensive error handling with context
    /// - Legacy C# compiler support (C# 5.0 syntax)
    /// </summary>
    class ProgramClean
    {
        private static HttpListener listener;
        
        // ==================== PHASE 3: CONFIGURATION MANAGEMENT ====================
        
        public class TeklaConfig
        {
            public string ServerUrl { get; set; } = "http://localhost:5050/";
            public string LogDirectory { get; set; } = @"C:\temp";
            public string LogFile { get; set; } = @"C:\temp\tekla_mcp_phase3.log";
            public int RequestTimeout { get; set; } = 30000;
            public bool EnableDetailedLogging { get; set; } = true;
        }
        
        private static TeklaConfig config = new TeklaConfig();

        // ==================== PHASE 3: ENDPOINT HANDLER REGISTRY ====================
        
        private static Dictionary<string, Func<HttpListenerRequest, string>> endpointHandlers;
        
        static void InitializeEndpointHandlers()
        {
            endpointHandlers = new Dictionary<string, Func<HttpListenerRequest, string>>
            {
                // Existing endpoints
                { "GET /test", _ => HandleTest() },
                { "GET /tekla-info", _ => HandleTeklaInfo() },
                { "POST /create-beam", HandleCreateBeam },
                { "POST /execute-code", HandleExecuteCode },
                
                // PHASE 4: CRUD Operations
                // READ operations
                { "GET /list-parts", HandleListParts },
                { "GET /get-part", HandleGetPart },  // Uses query param ?id=123
                { "POST /search-parts", HandleSearchParts },
                
                // UPDATE operations
                { "POST /update-part", HandleUpdatePart },  // Body contains id and updates
                { "POST /move-part", HandleMovePart },      // Body contains id and new positions
                
                // DELETE operations
                { "POST /delete-part", HandleDeletePart },  // Body contains id
                { "POST /delete-parts-bulk", HandleDeletePartsBulk },  // Body contains array of ids
                
                // ETABS-TEKLA BRIDGE operations
                { "POST /import-ubim-frames", HandleImportUBIMFrames },
                { "POST /connect-tekla", HandleConnectTekla },

                // ML DATASET EXTRACTION operations
                { "POST /extract-drawing-objects", HandleExtractDrawingObjects }
            };
        }

        // ==================== STANDARDIZED RESPONSE BUILDERS ====================
        
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

        // ==================== MAIN APPLICATION ====================
        
        static void Main(string[] args)
        {
            Directory.CreateDirectory(config.LogDirectory);
            InitializeEndpointHandlers();
            LogMessage("STARTUP", "TeklaMCP 0.3 PHASE 3 - Architectural Server starting...");

            Console.WriteLine("🏗️ TeklaMCP 0.3 - ETABS-Tekla Bridge");
            Console.WriteLine("✅ Complete CRUD operations for Part objects");
            Console.WriteLine("📊 READ: List, Get, Search with filtering");
            Console.WriteLine("✏️ UPDATE: Modify properties and positions");
            Console.WriteLine("🗑️ DELETE: Single and bulk with verification");
            Console.WriteLine("🌉 BRIDGE: UBIM import from ETABS exports");
            Console.WriteLine("📐 GEOMETRY: Exact coordinates (mm) preservation");
            Console.WriteLine("🎯 POSITIONING: MIDDLE/MIDDLE/TOP for accuracy");
            StartServer();
        }

        static void StartServer()
        {
            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add(config.ServerUrl);
                listener.Start();
                
                Console.WriteLine($"✅ Phase 3 server running at {config.ServerUrl}");
                Console.WriteLine("📡 Available Endpoints:");
                foreach (var endpoint in endpointHandlers.Keys)
                {
                    Console.WriteLine($"  {endpoint}");
                }
                Console.WriteLine("Press Ctrl+C to stop...");
                
                LogMessage("SERVER_START", $"Phase 4 CRUD server started on {config.ServerUrl}");

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

        // ==================== PHASE 3: CLEAN REQUEST HANDLING ====================
        
        static void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            var requestKey = $"{request.HttpMethod} {request.Url.AbsolutePath}";

            Console.WriteLine($"📞 {requestKey}");
            LogMessage("REQUEST", requestKey);

            string responseString;
            
            try
            {
                // Phase 3 endpoint-specific handler lookup
                if (endpointHandlers.ContainsKey(requestKey))
                {
                    responseString = endpointHandlers[requestKey](request);
                }
                else
                {
                    responseString = BuildErrorResponse("Endpoint not found", "routing", new {
                        requested_endpoint = requestKey,
                        available_endpoints = endpointHandlers.Keys.ToArray()
                    });
                }

                SendResponse(response, responseString, 200);
                LogMessage("RESPONSE_SENT", "Response sent successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Request error: {ex.Message}");
                LogMessage("REQUEST_ERROR", $"Error: {ex.Message}");
                
                var errorResponse = BuildErrorResponse(ex.Message, "request-handling", new { exception = ex.GetType().Name });
                SendResponse(response, errorResponse, 500);
            }
        }
        
        private static void SendResponse(HttpListenerResponse response, string content, int statusCode)
        {
            try
            {
                response.ContentType = "application/json";
                response.StatusCode = statusCode;
                
                byte[] buffer = Encoding.UTF8.GetBytes(content);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                LogMessage("SEND_RESPONSE_ERROR", $"Error sending response: {ex.Message}");
            }
        }

        // ==================== ENDPOINT HANDLERS ====================
        
        static string HandleTest()
        {
            LogMessage("TEST_SUCCESS", "Test endpoint called successfully");
            return BuildSuccessResponse(new {
                server_version = "TeklaMCP 0.3 PHASE 4",
                architecture = "Part-Based CRUD Operations",
                crud_operations = new[] {
                    "CREATE: Beam creation (existing)",
                    "READ: List, Get, Search parts",
                    "UPDATE: Modify properties and positions",
                    "DELETE: Single and bulk removal"
                },
                supported_parts = new[] { "Beam", "ContourPlate", "PolyBeam" }
            }, "TeklaMCP Phase 4 CRUD server is working!");
        }

        static string HandleTeklaInfo()
        {
            try
            {
                var model = new Model();
                if (!model.GetConnectionStatus())
                {
                    return BuildErrorResponse("Tekla Structures not running or no model open", "tekla-info");
                }

                var modelInfo = model.GetInfo();
                LogMessage("TEKLA_INFO_SUCCESS", $"Retrieved model info: {modelInfo.ModelName}");
                
                return BuildSuccessResponse(new {
                    tekla_connected = true,
                    model_name = modelInfo.ModelName,
                    model_path = modelInfo.ModelPath,
                    server_architecture = "Phase 3 Clean Handlers"
                });
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

                // Read and validate request
                string requestBody = ReadRequestBody(request);
                if (string.IsNullOrEmpty(requestBody))
                {
                    return BuildErrorResponse("Request body required with BeamRequest JSON", "create-beam");
                }

                var beamRequest = JsonConvert.DeserializeObject<BeamRequest>(requestBody);
                var beam = CreateBeamFromRequest(beamRequest);

                bool success = beam.Insert();
                model.CommitChanges();
                
                if (success)
                {
                    LogMessage("BEAM_CREATED", $"Beam created, ID: {beam.Identifier.ID}");
                    return BuildSuccessResponse(new {
                        beam_id = beam.Identifier.ID,
                        profile = beam.Profile.ProfileString,
                        material = beam.Material.MaterialString,
                        start_point = new { x = beam.StartPoint.X, y = beam.StartPoint.Y, z = beam.StartPoint.Z },
                        end_point = new { x = beam.EndPoint.X, y = beam.EndPoint.Y, z = beam.EndPoint.Z }
                    }, "Beam created successfully");
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
                string code = ReadRequestBody(request);
                if (string.IsNullOrEmpty(code))
                {
                    return BuildErrorResponse("Code required in request body", "execute-code");
                }

                Console.WriteLine($"📝 Code to execute: {code.Substring(0, Math.Min(100, code.Length))}...");
                LogMessage("EXECUTE_CODE_START", $"Code received: {code.Length} chars");

                string result = ExecuteCodeWithCapture(code);
                LogMessage("EXECUTE_SUCCESS", "Code executed successfully");
                
                return BuildSuccessResponse(new {
                    result = result,
                    code_length = code.Length
                }, "Code executed successfully");
            }
            catch (Exception ex)
            {
                LogMessage("EXECUTE_ERROR", $"Error: {ex.Message}");
                return BuildErrorResponse($"Code execution error: {ex.Message}", "execute-code", new { exception = ex.GetType().Name });
            }
        }

        // ==================== PHASE 4: CRUD HANDLERS ====================
        
        // READ Operations
        
        static string HandleListParts(HttpListenerRequest request)
        {
            try
            {
                var model = new Model();
                if (!model.GetConnectionStatus())
                {
                    return BuildErrorResponse("Tekla not connected", "list-parts");
                }

                var parts = new List<object>();
                var selector = model.GetModelObjectSelector();
                var enumerator = selector.GetAllObjectsWithType(ModelObject.ModelObjectEnum.BEAM);
                // Also get plates and other parts
                enumerator.SelectInstances = false;
                
                int count = 0;
                int maxResults = 100;  // Default limit
                
                while (enumerator.MoveNext() && count < maxResults)
                {
                    var part = enumerator.Current as Part;
                    if (part != null)
                    {
                        parts.Add(GetPartInfo(part));
                        count++;
                    }
                }
                
                LogMessage("LIST_PARTS_SUCCESS", $"Retrieved {parts.Count} parts");
                return BuildSuccessResponse(new {
                    total_parts = parts.Count,
                    parts = parts
                }, $"Retrieved {parts.Count} parts from model");
            }
            catch (Exception ex)
            {
                LogMessage("LIST_PARTS_ERROR", $"Error: {ex.Message}");
                return BuildErrorResponse($"List parts error: {ex.Message}", "list-parts");
            }
        }
        
        static string HandleGetPart(HttpListenerRequest request)
        {
            try
            {
                var model = new Model();
                if (!model.GetConnectionStatus())
                {
                    return BuildErrorResponse("Tekla not connected", "get-part");
                }

                // Parse ID from query string
                var queryString = request.Url.Query;
                if (string.IsNullOrEmpty(queryString) || !queryString.Contains("id="))
                {
                    return BuildErrorResponse("Part ID required in query string (?id=123)", "get-part");
                }
                
                var idString = queryString.Replace("?id=", "").Split('&')[0];
                int partId;
                if (!int.TryParse(idString, out partId))
                {
                    return BuildErrorResponse("Invalid part ID format", "get-part");
                }
                
                var part = model.SelectModelObject(new Tekla.Structures.Identifier(partId)) as Part;
                if (part == null)
                {
                    return BuildErrorResponse($"Part with ID {partId} not found", "get-part");
                }
                
                var partInfo = GetDetailedPartInfo(part);
                LogMessage("GET_PART_SUCCESS", $"Retrieved part {partId}");
                
                return BuildSuccessResponse(partInfo, $"Part {partId} retrieved successfully");
            }
            catch (Exception ex)
            {
                LogMessage("GET_PART_ERROR", $"Error: {ex.Message}");
                return BuildErrorResponse($"Get part error: {ex.Message}", "get-part");
            }
        }
        
        static string HandleSearchParts(HttpListenerRequest request)
        {
            try
            {
                var model = new Model();
                if (!model.GetConnectionStatus())
                {
                    return BuildErrorResponse("Tekla not connected", "search-parts");
                }

                string requestBody = ReadRequestBody(request);
                var filter = string.IsNullOrEmpty(requestBody) 
                    ? new PartFilter() 
                    : JsonConvert.DeserializeObject<PartFilter>(requestBody);
                
                var parts = new List<object>();
                var selector = model.GetModelObjectSelector();
                var enumerator = selector.GetAllObjectsWithType(ModelObject.ModelObjectEnum.BEAM);
                // Also get plates and other parts
                enumerator.SelectInstances = false;
                
                int count = 0;
                int maxResults = filter.MaxResults ?? 100;
                
                while (enumerator.MoveNext() && count < maxResults)
                {
                    var part = enumerator.Current as Part;
                    if (part != null && MatchesFilter(part, filter))
                    {
                        parts.Add(GetPartInfo(part));
                        count++;
                    }
                }
                
                LogMessage("SEARCH_PARTS_SUCCESS", $"Found {parts.Count} matching parts");
                return BuildSuccessResponse(new {
                    filter = filter,
                    results_count = parts.Count,
                    parts = parts
                }, $"Found {parts.Count} parts matching filter");
            }
            catch (Exception ex)
            {
                LogMessage("SEARCH_PARTS_ERROR", $"Error: {ex.Message}");
                return BuildErrorResponse($"Search parts error: {ex.Message}", "search-parts");
            }
        }
        
        // UPDATE Operations
        
        static string HandleUpdatePart(HttpListenerRequest request)
        {
            try
            {
                var model = new Model();
                if (!model.GetConnectionStatus())
                {
                    return BuildErrorResponse("Tekla not connected", "update-part");
                }

                string requestBody = ReadRequestBody(request);
                if (string.IsNullOrEmpty(requestBody))
                {
                    return BuildErrorResponse("Request body required with part ID and updates", "update-part");
                }
                
                var updateRequest = JsonConvert.DeserializeObject<Dictionary<string, object>>(requestBody);
                if (!updateRequest.ContainsKey("id"))
                {
                    return BuildErrorResponse("Part ID required in request body", "update-part");
                }
                
                int partId = Convert.ToInt32(updateRequest["id"]);
                var part = model.SelectModelObject(new Tekla.Structures.Identifier(partId)) as Part;
                
                if (part == null)
                {
                    return BuildErrorResponse($"Part with ID {partId} not found", "update-part");
                }
                
                // Apply updates
                bool modified = false;
                
                if (updateRequest.ContainsKey("material"))
                {
                    part.Material.MaterialString = updateRequest["material"].ToString();
                    modified = true;
                }
                
                if (updateRequest.ContainsKey("profile"))
                {
                    part.Profile.ProfileString = updateRequest["profile"].ToString();
                    modified = true;
                }
                
                if (updateRequest.ContainsKey("class"))
                {
                    part.Class = updateRequest["class"].ToString();
                    modified = true;
                }
                
                if (updateRequest.ContainsKey("name"))
                {
                    part.Name = updateRequest["name"].ToString();
                    modified = true;
                }
                
                // Apply position updates if present
                if (updateRequest.ContainsKey("position_depth"))
                {
                    part.Position.Depth = (Position.DepthEnum)Enum.Parse(
                        typeof(Position.DepthEnum), 
                        updateRequest["position_depth"].ToString().ToUpper()
                    );
                    modified = true;
                }
                
                if (updateRequest.ContainsKey("position_plane"))
                {
                    part.Position.Plane = (Position.PlaneEnum)Enum.Parse(
                        typeof(Position.PlaneEnum), 
                        updateRequest["position_plane"].ToString().ToUpper()
                    );
                    modified = true;
                }
                
                if (updateRequest.ContainsKey("position_rotation"))
                {
                    part.Position.Rotation = (Position.RotationEnum)Enum.Parse(
                        typeof(Position.RotationEnum), 
                        updateRequest["position_rotation"].ToString().ToUpper()
                    );
                    modified = true;
                }
                
                if (modified)
                {
                    bool success = part.Modify();
                    model.CommitChanges();
                    
                    if (success)
                    {
                        LogMessage("UPDATE_PART_SUCCESS", $"Part {partId} updated");
                        return BuildSuccessResponse(new {
                            part_id = partId,
                            updated = true,
                            new_values = GetPartInfo(part)
                        }, $"Part {partId} updated successfully");
                    }
                    else
                    {
                        return BuildErrorResponse($"Failed to modify part {partId}", "update-part");
                    }
                }
                else
                {
                    return BuildSuccessResponse(new {
                        part_id = partId,
                        updated = false,
                        message = "No modifications requested"
                    }, "No changes applied");
                }
            }
            catch (Exception ex)
            {
                LogMessage("UPDATE_PART_ERROR", $"Error: {ex.Message}");
                return BuildErrorResponse($"Update part error: {ex.Message}", "update-part");
            }
        }
        
        static string HandleMovePart(HttpListenerRequest request)
        {
            try
            {
                var model = new Model();
                if (!model.GetConnectionStatus())
                {
                    return BuildErrorResponse("Tekla not connected", "move-part");
                }

                string requestBody = ReadRequestBody(request);
                if (string.IsNullOrEmpty(requestBody))
                {
                    return BuildErrorResponse("Request body required with part ID and new positions", "move-part");
                }
                
                var moveRequest = JsonConvert.DeserializeObject<Dictionary<string, object>>(requestBody);
                if (!moveRequest.ContainsKey("id"))
                {
                    return BuildErrorResponse("Part ID required in request body", "move-part");
                }
                
                int partId = Convert.ToInt32(moveRequest["id"]);
                var part = model.SelectModelObject(new Tekla.Structures.Identifier(partId)) as Part;
                
                if (part == null)
                {
                    return BuildErrorResponse($"Part with ID {partId} not found", "move-part");
                }
                
                // Handle Beam-specific movement
                var beam = part as Beam;
                if (beam != null)
                {
                    if (moveRequest.ContainsKey("start_x")) beam.StartPoint.X = Convert.ToDouble(moveRequest["start_x"]);
                    if (moveRequest.ContainsKey("start_y")) beam.StartPoint.Y = Convert.ToDouble(moveRequest["start_y"]);
                    if (moveRequest.ContainsKey("start_z")) beam.StartPoint.Z = Convert.ToDouble(moveRequest["start_z"]);
                    if (moveRequest.ContainsKey("end_x")) beam.EndPoint.X = Convert.ToDouble(moveRequest["end_x"]);
                    if (moveRequest.ContainsKey("end_y")) beam.EndPoint.Y = Convert.ToDouble(moveRequest["end_y"]);
                    if (moveRequest.ContainsKey("end_z")) beam.EndPoint.Z = Convert.ToDouble(moveRequest["end_z"]);
                    
                    bool success = beam.Modify();
                    model.CommitChanges();
                    
                    if (success)
                    {
                        LogMessage("MOVE_PART_SUCCESS", $"Part {partId} moved");
                        return BuildSuccessResponse(new {
                            part_id = partId,
                            moved = true,
                            new_position = new {
                                start = new { x = beam.StartPoint.X, y = beam.StartPoint.Y, z = beam.StartPoint.Z },
                                end = new { x = beam.EndPoint.X, y = beam.EndPoint.Y, z = beam.EndPoint.Z }
                            }
                        }, $"Part {partId} moved successfully");
                    }
                }
                
                return BuildErrorResponse("Move operation failed or part type not supported", "move-part");
            }
            catch (Exception ex)
            {
                LogMessage("MOVE_PART_ERROR", $"Error: {ex.Message}");
                return BuildErrorResponse($"Move part error: {ex.Message}", "move-part");
            }
        }
        
        // DELETE Operations
        
        static string HandleDeletePart(HttpListenerRequest request)
        {
            try
            {
                var model = new Model();
                if (!model.GetConnectionStatus())
                {
                    return BuildErrorResponse("Tekla not connected", "delete-part");
                }

                string requestBody = ReadRequestBody(request);
                if (string.IsNullOrEmpty(requestBody))
                {
                    return BuildErrorResponse("Request body required with part ID", "delete-part");
                }
                
                var deleteRequest = JsonConvert.DeserializeObject<Dictionary<string, object>>(requestBody);
                if (!deleteRequest.ContainsKey("id"))
                {
                    return BuildErrorResponse("Part ID required in request body", "delete-part");
                }
                
                int partId = Convert.ToInt32(deleteRequest["id"]);
                var part = model.SelectModelObject(new Tekla.Structures.Identifier(partId)) as Part;
                
                if (part == null)
                {
                    return BuildErrorResponse($"Part with ID {partId} not found", "delete-part");
                }
                
                // Store part info before deletion
                var partInfo = GetPartInfo(part);
                
                bool success = part.Delete();
                model.CommitChanges();
                
                if (success)
                {
                    LogMessage("DELETE_PART_SUCCESS", $"Part {partId} deleted");
                    return BuildSuccessResponse(new {
                        part_id = partId,
                        deleted = true,
                        deleted_part_info = partInfo
                    }, $"Part {partId} deleted successfully");
                }
                else
                {
                    return BuildErrorResponse($"Failed to delete part {partId}", "delete-part");
                }
            }
            catch (Exception ex)
            {
                LogMessage("DELETE_PART_ERROR", $"Error: {ex.Message}");
                return BuildErrorResponse($"Delete part error: {ex.Message}", "delete-part");
            }
        }
        
        static string HandleDeletePartsBulk(HttpListenerRequest request)
        {
            try
            {
                var model = new Model();
                if (!model.GetConnectionStatus())
                {
                    return BuildErrorResponse("Tekla not connected", "delete-parts-bulk");
                }

                string requestBody = ReadRequestBody(request);
                if (string.IsNullOrEmpty(requestBody))
                {
                    return BuildErrorResponse("Request body required with part IDs array", "delete-parts-bulk");
                }
                
                var deleteRequest = JsonConvert.DeserializeObject<BulkDeleteRequest>(requestBody);
                if (deleteRequest.PartIds == null || deleteRequest.PartIds.Count == 0)
                {
                    return BuildErrorResponse("Part IDs array required and cannot be empty", "delete-parts-bulk");
                }
                
                var deletedParts = new List<object>();
                var failedParts = new List<object>();
                
                foreach (var partId in deleteRequest.PartIds)
                {
                    try
                    {
                        var part = model.SelectModelObject(new Tekla.Structures.Identifier(partId)) as Part;
                        if (part != null)
                        {
                            var partInfo = GetPartInfo(part);
                            
                            if (part.Delete())
                            {
                                deletedParts.Add(new { id = partId, info = partInfo });
                            }
                            else
                            {
                                failedParts.Add(new { id = partId, reason = "Delete operation failed" });
                            }
                        }
                        else
                        {
                            failedParts.Add(new { id = partId, reason = "Part not found" });
                        }
                    }
                    catch (Exception ex)
                    {
                        failedParts.Add(new { id = partId, reason = ex.Message });
                    }
                }
                
                model.CommitChanges();
                
                LogMessage("DELETE_BULK_COMPLETE", $"Deleted {deletedParts.Count}, Failed {failedParts.Count}");
                
                return BuildSuccessResponse(new {
                    total_requested = deleteRequest.PartIds.Count,
                    deleted_count = deletedParts.Count,
                    failed_count = failedParts.Count,
                    deleted_parts = deletedParts,
                    failed_parts = failedParts
                }, $"Bulk delete completed: {deletedParts.Count} deleted, {failedParts.Count} failed");
            }
            catch (Exception ex)
            {
                LogMessage("DELETE_BULK_ERROR", $"Error: {ex.Message}");
                return BuildErrorResponse($"Bulk delete error: {ex.Message}", "delete-parts-bulk");
            }
        }

        // ==================== ETABS-TEKLA BRIDGE HANDLERS ====================
        
        static string HandleImportUBIMFrames(HttpListenerRequest request)
        {
            try
            {
                var model = new Model();
                if (!model.GetConnectionStatus())
                {
                    return BuildErrorResponse("Tekla not connected", "import-ubim-frames");
                }

                string requestBody = ReadRequestBody(request);
                if (string.IsNullOrEmpty(requestBody))
                {
                    return BuildErrorResponse("UBIM data required in request body", "import-ubim-frames");
                }

                var importRequest = JsonConvert.DeserializeObject<UBIMImportRequest>(requestBody);
                var ubimData = importRequest.ubim_data;
                
                if (ubimData?.frames == null || ubimData.frames.Count == 0)
                {
                    return BuildErrorResponse("No frames found in UBIM data", "import-ubim-frames");
                }

                LogMessage("UBIM_IMPORT_START", $"Starting import of {ubimData.frames.Count} frames");

                var createdBeams = new List<object>();
                var failedFrames = new List<object>();
                var stats = new Dictionary<string, int>
                {
                    ["total_frames"] = ubimData.frames.Count,
                    ["beams"] = 0,
                    ["columns"] = 0,
                    ["braces"] = 0,
                    ["created"] = 0,
                    ["failed"] = 0
                };

                foreach (var frame in ubimData.frames)
                {
                    try
                    {
                        // Safe increment of frame type count
                        if (stats.ContainsKey(frame.type))
                        {
                            stats[frame.type]++;
                        }
                        else
                        {
                            stats[frame.type] = 1;
                        }
                        
                        // Create beam from UBIM frame
                        var beam = CreateBeamFromUBIMFrame(frame);
                        
                        if (beam.Insert())
                        {
                            stats["created"]++;
                            createdBeams.Add(new 
                            {
                                etabs_id = frame.id,
                                tekla_id = beam.Identifier.ID,
                                type = frame.type,
                                profile = frame.properties.profile,
                                material = frame.properties.material,
                                story = frame.properties.story,
                                start_point = new { x = beam.StartPoint.X, y = beam.StartPoint.Y, z = beam.StartPoint.Z },
                                end_point = new { x = beam.EndPoint.X, y = beam.EndPoint.Y, z = beam.EndPoint.Z }
                            });
                        }
                        else
                        {
                            stats["failed"]++;
                            failedFrames.Add(new 
                            {
                                etabs_id = frame.id,
                                type = frame.design_orientation ?? frame.type,
                                reason = "Failed to insert beam into model"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        stats["failed"]++;
                        failedFrames.Add(new 
                        {
                            etabs_id = frame.id,
                            type = frame.design_orientation ?? frame.type,
                            reason = ex.Message
                        });
                    }
                }

                // Commit all changes
                model.CommitChanges();
                
                LogMessage("UBIM_IMPORT_COMPLETE", $"Import completed: {stats["created"]} created, {stats["failed"]} failed");

                return BuildSuccessResponse(new 
                {
                    import_summary = stats,
                    created_beams = createdBeams,
                    failed_frames = failedFrames,
                    source_file = "ETABS UBIM Import",
                    positioning = "MIDDLE/MIDDLE/TOP for accuracy"
                }, $"ETABS import completed: {stats["created"]} beams created from {stats["total_frames"]} frames");
            }
            catch (Exception ex)
            {
                LogMessage("UBIM_IMPORT_ERROR", $"Error: {ex.Message}");
                return BuildErrorResponse($"UBIM import error: {ex.Message}", "import-ubim-frames");
            }
        }
        
        static string HandleConnectTekla(HttpListenerRequest request)
        {
            try
            {
                var model = new Model();
                var connectionStatus = model.GetConnectionStatus();

                if (connectionStatus)
                {
                    var modelInfo = model.GetInfo();
                    LogMessage("TEKLA_CONNECT_SUCCESS", $"Connected to model: {modelInfo.ModelName}");

                    return BuildSuccessResponse(new
                    {
                        tekla_connected = true,
                        model_name = modelInfo.ModelName,
                        model_path = modelInfo.ModelPath,
                        ready_for_import = true,
                        bridge_version = "ETABS-Tekla Bridge v1.0"
                    }, "Tekla Structures connected and ready for ETABS import");
                }
                else
                {
                    return BuildErrorResponse("Tekla Structures not running or no model open", "connect-tekla");
                }
            }
            catch (Exception ex)
            {
                LogMessage("TEKLA_CONNECT_ERROR", $"Error: {ex.Message}");
                return BuildErrorResponse($"Connection error: {ex.Message}", "connect-tekla");
            }
        }

        // ==================== ML DATASET EXTRACTION ====================

        static string HandleExtractDrawingObjects(HttpListenerRequest request)
        {
            try
            {
                var model = new Model();
                if (!model.GetConnectionStatus())
                {
                    return BuildErrorResponse("Tekla not connected", "extract-drawing-objects");
                }

                string requestBody = ReadRequestBody(request);
                var extractRequest = string.IsNullOrEmpty(requestBody)
                    ? new DrawingExtractionRequest()
                    : JsonConvert.DeserializeObject<DrawingExtractionRequest>(requestBody);

                LogMessage("DRAWING_EXTRACTION_START",
                    $"Starting extraction - Discovery: {extractRequest.DiscoveryMode}, Max: {extractRequest.MaxDrawings}");

                // Generate C# code for drawing extraction
                string extractionCode = GenerateDrawingExtractionCode(extractRequest);

                // Execute the extraction code
                string result = ExecuteCodeWithCapture(extractionCode);

                LogMessage("DRAWING_EXTRACTION_COMPLETE", "Extraction completed successfully");

                return BuildSuccessResponse(new {
                    extraction_mode = extractRequest.DiscoveryMode ? "discovery" : "full",
                    max_drawings = extractRequest.MaxDrawings,
                    result = result
                }, "Drawing extraction completed");
            }
            catch (Exception ex)
            {
                LogMessage("DRAWING_EXTRACTION_ERROR", $"Error: {ex.Message}");
                return BuildErrorResponse($"Drawing extraction error: {ex.Message}", "extract-drawing-objects");
            }
        }

        private static string GenerateDrawingExtractionCode(DrawingExtractionRequest request)
        {
            if (request.DiscoveryMode)
            {
                return GenerateDiscoveryModeCode(request);
            }
            else
            {
                return GenerateFullExtractionCode(request);
            }
        }

        private static string GenerateDiscoveryModeCode(DrawingExtractionRequest request)
        {
            return @"
var dh = new Tekla.Structures.Drawing.DrawingHandler();
var typeCount = new System.Collections.Generic.Dictionary<string, int>();
var typeSamples = new System.Collections.Generic.Dictionary<string, object>();
int drawingsProcessed = 0;
int totalObjects = 0;

var drawingEnumerator = dh.GetDrawings();
while (drawingEnumerator.MoveNext() && drawingsProcessed < " + request.MaxDrawings + @")
{
    var drawing = drawingEnumerator.Current as Tekla.Structures.Drawing.Drawing;
    if (drawing == null) continue;

    drawingsProcessed++;

    // GetSheet() returns the ContainerView (sheet)
    var sheet = drawing.GetSheet();
    if (sheet == null) continue;

    // Get all objects from the sheet
    var objects = sheet.GetAllObjects();
    while (objects.MoveNext())
    {
        totalObjects++;
        var obj = objects.Current;
        if (obj == null) continue;

        string typeName = obj.GetType().FullName;

        // Count occurrences
        if (!typeCount.ContainsKey(typeName))
        {
            typeCount[typeName] = 0;

            // Store sample object properties using reflection
            var props = obj.GetType().GetProperties();
            var sampleProps = new System.Collections.Generic.List<string>();

            foreach (var prop in props)
            {
                try
                {
                    var value = prop.GetValue(obj, null);
                    sampleProps.Add(prop.Name + "": "" + prop.PropertyType.Name + "" = "" + (value != null ? value.ToString() : ""null""));
                }
                catch
                {
                    sampleProps.Add(prop.Name + "": "" + prop.PropertyType.Name + "" (error reading)"");
                }
            }

            typeSamples[typeName] = string.Join("", "", sampleProps.ToArray());
        }

        typeCount[typeName]++;
    }
}

// Build discovery report
var report = new System.Text.StringBuilder();
report.AppendLine(""DRAWING OBJECT DISCOVERY REPORT"");
report.AppendLine(""=============================="");
report.AppendLine(""Drawings Scanned: "" + drawingsProcessed.ToString());
report.AppendLine(""Total Objects: "" + totalObjects.ToString());
report.AppendLine(""Unique Types Found: "" + typeCount.Count.ToString());
report.AppendLine("""");
report.AppendLine(""OBJECT TYPES:"");

foreach (var entry in typeCount)
{
    report.AppendLine("""");
    report.AppendLine(""Type: "" + entry.Key);
    report.AppendLine(""Count: "" + entry.Value.ToString());
    if (typeSamples.ContainsKey(entry.Key))
    {
        report.AppendLine(""Sample Properties: "" + typeSamples[entry.Key].ToString());
    }
}

result = report.ToString();
";
        }

        private static string GenerateFullExtractionCode(DrawingExtractionRequest request)
        {
            return @"
// ENHANCED ML EXTRACTION: Drawing types, relationships, geometric context, coordinate systems
// WSL COMPATIBILITY: Save to project directory instead of Windows temp (inaccessible from WSL)
var timestamp = System.DateTime.Now.ToString(""yyyyMMdd_HHmmss"");

// Create extractions directory in current working directory (WSL-accessible)
var currentDir = System.IO.Directory.GetCurrentDirectory();
var extractionsDir = System.IO.Path.Combine(currentDir, ""extractions"");
if (!System.IO.Directory.Exists(extractionsDir))
{
    System.IO.Directory.CreateDirectory(extractionsDir);
}

var outputPath = System.IO.Path.Combine(extractionsDir, ""tekla_extraction_"" + timestamp + "".jsonl"");
System.Console.WriteLine(""📁 Enhanced ML extraction to: "" + outputPath);

var startTime = System.DateTime.Now;
var dh = new Tekla.Structures.Drawing.DrawingHandler();
int drawingsProcessed = 0;
int totalObjects = 0;
long bytesWritten = 0;

var jsonSettings = new Newtonsoft.Json.JsonSerializerSettings
{
    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
    Formatting = Newtonsoft.Json.Formatting.None,
    NullValueHandling = Newtonsoft.Json.NullValueHandling.Include
};

using (var writer = new System.IO.StreamWriter(outputPath, false, System.Text.Encoding.UTF8))
{
    var drawingEnumerator = dh.GetDrawings();
    while (drawingEnumerator.MoveNext() && drawingsProcessed < " + request.MaxDrawings + @")
    {
        var drawing = drawingEnumerator.Current as Tekla.Structures.Drawing.Drawing;
        if (drawing == null) continue;

        drawingsProcessed++;

        var drawingData = new System.Collections.Generic.Dictionary<string, object>();
        drawingData[""mark""] = drawing.Mark;

        // ENHANCEMENT 1: Drawing type classification (actual Tekla class)
        drawingData[""drawing_type""] = drawing.GetType().Name;
        drawingData[""drawing_type_full""] = drawing.GetType().FullName;

        var allObjects = new System.Collections.Generic.List<object>();

        // GetSheet() returns the ContainerView (sheet)
        var sheet = drawing.GetSheet();
        if (sheet == null) continue;

        // Extract sheet/view transformation matrix for coordinate conversion
        // FIX: ContainerView is NOT a View - use reflection to get coordinate system
        var sheetTransform = new System.Collections.Generic.Dictionary<string, object>();
        try
        {
            // Try to get DisplayCoordinateSystem property via reflection
            var displayCSProp = sheet.GetType().GetProperty(""DisplayCoordinateSystem"");
            if (displayCSProp != null)
            {
                var transform = displayCSProp.GetValue(sheet, null);
                if (transform != null)
                {
                    var originProp = transform.GetType().GetProperty(""Origin"");
                    var axisXProp = transform.GetType().GetProperty(""AxisX"");
                    var axisYProp = transform.GetType().GetProperty(""AxisY"");

                    if (originProp != null && axisXProp != null && axisYProp != null)
                    {
                        var origin = originProp.GetValue(transform, null);
                        var axisX = axisXProp.GetValue(transform, null);
                        var axisY = axisYProp.GetValue(transform, null);

                        sheetTransform[""origin""] = new {
                            x = origin.GetType().GetProperty(""X"").GetValue(origin, null),
                            y = origin.GetType().GetProperty(""Y"").GetValue(origin, null),
                            z = origin.GetType().GetProperty(""Z"").GetValue(origin, null)
                        };
                        sheetTransform[""axis_x""] = new {
                            x = axisX.GetType().GetProperty(""X"").GetValue(axisX, null),
                            y = axisX.GetType().GetProperty(""Y"").GetValue(axisX, null),
                            z = axisX.GetType().GetProperty(""Z"").GetValue(axisX, null)
                        };
                        sheetTransform[""axis_y""] = new {
                            x = axisY.GetType().GetProperty(""X"").GetValue(axisY, null),
                            y = axisY.GetType().GetProperty(""Y"").GetValue(axisY, null),
                            z = axisY.GetType().GetProperty(""Z"").GetValue(axisY, null)
                        };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            sheetTransform[""error""] = ex.Message;
        }
        drawingData[""sheet_coordinate_system""] = sheetTransform;

        // Get all objects from the sheet
        var objects = sheet.GetAllObjects();
        while (objects.MoveNext())
        {
            var obj = objects.Current;
            if (obj == null) continue;

            var objData = new System.Collections.Generic.Dictionary<string, object>();
            objData[""type""] = obj.GetType().FullName;
            objData[""type_name""] = obj.GetType().Name;

            // ENHANCEMENT: Get object ID and model linkage (inspired by tekladrawingexamples.cs)
            try
            {
                var drawingObj = obj as Tekla.Structures.Drawing.DrawingObject;
                if (drawingObj != null)
                {
                    // Get drawing object identifier
                    var identifier = drawingObj.GetType().GetProperty(""Identifier"");
                    if (identifier != null)
                    {
                        var id = identifier.GetValue(drawingObj, null);
                        if (id != null)
                        {
                            objData[""object_id""] = id.ToString();
                        }
                    }

                    // ENHANCEMENT: Track ModelIdentifier for drawing objects linked to model objects
                    // This establishes the drawing→model relationship for ML training
                    var modelIdentifierProp = drawingObj.GetType().GetProperty(""ModelIdentifier"");
                    if (modelIdentifierProp != null)
                    {
                        var modelId = modelIdentifierProp.GetValue(drawingObj, null);
                        if (modelId != null)
                        {
                            objData[""model_object_id""] = modelId.ToString();
                            objData[""has_model_link""] = true;
                        }
                        else
                        {
                            objData[""has_model_link""] = false;
                        }
                    }
                }
            }
            catch { }

            // ENHANCEMENT 2 - LAYER 1: Explicit relationships via GetRelatedObjects
            var relatedObjects = new System.Collections.Generic.List<string>();
            try
            {
                var drawingObj = obj as Tekla.Structures.Drawing.DrawingObject;
                if (drawingObj != null)
                {
                    // FIX: Specify Type.EmptyTypes to avoid ambiguous match with GetRelatedObjects(Type) overload
                    var getRelatedMethod = drawingObj.GetType().GetMethod(""GetRelatedObjects"", System.Type.EmptyTypes);
                    if (getRelatedMethod != null)
                    {
                        var related = getRelatedMethod.Invoke(drawingObj, new object[] { });
                        if (related != null)
                        {
                            var enumerator = related as System.Collections.IEnumerator;
                            if (enumerator != null)
                            {
                                while (enumerator.MoveNext())
                                {
                                    var relObj = enumerator.Current;
                                    if (relObj != null)
                                    {
                                        relatedObjects.Add(relObj.GetType().Name + "": "" + relObj.ToString());
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                objData[""relationship_error""] = ex.Message;
            }
            if (relatedObjects.Count > 0)
            {
                objData[""related_objects""] = relatedObjects;
            }

            // ENHANCEMENT 3 - LAYER 2: Geometric inference data
            var geometricData = new System.Collections.Generic.Dictionary<string, object>();

            // For DIMENSION objects: Extract dimension points and geometry
            if (obj.GetType().Name.Contains(""Dimension""))
            {
                try
                {
                    var dimPoints = new System.Collections.Generic.List<object>();

                    // FIX: Handle DimensionSets separately - they contain child dimensions
                    if (obj.GetType().Name.Contains(""DimensionSet""))
                    {
                        // DimensionSet has GetObjects() method that returns child dimensions
                        var getObjects = obj.GetType().GetMethod(""GetObjects"", System.Type.EmptyTypes);
                        if (getObjects != null)
                        {
                            var children = getObjects.Invoke(obj, new object[] { });
                            if (children != null)
                            {
                                var childEnum = children as System.Collections.IEnumerator;
                                if (childEnum != null)
                                {
                                    while (childEnum.MoveNext())
                                    {
                                        var childDim = childEnum.Current;
                                        if (childDim != null)
                                        {
                                            // Extract points from each child dimension
                                            var getPoints = childDim.GetType().GetMethod(""GetPoints"");
                                            if (getPoints != null)
                                            {
                                                var points = getPoints.Invoke(childDim, new object[] { });
                                                if (points != null)
                                                {
                                                    var pointList = points as System.Collections.ArrayList;
                                                    if (pointList != null)
                                                    {
                                                        foreach (var pt in pointList)
                                                        {
                                                            var pointType = pt.GetType();
                                                            var xProp = pointType.GetProperty(""X"");
                                                            var yProp = pointType.GetProperty(""Y"");
                                                            var zProp = pointType.GetProperty(""Z"");
                                                            if (xProp != null && yProp != null && zProp != null)
                                                            {
                                                                dimPoints.Add(new {
                                                                    x = xProp.GetValue(pt, null),
                                                                    y = yProp.GetValue(pt, null),
                                                                    z = zProp.GetValue(pt, null),
                                                                    type = ""dimension_point""
                                                                });
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // Individual dimension - extract points directly
                        var getPoints = obj.GetType().GetMethod(""GetPoints"");
                        if (getPoints != null)
                        {
                            var points = getPoints.Invoke(obj, new object[] { });
                            if (points != null)
                            {
                                var pointList = points as System.Collections.ArrayList;
                                if (pointList != null)
                                {
                                    foreach (var pt in pointList)
                                    {
                                        var pointType = pt.GetType();
                                        var xProp = pointType.GetProperty(""X"");
                                        var yProp = pointType.GetProperty(""Y"");
                                        var zProp = pointType.GetProperty(""Z"");
                                        if (xProp != null && yProp != null && zProp != null)
                                        {
                                            dimPoints.Add(new {
                                                x = xProp.GetValue(pt, null),
                                                y = yProp.GetValue(pt, null),
                                                z = zProp.GetValue(pt, null),
                                                type = ""dimension_point""
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }

                    geometricData[""dimension_points""] = dimPoints;

                    // ENHANCEMENT: Extract InsertionPoint for dimension text placement (ML training feature)
                    try
                    {
                        var insertionPointProp = obj.GetType().GetProperty(""InsertionPoint"");
                        if (insertionPointProp != null)
                        {
                            var insertionPoint = insertionPointProp.GetValue(obj, null);
                            if (insertionPoint != null)
                            {
                                var ipX = insertionPoint.GetType().GetProperty(""X"");
                                var ipY = insertionPoint.GetType().GetProperty(""Y"");
                                var ipZ = insertionPoint.GetType().GetProperty(""Z"");

                                if (ipX != null && ipY != null && ipZ != null)
                                {
                                    geometricData[""dimension_text_position""] = new {
                                        x = ipX.GetValue(insertionPoint, null),
                                        y = ipY.GetValue(insertionPoint, null),
                                        z = ipZ.GetValue(insertionPoint, null),
                                        type = ""dimension_text_insertion_point""
                                    };
                                }
                            }
                        }
                    }
                    catch { /* Skip if InsertionPoint not available */ }

                    // FIX: Use reflection for dimension attributes (don't hardcode properties)
                    try
                    {
                        var attrsProp = obj.GetType().GetProperty(""Attributes"");
                        if (attrsProp != null)
                        {
                            var attributes = attrsProp.GetValue(obj, null);
                            if (attributes != null)
                            {
                                var attrData = new System.Collections.Generic.Dictionary<string, string>();
                                var attrProps = attributes.GetType().GetProperties();

                                foreach (var attrProp in attrProps)
                                {
                                    try
                                    {
                                        var attrValue = attrProp.GetValue(attributes, null);
                                        attrData[attrProp.Name] = attrValue != null ? attrValue.ToString() : ""null"";
                                    }
                                    catch (Exception attrEx)
                                    {
                                        attrData[attrProp.Name] = ""ERROR: "" + attrEx.Message;
                                    }
                                }

                                if (attrData.Count > 0)
                                {
                                    geometricData[""dimension_attributes""] = attrData;
                                }
                            }
                        }
                    }
                    catch { }
                }
                catch (Exception ex)
                {
                    geometricData[""dimension_extraction_error""] = ex.Message;
                }
            }

            // For PART objects: Extract vertices, edges, individual holes using SOLIDS API
            if (obj.GetType().Name.Contains(""Part""))
            {
                try
                {
                    var partGeometry = new System.Collections.Generic.Dictionary<string, object>();
                    var vertices = new System.Collections.Generic.List<object>();

                    // FIX: Use GetSolid() method to extract part geometry
                    var getSolid = obj.GetType().GetMethod(""GetSolid"", System.Type.EmptyTypes);
                    if (getSolid != null)
                    {
                        var solid = getSolid.Invoke(obj, new object[] { });
                        if (solid != null)
                        {
                            // Get faces from solid
                            var getFaceList = solid.GetType().GetMethod(""GetFaceList"");
                            if (getFaceList != null)
                            {
                                var faceList = getFaceList.Invoke(solid, new object[] { });
                                if (faceList != null)
                                {
                                    var faceEnum = faceList as System.Collections.IEnumerator;
                                    if (faceEnum != null)
                                    {
                                        while (faceEnum.MoveNext())
                                        {
                                            var face = faceEnum.Current;
                                            if (face != null)
                                            {
                                                // Get contour from face
                                                var getContour = face.GetType().GetMethod(""GetContour"");
                                                if (getContour != null)
                                                {
                                                    var contour = getContour.Invoke(face, new object[] { });
                                                    if (contour != null)
                                                    {
                                                        var contourPoints = contour as System.Collections.IEnumerable;
                                                        if (contourPoints != null)
                                                        {
                                                            foreach (var pt in contourPoints)
                                                            {
                                                                if (pt != null)
                                                                {
                                                                    var xProp = pt.GetType().GetProperty(""X"");
                                                                    var yProp = pt.GetType().GetProperty(""Y"");
                                                                    var zProp = pt.GetType().GetProperty(""Z"");
                                                                    if (xProp != null && yProp != null && zProp != null)
                                                                    {
                                                                        vertices.Add(new {
                                                                            x = xProp.GetValue(pt, null),
                                                                            y = yProp.GetValue(pt, null),
                                                                            z = zProp.GetValue(pt, null)
                                                                        });
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    partGeometry[""vertices""] = vertices;

                    // ENHANCEMENT: Extract bounding box (AABB) data inspired by tekladrawingexamples.cs
                    try
                    {
                        var minPointProp = solid.GetType().GetProperty(""MinimumPoint"");
                        var maxPointProp = solid.GetType().GetProperty(""MaximumPoint"");

                        if (minPointProp != null && maxPointProp != null)
                        {
                            var minPoint = minPointProp.GetValue(solid, null);
                            var maxPoint = maxPointProp.GetValue(solid, null);

                            if (minPoint != null && maxPoint != null)
                            {
                                var minX = minPoint.GetType().GetProperty(""X"");
                                var minY = minPoint.GetType().GetProperty(""Y"");
                                var minZ = minPoint.GetType().GetProperty(""Z"");
                                var maxX = maxPoint.GetType().GetProperty(""X"");
                                var maxY = maxPoint.GetType().GetProperty(""Y"");
                                var maxZ = maxPoint.GetType().GetProperty(""Z"");

                                if (minX != null && minY != null && minZ != null &&
                                    maxX != null && maxY != null && maxZ != null)
                                {
                                    partGeometry[""bounding_box_aabb""] = new {
                                        min = new {
                                            x = minX.GetValue(minPoint, null),
                                            y = minY.GetValue(minPoint, null),
                                            z = minZ.GetValue(minPoint, null)
                                        },
                                        max = new {
                                            x = maxX.GetValue(maxPoint, null),
                                            y = maxY.GetValue(maxPoint, null),
                                            z = maxZ.GetValue(maxPoint, null)
                                        },
                                        type = ""axis_aligned""
                                    };
                                }
                            }
                        }
                    }
                    catch { /* Skip bounding box if extraction fails */ }

                    // FIX: Get bolt/hole positions from Part children using Solids API
                    var holes = new System.Collections.Generic.List<object>();
                    var getChildren = obj.GetType().GetMethod(""GetChildren"");
                    if (getChildren != null)
                    {
                        var children = getChildren.Invoke(obj, new object[] { });
                        if (children != null)
                        {
                            var childEnum = children as System.Collections.IEnumerator;
                            if (childEnum != null)
                            {
                                while (childEnum.MoveNext())
                                {
                                    var child = childEnum.Current;
                                    if (child != null)
                                    {
                                        var childType = child.GetType().Name;

                                        // Try Bolt objects using GetSolid()
                                        if (childType.Contains(""Bolt""))
                                        {
                                            try
                                            {
                                                var getBoltSolid = child.GetType().GetMethod(""GetSolid"", System.Type.EmptyTypes);
                                                if (getBoltSolid != null)
                                                {
                                                    var boltSolid = getBoltSolid.Invoke(child, new object[] { });
                                                    if (boltSolid != null)
                                                    {
                                                        // Get center/origin from bolt solid
                                                        var getOrigin = boltSolid.GetType().GetMethod(""GetOrigin"");
                                                        if (getOrigin != null)
                                                        {
                                                            var origin = getOrigin.Invoke(boltSolid, new object[] { });
                                                            if (origin != null)
                                                            {
                                                                var xProp = origin.GetType().GetProperty(""X"");
                                                                var yProp = origin.GetType().GetProperty(""Y"");
                                                                var zProp = origin.GetType().GetProperty(""Z"");
                                                                if (xProp != null && yProp != null && zProp != null)
                                                                {
                                                                    holes.Add(new {
                                                                        x = xProp.GetValue(origin, null),
                                                                        y = yProp.GetValue(origin, null),
                                                                        z = zProp.GetValue(origin, null),
                                                                        type = ""bolt_hole"",
                                                                        source = childType
                                                                    });
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            catch { /* Skip if bolt extraction fails */ }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (holes.Count > 0)
                    {
                        partGeometry[""holes""] = holes;
                    }

                    geometricData[""part_geometry""] = partGeometry;
                }
                catch (Exception ex)
                {
                    geometricData[""part_extraction_error""] = ex.Message;
                }
            }

            // For BOLT objects: Extract bolt geometry using Solids API
            if (obj.GetType().Name.Contains(""Bolt""))
            {
                try
                {
                    var boltGeometry = new System.Collections.Generic.Dictionary<string, object>();
                    var boltPositions = new System.Collections.Generic.List<object>();

                    var getBoltSolid = obj.GetType().GetMethod(""GetSolid"", System.Type.EmptyTypes);
                    if (getBoltSolid != null)
                    {
                        var boltSolid = getBoltSolid.Invoke(obj, new object[] { });
                        if (boltSolid != null)
                        {
                            // Get center/origin from bolt solid
                            var getOrigin = boltSolid.GetType().GetMethod(""GetOrigin"");
                            if (getOrigin != null)
                            {
                                var origin = getOrigin.Invoke(boltSolid, new object[] { });
                                if (origin != null)
                                {
                                    var xProp = origin.GetType().GetProperty(""X"");
                                    var yProp = origin.GetType().GetProperty(""Y"");
                                    var zProp = origin.GetType().GetProperty(""Z"");
                                    if (xProp != null && yProp != null && zProp != null)
                                    {
                                        boltPositions.Add(new {
                                            x = xProp.GetValue(origin, null),
                                            y = yProp.GetValue(origin, null),
                                            z = zProp.GetValue(origin, null),
                                            type = ""bolt_position""
                                        });
                                    }
                                }
                            }
                        }
                    }

                    boltGeometry[""positions""] = boltPositions;
                    geometricData[""bolt_geometry""] = boltGeometry;
                }
                catch (Exception ex)
                {
                    geometricData[""bolt_extraction_error""] = ex.Message;
                }
            }

            // For VIEW objects: Extract transformation matrix
            if (obj.GetType().Name.Contains(""View""))
            {
                try
                {
                    var view = obj as Tekla.Structures.Drawing.View;
                    if (view != null)
                    {
                        var viewTransform = new System.Collections.Generic.Dictionary<string, object>();

                        // Get view coordinate system (transformation matrix)
                        var viewCS = view.DisplayCoordinateSystem;
                        if (viewCS != null)
                        {
                            viewTransform[""origin""] = new { x = viewCS.Origin.X, y = viewCS.Origin.Y, z = viewCS.Origin.Z };
                            viewTransform[""axis_x""] = new { x = viewCS.AxisX.X, y = viewCS.AxisX.Y, z = viewCS.AxisX.Z };
                            viewTransform[""axis_y""] = new { x = viewCS.AxisY.X, y = viewCS.AxisY.Y, z = viewCS.AxisY.Z };
                        }

                        geometricData[""view_coordinate_system""] = viewTransform;
                    }
                }
                catch (Exception ex)
                {
                    geometricData[""view_extraction_error""] = ex.Message;
                }
            }

            if (geometricData.Count > 0)
            {
                objData[""geometric_data""] = geometricData;
            }

            // ENHANCEMENT 4: View context (parent view ID)
            try
            {
                var drawingObj = obj as Tekla.Structures.Drawing.DrawingObject;
                if (drawingObj != null)
                {
                    var getView = drawingObj.GetType().GetProperty(""Parent"");
                    if (getView != null)
                    {
                        var parentView = getView.GetValue(drawingObj, null);
                        if (parentView != null)
                        {
                            objData[""parent_view_type""] = parentView.GetType().Name;
                        }
                    }
                }
            }
            catch { }

            // Extract ALL properties via reflection (keep existing functionality)
            var props = obj.GetType().GetProperties();
            var propData = new System.Collections.Generic.Dictionary<string, string>();

            foreach (var prop in props)
            {
                try
                {
                    var value = prop.GetValue(obj, null);
                    propData[prop.Name] = value != null ? value.ToString() : ""null"";
                }
                catch (Exception ex)
                {
                    propData[prop.Name] = ""ERROR: "" + ex.Message;
                }
            }

            objData[""properties""] = propData;
            allObjects.Add(objData);
        }

        drawingData[""objects""] = allObjects;
        drawingData[""object_count""] = allObjects.Count;
        totalObjects += allObjects.Count;

        // Write this drawing as a single JSON line
        var jsonLine = Newtonsoft.Json.JsonConvert.SerializeObject(drawingData, jsonSettings);
        writer.WriteLine(jsonLine);
        bytesWritten += System.Text.Encoding.UTF8.GetByteCount(jsonLine) + 2;

        // Progress indicator
        if (drawingsProcessed % 10 == 0)
        {
            System.Console.WriteLine(""📊 Progress: "" + drawingsProcessed + "" drawings, "" + totalObjects + "" objects"");
        }
    }
}

var duration = (System.DateTime.Now - startTime).TotalSeconds;
var fileSizeMB = bytesWritten / (1024.0 * 1024.0);

// WSL PATH CONVERSION: Convert Windows path to WSL-accessible format
// Windows: C:\Users\hugov\ClaudeProjects\...\extractions\file.jsonl
// WSL:     /mnt/c/Users/hugov/ClaudeProjects/.../extractions/file.jsonl
string wslPath = outputPath;
if (outputPath.Length >= 2 && outputPath[1] == ':')
{
    // Extract drive letter and convert to lowercase
    char driveLetter = System.Char.ToLower(outputPath[0]);
    string pathWithoutDrive = outputPath.Substring(2); // Remove ""C:""

    // Replace backslashes with forward slashes
    pathWithoutDrive = pathWithoutDrive.Replace(""\\\\"", ""/"");

    // Construct WSL path: /mnt/c/...
    wslPath = ""/mnt/"" + driveLetter + pathWithoutDrive;
}

// Return ONLY metadata - not the actual data
var metadata = new System.Collections.Generic.Dictionary<string, object>();
metadata[""file_path""] = outputPath;
metadata[""file_path_wsl""] = wslPath;
metadata[""total_drawings""] = drawingsProcessed;
metadata[""total_objects""] = totalObjects;
metadata[""file_size_mb""] = System.Math.Round(fileSizeMB, 2);
metadata[""duration_seconds""] = System.Math.Round(duration, 1);
metadata[""format""] = ""jsonl"";
metadata[""timestamp""] = timestamp;
metadata[""extraction_version""] = ""enhanced_v2_geometric"";

System.Console.WriteLine(""✅ Enhanced extraction complete: "" + totalObjects + "" objects from "" + drawingsProcessed + "" drawings"");
System.Console.WriteLine(""📦 File size: "" + fileSizeMB.ToString(""F2"") + "" MB"");
System.Console.WriteLine(""⏱️ Duration: "" + duration.ToString(""F1"") + "" seconds"");
System.Console.WriteLine(""🪟 Windows path: "" + outputPath);
System.Console.WriteLine(""🐧 WSL path: "" + wslPath);

result = Newtonsoft.Json.JsonConvert.SerializeObject(metadata, jsonSettings);
";
        }

        // ==================== HELPER METHODS ====================
        
        private static string ReadRequestBody(HttpListenerRequest request)
        {
            using (var reader = new StreamReader(request.InputStream))
            {
                return reader.ReadToEnd();
            }
        }
        
        private static object GetPartInfo(Part part)
        {
            // Need to get report properties for Part data
            string profileString = "";
            string materialString = "";
            string name = "";
            string classValue = "";
            
            part.GetReportProperty("PROFILE", ref profileString);
            part.GetReportProperty("MATERIAL", ref materialString);
            part.GetReportProperty("NAME", ref name);
            part.GetReportProperty("CLASS", ref classValue);
            
            part.Profile.ProfileString = profileString;
            part.Material.MaterialString = materialString;
            part.Name = name;
            part.Class = classValue;
            
            var beam = part as Beam;
            if (beam != null)
            {
                return new {
                    id = part.Identifier.ID,
                    type = "Beam",
                    name = part.Name,
                    profile = part.Profile.ProfileString,
                    material = part.Material.MaterialString,
                    @class = part.Class,
                    start_point = new { x = beam.StartPoint.X, y = beam.StartPoint.Y, z = beam.StartPoint.Z },
                    end_point = new { x = beam.EndPoint.X, y = beam.EndPoint.Y, z = beam.EndPoint.Z }
                };
            }
            
            // Generic part info for other types
            return new {
                id = part.Identifier.ID,
                type = part.GetType().Name,
                name = part.Name,
                profile = part.Profile.ProfileString,
                material = part.Material.MaterialString,
                @class = part.Class
            };
        }
        
        private static object GetDetailedPartInfo(Part part)
        {
            var basicInfo = GetPartInfo(part);
            
            // Add position information
            var positionInfo = new {
                depth = part.Position.Depth.ToString(),
                plane = part.Position.Plane.ToString(),
                rotation = part.Position.Rotation.ToString()
            };
            
            // Combine basic and position info
            var beam = part as Beam;
            if (beam != null)
            {
                return new {
                    id = part.Identifier.ID,
                    type = "Beam",
                    name = part.Name,
                    profile = part.Profile.ProfileString,
                    material = part.Material.MaterialString,
                    @class = part.Class,
                    position = positionInfo,
                    geometry = new {
                        start_point = new { x = beam.StartPoint.X, y = beam.StartPoint.Y, z = beam.StartPoint.Z },
                        end_point = new { x = beam.EndPoint.X, y = beam.EndPoint.Y, z = beam.EndPoint.Z },
                        length = Distance.PointToPoint(beam.StartPoint, beam.EndPoint)
                    }
                };
            }
            
            return new {
                id = part.Identifier.ID,
                type = part.GetType().Name,
                name = part.Name,
                profile = part.Profile.ProfileString,
                material = part.Material.MaterialString,
                @class = part.Class,
                position = positionInfo
            };
        }
        
        private static bool MatchesFilter(Part part, PartFilter filter)
        {
            // Check part type filter
            if (!string.IsNullOrEmpty(filter.PartType))
            {
                var partTypeName = part.GetType().Name;
                if (!partTypeName.Equals(filter.PartType, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            
            // Check material filter
            if (!string.IsNullOrEmpty(filter.Material))
            {
                if (!part.Material.MaterialString.Contains(filter.Material))
                    return false;
            }
            
            // Check profile filter
            if (!string.IsNullOrEmpty(filter.Profile))
            {
                if (!part.Profile.ProfileString.Contains(filter.Profile))
                    return false;
            }
            
            // Check class filter
            if (!string.IsNullOrEmpty(filter.Class))
            {
                if (!part.Class.Equals(filter.Class))
                    return false;
            }
            
            // Check name filter
            if (!string.IsNullOrEmpty(filter.Name))
            {
                if (!part.Name.Contains(filter.Name))
                    return false;
            }
            
            return true;
        }
        
        private static Beam CreateBeamFromRequest(BeamRequest beamRequest)
        {
            var beam = new Beam();
            
            // Core geometry
            beam.StartPoint = new Point(beamRequest.StartPointX, beamRequest.StartPointY, beamRequest.StartPointZ);
            beam.EndPoint = new Point(beamRequest.EndPointX, beamRequest.EndPointY, beamRequest.EndPointZ);
            
            // Material and profile
            beam.Profile.ProfileString = beamRequest.Profile;
            beam.Material.MaterialString = beamRequest.Material;
            
            // Properties
            beam.Class = beamRequest.Class;
            beam.Name = beamRequest.Name;
            
            // Position parameters
            beam.Position.Depth = (Position.DepthEnum)Enum.Parse(typeof(Position.DepthEnum), beamRequest.PositionDepth.ToUpper());
            beam.Position.Plane = (Position.PlaneEnum)Enum.Parse(typeof(Position.PlaneEnum), beamRequest.PositionPlane.ToUpper());
            beam.Position.Rotation = (Position.RotationEnum)Enum.Parse(typeof(Position.RotationEnum), beamRequest.PositionRotation.ToUpper());
            
            // Rotation parameters - UBIM enhancement for ETABS local angle support
            if (!string.IsNullOrEmpty(beamRequest.RotationEnum))
            {
                beam.Position.Rotation = (Position.RotationEnum)Enum.Parse(typeof(Position.RotationEnum), beamRequest.RotationEnum.ToUpper());
            }
            if (beamRequest.RotationOffset != 0.0)
            {
                beam.Position.RotationOffset = beamRequest.RotationOffset; // Tekla accepts degrees directly
            }
            
            return beam;
        }

        private static Beam CreateBeamFromUBIMFrame(UBIMFrame frame)
        {
            var beam = new Beam();
            
            // CRITICAL: Use exact coordinates from ETABS (already in mm)
            beam.StartPoint = new Point(frame.geometry.start.x, frame.geometry.start.y, frame.geometry.start.z);
            beam.EndPoint = new Point(frame.geometry.end.x, frame.geometry.end.y, frame.geometry.end.z);
            
            // Properties from UBIM frame
            beam.Profile.ProfileString = frame.properties.profile;  // "W36X182"
            
            // Handle actual ETABS materials (GetMaterial)
            beam.Material.MaterialString = ConvertETABSMaterial(frame.properties.material);  // "A992Fy50" -> "A992"
            
            // Set beam name using design_orientation
            beam.Name = GetFrameBaseName(frame.design_orientation);
            
            // Set beam class using simple numeric mapping
            beam.Class = GetFrameClass(frame.design_orientation).ToString();
            
            // Apply cardinal point positioning with correct order (Plane, Rotation, Depth)
            if (frame.properties != null && frame.properties.cardinal_point >= 1 && frame.properties.cardinal_point <= 11)
            {
                var position = ConvertCardinalPointToTeklaPosition(frame.properties.cardinal_point, frame.design_orientation);
                
                beam.Position.Plane = position.Plane;
                beam.Position.PlaneOffset = 0;
                beam.Position.Rotation = position.Rotation;
                beam.Position.RotationOffset = frame.local_angle;
                beam.Position.Depth = position.Depth;
                beam.Position.DepthOffset = 0;
                
                LogMessage("CARDINAL_POINT_APPLIED", 
                    "Frame " + frame.id + ": Cardinal point " + frame.properties.cardinal_point.ToString() + " -> " + position.Plane.ToString() + "/" + position.Rotation.ToString() + "/" + position.Depth.ToString());
            }
            else
            {
                // Default positioning based on design orientation
                beam.Position.Plane = Position.PlaneEnum.MIDDLE;
                beam.Position.PlaneOffset = 0;
                beam.Position.Rotation = Position.RotationEnum.TOP;
                beam.Position.RotationOffset = frame.local_angle;  // Apply local angle
                
                // CRITICAL: Different defaults for different frame types
                if ((frame.design_orientation ?? "").ToUpper() == "BEAM")
                {
                    // Beams: MIDDLE/TOP/BEHIND (top flange aligned)
                    beam.Position.Depth = Position.DepthEnum.BEHIND;
                }
                else
                {
                    // Columns/Braces: MIDDLE/TOP/MIDDLE (centered)
                    beam.Position.Depth = Position.DepthEnum.MIDDLE;
                }
                beam.Position.DepthOffset = 0;
            }
            
            // Log local angle if non-zero
            if (frame.local_angle != 0)
            {
                LogMessage("LOCAL_ANGLE_DETECTED", 
                    "Frame " + frame.id + ": Local angle " + frame.local_angle.ToString() + " degrees");
            }
            
            return beam;
        }

        /// <summary>
        /// Convert ETABS material names to Tekla material names
        /// Simple conversion for common materials since both systems use similar profiles
        /// </summary>
        private static string ConvertETABSMaterial(string etabsMaterial)
        {
            if (string.IsNullOrEmpty(etabsMaterial))
                return "A992"; // Default steel material
            
            // Handle ETABS GetMaterial format (e.g., "A992Fy50" -> "A992")
            if (etabsMaterial.StartsWith("A992"))
                return "A992";
            if (etabsMaterial.StartsWith("A36"))
                return "A36";
            if (etabsMaterial.StartsWith("A500"))
                return "A500B"; // A500GrB -> A500B
            if (etabsMaterial.Contains("CONC") || etabsMaterial.Contains("C30"))
                return "C30/37"; // Concrete grades
            
            // Log unmapped materials for future enhancement
            LogMessage("MATERIAL_MAPPING", $"Unmapped ETABS material: {etabsMaterial}, using default A992");
            return "A992"; // Safe default
        }
        
        /// <summary>
        /// Get beam base name from ETABS design orientation
        /// </summary>
        private static string GetFrameBaseName(string designOrientation)
        {
            if (string.IsNullOrEmpty(designOrientation))
                return "BEAM";
                
            switch (designOrientation.ToLower())
            {
                case "column":
                    return "COLUMN";
                case "beam":
                    return "BEAM";
                case "brace":
                    return "BRACE";
                case "other":
                    return "OTHER";
                default:
                    return "BEAM";  // Default fallback
            }
        }
        
        /// <summary>
        /// Get frame class number from ETABS design orientation
        /// 1=BEAM, 2=COLUMN, 3=BRACE, 4=OTHER
        /// </summary>
        private static int GetFrameClass(string designOrientation)
        {
            if (string.IsNullOrEmpty(designOrientation))
                return 1;
                
            switch (designOrientation.ToLower())
            {
                case "beam":
                    return 1;
                case "column":
                    return 2;
                case "brace":
                    return 3;
                case "other":
                    return 4;
                default:
                    return 1;  // Default fallback
            }
        }
        
        /// <summary>
        /// Convert ETABS cardinal points (1-11) to Tekla Position enums
        /// Based on ETABS cardinal point specification with design orientation context
        /// </summary>
        private static (Position.DepthEnum Depth, Position.PlaneEnum Plane, Position.RotationEnum Rotation) 
            ConvertCardinalPointToTeklaPosition(int cardinalPoint, string designOrientation)
        {
            // ETABS cardinal points (1-11) with correct Tekla mapping:
            // 1=bottom left, 2=bottom center, 3=bottom right
            // 4=middle left, 5=middle center, 6=middle right  
            // 7=top left, 8=top center, 9=top right
            // 10=centroid, 11=shear center
            
            // Log the design orientation for context
            LogMessage("CARDINAL_POINT_CONVERT", 
                "Converting cardinal point " + cardinalPoint.ToString() + " for " + (designOrientation ?? "unknown") + " orientation");
            
            switch (cardinalPoint)
            {
                case 1: // Bottom Left
                    return (Position.DepthEnum.FRONT, Position.PlaneEnum.LEFT, Position.RotationEnum.BELOW);
                case 2: // Bottom Center  
                    return (Position.DepthEnum.FRONT, Position.PlaneEnum.MIDDLE, Position.RotationEnum.BELOW);
                case 3: // Bottom Right
                    return (Position.DepthEnum.FRONT, Position.PlaneEnum.RIGHT, Position.RotationEnum.BELOW);
                case 4: // Middle Left
                    return (Position.DepthEnum.MIDDLE, Position.PlaneEnum.LEFT, Position.RotationEnum.TOP);
                case 5: // Middle Center (Most common)
                    return (Position.DepthEnum.MIDDLE, Position.PlaneEnum.MIDDLE, Position.RotationEnum.TOP);
                case 6: // Middle Right
                    return (Position.DepthEnum.MIDDLE, Position.PlaneEnum.RIGHT, Position.RotationEnum.TOP);
                case 7: // Top Left
                    return (Position.DepthEnum.BEHIND, Position.PlaneEnum.LEFT, Position.RotationEnum.TOP);
                case 8: // Top Center
                    return (Position.DepthEnum.BEHIND, Position.PlaneEnum.MIDDLE, Position.RotationEnum.TOP);
                case 9: // Top Right
                    return (Position.DepthEnum.BEHIND, Position.PlaneEnum.RIGHT, Position.RotationEnum.TOP);
                case 10: // Centroid (use middle/middle)
                    return (Position.DepthEnum.MIDDLE, Position.PlaneEnum.MIDDLE, Position.RotationEnum.TOP);
                case 11: // Shear Center (use middle/middle)  
                    return (Position.DepthEnum.MIDDLE, Position.PlaneEnum.MIDDLE, Position.RotationEnum.TOP);
                default:
                    LogMessage("CARDINAL_POINT_WARNING", 
                        "Unknown cardinal point " + cardinalPoint.ToString() + ", using default MIDDLE/MIDDLE/TOP");
                    return (Position.DepthEnum.MIDDLE, Position.PlaneEnum.MIDDLE, Position.RotationEnum.TOP);
            }
        }

        private static string ExecuteCodeWithCapture(string code)
        {
            var originalOut = Console.Out;
            var consoleOutput = new StringBuilder();

            using (var stringWriter = new StringWriter(consoleOutput))
            {
                Console.SetOut(stringWriter);
                
                try
                {
                    return ExecuteCodeDirect(code);
                }
                finally
                {
                    Console.SetOut(originalOut);
                }
            }
        }

        static string ExecuteCodeDirect(string code)
        {
            Console.WriteLine("=== PHASE 3 C# EXECUTION ===");
            Console.WriteLine("🔧 LEGACY SYNTAX REQUIREMENTS:");
            Console.WriteLine("❌ NO: $\"{variable}\" | ✅ USE: \"text \" + variable");
            Console.WriteLine("❌ NO: obj?.Property | ✅ USE: (obj != null) ? obj.Property : defaultValue");
            Console.WriteLine("");
            
            var model = new Model();
            if (!model.GetConnectionStatus())
            {
                throw new Exception("Tekla Structures not connected");
            }
            
            try
            {
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

        try 
        {{
            model.CommitChanges();
            Console.WriteLine(""✅ Changes committed successfully"");
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
                    parameters.ReferencedAssemblies.Add("System.dll");
                    parameters.ReferencedAssemblies.Add("System.Core.dll");

                    // Auto-discover Tekla assemblies
                    bool drawingDllAdded = false;
                    foreach (var loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            if (!loadedAssembly.IsDynamic && !string.IsNullOrEmpty(loadedAssembly.Location))
                            {
                                var name = loadedAssembly.GetName().Name;
                                if (name.StartsWith("Tekla.Structures") || name.Equals("Newtonsoft.Json"))
                                {
                                    parameters.ReferencedAssemblies.Add(loadedAssembly.Location);

                                    // Check if Drawing DLL was found
                                    if (name.Equals("Tekla.Structures.Drawing"))
                                    {
                                        drawingDllAdded = true;
                                        LogMessage("ASSEMBLY_DISCOVERY", "Tekla.Structures.Drawing found via auto-discovery");
                                    }
                                }
                            }
                        }
                        catch { /* Skip problematic assemblies */ }
                    }

                    // If Drawing DLL wasn't found via auto-discovery, add it manually
                    // It should be in the same directory as Tekla.Structures.Model.dll
                    if (!drawingDllAdded)
                    {
                        try
                        {
                            LogMessage("ASSEMBLY_MANUAL_ADD", "Drawing DLL not auto-discovered, attempting manual add");

                            // Find Tekla install directory from loaded Model assembly
                            var modelAssembly = AppDomain.CurrentDomain.GetAssemblies()
                                .FirstOrDefault(a => a.GetName().Name == "Tekla.Structures.Model");

                            if (modelAssembly != null && !string.IsNullOrEmpty(modelAssembly.Location))
                            {
                                var teklaDir = System.IO.Path.GetDirectoryName(modelAssembly.Location);
                                var drawingDllPath = System.IO.Path.Combine(teklaDir, "Tekla.Structures.Drawing.dll");

                                LogMessage("ASSEMBLY_SEARCH", $"Looking for Drawing DLL at: {drawingDllPath}");

                                if (System.IO.File.Exists(drawingDllPath))
                                {
                                    parameters.ReferencedAssemblies.Add(drawingDllPath);
                                    LogMessage("ASSEMBLY_ADDED", $"Successfully added Tekla.Structures.Drawing from {drawingDllPath}");
                                }
                                else
                                {
                                    LogMessage("ASSEMBLY_ERROR", $"Tekla.Structures.Drawing.dll not found at {drawingDllPath}");
                                }
                            }
                            else
                            {
                                LogMessage("ASSEMBLY_ERROR", "Could not locate Tekla.Structures.Model assembly");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage("ASSEMBLY_ERROR", $"Exception adding Drawing DLL: {ex.Message}");
                        }
                    }

                    parameters.GenerateInMemory = true;
                    parameters.GenerateExecutable = false;

                    var results = provider.CompileAssemblyFromSource(parameters, code);

                    if (results.Errors.HasErrors)
                    {
                        var errors = string.Join("\n", results.Errors.Cast<CompilerError>()
                            .Select(e => $"Line {e.Line}: {e.ErrorText}"));
                        throw new Exception($"Compilation errors:\n{errors}");
                    }

                    Console.WriteLine("✅ Code compiled successfully");

                    var compiledAssembly = results.CompiledAssembly;
                    var type = compiledAssembly.GetType("DynamicTeklaScript");
                    var instance = Activator.CreateInstance(type);
                    var method = type.GetMethod("Execute");

                    Console.WriteLine("🚀 Executing compiled code...");
                    var result = method.Invoke(instance, null);
                    
                    Console.WriteLine($"✅ Execution completed: {result}");
                    return result?.ToString() ?? "Executed successfully";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Compilation/execution error: {ex.Message}");
                throw new Exception($"Compilation/execution error: {ex.Message}");
            }
        }

        static void LogMessage(string category, string message)
        {
            try
            {
                var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {category} | {message}";
                File.AppendAllText(config.LogFile, logEntry + Environment.NewLine);
                
                if (config.EnableDetailedLogging)
                {
                    Console.WriteLine($"📋 LOG: {logEntry}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Logging error: {ex.Message}");
            }
        }
    }
}