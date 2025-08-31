using System;
using System.Net;
using TeklaMCP.HttpServer.Endpoints;

namespace TeklaMCP.HttpServer.Core
{
    public class EndpointRouter
    {
        private readonly TeklaModelManager modelManager;
        private readonly GridEndpoints gridEndpoints;
        private readonly BeamEndpoints beamEndpoints;
        private readonly ModelEndpoints modelEndpoints;

        public EndpointRouter(TeklaModelManager modelManager)
        {
            this.modelManager = modelManager;
            this.gridEndpoints = new GridEndpoints(modelManager);
            this.beamEndpoints = new BeamEndpoints(modelManager);
            this.modelEndpoints = new ModelEndpoints(modelManager);
        }

        public string RouteRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            string method = request.HttpMethod;
            string path = request.Url.AbsolutePath;

            Console.WriteLine($"Received {method} request: {path}");

            try
            {
                // Handle different endpoints
                switch ($"{method} {path}")
                {
                    // Basic endpoints
                    case "GET /test":
                        response.ContentType = "text/plain";
                        return "Hello! The Hybrid Tekla MCP server is working!";

                    case "GET /model-info":
                        return modelEndpoints.GetModelInfo();

                    case "GET /objects":
                        return modelEndpoints.GetAllObjects();

                    case "GET /grid/points":
                        return gridEndpoints.GetGridPoints();

                    // Grid endpoints (GET - working version style)
                    case "GET /create-grid":
                        response.ContentType = "text/plain";
                        return gridEndpoints.CreateCustomGrid(request);

                    case "GET /create-advanced-grid":
                        response.ContentType = "text/plain";
                        return gridEndpoints.CreateAdvancedGrid(request);

                    case "GET /create-master-grid":
                        response.ContentType = "text/plain";
                        return gridEndpoints.CreateMasterGrid(request);

                    case "POST /grid/create":
                        return gridEndpoints.CreateGridJSON(request);

                    // Beam/Column endpoints (GET - working version style)
                    case "GET /create-column":
                        response.ContentType = "text/plain";
                        return beamEndpoints.CreateColumn(request);

                    case "GET /create-advanced-beam":
                        response.ContentType = "text/plain";
                        return beamEndpoints.CreateAdvancedBeam(request);

                    case "GET /create-master-beam":
                        response.ContentType = "text/plain";
                        return beamEndpoints.CreateMasterBeam(request);

                    case "POST /beam/create":
                        return beamEndpoints.CreateBeamJSON(request);

                    case "GET /modify-column":
                        response.ContentType = "text/plain";
                        return beamEndpoints.ModifyColumn(request);

                    case "PUT /beam/modify":
                        return beamEndpoints.ModifyBeamJSON(request);

                    // Object lookup endpoints
                    default:
                        if (path.StartsWith("/object/"))
                        {
                            return modelEndpoints.GetObjectDetails(path);
                        }
                        else
                        {
                            return BaseEndpoint.CreateErrorResponse("Endpoint not found", 404);
                        }
                }
            }
            catch (Exception ex)
            {
                return BaseEndpoint.CreateErrorResponse($"Server error: {ex.Message}", 500);
            }
        }
    }
}