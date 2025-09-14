using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hearthvale.GameCode.Tools;

namespace Hearthvale.AtlasGeneratorUI
{
    public partial class BatchProcessDialog : Form
    {
        private TextBox sourceDirectoryTextBox = null!;
        private Button browseSourceButton = null!;
        private TextBox outputDirectoryTextBox = null!;
        private Button browseOutputButton = null!;
        private ListBox filesListBox = null!;
        private Button scanButton = null!;
        private ComboBox presetComboBox = null!;
        private CheckBox overwriteCheckBox = null!;
        private ProgressBar progressBar = null!;
        private TextBox logTextBox = null!;
        private Button processButton = null!;
        private Button closeButton = null!;

        public BatchProcessDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Batch Process Atlas Generation";
            this.Size = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(10)
            };

            // Configure row styles
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Source/Output
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Scan controls
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40)); // Files list
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Process controls
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60)); // Log
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons

            // Source and Output directories
            var pathPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Height = 60
            };
            pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Auto size for labels
            pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));  // Flexible width for text boxes
            pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); // Wider fixed width for buttons

            pathPanel.Controls.Add(new Label { Text = "Source Directory:", Anchor = AnchorStyles.Left }, 0, 0);
            sourceDirectoryTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            pathPanel.Controls.Add(sourceDirectoryTextBox, 1, 0);
            browseSourceButton = new Button 
            { 
                Text = "📂 Browse", 
                Width = 100, 
                Height = 26,
                BackColor = Color.LightSkyBlue,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderColor = Color.DarkBlue, BorderSize = 1 },
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Dock = DockStyle.Fill
            };
            browseSourceButton.Click += BrowseSourceButton_Click;
            pathPanel.Controls.Add(browseSourceButton, 2, 0);

            pathPanel.Controls.Add(new Label { Text = "Output Directory:", Anchor = AnchorStyles.Left }, 0, 1);
            outputDirectoryTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            pathPanel.Controls.Add(outputDirectoryTextBox, 1, 1);
            browseOutputButton = new Button 
            { 
                Text = "💾 Browse", 
                Width = 100, 
                Height = 26,
                BackColor = Color.LightGoldenrodYellow,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderColor = Color.DarkGoldenrod, BorderSize = 1 },
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Dock = DockStyle.Fill
            };
            browseOutputButton.Click += BrowseOutputButton_Click;
            pathPanel.Controls.Add(browseOutputButton, 2, 1);

            mainLayout.Controls.Add(pathPanel, 0, 0);

            // Scan controls
            var scanPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Height = 35
            };

            scanButton = new Button { Text = "Scan for Images", Width = 120, Height = 25 };
            scanButton.Click += ScanButton_Click;
            scanPanel.Controls.Add(scanButton);

            var filterLabel = new Label { Text = "Filter:", AutoSize = true, Anchor = AnchorStyles.Left };
            scanPanel.Controls.Add(filterLabel);

            var filterTextBox = new TextBox { Text = "*.png", Width = 80 };
            scanPanel.Controls.Add(filterTextBox);

            mainLayout.Controls.Add(scanPanel, 0, 1);

            // Files list
            var filesGroup = new GroupBox
            {
                Text = "Found Images",
                Dock = DockStyle.Fill
            };

            filesListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                SelectionMode = SelectionMode.MultiExtended
            };

            filesGroup.Controls.Add(filesListBox);
            mainLayout.Controls.Add(filesGroup, 0, 2);

            // Process controls
            var processPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Height = 35
            };

            processPanel.Controls.Add(new Label { Text = "Preset:", AutoSize = true });
            presetComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
            presetComboBox.Items.AddRange(new string[]
            {
                "RPG Character 32x32",
                "RPG Character 48x48",
                "Weapon Icons 64x64",
                "Item Icons 32x32",
                "Tileset 16x16"
            });
            presetComboBox.SelectedIndex = 0;
            processPanel.Controls.Add(presetComboBox);

            overwriteCheckBox = new CheckBox { Text = "Overwrite existing", AutoSize = true };
            processPanel.Controls.Add(overwriteCheckBox);

            processButton = new Button { Text = "Process All", Width = 100, Height = 25 };
            processButton.Click += ProcessButton_Click;
            processPanel.Controls.Add(processButton);

            mainLayout.Controls.Add(processPanel, 0, 3);

            // Progress and Log
            var logGroup = new GroupBox
            {
                Text = "Progress Log",
                Dock = DockStyle.Fill
            };

            var logLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            logLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            logLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            progressBar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 20,
                Style = ProgressBarStyle.Blocks
            };

            logTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 9)
            };

            logLayout.Controls.Add(progressBar, 0, 0);
            logLayout.Controls.Add(logTextBox, 0, 1);

            logGroup.Controls.Add(logLayout);
            mainLayout.Controls.Add(logGroup, 0, 4);

            // Bottom buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 35
            };

            closeButton = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Width = 75,
                Height = 25
            };

            buttonPanel.Controls.Add(closeButton);
            mainLayout.Controls.Add(buttonPanel, 0, 5);

            this.Controls.Add(mainLayout);
            this.AcceptButton = closeButton;
        }

        private void BrowseSourceButton_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog()
            {
                Description = "Select source directory containing spritesheets"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                sourceDirectoryTextBox.Text = dialog.SelectedPath;
                if (string.IsNullOrEmpty(outputDirectoryTextBox.Text))
                {
                    outputDirectoryTextBox.Text = Path.Combine(dialog.SelectedPath, "atlases");
                }
            }
        }

        private void BrowseOutputButton_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog()
            {
                Description = "Select output directory for atlas files"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                outputDirectoryTextBox.Text = dialog.SelectedPath;
            }
        }

        private void ScanButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(sourceDirectoryTextBox.Text) || !Directory.Exists(sourceDirectoryTextBox.Text))
            {
                MessageBox.Show("Please select a valid source directory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                filesListBox.Items.Clear();
                
                var imageFiles = Directory.GetFiles(sourceDirectoryTextBox.Text, "*.png", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(sourceDirectoryTextBox.Text, "*.jpg", SearchOption.AllDirectories))
                    .Concat(Directory.GetFiles(sourceDirectoryTextBox.Text, "*.jpeg", SearchOption.AllDirectories))
                    .Concat(Directory.GetFiles(sourceDirectoryTextBox.Text, "*.bmp", SearchOption.AllDirectories))
                    .OrderBy(f => f);

                foreach (var file in imageFiles)
                {
                    var relativePath = Path.GetRelativePath(sourceDirectoryTextBox.Text, file);
                    filesListBox.Items.Add(new FileItem(file, relativePath));
                }

                LogMessage($"Found {filesListBox.Items.Count} image files.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error scanning directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void ProcessButton_Click(object sender, EventArgs e)
        {
            if (filesListBox.Items.Count == 0)
            {
                MessageBox.Show("No files to process. Please scan for images first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrEmpty(outputDirectoryTextBox.Text))
            {
                MessageBox.Show("Please specify an output directory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                processButton.Enabled = false;
                progressBar.Maximum = filesListBox.Items.Count;
                progressBar.Value = 0;

                // Create output directory if it doesn't exist
                Directory.CreateDirectory(outputDirectoryTextBox.Text);

                var preset = presetComboBox.SelectedItem?.ToString() ?? "RPG Character 32x32";
                var processed = 0;
                var errors = 0;

                LogMessage($"Starting batch process with preset: {preset}");

                foreach (FileItem item in filesListBox.Items)
                {
                    try
                    {
                        var outputFile = Path.Combine(outputDirectoryTextBox.Text, 
                            Path.ChangeExtension(item.RelativePath, ".xml"));

                        if (!overwriteCheckBox.Checked && File.Exists(outputFile))
                        {
                            LogMessage($"Skipping {item.RelativePath} (already exists)");
                            continue;
                        }

                        // Create directory for output file if needed
                        Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

                        var config = CreateConfigFromPreset(preset, item.FullPath, outputFile);
                        
                        await Task.Run(() =>
                        {
                            var xml = AtlasGenerator.GenerateAtlas(config);
                            File.WriteAllText(outputFile, xml);
                        });

                        LogMessage($"✓ Processed {item.RelativePath} -> {Path.GetFileName(outputFile)}");
                        processed++;
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"✗ Error processing {item.RelativePath}: {ex.Message}");
                        errors++;
                    }

                    progressBar.Value++;
                    Application.DoEvents();
                }

                LogMessage($"Batch process complete: {processed} processed, {errors} errors.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Batch process error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                processButton.Enabled = true;
            }
        }

        private AtlasGenerator.AtlasConfig CreateConfigFromPreset(string preset, string inputPath, string outputPath)
        {
            var config = new AtlasGenerator.AtlasConfig
            {
                SpritesheetPath = inputPath,
                OutputPath = outputPath,
                TexturePath = Path.GetFileName(inputPath),
                TrimTransparency = true,
                TransparencyThreshold = 0
            };

            switch (preset)
            {
                case "RPG Character 32x32":
                    config.GridWidth = 32;
                    config.GridHeight = 32;
                    config.NamingPattern = "Character_{row}_{col}";
                    break;

                case "RPG Character 48x48":
                    config.GridWidth = 48;
                    config.GridHeight = 48;
                    config.NamingPattern = "Character_{row}_{col}";
                    break;

                case "Weapon Icons 64x64":
                    config.GridWidth = 64;
                    config.GridHeight = 64;
                    config.NamingPattern = "Weapon_{row}_{col}";
                    break;

                case "Item Icons 32x32":
                    config.GridWidth = 32;
                    config.GridHeight = 32;
                    config.NamingPattern = "Item_{row}_{col}";
                    break;

                case "Tileset 16x16":
                    config.GridWidth = 16;
                    config.GridHeight = 16;
                    config.NamingPattern = "Tile_{row}_{col}";
                    config.TrimTransparency = false;
                    break;
            }

            return config;
        }

        private void LogMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            logTextBox.AppendText($"[{timestamp}] {message}\r\n");
            logTextBox.SelectionStart = logTextBox.Text.Length;
            logTextBox.ScrollToCaret();
        }

        private class FileItem
        {
            public string FullPath { get; }
            public string RelativePath { get; }

            public FileItem(string fullPath, string relativePath)
            {
                FullPath = fullPath;
                RelativePath = relativePath;
            }

            public override string ToString() => RelativePath;
        }
    }
}