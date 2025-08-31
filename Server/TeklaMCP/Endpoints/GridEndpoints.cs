using System;
using System.Linq;
using System.Net;
using Tekla.Structures.Model;
using TeklaMCP.HttpServer.Core;

namespace TeklaMCP.HttpServer.Endpoints
{
    public class GridEndpoints : BaseEndpoint
    {
        public GridEndpoints(TeklaModelManager modelManager) : base(modelManager)
        {
        }

        public string CreateCustomGrid(HttpListenerRequest request)
        {
            try
            {
                if (!IsModelConnected())
                    return "{'error': 'Not connected to Tekla'}";

                var query = request.QueryString;
                string xLabels = query["x"] ?? "A,B,C";
                string yLabels = query["y"] ?? "1,2,3";
                double xSpacing = double.TryParse(query["x_spacing"], out double xSpace) ? xSpace : 20.0;
                double ySpacing = double.TryParse(query["y_spacing"], out double ySpace) ? ySpace : 15.0;
                double feetToMm = 304.8;

                string[] xLabelArray = xLabels.Split(',');
                string[] yLabelArray = yLabels.Split(',');

                Grid grid = new Grid();
                grid.Name = $"Grid_{DateTime.Now:HHmmss}";

                grid.CoordinateX = "0.00" + (xLabelArray.Length > 1 ?
                    $" {xLabelArray.Length - 1}*{xSpacing * feetToMm:F2}" : "");
                grid.LabelX = string.Join(" ", xLabelArray);

                grid.CoordinateY = "0.00" + (yLabelArray.Length > 1 ?
                    $" {yLabelArray.Length - 1}*{ySpacing * feetToMm:F2}" : "");
                grid.LabelY = string.Join(" ", yLabelArray);

                grid.CoordinateZ = "0.00";
                grid.LabelZ = "+0";

                double extension = 10 * feetToMm;
                grid.ExtensionLeftX = extension;
                grid.ExtensionRightX = extension;
                grid.ExtensionLeftY = extension;
                grid.ExtensionRightY = extension;

                bool success = grid.Insert();
                CommitChanges();

                if (success)
                {
                    modelManager.ObjectRegistry.RegisterObject(grid);
                    modelManager.GridManager.CacheGridPoints(modelManager.Model);
                    return $"{{'success': true, 'message': 'Custom grid created: {xLabels} ({xSpacing}ft spacing) and {yLabels} ({ySpacing}ft spacing)', 'id': {grid.Identifier.ID}}}";
                }
                else
                {
                    return "{'error': 'Failed to insert grid into model'}";
                }
            }
            catch (Exception ex)
            {
                return $"{{'error': 'Grid creation failed: {ex.Message}'}}";
            }
        }

        public string CreateAdvancedGrid(HttpListenerRequest request)
        {
            try
            {
                if (!IsModelConnected())
                    return "{'error': 'Not connected to Tekla'}";

                var query = request.QueryString;

                Grid grid = new Grid();
                grid.Name = query["name"] ?? $"Grid_{DateTime.Now:HHmmss}";
                grid.CoordinateX = query["x_coordinates"] ?? "0.00 2*6096.00";
                grid.CoordinateY = query["y_coordinates"] ?? "0.00 2*4572.00";
                grid.CoordinateZ = query["z_coordinates"] ?? "0.00";
                grid.LabelX = query["x_labels"] ?? "A B C";
                grid.LabelY = query["y_labels"] ?? "1 2 3";
                grid.LabelZ = query["z_labels"] ?? "+0";

                grid.ExtensionLeftX = double.TryParse(query["extension_left_x"], out double elx) ? elx : 3048.0;
                grid.ExtensionRightX = double.TryParse(query["extension_right_x"], out double erx) ? erx : 3048.0;
                grid.ExtensionLeftY = double.TryParse(query["extension_left_y"], out double ely) ? ely : 3048.0;
                grid.ExtensionRightY = double.TryParse(query["extension_right_y"], out double ery) ? ery : 3048.0;
                grid.IsMagnetic = bool.TryParse(query["is_magnetic"], out bool magnetic) ? magnetic : false;

                bool success = grid.Insert();
                CommitChanges();

                if (success)
                {
                    modelManager.ObjectRegistry.RegisterObject(grid);
                    modelManager.GridManager.CacheGridPoints(modelManager.Model);
                    return $"{{'success': true, 'message': 'Advanced grid \"{grid.Name}\" created', 'id': {grid.Identifier.ID}}}";
                }
                else
                {
                    return "{'error': 'Failed to insert advanced grid into model'}";
                }
            }
            catch (Exception ex)
            {
                return $"{{'error': 'Advanced grid creation failed: {ex.Message}'}}";
            }
        }

