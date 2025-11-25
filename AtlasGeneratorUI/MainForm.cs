using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Hearthvale.GameCode.Tools;

namespace Hearthvale.AtlasGeneratorUI
{
    public partial class MainForm : Form
    {
        private const float LeftPanelRatio = 0.33f;
        private AtlasGenerator.AtlasConfig _currentConfig = null!;
        private string? _currentConfigPath;
        private Bitmap? _spritesheetImage;
        private List<AtlasGenerator.SpriteRegion> _detectedRegions = new();
        private bool _isGenerating = false;
        private bool _isNpcConfig;
        private NpcConfigContext? _npcContext;

        // UI Controls
        private MenuStrip menuStrip = null!;
        private ToolStrip toolStrip = null!;
        private StatusStrip statusStrip = null!;
        private ToolStripStatusLabel mainStatusLabel = null!;
        private ToolStripProgressBar mainProgressBar = null!;
        private ToolStripStatusLabel spriteCountLabel = null!;
        private ToolTip toolTip = null!;
        private SplitContainer mainSplitContainer = null!;
        private SplitContainer leftSplitContainer = null!;
        private SplitContainer rightSplitContainer = null!;
        private FlowLayoutPanel leftPanelFlow = null!;
        
        // Left panel controls
        private GroupBox configGroupBox = null!;
        private TextBox spritesheetPathTextBox = null!;
        private Button browseSpritesheetButton = null!;
        private TextBox outputPathTextBox = null!;
        private Button browseOutputButton = null!;
        private TextBox texturePathTextBox = null!;
        private NumericUpDown gridWidthNumeric = null!;
        private NumericUpDown gridHeightNumeric = null!;
        private NumericUpDown marginLeftNumeric = null!;
        private NumericUpDown marginTopNumeric = null!;
        private TextBox namingPatternTextBox = null!;
        private CheckBox trimTransparencyCheckBox = null!;
        private NumericUpDown transparencyThresholdNumeric = null!;
        private ComboBox presetComboBox = null!;
        private Button loadPresetButton = null!;
        
        // Preview panel
        private Panel previewPanel = null!;
        private PictureBox spritesheetPictureBox = null!;
        private CheckBox showGridCheckBox = null!;
        private CheckBox showRegionsCheckBox = null!;
        private TrackBar zoomTrackBar = null!;
        private Label zoomLabel = null!;
        private Label gridDisplayLabel = null!;
        
        // Animation panel
        private GroupBox animationGroupBox = null!;
        private ListView animationListView = null!;
        private Button addAnimationButton = null!;
        private Button editAnimationButton = null!;
        private Button removeAnimationButton = null!;
        
        // Output panel
        private GroupBox outputGroupBox = null!;
        private TextBox xmlPreviewTextBox = null!;
        private Button generateButton = null!;
        private Button saveConfigButton = null!;
        private Button loadConfigButton = null!;


        public MainForm()
        {
            InitializeComponent();
            InitializeConfig();
        }

        private void InitializeComponent()
        {
            this.Text = "Atlas Generator";
            this.ClientSize = new Size(1920, 1080);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1000, 600);

            // Initialize ToolTip component
            toolTip = new ToolTip()
            {
                AutoPopDelay = 5000,
                InitialDelay = 1000,
                ReshowDelay = 500,
                ShowAlways = true
            };

            CreateMenuStrip();
            CreateToolStrip();
            CreateStatusStrip();
            CreateMainLayout();
            CreateLeftPanel();
            CreatePreviewPanel();
            CreateRightPanel();
            
            mainSplitContainer.BringToFront();
            
            this.Load += MainForm_Load;
            this.Resize += MainForm_Resize;
        }

