using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TeklaMCP.Controllers;
using TeklaMCP.Models;

namespace TeklaMCP.Server
{
    /// <summary>
    /// Phase 1 HTTP Server - Pure Infrastructure Layer
    /// Only handles routing, CORS, and response formatting
    /// All business logic delegated to controllers
    /// </summary>
    public class HttpServer
    {
        private HttpListener listener;
        private readonly ElementsController elementsController;
        private readonly UtilsController utilsController;
        private readonly int port;
        private bool isRunning;

        public HttpServer(int port = 8080)
        {
            this.port = port;
            this.elementsController = new ElementsController();
            this.utilsController = new UtilsController();
        }

        /// <summary>
        /// Start the HTTP server
        /// </summary>
        public async Task Start()
        {
            listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            
            try
            {
                listener.Start();
                isRunning = true;
                
                Console.WriteLine($"===========================================");
                Console.WriteLine($"   TeklaMCP HTTP Server - Phase 1");
                Console.WriteLine($"   Clean Controller Architecture");
                Console.WriteLine($"===========================================");
                Console.WriteLine($"Server listening on port {port}");
                Console.WriteLine($"Phase 1 Endpoints (as per manual):");
                Console.WriteLine($"  POST /create-element    - Create any element");
                Console.WriteLine($"  GET  /elements/{{id}}     - Get element info");
                Console.WriteLine($"  GET  /test              - Health check");
                Console.WriteLine($"  GET  /tekla-status      - Connection status");
                Console.WriteLine($"  POST /execute-code      - Custom operations");
                Console.WriteLine($"  GET  /utils/profiles    - Available profiles");
                Console.WriteLine($"===========================================");

                while (isRunning)
                {
                    try
                    {
                        var context = await listener.GetContextAsync();
                        _ = Task.Run(() => HandleRequest(context));
                    }
                    catch (HttpListenerException)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server startup error: {ex.Message}");
            }
        }

        /// <summary>
        /// Main request handler - pure routing logic only
        /// </summary>
        private async Task HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            
            // Infrastructure: CORS handling
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.Close();
                return;
            }

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {request.HttpMethod} {request.Url.AbsolutePath}");

            try
            {
                // Pure routing - delegate to controllers
                object result = await RouteRequest(request);
                await SendJsonResponse(response, result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request error: {ex.Message}");
                response.StatusCode = 500;
                await SendJsonResponse(response, new ApiResponse 
                { 
                    Success = false, 
                    Error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Route requests to appropriate controllers (following Phase 1 manual)
        /// </summary>
        private async Task<object> RouteRequest(HttpListenerRequest request)
        {
            var path = request.Url.AbsolutePath.ToLower();
            var method = request.HttpMethod;
            
            return path switch
            {
                // Phase 1 endpoints as specified in manual
                "/create-element" when method == "POST" => 
                    await HandleCreateElement(request),
                
                var p when p.StartsWith("/elements/") && method == "GET" => 
                    HandleGetElement(path),
                
                "/test" when method == "GET" => 
                    new ApiResponse { Success = true, Data = "Server is running" },
                
                "/tekla-status" when method == "GET" => 
                    utilsController.GetTeklaStatus(),
                
                "/execute-code" when method == "POST" => 
                    await HandleExecuteCode(request),
                
                "/utils/profiles" when method == "GET" => 
                    utilsController.GetAvailableProfiles(request.QueryString["type"]),
                
                _ => new ApiResponse 
                { 
                    Success = false, 
                    Error = "Endpoint not found", 
                    Data = new { path = request.Url.AbsolutePath, availableEndpoints = GetAvailableEndpoints() }
                }
            };
        }

        /// <summary>
        /// Handle element creation - delegate to controller
        /// </summary>
        private async Task<ApiResponse> HandleCreateElement(HttpListenerRequest request)
        {
            var body = await ReadRequestBody(request);
            var elementRequest = JsonConvert.DeserializeObject<ElementRequest>(body);
            
            // Pure delegation to controller
            return elementsController.CreateElement(elementRequest);
        }

        /// <summary>
        /// Handle element info retrieval - delegate to controller
        /// </summary>
        private ApiResponse HandleGetElement(string path)
        {
            var parts = path.Split('/');
            if (parts.Length == 3 && int.TryParse(parts[2], out int elementId))
            {
                return elementsController.GetElementInfo(elementId);
            }
            
            return new ApiResponse 
            { 
                Success = false, 
                Error = "Invalid element ID format" 
            };
        }

        /// <summary>
        /// Handle code execution - delegate to controller
        /// </summary>
        private async Task<ApiResponse> HandleExecuteCode(HttpListenerRequest request)
        {
            var code = await ReadRequestBody(request);
            
            // Pure delegation to controller
            return utilsController.ExecuteCode(code);
        }

        /// <summary>
        /// Get list of available endpoints for error responses
        /// </summary>
        private string[] GetAvailableEndpoints()
        {
            return new[]
            {
                "POST /create-element",
                "GET /elements/{id}",
                "GET /test",
                "GET /tekla-status", 
                "POST /execute-code",
                "GET /utils/profiles?type=BEAM|COLUMN|BRACE"
            };
        }

        /// <summary>
        /// Infrastructure: Read request body
        /// </summary>
        private async Task<string> ReadRequestBody(HttpListenerRequest request)
        {
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                return await reader.ReadToEndAsync();
            }
        }

        /// <summary>
        /// Infrastructure: Send JSON response
        /// </summary>
        private async Task SendJsonResponse(HttpListenerResponse response, object data)
        {
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            var buffer = Encoding.UTF8.GetBytes(json);
            
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.Close();
        }

        /// <summary>
        /// Stop the server
        /// </summary>
        public void Stop()
        {
            isRunning = false;
            listener?.Stop();
            listener?.Close();
            Console.WriteLine("Server stopped");
        }
    }

    /// <summary>
    /// Program entry point for Phase 1 architecture
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var server = new HttpServer(8081);
            
            // Handle shutdown gracefully
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                server.Stop();
            };
            
            await server.Start();
        }
    }
}