        public string GetGridPoints()
        {
            try
            {
                var gridPoints = modelManager.GridManager.GetAllGridPoints();
                var data = gridPoints.Select(kvp => new
                {
                    intersection = kvp.Key,
                    x = kvp.Value.X,
                    y = kvp.Value.Y,
                    z = kvp.Value.Z
                }).ToArray();

                return CreateSuccessResponse($"Retrieved {gridPoints.Count} grid points", data);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"Failed to get grid points: {ex.Message}");
            }
        }

        public string CreateMasterGrid(HttpListenerRequest request)
        {
            try
            {
                if (!IsModelConnected())
                    return CreateErrorResponse("Not connected to Tekla");

                var query = request.QueryString;

                Grid grid = new Grid();
                
                // Basic properties
                grid.Name = query["name"] ?? $"Grid_{DateTime.Now:HHmmss}";
                
                // Coordinate system properties
                grid.CoordinateX = query["coordinate_x"] ?? "0.00 4*3000.00";
                grid.CoordinateY = query["coordinate_y"] ?? "0.00 5*6000.00";
                grid.CoordinateZ = query["coordinate_z"] ?? "0.00 6000.00 8000.00 9000.00";
                
                // Label properties
                grid.LabelX = query["label_x"] ?? "A B C D E";
                grid.LabelY = query["label_y"] ?? "1 2 3 4 5 6";
                grid.LabelZ = query["label_z"] ?? "+0 +6000 +8000 +9000";
                
                // Extension properties
                grid.ExtensionLeftX = double.TryParse(query["extension_left_x"], out double elx) ? elx : 2000.0;
                grid.ExtensionLeftY = double.TryParse(query["extension_left_y"], out double ely) ? ely : 2000.0;
                grid.ExtensionLeftZ = double.TryParse(query["extension_left_z"], out double elz) ? elz : 2000.0;
                grid.ExtensionRightX = double.TryParse(query["extension_right_x"], out double erx) ? erx : 2000.0;
                grid.ExtensionRightY = double.TryParse(query["extension_right_y"], out double ery) ? ery : 2000.0;
                grid.ExtensionRightZ = double.TryParse(query["extension_right_z"], out double erz) ? erz : 2000.0;
                
                // Magnetic property
                grid.IsMagnetic = bool.TryParse(query["is_magnetic"], out bool magnetic) ? magnetic : false;
                
                // Font properties (if provided)
                if (int.TryParse(query["font_size"], out int fontSize))
                {
                    grid.FontSize = fontSize;
                }
                
                // Origin property (if provided)
                if (!string.IsNullOrEmpty(query["origin_x"]) && 
                    !string.IsNullOrEmpty(query["origin_y"]) && 
                    !string.IsNullOrEmpty(query["origin_z"]))
                {
                    double ox = double.TryParse(query["origin_x"], out double originX) ? originX : 0.0;
                    double oy = double.TryParse(query["origin_y"], out double originY) ? originY : 0.0;
                    double oz = double.TryParse(query["origin_z"], out double originZ) ? originZ : 0.0;
                    grid.Origin = new Tekla.Structures.Geometry3d.Point(ox, oy, oz);
                }

                bool success = grid.Insert();
                CommitChanges();

                if (success)
                {
                    modelManager.ObjectRegistry.RegisterObject(grid);
                    modelManager.GridManager.CacheGridPoints(modelManager.Model);
                    return CreateSuccessResponse($"Master grid '{grid.Name}' created successfully", new { id = grid.Identifier.ID });
                }
                else
                {
                    return CreateErrorResponse("Failed to insert master grid into model");
                }
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"Master grid creation failed: {ex.Message}");
            }
        }

        public string CreateGridJSON(HttpListenerRequest request)
        {
            return CreateErrorResponse("JSON grid creation not implemented yet");
        }
    }
}