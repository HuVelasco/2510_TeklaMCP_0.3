using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Tekla.Structures;
using Tekla.Structures.Model;
using Tekla.Structures.Drawing;
using System.Linq;

namespace VirtualFitter
{
    public partial class MainForm : Form
    {
        private Tekla.Structures.Model.Model model;
        private DrawingHandler drawingHandler;
        private List<DatabaseObject> createdDimensions;
        private AssemblyAnalyzer analyzer;
        private DimensionCreator dimensionCreator;

        public MainForm()
        {
            InitializeComponent();
            createdDimensions = new List<DatabaseObject>();

            // Initialize Tekla connection
            try
            {
                model = new Tekla.Structures.Model.Model();
                drawingHandler = new DrawingHandler();

                if (!model.GetConnectionStatus())
                {
                    MessageBox.Show("Please start Tekla Structures first!", "Connection Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect to Tekla: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Virtual Fitter - Automatic Dimension Creation";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Main panel
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(10)
            };

            // Title
            var titleLabel = new Label
            {
                Text = "Virtual Fitter - Smart Dimension Creator",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 50
            };

            // Status panel
            var statusGroup = new GroupBox
            {
                Text = "Status",
                Dock = DockStyle.Fill,
                Height = 100
            };

            var statusLabel = new Label
            {
                Name = "statusLabel",
                Text = "Ready. Open a drawing to begin.",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            statusGroup.Controls.Add(statusLabel);

            // Results panel
            var resultsGroup = new GroupBox
            {
                Text = "Results",
                Dock = DockStyle.Fill,
                Height = 250
            };

            var resultsText = new TextBox
            {
                Name = "resultsText",
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9),
                ReadOnly = true
            };
            resultsGroup.Controls.Add(resultsText);

            // Button panel
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Height = 80,
                Padding = new Padding(0, 10, 0, 0)
            };

            var analyzeButton = new Button
            {
                Text = "1. Analyze Assembly",
                Width = 150,
                Height = 50,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Margin = new Padding(5)
            };
            analyzeButton.Click += AnalyzeButton_Click;

            var createDimsButton = new Button
            {
                Text = "2. Create Dimensions",
                Width = 150,
                Height = 50,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Margin = new Padding(5),
                Enabled = false,
                Name = "createDimsButton"
            };
            createDimsButton.Click += CreateDimensionsButton_Click;

            var selectCreatedButton = new Button
            {
                Text = "Select Created Dims",
                Width = 150,
                Height = 50,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Margin = new Padding(5),
                BackColor = Color.LightGreen,
                Enabled = false,
                Name = "selectCreatedButton"
            };
            selectCreatedButton.Click += SelectCreatedButton_Click;

            var clearButton = new Button
            {
                Text = "Clear All",
                Width = 100,
                Height = 50,
                Margin = new Padding(5)
            };
            clearButton.Click += ClearButton_Click;

            buttonPanel.Controls.Add(analyzeButton);
            buttonPanel.Controls.Add(createDimsButton);
            buttonPanel.Controls.Add(selectCreatedButton);
            buttonPanel.Controls.Add(clearButton);

            // Add all to main panel
            mainPanel.Controls.Add(titleLabel);
            mainPanel.Controls.Add(statusGroup);
            mainPanel.Controls.Add(resultsGroup);
            mainPanel.Controls.Add(buttonPanel);

            this.Controls.Add(mainPanel);
        }

        private void AnalyzeButton_Click(object sender, EventArgs e)
        {
            var statusLabel = this.Controls.Find("statusLabel", true)[0] as Label;
            var resultsText = this.Controls.Find("resultsText", true)[0] as TextBox;
            var createDimsButton = this.Controls.Find("createDimsButton", true)[0] as Button;

            try
            {
                statusLabel.Text = "Analyzing current drawing...";
                statusLabel.BackColor = Color.LightYellow;
                Application.DoEvents();

                // Get current drawing
                var drawing = drawingHandler.GetActiveDrawing();
                if (drawing == null)
                {
                    MessageBox.Show("Please open a drawing first!", "No Drawing",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    statusLabel.Text = "No drawing open.";
                    statusLabel.BackColor = Color.LightCoral;
                    return;
                }

                // Initialize analyzer
                analyzer = new AssemblyAnalyzer(model, drawing);
                var analysis = analyzer.Analyze();

                // Display results
                resultsText.Clear();
                resultsText.AppendText($"=== ASSEMBLY ANALYSIS ==={Environment.NewLine}");
                resultsText.AppendText($"Drawing: {drawing.Mark}{Environment.NewLine}");
                resultsText.AppendText($"Parts found: {analysis.TotalParts}{Environment.NewLine}");
                resultsText.AppendText($"Existing dimensions: {analysis.ExistingDimensions}{Environment.NewLine}");
                resultsText.AppendText($"Available views: {analysis.AvailableViews}{Environment.NewLine}{Environment.NewLine}");

                resultsText.AppendText($"=== REQUIRED DIMENSIONS ==={Environment.NewLine}");
                resultsText.AppendText($"Missing dimensions: {analysis.MissingDimensions.Count}{Environment.NewLine}{Environment.NewLine}");

                if (analysis.MissingDimensions.Count > 0)
                {
                    resultsText.AppendText($"Dimensions to create:{Environment.NewLine}");
                    foreach (var dim in analysis.MissingDimensions.Take(10))
                    {
                        resultsText.AppendText($"  - {dim.Type}: {dim.Description}{Environment.NewLine}");
                    }
                    if (analysis.MissingDimensions.Count > 10)
                    {
                        resultsText.AppendText($"  ... and {analysis.MissingDimensions.Count - 10} more{Environment.NewLine}");
                    }
                }

                resultsText.AppendText($"{Environment.NewLine}=== VIEW ANALYSIS ==={Environment.NewLine}");
                resultsText.AppendText($"Sufficient views: {(analysis.SufficientViews ? "YES" : "NO")}{Environment.NewLine}");

                if (!analysis.SufficientViews)
                {
                    resultsText.AppendText($"{Environment.NewLine}MISSING VIEWS:{Environment.NewLine}");
                    foreach (var viewSuggestion in analysis.SuggestedViews)
                    {
                        resultsText.AppendText($"  - {viewSuggestion}{Environment.NewLine}");
                    }
                }

                statusLabel.Text = $"Analysis complete. Found {analysis.MissingDimensions.Count} missing dimensions.";
                statusLabel.BackColor = Color.LightGreen;
                createDimsButton.Enabled = analysis.MissingDimensions.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Analysis failed: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Analysis failed.";
                statusLabel.BackColor = Color.LightCoral;
            }
        }

        private void CreateDimensionsButton_Click(object sender, EventArgs e)
        {
            var statusLabel = this.Controls.Find("statusLabel", true)[0] as Label;
            var resultsText = this.Controls.Find("resultsText", true)[0] as TextBox;
            var selectCreatedButton = this.Controls.Find("selectCreatedButton", true)[0] as Button;

            if (analyzer == null)
            {
                MessageBox.Show("Please analyze the assembly first!", "No Analysis",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"This will create {analyzer.LastAnalysis.MissingDimensions.Count} dimensions in the drawing.{Environment.NewLine}{Environment.NewLine}" +
                "Created dimensions will be marked with [VF] prefix for easy identification.{Environment.NewLine}{Environment.NewLine}" +
                "Continue?",
                "Confirm Dimension Creation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            try
            {
                statusLabel.Text = "Creating dimensions...";
                statusLabel.BackColor = Color.LightYellow;
                Application.DoEvents();

                // Initialize dimension creator
                dimensionCreator = new DimensionCreator(analyzer.Drawing);

                var creationResult = dimensionCreator.CreateDimensions(analyzer.LastAnalysis.MissingDimensions);
                createdDimensions.AddRange(creationResult.CreatedDimensions);

                // Update results
                resultsText.AppendText($"{Environment.NewLine}=== DIMENSION CREATION ==={Environment.NewLine}");
                resultsText.AppendText($"Successfully created: {creationResult.SuccessCount}{Environment.NewLine}");
                resultsText.AppendText($"Failed: {creationResult.FailureCount}{Environment.NewLine}");

                if (creationResult.FailureCount > 0)
                {
                    resultsText.AppendText($"{Environment.NewLine}Failures:{Environment.NewLine}");
                    foreach (var error in creationResult.Errors.Take(5))
                    {
                        resultsText.AppendText($"  - {error}{Environment.NewLine}");
                    }
                }

                statusLabel.Text = $"Created {creationResult.SuccessCount} dimensions. Use 'Select Created Dims' to select them.";
                statusLabel.BackColor = Color.LightGreen;
                selectCreatedButton.Enabled = creationResult.SuccessCount > 0;

                MessageBox.Show(
                    $"Successfully created {creationResult.SuccessCount} dimensions!{Environment.NewLine}{Environment.NewLine}" +
                    "All created dimensions are prefixed with [VF] for easy identification.{Environment.NewLine}{Environment.NewLine}" +
                    "Click 'Select Created Dims' to select them all in the drawing.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dimension creation failed: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Dimension creation failed.";
                statusLabel.BackColor = Color.LightCoral;
            }
        }

        private void SelectCreatedButton_Click(object sender, EventArgs e)
        {
            if (createdDimensions.Count == 0)
            {
                MessageBox.Show("No dimensions have been created yet!", "Nothing to Select",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // Select all created dimensions in Tekla
                var picker = drawingHandler.GetPicker();
                var objectsToSelect = new DrawingObjectEnumerator();

                foreach (var dim in createdDimensions)
                {
                    if (dim is DrawingObject drawingObj)
                    {
                        objectsToSelect.Add(drawingObj);
                    }
                }

                picker.PickObjects(objectsToSelect);

                MessageBox.Show(
                    $"Selected {createdDimensions.Count} created dimensions in the drawing.{Environment.NewLine}{Environment.NewLine}" +
                    "You can now modify, move, or delete them as needed.",
                    "Dimensions Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Selection failed: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            var resultsText = this.Controls.Find("resultsText", true)[0] as TextBox;
            var statusLabel = this.Controls.Find("statusLabel", true)[0] as Label;
            var createDimsButton = this.Controls.Find("createDimsButton", true)[0] as Button;
            var selectCreatedButton = this.Controls.Find("selectCreatedButton", true)[0] as Button;

            resultsText.Clear();
            createdDimensions.Clear();
            analyzer = null;
            dimensionCreator = null;

            statusLabel.Text = "Ready. Open a drawing to begin.";
            statusLabel.BackColor = SystemColors.Control;
            createDimsButton.Enabled = false;
            selectCreatedButton.Enabled = false;
        }
    }
}
