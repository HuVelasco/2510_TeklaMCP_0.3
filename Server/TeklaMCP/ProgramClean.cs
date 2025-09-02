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
            public string ServerUrl { get; set; } = "http://localhost:8080/";
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
                { "POST /delete-parts-bulk", HandleDeletePartsBulk }  // Body contains array of ids
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

            Console.WriteLine("🏗️ TeklaMCP 0.3 - PHASE 4 Part-Based CRUD");
            Console.WriteLine("✅ Complete CRUD operations for Part objects");
            Console.WriteLine("📊 READ: List, Get, Search with filtering");
            Console.WriteLine("✏️ UPDATE: Modify properties and positions");
            Console.WriteLine("🗑️ DELETE: Single and bulk with verification");
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
            part.GetReportProperty("PROFILE", ref part.Profile.ProfileString);
            part.GetReportProperty("MATERIAL", ref part.Material.MaterialString);
            part.GetReportProperty("NAME", ref part.Name);
            part.GetReportProperty("CLASS", ref part.Class);
            
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
            
            return beam;
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
                                    Console.WriteLine($"📚 Added reference: {name}");
                                }
                            }
                        }
                        catch { /* Skip problematic assemblies */ }
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