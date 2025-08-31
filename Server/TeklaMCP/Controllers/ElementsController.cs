using System;
using Tekla.Structures.Model;
using Tekla.Structures.Geometry3d;
using TeklaMCP.Models;

namespace TeklaMCP.Controllers
{
    /// <summary>
    /// Controller for element creation operations following Phase 1 architecture
    /// </summary>
    public class ElementsController
    {
        private readonly Model model;

        public ElementsController()
        {
            model = new Model();
        }

        /// <summary>
        /// Create any element type with specified parameters (generic endpoint as per Phase 1 plan)
        /// </summary>
        public ApiResponse CreateElement(ElementRequest request)
        {
            try
            {
                if (!ValidateConnection())
                {
                    return new ApiResponse 
                    { 
                        Success = false, 
                        Error = "Tekla Structures not connected" 
                    };
                }

                // Generic element creation based on ElementType
                var element = new Beam
                {
                    StartPoint = new Point(request.StartPoint.X, request.StartPoint.Y, request.StartPoint.Z),
                    EndPoint = new Point(request.EndPoint.X, request.EndPoint.Y, request.EndPoint.Z)
                };
                
                element.Profile.ProfileString = request.Profile;
                element.Material.MaterialString = request.Material ?? "A992";
                element.Class = request.Class ?? GetDefaultClass(request.ElementType);
                element.Name = request.ElementType?.ToUpper() ?? "BEAM";

                if (element.Insert())
                {
                    model.CommitChanges();
                    return new ApiResponse
                    {
                        Success = true,
                        Data = new
                        {
                            id = element.Identifier.ID,
                            message = $"{request.ElementType} created successfully with ID: {element.Identifier.ID}",
                            elementType = request.ElementType,
                            profile = request.Profile,
                            material = request.Material ?? "A992"
                        }
                    };
                }
                else
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Error = $"Failed to insert {request.ElementType} into model"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Error = $"Exception creating element: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get information about a specific element by ID
        /// </summary>
        public ApiResponse GetElementInfo(int elementId)
        {
            try
            {
                if (!ValidateConnection())
                {
                    return new ApiResponse 
                    { 
                        Success = false, 
                        Error = "Tekla Structures not connected" 
                    };
                }

                var modelObject = model.SelectModelObject(new Tekla.Structures.Identifier(elementId));
                
                if (modelObject == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Error = $"Element with ID {elementId} not found"
                    };
                }

                if (modelObject is Beam beam)
                {
                    string profile = "";
                    string material = "";
                    double length = 0.0;
                    
                    beam.GetReportProperty("PROFILE", ref profile);
                    beam.GetReportProperty("MATERIAL", ref material);
                    beam.GetReportProperty("LENGTH", ref length);
                    
                    return new ApiResponse
                    {
                        Success = true,
                        Data = new
                        {
                            id = elementId,
                            name = beam.Name,
                            profile = profile,
                            material = material,
                            length = length,
                            startPoint = new { x = beam.StartPoint.X, y = beam.StartPoint.Y, z = beam.StartPoint.Z },
                            endPoint = new { x = beam.EndPoint.X, y = beam.EndPoint.Y, z = beam.EndPoint.Z },
                            className = beam.Class
                        }
                    };
                }

                return new ApiResponse
                {
                    Success = true,
                    Data = new
                    {
                        id = elementId,
                        type = modelObject.GetType().Name
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Error = $"Exception getting element info: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Validate Tekla connection
        /// </summary>
        private bool ValidateConnection()
        {
            return model != null && model.GetConnectionStatus();
        }

        /// <summary>
        /// Get default class based on element type
        /// </summary>
        private string GetDefaultClass(string elementType)
        {
            return elementType?.ToUpper() switch
            {
                "BEAM" => "1",
                "COLUMN" => "1", 
                "BRACE" => "2",
                _ => "1"
            };
        }
    }
}