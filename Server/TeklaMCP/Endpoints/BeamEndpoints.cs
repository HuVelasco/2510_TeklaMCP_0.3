using System;
using System.Linq;
using System.Net;
using Tekla.Structures.Model;
using Tekla.Structures.Geometry3d;
using TeklaMCP.HttpServer.Core;

namespace TeklaMCP.HttpServer.Endpoints
{
    public class BeamEndpoints : BaseEndpoint
    {
        public BeamEndpoints(TeklaModelManager modelManager) : base(modelManager)
        {
        }

        public string CreateColumn(HttpListenerRequest request)
        {
            try
            {
                if (!IsModelConnected())
                    return "{'error': 'Not connected to Tekla'}";

                var query = request.QueryString;
                string location = query["location"] ?? "A-1";
                string profile = query["profile"] ?? "W12X50";
                double height = double.TryParse(query["height"], out double h) ? h : 12000.0;

                // Get grid point for location
                var gridPoints = modelManager.GridManager.GetAllGridPoints();
                if (!gridPoints.ContainsKey(location))
                {
                    return $"{{'error': 'Grid location {location} not found. Create a grid first.'}}";
                }

                Point gridPoint = gridPoints[location];

                Beam column = new Beam();
                column.StartPoint = new Point(gridPoint.X, gridPoint.Y, gridPoint.Z);
                column.EndPoint = new Point(gridPoint.X, gridPoint.Y, gridPoint.Z + height);
                column.Profile.ProfileString = profile;
                column.Material.MaterialString = "A992";
                column.Class = "1";
                column.Name = $"COL-{location}";

                bool success = column.Insert();
                CommitChanges();

                if (success)
                {
                    modelManager.ObjectRegistry.RegisterObject(column);
                    return $"{{'success': true, 'message': 'Column {profile} created at {location}', 'id': {column.Identifier.ID}}}";
                }
                else
                {
                    return "{'error': 'Failed to insert column into model'}";
                }
            }
            catch (Exception ex)
            {
                return $"{{'error': 'Column creation failed: {ex.Message}'}}";
            }
        }

        public string CreateAdvancedBeam(HttpListenerRequest request)
        {
            try
            {
                if (!IsModelConnected())
                    return "{'error': 'Not connected to Tekla'}";

                var query = request.QueryString;

                Beam beam = new Beam();

                // Handle grid location vs coordinates
                if (!string.IsNullOrEmpty(query["grid_location"]))
                {
                    string location = query["grid_location"];
                    var gridPoints = modelManager.GridManager.GetAllGridPoints();
                    if (!gridPoints.ContainsKey(location))
                    {
                        return $"{{'error': 'Grid location {location} not found'}}";
                    }

                    Point gridPoint = gridPoints[location];
                    double height = double.TryParse(query["height"], out double h) ? h : 12000.0;

                    beam.StartPoint = new Point(gridPoint.X, gridPoint.Y, gridPoint.Z);
                    beam.EndPoint = new Point(gridPoint.X, gridPoint.Y, gridPoint.Z + height);
                }
                else
                {
                    // Use coordinate parameters
                    double startX = double.TryParse(query["start_x"], out double sx) ? sx : 0.0;
                    double startY = double.TryParse(query["start_y"], out double sy) ? sy : 0.0;
                    double startZ = double.TryParse(query["start_z"], out double sz) ? sz : 0.0;
                    double endX = double.TryParse(query["end_x"], out double ex) ? ex : 3000.0;
                    double endY = double.TryParse(query["end_y"], out double ey) ? ey : 0.0;
                    double endZ = double.TryParse(query["end_z"], out double ez) ? ez : 0.0;

                    beam.StartPoint = new Point(startX, startY, startZ);
                    beam.EndPoint = new Point(endX, endY, endZ);
                }

                // Set beam properties
                beam.Profile.ProfileString = query["profile"] ?? "W12X50";
                beam.Material.MaterialString = query["material"] ?? "A992";
                beam.Class = query["beam_class"] ?? "1";
                beam.Finish = query["finish"] ?? "PAINT";
                beam.Name = query["name"] ?? "BEAM";

                bool success = beam.Insert();
                CommitChanges();

                if (success)
                {
                    modelManager.ObjectRegistry.RegisterObject(beam);
                    return $"{{'success': true, 'message': 'Beam created successfully', 'id': {beam.Identifier.ID}}}";
                }
                else
                {
                    return "{'error': 'Failed to insert beam into model'}";
                }
            }
            catch (Exception ex)
            {
                return $"{{'error': 'Beam creation failed: {ex.Message}'}}";
            }
        }

