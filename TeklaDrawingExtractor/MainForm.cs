using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TeklaDrawingExtractor.Extraction;
using TeklaDrawingExtractor.Extraction.DataModels;
using TeklaDrawingExtractor.Utilities;

namespace TeklaDrawingExtractor
{
    public partial class MainForm : Form
    {
        private ExtractionConfig _config;
        private DrawingExtractor _extractor;
        private Logger _logger;
        private bool _isExtracting = false;

        // Output Settings Controls
        private TextBox txtOutputDirectory;
        private Button btnBrowse;
        private RadioButton rbJson;
        private RadioButton rbJsonL;
        private RadioButton rbPretty;
        private RadioButton rbCompact;

        // Filter Controls
        private NumericUpDown numMaxDrawings;

        // Debug Panel Controls
        private Button btnExtractSingleObject;
        private RichTextBox txtDebugOutput;

        // Extraction Options (only optional filters for ML training)
        private CheckBox chkExtractSolidVertices;
        private CheckBox chkIncludeDebugInfo;

        // Organization Options
        private RadioButton rbHierarchical;
        private RadioButton rbFlat;
        private CheckBox chkGroupByViews;
        private CheckBox chkGroupByObjectType;
        private CheckBox chkGroupByImportance;
        private RadioButton rbMainBeamSeparate;
        private RadioButton rbMainBeamWithAll;

        // Progress Controls
        private Button btnStartExtraction;
        private Button btnPause;
        private Button btnCancel;
        private ProgressBar progressBar;
        private Label lblDrawingsProcessed;
        private Label lblObjectsExtracted;
        private Label lblCurrentDrawing;
        private Label lblTimeElapsed;
        private Label lblEstimatedRemaining;

        // Log Output
        private RichTextBox txtLog;

        public MainForm()
        {
            _config = new ExtractionConfig();
            _extractor = new DrawingExtractor();

            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "Tekla Shop Drawing Extraction for ML Training";
            this.Size = new Size(900, 950);
            this.StartPosition = FormStartPosition.CenterScreen;

            int y = 10;

            // Output Settings Section
            y = CreateOutputSettingsSection(y);

            // Extraction Filters Section
            y = CreateExtractionFiltersSection(y);

            // Extraction Options Section
            y = CreateExtractionOptionsSection(y);

            // JSON Organization Section
            y = CreateJsonOrganizationSection(y);

            // Debug Panel Section
            y = CreateDebugPanelSection(y);

            // Action Buttons
            y = CreateActionButtons(y);

            // Progress Section
            y = CreateProgressSection(y);

            // Log Output
            y = CreateLogSection(y);
        }

        private int CreateOutputSettingsSection(int y)
        {
            var grpOutput = new GroupBox
            {
                Text = "📁 Output Settings",
                Location = new Point(10, y),
                Size = new Size(860, 100)
            };
            this.Controls.Add(grpOutput);

            var lblDirectory = new Label { Text = "Output Directory:", Location = new Point(10, 25), Size = new Size(100, 20) };
            grpOutput.Controls.Add(lblDirectory);

            txtOutputDirectory = new TextBox { Location = new Point(120, 23), Size = new Size(600, 20), Text = _config.OutputDirectory };
            grpOutput.Controls.Add(txtOutputDirectory);

            btnBrowse = new Button { Text = "Browse...", Location = new Point(730, 21), Size = new Size(100, 25) };
            btnBrowse.Click += BtnBrowse_Click;
            grpOutput.Controls.Add(btnBrowse);

            var lblFormat = new Label { Text = "Output Format:", Location = new Point(10, 55), Size = new Size(100, 20) };
            grpOutput.Controls.Add(lblFormat);

            rbJson = new RadioButton { Text = "JSON", Location = new Point(120, 53), Size = new Size(70, 20), Checked = true };
            grpOutput.Controls.Add(rbJson);

            rbJsonL = new RadioButton { Text = "JSONL", Location = new Point(200, 53), Size = new Size(70, 20) };
            grpOutput.Controls.Add(rbJsonL);

            var lblIndent = new Label { Text = "Indentation:", Location = new Point(300, 55), Size = new Size(80, 20) };
            grpOutput.Controls.Add(lblIndent);

            rbPretty = new RadioButton { Text = "Pretty", Location = new Point(390, 53), Size = new Size(70, 20), Checked = true };
            grpOutput.Controls.Add(rbPretty);

            rbCompact = new RadioButton { Text = "Compact", Location = new Point(470, 53), Size = new Size(80, 20) };
            grpOutput.Controls.Add(rbCompact);

            return y + 110;
        }

