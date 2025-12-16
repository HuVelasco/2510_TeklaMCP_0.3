// TEST: Verify that transforming global coordinates == extracting in view plane
// Based on TeklaDrawingExamples.cs lines 814-903

using System;
using System.Collections.Generic;
using Tekla.Structures.Model;
using Tekla.Structures.Drawing;
using Tekla.Structures.Geometry3d;
using Tekla.Structures.Solid;
using TSM = Tekla.Structures.Model;
using TSD = Tekla.Structures.Drawing;

namespace CoordinateTransformationTest
{
    class Program
    {
        static void Main()
        {
            try
            {
                var model = new Model();
                var drawingHandler = new DrawingHandler();
                var drawing = drawingHandler.GetActiveDrawing();

                if (drawing == null)
                {
                    Console.WriteLine("ERROR: No active drawing");
                    Console.ReadLine();
                    return;
                }

                var sheet = drawing.GetSheet();
                var objects = sheet.GetAllObjects();

                // Find first Part
                TSD.Part drawingPart = null;
                TSD.View partView = null;

                while (objects.MoveNext())
                {
                    var obj = objects.Current;
                    var part = obj as TSD.Part;
                    if (part != null && part.ModelIdentifier != null)
                    {
                        drawingPart = part;
                        partView = part.GetView() as TSD.View;
                        break;
                    }
                }

                if (drawingPart == null || partView == null)
                {
                    Console.WriteLine("ERROR: No Part found in drawing");
                    Console.ReadLine();
                    return;
                }

                Console.WriteLine("=== COORDINATE TRANSFORMATION TEST ===");
                Console.WriteLine("Testing Part ID: " + drawingPart.ModelIdentifier.ToString());

                // Get MODEL Part
                var modelObj = model.SelectModelObject(drawingPart.ModelIdentifier);
                var modelPart = modelObj as TSM.Part;

                if (modelPart == null)
                {
                    Console.WriteLine("ERROR: Could not get MODEL Part");
                    Console.ReadLine();
                    return;
                }

                // APPROACH 1: Extract vertices in GLOBAL coordinates
                Console.WriteLine("\nAPPROACH 1: Extracting in GLOBAL coordinates...");
                var globalVertices = new List<Point>();
                var solid1 = modelPart.GetSolid();
                var faceEnum1 = solid1.GetFaceEnumerator();
                while (faceEnum1.MoveNext())
                {
                    var face = faceEnum1.Current as Face;
                    if (face != null)
                    {
                        var loopEnum = face.GetLoopEnumerator();
                        while (loopEnum.MoveNext())
                        {
                            var loop = loopEnum.Current as Loop;
                            if (loop != null)
                            {
                                var vertexEnum = loop.GetVertexEnumerator() as VertexEnumerator;
                                while (vertexEnum.MoveNext())
                                {
                                    var vertex = vertexEnum.Current as Point;
                                    if (vertex != null)
                                    {
                                        globalVertices.Add(new Point(vertex));
                                    }
                                }
                            }
                        }
                    }
                }
                Console.WriteLine("Global vertices extracted: " + globalVertices.Count);
                if (globalVertices.Count > 0)
                {
                    Console.WriteLine("Sample global vertex [0]: (" +
                        globalVertices[0].X.ToString("F2") + ", " +
                        globalVertices[0].Y.ToString("F2") + ", " +
                        globalVertices[0].Z.ToString("F2") + ")");
                }

                // APPROACH 2: Extract vertices in VIEW coordinates using WorkPlane transformation
                Console.WriteLine("\nAPPROACH 2: Extracting in VIEW coordinates (WorkPlane method)...");
                var viewVertices = new List<Point>();

                // Store View Plane (from line 820)
                TSM.TransformationPlane viewPlane = new TSM.TransformationPlane(partView.ViewCoordinateSystem);

                // Store Model work plane (from line 823)
                TSM.WorkPlaneHandler workPlaneHandler = model.GetWorkPlaneHandler();
                TSM.TransformationPlane currentPlane = workPlaneHandler.GetCurrentTransformationPlane();

                // Clear workplane to new blank global (from line 826)
                workPlaneHandler.SetCurrentTransformationPlane(new TSM.TransformationPlane());

                // Set model everything to the view (from line 828)
                workPlaneHandler.SetCurrentTransformationPlane(viewPlane);

                // NOW extract vertices - they should be in VIEW coordinates
                var solid2 = modelPart.GetSolid();
                var faceEnum2 = solid2.GetFaceEnumerator();
                while (faceEnum2.MoveNext())
                {
                    var face = faceEnum2.Current as Face;
                    if (face != null)
                    {
                        var loopEnum = face.GetLoopEnumerator();
                        while (loopEnum.MoveNext())
                        {
                            var loop = loopEnum.Current as Loop;
                            if (loop != null)
                            {
                                var vertexEnum = loop.GetVertexEnumerator() as VertexEnumerator;
                                while (vertexEnum.MoveNext())
                                {
                                    var vertex = vertexEnum.Current as Point;
                                    if (vertex != null)
                                    {
                                        viewVertices.Add(new Point(vertex));
                                    }
                                }
                            }
                        }
                    }
                }

                // Return the original Model Plane (from line 900)
                workPlaneHandler.SetCurrentTransformationPlane(currentPlane);

                Console.WriteLine("View vertices extracted: " + viewVertices.Count);
                if (viewVertices.Count > 0)
                {
                    Console.WriteLine("Sample view vertex [0]: (" +
                        viewVertices[0].X.ToString("F2") + ", " +
                        viewVertices[0].Y.ToString("F2") + ", " +
                        viewVertices[0].Z.ToString("F2") + ")");
                }

                // COMPARISON
                Console.WriteLine("\n=== COMPARISON ===");
                if (globalVertices.Count == viewVertices.Count)
                {
                    Console.WriteLine("✓ Same vertex count: " + globalVertices.Count);

                    // Compare first vertex
                    bool same = Math.Abs(globalVertices[0].X - viewVertices[0].X) < 0.01 &&
                                Math.Abs(globalVertices[0].Y - viewVertices[0].Y) < 0.01 &&
                                Math.Abs(globalVertices[0].Z - viewVertices[0].Z) < 0.01;

                    if (same)
                    {
                        Console.WriteLine("✓ Coordinates are SAME (global == view)");
                        Console.WriteLine("  This means vertices are already in VIEW local coordinates!");
                    }
                    else
                    {
                        Console.WriteLine("✓ Coordinates are DIFFERENT (global != view)");
                        Console.WriteLine("  Global vertex [0]: (" +
                            globalVertices[0].X.ToString("F2") + ", " +
                            globalVertices[0].Y.ToString("F2") + ", " +
                            globalVertices[0].Z.ToString("F2") + ")");
                        Console.WriteLine("  View vertex [0]:   (" +
                            viewVertices[0].X.ToString("F2") + ", " +
                            viewVertices[0].Y.ToString("F2") + ", " +
                            viewVertices[0].Z.ToString("F2") + ")");
                        Console.WriteLine("  Delta: (" +
                            (globalVertices[0].X - viewVertices[0].X).ToString("F2") + ", " +
                            (globalVertices[0].Y - viewVertices[0].Y).ToString("F2") + ", " +
                            (globalVertices[0].Z - viewVertices[0].Z).ToString("F2") + ")");
                    }
                }
                else
                {
                    Console.WriteLine("✗ Different vertex count!");
                }

                Console.WriteLine("\nPress Enter to exit...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.ReadLine();
            }
        }
    }
}