        public string ModifyColumn(HttpListenerRequest request)
        {
            try
            {
                if (!IsModelConnected())
                    return "{'error': 'Not connected to Tekla'}";

                var query = request.QueryString;
                string location = query["location"];
                string newProfile = query["new_profile"];

                if (string.IsNullOrEmpty(location) || string.IsNullOrEmpty(newProfile))
                {
                    return "{'error': 'Both location and new_profile are required'}";
                }

                // Find column at grid location
                var trackedObj = modelManager.ObjectRegistry.GetAllObjects()
                    .FirstOrDefault(obj => obj.Name.EndsWith($"-{location}") && obj.Type == "Beam");

                if (trackedObj == null)
                {
                    return $"{{'error': 'No column found at location {location}'}}";
                }

                // Modify the column
                Beam column = new Beam();
                column.Identifier.ID = trackedObj.TeklaId;
                column.Select();
                column.Profile.ProfileString = newProfile;
                bool success = column.Modify();
                CommitChanges();

                if (success)
                {
                    return $"{{'success': true, 'message': 'Column at {location} modified to {newProfile}'}}";
                }
                else
                {
                    return "{'error': 'Failed to modify column'}";
                }
            }
            catch (Exception ex)
            {
                return $"{{'error': 'Column modification failed: {ex.Message}'}}";
            }
        }

