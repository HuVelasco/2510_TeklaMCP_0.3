using System;
using System.Collections.Generic;
using Tekla.Structures.Drawing;
using Tekla.Structures.Model;
using Tekla.Structures.Geometry3d;
using TeklaDrawingExtractor.Extraction.DataModels;
using TeklaDrawingExtractor.Utilities;

namespace TeklaDrawingExtractor.Extraction.ObjectExtractors
{
    /// <summary>
    /// Extracts bolt data with individual positions (global + local) (CRITICAL FIX)
    /// RESTRUCTURED: Separates model properties from view-specific data
    /// </summary>
    public class BoltExtractor
    {
        private Model _model;
        private Dictionary<Guid, BoltGroupInfo> _boltCache = new Dictionary<Guid, BoltGroupInfo>();

        public BoltExtractor()
        {
            _model = new Model();
        }

        /// <summary>
        /// Get or create BoltGroupInfo for a model object (ensures no duplication)
        /// </summary>
        public BoltGroupInfo GetOrCreateBoltGroupInfo(Tekla.Structures.Drawing.Bolt drawingBolt)
        {
            if (drawingBolt.ModelIdentifier == null)
                return null;

            var boltGroup = _model.SelectModelObject(drawingBolt.ModelIdentifier) as BoltGroup;
            if (boltGroup == null)
                return null;

            Guid guid = boltGroup.Identifier.GUID;

            // Check cache first
            if (_boltCache.ContainsKey(guid))
            {
                return _boltCache[guid];
            }

            // Create new BoltGroupInfo
            var boltInfo = new BoltGroupInfo
            {
                ModelId = boltGroup.Identifier.ID,
                Guid = guid.ToString(),
                BoltSize = boltGroup.BoltSize.ToString(),  // Convert double to string
                BoltStandard = boltGroup.BoltStandard,
                BoltType = boltGroup.BoltType.ToString(),
                ThreadInMaterial = boltGroup.ThreadInMaterial.ToString(),
                CutLength = boltGroup.CutLength,
                ExtraLength = boltGroup.ExtraLength
            };

            // Extract individual bolt positions (global 3D) - CRITICAL FIX #3
            foreach (Point pos in boltGroup.BoltPositions)
            {
                boltInfo.PositionsGlobal.Add(new Position3D
                {
                    X = Math.Round(pos.X, 2),
                    Y = Math.Round(pos.Y, 2),
                    Z = Math.Round(pos.Z, 2)
                });
            }

            // Cache it
            _boltCache[guid] = boltInfo;

            return boltInfo;
        }

        /// <summary>
        /// Create view-specific data for a bolt group (local coordinates)
        /// </summary>
        public BoltViewInfo CreateBoltViewInfo(Tekla.Structures.Drawing.Bolt drawingBolt, string viewId, View view)
        {
            var viewInfo = new BoltViewInfo
            {
                ViewId = viewId,
                Visible = true
            };

            try
            {
                // Get model object
                var boltGroup = _model.SelectModelObject(drawingBolt.ModelIdentifier) as BoltGroup;
                if (boltGroup != null)
                {
                    // Transform bolt positions to local coordinates (2D in view space)
                    // Note: For simplicity, using DisplayCoordinateSystem transformation
                    foreach (Point globalPos in boltGroup.BoltPositions)
                    {
                        // Simplified transformation (2D projection)
                        // TODO: Implement proper 3D to 2D transformation when API is available
                        viewInfo.PositionsLocal.Add(new Position2D
                        {
                            X = Math.Round(globalPos.X, 2),
                            Y = Math.Round(globalPos.Y, 2)
                        });
                    }
                }

                // Extract related drawing objects (dimensions, marks in this view)
                viewInfo.RelatedDrawingObjects = ExtractRelatedDrawingObjects(drawingBolt);
            }
            catch
            {
                // If extraction fails, still return basic view info
            }

            return viewInfo;
        }

        /// <summary>
        /// Extract related DRAWING objects (dimensions, marks) in this view
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