        private void CreateMenuStrip()
        {
            menuStrip = new MenuStrip();
            
            var fileMenu = new ToolStripMenuItem("&File");
            fileMenu.DropDownItems.Add("&New Configuration", null, (s, e) => NewConfiguration());
            fileMenu.DropDownItems.Add("&Open Configuration...", null, (s, e) => LoadConfiguration());
            fileMenu.DropDownItems.Add("&Save Configuration", null, (s, e) => SaveConfiguration());
            fileMenu.DropDownItems.Add("Save Configuration &As...", null, (s, e) => SaveConfigurationAs());
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("E&xit", null, (s, e) => this.Close());
            
            var toolsMenu = new ToolStripMenuItem("&Tools");
            toolsMenu.DropDownItems.Add("&Generate Atlas", null, async (s, e) => await GenerateAtlas());
            toolsMenu.DropDownItems.Add("&Preview Regions", null, (s, e) => PreviewRegions());
            toolsMenu.DropDownItems.Add(new ToolStripSeparator());
            toolsMenu.DropDownItems.Add("&Batch Process...", null, (s, e) => BatchProcess());
            
            var helpMenu = new ToolStripMenuItem("&Help");
            helpMenu.DropDownItems.Add("&About", null, (s, e) => ShowAbout());
            
            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(toolsMenu);
            menuStrip.Items.Add(helpMenu);
            
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        private void CreateToolStrip()
        {
            toolStrip = new ToolStrip()
            {
                Dock = DockStyle.Top
            };
            
            var newButton = new ToolStripButton("New", null, (s, e) => NewConfiguration()) { ToolTipText = "New Configuration" };
            var openButton = new ToolStripButton("Open", null, (s, e) => LoadConfiguration()) { ToolTipText = "Open Configuration" };
            var saveButton = new ToolStripButton("Save", null, (s, e) => SaveConfiguration()) { ToolTipText = "Save Configuration" };
            var separator1 = new ToolStripSeparator();
            var generateButton = new ToolStripButton("Generate", null, async (s, e) => await GenerateAtlas()) { ToolTipText = "Generate Atlas" };
            var previewButton = new ToolStripButton("Preview", null, (s, e) => PreviewRegions()) { ToolTipText = "Preview Regions" };
            
            toolStrip.Items.AddRange(new ToolStripItem[] { newButton, openButton, saveButton, separator1, generateButton, previewButton });
            this.Controls.Add(toolStrip);
        }

        private void CreateStatusStrip()
        {
            statusStrip = new StatusStrip();
            
            mainStatusLabel = new ToolStripStatusLabel("Ready") 
            { 
                Spring = true, 
                TextAlign = ContentAlignment.MiddleLeft 
            };
            
            spriteCountLabel = new ToolStripStatusLabel("No sprites detected") 
            { 
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched 
            };
            
            mainProgressBar = new ToolStripProgressBar() 
            { 
                Visible = false,
                Size = new Size(150, 16)
            };
            
            statusStrip.Items.AddRange(new ToolStripItem[] { mainStatusLabel, spriteCountLabel, mainProgressBar });
            this.Controls.Add(statusStrip);
        }

        private void CreateMainLayout()
        {
            mainSplitContainer = new SplitContainer()
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 450, // Increased from 300
                FixedPanel = FixedPanel.Panel1
            };
            
            leftSplitContainer = new SplitContainer()
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 400
            };
            
