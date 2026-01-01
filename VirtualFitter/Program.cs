using System;

namespace FitterAnalyzer
{
    /// <summary>
    /// Fitter Analyzer - Core dimension analysis algorithm
    /// Simplified console tool for assembly dimension analysis
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Fitter Analyzer - Assembly Dimension Analysis");
            Console.WriteLine("==============================================");
            Console.WriteLine();

            try
            {
                // Connect to Tekla
                var model = new Tekla.Structures.Model.Model();
                if (!model.GetConnectionStatus())
                {
                    Console.WriteLine("ERROR: Cannot connect to Tekla Structures.");
                    Console.WriteLine("Please ensure Tekla is running and a model is open.");
                    Console.WriteLine();
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine("Connected to Tekla Structures");
                Console.WriteLine();

                // Get current drawing
                var drawingHandler = new Tekla.Structures.Drawing.DrawingHandler();
                var drawing = drawingHandler.GetActiveDrawing();

                if (drawing == null)
                {
                    Console.WriteLine("ERROR: No drawing is currently open.");
                    Console.WriteLine("Please open a drawing in Tekla and try again.");
                    Console.WriteLine();
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine($"Analyzing drawing: {drawing.Mark}");
                Console.WriteLine();

                // Run analysis
                var analyzer = new FitterAnalyzer(model, drawing);
                var summary = analyzer.Analyze();

                // Display summary
                Console.WriteLine(summary.ToString());

                // Save summary to file
                var outputFile = $"FitterAnalysis_{drawing.Mark}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                System.IO.File.WriteAllText(outputFile, summary.ToString());
                Console.WriteLine();
                Console.WriteLine($"Summary saved to: {outputFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL ERROR: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
