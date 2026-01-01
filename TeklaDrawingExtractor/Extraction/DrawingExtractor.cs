using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Tekla.Structures.Drawing;
using TSDModel = Tekla.Structures.Model.Model;
using Newtonsoft.Json;
using TeklaDrawingExtractor.Extraction.DataModels;
using TeklaDrawingExtractor.Extraction.ObjectExtractors;
using TeklaDrawingExtractor.Utilities;

namespace TeklaDrawingExtractor.Extraction
{
    /// <summary>
    /// Main extraction engine for Tekla shop drawings
    /// </summary>
    public class DrawingExtractor
    {
        private ExtractionConfig _config;
        private IProgress<ExtractionProgress> _progress;
        private Logger _logger;

        private PartExtractor _partExtractor;
        private DimensionExtractor _dimensionExtractor;
        private BoltExtractor _boltExtractor;
        private ViewExtractor _viewExtractor;
        private JsonOrganizer _jsonOrganizer;

        public DrawingExtractor()
        {
            _partExtractor = new PartExtractor();
            _dimensionExtractor = new DimensionExtractor();
            _boltExtractor = new BoltExtractor();
            _viewExtractor = new ViewExtractor();
            _jsonOrganizer = new JsonOrganizer();
        }

        /// <summary>
        /// Main extraction method
        /// </summary>
        public async System.Threading.Tasks.Task<ExtractionResult> ExtractAsync(
            ExtractionConfig config,
            IProgress<ExtractionProgress> progress,
            Logger logger)
        {
            // Run extraction on background thread to prevent UI freezing
            return await System.Threading.Tasks.Task.Run(() =>
            {
                _config = config;
                _progress = progress;
                _logger = logger;

                var result = new ExtractionResult();
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    _logger.Info("Starting extraction...");

                    // Connect to Tekla
                    var model = new TSDModel();
                    if (!model.GetConnectionStatus())
                    {
                        _logger.Error("Not connected to Tekla Structures");
                        result.Success = false;
                        result.Errors.Add("Not connected to Tekla Structures");
                        return result;
                    }

                    _logger.Success("Connected to Tekla Structures");

                    // Get drawing handler
                    var drawingHandler = new DrawingHandler();

                    // Get drawings from selection or filters
                    var drawingsToProcess = GetDrawingsToProcess(drawingHandler);

                    _logger.Info($"Found {drawingsToProcess.Count} drawings to process");

                    // Process each drawing
                    int drawingIndex = 0;
                    foreach (var drawing in drawingsToProcess)
                    {
                        drawingIndex++;

                        try
                        {
                            _logger.Info($"Processing drawing [{drawing.Mark}] ({drawingIndex}/{drawingsToProcess.Count})");

                            var drawingData = ExtractDrawing(drawing, drawingIndex, drawingsToProcess.Count, stopwatch);

                            if (drawingData != null)
                            {
                                result.DrawingDataList.Add(drawingData);
                                result.TotalObjectsExtracted += drawingData.ModelObjectsCount +
                                                               drawingData.DimensionsCount;

                                _logger.Success($"Drawing [{drawing.Mark}]: {drawingData.ModelObjectsCount} model objects, " +
                                              $"{drawingData.DimensionsCount} dimensions extracted");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Failed to extract drawing [{drawing.Mark}]", ex);
                            result.Errors.Add($"Drawing [{drawing.Mark}]: {ex.Message}");
                        }
                    }

                    // Save results
                    if (result.DrawingDataList.Count > 0)
                    {
                        result.OutputFilePath = SaveResults(result.DrawingDataList);
                        _logger.Success($"Saved to: {result.OutputFilePath}");
                    }

                    result.Success = true;
                    _logger.Success($"Extraction complete! {result.TotalObjectsExtracted} objects extracted");
                }
                catch (Exception ex)
                {
                    _logger.Error("Extraction failed", ex);
                    result.Success = false;
                    result.Errors.Add(ex.Message);
                }

                return result;
            });
        }