            rightSplitContainer = new SplitContainer()
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 400
            };
            
            mainSplitContainer.Panel1.Controls.Add(leftSplitContainer);
            mainSplitContainer.Panel2.Controls.Add(rightSplitContainer);
            this.Controls.Add(mainSplitContainer);
            UpdateLeftPanelWidth();
        }

        private void CreateLeftPanel()
        {
            leftPanelFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true,
                WrapContents = false,
                Padding = new Padding(10)
            };
            leftPanelFlow.Resize += (_, __) => LayoutLeftPanelSections();

            // Preset GroupBox
            var presetGroupBox = new GroupBox
            {
                Text = "Configuration Preset",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 0, 10)
            };
            var presetPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            presetPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            presetPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            presetPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            
            presetComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(3, 6, 3, 3) };
            loadPresetButton = new Button { Text = "Load", Size = new Size(100, 28), BackColor = Color.LightBlue, FlatStyle = FlatStyle.Flat, Anchor = AnchorStyles.None };
            loadPresetButton.FlatAppearance.BorderColor = Color.DarkBlue;
            loadPresetButton.Click += LoadPresetButton_Click;
            
            presetPanel.Controls.Add(new Label { Text = "Preset:", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 0);
            presetPanel.Controls.Add(presetComboBox, 1, 0);
            presetPanel.Controls.Add(loadPresetButton, 2, 0);
            presetGroupBox.Controls.Add(presetPanel);
            leftPanelFlow.Controls.Add(presetGroupBox);

            // File Paths GroupBox
            var filePathsGroupBox = new GroupBox
            {
                Text = "File Paths",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 0, 10)
            };
            var filePathsPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            filePathsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            filePathsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            filePathsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));

            spritesheetPathTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            browseSpritesheetButton = new Button { Text = "📁 Browse", Size = new Size(100, 28), BackColor = Color.LightGreen, FlatStyle = FlatStyle.Flat, Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold), Anchor = AnchorStyles.None };
            browseSpritesheetButton.FlatAppearance.BorderColor = Color.DarkGreen;
            outputPathTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            browseOutputButton = new Button { Text = "💾 Browse", Size = new Size(100, 28), BackColor = Color.LightCoral, FlatStyle = FlatStyle.Flat, Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold), Anchor = AnchorStyles.None };
            browseOutputButton.FlatAppearance.BorderColor = Color.DarkRed;
            texturePathTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };

            spritesheetPathTextBox.TextChanged += SpritesheetPathTextBox_TextChanged;
            browseSpritesheetButton.Click += BrowseSpritesheetButton_Click;
            browseOutputButton.Click += BrowseOutputButton_Click;

            filePathsPanel.Controls.Add(new Label { Text = "Source Spritesheet:", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 0);
            filePathsPanel.Controls.Add(spritesheetPathTextBox, 1, 0);
            filePathsPanel.Controls.Add(browseSpritesheetButton, 2, 0);
            filePathsPanel.Controls.Add(new Label { Text = "Output Atlas XML:", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 1);
            filePathsPanel.Controls.Add(outputPathTextBox, 1, 1);
            filePathsPanel.Controls.Add(browseOutputButton, 2, 1);
            filePathsPanel.Controls.Add(new Label { Text = "Texture Reference Path:", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 2);
            filePathsPanel.Controls.Add(texturePathTextBox, 1, 2);
            filePathsGroupBox.Controls.Add(filePathsPanel);
            leftPanelFlow.Controls.Add(filePathsGroupBox);

            // Sprite Detection GroupBox
            var detectionGroupBox = new GroupBox
            {
                Text = "Sprite Detection",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 0, 10)
            };
            var detectionPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            detectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            detectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var gridPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            gridWidthNumeric = new NumericUpDown { Width = 60, Minimum = 0, Maximum = 1000, Value = 32 };
            gridHeightNumeric = new NumericUpDown { Width = 60, Minimum = 0, Maximum = 1000, Value = 32 };
            gridWidthNumeric.ValueChanged += GridSize_Changed;
            gridHeightNumeric.ValueChanged += GridSize_Changed;
            gridPanel.Controls.Add(gridWidthNumeric);
            gridPanel.Controls.Add(new Label { Text = "×", AutoSize = true, Margin = new Padding(3, 6, 3, 3) });
            gridPanel.Controls.Add(gridHeightNumeric);
            gridPanel.Controls.Add(new Label { Text = "px", ForeColor = Color.Gray, AutoSize = true, Margin = new Padding(3, 6, 3, 3) });

            var marginPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            marginLeftNumeric = new NumericUpDown { Width = 60, Minimum = 0, Maximum = 2000, Value = 0 };
            marginTopNumeric = new NumericUpDown { Width = 60, Minimum = 0, Maximum = 2000, Value = 0 };
            marginLeftNumeric.ValueChanged += Margins_Changed;
            marginTopNumeric.ValueChanged += Margins_Changed;
            marginPanel.Controls.Add(new Label { Text = "Left", AutoSize = true, Margin = new Padding(0, 6, 4, 3) });
            marginPanel.Controls.Add(marginLeftNumeric);
            marginPanel.Controls.Add(new Label { Text = "Top", AutoSize = true, Margin = new Padding(10, 6, 4, 3) });
            marginPanel.Controls.Add(marginTopNumeric);
            marginPanel.Controls.Add(new Label { Text = "px offset", ForeColor = Color.Gray, AutoSize = true, Margin = new Padding(6, 6, 3, 3) });

            var transparencyPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            trimTransparencyCheckBox = new CheckBox { Text = "Auto-trim", Checked = true };
            transparencyThresholdNumeric = new NumericUpDown { Width = 60, Minimum = 0, Maximum = 255, Value = 0 };
            transparencyPanel.Controls.Add(trimTransparencyCheckBox);
            transparencyPanel.Controls.Add(new Label { Text = "Alpha Threshold:", AutoSize = true, Margin = new Padding(10, 3, 0, 3) });
            transparencyPanel.Controls.Add(transparencyThresholdNumeric);

            detectionPanel.Controls.Add(new Label { Text = "Sprite Grid Size:", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 0);
            detectionPanel.Controls.Add(gridPanel, 1, 0);
            detectionPanel.Controls.Add(new Label { Text = "Sheet Margins:", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 1);
            detectionPanel.Controls.Add(marginPanel, 1, 1);
            detectionPanel.Controls.Add(new Label { Text = "Transparency:", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 2);
            detectionPanel.Controls.Add(transparencyPanel, 1, 2);
            
            detectionGroupBox.Controls.Add(detectionPanel);
            leftPanelFlow.Controls.Add(detectionGroupBox);

            // Sprite Naming GroupBox
            var namingGroupBox = new GroupBox
            {
                Text = "Sprite Naming",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 0, 10)
            };
            var namingPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            namingPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            namingPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            namingPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
            
            namingPatternTextBox = new TextBox { Text = "Sprite_{row}_{col}", Anchor = AnchorStyles.Left | AnchorStyles.Right };
            var namingHelp = new Label { Text = "💡", ForeColor = Color.Orange, Anchor = AnchorStyles.Left, AutoSize = true };
            toolTip.SetToolTip(namingHelp, "Use {row}, {col}, and {index} as placeholders.");

            namingPanel.Controls.Add(new Label { Text = "Naming Pattern:", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 0);
            namingPanel.Controls.Add(namingPatternTextBox, 1, 0);
            namingPanel.Controls.Add(namingHelp, 2, 0);
            
            namingGroupBox.Controls.Add(namingPanel);
            leftPanelFlow.Controls.Add(namingGroupBox);

            leftSplitContainer.Panel1.Controls.Add(leftPanelFlow);

            // Animation panel
            animationGroupBox = new GroupBox()
            {
                Text = "🎬 Animation Sequences",
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold)
            };

            var animPanel = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            animPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            animPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            animationListView = new ListView()
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            animationListView.Columns.Add("Animation Name", 120);
            animationListView.Columns.Add("Frame Delay (ms)", 100);
            animationListView.Columns.Add("Frame Count", 80);
            animationListView.Columns.Add("Frame Pattern", 200);

            var buttonPanel = new FlowLayoutPanel()
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Padding = new Padding(0, 5, 0, 0)
            };

            addAnimationButton = new Button { Text = "➕ Add Animation", Width = 110, Height = 28 };
            editAnimationButton = new Button { Text = "✏️ Edit", Width = 70, Height = 28 };
            removeAnimationButton = new Button { Text = "🗑️ Remove", Width = 80, Height = 28 };

            addAnimationButton.Click += AddAnimationButton_Click;
            editAnimationButton.Click += EditAnimationButton_Click;
            removeAnimationButton.Click += RemoveAnimationButton_Click;

            buttonPanel.Controls.AddRange(new Control[] { addAnimationButton, editAnimationButton, removeAnimationButton });

            animPanel.Controls.Add(animationListView, 0, 0);
            animPanel.Controls.Add(buttonPanel, 0, 1);

            animationGroupBox.Controls.Add(animPanel);
            leftSplitContainer.Panel2.Controls.Add(animationGroupBox);

            PopulatePresets();
            LayoutLeftPanelSections();
        }

        private void CreatePreviewPanel()
        {
            previewPanel = new Panel()
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true,
                BackColor = Color.LightGray
            };

            var previewGroupBox = new GroupBox()
            {
                Text = "🖼️ Spritesheet Preview & Analysis",
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold)
            };

            var previewLayout = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            previewLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Controls
            previewLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Info bar
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Preview image
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Preview controls with enhanced descriptions
            var controlPanel = new FlowLayoutPanel()
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Padding = new Padding(0, 5, 0, 10)
            };

            showGridCheckBox = new CheckBox { Text = "📐 Show Grid Lines", Checked = true, Font = new Font(this.Font.FontFamily, 9, FontStyle.Regular) };
            showRegionsCheckBox = new CheckBox { Text = "🎯 Show Detected Regions", Checked = true, Font = new Font(this.Font.FontFamily, 9, FontStyle.Regular) };
            
            controlPanel.Controls.Add(showGridCheckBox);
            controlPanel.Controls.Add(new Label { Text = "  " }); // Spacer
            controlPanel.Controls.Add(showRegionsCheckBox);
            controlPanel.Controls.Add(new Label { Text = "      " }); // Larger spacer
            
            zoomLabel = new Label { Text = "🔍 Zoom: 100%", Font = new Font(this.Font.FontFamily, 9, FontStyle.Regular) };
            controlPanel.Controls.Add(zoomLabel);
            zoomTrackBar = new TrackBar { Minimum = 25, Maximum = 400, Value = 100, TickFrequency = 25, Width = 120 };
            controlPanel.Controls.Add(zoomTrackBar);

            showGridCheckBox.CheckedChanged += PreviewOption_Changed;
            showRegionsCheckBox.CheckedChanged += PreviewOption_Changed;
            zoomTrackBar.ValueChanged += ZoomTrackBar_ValueChanged;

            // Info bar
            var infoPanel = new FlowLayoutPanel()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Padding = new Padding(5),
                BackColor = Color.WhiteSmoke,
                BorderStyle = BorderStyle.FixedSingle
            };

            gridDisplayLabel = new Label { Text = "Grid: 32x32 px | Margin: 0px left, 0px top", Font = new Font(this.Font.FontFamily, 9, FontStyle.Regular), ForeColor = Color.DarkSlateGray, Anchor = AnchorStyles.Left };
            infoPanel.Controls.Add(gridDisplayLabel);


            // Picture box for spritesheet
            spritesheetPictureBox = new PictureBox()
            {
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.Transparent
            };
            spritesheetPictureBox.Paint += SpritesheetPictureBox_Paint;

            previewPanel.Controls.Add(spritesheetPictureBox);

            previewLayout.Controls.Add(controlPanel, 0, 0);
            previewLayout.Controls.Add(infoPanel, 0, 1);
            previewLayout.Controls.Add(previewPanel, 0, 2);

            previewGroupBox.Controls.Add(previewLayout);
            rightSplitContainer.Panel1.Controls.Add(previewGroupBox);
        }

        private void CreateRightPanel()
        {
            outputGroupBox = new GroupBox()
            {
                Text = "📜 Atlas Output & Generation",
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold)
            };

            var outputLayout = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2 // Changed from 3 to 2
            };
            outputLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            outputLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // XML Preview
            xmlPreviewTextBox = new TextBox()
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 9),
                ReadOnly = true
            };

            // Action buttons
            var actionPanel = new FlowLayoutPanel()
            {
                Dock = DockStyle.Fill, // Changed from Bottom
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Padding = new Padding(0, 10, 0, 0) // Added padding
            };

            generateButton = new Button 
            { 
                Text = "🚀 Generate Atlas", 
                Width = 140, 
                Height = 40,
                BackColor = Color.LimeGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderColor = Color.DarkGreen, BorderSize = 2 },
                Font = new Font(this.Font.FontFamily, 10, FontStyle.Bold)
            };
            saveConfigButton = new Button 
            { 
                Text = "💾 Save Config", 
                Width = 110, 
                Height = 30,
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderColor = Color.DarkBlue, BorderSize = 1 },
                Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold)
            };
            loadConfigButton = new Button 
            { 
                Text = "📂 Load Config", 
                Width = 110, 
                Height = 30,
                BackColor = Color.Orange,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderColor = Color.DarkOrange, BorderSize = 1 },
                Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold)
            };

            generateButton.Click += GenerateButton_Click;
            saveConfigButton.Click += SaveConfigButton_Click;
            loadConfigButton.Click += LoadConfigButton_Click;

            actionPanel.Controls.AddRange(new Control[] { generateButton, saveConfigButton, loadConfigButton });

            outputLayout.Controls.Add(xmlPreviewTextBox, 0, 0);
            outputLayout.Controls.Add(actionPanel, 0, 1);

            outputGroupBox.Controls.Add(outputLayout);
            rightSplitContainer.Panel2.Controls.Add(outputGroupBox);
        }

        private void InitializeConfig()
        {
            _currentConfig = AtlasConfigManager.CreateSampleConfig("", "", "");
            _currentConfigPath = null;
            _isNpcConfig = false;
            _npcContext = null;
            UpdateUI();
            if (generateButton != null) generateButton.Enabled = true;
            if (saveConfigButton != null) saveConfigButton.Enabled = true;
        }

        private void UpdateLeftPanelWidth()
        {
            if (mainSplitContainer == null) return;
            var target = (int)(ClientSize.Width * LeftPanelRatio);
            if (target <= 0) return;
            mainSplitContainer.SplitterDistance = Math.Clamp(target, 200, Math.Max(200, ClientSize.Width - 400));
            LayoutLeftPanelSections();
        }

        private void PopulatePresets()
        {
            presetComboBox.Items.AddRange(new string[]
            {
                "Custom",
                "RPG Character 32x32",
                "RPG Character 48x48", 
                "Weapon Icons 64x64",
                "Item Icons 32x32",
                "Tileset 16x16",
                "UI Elements"
            });
            presetComboBox.SelectedIndex = 0;
        }

        private void LayoutLeftPanelSections()
        {
            if (leftPanelFlow == null) return;
            var availableWidth = leftPanelFlow.ClientSize.Width - leftPanelFlow.Padding.Horizontal;
            if (availableWidth <= 0) return;

            foreach (Control control in leftPanelFlow.Controls)
            {
                if (control is GroupBox group)
                {
                    var width = availableWidth - group.Margin.Horizontal;
                    if (width <= 0) continue;

                    var preferred = group.GetPreferredSize(new Size(width, 0));
                    group.MinimumSize = new Size(width, 0);
                    group.MaximumSize = new Size(width, 0);
                    group.Size = new Size(width, preferred.Height);
                }
            }
        }

        // Event handlers will be implemented in the next part...
    }
}
