#nullable enable
using NMSE.Config;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

partial class InventoryGridPanel
{
    private System.ComponentModel.IContainer? components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sharedToolTip?.Dispose();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Component Designer generated code
    private void InitializeComponent()
    {
        this.SuspendLayout();
        // 
        // InventoryGridPanel
        // 
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.DoubleBuffered = true;
        this.ResumeLayout(false);
    }
    #endregion

    private void SetupLayout()
    {
        SuspendLayout();

        var desiredRightPanelWidth = 300;
        var splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            FixedPanel = FixedPanel.Panel2,
            SplitterDistance = 280
        };

        splitContainer.SizeChanged += (s, e) =>
        {
            if (splitContainer.Width <= desiredRightPanelWidth)
                return;

            splitContainer.Panel2MinSize = desiredRightPanelWidth;
            splitContainer.SplitterDistance = splitContainer.Width - desiredRightPanelWidth;
        };

        // Left: info row above toolbar/grid for long inventory guidance text
        _infoPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 26,
            Padding = new Padding(4, 4, 4, 0),
            Visible = false
        };

        // Left: resize controls above the grid
        _toolbarPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            MinimumSize = new Size(0, 36),
            MaximumSize = new Size(int.MaxValue, 36),
            Padding = new Padding(4, 4, 4, 0),
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = false,
            WrapContents = false
        };
        _resizeWidthLabel = new Label { Text = "Width:", AutoSize = true, Dock = DockStyle.Left, Padding = new Padding(0, 4, 2, 0) };
        _resizeWidth = new NumericUpDown { Minimum = 1, Maximum = 20, Value = 10, Width = 50, Dock = DockStyle.Left };
        _resizeHeightLabel = new Label { Text = "Height:", AutoSize = true, Dock = DockStyle.Left, Padding = new Padding(8, 4, 2, 0) };
        _resizeHeight = new NumericUpDown { Minimum = 1, Maximum = 20, Value = 6, Width = 50, Dock = DockStyle.Left };
        _resizeButton = new Button { Text = "Resize", AutoSize = true, Size = new Size(75, 28), MinimumSize = new Size(75, 28), Margin = new Padding(8, 0, 0, 0) };
        _resizeButton.Click += OnResizeInventory;
        _sortModeLabel = new Label { Text = "Sort:", AutoSize = true, Dock = DockStyle.Left, Padding = new Padding(12, 4, 2, 0), Margin = new Padding(0, 2, 0, 0) };
        _sortModeCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80, Margin = new Padding(0, 2, 0, 0) };
        _sortModeCombo.SelectedIndexChanged += OnSortModeChanged;
        _autoStackToolStrip = new ToolStrip
        {
            AutoSize = true,
            MinimumSize = new Size(60, 28),
            GripStyle = ToolStripGripStyle.Hidden,
            CanOverflow = false,
            Padding = Padding.Empty,
            Margin = new Padding(8, 0, 0, 0),
            RenderMode = ToolStripRenderMode.System
        };
        _autoStackDropDownButton = new ToolStripDropDownButton("Auto-Stack");
        _autoStackToChestsButtonMenuItem = new ToolStripMenuItem("To Chests", null, OnAutoStackToStorage);
        _autoStackToStarshipButtonMenuItem = new ToolStripMenuItem("To Starship", null, OnAutoStackToStarship);
        _autoStackToFreighterButtonMenuItem = new ToolStripMenuItem("To Freighter", null, OnAutoStackToFreighter);
        _autoStackDropDownButton.DropDownItems.Add(_autoStackToChestsButtonMenuItem);
        _autoStackDropDownButton.DropDownItems.Add(_autoStackToStarshipButtonMenuItem);
        _autoStackDropDownButton.DropDownItems.Add(_autoStackToFreighterButtonMenuItem);
        _autoStackToolStrip.Items.Add(_autoStackDropDownButton);
        _exportButton = new Button { Text = "Export", AutoSize = true, Size = new Size(75, 28), MinimumSize = new Size(75, 28), Margin = new Padding(8, 0, 0, 0) };
        _exportButton.Click += OnExportInventory;
        _importButton = new Button { Text = "Import", AutoSize = true, Size = new Size(75, 28), MinimumSize = new Size(75, 28), Margin = new Padding(4, 0, 0, 0) };
        _importButton.Click += OnImportInventory;
        _toolbarPanel.Controls.Add(_resizeWidthLabel);
        _toolbarPanel.Controls.Add(_resizeWidth);
        _toolbarPanel.Controls.Add(_resizeHeightLabel);
        _toolbarPanel.Controls.Add(_resizeHeight);
        _toolbarPanel.Controls.Add(_resizeButton);
        _toolbarPanel.Controls.Add(_sortModeLabel);
        _toolbarPanel.Controls.Add(_sortModeCombo);
        _toolbarPanel.Controls.Add(_autoStackToolStrip);
        _toolbarPanel.Controls.Add(_exportButton);
        _toolbarPanel.Controls.Add(_importButton);
        // Note: Dock=Left controls are added in reverse visual order

        // Left: grid of slot cells
        _gridContainer = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        splitContainer.Panel1.Controls.Add(_gridContainer);
        splitContainer.Panel1.Controls.Add(_toolbarPanel);
        splitContainer.Panel1.Controls.Add(_infoPanel);

        // Right: detail/editor panel
        _detailPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            AutoScroll = true,
            BackColor = SystemColors.Control
        };

        var detailLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            Padding = new Padding(4)
        };
        detailLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        detailLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;

        // === SLOT DETAILS SECTION ===
        _slotDetailHeader = new Label
        {
            Text = "Slot Details",
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 4)
        };
        FontManager.ApplyHeadingFont(_slotDetailHeader, 11);
        detailLayout.Controls.Add(_slotDetailHeader, 0, row);
        detailLayout.SetColumnSpan(_slotDetailHeader, 2);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        _detailIcon = new PictureBox
        {
            Size = new Size(72, 72),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(40, 40, 40),
            BorderStyle = BorderStyle.FixedSingle
        };
        detailLayout.Controls.Add(_detailIcon, 0, row);
        detailLayout.SetColumnSpan(_detailIcon, 2);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Item Name
        _detailItemName = new Label
        {
            Text = "(no slot selected)",
            AutoSize = true,
            ForeColor = Color.DarkBlue
        };
        FontManager.ApplyHeadingFont(_detailItemName, 11);
        _detailNameLabel = CreateLabel("Name:");
        detailLayout.Controls.Add(_detailNameLabel, 0, row);
        detailLayout.Controls.Add(_detailItemName, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Slot Position
        _detailSlotPosition = new Label { Text = "", AutoSize = true };
        _detailPositionLabel = CreateLabel("Position:");
        detailLayout.Controls.Add(_detailPositionLabel, 0, row);
        detailLayout.Controls.Add(_detailSlotPosition, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Item Type
        _detailItemType = new Label { Text = "", AutoSize = true };
        _detailTypeLabel = CreateLabel("Type:");
        detailLayout.Controls.Add(_detailTypeLabel, 0, row);
        detailLayout.Controls.Add(_detailItemType, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Item Category
        _detailItemCategory = new Label { Text = "", AutoSize = true };
        _detailCategoryLabel = CreateLabel("Category:");
        detailLayout.Controls.Add(_detailCategoryLabel, 0, row);
        detailLayout.Controls.Add(_detailItemCategory, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Item ID (editable) with inline seed field for procedural items
        // Layout: "Item ID: [_detailItemId] # [_detailSeedField]"
        _detailItemId = new TextBox { Width = 120, Anchor = AnchorStyles.Left, Margin = new Padding(0, 4, 0, 0) };
        _detailSeedLabel = CreateLabel("#");
        _detailSeedLabel.AutoSize = true;
        _detailSeedLabel.Font = new Font(_detailSeedLabel.Font.FontFamily, _detailSeedLabel.Font.Size + 2, _detailSeedLabel.Font.Style);
        _detailSeedLabel.Margin = new Padding(0, 4, 0, 0);
        _detailSeedLabel.Visible = false;
        _detailSeedField = new TextBox { Width = 56, MaxLength = 5, Visible = false, Margin = new Padding(0, 4, 0, 0) };
        _detailItemIdLabel = CreateLabel("Item ID:");
        _detailIdSeedPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _detailIdSeedPanel.Controls.Add(_detailItemId);
        _detailIdSeedPanel.Controls.Add(_detailSeedLabel);
        _detailIdSeedPanel.Controls.Add(_detailSeedField);

        var _detailSeedEntryPanel = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
            Dock = DockStyle.Left,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _detailSeedEntryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _detailSeedEntryPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _detailSeedEntryPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _detailSeedEntryPanel.Controls.Add(_detailIdSeedPanel, 0, 0);

        _detailGenSeedButton = new Button
        {
            Text = "Gen",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowOnly,
            Visible = false,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0, 4, 0, 0),
            Padding = new Padding(0, 0, 0, 0)
        };
        _detailGenSeedButton.Click += OnGenSeedClick;
        _detailSeedEntryPanel.Controls.Add(_detailGenSeedButton, 0, 1);

        detailLayout.Controls.Add(_detailItemIdLabel, 0, row);
        detailLayout.Controls.Add(_detailSeedEntryPanel, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Amount
        _detailAmount = new NumericUpDown { Dock = DockStyle.Fill, Minimum = int.MinValue, Maximum = int.MaxValue };
        _detailAmountLabel = CreateLabel("Amount:");
        detailLayout.Controls.Add(_detailAmountLabel, 0, row);
        detailLayout.Controls.Add(_detailAmount, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Max Amount
        _detailMaxAmount = new NumericUpDown { Dock = DockStyle.Fill, Minimum = int.MinValue, Maximum = int.MaxValue };
        _detailMaxLabel = CreateLabel("Max:");
        detailLayout.Controls.Add(_detailMaxLabel, 0, row);
        detailLayout.Controls.Add(_detailMaxAmount, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Damage Factor
        _detailDamageFactor = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = 0,
            Maximum = 1,
            DecimalPlaces = 4,
            Increment = 0.01m
        };
        _detailDamageLabel = CreateLabel("Damage:");
        detailLayout.Controls.Add(_detailDamageLabel, 0, row);
        detailLayout.Controls.Add(_detailDamageFactor, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Apply button
        _applyButton = new Button
        {
            Text = "Apply Changes",
            Dock = DockStyle.Fill,
            Height = 30,
            Enabled = false
        };
        _applyButton.Click += OnApplyChanges;
        detailLayout.Controls.Add(_applyButton, 0, row);
        detailLayout.SetColumnSpan(_applyButton, 2);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Description
        _detailDescription = new Label
        {
            Text = "",
            AutoSize = true,
            MaximumSize = new Size(250, 0),
            ForeColor = Color.Gray,
            Padding = new Padding(0, 4, 0, 0)
        };
        detailLayout.Controls.Add(_detailDescription, 0, row);
        detailLayout.SetColumnSpan(_detailDescription, 2);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // === ITEM PICKER SECTION ===
        var separator = new Label
        {
            Text = "",
            BorderStyle = BorderStyle.Fixed3D,
            AutoSize = false,
            Height = 2,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 8, 0, 8)
        };
        detailLayout.Controls.Add(separator, 0, row);
        detailLayout.SetColumnSpan(separator, 2);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        _itemPickerHeader = new Label
        {
            Text = "Item Picker",
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 4)
        };
        FontManager.ApplyHeadingFont(_itemPickerHeader, 10);
        detailLayout.Controls.Add(_itemPickerHeader, 0, row);
        detailLayout.SetColumnSpan(_itemPickerHeader, 2);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Search box
        var searchPanel = new Panel { Dock = DockStyle.Fill, Height = 26 };
        _searchBox = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Search items..." };
        _searchButton = new Button { Text = "Search", Dock = DockStyle.Right, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(60, 0) };
        _searchButton.Click += OnSearch;
        _searchBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) OnSearch(s, e); };
        searchPanel.Controls.Add(_searchBox);
        searchPanel.Controls.Add(_searchButton);
        _searchLabel = CreateLabel("Search:");
        detailLayout.Controls.Add(_searchLabel, 0, row);
        detailLayout.Controls.Add(searchPanel, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Type filter
        _typeFilter = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _typeFilter.SelectedIndexChanged += OnTypeFilterChanged;
        _typeFilterLabel = CreateLabel("Type:");
        detailLayout.Controls.Add(_typeFilterLabel, 0, row);
        detailLayout.Controls.Add(_typeFilter, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Category filter
        _categoryFilter = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _categoryFilter.SelectedIndexChanged += OnCategoryFilterChanged;
        _categoryFilterLabel = CreateLabel("Category:");
        detailLayout.Controls.Add(_categoryFilterLabel, 0, row);
        detailLayout.Controls.Add(_categoryFilter, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Item picker
        _itemPicker = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            MaxDropDownItems = 20
        };
        _itemPicker.SelectedIndexChanged += OnItemPickerChanged;
        _itemFilterLabel = CreateLabel("Item:");
        detailLayout.Controls.Add(_itemFilterLabel, 0, row);
        detailLayout.Controls.Add(_itemPicker, 1, row);
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        _detailPanel.Controls.Add(detailLayout);
        splitContainer.Panel2.Controls.Add(_detailPanel);

        // Context menu
        _cellContextMenu = new ContextMenuStrip();
        _addItemMenuItem = new ToolStripMenuItem("Add Item", null, OnAddItem);
        _removeItemMenuItem = new ToolStripMenuItem("Remove Item", null, OnRemoveItem);
        _enableSlotMenuItem = new ToolStripMenuItem("Enable/Disable Slot", null, OnEnableSlot);
        _enableAllSlotsMenuItem = new ToolStripMenuItem("Enable All Slots", null, OnEnableAllSlots);
        _pinSlotMenuItem = new ToolStripMenuItem("Pin Slot", null, OnTogglePinnedSlot);
        _repairSlotMenuItem = new ToolStripMenuItem("Repair Slot", null, OnRepairSlot);
        _repairAllSlotsMenuItem = new ToolStripMenuItem("Repair All Slots", null, OnRepairAllSlots);
        _superchargeSlotMenuItem = new ToolStripMenuItem("Supercharge Slot", null, OnSuperchargeSlot);
        _superchargeAllSlotsMenuItem = new ToolStripMenuItem("Supercharge All Slots", null, OnSuperchargeAllSlots);
        _fillStackMenuItem = new ToolStripMenuItem("Fill Stack", null, OnFillStack);
        _rechargeAllTechMenuItem = new ToolStripMenuItem("Recharge All Technology", null, OnRechargeAllTech);
        _refillAllStacksMenuItem = new ToolStripMenuItem("Refill All Stacks", null, OnRefillAllStacks);
        _copyItemMenuItem = new ToolStripMenuItem("Copy Item", null, OnCopyItem);
        _pasteItemMenuItem = new ToolStripMenuItem("Paste Item", null, OnPasteItem);
        _sortByNameMenuItem = new ToolStripMenuItem("Sort by Name", null, OnSortByName);
        _sortByCategoryMenuItem = new ToolStripMenuItem("Sort by Category", null, OnSortByCategory);
        _autoStackToStorageMenuItem = new ToolStripMenuItem("Auto-Stack to Chests", null, OnAutoStackToStorage);
        _autoStackToStarshipMenuItem = new ToolStripMenuItem("Auto-Stack to Starship", null, OnAutoStackToStarship);
        _autoStackToFreighterMenuItem = new ToolStripMenuItem("Auto-Stack to Freighter", null, OnAutoStackToFreighter);
        _cellContextMenu.Items.Add(_addItemMenuItem);
        _cellContextMenu.Items.Add(_removeItemMenuItem);
        _cellContextMenu.Items.Add(new ToolStripSeparator());
        _cellContextMenu.Items.Add(_enableSlotMenuItem);
        _cellContextMenu.Items.Add(_enableAllSlotsMenuItem);
        _cellContextMenu.Items.Add(_pinSlotMenuItem);
        _cellContextMenu.Items.Add(new ToolStripSeparator());
        _cellContextMenu.Items.Add(_repairSlotMenuItem);
        _cellContextMenu.Items.Add(_repairAllSlotsMenuItem);
        _cellContextMenu.Items.Add(new ToolStripSeparator());
        _cellContextMenu.Items.Add(_superchargeSlotMenuItem);
        _cellContextMenu.Items.Add(_superchargeAllSlotsMenuItem);
        _cellContextMenu.Items.Add(new ToolStripSeparator());
        _cellContextMenu.Items.Add(_fillStackMenuItem);
        _cellContextMenu.Items.Add(_rechargeAllTechMenuItem);
        _cellContextMenu.Items.Add(_refillAllStacksMenuItem);
        _cellContextMenu.Items.Add(new ToolStripSeparator());
        _cellContextMenu.Items.Add(_copyItemMenuItem);
        _cellContextMenu.Items.Add(_pasteItemMenuItem);
        _cellContextMenu.Items.Add(new ToolStripSeparator());
        _cellContextMenu.Items.Add(_sortByNameMenuItem);
        _cellContextMenu.Items.Add(_sortByCategoryMenuItem);
        _cellContextMenu.Items.Add(_autoStackToStorageMenuItem);
        _cellContextMenu.Items.Add(_autoStackToStarshipMenuItem);
        _cellContextMenu.Items.Add(_autoStackToFreighterMenuItem);
        _cellContextMenu.Opening += OnContextMenuOpening;


        Controls.Add(splitContainer);
        ResumeLayout(false);
        DisableControlsOnInit();
        PerformLayout();
    }

    // Grid area
    private Panel _infoPanel = null!;
    private FlowLayoutPanel _toolbarPanel = null!;
    private Panel _gridContainer = null!;

    // Detail/editor panel controls
    private Panel _detailPanel = null!;
    private PictureBox _detailIcon = null!;
    private Label _detailItemName = null!;
    private Label _detailSlotPosition = null!;
    private Label _detailItemType = null!;
    private Label _detailItemCategory = null!;
    private TextBox _detailItemId = null!;
    private TextBox _detailSeedField = null!;
    private Label _detailSeedLabel = null!;
    private FlowLayoutPanel _detailIdSeedPanel = null!;
    private Button _detailGenSeedButton = null!;
    private NumericUpDown _detailAmount = null!;
    private NumericUpDown _detailMaxAmount = null!;
    private NumericUpDown _detailDamageFactor = null!;
    private Button _applyButton = null!;
    private Label _detailDescription = null!;
    private Label _detailAmountLabel = null!;
    private Label _detailMaxLabel = null!;
    private Label _detailNameLabel = null!;
    private Label _detailPositionLabel = null!;
    private Label _detailTypeLabel = null!;
    private Label _detailCategoryLabel = null!;
    private Label _detailItemIdLabel = null!;
    private Label _detailDamageLabel = null!;
    private Label _slotDetailHeader = null!;
    private Label _itemPickerHeader = null!;

    // Item picker controls
    private ComboBox _typeFilter = null!;
    private ComboBox _categoryFilter = null!;
    private ComboBox _itemPicker = null!;
    private TextBox _searchBox = null!;
    private Button _searchButton = null!;
    private Label _searchLabel = null!;
    private Label _typeFilterLabel = null!;
    private Label _categoryFilterLabel = null!;
    private Label _itemFilterLabel = null!;

    // Resize controls
    private NumericUpDown _resizeWidth = null!;
    private NumericUpDown _resizeHeight = null!;
    private Label _resizeWidthLabel = null!;
    private Label _resizeHeightLabel = null!;
    private Button _resizeButton = null!;
    private Label _sortModeLabel = null!;
    private ComboBox _sortModeCombo = null!;
    private ToolStrip _autoStackToolStrip = null!;
    private ToolStripDropDownButton _autoStackDropDownButton = null!;
    private ToolStripMenuItem _autoStackToChestsButtonMenuItem = null!;
    private ToolStripMenuItem _autoStackToStarshipButtonMenuItem = null!;
    private ToolStripMenuItem _autoStackToFreighterButtonMenuItem = null!;
    private Button _importButton = null!;
    private Button _exportButton = null!;

    // Context menu
    private ContextMenuStrip _cellContextMenu = null!;
    private ToolStripMenuItem _addItemMenuItem = null!;
    private ToolStripMenuItem _removeItemMenuItem = null!;
    private ToolStripMenuItem _enableSlotMenuItem = null!;
    private ToolStripMenuItem _enableAllSlotsMenuItem = null!;
    private ToolStripMenuItem _pinSlotMenuItem = null!;
    private ToolStripMenuItem _repairSlotMenuItem = null!;
    private ToolStripMenuItem _repairAllSlotsMenuItem = null!;
    private ToolStripMenuItem _superchargeSlotMenuItem = null!;
    private ToolStripMenuItem _superchargeAllSlotsMenuItem = null!;
    private ToolStripMenuItem _fillStackMenuItem = null!;
    private ToolStripMenuItem _rechargeAllTechMenuItem = null!;
    private ToolStripMenuItem _refillAllStacksMenuItem = null!;
    private ToolStripMenuItem _copyItemMenuItem = null!;
    private ToolStripMenuItem _pasteItemMenuItem = null!;
    private ToolStripMenuItem _sortByNameMenuItem = null!;
    private ToolStripMenuItem _sortByCategoryMenuItem = null!;
    private ToolStripMenuItem _autoStackToStorageMenuItem = null!;
    private ToolStripMenuItem _autoStackToStarshipMenuItem = null!;
    private ToolStripMenuItem _autoStackToFreighterMenuItem = null!;
}
