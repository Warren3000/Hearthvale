using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using System.Windows.Forms;
using Hearthvale.GameCode.Tools;

namespace Hearthvale.AtlasGeneratorUI
{
    public partial class MainForm
    {
        private static readonly JsonSerializerOptions NpcJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private sealed record NpcAnimationMetadata(string Name, string AtlasAnimation, float Fps, bool Loop);

        private sealed class NpcConfigContext
        {
            public NpcConfigContext(
                string configPath,
                string npcId,
                string displayName,
                string textureAtlasRelative,
                string textureAtlasAbsolute,
                string textureReference,
                string spritesheetRelative,
                string spritesheetAbsolute,
                List<AtlasGenerator.SpriteRegion> regions,
                List<AtlasGenerator.AnimationConfig> animations,
                Dictionary<string, NpcAnimationMetadata> metadata)
            {
                ConfigPath = configPath;
                NpcId = npcId;
                DisplayName = displayName;
                TextureAtlasRelative = textureAtlasRelative;
                TextureAtlasAbsolute = textureAtlasAbsolute;
                TextureReference = textureReference;
                SpritesheetRelative = spritesheetRelative;
                SpritesheetAbsolute = spritesheetAbsolute;
                Regions = new List<AtlasGenerator.SpriteRegion>(regions);
                AnimationConfigs = new List<AtlasGenerator.AnimationConfig>(animations);
                AnimationMetadata = metadata;
                AtlasConfig = new AtlasGenerator.AtlasConfig
                {
                    SpritesheetPath = SpritesheetAbsolute,
                    OutputPath = TextureAtlasAbsolute,
                    TexturePath = TextureReference,
                    GridWidth = 0,
                    GridHeight = 0,
                    NamingPattern = "{name}_{index}",
                    TrimTransparency = false,
                    TransparencyThreshold = 0,
                    IsGridBased = false,
                    Animations = AnimationConfigs
                };
            }

            public string ConfigPath { get; }
            public string NpcId { get; }
            public string DisplayName { get; }
            public string TextureAtlasRelative { get; }
            public string TextureAtlasAbsolute { get; }
            public string TextureReference { get; }
            public string SpritesheetRelative { get; }
            public string SpritesheetAbsolute { get; }
            public List<AtlasGenerator.SpriteRegion> Regions { get; }
            public List<AtlasGenerator.AnimationConfig> AnimationConfigs { get; }
            public IReadOnlyDictionary<string, NpcAnimationMetadata> AnimationMetadata { get; }
            public AtlasGenerator.AtlasConfig AtlasConfig { get; }
        }

        private sealed class NpcAtlasFile
        {
            [JsonPropertyName("npcs")]
            public List<NpcDefinition>? Npcs { get; set; }
        }

        private sealed class NpcDefinition
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("displayName")]
            public string? DisplayName { get; set; }

            [JsonPropertyName("textureAtlas")]
            public string? TextureAtlas { get; set; }

