using System;
using System.Threading.Tasks;
using TeklaDrawingExtractor.Extraction;
using TeklaDrawingExtractor.Extraction.DataModels;
using TeklaDrawingExtractor.Utilities;

namespace TeklaDrawingExtractor
{
    /// <summary>
    /// Automated test for extraction - runs headless
    /// </summary>
    class TestExtraction
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== TeklaDrawingExtractor Automated Test ===");
            Console.WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

            // Create configuration
            var config = new ExtractionConfig
            {
                OutputDirectory = @"C:\Tekla\Extractions",
                UseHierarchicalJson = true,
                PrettyFormat = true,
                MaxDrawings = 10,

                // ML Training: ALL objects extracted
                ExtractProfiles = true,
                ExtractDimensionValues = true,
                ExtractBoltPositions = true,
                ExtractChamfers = true,
                ExtractSolidVertices = false,  // Skip for speed
                IncludeDebugInfo = true,
                IncludeSecondaryParts = true
            };

            // Create logger
            var logger = new Logger((msg) => Console.WriteLine(msg));

            // Create progress reporter
            var progress = new Progress<ExtractionProgress>(p =>
            {
                Console.WriteLine($"[{p.DrawingsProcessed}/{p.TotalDrawings}] {p.CurrentDrawing} - {p.CurrentView} | Objects: {p.ObjectsExtracted}");
            });

            // Create extractor
            var extractor = new DrawingExtractor();

            try
            {
                logger.Info("Starting extraction...");
                var result = await extractor.ExtractAsync(config, progress, logger);

                Console.WriteLine("\n" + new string('=', 80));
                if (result.Success)
                {
                    Console.WriteLine("✅ EXTRACTION SUCCESSFUL");
                    Console.WriteLine($"   Drawings extracted: {result.DrawingDataList.Count}");
                    Console.WriteLine($"   Total objects: {result.TotalObjectsExtracted}");
                    Console.WriteLine($"   Output file: {result.OutputFilePath}");

                    // Print summary of first drawing
                    if (result.DrawingDataList.Count > 0)
                    {
                        var first = result.DrawingDataList[0];
                        Console.WriteLine($"\n   First drawing [{first.DrawingMark}]:");
                        Console.WriteLine($"     - Views: {first.Views.Count}");
                        Console.WriteLine($"     - Parts: {first.Parts.Count}");
                        Console.WriteLine($"     - Bolt groups: {first.BoltGroups.Count}");
                        Console.WriteLine($"     - Dimensions: {first.Dimensions.Count}");
                        Console.WriteLine($"     - Marks: {first.Marks.Count}");
                        Console.WriteLine($"     - Text objects: {first.TextObjects.Count}");
                    }
                }
                else
                {
                    Console.WriteLine("❌ EXTRACTION FAILED");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"   ERROR: {error}");
                    }
                }
                Console.WriteLine(new string('=', 80));
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n" + new string('=', 80));
                Console.WriteLine("❌ EXCEPTION OCCURRED");
                Console.WriteLine($"   {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"   {ex.StackTrace}");
                Console.WriteLine(new string('=', 80));
            }

            Console.WriteLine($"\nCompleted: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
