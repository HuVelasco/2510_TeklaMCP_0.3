using System;
using System.Linq;
using System.Net;
using TeklaMCP.HttpServer.Core;

namespace TeklaMCP.HttpServer.Endpoints
{
    public class ModelEndpoints : BaseEndpoint
    {
        public ModelEndpoints(TeklaModelManager modelManager) : base(modelManager)
        {
        }

        public string GetModelInfo()
        {
            try
            {
                if (!IsModelConnected()) 
                    return CreateErrorResponse("Not connected to Tekla");

                var modelInfo = modelManager.GetModelInfo();
                if (modelInfo == null)
                    return CreateErrorResponse("Failed to get model information");

                var data = new
                {
                    modelName = modelInfo.ModelName,
                    objectCounts = modelInfo.ObjectCounts,
                    gridPoints = modelInfo.GridPointCount,
                    connectionStatus = modelInfo.ConnectionStatus
                };

                return CreateSuccessResponse("Model information retrieved", data);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"Failed to get model info: {ex.Message}");
            }
        }

        public string GetAllObjects()
        {
            try
            {
                var objects = modelManager.ObjectRegistry.GetAllObjects();
                return CreateSuccessResponse("All tracked objects retrieved", objects);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"Failed to get objects: {ex.Message}");
            }
        }

        public string GetObjectDetails(string path)
        {
            try
            {
                // Parse path: /object/123 or /object/name/Column-A1
                var parts = path.Split('/');

                if (parts.Length >= 3)
                {
                    if (parts[2] == "name" && parts.Length >= 4)
                    {
                        // Get by name: /object/name/Column-A1
                        string objectName = string.Join("/", parts.Skip(3));
                        var obj = modelManager.ObjectRegistry.GetObjectByName(objectName);
                        return obj != null ?
                            CreateSuccessResponse("Object found", obj) :
                            CreateErrorResponse("Object not found", 404);
                    }
                    else
                    {
                        // Get by ID: /object/123
                        if (int.TryParse(parts[2], out int id))
                        {
                            var obj = modelManager.ObjectRegistry.GetObjectById(id);
                            return obj != null ?
                                CreateSuccessResponse("Object found", obj) :
                                CreateErrorResponse("Object not found", 404);
                        }
                    }
                }

                return CreateErrorResponse("Invalid object path format", 400);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"Error getting object details: {ex.Message}");
            }
        }
    }
}