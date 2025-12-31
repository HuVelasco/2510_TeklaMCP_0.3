using System;
using System.Collections.Generic;
using Tekla.Structures.Drawing;
using TeklaDrawingExtractor.Extraction.DataModels;
using TeklaDrawingExtractor.Utilities;

namespace TeklaDrawingExtractor.Extraction.ObjectExtractors
{
    /// <summary>
    /// Extracts view data and finds parent views for objects
    /// </summary>
    public class ViewExtractor
    {
        /// <summary>
        /// Extract view information
        /// </summary>
        public ViewInfo ExtractView(View view, int viewIndex)
        {
            var viewInfo = new ViewInfo
            {
                ViewId = $"view_{viewIndex}",
                ViewIndex = viewIndex
            };

            try
            {
                // Extract view properties
                try
                {
                    // Try to get name using reflection (may not be available)
                    var nameProp = view.GetType().GetProperty("Name");
                    if (nameProp != null)
                    {
                        var name = nameProp.GetValue(view, null);
                        viewInfo.Name = name != null ? name.ToString() : $"View_{viewIndex}";
                    }
                    else
                    {
                        viewInfo.Name = $"View_{viewIndex}";
                    }

                    // Extract ViewType
                    var viewTypeProp = view.GetType().GetProperty("ViewType");
                    if (viewTypeProp != null)
                    {
                        var viewType = viewTypeProp.GetValue(view, null);
                        if (viewType != null)
                        {
                            viewInfo.ViewType = viewType.ToString();
                        }
                    }
                }
                catch { }

                // Extract coordinate system
                try
                {
                    var coordSystem = view.ViewCoordinateSystem;
                    if (coordSystem != null)
                    {
                        viewInfo.CoordinateSystem3D = new
                        {
                            origin = GeometryHelper.ParsePoint3D(coordSystem.Origin),
                            axis_x = GeometryHelper.ParsePoint3D(coordSystem.AxisX),
                            axis_y = GeometryHelper.ParsePoint3D(coordSystem.AxisY)
                        };
                    }
                }
                catch { }

                // Extract display coordinate system
                try
                {
                    var displayCS = view.DisplayCoordinateSystem;
                    if (displayCS != null)
                    {
                        viewInfo.CoordinateSystem2D = new
                        {
                            origin_x = displayCS.Origin.X,
                            origin_y = displayCS.Origin.Y,
                            origin_z = displayCS.Origin.Z
                        };
                    }
                }
                catch { }

                // Extract view bounds
                try
                {
                    viewInfo.Width = view.Width;
                    viewInfo.Height = view.Height;
                }
                catch { }
            }
            catch { }

            return viewInfo;
        }

        /// <summary>
        /// Find parent view for a drawing object
        /// </summary>
        public string FindParentView(DrawingObject obj, List<View> views)
        {
            try
            {
                for (int i = 0; i < views.Count; i++)
                {
                    var view = views[i];

                    // Check if object belongs to this view
                    var viewObjects = view.GetObjects();
                    while (viewObjects.MoveNext())
                    {
                        var viewObj = viewObjects.Current as DrawingObject;
                        if (viewObj != null)
                        {
                            // Compare by ModelIdentifier or other unique property
                            try
                            {
                                var objModelId = GetModelIdentifier(obj);
                                var viewObjModelId = GetModelIdentifier(viewObj);

                                if (objModelId != null && viewObjModelId != null && objModelId.Equals(viewObjModelId))
                                {
                                    return $"view_{i}";
                                }
                            }
                            catch { }

                            // Also check if it's the same object reference
                            if (ReferenceEquals(obj, viewObj))
                            {
                                return $"view_{i}";
                            }
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Get model identifier from drawing object
        /// </summary>
        private object GetModelIdentifier(DrawingObject obj)
        {
            try
            {
                var idProp = obj.GetType().GetProperty("ModelIdentifier");
                if (idProp != null)
                {
                    return idProp.GetValue(obj, null);
                }
            }
            catch { }

            return null;
        }
    }
}
