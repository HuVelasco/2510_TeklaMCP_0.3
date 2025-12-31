using System;
using System.Collections;
using System.Collections.Generic;
using Tekla.Structures.Drawing;
using Tekla.Structures.Model;
using Tekla.Structures.Geometry3d;
using TeklaDrawingExtractor.Extraction.DataModels;
using TeklaDrawingExtractor.Utilities;

namespace TeklaDrawingExtractor.Extraction.ObjectExtractors
{
    /// <summary>
    /// Extracts part data from drawing objects with full model properties
    /// RESTRUCTURED: Separates model properties from view-specific data
    /// </summary>
    public class PartExtractor
    {
        private Model _model;
        private Dictionary<Guid, Hashtable> _staticCache = new Dictionary<Guid, Hashtable>();
        private Dictionary<Guid, PartInfo> _partCache = new Dictionary<Guid, PartInfo>(); // Cache for model objects

        public PartExtractor()
        {
            _model = new Model();
        }

        /// <summary>
        /// Get or create PartInfo for a model object (ensures no duplication)
        /// </summary>
        public PartInfo GetOrCreatePartInfo(Tekla.Structures.Drawing.Part drawingPart, bool extractSolidVertices = false)
        {
            if (drawingPart.ModelIdentifier == null)
                return null;

            var modelObj = _model.SelectModelObject(drawingPart.ModelIdentifier);
            if (modelObj == null)
                return null;

            Guid guid = modelObj.Identifier.GUID;

            // Check cache first
            if (_partCache.ContainsKey(guid))
            {
                return _partCache[guid];
            }

            // Create new PartInfo
            var partInfo = new PartInfo
            {
                ModelId = modelObj.Identifier.ID,
                Guid = guid.ToString()
            };

            // Extract model properties based on type
            if (modelObj is Beam beam)
            {
                ExtractBeamProperties(beam, partInfo, extractSolidVertices);
            }
            else if (modelObj is ContourPlate plate)
            {
                ExtractPlateProperties(plate, partInfo);
            }
            else if (modelObj is PolyBeam polyBeam)
            {
                ExtractPolyBeamProperties(polyBeam, partInfo);
            }

            // Extract main part flag using GetReportProperty (CRITICAL FIX #6)
            partInfo.IsMainPart = ExtractIsMainPart(modelObj);

            // Cache it
            _partCache[guid] = partInfo;

            return partInfo;
        }

        /// <summary>
        /// Create view-specific data for a part (local coordinates)
        /// </summary>
        public PartViewInfo CreatePartViewInfo(Tekla.Structures.Drawing.Part drawingPart, string viewId, View view)
        {
            var viewInfo = new PartViewInfo
            {
                ViewId = viewId,
                Visible = true // If we found it in this view, it's visible
            };

            try
            {
                // Extract local geometry using work plane transformation
                viewInfo.GeometryLocal = ExtractLocalGeometry(drawingPart, view);

                // Extract related drawing objects (dimensions, marks in this view)
                viewInfo.RelatedDrawingObjects = ExtractRelatedDrawingObjects(drawingPart);
            }
            catch
            {
                // If extraction fails, still return basic view info
            }

            return viewInfo;
        }

