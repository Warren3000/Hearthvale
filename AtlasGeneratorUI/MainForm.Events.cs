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
    public partial class MainForm
    {
        // Event Handlers
        private void MainForm_Load(object? sender, EventArgs e)
        {
            mainStatusLabel.Text = "Ready - Load a spritesheet to begin";
        }

        private void SpritesheetPathTextBox_TextChanged(object? sender, EventArgs e)
        {
            LoadSpritesheetImage();
        }

        private void BrowseSpritesheetButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog()
            {
                Title = "Select Spritesheet Image",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                spritesheetPathTextBox.Text = dialog.FileName;
                LoadSpritesheetImage();
            }
        }

        private void BrowseOutputButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog()
            {
                Title = "Save Atlas XML",
                Filter = "XML Files|*.xml|All Files|*.*",
                DefaultExt = "xml"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                outputPathTextBox.Text = dialog.FileName;
            }
        }

        private void LoadPresetButton_Click(object? sender, EventArgs e)
        {
            ApplyPreset(presetComboBox.SelectedItem?.ToString() ?? "Custom");
        }

        private void PreviewOption_Changed(object? sender, EventArgs e)
        {
            RefreshPreview();
        }

        private void ZoomTrackBar_ValueChanged(object? sender, EventArgs e)
        {
            var zoom = zoomTrackBar.Value;
            zoomLabel.Text = $"Zoom: {zoom}%";
            ApplyZoom();
        }

        private void SpritesheetPictureBox_Paint(object? sender, PaintEventArgs e)
        {
            DrawPreviewOverlay(e.Graphics);
        }

        private async void GenerateButton_Click(object? sender, EventArgs e)
        {
            await GenerateAtlas();
        }

        private void SaveConfigButton_Click(object? sender, EventArgs e)
        {
            SaveConfiguration();
        }

        private void LoadConfigButton_Click(object? sender, EventArgs e)
        {
            LoadConfiguration();
        }

        private void AddAnimationButton_Click(object? sender, EventArgs e)
        {
            var dialog = new AnimationDialog(new List<AtlasGenerator.SpriteRegion>());
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var animation = dialog.Animation;
                _currentConfig.Animations ??= new List<AtlasGenerator.AnimationConfig>();
                _currentConfig.Animations.Add(animation);
                RefreshAnimationList();
            }
        }

        private void EditAnimationButton_Click(object? sender, EventArgs e)
        {
            if (animationListView.SelectedItems.Count == 0) return;

            var index = animationListView.SelectedIndices[0];
            var animation = _currentConfig.Animations?[index];
            if (animation == null) return;

            var dialog = new AnimationDialog(new List<AtlasGenerator.SpriteRegion>(), animation);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (_currentConfig.Animations != null)
                {
                    _currentConfig.Animations[index] = dialog.Animation;
                    RefreshAnimationList();
                }
            }
        }

        private void RemoveAnimationButton_Click(object? sender, EventArgs e)
        {
            if (animationListView.SelectedItems.Count == 0) return;

            var index = animationListView.SelectedIndices[0];
            _currentConfig.Animations?.RemoveAt(index);
            RefreshAnimationList();
        }

        private void GridSize_Changed(object? sender, EventArgs e)
        {
            gridDisplayLabel.Text = $"Grid: {gridWidthNumeric.Value}x{gridHeightNumeric.Value} px";
            RefreshPreview();
        }

        // Helper Methods
        private void LoadSpritesheetImage()
        {
            try
            {
                if (!string.IsNullOrEmpty(spritesheetPathTextBox.Text) && File.Exists(spritesheetPathTextBox.Text))
                {
                    // Dispose previous image
                    _spritesheetImage?.Dispose();
                    
                    _spritesheetImage = new Bitmap(spritesheetPathTextBox.Text);
                    spritesheetPictureBox.Image = _spritesheetImage;
                    
                    // Auto-fill texture path if empty
                    if (string.IsNullOrEmpty(texturePathTextBox.Text))
                    {
                        texturePathTextBox.Text = Path.GetFileName(spritesheetPathTextBox.Text);
                    }
                    
                    // Auto-fill output path if empty
                    if (string.IsNullOrEmpty(outputPathTextBox.Text))
                    {
                        var dir = Path.GetDirectoryName(spritesheetPathTextBox.Text);
                        var name = Path.GetFileNameWithoutExtension(spritesheetPathTextBox.Text);
                        outputPathTextBox.Text = Path.Combine(dir ?? "", $"{name}_atlas.xml");
                    }

                    ApplyZoom();
                    mainStatusLabel.Text = $"Loaded: {_spritesheetImage.Width}x{_spritesheetImage.Height} image";
                }
                else
                {
                    _spritesheetImage?.Dispose();
                    _spritesheetImage = null;
                    spritesheetPictureBox.Image = null;
                    mainStatusLabel.Text = "No image loaded";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                mainStatusLabel.Text = "Error loading image";
            }
        }

        private void ApplyZoom()
        {
            if (_spritesheetImage == null) return;

            var zoom = zoomTrackBar.Value / 100.0f;
            var newSize = new Size(
                (int)(_spritesheetImage.Width * zoom), 
                (int)(_spritesheetImage.Height * zoom)
            );
            
            spritesheetPictureBox.Size = newSize;
            RefreshPreview();
        }

        private void RefreshPreview()
        {
            spritesheetPictureBox.Invalidate();
        }

        private void DrawPreviewOverlay(Graphics graphics)
        {
            if (_spritesheetImage == null) return;

            var zoom = zoomTrackBar.Value / 100.0f;

            // Draw grid if enabled
            if (showGridCheckBox.Checked && gridWidthNumeric.Value > 0 && gridHeightNumeric.Value > 0)
            {
                using var gridPen = new Pen(Color.Red, 1.0f);
                var gridWidth = (int)gridWidthNumeric.Value;
                var gridHeight = (int)gridHeightNumeric.Value;

                // Vertical lines
                for (int x = 0; x < _spritesheetImage.Width; x += gridWidth)
                {
                    graphics.DrawLine(gridPen, x * zoom, 0, x * zoom, _spritesheetImage.Height * zoom);
                }

                // Horizontal lines
                for (int y = 0; y < _spritesheetImage.Height; y += gridHeight)
                {
                    graphics.DrawLine(gridPen, 0, y * zoom, _spritesheetImage.Width * zoom, y * zoom);
                }
            }

            // Draw detected regions if enabled
            if (showRegionsCheckBox.Checked && _detectedRegions != null)
            {
                using var regionPen = new Pen(Color.Lime, 2.0f);
                using var textBrush = new SolidBrush(Color.Yellow);
                using var font = new Font("Arial", 8);

                foreach (var region in _detectedRegions)
                {
                    var rect = new RectangleF(
                        region.Bounds.X * zoom,
                        region.Bounds.Y * zoom,
                        region.Bounds.Width * zoom,
                        region.Bounds.Height * zoom);

                    graphics.DrawRectangle(regionPen, Rectangle.Round(rect));
                    graphics.DrawString(region.Name, font, textBrush, rect.Location);
                }
            }
        }

        private void ApplyPreset(string presetName)
        {
            switch (presetName)
            {
                case "RPG Character 32x32":
                    gridWidthNumeric.Value = 32;
                    gridHeightNumeric.Value = 32;
                    namingPatternTextBox.Text = "Character_{row}_{col}";
                    trimTransparencyCheckBox.Checked = true;
                    transparencyThresholdNumeric.Value = 0;
                    break;

                case "RPG Character 48x48":
                    gridWidthNumeric.Value = 48;
                    gridHeightNumeric.Value = 48;
                    namingPatternTextBox.Text = "Character_{row}_{col}";
                    trimTransparencyCheckBox.Checked = true;
                    transparencyThresholdNumeric.Value = 0;
                    break;

                case "Weapon Icons 64x64":
                    gridWidthNumeric.Value = 64;
                    gridHeightNumeric.Value = 64;
                    namingPatternTextBox.Text = "Weapon_{row}_{col}";
                    trimTransparencyCheckBox.Checked = true;
                    transparencyThresholdNumeric.Value = 0;
                    break;

                case "Item Icons 32x32":
                    gridWidthNumeric.Value = 32;
                    gridHeightNumeric.Value = 32;
                    namingPatternTextBox.Text = "Item_{row}_{col}";
                    trimTransparencyCheckBox.Checked = true;
                    transparencyThresholdNumeric.Value = 0;
                    break;

                case "Tileset 16x16":
                    gridWidthNumeric.Value = 16;
                    gridHeightNumeric.Value = 16;
                    namingPatternTextBox.Text = "Tile_{row}_{col}";
                    trimTransparencyCheckBox.Checked = false;
                    transparencyThresholdNumeric.Value = 0;
                    break;

                case "UI Elements":
                    gridWidthNumeric.Value = 0; // Auto-detect
                    gridHeightNumeric.Value = 0;
                    namingPatternTextBox.Text = "UI_{row}_{col}";
                    trimTransparencyCheckBox.Checked = true;
                    transparencyThresholdNumeric.Value = 10;
                    break;
            }

            RefreshPreview();
            mainStatusLabel.Text = $"Applied preset: {presetName}";
        }

        private void UpdateUI()
        {
            if (_currentConfig == null) return;

            spritesheetPathTextBox.Text = _currentConfig.SpritesheetPath ?? "";
            outputPathTextBox.Text = _currentConfig.OutputPath ?? "";
            texturePathTextBox.Text = _currentConfig.TexturePath ?? "";
            gridWidthNumeric.Value = _currentConfig.GridWidth;
            gridHeightNumeric.Value = _currentConfig.GridHeight;
            namingPatternTextBox.Text = _currentConfig.NamingPattern ?? "";
            trimTransparencyCheckBox.Checked = _currentConfig.TrimTransparency;
            transparencyThresholdNumeric.Value = _currentConfig.TransparencyThreshold;

            RefreshAnimationList();
        }

        private void UpdateConfigFromUI()
        {
            if (_currentConfig == null) return;

            _currentConfig.SpritesheetPath = spritesheetPathTextBox.Text;
            _currentConfig.OutputPath = outputPathTextBox.Text;
            _currentConfig.TexturePath = texturePathTextBox.Text;
            _currentConfig.GridWidth = (int)gridWidthNumeric.Value;
            _currentConfig.GridHeight = (int)gridHeightNumeric.Value;
            _currentConfig.NamingPattern = namingPatternTextBox.Text;
            _currentConfig.TrimTransparency = trimTransparencyCheckBox.Checked;
            _currentConfig.TransparencyThreshold = (int)transparencyThresholdNumeric.Value;
        }

        private void RefreshAnimationList()
        {
            animationListView.Items.Clear();

            if (_currentConfig?.Animations == null) return;

            foreach (var animation in _currentConfig.Animations)
            {
                var item = new ListViewItem(animation.Name ?? "");
                item.SubItems.Add(animation.DelayMs.ToString());
                item.SubItems.Add(animation.FrameNames?.Count.ToString() ?? "0");
                item.SubItems.Add(animation.Pattern ?? "");
                animationListView.Items.Add(item);
            }
        }

        // Menu/Toolbar Actions
        private void NewConfiguration()
        {
            _currentConfig = AtlasConfigManager.CreateSampleConfig("", "", "");
            UpdateUI();
            mainStatusLabel.Text = "New configuration created";
        }

        private void LoadConfiguration()
        {
            using var dialog = new OpenFileDialog()
            {
                Title = "Load Atlas Configuration",
                Filter = "JSON Files|*.json|All Files|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    _currentConfig = AtlasConfigManager.LoadConfig(dialog.FileName);
                    UpdateUI();
                    LoadSpritesheetImage();
                    mainStatusLabel.Text = $"Configuration loaded: {Path.GetFileName(dialog.FileName)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveConfiguration()
        {
            if (string.IsNullOrEmpty(_currentConfig?.SpritesheetPath))
            {
                SaveConfigurationAs();
                return;
            }

            UpdateConfigFromUI();

            var configPath = Path.ChangeExtension(_currentConfig.SpritesheetPath, ".json");
            
            try
            {
                AtlasConfigManager.SaveConfig(_currentConfig, configPath);
                mainStatusLabel.Text = $"Configuration saved: {Path.GetFileName(configPath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveConfigurationAs()
        {
            using var dialog = new SaveFileDialog()
            {
                Title = "Save Atlas Configuration",
                Filter = "JSON Files|*.json|All Files|*.*",
                DefaultExt = "json"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    UpdateConfigFromUI();
                    AtlasConfigManager.SaveConfig(_currentConfig, dialog.FileName);
                    mainStatusLabel.Text = $"Configuration saved: {Path.GetFileName(dialog.FileName)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async Task GenerateAtlas()
        {
            if (_isGenerating) return;

            try
            {
                _isGenerating = true;
                generateButton.Enabled = false;
                mainProgressBar.Visible = true;
                mainStatusLabel.Text = "Generating atlas...";

                UpdateConfigFromUI();

                if (string.IsNullOrEmpty(_currentConfig.SpritesheetPath) || !File.Exists(_currentConfig.SpritesheetPath))
                {
                    MessageBox.Show("Please select a valid spritesheet image.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(_currentConfig.OutputPath))
                {
                    MessageBox.Show("Please specify an output path.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Generate on background thread
                await Task.Run(() =>
                {
                    var xml = AtlasGenerator.GenerateAtlas(_currentConfig);
                    
                    // Update UI on main thread
                    this.Invoke(() =>
                    {
                        xmlPreviewTextBox.Text = xml;
                        // Note: Region detection for preview would need additional implementation
                        RefreshPreview();
                    });
                });

                mainStatusLabel.Text = $"Atlas generated successfully!";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating atlas: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                mainStatusLabel.Text = "Error generating atlas";
            }
            finally
            {
                _isGenerating = false;
                generateButton.Enabled = true;
                mainProgressBar.Visible = false;
            }
        }

        private void PreviewRegions()
        {
            if (_spritesheetImage == null)
            {
                MessageBox.Show("Please load a spritesheet first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                mainStatusLabel.Text = "Detecting regions...";
                UpdateConfigFromUI();

                // Call the new method to get regions
                _detectedRegions = AtlasGenerator.DetectRegions(_currentConfig);

                // Refresh the picture box to draw the new regions
                RefreshPreview();
                
                mainStatusLabel.Text = $"Preview updated. Found {_detectedRegions.Count} regions.";
                xmlPreviewTextBox.Text = "(Region preview does not generate XML. Use 'Generate Atlas' for the final XML output.)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error previewing regions: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                mainStatusLabel.Text = "Error during preview.";
                _detectedRegions?.Clear();
                RefreshPreview();
            }
        }

        private void BatchProcess()
        {
            var dialog = new BatchProcessDialog();
            dialog.ShowDialog();
        }

        private void ShowAbout()
        {
            MessageBox.Show(
                "Atlas Generator v1.0\n\n" +
                "A tool for automatically generating texture atlas XML files from spritesheets.\n\n" +
                "Features:\n" +
                "- Grid-based sprite detection\n" +
                "- Transparency trimming\n" +
                "- Animation support\n" +
                "- Batch processing\n" +
                "- Configuration presets",
                "About Atlas Generator",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _spritesheetImage?.Dispose();
            base.OnFormClosed(e);
        }
    }
}