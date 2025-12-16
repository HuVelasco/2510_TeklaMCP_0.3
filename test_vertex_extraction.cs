// TEST: Vertex extraction from MODEL Part solid
// Based on TeklaDrawingExamples.cs lines 814-903 (GetSolidPointsFromPart)

using System;
using System.Collections;
using System.Collections.Generic;
using Tekla.Structures.Model;
using Tekla.Structures.Drawing;
using Tekla.Structures.Geometry3d;
using Tekla.Structures.Solid;

namespace VertexExtractionTest
{
    class Program
    {
        static void Main()
        {
            try
            {
                var model = new Model();
                if (!model.GetConnectionStatus())
                {
                    Console.WriteLine("ERROR: Tekla not connected");
                    return;
                }

                var drawingHandler = new DrawingHandler();
                if (!drawingHandler.GetConnectionStatus())
                {
                    Console.WriteLine("ERROR: Drawing handler not connected");
                    return;
                }

                var drawing = drawingHandler.GetActiveDrawing();
                if (drawing == null)
                {
                    Console.WriteLine("ERROR: No active drawing");
                    return;
                }

                // Test with first drawing
                int partCount = 0;
                int vertexCount = 0;
                int maxPartsToTest = 5; // Start with just 5 parts to test performance

                var sheet = drawing.GetSheet();
                var objects = sheet.GetAllObjects();

                Console.WriteLine("Starting vertex extraction test...");
                var startTime = DateTime.Now;

                while (objects.MoveNext() && partCount < maxPartsToTest)
                {
                    var obj = objects.Current;
                    var drawingPart = obj as Tekla.Structures.Drawing.Part;

                    if (drawingPart != null && drawingPart.ModelIdentifier != null)
                    {
                        partCount++;
                        Console.WriteLine($"\nProcessing Part {partCount}...");

                        // Get MODEL Part
                        var modelObj = model.SelectModelObject(drawingPart.ModelIdentifier);
                        var modelPart = modelObj as Part;

                        if (modelPart != null)
                        {
                            // DIRECT APPROACH from TeklaDrawingExamples.cs line 833
                            var solid = modelPart.GetSolid();
                            if (solid != null)
                            {
                                var vertices = new List<Point>();

                                // Use FaceEnumerator -> LoopEnumerator -> VertexEnumerator
                                FaceEnumerator faceEnum = solid.GetFaceEnumerator();
                                while (faceEnum.MoveNext())
                                {
                                    Face face = faceEnum.Current as Face;
                                    if (face != null)
                                    {
                                        LoopEnumerator loopEnum = face.GetLoopEnumerator();
                                        while (loopEnum.MoveNext())
                                        {
                                            Loop loop = loopEnum.Current as Loop;
                                            if (loop != null)
                                            {
                                                VertexEnumerator vertexEnum = loop.GetVertexEnumerator() as VertexEnumerator;
                                                while (vertexEnum.MoveNext())
                                                {
                                                    Point vertex = vertexEnum.Current as Point;
                                                    if (vertex != null)
                                                    {
                                                        // Simple duplicate check (from line 858-864)
                                                        bool isDuplicate = false;
                                                        foreach (Point existingPoint in vertices)
                                                        {
                                                            if (Math.Abs(existingPoint.X - vertex.X) < 0.01 &&
                                                                Math.Abs(existingPoint.Y - vertex.Y) < 0.01 &&
                                                                Math.Abs(existingPoint.Z - vertex.Z) < 0.01)
                                                            {
                                                                isDuplicate = true;
                                                                break;
                                                            }
                                                        }

                                                        if (!isDuplicate)
                                                        {
                                                            vertices.Add(vertex);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                Console.WriteLine($"  Part ID {modelPart.Identifier.ID}: {vertices.Count} unique vertices");
                                vertexCount += vertices.Count;

                                // Show first 3 vertices as sample
                                if (vertices.Count > 0)
                                {
                                    Console.WriteLine($"  Sample vertices:");
                                    for (int i = 0; i < Math.Min(3, vertices.Count); i++)
                                    {
                                        Console.WriteLine($"    [{i}] ({vertices[i].X:F2}, {vertices[i].Y:F2}, {vertices[i].Z:F2})");
                                    }
                                }
                            }
                        }
                    }
                }

                var elapsed = DateTime.Now - startTime;
                Console.WriteLine($"\n=== TEST RESULTS ===");
                Console.WriteLine($"Parts processed: {partCount}");
                Console.WriteLine($"Total vertices: {vertexCount}");
                Console.WriteLine($"Average vertices per part: {(partCount > 0 ? vertexCount / partCount : 0)}");
                Console.WriteLine($"Time elapsed: {elapsed.TotalSeconds:F2} seconds");
                Console.WriteLine($"Time per part: {(partCount > 0 ? elapsed.TotalSeconds / partCount : 0):F2} seconds");

                Console.WriteLine("\nPress Enter to exit...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.ReadLine();
            }
        }
    }
}