        public string CreateMasterBeam(HttpListenerRequest request)
        {
            try
            {
                if (!IsModelConnected())
                    return CreateErrorResponse("Not connected to Tekla");

                var query = request.QueryString;

                Beam beam = new Beam();

                // Basic geometry - points (required)
                if (!string.IsNullOrEmpty(query["grid_location"]))
                {
                    // Grid-based positioning
                    string location = query["grid_location"];
                    var gridPoints = modelManager.GridManager.GetAllGridPoints();
                    if (!gridPoints.ContainsKey(location))
                    {
                        return CreateErrorResponse($"Grid location {location} not found");
                    }

                    Point gridPoint = gridPoints[location];
                    double height = double.TryParse(query["height"], out double h) ? h : 12000.0;
                    double lengthX = double.TryParse(query["length_x"], out double lx) ? lx : 0.0;
                    double lengthY = double.TryParse(query["length_y"], out double ly) ? ly : 0.0;

                    beam.StartPoint = new Point(gridPoint.X, gridPoint.Y, gridPoint.Z);
                    beam.EndPoint = new Point(gridPoint.X + lengthX, gridPoint.Y + lengthY, gridPoint.Z + height);
                }
                else
                {
                    // Direct coordinate positioning
                    double startX = double.TryParse(query["start_x"], out double sx) ? sx : 0.0;
                    double startY = double.TryParse(query["start_y"], out double sy) ? sy : 0.0;
                    double startZ = double.TryParse(query["start_z"], out double sz) ? sz : 0.0;
                    double endX = double.TryParse(query["end_x"], out double ex) ? ex : 3000.0;
                    double endY = double.TryParse(query["end_y"], out double ey) ? ey : 0.0;
                    double endZ = double.TryParse(query["end_z"], out double ez) ? ez : 0.0;

                    beam.StartPoint = new Point(startX, startY, startZ);
                    beam.EndPoint = new Point(endX, endY, endZ);
                }

                // Profile properties
                beam.Profile.ProfileString = query["profile"] ?? "W12X50";
                
                // Material properties
                beam.Material.MaterialString = query["material"] ?? "A992";
                
                // Part properties
                beam.Name = query["name"] ?? "BEAM";
                beam.Class = query["beam_class"] ?? "1";
                beam.Finish = query["finish"] ?? "";
                
                // Assembly and part numbering
                if (!string.IsNullOrEmpty(query["assembly_number"]))
                {
                    beam.AssemblyNumber.Prefix = query["assembly_prefix"] ?? "";
                    if (int.TryParse(query["assembly_number"], out int assemblyNum))
                    {
                        beam.AssemblyNumber.StartNumber = assemblyNum;
                    }
                }
                
                if (!string.IsNullOrEmpty(query["part_number"]))
                {
                    beam.PartNumber.Prefix = query["part_prefix"] ?? "";
                    if (int.TryParse(query["part_number"], out int partNum))
                    {
                        beam.PartNumber.StartNumber = partNum;
                    }
                }

                // Position properties (comprehensive support)
                if (query["position_depth"] != null || query["position_plane"] != null || query["position_rotation"] != null)
                {
                    beam.Position = new Position();
                    
                    // Depth positioning
                    if (Enum.TryParse<Position.DepthEnum>(query["position_depth"], true, out Position.DepthEnum depth))
                    {
                        beam.Position.Depth = depth;
                    }
                    if (double.TryParse(query["position_depth_offset"], out double depthOffset))
                    {
                        beam.Position.DepthOffset = depthOffset;
                    }
                    
                    // Plane positioning
                    if (Enum.TryParse<Position.PlaneEnum>(query["position_plane"], true, out Position.PlaneEnum plane))
                    {
                        beam.Position.Plane = plane;
                    }
                    if (double.TryParse(query["position_plane_offset"], out double planeOffset))
                    {
                        beam.Position.PlaneOffset = planeOffset;
                    }
                    
                    // Rotation positioning
                    if (Enum.TryParse<Position.RotationEnum>(query["position_rotation"], true, out Position.RotationEnum rotation))
                    {
                        beam.Position.Rotation = rotation;
                    }
                    if (double.TryParse(query["position_rotation_offset"], out double rotationOffset))
                    {
                        beam.Position.RotationOffset = rotationOffset;
                    }
                }

                // Offset properties (end point offsets)
                if (query["start_offset_dx"] != null || query["start_offset_dy"] != null || query["start_offset_dz"] != null)
                {
                    beam.StartPointOffset = new Offset();
                    beam.StartPointOffset.Dx = double.TryParse(query["start_offset_dx"], out double sox) ? sox : 0.0;
                    beam.StartPointOffset.Dy = double.TryParse(query["start_offset_dy"], out double soy) ? soy : 0.0;
                    beam.StartPointOffset.Dz = double.TryParse(query["start_offset_dz"], out double soz) ? soz : 0.0;
                }

                if (query["end_offset_dx"] != null || query["end_offset_dy"] != null || query["end_offset_dz"] != null)
                {
                    beam.EndPointOffset = new Offset();
                    beam.EndPointOffset.Dx = double.TryParse(query["end_offset_dx"], out double eox) ? eox : 0.0;
                    beam.EndPointOffset.Dy = double.TryParse(query["end_offset_dy"], out double eoy) ? eoy : 0.0;
                    beam.EndPointOffset.Dz = double.TryParse(query["end_offset_dz"], out double eoz) ? eoz : 0.0;
                }

                // Cast unit type (for concrete parts)
                if (!string.IsNullOrEmpty(query["cast_unit_type"]))
                {
                    if (Enum.TryParse<Part.CastUnitTypeEnum>(query["cast_unit_type"], true, out Part.CastUnitTypeEnum castType))
                    {
                        beam.CastUnitType = castType;
                    }
                }

                // Pour phase (for concrete parts)
                if (!string.IsNullOrEmpty(query["pour_phase"]))
                {
                    if (int.TryParse(query["pour_phase"], out int pourPhase))
                    {
                        beam.PourPhase = pourPhase;
                    }
                }

                bool success = beam.Insert();
                CommitChanges();

                if (success)
                {
                    modelManager.ObjectRegistry.RegisterObject(beam);
                    
                    // Determine object type for response
                    string objectType = GetBeamTypeDescription(beam);
                    
                    return CreateSuccessResponse($"Master {objectType} '{beam.Name}' created successfully", 
                        new { 
                            id = beam.Identifier.ID,
                            type = objectType,
                            profile = beam.Profile.ProfileString,
                            material = beam.Material.MaterialString,
                            beamClass = beam.Class
                        });
                }
                else
                {
                    return CreateErrorResponse("Failed to insert master beam into model");
                }
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"Master beam creation failed: {ex.Message}");
            }
        }

        private string GetBeamTypeDescription(Beam beam)
        {
            // Determine if it's a column, beam, brace, etc. based on orientation
            var startPoint = beam.StartPoint;
            var endPoint = beam.EndPoint;
            
            double deltaX = Math.Abs(endPoint.X - startPoint.X);
            double deltaY = Math.Abs(endPoint.Y - startPoint.Y);
            double deltaZ = Math.Abs(endPoint.Z - startPoint.Z);
            
            if (deltaZ > deltaX && deltaZ > deltaY)
            {
                return "Column";
            }
            else if (deltaX > deltaZ && deltaX > deltaY)
            {
                return "Beam (X-direction)";
            }
            else if (deltaY > deltaZ && deltaY > deltaX)
            {
                return "Beam (Y-direction)";
            }
            else
            {
                return "Beam (Diagonal)";
            }
        }

        public string CreateBeamJSON(HttpListenerRequest request)
        {
            return CreateErrorResponse("JSON beam creation not implemented yet");
        }

        public string ModifyBeamJSON(HttpListenerRequest request)
        {
            return CreateErrorResponse("JSON beam modification not implemented yet");
        }
    }
}