        private int CreateExtractionFiltersSection(int y)
        {
            var grpFilters = new GroupBox
            {
                Text = "🎯 Drawing Selection & Filters",
                Location = new Point(10, y),
                Size = new Size(860, 120)
            };
            this.Controls.Add(grpFilters);

            var lblInfo = new Label
            {
                Text = "💡 Extracts from selected drawings in Document Manager (or all drawings if none selected)",
                Location = new Point(10, 20),
                Size = new Size(840, 20),
                ForeColor = Color.DarkGreen,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            grpFilters.Controls.Add(lblInfo);

            var lblNote = new Label
            {
                Text = "Note: ALL object types and ALL views are extracted for ML training (not filtered)",
                Location = new Point(10, 45),
                Size = new Size(840, 20),
                ForeColor = Color.DarkBlue
            };
            grpFilters.Controls.Add(lblNote);

            var lblMax = new Label { Text = "Max Drawings:", Location = new Point(10, 75), Size = new Size(100, 20) };
            grpFilters.Controls.Add(lblMax);

            numMaxDrawings = new NumericUpDown
            {
                Location = new Point(120, 73),
                Size = new Size(100, 20),
                Minimum = 1,
                Maximum = 1000,
                Value = 10
            };
            grpFilters.Controls.Add(numMaxDrawings);

            return y + 110;
        }

        private int CreateExtractionOptionsSection(int y)
        {
            var grpOptions = new GroupBox
            {
                Text = "⚙️ ML Training Extraction (ALL objects extracted)",
                Location = new Point(10, y),
                Size = new Size(860, 110)
            };
            this.Controls.Add(grpOptions);

            // Information label
            var lblInfo = new Label
            {
                Text = "ℹ️ For ML training: ALL object types and ALL views are extracted automatically.\n" +
                       "Parts, dimensions, bolts, marks, text - everything is included.",
                Location = new Point(10, 25),
                Size = new Size(820, 35),
                ForeColor = Color.DarkBlue
            };
            grpOptions.Controls.Add(lblInfo);

            chkExtractSolidVertices = new CheckBox
            {
                Text = "☐ Extract solid vertices (slow, large files)",
                Location = new Point(10, 65),
                Size = new Size(400, 20),
                Checked = false
            };
            grpOptions.Controls.Add(chkExtractSolidVertices);

            chkIncludeDebugInfo = new CheckBox
            {
                Text = "☐ Include debug information",
                Location = new Point(450, 65),
                Size = new Size(400, 20),
                Checked = false
            };
            grpOptions.Controls.Add(chkIncludeDebugInfo);

            return y + 120;
        }

        private int CreateJsonOrganizationSection(int y)
        {
            var grpOrg = new GroupBox
            {
                Text = "📊 JSON Organization",
                Location = new Point(10, y),
                Size = new Size(860, 120)
            };
            this.Controls.Add(grpOrg);

            var lblStructure = new Label { Text = "Structure:", Location = new Point(10, 25), Size = new Size(80, 20) };
            grpOrg.Controls.Add(lblStructure);

            rbHierarchical = new RadioButton
            {
                Text = "● Hierarchical (Recommended for viewing)",
                Location = new Point(100, 23),
                Size = new Size(300, 20),
                Checked = true
            };
            grpOrg.Controls.Add(rbHierarchical);

            rbFlat = new RadioButton
            {
                Text = "○ Flat (Better for ML processing)",
                Location = new Point(100, 48),
                Size = new Size(300, 20)
            };
            grpOrg.Controls.Add(rbFlat);

            var lblGroupBy = new Label { Text = "Group by:", Location = new Point(10, 80), Size = new Size(80, 20) };
            grpOrg.Controls.Add(lblGroupBy);

            chkGroupByViews = new CheckBox { Text = "Views", Location = new Point(100, 78), Size = new Size(80, 20), Checked = true };
            grpOrg.Controls.Add(chkGroupByViews);

            chkGroupByObjectType = new CheckBox { Text = "Object Type", Location = new Point(190, 78), Size = new Size(120, 20), Checked = true };
            grpOrg.Controls.Add(chkGroupByObjectType);

            chkGroupByImportance = new CheckBox { Text = "Importance", Location = new Point(320, 78), Size = new Size(120, 20), Checked = true };
            grpOrg.Controls.Add(chkGroupByImportance);

            var lblMainBeam = new Label { Text = "Main beam:", Location = new Point(450, 25), Size = new Size(80, 20) };
            grpOrg.Controls.Add(lblMainBeam);

            rbMainBeamSeparate = new RadioButton
            {
                Text = "● Separate section",
                Location = new Point(540, 23),
                Size = new Size(150, 20),
                Checked = true
            };
            grpOrg.Controls.Add(rbMainBeamSeparate);

            rbMainBeamWithAll = new RadioButton
            {
                Text = "○ With all parts",
                Location = new Point(540, 48),
                Size = new Size(150, 20)
            };
            grpOrg.Controls.Add(rbMainBeamWithAll);

            return y + 130;
        }

        private int CreateDebugPanelSection(int y)
        {
            var grpDebug = new GroupBox
            {
                Text = "🔍 Debug: Extract Single Object",
                Location = new Point(10, y),
                Size = new Size(860, 200)
            };
            this.Controls.Add(grpDebug);

            var lblInfo = new Label
            {
                Text = "1. Open a drawing in Tekla | 2. Click button | 3. Select object(s) | 4. Mid-click to confirm",
                Location = new Point(10, 20),
                Size = new Size(840, 20),
                ForeColor = Color.DarkGreen
            };
            grpDebug.Controls.Add(lblInfo);

            btnExtractSingleObject = new Button
            {
                Text = "📌 Extract Selected Object(s)",
                Location = new Point(10, 45),
                Size = new Size(200, 30),
                BackColor = Color.LightGreen
            };
            btnExtractSingleObject.Click += BtnExtractSingleObject_Click;
            grpDebug.Controls.Add(btnExtractSingleObject);

            var lblOutput = new Label { Text = "Object JSON Output:", Location = new Point(10, 85), Size = new Size(150, 20) };
            grpDebug.Controls.Add(lblOutput);

            txtDebugOutput = new RichTextBox
            {
                Location = new Point(10, 105),
                Size = new Size(840, 85),
                Font = new Font("Consolas", 8),
                ReadOnly = true,
                BackColor = Color.WhiteSmoke
            };
            grpDebug.Controls.Add(txtDebugOutput);

            return y + 210;
        }

        private int CreateActionButtons(int y)
        {
            btnStartExtraction = new Button
            {
                Text = "🚀 START EXTRACTION",
                Location = new Point(10, y),
                Size = new Size(200, 35),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            btnStartExtraction.Click += BtnStartExtraction_Click;
            this.Controls.Add(btnStartExtraction);

            btnPause = new Button
            {
                Text = "⏸️ PAUSE",
                Location = new Point(220, y),
                Size = new Size(100, 35),
                Enabled = false
            };
            this.Controls.Add(btnPause);

            btnCancel = new Button
            {
                Text = "❌ CANCEL",
                Location = new Point(330, y),
                Size = new Size(100, 35),
                Enabled = false
            };
            this.Controls.Add(btnCancel);

            return y + 45;
        }

        private int CreateProgressSection(int y)
        {
            var grpProgress = new GroupBox
            {
                Text = "📋 Progress",
                Location = new Point(10, y),
                Size = new Size(860, 120)
            };
            this.Controls.Add(grpProgress);

            progressBar = new ProgressBar
            {
                Location = new Point(10, 25),
                Size = new Size(830, 25)
            };
            grpProgress.Controls.Add(progressBar);

            lblDrawingsProcessed = new Label
            {
                Text = "Drawings processed: 0 / 0",
                Location = new Point(10, 60),
                Size = new Size(400, 20)
            };
            grpProgress.Controls.Add(lblDrawingsProcessed);

            lblObjectsExtracted = new Label
            {
                Text = "Objects extracted: 0",
                Location = new Point(10, 85),
                Size = new Size(400, 20)
            };
            grpProgress.Controls.Add(lblObjectsExtracted);

            lblCurrentDrawing = new Label
            {
                Text = "Current: -",
                Location = new Point(450, 60),
                Size = new Size(400, 20)
            };
            grpProgress.Controls.Add(lblCurrentDrawing);

            lblTimeElapsed = new Label
            {
                Text = "Time elapsed: 00:00:00",
                Location = new Point(450, 85),
                Size = new Size(200, 20)
            };
            grpProgress.Controls.Add(lblTimeElapsed);

            lblEstimatedRemaining = new Label
            {
                Text = "Estimated remaining: 00:00:00",
                Location = new Point(650, 85),
                Size = new Size(200, 20)
            };
            grpProgress.Controls.Add(lblEstimatedRemaining);

            return y + 130;
        }

        private int CreateLogSection(int y)
        {
            var lblLog = new Label
            {
                Text = "📝 Log Output:",
                Location = new Point(10, y),
                Size = new Size(100, 20)
            };
            this.Controls.Add(lblLog);

            txtLog = new RichTextBox
            {
                Location = new Point(10, y + 25),
                Size = new Size(860, 120),
                ReadOnly = true,
                BackColor = Color.White,
                Font = new Font("Consolas", 9)
            };
            this.Controls.Add(txtLog);

            return y + 150;
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.SelectedPath = txtOutputDirectory.Text;
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    txtOutputDirectory.Text = folderDialog.SelectedPath;
                }
            }
        }

