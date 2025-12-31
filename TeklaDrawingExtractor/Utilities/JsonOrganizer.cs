using System.Collections.Generic;
using System.Linq;
using TeklaDrawingExtractor.Extraction.DataModels;

namespace TeklaDrawingExtractor.Utilities
{
    /// <summary>
    /// Organizes extracted data into hierarchical or flat JSON structure
    /// RESTRUCTURED: Works with new data model (Parts with ViewData, etc.)
    /// </summary>
    public class JsonOrganizer
    {
        /// <summary>
        /// Organize data into hierarchical structure (recommended for viewing)
        /// </summary>
        public object OrganizeHierarchical(DrawingData data)
        {
            return new
            {
                drawing_info = new
                {
                    mark = data.DrawingMark,
                    type = data.DrawingType,
                    timestamp = data.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss")
                },
                views = data.Views,
                model_objects = new
                {
                    parts = data.Parts,
                    bolt_groups = data.BoltGroups
                },
                drawing_objects = new
                {
                    dimensions = data.Dimensions,
                    marks = data.Marks,
                    text_objects = data.TextObjects
                },
                statistics = new
                {
                    views_count = data.Views.Count,
                    parts_count = data.Parts.Count,
                    main_parts_count = data.Parts.Count(p => p.IsMainPart),
                    secondary_parts_count = data.Parts.Count(p => !p.IsMainPart),
                    bolt_groups_count = data.BoltGroups.Count,
                    dimensions_count = data.Dimensions.Count,
                    marks_count = data.Marks.Count,
                    text_objects_count = data.TextObjects.Count,
                    model_objects_count = data.ModelObjectsCount
                }
            };
        }

        /// <summary>
        /// Organize data into flat structure (better for ML processing)
        /// </summary>
        public object OrganizeFlat(DrawingData data)
        {
            var allObjects = new List<object>();
            allObjects.AddRange(data.Views);
            allObjects.AddRange(data.Parts);
            allObjects.AddRange(data.BoltGroups);
            allObjects.AddRange(data.Dimensions);
            allObjects.AddRange(data.Marks);
            allObjects.AddRange(data.TextObjects);

            return new
            {
                drawing_mark = data.DrawingMark,
                drawing_type = data.DrawingType,
                timestamp = data.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss"),
                objects = allObjects
            };
        }
    }
}
