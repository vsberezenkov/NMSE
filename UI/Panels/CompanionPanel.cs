using NMSE.Data;
using NMSE.Models;
using NMSE.Core;

namespace NMSE.UI.Panels;

public partial class CompanionPanel : UserControl
{
    private readonly Random _rng = new();

    private JsonObject? _playerState;
    private readonly List<(JsonObject Companion, string Label, string Source, int OriginalIndex, bool IsEmpty)> _entries = new();
    private bool _loading;

    public CompanionPanel()
    {
        InitializeComponent();
        SetupLayout();
    }

    private static Label AddRow(TableLayoutPanel layout, string label, Control field, int row)
    {
        var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 5, 10, 0) };
        layout.Controls.Add(lbl, 0, row);
        layout.Controls.Add(field, 1, row);
        return lbl;
    }

    private Label AddSeedRow(TableLayoutPanel layout, string label, TextBox seedField, int row, Action? onSeedChanged = null)
    {
        var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 5, 10, 0) };
        layout.Controls.Add(lbl, 0, row);

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        seedField.Dock = DockStyle.Fill;
        panel.Controls.Add(seedField, 0, 0);

        var genBtn = new Button { Text = UiStrings.Get("companion.gen"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(40, 23) };
        genBtn.Click += (s, e) =>
        {
            byte[] bytes = new byte[8];
            _rng.NextBytes(bytes);
            seedField.Text = "0x" + BitConverter.ToString(bytes).Replace("-", "");
            onSeedChanged?.Invoke();
        };
        _seedGenButtons.Add(genBtn);
        panel.Controls.Add(genBtn, 1, 0);

        layout.Controls.Add(panel, 1, row);
        return lbl;
    }

    public void LoadData(JsonObject saveData)
    {
        _loading = true;
        SuspendLayout();
        _companionList.BeginUpdate();
        try
        {
            _companionList.Items.Clear();
            _entries.Clear();
            _detailPanel.Visible = false;
            _playerState = null;

            try
            {
                var playerState = saveData.GetObject("PlayerStateData");
                if (playerState == null) return;
                _playerState = playerState;

                LoadAllSlots(playerState.GetArray("Pets"), "Pet");
                LoadAllSlots(playerState.GetArray("Eggs"), "Egg");

                _countLabel.Text = UiStrings.Format("companion.total_slots", _entries.Count);

                if (_entries.Count > 0 && _companionList.Items.Count > 0)
                    _companionList.SelectedIndex = 0;
            }
            catch { }
        }
        finally
        {
            _companionList.EndUpdate();
            ResumeLayout(true);
            _loading = false;
        }
    }

    /// <summary>
    /// Loads ALL slots from the array (including empty ones).
    /// </summary>
    private void LoadAllSlots(JsonArray? array, string prefix)
    {
        if (array == null) return;
        for (int i = 0; i < array.Length; i++)
        {
            try
            {
                var comp = array.GetObject(i);

                bool occupied = IsSlotOccupied(comp);

                bool locked = false;
                if (prefix == "Pet" && _playerState != null)
                {
                    var unlockedSlots = _playerState.GetArray("UnlockedPetSlots");
                    locked = unlockedSlots == null || i >= unlockedSlots.Length || !unlockedSlots.GetBool(i);
                }

                string label = GetSlotLabel(comp, prefix, i, occupied, locked);
                _entries.Add((comp, label, prefix, i, !occupied));
                _companionList.Items.Add(label);
            }
            catch
            {
                string errLabel = UiStrings.Format("companion.error_format", prefix, i);
                _entries.Add((new JsonObject(), errLabel, prefix, i, true));
                _companionList.Items.Add(errLabel);
            }
        }
    }

    private static bool IsSlotOccupied(JsonObject comp)
    {
        try
        {
            var seedArray = comp.GetArray("CreatureSeed");
            if (seedArray != null && seedArray.Length >= 2)
                return seedArray.GetBool(0);
        }
        catch { }
        return false;
    }

    private static string GetSlotLabel(JsonObject comp, string prefix, int index, bool occupied, bool locked)
    {
        if (!occupied)
        {
            return locked ? $"{prefix} {index} (Locked)" : $"{prefix} {index} (Empty)";
        }

        string customName = "";
        try { customName = comp.GetString("CustomName") ?? ""; } catch { }
        if (string.IsNullOrEmpty(customName) || customName == "^")
            return $"{prefix} {index}";
        return $"{prefix} {index} - {customName}";
    }

    /// <summary>
    /// Returns the companion JsonObject for the currently selected slot.
    /// Returns the object even for empty/blank slots so the user can populate them from scratch.
    /// </summary>
    private JsonObject? SelectedCompanion
    {
        get
        {
            int idx = _companionList.SelectedIndex;
            if (idx < 0 || idx >= _entries.Count) return null;
            return _entries[idx].Companion;
        }
    }

    /// <summary>
    /// Returns the raw entry at the current list index (even for empty slots).
    /// </summary>
    private (JsonObject Companion, string Label, string Source, int OriginalIndex, bool IsEmpty) SelectedEntry
    {
        get
        {
            int idx = _companionList.SelectedIndex;
            if (idx < 0 || idx >= _entries.Count)
                return (new JsonObject(), "", "", -1, true);
            return _entries[idx];
        }
    }

    private void OnCompanionSelected(object? sender, EventArgs e)
    {
        int idx = _companionList.SelectedIndex;
        if (idx < 0 || idx >= _entries.Count)
        {
            _detailPanel.Visible = false;
            return;
        }

        var entry = _entries[idx];
        _loading = true;
        try
        {
            _detailPanel.Visible = true;

            _deleteBtn.Enabled = !entry.IsEmpty;

            var comp = entry.Companion;

            // Type
            string creatureId = comp.GetString("CreatureID") ?? "";
            int typeIdx = -1;
            for (int i = 0; i < _typeField.Items.Count; i++)
            {
                var ce = _typeField.Items[i] as CompanionEntry;
                if (ce != null && string.Equals(ce.Id, creatureId, StringComparison.OrdinalIgnoreCase))
                { typeIdx = i; break; }
            }
            _typeField.SelectedIndex = typeIdx;

            // Name
            _nameField.Text = comp.GetString("CustomName") ?? "";

            // Creature Seed
            try
            {
                var seedArr = comp.GetArray("CreatureSeed");
                _creatureSeedField.Text = seedArr != null && seedArr.Length >= 2 ? seedArr.GetString(1) ?? "" : "";
            }
            catch { _creatureSeedField.Text = ""; }

            // Secondary Seed
            try
            {
                var secArr = comp.GetArray("CreatureSecondarySeed");
                _secondarySeedField.Text = secArr != null && secArr.Length >= 2 ? secArr.GetString(1) ?? "" : "";
            }
            catch { _secondarySeedField.Text = ""; }

            // Species Seed
            _speciesSeedField.Text = comp.GetString("SpeciesSeed") ?? "";

            // Genus Seed
            _genusSeedField.Text = comp.GetString("GenusSeed") ?? "";

            // Predator
            try { _predatorField.Checked = comp.GetBool("Predator"); } catch { _predatorField.Checked = false; }

            // Biome
            try
            {
                var biomeObj = comp.GetObject("Biome");
                string biome = biomeObj?.GetString("Biome") ?? "";
                _biomeField.SelectedItem = biome;
            }
            catch { _biomeField.SelectedIndex = -1; }

            // CreatureType
            try
            {
                var ctObj = comp.GetObject("CreatureType");
                string ct = ctObj?.GetString("CreatureType") ?? "";
                _creatureTypeField.SelectedItem = ct;
            }
            catch { _creatureTypeField.SelectedIndex = -1; }

            // Scale
            try { _scaleField.Text = comp.GetDouble("Scale").ToString(); } catch { _scaleField.Text = ""; }

            // Trust
            try { _trustField.Text = comp.GetDouble("Trust").ToString(); } catch { _trustField.Text = ""; }

            // Bone Scale Seed
            try
            {
                var bsArr = comp.GetArray("BoneScaleSeed");
                _boneScaleSeedField.Text = bsArr != null && bsArr.Length >= 2 ? bsArr.GetString(1) ?? "" : "";
            }
            catch { _boneScaleSeedField.Text = ""; }

            // Colour Base Seed
            try
            {
                var cbArr = comp.GetArray("ColourBaseSeed");
                _colourBaseSeedField.Text = cbArr != null && cbArr.Length >= 2 ? cbArr.GetString(1) ?? "" : "";
            }
            catch { _colourBaseSeedField.Text = ""; }

            // Has Fur
            try { _hasFurField.Checked = comp.GetBool("HasFur"); } catch { _hasFurField.Checked = false; }

            // Traits
            try
            {
                var traits = comp.GetArray("Traits");
                _helpfulnessField.Text = traits != null && traits.Length > 0 ? traits.GetDouble(0).ToString() : "0";
                _aggressionField.Text = traits != null && traits.Length > 1 ? traits.GetDouble(1).ToString() : "0";
                _independenceField.Text = traits != null && traits.Length > 2 ? traits.GetDouble(2).ToString() : "0";
            }
            catch
            {
                _helpfulnessField.Text = "0";
                _aggressionField.Text = "0";
                _independenceField.Text = "0";
            }

            // Moods
            try
            {
                var moods = comp.GetArray("Moods");
                _hungryField.Text = moods != null && moods.Length > 0 ? moods.GetDouble(0).ToString() : "0";
                _lonelyField.Text = moods != null && moods.Length > 1 ? moods.GetDouble(1).ToString() : "0";
            }
            catch
            {
                _hungryField.Text = "0";
                _lonelyField.Text = "0";
            }

            // BirthTime
            try
            {
                long birthUnix = comp.GetLong("BirthTime");
                _birthTimePicker.Value = DateTimeOffset.FromUnixTimeSeconds(birthUnix).ToLocalTime().DateTime;
            }
            catch { _birthTimePicker.Value = DateTime.Now; }

            // LastEggTime
            try
            {
                long eggUnix = comp.GetLong("LastEggTime");
                _lastEggTimePicker.Value = DateTimeOffset.FromUnixTimeSeconds(eggUnix).ToLocalTime().DateTime;
            }
            catch { _lastEggTimePicker.Value = DateTime.Now; }

            // CustomSpeciesName
            try
            {
                string csn = comp.GetString("CustomSpeciesName") ?? "";
                _customSpeciesNameField.Text = csn == "^" ? "" : csn.TrimStart('^');
            }
            catch { _customSpeciesNameField.Text = ""; }

            // EggModified
            try { _eggModifiedField.Checked = comp.GetBool("EggModified"); } catch { _eggModifiedField.Checked = false; }

            // HasBeenSummoned
            try { _hasBeenSummonedField.Checked = comp.GetBool("HasBeenSummoned"); } catch { _hasBeenSummonedField.Checked = false; }

            // AllowUnmodifiedReroll
            try { _allowUnmodifiedRerollField.Checked = comp.GetBool("AllowUnmodifiedReroll"); } catch { _allowUnmodifiedRerollField.Checked = false; }

            // UA
            try { _uaField.Text = comp.GetLong("UA").ToString(); } catch { _uaField.Text = "0"; }

            // LastTrustIncreaseTime
            try
            {
                long trustInc = comp.GetLong("LastTrustIncreaseTime");
                _lastTrustIncreaseTimePicker.Value = DateTimeOffset.FromUnixTimeSeconds(trustInc).ToLocalTime().DateTime;
            }
            catch { _lastTrustIncreaseTimePicker.Value = DateTime.Now; }

            // LastTrustDecreaseTime
            try
            {
                long trustDec = comp.GetLong("LastTrustDecreaseTime");
                _lastTrustDecreaseTimePicker.Value = DateTimeOffset.FromUnixTimeSeconds(trustDec).ToLocalTime().DateTime;
            }
            catch { _lastTrustDecreaseTimePicker.Value = DateTime.Now; }

            // Descriptors (Parts)
            LoadDescriptors(comp);

            // Slot Unlocked
            LoadUnlockStatus(entry);
        }
        finally { _loading = false; }
    }

    private void LoadUnlockStatus((JsonObject Companion, string Label, string Source, int OriginalIndex, bool IsEmpty) entry)
    {
        try
        {
            if (entry.Source == "Pet" && _playerState != null)
            {
                var unlockedSlots = _playerState.GetArray("UnlockedPetSlots");
                int origIdx = entry.OriginalIndex;
                _unlockedCheck.Checked = unlockedSlots != null && origIdx < unlockedSlots.Length && unlockedSlots.GetBool(origIdx);
                _unlockedCheck.Enabled = true;
            }
            else
            {
                _unlockedCheck.Checked = false;
                _unlockedCheck.Enabled = false;
            }
        }
        catch { _unlockedCheck.Checked = false; _unlockedCheck.Enabled = false; }
    }

    private void RefreshListEntry()
    {
        int idx = _companionList.SelectedIndex;
        if (idx < 0 || idx >= _entries.Count) return;
        var entry = _entries[idx];
        var comp = entry.Companion;
        bool occupied = !entry.IsEmpty;

        bool locked = false;
        if (entry.Source == "Pet" && _playerState != null)
        {
            var unlockedSlots = _playerState.GetArray("UnlockedPetSlots");
            locked = unlockedSlots == null || entry.OriginalIndex >= unlockedSlots.Length || !unlockedSlots.GetBool(entry.OriginalIndex);
        }

        string newLabel = GetSlotLabel(comp, entry.Source, entry.OriginalIndex, occupied, locked);
        _entries[idx] = (comp, newLabel, entry.Source, entry.OriginalIndex, entry.IsEmpty);
        _companionList.Items[idx] = newLabel;
    }

    // Write-back helpers (all write directly to the underlying JsonObject)
    private void WriteType()
    {
        var comp = SelectedCompanion;
        if (comp == null || _typeField.SelectedItem == null) return;
        var entry = _typeField.SelectedItem as CompanionEntry;
        if (entry != null)
        {
            comp.Set("CreatureID", entry.Id);

            // Mark the slot as occupied so the game recognises it
            ActivateSlotIfEmpty(comp);
        }
    }

    /// <summary>
    /// When editing a previously-empty slot, flips CreatureSeed[0] to true and
    /// updates the internal entry so subsequent writes and descriptor loading work.
    /// </summary>
    private void ActivateSlotIfEmpty(JsonObject comp)
    {
        int idx = _companionList.SelectedIndex;
        if (idx < 0 || idx >= _entries.Count) return;
        var e = _entries[idx];
        if (!e.IsEmpty) return;

        // Activate the seed flag
        var seedArr = comp.GetArray("CreatureSeed");
        if (seedArr != null && seedArr.Length >= 2)
            seedArr.Set(0, true);

        // Mark entry as no longer empty
        _entries[idx] = (e.Companion, e.Label, e.Source, e.OriginalIndex, false);
        _deleteBtn.Enabled = true;
        RefreshListEntry();
    }

    private void WriteName()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        comp.Set("CustomName", _nameField.Text);
        RefreshListEntry();
    }

    private void WriteCreatureSeed()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        var normalized = SeedHelper.NormalizeSeed(_creatureSeedField.Text);
        if (normalized == null) return;
        var arr = comp.GetArray("CreatureSeed");
        if (arr != null && arr.Length >= 2)
            arr.Set(1, normalized);
    }

    private void WriteSecondarySeed()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        var arr = comp.GetArray("CreatureSecondarySeed");
        if (arr != null && arr.Length >= 2)
        {
            var normalized = SeedHelper.NormalizeSeed(_secondarySeedField.Text);
            bool hasValue = normalized != null;
            arr.Set(0, hasValue);
            arr.Set(1, hasValue ? normalized! : "0x0");
        }
    }

    private void WriteSpeciesSeed()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        var normalized = SeedHelper.NormalizeSeed(_speciesSeedField.Text);
        if (normalized != null)
            comp.Set("SpeciesSeed", normalized);
    }

    private void WriteGenusSeed()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        var normalized = SeedHelper.NormalizeSeed(_genusSeedField.Text);
        if (normalized != null)
            comp.Set("GenusSeed", normalized);
    }

    private void WritePredator()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        comp.Set("Predator", _predatorField.Checked);
    }

    private void WriteBiome()
    {
        var comp = SelectedCompanion;
        if (comp == null || _biomeField.SelectedItem == null) return;
        var biomeObj = comp.GetObject("Biome");
        biomeObj?.Set("Biome", (string)_biomeField.SelectedItem);
    }

    private void WriteCreatureType()
    {
        var comp = SelectedCompanion;
        if (comp == null || _creatureTypeField.SelectedItem == null) return;
        var ctObj = comp.GetObject("CreatureType");
        ctObj?.Set("CreatureType", (string)_creatureTypeField.SelectedItem);
    }

    private void WriteScale()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        if (double.TryParse(_scaleField.Text, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double val))
            comp.Set("Scale", val);
    }

    private void WriteTrust()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        if (double.TryParse(_trustField.Text, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double val))
            comp.Set("Trust", val);
    }

    private void WriteBoneScaleSeed()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        var arr = comp.GetArray("BoneScaleSeed");
        if (arr != null && arr.Length >= 2)
        {
            var normalized = SeedHelper.NormalizeSeed(_boneScaleSeedField.Text);
            bool hasValue = normalized != null && normalized != "0x0";
            arr.Set(0, hasValue);
            arr.Set(1, hasValue ? normalized! : "0x0");
        }
    }

    private void WriteColourBaseSeed()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        var arr = comp.GetArray("ColourBaseSeed");
        if (arr != null && arr.Length >= 2)
        {
            var normalized = SeedHelper.NormalizeSeed(_colourBaseSeedField.Text);
            bool hasValue = normalized != null && normalized != "0x0";
            arr.Set(0, hasValue);
            arr.Set(1, hasValue ? normalized! : "0x0");
        }
    }

    private void WriteHasFur()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        comp.Set("HasFur", _hasFurField.Checked);
    }

    private void WriteTrait(int index, TextBox field)
    {
        if (_loading) return;
        var comp = SelectedCompanion;
        if (comp == null) return;
        var traits = comp.GetArray("Traits");
        if (traits != null && index < traits.Length && double.TryParse(field.Text,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double val))
            traits.Set(index, val);
    }

    private void WriteMood(int index, TextBox field)
    {
        if (_loading) return;
        var comp = SelectedCompanion;
        if (comp == null) return;
        var moods = comp.GetArray("Moods");
        if (moods != null && index < moods.Length && double.TryParse(field.Text,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double val))
            moods.Set(index, val);
    }

    private void WriteCustomSpeciesName()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        string text = _customSpeciesNameField.Text;
        // Save format uses ^ prefix for species names
        comp.Set("CustomSpeciesName", string.IsNullOrEmpty(text) ? "^" : $"^{text.TrimStart('^')}");
    }

    private void WriteEggModified()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        comp.Set("EggModified", _eggModifiedField.Checked);
    }

    private void WriteHasBeenSummoned()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        comp.Set("HasBeenSummoned", _hasBeenSummonedField.Checked);
    }

    private void WriteAllowUnmodifiedReroll()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        comp.Set("AllowUnmodifiedReroll", _allowUnmodifiedRerollField.Checked);
    }

    private void WriteUA()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        if (long.TryParse(_uaField.Text, out long val))
            comp.Set("UA", val);
    }

    /// <summary>
    /// Reloads descriptor dropdowns when the creature type changes.
    /// Clears old descriptors (they belong to the old creature type) and shows the
    /// new creature's part groups from the Creature Builder database.
    /// </summary>
    private void ReloadDescriptorsForCurrentCompanion()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;

        // Clear existing descriptors since they belong to the old creature type
        var descArr = comp.GetArray("Descriptors");
        if (descArr != null)
        {
            for (int i = descArr.Length - 1; i >= 0; i--)
                descArr.RemoveAt(i);
            // Add a fresh descriptor ID for the new creature
            descArr.Add($"^{CreaturePartDatabase.NewDescriptorId()}");
        }

        _loading = true;
        try { LoadDescriptors(comp); }
        finally { _loading = false; }
    }

    /// <summary>
    /// Loads descriptor part dropdowns for the selected companion based on its CreatureID.
    /// Each creature type has a tree of part groups from the Creature Builder database.
    /// </summary>
    private void LoadDescriptors(JsonObject comp)
    {
        _descriptorPanel.SuspendLayout();
        _descriptorPanel.Controls.Clear();

        string creatureId = comp.GetString("CreatureID") ?? "";
        var partEntry = CreaturePartDatabase.GetForCreatureId(creatureId);

        // Read current descriptors from save (stored as ["^DESC1", "^DESC2", ..., "^0123456789"])
        var currentDescriptors = new List<string>();
        try
        {
            var descArr = comp.GetArray("Descriptors");
            if (descArr != null)
            {
                for (int i = 0; i < descArr.Length; i++)
                {
                    string d = descArr.GetString(i) ?? "";
                    currentDescriptors.Add(d.TrimStart('^'));
                }
            }
        }
        catch { }

        if (partEntry == null || partEntry.Details.Count == 0)
        {
            // No part data available - show raw descriptor list for manual editing
            var rawLabel = new Label
            {
                Text = currentDescriptors.Count > 0
                    ? UiStrings.Format("companion.raw_descriptors", currentDescriptors.Count) + string.Join(", ", currentDescriptors)
                    : UiStrings.Get("companion.no_part_data"),
                AutoSize = true,
                Padding = new Padding(0, 2, 0, 2),
            };
            _descriptorPanel.Controls.Add(rawLabel);
            _descriptorPanel.ResumeLayout(true);
            return;
        }

        // Get flattened groups based on current selections
        var flatGroups = CreaturePartDatabase.GetFlatGroups(partEntry, currentDescriptors);

        foreach (var group in flatGroups)
        {
            var row = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 2, 0, 2),
            };

            var lbl = new Label
            {
                Text = group.GroupId.Trim('_') + ":",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Padding = new Padding(0, 5, 5, 0),
                Width = 120,
            };

            var combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200,
                Tag = group.GroupId,
            };

            // Add a "(none)" option
            combo.Items.Add(UiStrings.Get("companion.none"));
            int selectedIdx = 0;

            for (int i = 0; i < group.Descriptors.Count; i++)
            {
                var desc = group.Descriptors[i];
                combo.Items.Add(desc);
                if (currentDescriptors.Contains(desc.Id, StringComparer.OrdinalIgnoreCase))
                    selectedIdx = i + 1; // +1 for (none)
            }

            combo.SelectedIndex = selectedIdx;
            combo.SelectedIndexChanged += OnDescriptorChanged;

            row.Controls.Add(lbl);
            row.Controls.Add(combo);
            _descriptorPanel.Controls.Add(row);
        }

        // Add a "Regen Descriptor ID" button
        _regenDescriptorBtn = new Button { Text = UiStrings.Get("companion.regen_descriptor_id"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _regenDescriptorBtn.Click += (s, e) =>
        {
            if (_loading) return;
            WriteDescriptors();
        };
        _descriptorPanel.Controls.Add(_regenDescriptorBtn);

        _descriptorPanel.ResumeLayout(true);
    }

    /// <summary>
    /// Handles descriptor dropdown changes - writes descriptors and refreshes child groups.
    /// </summary>
    private void OnDescriptorChanged(object? sender, EventArgs e)
    {
        if (_loading) return;
        WriteDescriptors();
        // Reload descriptors to show/hide child groups based on new selection
        var comp = SelectedCompanion;
        if (comp != null)
        {
            _loading = true;
            try { LoadDescriptors(comp); }
            finally { _loading = false; }
        }
    }

    /// <summary>
    /// Collects current descriptor selections from the UI and writes them back to the companion JSON.
    /// Always appends a descriptor ID as the last element.
    /// </summary>
    private void WriteDescriptors()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;

        var descriptors = new List<string>();
        foreach (Control ctrl in _descriptorPanel.Controls)
        {
            if (ctrl is FlowLayoutPanel row)
            {
                foreach (Control rowChild in row.Controls)
                {
                    if (rowChild is ComboBox combo && combo.SelectedItem is DescriptorOption opt)
                    {
                        descriptors.Add(opt.Id);
                    }
                }
            }
        }

        // Always append a descriptor ID (10-digit random number)
        descriptors.Add(CreaturePartDatabase.NewDescriptorId());

        // Write to the Descriptors array on the companion
        var descArr = comp.GetArray("Descriptors");
        if (descArr != null)
        {
            // Clear existing
            for (int i = descArr.Length - 1; i >= 0; i--)
                descArr.RemoveAt(i);
            // Add new values (with ^ prefix)
            foreach (var d in descriptors)
                descArr.Add($"^{d}");
        }
    }

    private void OnResetAccessory(object? sender, EventArgs e)
    {
        var comp = SelectedCompanion;
        if (comp == null) return;

        var result = MessageBox.Show(this, UiStrings.Get("companion.reset_accessory_confirm"),
            UiStrings.Get("companion.reset_accessory_title"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;

        CompanionLogic.ResetAccessoryCustomisation(comp);
    }

    private void OnDelete(object? sender, EventArgs e)
    {
        var comp = SelectedCompanion;
        if (comp == null) return;

        var result = MessageBox.Show(this, UiStrings.Get("companion.delete_confirm"),
            UiStrings.Get("companion.delete_title"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;

        CompanionLogic.DeleteCompanion(comp);

        int idx = _companionList.SelectedIndex;
        var entry = _entries[idx];

        // Lock the slot when deleting a Pet companion
        if (entry.Source == "Pet" && _playerState != null)
            CompanionLogic.SetSlotUnlocked(_playerState, entry.OriginalIndex, false);

        // Mark entry as empty and refresh
        _entries[idx] = (entry.Companion, entry.Label, entry.Source, entry.OriginalIndex, true);

        // Refresh the whole list to update labels
        var saveData = FindSaveDataRoot();
        if (saveData != null) LoadData(saveData);
        else
        {
            RefreshListEntry();
            OnCompanionSelected(null, EventArgs.Empty);
        }
    }

    private void OnExport(object? sender, EventArgs e)
    {
        var comp = SelectedCompanion;
        if (comp == null) return;

        var config = ExportConfig.Instance;
        var typeEntry = _typeField.SelectedItem as CompanionEntry;
        var vars = new Dictionary<string, string>
        {
            ["name"] = _nameField.Text ?? "",
            ["species"] = typeEntry?.Species ?? typeEntry?.Id ?? "",
            ["creature_seed"] = _creatureSeedField.Text ?? ""
        };

        using var dialog = new SaveFileDialog
        {
            Filter = ExportConfig.BuildDialogFilter(config.CompanionExt, "Companion files"),
            DefaultExt = config.CompanionExt.TrimStart('.'),
            FileName = ExportConfig.BuildFileName(config.CompanionTemplate, config.CompanionExt, vars)
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try { CompanionLogic.ExportCompanion(comp, dialog.FileName); }
            catch (Exception ex)
            {
                MessageBox.Show(UiStrings.Format("companion.export_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void OnImport(object? sender, EventArgs e)
    {
        if (_playerState == null) return;

        var config = ExportConfig.Instance;
        using var dialog = new OpenFileDialog
        {
            Filter = ExportConfig.BuildImportFilter(config.CompanionExt, "Companion files", ".pet", ".cmp")
        };

        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            // Try Pets first, then Eggs
            var pets = _playerState.GetArray("Pets");
            var eggs = _playerState.GetArray("Eggs");
            JsonArray? target = null;
            if (pets != null) target = pets;
            else if (eggs != null) target = eggs;

            if (target == null)
            {
                MessageBox.Show(UiStrings.Get("companion.no_arrays_found"), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int importedIdx;
            bool importedToPets;
            try
            {
                importedIdx = CompanionLogic.ImportCompanion(target, dialog.FileName);
                importedToPets = (target == pets);
            }
            catch (InvalidOperationException)
            {
                // First array full, try the other
                JsonArray? fallback = target == pets ? eggs : pets;
                if (fallback != null)
                {
                    importedIdx = CompanionLogic.ImportCompanion(fallback, dialog.FileName);
                    importedToPets = (fallback == pets);
                }
                else
                    throw new InvalidOperationException("No empty companion slot available in Pets or Eggs.");
            }

            // Unlock the slot when importing to Pets
            if (importedToPets)
                CompanionLogic.SetSlotUnlocked(_playerState, importedIdx, true);

            // Refresh list
            var saveData = FindSaveDataRoot();
            if (saveData != null) LoadData(saveData);
        }
        catch (Exception ex)
        {
            MessageBox.Show(UiStrings.Format("companion.import_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private JsonObject? FindSaveDataRoot()
    {
        if (_playerState?.Parent is JsonObject root) return root;
        return null;
    }

    public void SaveData(JsonObject saveData)
    {
        // No-op: edits are applied directly to the underlying JsonObjects.
    }

    public void ApplyUiLocalisation()
    {
        _titleLabel.Text = UiStrings.Get("companion.title");
        _creatureBuilderBtn.Text = UiStrings.Get("companion.creature_builder");
        _deleteBtn.Text = UiStrings.Get("common.delete");

        // Left column labels
        _slotUnlockedLabel.Text = UiStrings.GetOrNull("companion.slot_unlocked") ?? "Slot Unlocked:";
        _speciesLabel.Text = UiStrings.GetOrNull("companion.species") ?? "Species:";
        _nameLabel.Text = UiStrings.GetOrNull("companion.name") ?? "Name:";
        _nameField.PlaceholderText = UiStrings.Get("common.procedural_no_name");
        _typeLabel.Text = UiStrings.GetOrNull("companion.type") ?? "Type:";
        _biomeLabel.Text = UiStrings.GetOrNull("companion.biome") ?? "Biome:";
        _predatorLabel.Text = UiStrings.GetOrNull("companion.predator") ?? "Predator:";
        _hasFurLabel.Text = UiStrings.GetOrNull("companion.has_fur") ?? "Has Fur:";
        _scaleLabel.Text = UiStrings.GetOrNull("companion.scale") ?? "Scale:";
        _trustLabel.Text = UiStrings.GetOrNull("companion.trust") ?? "Trust:";
        _birthTimeLabel.Text = UiStrings.GetOrNull("companion.birth_time") ?? "Birth Time:";
        _lastEggTimeLabel.Text = UiStrings.GetOrNull("companion.last_egg_time") ?? "Last Egg Time:";
        _customSpeciesNameLabel.Text = UiStrings.GetOrNull("companion.custom_species_name") ?? "Custom Species Name:";
        _eggModifiedLabel.Text = UiStrings.GetOrNull("companion.egg_modified") ?? "Egg Modified:";
        _summonedLabel.Text = UiStrings.GetOrNull("companion.summoned") ?? "Summoned:";
        _allowRerollLabel.Text = UiStrings.GetOrNull("companion.allow_reroll") ?? "Allow Reroll:";
        _uaLabel.Text = UiStrings.GetOrNull("companion.ua") ?? "UA:";

        // Right column seed labels
        _creatureSeedLabel.Text = UiStrings.GetOrNull("companion.creature_seed") ?? "Creature Seed:";
        _secondarySeedLabel.Text = UiStrings.GetOrNull("companion.secondary_seed") ?? "Secondary Seed:";
        _speciesSeedLabel.Text = UiStrings.GetOrNull("companion.species_seed") ?? "Species Seed:";
        _genusSeedLabel.Text = UiStrings.GetOrNull("companion.genus_seed") ?? "Genus Seed:";
        _boneScaleSeedLabel.Text = UiStrings.GetOrNull("companion.bone_scale_seed") ?? "Bone Scale Seed:";
        _colourBaseSeedLabel.Text = UiStrings.GetOrNull("companion.colour_base_seed") ?? "Colour Base Seed:";

        // Right column trait & mood labels
        _helpfulnessLabel.Text = UiStrings.GetOrNull("companion.helpfulness") ?? "Helpfulness:";
        _aggressionLabel.Text = UiStrings.GetOrNull("companion.aggression") ?? "Aggression:";
        _independenceLabel.Text = UiStrings.GetOrNull("companion.independence") ?? "Independence:";
        _hungryLabel.Text = UiStrings.GetOrNull("companion.hungry") ?? "Hungry:";
        _lonelyLabel.Text = UiStrings.GetOrNull("companion.lonely") ?? "Lonely:";
        _trustIncreaseLabel.Text = UiStrings.GetOrNull("companion.trust_increase") ?? "Trust Increase:";
        _trustDecreaseLabel.Text = UiStrings.GetOrNull("companion.trust_decrease") ?? "Trust Decrease:";

        // Descriptors heading
        _descriptorsHeading.Text = UiStrings.GetOrNull("companion.descriptors") ?? "Descriptors (Parts)";

        // Button labels
        _exportCompanionBtn.Text = UiStrings.Get("common.export");
        _importCompanionBtn.Text = UiStrings.Get("common.import");
        _resetAccessoryBtn.Text = UiStrings.GetOrNull("companion.reset_accessory") ?? "Reset Accessory";

        // Seed "Gen" buttons
        foreach (var btn in _seedGenButtons)
            btn.Text = UiStrings.Get("companion.gen");

        // Regen Descriptor ID button
        if (_regenDescriptorBtn != null)
            _regenDescriptorBtn.Text = UiStrings.Get("companion.regen_descriptor_id");
    }
}
