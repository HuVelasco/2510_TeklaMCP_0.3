using System;
using System.Net;
using System.Text;

namespace TeklaMCP.Core
{
    public class TeklaHttpServer
    {
        private HttpListener listener = null!;
        private bool isRunning;
        private TeklaModelManager modelManager = null!;
        private EndpointRouter router = null!;

        public void Start()
        {
            // Connect to Tekla and initialize systems
            modelManager = new TeklaModelManager();
            if (!modelManager.Initialize())
            {
                return;
            }

            // Initialize router
            router = new EndpointRouter(modelManager);

            // Start HTTP server
            listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            listener.Start();
            isRunning = true;

            Console.WriteLine("HTTP Server started on http://localhost:8080/");
            LogAvailableEndpoints();

            // Keep listening for requests
            while (isRunning)
            {
                try
                {
                    HttpListenerContext context = listener.GetContext();
                    ProcessRequest(context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private void LogAvailableEndpoints()
        {
            Console.WriteLine("📍 Available endpoints:");
            Console.WriteLine("   GET  /test - Simple server test");
            Console.WriteLine("   GET  /model-info - Get model information");
            Console.WriteLine("   GET  /objects - List all tracked objects");
            Console.WriteLine("   GET  /grid/points - Get grid intersection points");
            Console.WriteLine("");
            Console.WriteLine("🏗️ Grid endpoints:");
            Console.WriteLine("   GET  /create-grid - Simple grid creation");
            Console.WriteLine("   GET  /create-advanced-grid - Advanced grid creation");
            Console.WriteLine("   POST /grid/create - JSON grid creation");
            Console.WriteLine("");
            Console.WriteLine("🏢 Beam/Column endpoints:");
            Console.WriteLine("   GET  /create-column - Simple column creation");
            Console.WriteLine("   GET  /create-advanced-beam - Advanced beam creation");
            Console.WriteLine("   POST /beam/create - JSON beam/column creation");
            Console.WriteLine("   GET  /modify-column - Modify existing column");
            Console.WriteLine("   PUT  /beam/modify - JSON beam modification");
            Console.WriteLine("");
            Console.WriteLine("🔍 Object endpoints:");
            Console.WriteLine("   GET  /object/{id} - Get object details by ID");
            Console.WriteLine("   GET  /object/name/{name} - Get object by name");
            Console.WriteLine("");
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            string responseText = router.RouteRequest(request, response);

            // Send response
            if (response.ContentType == null)
            {
                response.ContentType = "application/json";
            }

            SendResponse(response, responseText);
            Console.WriteLine($"Response sent: {responseText.Substring(0, Math.Min(100, responseText.Length))}...");
        }

        private void SendResponse(HttpListenerResponse response, string content)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        public void Stop()
        {
            isRunning = false;
            listener?.Stop();
            Console.WriteLine("Server stopped.");
        }
    }
}