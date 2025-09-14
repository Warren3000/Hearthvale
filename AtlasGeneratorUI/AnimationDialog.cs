using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Hearthvale.GameCode.Tools;

namespace Hearthvale.AtlasGeneratorUI
{
    public partial class AnimationDialog : Form
    {
        public AtlasGenerator.AnimationConfig Animation { get; private set; }
        private readonly List<AtlasGenerator.SpriteRegion> _allRegions;

        private TextBox nameTextBox = null!;
        private NumericUpDown frameDelayNumeric = null!;
        private NumericUpDown frameCountNumeric = null!;
        private TextBox framePatternTextBox = null!;
        private ListBox frameNamesListBox = null!;
        private TextBox frameNameTextBox = null!;
        private Button addFrameButton = null!;
        private Button removeFrameButton = null!;
        private Button generateNamesButton = null!;
        private Button okButton = null!;
        private Button cancelButton = null!;

        public AnimationDialog(List<AtlasGenerator.SpriteRegion> allRegions, AtlasGenerator.AnimationConfig? existingAnimation = null)
        {
            _allRegions = allRegions;
            InitializeComponent();
            
            if (existingAnimation != null)
            {
                Animation = new AtlasGenerator.AnimationConfig
                {
                    Name = existingAnimation.Name,
                    DelayMs = existingAnimation.DelayMs,
                    Pattern = existingAnimation.Pattern,
                    FrameNames = existingAnimation.FrameNames?.ToList() ?? new List<string>(),
                    FrameCount = existingAnimation.FrameCount
                };
                UpdateUI();
            }
            else
            {
                Animation = new AtlasGenerator.AnimationConfig
                {
                    Name = "NewAnimation",
                    DelayMs = 100,
                    Pattern = "",
                    FrameNames = new List<string>(),
                    FrameCount = 1
                };
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Animation Configuration";
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Padding = new Padding(10)
            };

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            for (int i = 0; i < 7; i++)
            {
                mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Button row

            int row = 0;

            // Animation Name
            mainLayout.Controls.Add(new Label { Text = "Name:", Anchor = AnchorStyles.Left }, 0, row);
            nameTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Text = "NewAnimation" };
            mainLayout.Controls.Add(nameTextBox, 1, row++);

            // Frame Delay
            mainLayout.Controls.Add(new Label { Text = "Frame Delay (ms):", Anchor = AnchorStyles.Left }, 0, row);
            frameDelayNumeric = new NumericUpDown 
            { 
                Anchor = AnchorStyles.Left,
                Minimum = 1,
                Maximum = 10000,
                Value = 100,
                Width = 80
            };
            mainLayout.Controls.Add(frameDelayNumeric, 1, row++);

            // Frame Count
            mainLayout.Controls.Add(new Label { Text = "Frame Count:", Anchor = AnchorStyles.Left }, 0, row);
            var frameCountPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0), Padding = new Padding(0) };
            frameCountNumeric = new NumericUpDown
            {
                Anchor = AnchorStyles.Left,
                Minimum = 1,
                Maximum = 999,
                Value = 1,
                Width = 80
            };
            generateNamesButton = new Button { Text = "Generate Names", AutoSize = true, Margin = new Padding(10, 0, 0, 0) };
            generateNamesButton.Click += GenerateFrameNames_Click;
            frameCountPanel.Controls.Add(frameCountNumeric);
            frameCountPanel.Controls.Add(generateNamesButton);
            mainLayout.Controls.Add(frameCountPanel, 1, row++);

            // Frame Pattern
            mainLayout.Controls.Add(new Label { Text = "Pattern (regex):", Anchor = AnchorStyles.Left }, 0, row);
            framePatternTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            mainLayout.Controls.Add(framePatternTextBox, 1, row++);

            // Frame Names
            mainLayout.Controls.Add(new Label { Text = "Frame Names:", Anchor = AnchorStyles.Left | AnchorStyles.Top }, 0, row);
            
            var framePanel = new Panel { Anchor = AnchorStyles.Left | AnchorStyles.Right, Height = 150 };
            
            var frameLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            frameLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            frameLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            frameLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var addPanel = new Panel { Dock = DockStyle.Top, Height = 30 };
            frameNameTextBox = new TextBox
            {
                Width = 200,
                Dock = DockStyle.Left
            };
            addFrameButton = new Button
            {
                Text = "Add",
                Width = 50,
                Height = 25,
                Dock = DockStyle.Right
            };
            addPanel.Controls.Add(frameNameTextBox);
            addPanel.Controls.Add(addFrameButton);

            frameNamesListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                SelectionMode = SelectionMode.One
            };

            removeFrameButton = new Button { Text = "Remove Selected", Dock = DockStyle.Bottom, Height = 25 };

            frameLayout.Controls.Add(addPanel, 0, 0);
            frameLayout.Controls.Add(frameNamesListBox, 0, 1);
            frameLayout.Controls.Add(removeFrameButton, 0, 2);

            framePanel.Controls.Add(frameLayout);
            mainLayout.Controls.Add(framePanel, 1, row++);

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 35
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Width = 75,
                Height = 25
            };

            okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Width = 75,
                Height = 25
            };

            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(okButton);

            var buttonContainer = new Panel { Dock = DockStyle.Bottom, Height = 40 };
            buttonContainer.Controls.Add(buttonPanel);
            mainLayout.Controls.Add(buttonContainer, 0, row);
            mainLayout.SetColumnSpan(buttonContainer, 2);

            this.Controls.Add(mainLayout);

            // Wire up events
            addFrameButton.Click += (s, e) => AddFrameName();
            removeFrameButton.Click += (s, e) => RemoveSelectedFrame();
            okButton.Click += (s, e) => SaveAnimation();
            frameNameTextBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) AddFrameName(); };
            framePatternTextBox.TextChanged += FramePatternTextBox_TextChanged;

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void GenerateFrameNames_Click(object? sender, EventArgs e)
        {
            var animName = nameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(animName))
            {
                MessageBox.Show("Please enter an animation name first.", "Name Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                nameTextBox.Focus();
                return;
            }

            var frameCount = (int)frameCountNumeric.Value;
            if (MessageBox.Show($"This will clear the current frame list and generate {frameCount} new frame names based on the pattern '{animName}_<n>'.\n\nContinue?", "Confirm Generation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                return;
            }

            frameNamesListBox.Items.Clear();
            for (int i = 1; i <= frameCount; i++)
            {
                frameNamesListBox.Items.Add($"{animName}_{i}");
            }
        }

        private void FramePatternTextBox_TextChanged(object? sender, EventArgs e)
        {
            UpdateMatchingFrames();
        }

        private void UpdateMatchingFrames()
        {
            frameNamesListBox.Items.Clear();
            var pattern = framePatternTextBox.Text;
            if (string.IsNullOrWhiteSpace(pattern) || _allRegions == null)
            {
                return;
            }

            try
            {
                var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var matchingFrames = _allRegions
                    .Where(r => regex.IsMatch(r.Name))
                    .OrderBy(r => r.Name)
                    .Select(r => r.Name);

                foreach (var frame in matchingFrames)
                {
                    frameNamesListBox.Items.Add(frame);
                }
            }
            catch (System.Text.RegularExpressions.RegexParseException)
            {
                // Ignore invalid regex patterns during typing
            }
        }




        private void UpdateUI()
        {
            if (Animation == null) return;

            nameTextBox.Text = Animation.Name ?? "";
            frameDelayNumeric.Value = Animation.DelayMs;
            frameCountNumeric.Value = Animation.FrameCount;
            framePatternTextBox.Text = Animation.Pattern ?? "";

            frameNamesListBox.Items.Clear();
            if (Animation.FrameNames != null)
            {
                foreach (var frameName in Animation.FrameNames)
                {
                    frameNamesListBox.Items.Add(frameName);
                }
            }
        }

        private void AddFrameName()
        {
            var frameName = frameNameTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(frameName))
            {
                frameNamesListBox.Items.Add(frameName);
                frameNameTextBox.Clear();
                frameNameTextBox.Focus();
            }
        }

        private void RemoveSelectedFrame()
        {
            if (frameNamesListBox.SelectedIndex >= 0)
            {
                frameNamesListBox.Items.RemoveAt(frameNamesListBox.SelectedIndex);
            }
        }

        private void SaveAnimation()
        {
            if (string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                MessageBox.Show("Please enter a name for the animation.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                nameTextBox.Focus();
                return;
            }

            Animation.Name = nameTextBox.Text.Trim();
            Animation.DelayMs = (int)frameDelayNumeric.Value;
            Animation.FrameCount = (int)frameCountNumeric.Value;
            Animation.Pattern = framePatternTextBox.Text.Trim();
            Animation.FrameNames = frameNamesListBox.Items.Cast<string>().ToList();
        }
    }
}