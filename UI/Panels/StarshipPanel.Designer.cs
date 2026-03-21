#nullable enable
using NMSE.Core;
using NMSE.UI.Controls;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

partial class StarshipPanel
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer? components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Component Designer generated code
    private void InitializeComponent()
    {
        this.SuspendLayout();
        // 
        // StarshipPanel
        // 
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.DoubleBuffered = true;
        this.ResumeLayout(false);
    }
    #endregion

    private void SetupLayout()
    {
        SuspendLayout();

        // Main layout: 1 column, 3 rows (title, details/stats+buttons, inventory)
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10),
            AutoSize = true
        };

        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // title
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // details/stats+buttons
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // inventory

        var titlePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
        };
        _titleLabel = new Label
        {
            Text = "Starships",
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 2)
        };
        FontManager.ApplyHeadingFont(_titleLabel, 14);
        _primaryShipLabel = new Label
        {
            Text = "",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Padding = new Padding(15, 5, 0, 0),
            Font = new Font(Font.FontFamily, 9, FontStyle.Italic),
        };
        titlePanel.Controls.Add(_titleLabel);
        titlePanel.Controls.Add(_primaryShipLabel);
        mainLayout.Controls.Add(titlePanel, 0, 0);

        // Details+Stats layout: 2 columns, 2 rows (details/stats, buttons)
        var detailsStatsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true
        };
        detailsStatsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        detailsStatsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        detailsStatsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // details/stats
        detailsStatsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // buttons

        // Left panel for selection and properties
        var leftPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            AutoSize = true
        };
        for (int i = 0; i < 7; i++)
            leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        leftPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;
        _detailsLabel = new Label
        {
            Text = "Details",
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 2)
        };
        FontManager.ApplyHeadingFont(_detailsLabel, 10);
        leftPanel.Controls.Add(_detailsLabel, 0, row);
        leftPanel.SetColumnSpan(_detailsLabel, 2);
        row++;

        _shipSelector = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _shipSelector.SelectedIndexChanged += OnShipSelected;
        _selectLabel = AddRow(leftPanel, "Select Ship:", _shipSelector, row++);

        _shipName = new TextBox { Dock = DockStyle.Fill };
        _shipName.Leave += OnShipNameChanged;
        _nameLabel = AddRow(leftPanel, "Name:", _shipName, row++);

        _shipType = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _shipType.SelectedIndexChanged += OnShipTypeChanged;
        _typeLabel = AddRow(leftPanel, "Type:", _shipType, row++);

        _shipClass = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _shipClass.Items.AddRange(StarshipLogic.ShipClasses);
        _classLabel = AddRow(leftPanel, "Class:", _shipClass, row++);

        var seedPanel = new Panel { Dock = DockStyle.Fill, Height = 26 };
        _shipSeed = new TextBox { Dock = DockStyle.Fill };
        _generateSeedBtn = new Button { Text = "Generate", Dock = DockStyle.Right, AutoSize = true, MinimumSize = new Size(70, 0) };
        _generateSeedBtn.Click += (s, e) =>
        {
            byte[] bytes = new byte[8];
            _rng.NextBytes(bytes);
            _shipSeed.Text = "0x" + BitConverter.ToString(bytes).Replace("-", "");
        };
        seedPanel.Controls.Add(_shipSeed);
        seedPanel.Controls.Add(_generateSeedBtn);
        _seedLabel = AddRow(leftPanel, "Seed:", seedPanel, row++);

        // Right panel for base stats
        var rightPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            AutoSize = true
        };
        for (int i = 0; i < 5; i++)
            rightPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rightPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rightPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int statRow = 0;
        _statsLabel = new Label
        {
            Text = "Base Stats",
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 2)
        };
        FontManager.ApplyHeadingFont(_statsLabel, 10);
        rightPanel.Controls.Add(_statsLabel, 0, statRow);
        rightPanel.SetColumnSpan(_statsLabel, 2);
        statRow++;

        _damageField = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Minimum = 0, Maximum = 999999, Increment = 0.01m };
        _damageLabel = AddRow(rightPanel, "Damage:", _damageField, statRow++);

        _shieldField = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Minimum = 0, Maximum = 999999, Increment = 0.01m };
        _shieldLabel = AddRow(rightPanel, "Shield:", _shieldField, statRow++);

        _hyperdriveField = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Minimum = 0, Maximum = 999999, Increment = 0.01m };
        _hyperdriveLabel = AddRow(rightPanel, "Hyperdrive:", _hyperdriveField, statRow++);

        _maneuverField = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Minimum = 0, Maximum = 999999, Increment = 0.01m };
        _maneuverLabel = AddRow(rightPanel, "Maneuverability:", _maneuverField, statRow++);

        _useOldColours = new CheckBox { Text = "Use Old Color", AutoSize = true };
        rightPanel.Controls.Add(new Label(), 0, statRow);
        rightPanel.Controls.Add(_useOldColours, 1, statRow++);

        detailsStatsLayout.Controls.Add(leftPanel, 0, 0);
        detailsStatsLayout.Controls.Add(rightPanel, 1, 0);

        // Buttons panel
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight
        };
        _deleteBtn = new Button { Text = "Delete Ship", AutoSize = true, MinimumSize = new Size(76, 0) };
        _deleteBtn.Click += OnDeleteShip;
        _exportBtn = new Button { Text = "Export Starship", AutoSize = true, MinimumSize = new Size(70, 0) };
        _exportBtn.Click += OnExportShip;
        _importBtn = new Button { Text = "Import Starship", AutoSize = true, MinimumSize = new Size(70, 0) };
        _importBtn.Click += OnImportShip;
        _exportCorvetteBtn = new Button { Text = "Export Corvette", AutoSize = true, MinimumSize = new Size(100, 0), Visible = false };
        _exportCorvetteBtn.Click += OnExportCorvette;
        _importCorvetteBtn = new Button { Text = "Import Corvette", AutoSize = true, MinimumSize = new Size(100, 0), Visible = false };
        _importCorvetteBtn.Click += OnImportCorvette;
        _makePrimaryBtn = new Button { Text = "Make Primary", AutoSize = true, MinimumSize = new Size(88, 0) };
        _makePrimaryBtn.Click += OnMakePrimary;
        _snapshotTechBtn = new Button { Text = "Snapshot Tech", AutoSize = true, MinimumSize = new Size(100, 0), Visible = false };
        _snapshotTechBtn.Click += OnSnapshotTech;
        _importSnapshotBtn = new Button { Text = "Import Snapshot", AutoSize = true, MinimumSize = new Size(100, 0), Visible = false };
        _importSnapshotBtn.Click += OnImportSnapshot;
        _corvetteWarningLabel = new ColorEmojiLabel
        {
            Text = "\u26A0 Saves only store full Technology slots for your last active Corvette.",
            Font = new Font("Segoe UI Emoji", 9, FontStyle.Bold),
            AutoSize = true,
            ForeColor = Color.Red,
            Padding = new Padding(5, 5, 0, 0),
            Visible = false,
        };
        buttonPanel.Controls.Add(_deleteBtn);
        buttonPanel.Controls.Add(_exportBtn);
        buttonPanel.Controls.Add(_importBtn);
        buttonPanel.Controls.Add(_exportCorvetteBtn);
        buttonPanel.Controls.Add(_importCorvetteBtn);
        buttonPanel.Controls.Add(_makePrimaryBtn);
        buttonPanel.Controls.Add(_snapshotTechBtn);
        buttonPanel.Controls.Add(_importSnapshotBtn);
        buttonPanel.Controls.Add(_corvetteWarningLabel);
        detailsStatsLayout.Controls.Add(buttonPanel, 0, 1);
        detailsStatsLayout.SetColumnSpan(buttonPanel, 2);

        mainLayout.Controls.Add(detailsStatsLayout, 0, 1);

        // Inventory tabs (fill remaining space)
        _inventoryGrid = new InventoryGridPanel { Dock = DockStyle.Fill };
        _techGrid = new InventoryGridPanel { Dock = DockStyle.Fill };
        _techGrid.SetIsTechInventory(true);
        _inventoryGrid.SetIsCargoInventory(true);
        _techGrid.SetInventoryOwnerType("Ship");
        _inventoryGrid.SetInventoryOwnerType("Ship");
        _inventoryGrid.SetInventoryGroup("ShipCargo");
        _techGrid.SetInventoryGroup("Ship");
        _inventoryGrid.DataModified += (s, e) => DataModified?.Invoke(this, e);
        _techGrid.DataModified += (s, e) => DataModified?.Invoke(this, e);

        _invTabs = new DoubleBufferedTabControl { Dock = DockStyle.Fill };
        _cargoTabPage = new TabPage("Cargo");
        _cargoTabPage.Controls.Add(_inventoryGrid);
        _techTabPage = new TabPage("Technology");
        _techTabPage.Controls.Add(_techGrid);
        _invTabs.TabPages.Add(_cargoTabPage);
        _invTabs.TabPages.Add(_techTabPage);

        mainLayout.Controls.Add(_invTabs, 0, 2);

        Controls.Add(mainLayout);

        _inventoryGrid.SetSuperchargeDisabled(true);

        // Set initial Max Supported labels (will be updated on ship selection)
        SetStarshipMaxSupportedLabels("Unknown");

        ResumeLayout(false);
        PerformLayout();
    }

    private ComboBox _shipSelector = null!;
    private TextBox _shipName = null!;
    private ComboBox _shipClass = null!;
    private ComboBox _shipType = null!;
    private TextBox _shipSeed = null!;
    private Button _generateSeedBtn = null!;
    private CheckBox _useOldColours = null!;
    private NumericUpDown _damageField = null!;
    private NumericUpDown _shieldField = null!;
    private NumericUpDown _hyperdriveField = null!;
    private NumericUpDown _maneuverField = null!;
    private Button _deleteBtn = null!;
    private Button _exportBtn = null!;
    private Button _importBtn = null!;
    private Button _makePrimaryBtn = null!;
    private Label _primaryShipLabel = null!;
    private ColorEmojiLabel _corvetteWarningLabel = null!;
    private Button _exportCorvetteBtn = null!;
    private Button _importCorvetteBtn = null!;
    private Button _snapshotTechBtn = null!;
    private Button _importSnapshotBtn = null!;
    private DoubleBufferedTabControl _invTabs = null!;
    private InventoryGridPanel _inventoryGrid = null!;
    private InventoryGridPanel _techGrid = null!;
    private Label _titleLabel = null!;
    private Label _detailsLabel = null!;
    private Label _statsLabel = null!;
    private Label _selectLabel = null!;
    private Label _nameLabel = null!;
    private Label _typeLabel = null!;
    private Label _classLabel = null!;
    private Label _seedLabel = null!;
    private Label _damageLabel = null!;
    private Label _shieldLabel = null!;
    private Label _hyperdriveLabel = null!;
    private Label _maneuverLabel = null!;
    private TabPage _cargoTabPage = null!;
    private TabPage _techTabPage = null!;
}