        /// <summary>
        /// Get drawings to process from Document Manager selection
        /// </summary>
        private List<Drawing> GetDrawingsToProcess(DrawingHandler drawingHandler)
        {
            var drawingsToProcess = new List<Drawing>();

            try
            {
                // Try to get selected drawings from Document Manager
                _logger.Info("Checking for selected drawings in Document Manager...");

                var drawingSelector = drawingHandler.GetDrawingSelector();
                if (drawingSelector != null)
                {
                    var selectedEnum = drawingSelector.GetSelected();
                    if (selectedEnum != null)
                    {
                        int selectedCount = 0;
                        while (selectedEnum.MoveNext())
                        {
                            var drawing = selectedEnum.Current as Drawing;
                            if (drawing != null)
                            {
                                drawingsToProcess.Add(drawing);
                                selectedCount++;

                                if (_config.MaxDrawings > 0 && drawingsToProcess.Count >= _config.MaxDrawings)
                                    break;
                            }
                        }

                        if (selectedCount > 0)
                        {
                            _logger.Success($"Found {selectedCount} selected drawings in Document Manager");
                            return drawingsToProcess;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Could not get selected drawings: {ex.Message}");
            }

            // If no selection, process all drawings
            _logger.Info("No selection found, processing all drawings");
            var allDrawings = drawingHandler.GetDrawings();
            while (allDrawings.MoveNext())
            {
                var drawing = allDrawings.Current as Drawing;
                if (drawing != null)
                {
                    drawingsToProcess.Add(drawing);

                    // Respect max drawings limit
                    if (_config.MaxDrawings > 0 && drawingsToProcess.Count >= _config.MaxDrawings)
                        break;
                }
            }

            return drawingsToProcess;
        }

        /// <summary>
        /// Extract single drawing with restructured approach (no model object duplication)
        /// </summary>
        private DrawingData ExtractDrawing(Drawing drawing, int currentIndex, int totalCount, Stopwatch stopwatch)
        {
            var drawingData = new DrawingData
            {
                DrawingMark = drawing.Mark,
                DrawingType = drawing.GetType().Name,
                Timestamp = DateTime.Now
            };

            var sheet = drawing.GetSheet();
            if (sheet == null)
                return drawingData;

            // STEP 1: Extract all views metadata
            var views = new List<View>();
            var allSheetObjects = sheet.GetAllObjects();
            while (allSheetObjects.MoveNext())
            {
                var view = allSheetObjects.Current as View;
                if (view != null)
                {
                    views.Add(view);
                    _logger.Info($"  Found view: {view.GetType().Name}");
                }
            }

            _logger.Info($"  Extracting {views.Count} views...");
            for (int i = 0; i < views.Count; i++)
            {
                var viewInfo = _viewExtractor.ExtractView(views[i], i);
                drawingData.Views.Add(viewInfo);
            }

            // STEP 2: Process each view and extract objects
            _logger.Info($"  Processing objects in {views.Count} views...");
            int objectCount = 0;
            int dimIdCounter = 0;
            int markIdCounter = 0;
            int textIdCounter = 0;

            for (int viewIndex = 0; viewIndex < views.Count; viewIndex++)
            {
                var view = views[viewIndex];
                string viewId = $"view_{viewIndex}";

                var viewObjects = view.GetObjects();
                while (viewObjects.MoveNext())
                {
                    var obj = viewObjects.Current;
                    if (obj == null) continue;

                    objectCount++;

                    // Report progress periodically
                    if (objectCount % 10 == 0)
                    {
                        ReportProgress(currentIndex, totalCount, drawingData.ModelObjectsCount,
                                      drawing.Mark, viewId, stopwatch);
                    }

                    try
                    {
                        // MODEL OBJECTS: Extract once, add view-specific data
                        if (obj is Tekla.Structures.Drawing.Part drawingPart)
                        {
                            ProcessPart(drawingPart, viewId, view, drawingData);
                        }
                        else if (obj is Tekla.Structures.Drawing.Bolt drawingBolt)
                        {
                            ProcessBolt(drawingBolt, viewId, view, drawingData);
                        }
                        // DRAWING OBJECTS: View-specific
                        else if (obj is StraightDimensionSet dimensionSet)
                        {
                            var dimInfo = _dimensionExtractor.ExtractDimensionSet(dimensionSet, viewId, ref dimIdCounter);
                            if (dimInfo != null)
                                drawingData.Dimensions.Add(dimInfo);
                        }
                        else if (obj is StraightDimension straightDim)
                        {
                            var dimInfo = _dimensionExtractor.ExtractStraightDimension(straightDim, viewId, ref dimIdCounter);
                            if (dimInfo != null)
                                drawingData.Dimensions.Add(dimInfo);
                        }
                        else if (obj is RadiusDimension radiusDim)
                        {
                            var dimInfo = _dimensionExtractor.ExtractRadiusDimension(radiusDim, viewId, ref dimIdCounter);
                            if (dimInfo != null)
                                drawingData.Dimensions.Add(dimInfo);
                        }
                        else if (obj is AngleDimension angleDim)
                        {
                            var dimInfo = _dimensionExtractor.ExtractAngleDimension(angleDim, viewId, ref dimIdCounter);
                            if (dimInfo != null)
                                drawingData.Dimensions.Add(dimInfo);
                        }
                        else if (obj is Text textObj)
                        {
                            var textInfo = ExtractText(textObj, viewId, ref textIdCounter);
                            drawingData.TextObjects.Add(textInfo);
                        }
                        else if (obj is Mark mark)
                        {
                            var markInfo = ExtractMark(mark, viewId, ref markIdCounter);
                            drawingData.Marks.Add(markInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_config.IncludeDebugInfo)
                        {
                            _logger.Warning($"Failed to extract object in {viewId}: {ex.Message}");
                        }
                    }
                }
            }

            // Log extraction summary
            _logger.Info($"  Extraction complete: {objectCount} total objects processed");
            _logger.Info($"    Views: {drawingData.Views.Count}");
            _logger.Info($"    Parts: {drawingData.Parts.Count}");
            _logger.Info($"    Bolt groups: {drawingData.BoltGroups.Count}");
            _logger.Info($"    Dimensions: {drawingData.Dimensions.Count}");
            _logger.Info($"    Marks: {drawingData.Marks.Count}");
            _logger.Info($"    Text: {drawingData.TextObjects.Count}");

            return drawingData;
        }

        /// <summary>
        /// Process part: get or create PartInfo, add view-specific data
        /// </summary>
        private void ProcessPart(Tekla.Structures.Drawing.Part drawingPart, string viewId, View view, DrawingData drawingData)
        {
            // Get or create PartInfo (uses cache, no duplication)
            var partInfo = _partExtractor.GetOrCreatePartInfo(drawingPart, _config.ExtractSolidVertices);
            if (partInfo == null)
                return;

            // Check if already in list
            if (!drawingData.Parts.Contains(partInfo))
            {
                drawingData.Parts.Add(partInfo);
            }

            // Add view-specific data
            var viewData = _partExtractor.CreatePartViewInfo(drawingPart, viewId, view);
            partInfo.ViewData.Add(viewData);
        }

        /// <summary>
        /// Process bolt: get or create BoltGroupInfo, add view-specific data
        /// </summary>
        private void ProcessBolt(Tekla.Structures.Drawing.Bolt drawingBolt, string viewId, View view, DrawingData drawingData)
        {
            // Get or create BoltGroupInfo (uses cache, no duplication)
            var boltInfo = _boltExtractor.GetOrCreateBoltGroupInfo(drawingBolt);
            if (boltInfo == null)
                return;

            // Check if already in list
            if (!drawingData.BoltGroups.Contains(boltInfo))
            {
                drawingData.BoltGroups.Add(boltInfo);
            }

            // Add view-specific data
            var viewData = _boltExtractor.CreateBoltViewInfo(drawingBolt, viewId, view);
            boltInfo.ViewData.Add(viewData);
        }

        /// <summary>
        /// Extract text object with ViewId and unique ID
        /// </summary>
        private TextInfo ExtractText(Text textObj, string viewId, ref int idCounter)
        {
            idCounter++;
            return new TextInfo
            {
                ViewId = viewId,
                TextId = $"text_{idCounter}",
                Text = textObj.TextString,
                InsertionPoint = GeometryHelper.ParsePoint(textObj.InsertionPoint)
            };
        }

        /// <summary>
        /// Extract mark object with ViewId and unique ID
        /// </summary>
        private MarkInfo ExtractMark(Mark mark, string viewId, ref int idCounter)
        {
            idCounter++;
            var markInfo = new MarkInfo
            {
                ViewId = viewId,
                MarkId = $"mark_{idCounter}",
                MarkType = mark.GetType().Name,
                InsertionPoint = GeometryHelper.ParsePoint(mark.InsertionPoint)
            };

            try
            {
                // Try to get model identifier
                var modelIdProp = mark.GetType().GetProperty("ModelIdentifier");
                if (modelIdProp != null)
                {
                    var modelId = modelIdProp.GetValue(mark, null);
                    if (modelId != null)
                    {
                        var idProp = modelId.GetType().GetProperty("ID");
                        if (idProp != null)
                        {
                            var id = idProp.GetValue(modelId, null);
                            if (id != null)
                            {
                                markInfo.ModelIdentifier = Convert.ToInt32(id);
                            }
                        }
                    }
                }

                // Try to get mark text
                try
                {
                    var textProp = mark.GetType().GetProperty("TextString");
                    if (textProp != null)
                    {
                        var text = textProp.GetValue(mark, null);
                        if (text != null)
                        {
                            markInfo.Text = text.ToString();
                        }
                    }
                }
                catch { }
            }
            catch { }

            return markInfo;
        }

        /// <summary>
        /// Save extraction results to file
        /// </summary>
        private string SaveResults(List<DrawingData> drawingDataList)
        {
            // Create output directory if it doesn't exist
            if (!Directory.Exists(_config.OutputDirectory))
            {
                Directory.CreateDirectory(_config.OutputDirectory);
                _logger.Info($"Created output directory: {_config.OutputDirectory}");
            }

            // Generate filename
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string extension = _config.UseJsonL ? "jsonl" : "json";
            string filename = $"extraction_{timestamp}.{extension}";
            string filepath = Path.Combine(_config.OutputDirectory, filename);

            // Serialize settings
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = _config.PrettyFormat ? Formatting.Indented : Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include  // Include ViewIndex=0
            };

            if (_config.UseJsonL)
            {
                // JSONL format: one JSON object per line
                using (var writer = new StreamWriter(filepath))
                {
                    foreach (var drawingData in drawingDataList)
                    {
                        var json = _config.UseHierarchicalJson
                            ? _jsonOrganizer.OrganizeHierarchical(drawingData)
                            : _jsonOrganizer.OrganizeFlat(drawingData);

                        string line = JsonConvert.SerializeObject(json, settings);
                        writer.WriteLine(line);
                    }
                }
            }
            else
            {
                // Regular JSON: array of objects
                var allData = new List<object>();
                foreach (var drawingData in drawingDataList)
                {
                    var json = _config.UseHierarchicalJson
                        ? _jsonOrganizer.OrganizeHierarchical(drawingData)
                        : _jsonOrganizer.OrganizeFlat(drawingData);

                    allData.Add(json);
                }

                string jsonContent = JsonConvert.SerializeObject(allData, settings);
                File.WriteAllText(filepath, jsonContent);
            }

            return filepath;
        }

        /// <summary>
        /// Report progress
        /// </summary>
        private void ReportProgress(int currentDrawing, int totalDrawings, int objectsExtracted,
                                   string drawingMark, string viewName, Stopwatch stopwatch)
        {
            var elapsed = stopwatch.Elapsed;
            var estimated = TimeSpan.Zero;

            if (currentDrawing > 0)
            {
                double avgPerDrawing = elapsed.TotalSeconds / currentDrawing;
                double remaining = avgPerDrawing * (totalDrawings - currentDrawing);
                estimated = TimeSpan.FromSeconds(remaining);
            }

            _progress?.Report(new ExtractionProgress
            {
                DrawingsProcessed = currentDrawing,
                TotalDrawings = totalDrawings,
                ObjectsExtracted = objectsExtracted,
                CurrentDrawing = drawingMark,
                CurrentView = viewName,
                Elapsed = elapsed,
                EstimatedRemaining = estimated,
                Message = $"Processing [{drawingMark}] - {viewName}"
            });
        }
    }
}
