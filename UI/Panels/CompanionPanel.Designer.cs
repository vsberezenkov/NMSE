#nullable enable
using NMSE.Core;
using NMSE.Data;
using NMSE.UI.Util;
using System.Diagnostics;

namespace NMSE.UI.Panels;

partial class CompanionPanel
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
        // CompanionPanel
        //
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.DoubleBuffered = true;
        this.ResumeLayout(false);
    }
    #endregion

    private void SetupLayout()
    {
        // -- Main two-column layout: list (left) | detail (right) --
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(10)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // -- Left column: title + Creature Builder button + listbox + buttons --
        var leftLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };
        leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _titleLabel = new Label
        {
            Text = "Companions",
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 5)
        };
        FontManager.ApplyHeadingFont(_titleLabel, 14);
        leftLayout.Controls.Add(_titleLabel, 0, 0);

        _creatureBuilderBtn = new Button { Text = "Creature Builder (Web)", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill };
        _creatureBuilderBtn.Click += (s, e) =>
        {
            try { Process.Start(new ProcessStartInfo("https://creature.nmscd.com/#/builder") { UseShellExecute = true }); }
            catch { }
        };
        leftLayout.Controls.Add(_creatureBuilderBtn, 0, 1);

        _companionList = new ListBox { Dock = DockStyle.Fill };
        _companionList.SelectedIndexChanged += OnCompanionSelected;
        leftLayout.Controls.Add(_companionList, 0, 2);

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight
        };
        _countLabel = new Label { Text = "Total: 0 slots", AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 5, 0, 0) };
        _deleteBtn = new Button { Text = "Delete", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(75, 0) };
        _deleteBtn.Click += OnDelete;
        _exportCompanionBtn = new Button { Text = "Export", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(75, 0) };
        _exportCompanionBtn.Click += OnExport;
        _importCompanionBtn = new Button { Text = "Import", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(75, 0) };
        _importCompanionBtn.Click += OnImport;
        _resetAccessoryBtn = new Button { Text = "Reset Accessory", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _resetAccessoryBtn.Click += OnResetAccessory;
        btnPanel.Controls.Add(_deleteBtn);
        btnPanel.Controls.Add(_exportCompanionBtn);
        btnPanel.Controls.Add(_importCompanionBtn);
        btnPanel.Controls.Add(_resetAccessoryBtn);
        btnPanel.Controls.Add(_countLabel);
        leftLayout.Controls.Add(btnPanel, 0, 3);

        mainLayout.Controls.Add(leftLayout, 0, 0);

        // -- Right column: detail panel with two-column form --
        _detailPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Visible = false };

        _formLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
        };
        _formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _formLayout.RowCount = 1;
        _formLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Left detail column: Identity & Properties
        _leftColumn = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(0, 0, 8, 0),
        };
        _leftColumn.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _leftColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Right detail column: Seeds, Traits & Moods
        _rightColumn = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(8, 0, 0, 0),
        };
        _rightColumn.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _rightColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _formLayout.Controls.Add(_leftColumn, 0, 0);
        _formLayout.Controls.Add(_rightColumn, 1, 0);

        // -- Create all field controls --

        _typeField = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        foreach (var entry in CompanionDatabase.Entries)
            _typeField.Items.Add(entry);
        _typeField.SelectedIndexChanged += (s, e) => { if (!_loading) { WriteType(); ReloadDescriptorsForCurrentCompanion(); } };

        _nameField = new TextBox { Dock = DockStyle.Fill };
        _nameField.Leave += (s, e) => WriteName();

        _creatureSeedField = new TextBox { Dock = DockStyle.Fill };
        _creatureSeedField.Leave += (s, e) => WriteCreatureSeed();

        _secondarySeedField = new TextBox { Dock = DockStyle.Fill };
        _secondarySeedField.Leave += (s, e) => WriteSecondarySeed();

        _speciesSeedField = new TextBox { Dock = DockStyle.Fill };
        _speciesSeedField.Leave += (s, e) => WriteSpeciesSeed();

        _genusSeedField = new TextBox { Dock = DockStyle.Fill };
        _genusSeedField.Leave += (s, e) => WriteGenusSeed();

        _predatorField = new CheckBox { Text = "", AutoSize = true };
        _predatorField.CheckedChanged += (s, e) => { if (!_loading) WritePredator(); };

        _biomeField = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _biomeField.Items.AddRange(CompanionLogic.BiomeTypes);
        _biomeField.SelectedIndexChanged += (s, e) => { if (!_loading) WriteBiome(); };

        _creatureTypeField = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _creatureTypeField.Items.AddRange(CompanionDatabase.CreatureTypes);
        _creatureTypeField.SelectedIndexChanged += (s, e) => { if (!_loading) WriteCreatureType(); };

        _scaleField = new TextBox { Dock = DockStyle.Fill };
        _scaleField.Leave += (s, e) => WriteScale();

        _trustField = new TextBox { Dock = DockStyle.Fill };
        _trustField.Leave += (s, e) => WriteTrust();

        _boneScaleSeedField = new TextBox { Dock = DockStyle.Fill };
        _boneScaleSeedField.Leave += (s, e) => WriteBoneScaleSeed();

        _colourBaseSeedField = new TextBox { Dock = DockStyle.Fill };
        _colourBaseSeedField.Leave += (s, e) => WriteColourBaseSeed();

        _hasFurField = new CheckBox { Text = "", AutoSize = true };
        _hasFurField.CheckedChanged += (s, e) => { if (!_loading) WriteHasFur(); };

        _helpfulnessField = new TextBox { Dock = DockStyle.Fill };
        _helpfulnessField.Leave += (s, e) => WriteTrait(0, _helpfulnessField);

        _aggressionField = new TextBox { Dock = DockStyle.Fill };
        _aggressionField.Leave += (s, e) => WriteTrait(1, _aggressionField);

        _independenceField = new TextBox { Dock = DockStyle.Fill };
        _independenceField.Leave += (s, e) => WriteTrait(2, _independenceField);

        _hungryField = new TextBox { Dock = DockStyle.Fill };
        _hungryField.Leave += (s, e) => WriteMood(0, _hungryField);

        _lonelyField = new TextBox { Dock = DockStyle.Fill };
        _lonelyField.Leave += (s, e) => WriteMood(1, _lonelyField);

        // Slot Unlocked checkbox
        _unlockedCheck = new CheckBox { Text = "", AutoSize = true };
        _unlockedCheck.CheckedChanged += (s, e) =>
        {
            if (_loading || _playerState == null) return;
            int idx = _companionList.SelectedIndex;
            if (idx < 0 || idx >= _entries.Count) return;
            var entry = _entries[idx];
            if (entry.Source != "Pet") return;
            CompanionLogic.SetSlotUnlocked(_playerState, entry.OriginalIndex, _unlockedCheck.Checked);
            RefreshListEntry();
        };

        // DateTimePickers
        _birthTimePicker = new DateTimePicker
        {
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss",
            ShowUpDown = true,
        };
        _birthTimePicker.ValueChanged += (s, e) =>
        {
            if (_loading) return;
            var comp = SelectedCompanion;
            if (comp == null) return;
            try
            {
                long unix = ((DateTimeOffset)_birthTimePicker.Value.ToUniversalTime()).ToUnixTimeSeconds();
                comp.Set("BirthTime", unix);
            }
            catch { }
        };

        _lastEggTimePicker = new DateTimePicker
        {
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss",
            ShowUpDown = true,
        };
        _lastEggTimePicker.ValueChanged += (s, e) =>
        {
            if (_loading) return;
            var comp = SelectedCompanion;
            if (comp == null) return;
            try
            {
                long unix = ((DateTimeOffset)_lastEggTimePicker.Value.ToUniversalTime()).ToUnixTimeSeconds();
                comp.Set("LastEggTime", unix);
            }
            catch { }
        };

        _customSpeciesNameField = new TextBox { Dock = DockStyle.Fill };
        _customSpeciesNameField.Leave += (s, e) => WriteCustomSpeciesName();

        _eggModifiedField = new CheckBox { Text = "", AutoSize = true };
        _eggModifiedField.CheckedChanged += (s, e) => { if (!_loading) WriteEggModified(); };

        _hasBeenSummonedField = new CheckBox { Text = "", AutoSize = true };
        _hasBeenSummonedField.CheckedChanged += (s, e) => { if (!_loading) WriteHasBeenSummoned(); };

        _allowUnmodifiedRerollField = new CheckBox { Text = "", AutoSize = true };
        _allowUnmodifiedRerollField.CheckedChanged += (s, e) => { if (!_loading) WriteAllowUnmodifiedReroll(); };

        _uaField = new TextBox { Dock = DockStyle.Fill };
        _uaField.Leave += (s, e) => WriteUA();

        _lastTrustIncreaseTimePicker = new DateTimePicker
        {
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss",
            ShowUpDown = true,
        };
        _lastTrustIncreaseTimePicker.ValueChanged += (s, e) =>
        {
            if (_loading) return;
            var comp = SelectedCompanion;
            if (comp == null) return;
            try
            {
                long unix = ((DateTimeOffset)_lastTrustIncreaseTimePicker.Value.ToUniversalTime()).ToUnixTimeSeconds();
                comp.Set("LastTrustIncreaseTime", unix);
            }
            catch { }
        };

        _lastTrustDecreaseTimePicker = new DateTimePicker
        {
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss",
            ShowUpDown = true,
        };
        _lastTrustDecreaseTimePicker.ValueChanged += (s, e) =>
        {
            if (_loading) return;
            var comp = SelectedCompanion;
            if (comp == null) return;
            try
            {
                long unix = ((DateTimeOffset)_lastTrustDecreaseTimePicker.Value.ToUniversalTime()).ToUnixTimeSeconds();
                comp.Set("LastTrustDecreaseTime", unix);
            }
            catch { }
        };

        _descriptorPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 5, 0, 5),
        };

        // -- Left column rows: Identity & Properties --
        int lRow = 0;
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _leftColumn.Controls.Add(new Label { Text = "", Height = 30 }, 0, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _slotUnlockedLabel = AddRow(_leftColumn, "Slot Unlocked:", _unlockedCheck, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _speciesLabel = AddRow(_leftColumn, "Species:", _typeField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _nameLabel = AddRow(_leftColumn, "Name:", _nameField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _typeLabel = AddRow(_leftColumn, "Type:", _creatureTypeField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _biomeLabel = AddRow(_leftColumn, "Biome:", _biomeField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _predatorLabel = AddRow(_leftColumn, "Predator:", _predatorField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _hasFurLabel = AddRow(_leftColumn, "Has Fur:", _hasFurField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _scaleLabel = AddRow(_leftColumn, "Scale:", _scaleField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _trustLabel = AddRow(_leftColumn, "Trust:", _trustField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _birthTimeLabel = AddRow(_leftColumn, "Birth Time:", _birthTimePicker, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _lastEggTimeLabel = AddRow(_leftColumn, "Last Egg Time:", _lastEggTimePicker, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _customSpeciesNameLabel = AddRow(_leftColumn, "Custom Species Name:", _customSpeciesNameField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _eggModifiedLabel = AddRow(_leftColumn, "Egg Modified:", _eggModifiedField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _summonedLabel = AddRow(_leftColumn, "Summoned:", _hasBeenSummonedField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _allowRerollLabel = AddRow(_leftColumn, "Allow Reroll:", _allowUnmodifiedRerollField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _uaLabel = AddRow(_leftColumn, "UA:", _uaField, lRow++);
        // Blank spacer row at bottom of left column for alignment
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _leftColumn.Controls.Add(new Label { Text = "", AutoSize = true }, 0, lRow++);

        // -- Right column rows: Seeds, Traits & Moods --
        int rRow = 0;
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rightColumn.Controls.Add(new Label { Text = "", Height = 52 }, 0, rRow++);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _creatureSeedLabel = AddSeedRow(_rightColumn, "Creature Seed:", _creatureSeedField, rRow++, WriteCreatureSeed);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _secondarySeedLabel = AddSeedRow(_rightColumn, "Secondary Seed:", _secondarySeedField, rRow++, WriteSecondarySeed);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _speciesSeedLabel = AddSeedRow(_rightColumn, "Species Seed:", _speciesSeedField, rRow++, WriteSpeciesSeed);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _genusSeedLabel = AddSeedRow(_rightColumn, "Genus Seed:", _genusSeedField, rRow++, WriteGenusSeed);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _boneScaleSeedLabel = AddSeedRow(_rightColumn, "Bone Scale Seed:", _boneScaleSeedField, rRow++, WriteBoneScaleSeed);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _colourBaseSeedLabel = AddSeedRow(_rightColumn, "Colour Base Seed:", _colourBaseSeedField, rRow++, WriteColourBaseSeed);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _helpfulnessLabel = AddRow(_rightColumn, "Helpfulness:", _helpfulnessField, rRow++);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _aggressionLabel = AddRow(_rightColumn, "Aggression:", _aggressionField, rRow++);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _independenceLabel = AddRow(_rightColumn, "Independence:", _independenceField, rRow++);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _hungryLabel = AddRow(_rightColumn, "Hungry:", _hungryField, rRow++);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _lonelyLabel = AddRow(_rightColumn, "Lonely:", _lonelyField, rRow++);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _trustIncreaseLabel = AddRow(_rightColumn, "Trust Increase:", _lastTrustIncreaseTimePicker, rRow++);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _trustDecreaseLabel = AddRow(_rightColumn, "Trust Decrease:", _lastTrustDecreaseTimePicker, rRow++);
        // Blank spacer row at bottom of right column for alignment
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rightColumn.Controls.Add(new Label { Text = "", AutoSize = true }, 0, rRow++);

        _descriptorsHeading = new Label
        {
            Text = "Descriptors (Parts)",
            AutoSize = true,
            Padding = new Padding(0, 10, 0, 5)
        };
        FontManager.ApplyHeadingFont(_descriptorsHeading, 11);

        var innerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 3,
        };
        innerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        innerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        innerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        innerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        innerLayout.Controls.Add(_formLayout, 0, 0);
        innerLayout.Controls.Add(_descriptorsHeading, 0, 1);
        innerLayout.Controls.Add(_descriptorPanel, 0, 2);

        _detailPanel.Controls.Add(innerLayout);
        mainLayout.Controls.Add(_detailPanel, 1, 0);

        Controls.Add(mainLayout);
        ResumeLayout(false);
        PerformLayout();
    }

    private ListBox _companionList = null!;
    private Label _countLabel = null!;
    private Panel _detailPanel = null!;
    private TableLayoutPanel _formLayout = null!;
    private TableLayoutPanel _leftColumn = null!;
    private TableLayoutPanel _rightColumn = null!;
    private Button _deleteBtn = null!;
    private Button _creatureBuilderBtn = null!;
    private ComboBox _typeField = null!;
    private TextBox _nameField = null!;
    private TextBox _creatureSeedField = null!;
    private TextBox _secondarySeedField = null!;
    private TextBox _speciesSeedField = null!;
    private TextBox _genusSeedField = null!;
    private CheckBox _predatorField = null!;
    private ComboBox _biomeField = null!;
    private ComboBox _creatureTypeField = null!;
    private TextBox _scaleField = null!;
    private TextBox _trustField = null!;
    private TextBox _boneScaleSeedField = null!;
    private TextBox _colourBaseSeedField = null!;
    private CheckBox _hasFurField = null!;
    private TextBox _helpfulnessField = null!;
    private TextBox _aggressionField = null!;
    private TextBox _independenceField = null!;
    private TextBox _hungryField = null!;
    private TextBox _lonelyField = null!;
    private DateTimePicker _birthTimePicker = null!;
    private DateTimePicker _lastEggTimePicker = null!;
    private CheckBox _unlockedCheck = null!;
    private TextBox _customSpeciesNameField = null!;
    private CheckBox _eggModifiedField = null!;
    private CheckBox _hasBeenSummonedField = null!;
    private CheckBox _allowUnmodifiedRerollField = null!;
    private TextBox _uaField = null!;
    private DateTimePicker _lastTrustIncreaseTimePicker = null!;
    private DateTimePicker _lastTrustDecreaseTimePicker = null!;
    private FlowLayoutPanel _descriptorPanel = null!;
    private Label _titleLabel = null!;
    private Label _descriptorsHeading = null!;

    // Left column labels
    private Label _slotUnlockedLabel = null!;
    private Label _speciesLabel = null!;
    private Label _nameLabel = null!;
    private Label _typeLabel = null!;
    private Label _biomeLabel = null!;
    private Label _predatorLabel = null!;
    private Label _hasFurLabel = null!;
    private Label _scaleLabel = null!;
    private Label _trustLabel = null!;
    private Label _birthTimeLabel = null!;
    private Label _lastEggTimeLabel = null!;
    private Label _customSpeciesNameLabel = null!;
    private Label _eggModifiedLabel = null!;
    private Label _summonedLabel = null!;
    private Label _allowRerollLabel = null!;
    private Label _uaLabel = null!;

    // Right column seed labels
    private Label _creatureSeedLabel = null!;
    private Label _secondarySeedLabel = null!;
    private Label _speciesSeedLabel = null!;
    private Label _genusSeedLabel = null!;
    private Label _boneScaleSeedLabel = null!;
    private Label _colourBaseSeedLabel = null!;

    // Right column trait & mood labels
    private Label _helpfulnessLabel = null!;
    private Label _aggressionLabel = null!;
    private Label _independenceLabel = null!;
    private Label _hungryLabel = null!;
    private Label _lonelyLabel = null!;
    private Label _trustIncreaseLabel = null!;
    private Label _trustDecreaseLabel = null!;

    // Button fields for localisation
    private Button _exportCompanionBtn = null!;
    private Button _importCompanionBtn = null!;
    private Button _resetAccessoryBtn = null!;

    // Seed "Gen" buttons created by AddSeedRow - stored for re-localisation
    private readonly List<Button> _seedGenButtons = new();
    // Regen Descriptor ID button - stored for re-localisation
    private Button? _regenDescriptorBtn;
}
