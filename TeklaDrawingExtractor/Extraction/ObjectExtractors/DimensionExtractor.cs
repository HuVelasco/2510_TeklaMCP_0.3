using System;
using System.Collections;
using System.Collections.Generic;
using Tekla.Structures.Drawing;
using TeklaDrawingExtractor.Extraction.DataModels;
using TeklaDrawingExtractor.Utilities;

namespace TeklaDrawingExtractor.Extraction.ObjectExtractors
{
    /// <summary>
    /// Extracts dimension data with enhanced information (CRITICAL FIX)
    /// Direction vectors, mm values, prefix/postfix
    /// </summary>
    public class DimensionExtractor
    {
        /// <summary>
        /// Extract straight dimension with enhanced data (CRITICAL FIX #4, #5)
        /// </summary>
        public DimensionInfo ExtractStraightDimension(StraightDimension dimension, string viewId, ref int dimIdCounter)
        {
            dimIdCounter++;
            var dimInfo = new DimensionInfo
            {
                ViewId = viewId,
                DimensionId = $"dim_{dimIdCounter}",
                Type = "StraightDimension"
            };

            try
            {
                // Extract coordinates
                var startPoint = dimension.StartPoint;
                var endPoint = dimension.EndPoint;

                dimInfo.StartPoint = GeometryHelper.ParsePoint(startPoint);
                dimInfo.EndPoint = GeometryHelper.ParsePoint(endPoint);

                // Extract dimension value text
                string valueText = "";
                try
                {
                    var valueProp = dimension.GetType().GetProperty("Value");
                    if (valueProp != null)
                    {
                        var dimensionValue = valueProp.GetValue(dimension, null);
                        if (dimensionValue != null)
                        {
                            var containerElement = dimensionValue as ContainerElement;
                            if (containerElement != null)
                            {
                                valueText = containerElement.GetUnformattedString();
                                dimInfo.ValueText = valueText;
                            }
                        }
                    }

                    // Get prefix and postfix from attributes
                    var attributes = dimension.Attributes as StraightDimension.StraightDimensionAttributes;
                    if (attributes != null)
                    {
                        var prefix = attributes.DimensionValuePrefix as ContainerElement;
                        var postfix = attributes.DimensionValuePostfix as ContainerElement;

                        dimInfo.Prefix = prefix != null ? prefix.GetUnformattedString() : "";
                        dimInfo.Postfix = postfix != null ? postfix.GetUnformattedString() : "";
                    }
                }
                catch { }

                // Convert value to mm (CRITICAL FIX #5)
                dimInfo.ValueMm = DimensionParser.ParseToMm(valueText);

                // If parsing failed, use calculated distance as fallback
                if (dimInfo.ValueMm == 0.0)
                {
                    dimInfo.ValueMm = Math.Round(GeometryHelper.Distance2D(startPoint, endPoint), 2);
                }

                // Calculate direction string and vector (CRITICAL FIX #4)
                dimInfo.DirectionString = GeometryHelper.DetermineDirection(startPoint, endPoint);

                // Calculate direction vector (normalized)
                var dirVector = DimensionParser.CalculateDirectionVector(
                    startPoint.X, startPoint.Y, 0,
                    endPoint.X, endPoint.Y, 0);

                dimInfo.DirectionVector = new DirectionVector
                {
                    X = dirVector.x,
                    Y = dirVector.y,
                    Z = dirVector.z
                };

                // Distance to line
                dimInfo.DistanceToLine = Math.Round(dimension.Distance, 2);

                // Extract related objects
                dimInfo.RelatedModelObjects = ExtractRelatedModelObjects(dimension);
                dimInfo.RelatedDrawingObjects = ExtractRelatedDrawingObjects(dimension);
            }
            catch { }

            return dimInfo;
        }

