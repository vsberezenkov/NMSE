using NMSE.Data;
using NMSE.Models;
using NMSE.Core;

namespace NMSE.UI.Panels;

public partial class SettlementPanel : UserControl
{
    private static int[] StatMaxValues => SettlementLogic.StatMaxValues;
    private static string[] StatLabels => SettlementLogic.StatLabels;
    private const int StatCount = SettlementLogic.StatCount;
    private const int ProductionMaxAmount = SettlementLogic.ProductionMaxAmount;
    private const int PerkSlotCount = 6;

    // Filtered settlement data: indices into SettlementStatesV2
    private readonly List<int> _filteredIndices = new();
    private JsonArray? _settlements;
    private readonly Random _rng = new();
    private GameItemDatabase? _database;
    private IconManager? _iconManager;
    private Dictionary<string, string>? _allowedProductionItems;

    public void SetDatabase(GameItemDatabase? database)
    {
        _database = database;
        _allowedProductionItems = database != null
            ? SettlementLogic.BuildAllowedProductionItems(database)
            : null;
    }
    public void SetIconManager(IconManager? iconManager) => _iconManager = iconManager;

    public SettlementPanel()
    {
        InitializeComponent();
        SetupLayout();
    }

    private static Label AddRow(TableLayoutPanel layout, string label, Control field, int row)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 5, 10, 0) };
        layout.Controls.Add(lbl, 0, row);
        layout.Controls.Add(field, 1, row);
        return lbl;
    }

    // --- Perk ComboBox support ---

    private sealed class PerkComboItem
    {
        public SettlementPerk? Perk { get; }
        public string DisplayText { get; }

        public PerkComboItem(SettlementPerk? perk)
        {
            Perk = perk;
            DisplayText = perk == null
                ? "(None)"
                : $"{perk.Name} ({perk.StatEffectSummary})";
        }

        public override string ToString() => DisplayText;
    }

    private static void PopulatePerkCombo(ComboBox combo)
    {
        combo.Items.Add(new PerkComboItem(null));
        foreach (var perk in SettlementPerkDatabase.Perks)
            combo.Items.Add(new PerkComboItem(perk));
        combo.SelectedIndex = 0;
    }

    /// <summary>
    /// Repopulates the perk combo boxes from SettlementPerkDatabase.
    /// Must be called after LoadDatabase() since the panels are constructed
    /// before the JSON databases are loaded.
    /// </summary>
    public void RefreshPerkCombos()
    {
        if (_perkCombos == null) return;
        foreach (var combo in _perkCombos)
        {
            int prevIdx = combo.SelectedIndex;
            combo.Items.Clear();
            PopulatePerkCombo(combo);
            if (prevIdx >= 0 && prevIdx < combo.Items.Count)
                combo.SelectedIndex = prevIdx;
        }
    }

    private static void OnPerkComboDrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        var combo = (ComboBox)sender!;
        if (combo.Items[e.Index] is not PerkComboItem item) return;

        e.DrawBackground();
        Color textColor;
        if ((e.State & DrawItemState.Selected) != 0)
            textColor = SystemColors.HighlightText;
        else if (item.Perk == null)
            textColor = combo.ForeColor;
        else
            textColor = item.Perk.Beneficial ? Color.RoyalBlue : Color.IndianRed;

        var font = e.Font ?? combo.Font;
        using var brush = new SolidBrush(textColor);
        e.Graphics.DrawString(item.DisplayText, font, brush, e.Bounds);
        e.DrawFocusRectangle();
    }

    private void OnPerkChanged(int slot)
    {
        if (slot < 0 || slot >= PerkSlotCount) return;
        var item = _perkCombos[slot].SelectedItem as PerkComboItem;
        bool showSeed = item?.Perk?.Procedural == true;
        _perkSeedPanels[slot].Visible = showSeed;
        if (!showSeed)
            _perkSeedFields[slot].Text = "";
    }

    private void LoadPerkSlot(int slot, string raw)
    {
        if (string.IsNullOrEmpty(raw) || raw == "^")
        {
            _perkCombos[slot].SelectedIndex = 0;
            return;
        }

        string perkId = raw;
        string seed = "";
        int hashIdx = raw.IndexOf('#');
        if (hashIdx >= 0)
        {
            perkId = raw[..hashIdx];
            seed = raw[(hashIdx + 1)..];
        }

        if (SettlementPerkDatabase.ById.TryGetValue(perkId, out var perk))
        {
            for (int j = 0; j < _perkCombos[slot].Items.Count; j++)
            {
                if (_perkCombos[slot].Items[j] is PerkComboItem item && item.Perk?.Id == perk.Id)
                {
                    _perkCombos[slot].SelectedIndex = j;
                    break;
                }
            }

            if (perk.Procedural && !string.IsNullOrEmpty(seed))
            {
                _perkSeedPanels[slot].Visible = true;
                _perkSeedFields[slot].Text = seed;
            }
        }
        else
        {
            _perkCombos[slot].SelectedIndex = 0;
        }
    }

    // --- Delete Settlement ---

    private void OnDeleteSettlement(object? sender, EventArgs e)
    {
        if (_settlements == null || _settlementSelector.SelectedIndex < 0 || _filteredIndices.Count == 0) return;

        var result = MessageBox.Show(this, UiStrings.Get("settlement.delete_confirm"),
            UiStrings.Get("settlement.delete_title"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;

        int selIdx = _settlementSelector.SelectedIndex;
        int dataIdx = _filteredIndices[selIdx];
        if (dataIdx >= _settlements.Length) return;

        // Remove the settlement entry from the array
        SettlementLogic.RemoveSettlement(_settlements, dataIdx);

        // Adjust all filtered indices that were after the removed entry
        _filteredIndices.RemoveAt(selIdx);
        for (int i = 0; i < _filteredIndices.Count; i++)
        {
            if (_filteredIndices[i] > dataIdx)
                _filteredIndices[i]--;
        }

        _settlementSelector.Items.RemoveAt(selIdx);

        if (_settlementSelector.Items.Count > 0)
            _settlementSelector.SelectedIndex = Math.Min(selIdx, _settlementSelector.Items.Count - 1);
        else
            ClearFields();
    }

    // --- Load / Save / Clear ---

    public void LoadData(JsonObject saveData)
    {
        SuspendLayout();
        _settlementSelector.BeginUpdate();
        try
        {
        _settlementSelector.Items.Clear();
        _filteredIndices.Clear();
        ClearFields();
        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            _settlements = playerState.GetArray("SettlementStatesV2");
            if (_settlements == null || _settlements.Length == 0)
            {
                _infoLabel.Text = UiStrings.Get("settlement.no_settlements");
                return;
            }

            var filtered = SettlementLogic.FilterSettlements(saveData, playerState, _settlements);
            foreach (int i in filtered)
            {
                try
                {
                    _filteredIndices.Add(i);
                    var settlement = _settlements.GetObject(i);
                    string name = settlement.GetString("Name") ?? UiStrings.Format("settlement.fallback_name", i + 1);
                    _settlementSelector.Items.Add(name);
                }
                catch
                {
                    _filteredIndices.Add(i);
                    _settlementSelector.Items.Add(UiStrings.Format("settlement.fallback_name", i + 1));
                }
            }

            if (_settlementSelector.Items.Count > 0)
                _settlementSelector.SelectedIndex = 0;

            _infoLabel.Text = UiStrings.Format("settlement.found_count", _filteredIndices.Count);
        }
        catch { _infoLabel.Text = UiStrings.Get("settlement.load_failed"); }
        }
        finally
        {
            _settlementSelector.EndUpdate();
            ResumeLayout(true);
        }
    }

    public void SaveData(JsonObject saveData)
    {
        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            var settlements = playerState.GetArray("SettlementStatesV2");
            if (settlements == null || _settlementSelector.SelectedIndex < 0 || _filteredIndices.Count == 0) return;

            int dataIdx = _filteredIndices[_settlementSelector.SelectedIndex];
            if (dataIdx >= settlements.Length) return;

            var settlement = settlements.GetObject(dataIdx);

            var saveValues = new SettlementLogic.SettlementSaveValues
            {
                Name = _settlementName.Text,
                SeedValue = _seedField.Text,
                DecisionTypeIndex = _decisionTypeField.SelectedIndex,
                LastDecisionTime = _lastDecisionTimeField.Value,
            };
            for (int i = 0; i < StatCount; i++)
                saveValues.Stats[i] = (int)_statFields[i].Value;

            SettlementLogic.SaveSettlementData(settlement, saveValues);

            // Save perks
            var perksArr = settlement.GetArray("Perks");
            if (perksArr != null)
            {
                for (int i = 0; i < PerkSlotCount && i < perksArr.Length; i++)
                {
                    var item = _perkCombos[i].SelectedItem as PerkComboItem;
                    string val;
                    if (item?.Perk == null)
                    {
                        val = "^";
                    }
                    else if (item.Perk.Procedural && !string.IsNullOrEmpty(_perkSeedFields[i].Text))
                    {
                        val = $"{item.Perk.Id}#{_perkSeedFields[i].Text}";
                    }
                    else
                    {
                        val = item.Perk.Id;
                    }
                    perksArr.Set(i, val);
                }
            }

            // Save production state
            var prodArr = settlement.GetArray("ProductionState");
            if (prodArr != null)
            {
                for (int i = 0; i < _productionGrid.Rows.Count && i < prodArr.Length; i++)
                {
                    var prodObj = prodArr.GetObject(i);
                    var elementId = _productionGrid.Rows[i].Cells["ElementId"].Value?.ToString() ?? "";
                    prodObj.Set("ElementId", elementId);

                    if (int.TryParse(_productionGrid.Rows[i].Cells["Amount"].Value?.ToString(), out int amount))
                    {
                        amount = Math.Clamp(amount, 0, ProductionMaxAmount);
                        prodObj.Set("Amount", amount);
                    }
                }
            }
        }
        catch { }
    }

    private void OnSettlementSelected(object? sender, EventArgs e)
    {
        ClearFields();
        try
        {
            if (_settlements == null || _settlementSelector.SelectedIndex < 0 || _filteredIndices.Count == 0) return;
            int dataIdx = _filteredIndices[_settlementSelector.SelectedIndex];
            if (dataIdx >= _settlements.Length) return;

            var settlement = _settlements.GetObject(dataIdx);

            var sdata = SettlementLogic.LoadSettlementData(settlement);
            _settlementName.Text = sdata.Name;
            _seedField.Text = sdata.SeedValue;

            for (int i = 0; i < StatCount; i++)
                _statFields[i].Value = sdata.Stats[i];

            _decisionTypeField.SelectedIndex = sdata.DecisionTypeIndex;
            if (sdata.LastDecisionTime.HasValue)
                _lastDecisionTimeField.Value = sdata.LastDecisionTime.Value;
            else
                _lastDecisionTimeField.Value = _lastDecisionTimeField.MinDate;

            // Perks
            var perksArr = settlement.GetArray("Perks");
            if (perksArr != null)
            {
                for (int i = 0; i < PerkSlotCount && i < perksArr.Length; i++)
                {
                    try
                    {
                        string raw = perksArr.GetString(i) ?? "";
                        LoadPerkSlot(i, raw);
                    }
                    catch { _perkCombos[i].SelectedIndex = 0; }
                }
            }

            // Production state
            _productionGrid.Rows.Clear();
            var prodArr = settlement.GetArray("ProductionState");
            if (prodArr != null)
            {
                for (int i = 0; i < prodArr.Length; i++)
                {
                    try
                    {
                        var prodObj = prodArr.GetObject(i);
                        string elementId = prodObj.GetString("ElementId") ?? prodObj.Get("ElementId")?.ToString() ?? "";
                        // Strip "^" prefix for database lookup (save format: "^GAS1", db key: "GAS1")
                        string lookupId = elementId.StartsWith('^') ? elementId[1..] : elementId;
                        var dbItem = string.IsNullOrEmpty(lookupId) ? null : _database?.GetItem(lookupId);
                        string itemName = dbItem?.Name ?? lookupId;
                        Image? icon = GetProductionIcon(lookupId);
                        int amount = 0;
                        try { amount = prodObj.GetInt("Amount"); } catch { }
                        _productionGrid.Rows.Add(icon ?? (object)_placeholderIcon, itemName, elementId, amount);
                    }
                    catch { }
                }
            }
        }
        catch { }
    }

    private void OnSettlementNameChanged(object? sender, EventArgs e)
    {
        if (_settlementSelector.SelectedIndex < 0 || _settlementSelector.Items.Count == 0) return;
        string newName = string.IsNullOrWhiteSpace(_settlementName.Text)
            ? UiStrings.Format("settlement.fallback_name", _filteredIndices[_settlementSelector.SelectedIndex] + 1)
            : _settlementName.Text;
        int idx = _settlementSelector.SelectedIndex;
        _settlementSelector.SelectedIndexChanged -= OnSettlementSelected;
        _settlementSelector.Items.RemoveAt(idx);
        _settlementSelector.Items.Insert(idx, newName);
        _settlementSelector.SelectedIndex = idx;
        _settlementSelector.SelectedIndexChanged += OnSettlementSelected;
    }

    private void ClearFields()
    {
        _settlementName.Text = "";
        _seedField.Text = "";
        for (int i = 0; i < StatCount; i++)
            _statFields[i].Value = 0;
        _decisionTypeField.SelectedIndex = -1;
        _lastDecisionTimeField.Value = _lastDecisionTimeField.MinDate;
        for (int i = 0; i < PerkSlotCount; i++)
        {
            _perkCombos[i].SelectedIndex = 0;
            _perkSeedFields[i].Text = "";
            _perkSeedPanels[i].Visible = false;
        }
        _productionGrid.Rows.Clear();
    }

    // --- Production icon support ---

    private static readonly Bitmap _placeholderIcon = new(24, 24);
    private readonly Dictionary<string, Image> _scaledIconCache = new(StringComparer.OrdinalIgnoreCase);

    private Image? GetProductionIcon(string itemId)
    {
        if (_iconManager == null) return null;
        if (_scaledIconCache.TryGetValue(itemId, out var cached)) return cached;

        var icon = _iconManager.GetIconForItem(itemId, _database);
        if (icon == null) return null;

        var scaled = new Bitmap(24, 24);
        using (var g = Graphics.FromImage(scaled))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(icon, 0, 0, 24, 24);
        }
        _scaledIconCache[itemId] = scaled;
        return scaled;
    }

    private void OnProductionGridCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
        if (_productionGrid.Columns[e.ColumnIndex].Name != "ChangeElement") return;
        if (_database == null) return;

        var items = _database.Items.Values
            .Where(item =>
            {
                // If we have a curated allowed-production list, use it
                if (_allowedProductionItems is { Count: > 0 })
                    return _allowedProductionItems.ContainsKey(item.Id);

                // Fallback: broad filter by type
                return (string.Equals(item.ItemType, "Products", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(item.ItemType, "Raw Materials", StringComparison.OrdinalIgnoreCase))
                     && !GameItemDatabase.IsPickerExcluded(item.Id);
            })
            .OrderBy(item => item.Name)
            .Select(item =>
            {
                Image? icon = GetProductionIcon(item.Id);
                return (icon: (Image?)(icon ?? (Image)_placeholderIcon), name: item.Name, id: item.Id, category: item.ItemType);
            })
            .ToList();

        using var picker = new ItemPickerDialog("Select Production Element", items);
        if (picker.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(picker.SelectedId))
        {
            var gridRow = _productionGrid.Rows[e.RowIndex];
            string rawId = picker.SelectedId!;
            // Save format uses "^" prefix for element IDs (e.g. "^GAS1")
            string saveId = rawId.StartsWith('^') ? rawId : "^" + rawId;
            gridRow.Cells["ElementId"].Value = saveId;
            var dbItem = _database.GetItem(rawId);
            gridRow.Cells["ItemName"].Value = dbItem?.Name ?? rawId;
            gridRow.Cells["Icon"].Value = GetProductionIcon(rawId) ?? (object)_placeholderIcon;
        }
    }

    private void OnExportSettlement(object? sender, EventArgs e)
    {
        try
        {
            if (_settlements == null || _settlementSelector.SelectedIndex < 0 || _filteredIndices.Count == 0) return;
            int dataIdx = _filteredIndices[_settlementSelector.SelectedIndex];
            if (dataIdx >= _settlements.Length) return;

            var settlement = _settlements.GetObject(dataIdx);
            var cfg = ExportConfig.Instance;
            var vars = new Dictionary<string, string>
            {
                ["settlement_name"] = _settlementName.Text ?? "",
                ["seed"] = _seedField.Text ?? ""
            };

            using var dialog = new SaveFileDialog
            {
                Filter = ExportConfig.BuildDialogFilter(cfg.SettlementExt, "Settlement files"),
                DefaultExt = cfg.SettlementExt.TrimStart('.'),
                FileName = ExportConfig.BuildFileName(cfg.SettlementTemplate, cfg.SettlementExt, vars)
            };

            if (dialog.ShowDialog() == DialogResult.OK)
                settlement.ExportToFile(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(UiStrings.Format("settlement.export_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnImportSettlement(object? sender, EventArgs e)
    {
        try
        {
            if (_settlements == null) return;

            var cfg = ExportConfig.Instance;
            using var dialog = new OpenFileDialog
            {
                Filter = ExportConfig.BuildImportFilter(cfg.SettlementExt, "Settlement files", ".stl")
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            var imported = JsonObject.ImportFromFile(dialog.FileName);

            // Unwrap NomNom wrapper if present (Data -> Settlement)
            imported = InventoryImportHelper.UnwrapNomNom(imported, "Settlement");

            int selectedDataIdx = (_settlementSelector.SelectedIndex >= 0 && _filteredIndices.Count > 0)
                ? _filteredIndices[_settlementSelector.SelectedIndex]
                : -1;

            int target = SettlementLogic.FindImportTargetIndex(_settlements, selectedDataIdx);

            if (target == -2)
            {
                // Array full and no selection – ask user which slot to overwrite
                target = ShowSlotPickerDialog();
                if (target < 0) return; // cancelled
            }

            if (target == -1)
            {
                // Append new entry
                _settlements.Add(imported);
                int newIdx = _settlements.Length - 1;
                _filteredIndices.Add(newIdx);
                string name = imported.GetString("Name") ?? UiStrings.Format("settlement.fallback_name", newIdx + 1);
                _settlementSelector.Items.Add(name);
                _settlementSelector.SelectedIndex = _settlementSelector.Items.Count - 1;
            }
            else
            {
                // Overwrite existing entry
                var settlement = _settlements.GetObject(target);
                foreach (var name in imported.Names())
                    settlement.Set(name, imported.Get(name));

                OnSettlementSelected(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(UiStrings.Format("settlement.import_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Shows a dialog letting the user pick which settlement slot to overwrite
    /// when the array is full and no settlement is currently selected.
    /// </summary>
    private int ShowSlotPickerDialog()
    {
        if (_settlements == null) return -1;

        using var form = new Form
        {
            Text = UiStrings.Get("settlement.slot_picker_title"),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ClientSize = new Size(400, 140)
        };

        var label = new Label
        {
            Text = UiStrings.Get("settlement.slot_picker_message"),
            AutoSize = true,
            Location = new Point(12, 12)
        };

        var combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(12, 40),
            Width = 370
        };

        for (int i = 0; i < _settlements.Length; i++)
        {
            try
            {
                var s = _settlements.GetObject(i);
                string name = s.GetString("Name") ?? "";
                combo.Items.Add(string.IsNullOrEmpty(name) ? UiStrings.Format("settlement.slot_label", i + 1) : UiStrings.Format("settlement.slot_label_named", i + 1, name));
            }
            catch
            {
                combo.Items.Add(UiStrings.Format("settlement.slot_label", i + 1));
            }
        }
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;

        var okBtn = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(220, 80), Width = 75 };
        var cancelBtn = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(305, 80), Width = 75 };
        form.AcceptButton = okBtn;
        form.CancelButton = cancelBtn;

        form.Controls.AddRange(new Control[] { label, combo, okBtn, cancelBtn });

        return form.ShowDialog(this) == DialogResult.OK && combo.SelectedIndex >= 0
            ? combo.SelectedIndex
            : -1;
    }

    private static readonly string[] StatLocKeys =
    {
        "settlement.population", "settlement.happiness", "settlement.productivity",
        "settlement.upkeep", "settlement.sentinels", "settlement.debt",
        "settlement.alert", "settlement.bug_attack"
    };

    public void ApplyUiLocalisation()
    {
        _titleLabel.Text = UiStrings.Get("settlement.title");
        _deleteSettlementBtn.Text = UiStrings.Get("settlement.delete");
        _exportSettlementBtn.Text = UiStrings.Get("common.export");
        _importSettlementBtn.Text = UiStrings.Get("common.import");
        _generateSeedBtn.Text = UiStrings.Get("common.generate");

        // Form row labels
        _nameLabel.Text = UiStrings.Get("settlement.name");
        _seedLabel.Text = UiStrings.Get("settlement.seed");
        for (int i = 0; i < _perkLabels.Length; i++)
            _perkLabels[i].Text = UiStrings.Format("settlement.perk", i + 1);
        _decisionTypeLabel.Text = UiStrings.Get("settlement.decision_type");
        _lastDecisionLabel.Text = UiStrings.Get("settlement.last_decision");

        // Stat row labels
        for (int i = 0; i < StatCount && i < StatLocKeys.Length; i++)
            _statRowLabels[i].Text = UiStrings.Get(StatLocKeys[i]);

        // Production section
        _productionHeaderLabel.Text = UiStrings.Get("settlement.production_header");

        // Production grid column headers
        if (_productionGrid.Columns["Name"] is DataGridViewColumn nameCol) nameCol.HeaderText = UiStrings.Get("settlement.col_name");
        if (_productionGrid.Columns["ElementID"] is DataGridViewColumn elemCol) elemCol.HeaderText = UiStrings.Get("settlement.col_element_id");
        if (_productionGrid.Columns["Edit"] is DataGridViewColumn editCol) editCol.HeaderText = UiStrings.Get("settlement.col_edit");
        if (_productionGrid.Columns["Amount"] is DataGridViewColumn amtCol) amtCol.HeaderText = UiStrings.Get("settlement.col_amount");

        // Refresh decision type combo with localised display names
        RefreshDecisionTypeCombo();
    }

    private void RefreshDecisionTypeCombo()
    {
        int savedIdx = _decisionTypeField.SelectedIndex;
        _decisionTypeField.BeginUpdate();
        _decisionTypeField.Items.Clear();
        foreach (var dt in SettlementLogic.DecisionTypes)
        {
            if (SettlementLogic.DecisionTypeLocKeys.TryGetValue(dt, out var key))
                _decisionTypeField.Items.Add(UiStrings.Get(key));
            else
                _decisionTypeField.Items.Add(dt);
        }
        if (savedIdx >= 0 && savedIdx < _decisionTypeField.Items.Count)
            _decisionTypeField.SelectedIndex = savedIdx;
        _decisionTypeField.EndUpdate();
    }
}