        private async void BtnStartExtraction_Click(object sender, EventArgs e)
        {
            if (_isExtracting)
            {
                MessageBox.Show("Extraction is already running!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Build configuration from UI
            _config.OutputDirectory = txtOutputDirectory.Text;
            _config.UseJsonL = rbJsonL.Checked;
            _config.PrettyFormat = rbPretty.Checked;
            _config.UseHierarchicalJson = rbHierarchical.Checked;

            // Drawing selection (from Document Manager or all drawings)
            _config.MaxDrawings = (int)numMaxDrawings.Value;

            // WARNING: >10 drawings will take a long time (requirement #8)
            if (_config.MaxDrawings > 10)
            {
                var result = MessageBox.Show(
                    $"You are about to extract {_config.MaxDrawings} drawings.\n\n" +
                    "This will take a long time and create large files.\n\n" +
                    "Do you want to continue?",
                    "Warning: Large Extraction",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (result != DialogResult.Yes)
                {
                    return;
                }
            }

            // ML Training: Extract ALL object types and ALL views (no filtering)
            _config.ExtractProfiles = true;  // Always extract
            _config.ExtractDimensionValues = true;  // Always extract
            _config.ExtractBoltPositions = true;  // Always extract
            _config.ExtractChamfers = true;  // Always extract
            _config.IncludeSecondaryParts = true;  // Always include ALL parts

            // Optional settings (only these two)
            _config.ExtractSolidVertices = chkExtractSolidVertices.Checked;
            _config.IncludeDebugInfo = chkIncludeDebugInfo.Checked;

            // Organization options
            _config.GroupByViews = chkGroupByViews.Checked;
            _config.GroupByObjectType = chkGroupByObjectType.Checked;
            _config.GroupByImportance = chkGroupByImportance.Checked;
            _config.MainBeamSeparate = rbMainBeamSeparate.Checked;

            // Setup logger
            _logger = new Logger(LogMessage);
            _logger.Info("Starting extraction...");

            // Setup progress reporting
            var progress = new Progress<ExtractionProgress>(UpdateProgress);

            // Disable UI
            _isExtracting = true;
            btnStartExtraction.Enabled = false;
            btnPause.Enabled = true;
            btnCancel.Enabled = true;

            try
            {
                // Run extraction
                var result = await _extractor.ExtractAsync(_config, progress, _logger);

                if (result.Success)
                {
                    MessageBox.Show(
                        $"Extraction complete!\n\n" +
                        $"Drawings: {result.DrawingDataList.Count}\n" +
                        $"Objects: {result.TotalObjectsExtracted}\n" +
                        $"Output: {result.OutputFilePath}",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else
                {
                    MessageBox.Show(
                        $"Extraction failed:\n\n{string.Join("\n", result.Errors)}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _logger.Error("Extraction failed", ex);
            }
            finally
            {
                // Re-enable UI
                _isExtracting = false;
                btnStartExtraction.Enabled = true;
                btnPause.Enabled = false;
                btnCancel.Enabled = false;
            }
        }

        private void UpdateProgress(ExtractionProgress progress)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<ExtractionProgress>(UpdateProgress), progress);
                return;
            }

            progressBar.Maximum = progress.TotalDrawings;
            progressBar.Value = Math.Min(progress.DrawingsProcessed, progress.TotalDrawings);

            lblDrawingsProcessed.Text = $"Drawings processed: {progress.DrawingsProcessed} / {progress.TotalDrawings}";
            lblObjectsExtracted.Text = $"Objects extracted: {progress.ObjectsExtracted}";
            lblCurrentDrawing.Text = $"Current: [{progress.CurrentDrawing}] - {progress.CurrentView}";
            lblTimeElapsed.Text = $"Time elapsed: {progress.Elapsed:hh\\:mm\\:ss}";
            lblEstimatedRemaining.Text = $"Estimated remaining: {progress.EstimatedRemaining:hh\\:mm\\:ss}";
        }

        private void LogMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(LogMessage), message);
                return;
            }

            txtLog.AppendText(message + Environment.NewLine);
            txtLog.ScrollToCaret();
        }

        private void BtnExtractSingleObject_Click(object sender, EventArgs e)
        {
            txtDebugOutput.Text = "DEBUG FEATURE: Coming soon!\n\n" +
                                 "This feature will allow you to select individual objects from a drawing\n" +
                                 "and view their extraction data.\n\n" +
                                 "For now, use the full extraction to test the data quality.";
            txtDebugOutput.ForeColor = Color.Blue;

            // TODO: Implement object picking using Tekla API once correct API methods are identified
        }

        private void LoadSettings()
        {
            // Load saved settings if available
            // TODO: Implement settings persistence
        }

        private void SaveSettings()
        {
            // Save current settings
            // TODO: Implement settings persistence
        }
    }
}