        /// <summary>
        /// Extract dimension set with child dimensions
        /// Note: Returns first child dimension (dimension sets are treated as individual dimensions)
        /// </summary>
        public DimensionInfo ExtractDimensionSet(StraightDimensionSet dimensionSet, string viewId, ref int dimIdCounter)
        {
            try
            {
                // Extract first child dimension from the set
                var dimSetObjects = dimensionSet.GetObjects();
                while (dimSetObjects.MoveNext())
                {
                    var childDim = dimSetObjects.Current as StraightDimension;
                    if (childDim != null)
                    {
                        // Return first dimension (simplified for ML training)
                        return ExtractStraightDimension(childDim, viewId, ref dimIdCounter);
                    }
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Extract radius dimension
        /// </summary>
        public DimensionInfo ExtractRadiusDimension(RadiusDimension dimension, string viewId, ref int dimIdCounter)
        {
            dimIdCounter++;
            var dimInfo = new DimensionInfo
            {
                ViewId = viewId,
                DimensionId = $"dim_{dimIdCounter}",
                Type = "RadiusDimension"
            };

            try
            {
                // Use GetPoints() method via reflection
                var getPoints = dimension.GetType().GetMethod("GetPoints");
                if (getPoints != null)
                {
                    var points = getPoints.Invoke(dimension, new object[] { });
                    if (points != null)
                    {
                        var pointList = points as ArrayList;
                        if (pointList != null && pointList.Count > 0)
                        {
                            // Store first and last points as start/end
                            if (pointList.Count >= 2)
                            {
                                dimInfo.StartPoint = GeometryHelper.ParsePoint(pointList[0]);
                                dimInfo.EndPoint = GeometryHelper.ParsePoint(pointList[pointList.Count - 1]);
                            }
                        }
                    }
                }

                // Extract related objects
                dimInfo.RelatedModelObjects = ExtractRelatedModelObjects(dimension);
                dimInfo.RelatedDrawingObjects = ExtractRelatedDrawingObjects(dimension);
            }
            catch { }

            return dimInfo;
        }

        /// <summary>
        /// Extract angle dimension
        /// </summary>
        public DimensionInfo ExtractAngleDimension(AngleDimension dimension, string viewId, ref int dimIdCounter)
        {
            dimIdCounter++;
            var dimInfo = new DimensionInfo
            {
                ViewId = viewId,
                DimensionId = $"dim_{dimIdCounter}",
                Type = "AngleDimension"
            };

            try
            {
                // Use GetPoints() method via reflection
                var getPoints = dimension.GetType().GetMethod("GetPoints");
                if (getPoints != null)
                {
                    var points = getPoints.Invoke(dimension, new object[] { });
                    if (points != null)
                    {
                        var pointList = points as ArrayList;
                        if (pointList != null && pointList.Count > 0)
                        {
                            // Store first and last points as start/end
                            if (pointList.Count >= 2)
                            {
                                dimInfo.StartPoint = GeometryHelper.ParsePoint(pointList[0]);
                                dimInfo.EndPoint = GeometryHelper.ParsePoint(pointList[pointList.Count - 1]);
                            }
                        }
                    }
                }

                // Extract related objects
                dimInfo.RelatedModelObjects = ExtractRelatedModelObjects(dimension);
                dimInfo.RelatedDrawingObjects = ExtractRelatedDrawingObjects(dimension);
            }
            catch { }

            return dimInfo;
        }

        /// <summary>
        /// Extract related MODEL objects (parts, bolts that this dimension measures)
        /// </summary>
        private List<string> ExtractRelatedModelObjects(DrawingObject drawingObject)
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

                        // Only include MODEL objects (parts, bolts)
                        if (typeName != "Part" && typeName != "Bolt")
                            continue;

                        // Get ModelIdentifier
                        try
                        {
                            var idProp = relatedObj.GetType().GetProperty("ModelIdentifier");
                            if (idProp != null)
                            {
                                var idValue = idProp.GetValue(relatedObj, null);
                                if (idValue != null)
                                {
                                    var idType = idValue.GetType();
                                    var idNumberProp = idType.GetProperty("ID");
                                    if (idNumberProp != null)
                                    {
                                        var idNumber = idNumberProp.GetValue(idValue, null);
                                        if (idNumber != null)
                                        {
                                            relatedObjects.Add($"{typeName}:{idNumber}");
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return relatedObjects;
        }

        /// <summary>
        /// Extract related DRAWING objects (marks, text near this dimension)
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

                        // Skip MODEL objects
                        if (typeName == "Part" || typeName == "Bolt")
                            continue;

                        string identifier = relatedObj.GetHashCode().ToString();
                        relatedObjects.Add($"{typeName}:{identifier}");
                    }
                }
            }
            catch { }

            return relatedObjects;
        }
    }
}