        /// <summary>
        /// Extract local geometry (simplified 2D projection)
        /// </summary>
        private object ExtractLocalGeometry(Tekla.Structures.Drawing.Part drawingPart, View view)
        {
            try
            {
                // Get model object
                var modelObj = _model.SelectModelObject(drawingPart.ModelIdentifier);

                object localGeometry = null;

                if (modelObj is Beam beam)
                {
                    // Simplified 2D projection (X, Y coordinates)
                    // TODO: Implement proper 3D to 2D transformation when API is available
                    localGeometry = new
                    {
                        part_type = "Beam",
                        start_point_local = new { x = Math.Round(beam.StartPoint.X, 2), y = Math.Round(beam.StartPoint.Y, 2) },
                        end_point_local = new { x = Math.Round(beam.EndPoint.X, 2), y = Math.Round(beam.EndPoint.Y, 2) }
                    };
                }
                else if (modelObj is ContourPlate plate)
                {
                    // Simplified 2D projection
                    var contourPointsLocal = new List<object>();
                    foreach (ContourPoint cp in plate.Contour.ContourPoints)
                    {
                        contourPointsLocal.Add(new
                        {
                            x = Math.Round(cp.X, 2),
                            y = Math.Round(cp.Y, 2)
                        });
                    }

                    localGeometry = new
                    {
                        part_type = "ContourPlate",
                        contour_points_local = contourPointsLocal
                    };
                }

                return localGeometry;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Extract main part flag using GetReportProperty (CRITICAL FIX #6)
        /// </summary>
        private bool ExtractIsMainPart(Tekla.Structures.Model.ModelObject modelObj)
        {
            try
            {
                Hashtable attrs = new Hashtable();
                ArrayList strings = new ArrayList() { "MAIN_PART" };

                modelObj.GetAllReportProperties(strings, new ArrayList(), new ArrayList(), ref attrs);

                if (attrs.ContainsKey("MAIN_PART"))
                {
                    var mainPartValue = attrs["MAIN_PART"];
                    if (mainPartValue != null)
                    {
                        // MAIN_PART could be string "1" or integer 1
                        string valueStr = mainPartValue.ToString();
                        return valueStr == "1" || valueStr.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch
            {
                // If GetReportProperty fails, use fallback logic
            }

            // Fallback: Beams are typically main parts, plates are secondary
            if (modelObj is Beam)
                return true;

            return false;
        }

        /// <summary>
        /// Extract beam properties and populate PartInfo (CRITICAL FIX)
        /// </summary>
        private void ExtractBeamProperties(Beam beam, PartInfo partInfo, bool extractSolidVertices)
        {
            Hashtable attrs = new Hashtable();

            // Static attributes
            ArrayList strings = new ArrayList() {
                "NAME", "MATERIAL", "FINISH",
                "ASSEMBLY_POSITION_CODE", "PROFILE",
                "PART_POS", "ASSEMBLY_POS", "PROFILE_TYPE"
            };

            ArrayList doubles = new ArrayList() {
                "START_X", "START_Y", "START_Z",
                "END_X", "END_Y", "END_Z",
                "TOP_LEVEL_GLOBAL", "BOTTOM_LEVEL_GLOBAL"
            };

            beam.GetAllReportProperties(strings, doubles, new ArrayList(), ref attrs);

            // Populate PartInfo
            partInfo.PartType = "Beam";
            partInfo.Profile = attrs["PROFILE"]?.ToString() ?? beam.Profile.ProfileString;
            partInfo.Material = attrs["MATERIAL"]?.ToString() ?? beam.Material.MaterialString;
            partInfo.Name = attrs["NAME"]?.ToString() ?? "";
            partInfo.Finish = attrs["FINISH"]?.ToString() ?? "";
            partInfo.Class = beam.Class;
            partInfo.Length = Math.Round(GeometryHelper.Distance3D(beam.StartPoint, beam.EndPoint), 2);
            partInfo.PartPos = attrs["PART_POS"]?.ToString() ?? "";
            partInfo.AssemblyPos = attrs["ASSEMBLY_POS"]?.ToString() ?? "";

            // Extract global geometry
            partInfo.GeometryGlobal = ExtractBeamGeometry(beam, extractSolidVertices);
        }

        /// <summary>
        /// Extract beam geometry with chamfer extraction (CRITICAL FIX)
        /// </summary>
        private object ExtractBeamGeometry(Beam beam, bool extractSolidVertices)
        {
            var geometry = new Dictionary<string, object>
            {
                ["part_type_model"] = "Beam",
                ["start_point"] = GeometryHelper.ParsePoint3D(beam.StartPoint),
                ["end_point"] = GeometryHelper.ParsePoint3D(beam.EndPoint)
            };

            // Extract chamfer/offset (CRITICAL FIX)
            if (beam.StartPointOffset.Dx != 0 || beam.StartPointOffset.Dy != 0)
            {
                geometry["chamfer"] = new
                {
                    x = beam.StartPointOffset.Dx,
                    y = beam.StartPointOffset.Dy
                };
            }

            // Extract solid vertices if requested
            if (extractSolidVertices)
            {
                var solid = beam.GetSolid();
                if (solid != null)
                {
                    var vertices = ExtractSolidVertices(solid);
                    geometry["vertices"] = vertices;
                }
            }

            // Calculate bounding box
            var points = new List<Point> { beam.StartPoint, beam.EndPoint };
            geometry["bbox"] = GeometryHelper.CalculateBoundingBox(points);

            return geometry;
        }

        /// <summary>
        /// Extract plate properties and populate PartInfo
        /// </summary>
        private void ExtractPlateProperties(ContourPlate plate, PartInfo partInfo)
        {
            Hashtable attrs = new Hashtable();

            ArrayList strings = new ArrayList() {
                "NAME", "MATERIAL", "FINISH",
                "PART_POS", "ASSEMBLY_POS", "PROFILE"
            };

            ArrayList doubles = new ArrayList() {
                "TOP_LEVEL_GLOBAL", "BOTTOM_LEVEL_GLOBAL"
            };

            plate.GetAllReportProperties(strings, doubles, new ArrayList(), ref attrs);

            // Populate PartInfo
            partInfo.PartType = "ContourPlate";
            partInfo.Profile = attrs["PROFILE"]?.ToString() ?? plate.Profile.ProfileString;
            partInfo.Material = attrs["MATERIAL"]?.ToString() ?? plate.Material.MaterialString;
            partInfo.Name = attrs["NAME"]?.ToString() ?? "";
            partInfo.Finish = attrs["FINISH"]?.ToString() ?? "";
            partInfo.Class = plate.Class;
            partInfo.Length = 0; // Plates don't have length
            partInfo.PartPos = attrs["PART_POS"]?.ToString() ?? "";
            partInfo.AssemblyPos = attrs["ASSEMBLY_POS"]?.ToString() ?? "";

            // Extract global geometry
            partInfo.GeometryGlobal = ExtractPlateGeometry(plate);
        }

        /// <summary>
        /// Extract plate geometry with contour points
        /// </summary>
        private object ExtractPlateGeometry(ContourPlate plate)
        {
            var geometry = new Dictionary<string, object>
            {
                ["part_type_model"] = "ContourPlate"
            };

            // Extract contour points
            var contourPoints = new List<object>();
            foreach (ContourPoint cp in plate.Contour.ContourPoints)
            {
                contourPoints.Add(new
                {
                    x = Math.Round(cp.X, 2),
                    y = Math.Round(cp.Y, 2),
                    z = Math.Round(cp.Z, 2),
                    chamfer = new
                    {
                        x = cp.Chamfer.X,
                        y = cp.Chamfer.Y
                    }
                });
            }
            geometry["contour_points"] = contourPoints;

            return geometry;
        }

        /// <summary>
        /// Extract PolyBeam properties and populate PartInfo
        /// </summary>
        private void ExtractPolyBeamProperties(PolyBeam polyBeam, PartInfo partInfo)
        {
            Hashtable attrs = new Hashtable();

            ArrayList strings = new ArrayList() {
                "NAME", "MATERIAL", "FINISH",
                "PART_POS", "ASSEMBLY_POS", "PROFILE"
            };

            polyBeam.GetAllReportProperties(strings, new ArrayList(), new ArrayList(), ref attrs);

            // Populate PartInfo
            partInfo.PartType = "PolyBeam";
            partInfo.Profile = attrs["PROFILE"]?.ToString() ?? polyBeam.Profile.ProfileString;
            partInfo.Material = attrs["MATERIAL"]?.ToString() ?? polyBeam.Material.MaterialString;
            partInfo.Name = attrs["NAME"]?.ToString() ?? "";
            partInfo.Finish = "";
            partInfo.Class = polyBeam.Class;
            partInfo.Length = 0; // PolyBeams have complex geometry
            partInfo.PartPos = attrs["PART_POS"]?.ToString() ?? "";
            partInfo.AssemblyPos = attrs["ASSEMBLY_POS"]?.ToString() ?? "";

            // Extract global geometry
            partInfo.GeometryGlobal = ExtractPolyBeamGeometry(polyBeam);
        }

        /// <summary>
        /// Extract PolyBeam geometry with contour points
        /// </summary>
        private object ExtractPolyBeamGeometry(PolyBeam polyBeam)
        {
            var geometry = new Dictionary<string, object>
            {
                ["part_type_model"] = "PolyBeam"
            };

            // Extract contour points
            var contourPoints = new List<object>();
            foreach (ContourPoint cp in polyBeam.Contour.ContourPoints)
            {
                contourPoints.Add(GeometryHelper.ParsePoint3D(new Point(cp.X, cp.Y, cp.Z)));
            }
            geometry["contour_points"] = contourPoints;

            return geometry;
        }

        /// <summary>
        /// Extract generic part properties
        /// </summary>
        private object ExtractGenericPartProperties(Tekla.Structures.Model.ModelObject obj)
        {
            return new
            {
                id = obj.Identifier.ID,
                guid = obj.Identifier.GUID.ToString(),
                type = obj.GetType().Name
            };
        }

        /// <summary>
        /// Extract solid vertices using reflection (optional, performance-heavy)
        /// </summary>
        private List<object> ExtractSolidVertices(Solid solid)
        {
            var vertices = new List<object>();

            try
            {
                // Use reflection to access Solid API (Face, Loop types not directly accessible)
                var getFaceList = solid.GetType().GetMethod("GetFaceList");
                if (getFaceList != null)
                {
                    var faceList = getFaceList.Invoke(solid, new object[] { });
                    if (faceList != null)
                    {
                        var faceEnum = faceList as IEnumerator;
                        if (faceEnum != null)
                        {
                            while (faceEnum.MoveNext())
                            {
                                var face = faceEnum.Current;
                                if (face != null)
                                {
                                    // Get contour from face using reflection
                                    var getContour = face.GetType().GetMethod("GetContour");
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
                                                        // Extract X, Y, Z using reflection
                                                        var xProp = pt.GetType().GetProperty("X");
                                                        var yProp = pt.GetType().GetProperty("Y");
                                                        var zProp = pt.GetType().GetProperty("Z");

                                                        if (xProp != null && yProp != null && zProp != null)
                                                        {
                                                            vertices.Add(new
                                                            {
                                                                x = Math.Round(Convert.ToDouble(xProp.GetValue(pt, null)), 2),
                                                                y = Math.Round(Convert.ToDouble(yProp.GetValue(pt, null)), 2),
                                                                z = Math.Round(Convert.ToDouble(zProp.GetValue(pt, null)), 2)
                                                            });

                                                            // Limit to 500 vertices to avoid huge files
                                                            if (vertices.Count >= 500)
                                                                return vertices;
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
            catch
            {
                // Solid vertex extraction failed, return what we have
            }

            return vertices;
        }

        /// <summary>
        /// Extract related DRAWING objects (dimensions, marks, text) in this view
        /// These are view-specific drawing objects that reference this part
        /// </summary>
        private List<string> ExtractRelatedDrawingObjects(DrawingObject drawingObject)
        {
            var relatedObjects = new List<string>();

            try
            {
                var relatedEnum = drawingObject.GetRelatedObjects();
                if (relatedEnum != null)
                {
                    while (relatedEnum.MoveNext())
                    {
                        var relatedObj = relatedEnum.Current;
                        if (relatedObj == null) continue;

                        string typeName = relatedObj.GetType().Name;

                        // Only include DRAWING objects (dimensions, marks, text)
                        // Skip model objects (parts, bolts)
                        if (typeName == "Part" || typeName == "Bolt")
                            continue;

                        string identifier = "";

                        // Try to get a unique identifier
                        try
                        {
                            // For drawing objects, we'll use type:hash for now
                            // Later we'll replace this with proper IDs when we generate them
                            identifier = relatedObj.GetHashCode().ToString();
                        }
                        catch { }

                        relatedObjects.Add($"{typeName}:{identifier}");
                    }
                }
            }
            catch { }

            return relatedObjects;
        }
    }
}
