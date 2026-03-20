#nullable enable
using NMSE.Core;
using NMSE.Data;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

partial class SettlementPanel
{
    private System.ComponentModel.IContainer? components = null;

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
        // SettlementPanel
        // 
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.DoubleBuffered = true;
        this.ResumeLayout(false);
    }
    #endregion

    private void SetupLayout()
    {
        SuspendLayout();

        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(0)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            AutoSize = true,
            Padding = new Padding(10)
        };

        int row = 0;

        // Title
        _titleLabel = new Label
        {
            Text = "Settlements",
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 5)
        };
        FontManager.ApplyHeadingFont(_titleLabel, 14);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(_titleLabel, 0, row++);

        // Top: Settlement selector + Delete button
        var selectorPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 0, 0, 5)
        };
        _settlementSelector = new ComboBox { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
        _settlementSelector.SelectedIndexChanged += OnSettlementSelected;
        _deleteSettlementBtn = new Button { Text = "Delete Settlement", AutoSize = true };
        _deleteSettlementBtn.Click += OnDeleteSettlement;
        _exportSettlementBtn = new Button { Text = "Export", AutoSize = true };
        _exportSettlementBtn.Click += OnExportSettlement;
        _importSettlementBtn = new Button { Text = "Import", AutoSize = true };
        _importSettlementBtn.Click += OnImportSettlement;
        selectorPanel.Controls.Add(_settlementSelector);
        selectorPanel.Controls.Add(_deleteSettlementBtn);
        selectorPanel.Controls.Add(_exportSettlementBtn);
        selectorPanel.Controls.Add(_importSettlementBtn);

        // Warning
        var settleWarn = new Label
        {
            Text = "\u26A0 Deleting a settlement doesn't remove the teleporter entry. You can remove it from Discoveries -> Known Location.",
            AutoSize = true,
            ForeColor = Color.DarkOrange,
            Font = new System.Drawing.Font("Segoe UI Emoji", 9F, System.Drawing.FontStyle.Bold),
            Padding = new Padding(0, 0, 0, 5)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(selectorPanel, 0, row++);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(settleWarn, 0, row++);

        // 2-column form
        var formPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 10)
        };
        formPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        formPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

        // Left column: Name, Seed, Perks 1-6, Decision Type, Last Decision
        var leftPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true
        };
        leftPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        leftPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        int leftRow = 0;

        _settlementName = new TextBox { Dock = DockStyle.Fill };
        _settlementName.Leave += OnSettlementNameChanged;
        _nameLabel = AddRow(leftPanel, "Name:", _settlementName, leftRow++);

        var seedPanel = new Panel { Dock = DockStyle.Fill, Height = 23 };
        _seedField = new TextBox { Dock = DockStyle.Fill };
        _generateSeedBtn = new Button { Text = "Generate", Dock = DockStyle.Right, Width = 70, Height = 23 };
        _generateSeedBtn.Click += (s, e) =>
        {
            byte[] bytes = new byte[8];
            _rng.NextBytes(bytes);
            _seedField.Text = "0x" + BitConverter.ToString(bytes).Replace("-", "");
        };
        seedPanel.Controls.Add(_seedField);
        seedPanel.Controls.Add(_generateSeedBtn);
        _seedLabel = AddRow(leftPanel, "Seed:", seedPanel, leftRow++);

        // Perk slots 1-6
        _perkCombos = new ComboBox[PerkSlotCount];
        _perkSeedFields = new TextBox[PerkSlotCount];
        _perkSeedPanels = new Panel[PerkSlotCount];
        _perkRemoveButtons = new Button[PerkSlotCount];
        _perkLabels = new Label[PerkSlotCount];

        for (int i = 0; i < PerkSlotCount; i++)
        {
            var perkRowPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                AutoSize = true,
                Margin = new Padding(0)
            };
            perkRowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            perkRowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            perkRowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var combo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            PopulatePerkCombo(combo);
            int slot = i;
            combo.SelectedIndexChanged += (s, e) => OnPerkChanged(slot);
            combo.DrawItem += OnPerkComboDrawItem;
            _perkCombos[i] = combo;

            var perkSeedContainer = new Panel { Width = 120, Height = 23, Visible = false };
            var perkSeedField = new TextBox { Dock = DockStyle.Fill };
            perkSeedContainer.Controls.Add(perkSeedField);
            _perkSeedFields[i] = perkSeedField;
            _perkSeedPanels[i] = perkSeedContainer;

            var removeBtn = new Button { Text = "×", Width = 25, Height = 23 };
            removeBtn.AccessibleName = $"Remove Perk {i + 1}";
            int removeSlot = i;
            removeBtn.Click += (s, e) => { _perkCombos[removeSlot].SelectedIndex = 0; };
            _perkRemoveButtons[i] = removeBtn;

            perkRowPanel.Controls.Add(combo, 0, 0);
            perkRowPanel.Controls.Add(perkSeedContainer, 1, 0);
            perkRowPanel.Controls.Add(removeBtn, 2, 0);

            _perkLabels[i] = AddRow(leftPanel, $"Perk {i + 1}:", perkRowPanel, leftRow++);
        }

        // Decision controls at bottom of left column (after perks)
        _decisionTypeField = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _decisionTypeField.Items.AddRange(SettlementLogic.DecisionTypes);
        _decisionTypeLabel = AddRow(leftPanel, "Decision Type:", _decisionTypeField, leftRow++);

        _lastDecisionTimeField = new DateTimePicker
        {
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss"
        };
        _lastDecisionLabel = AddRow(leftPanel, "Last Decision:", _lastDecisionTimeField, leftRow++);

        formPanel.Controls.Add(leftPanel, 0, 0);

        // Right column: Stats only
        var rightPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true
        };
        rightPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rightPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        int rightRow = 0;

        _statFields = new NumericUpDown[StatCount];
        _statRowLabels = new Label[StatCount];
        for (int i = 0; i < StatCount; i++)
        {
            _statFields[i] = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Maximum = StatMaxValues[i],
                Minimum = SettlementLogic.StatMinValues[i],
            };
            _statRowLabels[i] = AddRow(rightPanel, StatLabels[i] + ":", _statFields[i], rightRow++);
        }

        formPanel.Controls.Add(rightPanel, 1, 0);

        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(formPanel, 0, row++);

        // Production section header
        _productionHeaderLabel = new Label
        {
            Text = "Production",
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 4)
        };
        FontManager.ApplyHeadingFont(_productionHeaderLabel, 10);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(_productionHeaderLabel, 0, row++);

        // Production DataGridView
        _productionGrid = new DataGridView
        {
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            Dock = DockStyle.Fill,
            Height = 150,
        };
        _productionGrid.Columns.Add(new DataGridViewImageColumn
        {
            Name = "Icon",
            HeaderText = "🏭",
            Width = 30,
            ImageLayout = DataGridViewImageCellLayout.Zoom,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
        });
        _productionGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ItemName",
            HeaderText = "Name",
            ReadOnly = true,
            FillWeight = 30,
        });
        _productionGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ElementId",
            HeaderText = "Element ID",
            ReadOnly = false,
            FillWeight = 25,
        });
        _productionGrid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "ChangeElement",
            HeaderText = "Edit",
            Text = "...",
            UseColumnTextForButtonValue = true,
            FillWeight = 8,
        });
        _productionGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Amount",
            HeaderText = "Amount",
            ReadOnly = false,
            FillWeight = 20,
        });
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(_productionGrid, 0, row++);
        _productionGrid.CellContentClick += OnProductionGridCellClick;

        // Info label
        _infoLabel = new Label
        {
            Text = "Load a save file to view settlement data.",
            AutoSize = true,
            Padding = new Padding(0, 10, 0, 0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(_infoLabel, 0, row);

        layout.RowCount = row + 1;
        scrollPanel.Controls.Add(layout);
        Controls.Add(scrollPanel);

        ResumeLayout(false);
        PerformLayout();
    }

    private ComboBox _settlementSelector = null!;
    private Button _deleteSettlementBtn = null!;
    private Button _exportSettlementBtn = null!;
    private Button _importSettlementBtn = null!;
    private TextBox _settlementName = null!;
    private TextBox _seedField = null!;
    private Button _generateSeedBtn = null!;
    private NumericUpDown[] _statFields = null!;
    private ComboBox _decisionTypeField = null!;
    private DateTimePicker _lastDecisionTimeField = null!;
    private ComboBox[] _perkCombos = null!;
    private TextBox[] _perkSeedFields = null!;
    private Panel[] _perkSeedPanels = null!;
    private Button[] _perkRemoveButtons = null!;
    private DataGridView _productionGrid = null!;
    private Label _infoLabel = null!;
    private Label _titleLabel = null!;
    private Label _nameLabel = null!;
    private Label _seedLabel = null!;
    private Label[] _perkLabels = null!;
    private Label _decisionTypeLabel = null!;
    private Label _lastDecisionLabel = null!;
    private Label[] _statRowLabels = null!;
    private Label _productionHeaderLabel = null!;
}