            [JsonPropertyName("animations")]
            public Dictionary<string, NpcAnimationDefinition>? Animations { get; set; }
        }

        private sealed class NpcAnimationDefinition
        {
            [JsonPropertyName("atlasAnimation")]
            public string? AtlasAnimation { get; set; }

            [JsonPropertyName("fps")]
            public float? Fps { get; set; }

            [JsonPropertyName("loop")]
            public bool? Loop { get; set; }
        }

        private sealed class NpcListItem
        {
            public NpcListItem(NpcDefinition definition)
            {
                Definition = definition;
            }

            public NpcDefinition Definition { get; }

            public override string ToString()
            {
                if (!string.IsNullOrWhiteSpace(Definition.DisplayName))
                {
                    return Definition.DisplayName!;
                }

                return !string.IsNullOrWhiteSpace(Definition.Id)
                    ? Definition.Id!
                    : "(Unnamed NPC)";
            }
        }

        // Event Handlers
        private void MainForm_Load(object? sender, EventArgs e)
        {
            mainStatusLabel.Text = "Ready - Load a spritesheet to begin";
            UpdateLeftPanelWidth();
        }

        private void MainForm_Resize(object? sender, EventArgs e)
        {
            UpdateLeftPanelWidth();
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

            ConfigureDialogInitialPath(dialog, spritesheetPathTextBox.Text, selectFile: true, preferredContentSubdirectory: "images/sprites");

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                spritesheetPathTextBox.Text = NormalizePathForConfig(dialog.FileName);
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

            ConfigureDialogInitialPath(dialog, outputPathTextBox.Text, selectFile: true, preferredContentSubdirectory: "images/xml");

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                outputPathTextBox.Text = NormalizePathForConfig(dialog.FileName);
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
            UpdateGridSummary();
            RefreshPreview();
        }

        private void Margins_Changed(object? sender, EventArgs e)
        {
            UpdateGridSummary();
            RefreshPreview();
        }

        // Helper Methods
        private void LoadSpritesheetImage()
        {
            try
            {
                var rawPath = spritesheetPathTextBox.Text;
                var resolvedPath = ResolveAbsolutePath(rawPath);

                if (!string.IsNullOrEmpty(rawPath) && resolvedPath != null && File.Exists(resolvedPath))
                {
                    // Dispose previous image
                    _spritesheetImage?.Dispose();

                    _spritesheetImage = new Bitmap(resolvedPath);
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
                        outputPathTextBox.Text = NormalizeSlashes(Path.Combine(dir ?? string.Empty, $"{name}_atlas.xml"));
                    }

                    ApplyZoom();
                    mainStatusLabel.Text = $"Loaded: {_spritesheetImage.Width}x{_spritesheetImage.Height} image";
                }
                else
                {
                    _spritesheetImage?.Dispose();
                    _spritesheetImage = null;
                    spritesheetPictureBox.Image = null;
                    mainStatusLabel.Text = string.IsNullOrEmpty(rawPath)
                        ? "No image loaded"
                        : $"Spritesheet not found: {rawPath}";
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
                var marginLeft = (int)marginLeftNumeric.Value;
                var marginTop = (int)marginTopNumeric.Value;

                // Vertical lines
                for (int x = marginLeft; x < _spritesheetImage.Width; x += gridWidth)
                {
                    graphics.DrawLine(gridPen, x * zoom, 0, x * zoom, _spritesheetImage.Height * zoom);
                }

                // Horizontal lines
                for (int y = marginTop; y < _spritesheetImage.Height; y += gridHeight)
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
            marginLeftNumeric.Value = 0;
            marginTopNumeric.Value = 0;

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

            UpdateGridSummary();
            RefreshPreview();
            mainStatusLabel.Text = $"Applied preset: {presetName}";
        }

        private void UpdateUI()
        {
            if (_currentConfig == null) return;

            spritesheetPathTextBox.Text = NormalizePathForConfig(_currentConfig.SpritesheetPath);
            outputPathTextBox.Text = NormalizePathForConfig(_currentConfig.OutputPath);
            texturePathTextBox.Text = NormalizeSlashes(_currentConfig.TexturePath ?? string.Empty);
            SetNumericValue(gridWidthNumeric, _currentConfig.GridWidth);
            SetNumericValue(gridHeightNumeric, _currentConfig.GridHeight);
            SetNumericValue(marginLeftNumeric, _currentConfig.MarginLeft);
            SetNumericValue(marginTopNumeric, _currentConfig.MarginTop);
            namingPatternTextBox.Text = _currentConfig.NamingPattern ?? "";
            trimTransparencyCheckBox.Checked = _currentConfig.TrimTransparency;
            transparencyThresholdNumeric.Value = _currentConfig.TransparencyThreshold;

            UpdateGridSummary();
            RefreshAnimationList();
        }

        private void UpdateConfigFromUI()
        {
            if (_currentConfig == null) return;
            if (_isNpcConfig) return;

            var spriteResolved = ResolveAbsolutePath(spritesheetPathTextBox.Text);
            _currentConfig.SpritesheetPath = NormalizePathForConfig(spriteResolved ?? spritesheetPathTextBox.Text);

            var outputResolved = ResolveAbsolutePath(outputPathTextBox.Text, allowNonExisting: true);
            _currentConfig.OutputPath = NormalizePathForConfig(outputResolved ?? outputPathTextBox.Text);

            _currentConfig.TexturePath = NormalizeSlashes(texturePathTextBox.Text);
            _currentConfig.GridWidth = (int)gridWidthNumeric.Value;
            _currentConfig.GridHeight = (int)gridHeightNumeric.Value;
            _currentConfig.MarginLeft = (int)marginLeftNumeric.Value;
            _currentConfig.MarginTop = (int)marginTopNumeric.Value;
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
                var item = new ListViewItem(animation.Name ?? string.Empty);

                var frameCount = animation.FrameNames?.Count ?? (animation.FrameCount > 0 ? animation.FrameCount : 0);
                var delayText = animation.DelayMs.ToString(CultureInfo.InvariantCulture);
                var patternText = animation.Pattern ?? string.Empty;

                if (_isNpcConfig && _npcContext != null && !string.IsNullOrEmpty(animation.Name) &&
                    _npcContext.AnimationMetadata.TryGetValue(animation.Name, out var metadata))
                {
                    delayText = string.Format(CultureInfo.InvariantCulture, "{0:0.##} fps ({1} ms)", metadata.Fps, animation.DelayMs);
                    patternText = metadata.AtlasAnimation;
                }

                item.SubItems.Add(delayText);
                item.SubItems.Add(frameCount.ToString(CultureInfo.InvariantCulture));
                item.SubItems.Add(patternText);
                animationListView.Items.Add(item);
            }
        }

        private bool TryLoadNpcConfiguration(string configPath)
        {
            string json;
            try
            {
                json = File.ReadAllText(configPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(json);
            }
            catch (JsonException)
            {
                return false;
            }

            using (document)
            {
                if (!document.RootElement.TryGetProperty("npcs", out var npcsElement) ||
                    npcsElement.ValueKind != JsonValueKind.Array ||
                    npcsElement.GetArrayLength() == 0)
                {
                    return false;
                }
            }

            NpcAtlasFile? npcFile;
            try
            {
                npcFile = JsonSerializer.Deserialize<NpcAtlasFile>(json, NpcJsonOptions);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to parse NPC configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }

            if (npcFile?.Npcs == null || npcFile.Npcs.Count == 0)
            {
                MessageBox.Show("NPC configuration does not contain any NPC entries.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }

            var npc = SelectNpcDefinition(this, npcFile.Npcs);
            if (npc == null)
            {
                return true;
            }

            try
            {
                var context = BuildNpcContext(configPath, npc);
                ApplyNpcContext(context);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load NPC configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return true;
        }

        private void ApplyNpcContext(NpcConfigContext context)
        {
            _npcContext = context;
            _isNpcConfig = true;
            _currentConfigPath = context.ConfigPath;
            _currentConfig = context.AtlasConfig;
            _detectedRegions = context.Regions.Select(region => new AtlasGenerator.SpriteRegion
            {
                Name = region.Name,
                Bounds = region.Bounds,
                ContentBounds = region.ContentBounds
            }).ToList();

            UpdateUI();
            LoadSpritesheetImage();
            RefreshPreview();

            try
            {
                xmlPreviewTextBox.Text = File.ReadAllText(context.TextureAtlasAbsolute);
            }
            catch
            {
                xmlPreviewTextBox.Text = string.Empty;
            }

            spriteCountLabel.Text = string.Format(
                CultureInfo.InvariantCulture,
                "{0} regions • {1} animations",
                _detectedRegions.Count,
                context.AnimationConfigs.Count);

            mainStatusLabel.Text = string.Format(
                CultureInfo.InvariantCulture,
                "Loaded NPC config: {0} ({1})",
                context.DisplayName,
                context.NpcId);

            generateButton.Enabled = false;
            saveConfigButton.Enabled = false;
        }

        private NpcConfigContext BuildNpcContext(string configPath, NpcDefinition npc)
        {
            var contentRoot = GetContentRoot();
            if (contentRoot == null)
            {
                throw new InvalidOperationException("Unable to locate the Hearthvale Content directory.");
            }

            var textureAtlasRaw = NormalizeSlashes(npc.TextureAtlas ?? string.Empty);
            if (string.IsNullOrEmpty(textureAtlasRaw))
            {
                throw new InvalidOperationException("NPC definition is missing a textureAtlas value.");
            }

            var textureAtlasAbsolute = ResolveContentRelativeFile(textureAtlasRaw, ".xml");
            if (textureAtlasAbsolute == null || !File.Exists(textureAtlasAbsolute))
            {
                throw new FileNotFoundException($"Texture atlas not found: {textureAtlasRaw}");
            }

            var atlasDocument = XDocument.Load(textureAtlasAbsolute);
            var textureElement = atlasDocument.Root?.Element("Texture");
            if (textureElement == null)
            {
                throw new InvalidOperationException("Texture atlas is missing a <Texture> element.");
            }

            var textureReference = NormalizeSlashes((textureElement.Value ?? string.Empty).Trim());
            if (string.IsNullOrEmpty(textureReference))
            {
                throw new InvalidOperationException("Texture atlas has an empty texture reference.");
            }

            var spritesheetAbsolute = ResolveContentRelativeFile(textureReference, ".png", ".jpg", ".jpeg", ".bmp");
            if (spritesheetAbsolute == null || !File.Exists(spritesheetAbsolute))
            {
                throw new FileNotFoundException($"Spritesheet not found for texture reference: {textureReference}");
            }

            var regionsElement = atlasDocument.Root?.Element("Regions");
            var regions = new List<AtlasGenerator.SpriteRegion>();
            var regionLookup = new Dictionary<string, AtlasGenerator.SpriteRegion>(StringComparer.OrdinalIgnoreCase);

            if (regionsElement != null)
            {
                foreach (var regionElement in regionsElement.Elements("Region"))
                {
                    var name = regionElement.Attribute("name")?.Value;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    if (!int.TryParse(regionElement.Attribute("x")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) ||
                        !int.TryParse(regionElement.Attribute("y")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var y) ||
                        !int.TryParse(regionElement.Attribute("width")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var width) ||
                        !int.TryParse(regionElement.Attribute("height")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var height))
                    {
                        continue;
                    }

                    var bounds = new Rectangle(x, y, width, height);
                    var region = new AtlasGenerator.SpriteRegion
                    {
                        Name = name,
                        Bounds = bounds,
                        ContentBounds = bounds
                    };

                    regions.Add(region);
                    regionLookup[name] = region;
                }
            }

            if (regions.Count == 0)
            {
                throw new InvalidOperationException("No regions were found in the texture atlas XML.");
            }

            var animations = new List<AtlasGenerator.AnimationConfig>();
            var metadata = new Dictionary<string, NpcAnimationMetadata>(StringComparer.OrdinalIgnoreCase);

            if (npc.Animations != null)
            {
                foreach (var pair in npc.Animations)
                {
                    var animationName = pair.Key;
                    var definition = pair.Value;
                    if (definition?.AtlasAnimation == null)
                    {
                        continue;
                    }

                    var atlasAnimation = definition.AtlasAnimation.Trim();
                    if (string.IsNullOrEmpty(atlasAnimation))
                    {
                        continue;
                    }

                    var frameNames = CollectFramesForAnimation(atlasAnimation, regionLookup);
                    if (frameNames.Count == 0)
                    {
                        continue;
                    }

                    var fps = definition.Fps.HasValue && definition.Fps.Value > 0 ? definition.Fps.Value : 10f;
                    var loop = definition.Loop ?? true;

                    var animationConfig = new AtlasGenerator.AnimationConfig
                    {
                        Name = animationName,
                        DelayMs = CalculateDelayMs(fps),
                        FrameNames = frameNames,
                        FrameCount = frameNames.Count,
                        StartFrame = 0,
                        Pattern = atlasAnimation
                    };

                    animations.Add(animationConfig);
                    metadata[animationName] = new NpcAnimationMetadata(animationName, atlasAnimation, fps, loop);
                }
            }

            var textureAtlasRelative = NormalizeSlashes(GetRelativePathIfUnder(textureAtlasAbsolute, contentRoot) ?? textureAtlasRaw);
            var spritesheetRelative = NormalizeSlashes(GetRelativePathIfUnder(spritesheetAbsolute, contentRoot) ?? textureReference);

            var npcId = npc.Id ?? Path.GetFileNameWithoutExtension(configPath);
            var npcDisplayName = string.IsNullOrWhiteSpace(npc.DisplayName) ? npcId : npc.DisplayName!;

            return new NpcConfigContext(
                configPath,
                npcId,
                npcDisplayName,
                textureAtlasRelative,
                textureAtlasAbsolute,
                textureReference,
                spritesheetRelative,
                spritesheetAbsolute,
                regions,
                animations,
                metadata);
        }

        private static NpcDefinition? SelectNpcDefinition(IWin32Window owner, IList<NpcDefinition> definitions)
        {
            if (definitions == null || definitions.Count == 0)
            {
                return null;
            }

            if (definitions.Count == 1)
            {
                return definitions[0];
            }

            using var dialog = new Form
            {
                Text = "Select NPC",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ClientSize = new Size(360, 240)
            };

            var listBox = new ListBox
            {
                Dock = DockStyle.Fill
            };

            foreach (var definition in definitions)
            {
                listBox.Items.Add(new NpcListItem(definition));
            }

            if (listBox.Items.Count > 0)
            {
                listBox.SelectedIndex = 0;
            }

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 5, 5, 5),
                AutoSize = true
            };

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Width = 75
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Width = 75
            };

            buttonPanel.Controls.Add(okButton);
            buttonPanel.Controls.Add(cancelButton);

            dialog.Controls.Add(listBox);
            dialog.Controls.Add(buttonPanel);
            dialog.AcceptButton = okButton;
            dialog.CancelButton = cancelButton;

            listBox.DoubleClick += (_, _) => dialog.DialogResult = DialogResult.OK;

            return dialog.ShowDialog(owner) == DialogResult.OK && listBox.SelectedItem is NpcListItem item
                ? item.Definition
                : null;
        }

        private string? ResolveContentRelativeFile(string relativePath, params string[] candidateExtensions)
        {
            var contentRoot = GetContentRoot();
            if (contentRoot == null || string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);

            if (Path.IsPathRooted(normalized))
            {
                if (File.Exists(normalized))
                {
                    return Path.GetFullPath(normalized);
                }

                if (!Path.HasExtension(normalized))
                {
                    foreach (var extension in candidateExtensions)
                    {
                        var candidate = normalized + extension;
                        if (File.Exists(candidate))
                        {
                            return Path.GetFullPath(candidate);
                        }
                    }
                }

                return null;
            }

            var fullPath = Path.Combine(contentRoot, normalized);
            if (File.Exists(fullPath))
            {
                return Path.GetFullPath(fullPath);
            }

            if (!Path.HasExtension(normalized))
            {
                foreach (var extension in candidateExtensions)
                {
                    var candidate = Path.Combine(contentRoot, normalized + extension);
                    if (File.Exists(candidate))
                    {
                        return Path.GetFullPath(candidate);
                    }
                }
            }

            return null;
        }

        private static List<string> CollectFramesForAnimation(string atlasAnimation, Dictionary<string, AtlasGenerator.SpriteRegion> regions)
        {
            return regions.Keys
                .Where(name => FrameMatchesAtlasAnimation(name, atlasAnimation))
                .Select(name => new { name, index = ExtractFrameIndex(name, atlasAnimation) })
                .OrderBy(item => item.index)
                .ThenBy(item => item.name, StringComparer.OrdinalIgnoreCase)
                .Select(item => item.name)
                .ToList();
        }

        private static bool FrameMatchesAtlasAnimation(string regionName, string atlasAnimation)
        {
            if (regionName.Equals(atlasAnimation, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var expectedPrefix = atlasAnimation + "_";
            return regionName.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase);
        }

        private static int ExtractFrameIndex(string regionName, string atlasAnimation)
        {
            if (regionName.Length <= atlasAnimation.Length)
            {
                return 0;
            }

            var suffix = regionName.Substring(atlasAnimation.Length).TrimStart('_');
            return int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
                ? index
                : int.MaxValue;
        }

        private static int CalculateDelayMs(float fps)
        {
            if (fps <= 0)
            {
                return 100;
            }

            var delay = (int)Math.Round(1000f / fps);
            return Math.Max(delay, 1);
        }

        private AtlasGenerator.AtlasConfig? BuildRuntimeConfig()
        {
            if (_currentConfig == null)
            {
                return null;
            }

            var spritesheetAbsolute = ResolveAbsolutePath(_currentConfig.SpritesheetPath);
            if (string.IsNullOrEmpty(spritesheetAbsolute) || !File.Exists(spritesheetAbsolute))
            {
                return null;
            }

            var outputAbsolute = ResolveAbsolutePath(_currentConfig.OutputPath, allowNonExisting: true);
            if (string.IsNullOrEmpty(outputAbsolute))
            {
                return null;
            }

            var outputDirectory = Path.GetDirectoryName(outputAbsolute);
            if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var textureReference = NormalizeTextureReference(_currentConfig.TexturePath);
            if (string.IsNullOrEmpty(textureReference))
            {
                textureReference = NormalizeSlashes(Path.GetFileName(spritesheetAbsolute));
            }

            return new AtlasGenerator.AtlasConfig
            {
                SpritesheetPath = spritesheetAbsolute,
                OutputPath = outputAbsolute,
                TexturePath = textureReference,
                GridWidth = _currentConfig.GridWidth,
                GridHeight = _currentConfig.GridHeight,
                MarginLeft = _currentConfig.MarginLeft,
                MarginTop = _currentConfig.MarginTop,
                NamingPattern = _currentConfig.NamingPattern,
                TrimTransparency = _currentConfig.TrimTransparency,
                TransparencyThreshold = _currentConfig.TransparencyThreshold,
                IsGridBased = _currentConfig.IsGridBased,
                Animations = CloneAnimations(_currentConfig.Animations)
            };
        }

        private static void SetNumericValue(NumericUpDown control, int value)
        {
            if (control == null)
            {
                return;
            }

            var decimalValue = (decimal)value;
            var clamped = Math.Min(control.Maximum, Math.Max(control.Minimum, decimalValue));
            control.Value = clamped;
        }

        private static List<AtlasGenerator.AnimationConfig> CloneAnimations(List<AtlasGenerator.AnimationConfig>? animations)
        {
            if (animations == null || animations.Count == 0)
            {
                return new List<AtlasGenerator.AnimationConfig>();
            }

            var clones = new List<AtlasGenerator.AnimationConfig>(animations.Count);
            foreach (var animation in animations)
            {
                clones.Add(new AtlasGenerator.AnimationConfig
                {
                    Name = animation.Name,
                    DelayMs = animation.DelayMs,
                    FrameCount = animation.FrameCount,
                    FrameNames = animation.FrameNames != null ? new List<string>(animation.FrameNames) : new List<string>(),
                    Pattern = animation.Pattern,
                    StartFrame = animation.StartFrame
                });
            }

            return clones;
        }

        private string NormalizeTextureReference(string? texturePath)
        {
            if (string.IsNullOrWhiteSpace(texturePath))
            {
                return string.Empty;
            }

            var trimmed = texturePath.Trim();
            if (Path.IsPathRooted(trimmed))
            {
                var contentRoot = GetContentRoot();
                if (contentRoot != null)
                {
                    var relative = GetRelativePathIfUnder(trimmed, contentRoot);
                    if (!string.IsNullOrEmpty(relative))
                    {
                        return NormalizeSlashes(relative);
                    }
                }

                return NormalizeSlashes(Path.GetFileName(trimmed));
            }

            return NormalizeSlashes(trimmed);
        }

        private string NormalizePathForConfig(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            var trimmed = path.Trim();
            string? absoluteCandidate = null;

            if (Path.IsPathRooted(trimmed))
            {
                absoluteCandidate = Path.GetFullPath(trimmed);
            }
            else
            {
                absoluteCandidate = ResolveAbsolutePath(trimmed, allowNonExisting: true);
            }

            var contentRoot = GetContentRoot();
            if (!string.IsNullOrEmpty(absoluteCandidate) && contentRoot != null)
            {
                var relativeToContent = GetRelativePathIfUnder(absoluteCandidate, contentRoot);
                if (!string.IsNullOrEmpty(relativeToContent) && relativeToContent != ".")
                {
                    return NormalizeSlashes(relativeToContent);
                }
            }

            var repoRoot = FindRepositoryRoot();
            if (!string.IsNullOrEmpty(absoluteCandidate) && repoRoot != null)
            {
                var relativeToRepo = GetRelativePathIfUnder(absoluteCandidate, repoRoot.FullName);
                if (!string.IsNullOrEmpty(relativeToRepo) && relativeToRepo != ".")
                {
                    return NormalizeSlashes(relativeToRepo);
                }
            }

            if (!string.IsNullOrEmpty(_currentConfigPath))
            {
                var configDir = Path.GetDirectoryName(_currentConfigPath);
                if (!string.IsNullOrEmpty(configDir) && !string.IsNullOrEmpty(absoluteCandidate))
                {
                    var relativeToConfig = Path.GetRelativePath(configDir, absoluteCandidate);
                    if (!string.IsNullOrEmpty(relativeToConfig) && relativeToConfig != ".")
                    {
                        return NormalizeSlashes(relativeToConfig);
                    }
                }
            }

            return NormalizeSlashes(trimmed);
        }

        private string NormalizeSlashes(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            return path.Replace('\\', '/');
        }

        private void ConfigureDialogInitialPath(FileDialog dialog, string? pathText, bool selectFile, string? preferredContentSubdirectory = null)
        {
            if (dialog == null)
            {
                return;
            }

            string? resolved = ResolveAbsolutePath(pathText, allowNonExisting: true);

            if (!string.IsNullOrEmpty(resolved))
            {
                if (Directory.Exists(resolved))
                {
                    dialog.InitialDirectory = resolved;
                }
                else
                {
                    var directory = Path.GetDirectoryName(resolved);
                    if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                    {
                        dialog.InitialDirectory = directory;
                    }

                    if (selectFile)
                    {
                        dialog.FileName = resolved;
                    }
                }

                if (!string.IsNullOrEmpty(dialog.InitialDirectory))
                {
                    return;
                }
            }

            string? fallbackDir = null;

            if (!string.IsNullOrEmpty(preferredContentSubdirectory))
            {
                var contentRoot = GetContentRoot();
                if (contentRoot != null)
                {
                    var preferred = Path.Combine(contentRoot, preferredContentSubdirectory.Replace('/', Path.DirectorySeparatorChar));
                    if (Directory.Exists(preferred))
                    {
                        fallbackDir = preferred;
                    }
                }
            }

            if (string.IsNullOrEmpty(fallbackDir) && !string.IsNullOrEmpty(_currentConfigPath))
            {
                fallbackDir = Path.GetDirectoryName(_currentConfigPath);
            }

            if (string.IsNullOrEmpty(fallbackDir))
            {
                fallbackDir = GetContentRoot();
            }

            if (!string.IsNullOrEmpty(fallbackDir) && Directory.Exists(fallbackDir))
            {
                dialog.InitialDirectory = fallbackDir;
            }
        }

        private void UpdateGridSummary()
        {
            if (gridDisplayLabel == null)
            {
                return;
            }

            gridDisplayLabel.Text = $"Grid: {gridWidthNumeric.Value}x{gridHeightNumeric.Value} px | Margin: {marginLeftNumeric.Value}px left, {marginTopNumeric.Value}px top";
        }

        private string? ResolveAbsolutePath(string? path, bool allowNonExisting = false)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var trimmed = path.Trim();
            var normalized = trimmed.Replace('/', Path.DirectorySeparatorChar);

            if (Path.IsPathRooted(normalized))
            {
                var full = Path.GetFullPath(normalized);
                if (allowNonExisting || File.Exists(full) || Directory.Exists(full))
                {
                    return full;
                }

                return null;
            }

            var searchRoots = new List<string?>();

            if (!string.IsNullOrEmpty(_currentConfigPath))
            {
                var configDir = Path.GetDirectoryName(_currentConfigPath);
                searchRoots.Add(configDir);
            }

            var repoRoot = FindRepositoryRoot();
            var contentRoot = GetContentRoot();

            if (contentRoot != null)
            {
                searchRoots.Add(contentRoot);
            }

            if (repoRoot != null)
            {
                searchRoots.Add(Path.Combine(repoRoot.FullName, "Hearthvale"));
                searchRoots.Add(repoRoot.FullName);
            }

            searchRoots.Add(AppDomain.CurrentDomain.BaseDirectory);

            foreach (var baseDir in searchRoots.Where(dir => !string.IsNullOrEmpty(dir)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(baseDir) || !Directory.Exists(baseDir))
                {
                    continue;
                }

                var candidate = Path.GetFullPath(Path.Combine(baseDir, normalized));
                if (allowNonExisting || File.Exists(candidate) || Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            if (!allowNonExisting && contentRoot != null)
            {
                var fileName = Path.GetFileName(normalized);
                if (!string.IsNullOrEmpty(fileName))
                {
                    var matches = Directory.GetFiles(contentRoot, fileName, SearchOption.AllDirectories);
                    if (matches.Length > 0)
                    {
                        return matches[0];
                    }
                }
            }

            if (allowNonExisting && contentRoot != null)
            {
                return Path.GetFullPath(Path.Combine(contentRoot, normalized));
            }

            return null;
        }

        private string? GetContentRoot()
        {
            var repoRoot = FindRepositoryRoot();
            if (repoRoot == null)
            {
                return null;
            }

            var contentPath = Path.Combine(repoRoot.FullName, "Hearthvale", "Content");
            return Directory.Exists(contentPath) ? contentPath : null;
        }

        private static string? GetRelativePathIfUnder(string path, string basePath)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(basePath))
            {
                return null;
            }

            var fullPath = Path.GetFullPath(path);
            var fullBase = Path.GetFullPath(basePath);

            if (!fullPath.StartsWith(fullBase, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var relative = Path.GetRelativePath(fullBase, fullPath);
            return string.IsNullOrEmpty(relative) ? null : relative;
        }

        // Menu/Toolbar Actions
        private void NewConfiguration()
        {
            var nameInput = PromptForConfigurationName(this);
            if (string.IsNullOrWhiteSpace(nameInput))
            {
                return;
            }

            var sanitizedName = SanitizeConfigFileName(nameInput);
            if (string.IsNullOrEmpty(sanitizedName))
            {
                MessageBox.Show("Please provide a valid configuration name.", "Invalid Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var configDirectory = ResolveDefaultConfigDirectory();
            var fileName = sanitizedName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? sanitizedName
                : sanitizedName + ".json";

            var configPath = Path.Combine(configDirectory, fileName);
            var configExists = File.Exists(configPath);

            if (configExists)
            {
                var response = MessageBox.Show(
                    "A configuration with that name already exists. Load the existing configuration?",
                    "Configuration Exists",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (response != DialogResult.Yes)
                {
                    return;
                }
            }
            else
            {
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                var defaultSpritePath = Path.Combine(configDirectory, nameWithoutExtension + ".png");
                var defaultOutputPath = Path.Combine(configDirectory, nameWithoutExtension + ".xml");
                var textureReference = Path.GetFileName(defaultSpritePath);

                try
                {
                    var newConfig = AtlasConfigManager.CreateSampleConfig(defaultSpritePath, defaultOutputPath, textureReference);
                    AtlasConfigManager.SaveConfig(newConfig, configPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error creating configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            try
            {
                _currentConfig = AtlasConfigManager.LoadConfig(configPath);
                _isNpcConfig = false;
                _npcContext = null;
                _currentConfigPath = configPath;
                UpdateUI();
                LoadSpritesheetImage();
                xmlPreviewTextBox.Clear();
                mainStatusLabel.Text = configExists
                    ? $"Loaded configuration: {Path.GetFileName(configPath)}"
                    : $"New configuration created: {Path.GetFileName(configPath)}";
                generateButton.Enabled = true;
                saveConfigButton.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string? PromptForConfigurationName(IWin32Window owner)
        {
            using var prompt = new Form
            {
                Text = "New Configuration",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ClientSize = new Size(320, 130)
            };

            var label = new Label
            {
                AutoSize = true,
                Text = "Enter configuration name:",
                Location = new Point(12, 12)
            };

            var inputBox = new TextBox
            {
                Location = new Point(15, 40),
                Width = 280,
                Text = "atlas-config"
            };

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(140, 80),
                Width = 75
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(220, 80),
                Width = 75
            };

            prompt.Controls.Add(label);
            prompt.Controls.Add(inputBox);
            prompt.Controls.Add(okButton);
            prompt.Controls.Add(cancelButton);
            prompt.AcceptButton = okButton;
            prompt.CancelButton = cancelButton;

            return prompt.ShowDialog(owner) == DialogResult.OK ? inputBox.Text : null;
        }

        private static string SanitizeConfigFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            var trimmed = name.Trim();
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(trimmed.Where(ch => !invalidChars.Contains(ch)).ToArray());
            return sanitized.Trim().TrimEnd('.');
        }

        private static string ResolveDefaultConfigDirectory()
        {
            var repoRoot = FindRepositoryRoot();
            if (repoRoot != null)
            {
                var preferredPath = Path.Combine(repoRoot.FullName, "Hearthvale", "Content", "atlas-configs");
                Directory.CreateDirectory(preferredPath);
                return preferredPath;
            }

            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            while (directory != null)
            {
                var contentCandidate = Path.Combine(directory.FullName, "Content");
                if (Directory.Exists(contentCandidate))
                {
                    var atlasDir = Path.Combine(contentCandidate, "atlas-configs");
                    Directory.CreateDirectory(atlasDir);
                    return atlasDir;
                }

                var hearthvaleCandidate = Path.Combine(directory.FullName, "Hearthvale", "Content");
                if (Directory.Exists(hearthvaleCandidate))
                {
                    var atlasDir = Path.Combine(hearthvaleCandidate, "atlas-configs");
                    Directory.CreateDirectory(atlasDir);
                    return atlasDir;
                }

                directory = directory.Parent;
            }

            var fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "atlas-configs");
            Directory.CreateDirectory(fallback);
            return fallback;
        }

        private static DirectoryInfo? FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Hearthvale.sln")))
                {
                    return directory;
                }

                directory = directory.Parent;
            }

            return null;
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
                if (TryLoadNpcConfiguration(dialog.FileName))
                {
                    return;
                }

                try
                {
                    _currentConfig = AtlasConfigManager.LoadConfig(dialog.FileName);
                    _isNpcConfig = false;
                    _npcContext = null;
                    _currentConfigPath = dialog.FileName;
                    UpdateUI();
                    LoadSpritesheetImage();
                    xmlPreviewTextBox.Clear();
                    mainStatusLabel.Text = $"Configuration loaded: {Path.GetFileName(dialog.FileName)}";
                    generateButton.Enabled = true;
                    saveConfigButton.Enabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveConfiguration()
        {
            if (_currentConfig == null)
            {
                return;
            }

            if (_isNpcConfig)
            {
                MessageBox.Show("NPC configurations are read-only in this tool.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var configPath = _currentConfigPath;

            if (string.IsNullOrEmpty(configPath))
            {
                if (string.IsNullOrEmpty(_currentConfig.SpritesheetPath))
                {
                    SaveConfigurationAs();
                    return;
                }

                configPath = Path.ChangeExtension(_currentConfig.SpritesheetPath, ".json");
                _currentConfigPath = configPath;
            }

            UpdateConfigFromUI();

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
            if (_isNpcConfig)
            {
                MessageBox.Show("NPC configurations are read-only in this tool.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

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
                    _currentConfigPath = dialog.FileName;
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
            if (_isNpcConfig)
            {
                MessageBox.Show("Atlas generation is not available for NPC configurations.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                _isGenerating = true;
                generateButton.Enabled = false;
                mainProgressBar.Visible = true;
                mainStatusLabel.Text = "Generating atlas...";

                UpdateConfigFromUI();

                var runtimeConfig = BuildRuntimeConfig();
                if (runtimeConfig == null)
                {
                    MessageBox.Show("Please select a valid spritesheet image located under Hearthvale/Content/images/sprites.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

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
                    var xml = AtlasGenerator.GenerateAtlas(runtimeConfig);
                    
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

            if (_isNpcConfig && _npcContext != null)
            {
                _detectedRegions = _npcContext.Regions.Select(region => new AtlasGenerator.SpriteRegion
                {
                    Name = region.Name,
                    Bounds = region.Bounds,
                    ContentBounds = region.ContentBounds
                }).ToList();

                RefreshPreview();
                mainStatusLabel.Text = $"Preview loaded from atlas XML. Found {_detectedRegions.Count} regions.";
                try
                {
                    xmlPreviewTextBox.Text = File.Exists(_npcContext.TextureAtlasAbsolute)
                        ? File.ReadAllText(_npcContext.TextureAtlasAbsolute)
                        : string.Empty;
                }
                catch
                {
                    xmlPreviewTextBox.Text = string.Empty;
                }
                return;
            }

            try
            {
                mainStatusLabel.Text = "Detecting regions...";
                UpdateConfigFromUI();

                var runtimeConfig = BuildRuntimeConfig();
                if (runtimeConfig == null)
                {
                    MessageBox.Show("Please select a valid spritesheet image located under Hearthvale/Content/images/sprites.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Call the new method to get regions
                _detectedRegions = AtlasGenerator.DetectRegions(runtimeConfig);

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