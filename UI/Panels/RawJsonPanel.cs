using NMSE.Models;
using NMSE.Core;
using NMSE.Data;

namespace NMSE.UI.Panels;

public partial class RawJsonPanel : UserControl
{
    private bool _cancelExpand;

    private JsonObject? _saveData;
    private JsonObject? _accountData;
    private string? _saveFilePath;
    private string? _accountFilePath;
    private bool _isTreeView = true;
    private bool _treeModified;
    private bool _isShowingAccount;

    public RawJsonPanel()
    {
        InitializeComponent();
        SetupLayout();
    }

    #region Public API

    public void LoadData(JsonObject saveData)
    {
        _saveData = saveData;
        _isShowingAccount = false;
        _treeModified = false;
        UpdateFileSelector();
        if (_isTreeView)
            BuildTree(saveData);
        else
            _jsonTextBox.Text = RawJsonLogic.ToDisplayString(saveData);
        _statusLabel.Text = UiStrings.Format("raw_json.loaded_keys", saveData.Size().ToString("N0"));
        _statusLabel.ForeColor = Color.Gray;
    }

    /// <summary>
    /// Sets the account data that can be edited via the file selector.
    /// </summary>
    public void SetAccountData(JsonObject? accountData, string? accountFilePath)
    {
        _accountData = accountData;
        _accountFilePath = accountFilePath;
        UpdateFileSelector();
    }

    /// <summary>
    /// Sets the save file path for display in the file selector.
    /// </summary>
    public void SetSaveFilePath(string? filePath)
    {
        _saveFilePath = filePath;
        UpdateFileSelector();
    }

    private void UpdateFileSelector()
    {
        _fileSelector.SelectedIndexChanged -= OnFileSelectorChanged;
        _fileSelector.Items.Clear();
        string saveName = !string.IsNullOrEmpty(_saveFilePath) ? Path.GetFileName(_saveFilePath) : "Save File";
        _fileSelector.Items.Add(saveName);
        if (_accountData != null)
        {
            string accountName = !string.IsNullOrEmpty(_accountFilePath) ? Path.GetFileName(_accountFilePath) : "accountdata.hg";
            _fileSelector.Items.Add(accountName);
        }
        _fileSelector.SelectedIndex = _isShowingAccount && _accountData != null ? 1 : 0;
        _fileSelector.SelectedIndexChanged += OnFileSelectorChanged;
    }

    private void OnFileSelectorChanged(object? sender, EventArgs e)
    {
        if (_fileSelector.SelectedIndex == 1 && _accountData != null)
        {
            // Switch to account data
            _isShowingAccount = true;
            _treeModified = false;
            if (_isTreeView)
                BuildTree(_accountData);
            else
                _jsonTextBox.Text = RawJsonLogic.ToDisplayString(_accountData);
            _statusLabel.Text = UiStrings.Format("raw_json.edited_account", _accountData.Size().ToString("N0"));
            _statusLabel.ForeColor = Color.DarkBlue;
        }
        else if (_saveData != null)
        {
            // Switch back to save data
            _isShowingAccount = false;
            _treeModified = false;
            if (_isTreeView)
                BuildTree(_saveData);
            else
                _jsonTextBox.Text = RawJsonLogic.ToDisplayString(_saveData);
            _statusLabel.Text = UiStrings.Format("raw_json.loaded_keys", _saveData.Size().ToString("N0"));
            _statusLabel.ForeColor = Color.Gray;
        }
    }

    public void SaveData(JsonObject saveData)
    {
        // Tree edits are applied directly to the JsonObject in real-time via Set/Add/Remove.
        // SaveData only needs to clear the modified flag.
        if (_treeModified && _isTreeView)
            _treeModified = false;
    }

    /// <summary>
    /// Rebuilds the tree/text view from the current in-memory JSON data.
    /// Call this after syncing panel data to the JsonObject so the Raw JSON
    /// editor reflects the latest state of all editable fields.
    /// </summary>
    public void RefreshTree()
    {
        var data = _isShowingAccount ? _accountData : _saveData;
        if (data == null) return;

        if (_isTreeView)
            BuildTree(data);
        else
            _jsonTextBox.Text = RawJsonLogic.ToDisplayString(data);
    }

    public JsonObject? GetEditedData()
    {
        if (_isTreeView)
        {
            if (_saveData == null) return null;
            _treeModified = false;
            return _saveData;
        }
        try
        {
            return RawJsonLogic.ParseJson(_jsonTextBox.Text);
        }
        catch (JsonException ex)
        {
            MessageBox.Show(UiStrings.Format("raw_json.invalid_json", ex.Message), UiStrings.Get("raw_json.validation_error"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }
    }

    #endregion

    #region View Switching

    private void ShowTreeView()
    {
        if (_isTreeView) return;
        _isTreeView = true;
        _treeViewButton.Enabled = false;
        _textViewButton.Enabled = true;
        _expandAllButton.Visible = true;
        _collapseAllButton.Visible = true;
        _formatButton.Visible = false;
        _validateButton.Visible = false;
        _searchBox.Visible = true;
        _searchButton.Visible = true;
        _clearSearchButton.Visible = true;
        _textPanel.Visible = false;
        _treePanel.Visible = true;

        if (_jsonTextBox.Modified && _jsonTextBox.Text.Length > 0)
        {
            try
            {
                var parsed = RawJsonLogic.ParseJson(_jsonTextBox.Text);
                if (_isShowingAccount)
                    _accountData = parsed;
                else
                    _saveData = parsed;
                BuildTree(parsed);
                _statusLabel.Text = UiStrings.Get("raw_json.tree_rebuilt");
                _statusLabel.ForeColor = Color.Green;
            }
            catch (JsonException ex)
            {
                _statusLabel.Text = UiStrings.Format("raw_json.parse_error", ex.Message);
                _statusLabel.ForeColor = Color.Red;
            }
        }
    }

    private void ShowTextView()
    {
        if (!_isTreeView) return;
        _isTreeView = false;
        _textViewButton.Enabled = false;
        _treeViewButton.Enabled = true;
        _expandAllButton.Visible = false;
        _collapseAllButton.Visible = false;
        _formatButton.Visible = true;
        _validateButton.Visible = true;
        _searchBox.Visible = false;
        _searchButton.Visible = false;
        _clearSearchButton.Visible = false;
        _treePanel.Visible = false;
        _textPanel.Visible = true;

        var data = _isShowingAccount ? _accountData : _saveData;
        if (data != null)
            _jsonTextBox.Text = RawJsonLogic.ToDisplayString(data);
    }

    #endregion

    #region Tree Building

    private void BuildTree(JsonObject root)
    {
        _treeView.BeginUpdate();
        _treeView.Nodes.Clear();

        var rootNode = new TreeNode("Root") { Tag = new NodeTag(root, null, null) };
        PopulateObjectNode(rootNode, root, maxDepth: 2, currentDepth: 0);
        _treeView.Nodes.Add(rootNode);
        rootNode.Expand();

        _treeView.EndUpdate();
    }

    /// <summary>
    /// Expand all tree nodes asynchronously in batches to prevent UI freeze and reduce peak memory.
    /// Uses Task.Delay to yield to the UI thread between batches, keeping the application responsive.
    /// </summary>
    private async Task ExpandAllBatchedAsync()
    {
        var result = MessageBox.Show(
            UiStrings.Get("raw_json.expand_confirm"),
            UiStrings.Get("raw_json.expand_title"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;

        _cancelExpand = false;
        _expandAllButton.Enabled = false;
        _collapseAllButton.Enabled = false;
        _stopExpandBtn.Visible = true;
        _statusLabel.Text = UiStrings.Get("raw_json.expanding");
        _statusLabel.ForeColor = Color.Blue;

        int count = 0;
        var stack = new Stack<TreeNode>();
        if (_treeView.Nodes.Count > 0)
            stack.Push(_treeView.Nodes[0]);

        _treeView.BeginUpdate();

        while (stack.Count > 0)
        {
            if (_cancelExpand) break;

            var node = stack.Pop();

            // Force lazy loading by expanding (triggers OnBeforeExpand)
            if (!node.IsExpanded)
                node.Expand();

            // Queue children for expansion
            for (int i = node.Nodes.Count - 1; i >= 0; i--)
                stack.Push(node.Nodes[i]);

            count++;
            if (count % 500 == 0)
            {
                _treeView.EndUpdate();
                _statusLabel.Text = UiStrings.Format("raw_json.expanding_count", count.ToString("N0"));
                await Task.Delay(1); // Yield to UI thread
                _treeView.BeginUpdate();
            }
        }

        if (_treeView.Nodes.Count > 0) _treeView.Nodes[0].EnsureVisible();
        _treeView.EndUpdate();
        _expandAllButton.Enabled = true;
        _collapseAllButton.Enabled = true;
        _stopExpandBtn.Visible = false;
        _statusLabel.Text = _cancelExpand ? UiStrings.Format("raw_json.stopped_at", count.ToString("N0")) : UiStrings.Format("raw_json.expanded_nodes", count.ToString("N0"));
        _statusLabel.ForeColor = Color.Gray;
    }

    private void PopulateObjectNode(TreeNode parentNode, JsonObject obj, int maxDepth, int currentDepth)
    {
        var names = obj.Names();
        for (int i = 0; i < names.Count; i++)
        {
            string key = names[i];
            object? val = obj.Get(key);
            parentNode.Nodes.Add(CreateValueNode(key, val, obj, maxDepth, currentDepth));
        }
    }

    private void PopulateArrayNode(TreeNode parentNode, JsonArray arr, int maxDepth, int currentDepth)
    {
        for (int i = 0; i < arr.Length; i++)
        {
            object? val = arr.Get(i);
            parentNode.Nodes.Add(CreateValueNode($"[{i}]", val, arr, maxDepth, currentDepth));
        }
    }

    private TreeNode CreateValueNode(string key, object? value, object parent, int maxDepth, int currentDepth)
    {
        if (value is JsonObject childObj)
        {
            int count = childObj.Size();
            var node = new TreeNode($"{key}  {{...}}  ({count} properties)")
            {
                Tag = new NodeTag(childObj, parent, key),
                ForeColor = Color.DarkBlue
            };
            if (currentDepth < maxDepth)
                PopulateObjectNode(node, childObj, maxDepth, currentDepth + 1);
            else if (count > 0)
                node.Nodes.Add(new TreeNode("Loading...") { Tag = LazyTag.Instance });
            return node;
        }
        if (value is JsonArray childArr)
        {
            int count = childArr.Length;
            var node = new TreeNode($"{key}  [...]  ({count} items)")
            {
                Tag = new NodeTag(childArr, parent, key),
                ForeColor = Color.DarkGreen
            };
            if (currentDepth < maxDepth)
                PopulateArrayNode(node, childArr, maxDepth, currentDepth + 1);
            else if (count > 0)
                node.Nodes.Add(new TreeNode("Loading...") { Tag = LazyTag.Instance });
            return node;
        }

        string displayVal = FormatValue(value);
        return new TreeNode($"{key} : {displayVal}")
        {
            Tag = new NodeTag(value, parent, key),
            ForeColor = GetValueColor(value)
        };
    }

    private static string FormatValue(object? value) => value switch
    {
        null => "null",
        string s => $"\"{EscapeString(s)}\"",
        bool b => b ? "true" : "false",
        BinaryData bd => $"<binary:{bd.ToHexString()}>",
        _ => value.ToString() ?? "null"
    };

    private static string EscapeString(string s)
    {
        if (s.Length > 200) return s[..200] + "...";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }

    private static Color GetValueColor(object? value) => value switch
    {
        string => Color.DarkRed,
        bool => Color.DarkMagenta,
        null => Color.Gray,
        _ => Color.DarkOrange
    };

    #endregion

    #region Lazy Loading

    private void OnBeforeExpand(object? sender, TreeViewCancelEventArgs e)
    {
        if (e.Node == null) return;
        if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Tag is LazyTag)
        {
            e.Node.Nodes.Clear();
            var tag = e.Node.Tag as NodeTag;
            if (tag?.Value is JsonObject obj)
                PopulateObjectNode(e.Node, obj, maxDepth: 2, currentDepth: 0);
            else if (tag?.Value is JsonArray arr)
                PopulateArrayNode(e.Node, arr, maxDepth: 2, currentDepth: 0);
        }
    }

    #endregion

    #region Editing

    private void OnNodeDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Node?.Tag is NodeTag tag && tag.Value is not JsonObject && tag.Value is not JsonArray)
            BeginEditSelectedNode();
    }

    private void BeginEditSelectedNode()
    {
        var node = _treeView.SelectedNode;
        if (node?.Tag is not NodeTag tag) return;
        if (tag.Value is JsonObject || tag.Value is JsonArray) return;

        string currentVal = FormatValue(tag.Value);
        string key = tag.Key ?? "";

        using var dialog = new Form
        {
            Text = UiStrings.Format("raw_json.edit_title", key),
            Size = new Size(450, 160),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false
        };

        var label = new Label { Text = UiStrings.Get("raw_json.label_value"), Location = new Point(10, 15), AutoSize = true };
        var textBox = new TextBox { Text = currentVal, Location = new Point(60, 12), Width = 360 };
        var okBtn = new Button { Text = UiStrings.Get("common.ok"), DialogResult = DialogResult.OK, Location = new Point(260, 80), Width = 75 };
        var cancelBtn = new Button { Text = UiStrings.Get("common.cancel"), DialogResult = DialogResult.Cancel, Location = new Point(345, 80), Width = 75 };
        dialog.AcceptButton = okBtn;
        dialog.CancelButton = cancelBtn;
        dialog.Controls.AddRange([label, textBox, okBtn, cancelBtn]);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            string newVal = textBox.Text.Trim();
            object? parsed = ParseInputValue(newVal);
            tag.Value = parsed;

            if (tag.Parent is JsonObject parentObj && tag.Key != null && !tag.Key.StartsWith('['))
                parentObj.Set(tag.Key, parsed);
            else if (tag.Parent is JsonArray parentArr && tag.Key != null && tag.Key.StartsWith('['))
            {
                int idx = int.Parse(tag.Key.Trim('[', ']'));
                parentArr.Set(idx, parsed);
            }

            node.Text = $"{key} : {FormatValue(parsed)}";
            node.ForeColor = GetValueColor(parsed);
            _treeModified = true;
            _statusLabel.Text = UiStrings.Get("raw_json.value_modified");
            _statusLabel.ForeColor = Color.DarkOrange;
        }
    }

    private void OnAfterLabelEdit(object? sender, NodeLabelEditEventArgs e) => e.CancelEdit = true;

    private static object? ParseInputValue(string input)
    {
        if (input == "null") return null;
        if (input == "true") return true;
        if (input == "false") return false;
        if (input.StartsWith('"') && input.EndsWith('"'))
            return input[1..^1].Replace("\\\"", "\"").Replace("\\\\", "\\")
                               .Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t");
        if (long.TryParse(input, out long l))
            return l >= int.MinValue && l <= int.MaxValue ? (int)l : l;
        if (double.TryParse(input, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double d))
            return d;
        return input;
    }

    #endregion

    #region Add / Delete

    private void AddProperty()
    {
        var node = _treeView.SelectedNode;
        if (node?.Tag is not NodeTag tag || tag.Value is not JsonObject obj) return;

        using var dialog = new Form
        {
            Text = UiStrings.Get("raw_json.add_property_title"),
            Size = new Size(400, 200),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false
        };
        var keyLabel = new Label { Text = UiStrings.Get("raw_json.label_key"), Location = new Point(10, 15), AutoSize = true };
        var keyBox = new TextBox { Location = new Point(60, 12), Width = 310 };
        var valLabel = new Label { Text = UiStrings.Get("raw_json.label_value"), Location = new Point(10, 50), AutoSize = true };
        var valBox = new TextBox { Location = new Point(60, 47), Width = 310 };
        var okBtn = new Button { Text = UiStrings.Get("common.ok"), DialogResult = DialogResult.OK, Location = new Point(210, 120), Width = 75 };
        var cancelBtn = new Button { Text = UiStrings.Get("common.cancel"), DialogResult = DialogResult.Cancel, Location = new Point(295, 120), Width = 75 };
        dialog.AcceptButton = okBtn;
        dialog.CancelButton = cancelBtn;
        dialog.Controls.AddRange([keyLabel, keyBox, valLabel, valBox, okBtn, cancelBtn]);

        if (dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(keyBox.Text))
        {
            string newKey = keyBox.Text.Trim();
            if (obj.Contains(newKey))
            {
                MessageBox.Show(UiStrings.Format("raw_json.duplicate_key", newKey), UiStrings.Get("raw_json.duplicate_key_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            object? newVal = ParseInputValue(valBox.Text.Trim());
            obj.Add(newKey, newVal);
            node.Nodes.Add(CreateValueNode(newKey, newVal, obj, 0, 0));
            UpdateContainerNodeText(node);
            _treeModified = true;
            _statusLabel.Text = UiStrings.Format("raw_json.added_property", newKey);
            _statusLabel.ForeColor = Color.Green;
        }
    }

    private void AddArrayItem()
    {
        var node = _treeView.SelectedNode;
        if (node?.Tag is not NodeTag tag || tag.Value is not JsonArray arr) return;

        using var dialog = new Form
        {
            Text = UiStrings.Get("raw_json.add_array_item_title"),
            Size = new Size(400, 160),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false
        };
        var valLabel = new Label { Text = UiStrings.Get("raw_json.label_value"), Location = new Point(10, 15), AutoSize = true };
        var valBox = new TextBox { Location = new Point(60, 12), Width = 310 };
        var okBtn = new Button { Text = UiStrings.Get("common.ok"), DialogResult = DialogResult.OK, Location = new Point(210, 80), Width = 75 };
        var cancelBtn = new Button { Text = UiStrings.Get("common.cancel"), DialogResult = DialogResult.Cancel, Location = new Point(295, 80), Width = 75 };
        dialog.AcceptButton = okBtn;
        dialog.CancelButton = cancelBtn;
        dialog.Controls.AddRange([valLabel, valBox, okBtn, cancelBtn]);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            object? newVal = ParseInputValue(valBox.Text.Trim());
            arr.Add(newVal);
            int idx = arr.Length - 1;
            node.Nodes.Add(CreateValueNode($"[{idx}]", newVal, arr, 0, 0));
            UpdateContainerNodeText(node);
            _treeModified = true;
            _statusLabel.Text = UiStrings.Format("raw_json.added_array_item", idx);
            _statusLabel.ForeColor = Color.Green;
        }
    }

    private void DeleteSelectedNode()
    {
        var node = _treeView.SelectedNode;
        if (node?.Tag is not NodeTag tag || tag.Parent == null || tag.Key == null) return;

        if (MessageBox.Show(UiStrings.Format("raw_json.confirm_delete", tag.Key), UiStrings.Get("raw_json.confirm_delete_title"),
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        if (tag.Parent is JsonObject parentObj && !tag.Key.StartsWith('['))
        {
            parentObj.Remove(tag.Key);
            var parent2 = node.Parent;
            node.Remove();
            if (parent2 != null) UpdateContainerNodeText(parent2);
        }
        else if (tag.Parent is JsonArray parentArr && tag.Key.StartsWith('['))
        {
            int idx = int.Parse(tag.Key.Trim('[', ']'));
            parentArr.RemoveAt(idx);
            var parentNode = node.Parent;
            node.Remove();
            // Re-index remaining sibling nodes after removal
            if (parentNode != null)
            {
                for (int i = idx; i < parentNode.Nodes.Count; i++)
                {
                    if (parentNode.Nodes[i].Tag is NodeTag sibTag)
                    {
                        sibTag.Key = $"[{i}]";
                        string text = parentNode.Nodes[i].Text;
                        int bracketEnd = text.IndexOf(']');
                        if (bracketEnd >= 0)
                            parentNode.Nodes[i].Text = $"[{i}" + text[bracketEnd..];
                    }
                }
                UpdateContainerNodeText(parentNode);
            }
        }
        else
        {
            var parent2 = node.Parent;
            node.Remove();
            if (parent2 != null) UpdateContainerNodeText(parent2);
        }
        _treeModified = true;
        _statusLabel.Text = UiStrings.Format("raw_json.deleted", tag.Key);
        _statusLabel.ForeColor = Color.DarkOrange;
    }

    private static void UpdateContainerNodeText(TreeNode node)
    {
        if (node.Tag is not NodeTag tag) return;
        string key = tag.Key ?? "Root";
        if (tag.Value is JsonObject obj)
            node.Text = $"{key}  {{...}}  ({obj.Size()} properties)";
        else if (tag.Value is JsonArray arr)
            node.Text = $"{key}  [...]  ({arr.Length} items)";
    }

    #endregion

    #region Copy Operations

    private void CopyKey()
    {
        if (_treeView.SelectedNode?.Tag is NodeTag tag && tag.Key != null)
            Clipboard.SetText(tag.Key);
    }

    private void CopyValue()
    {
        if (_treeView.SelectedNode?.Tag is not NodeTag tag) return;
        string val = tag.Value switch
        {
            JsonObject obj => obj.ToFormattedString(),
            JsonArray arr => JsonParser.Serialize(arr, true),
            _ => FormatValue(tag.Value)
        };
        Clipboard.SetText(val);
    }

    private void CopyPath()
    {
        if (_treeView.SelectedNode == null) return;
        var parts = new List<string>();
        var current = _treeView.SelectedNode;
        while (current?.Parent != null)
        {
            if (current.Tag is NodeTag tag && tag.Key != null)
                parts.Add(tag.Key.StartsWith('[') ? tag.Key : $".{tag.Key}");
            current = current.Parent;
        }
        parts.Reverse();
        string path = string.Join("", parts).TrimStart('.');
        if (!string.IsNullOrEmpty(path))
            Clipboard.SetText(path);
    }

    #endregion

    #region Context Menu

    private void OnContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        var node = _treeView.SelectedNode;
        bool isObj = node?.Tag is NodeTag { Value: JsonObject };
        bool isArr = node?.Tag is NodeTag { Value: JsonArray };
        bool isLeaf = node?.Tag is NodeTag t && t.Value is not JsonObject && t.Value is not JsonArray;
        bool isRoot = node?.Parent == null;

        _contextMenu.Items[0].Visible = isLeaf;     // Edit Value
        _contextMenu.Items[2].Visible = isObj;       // Add Property
        _contextMenu.Items[3].Visible = isArr;       // Add Array Item
        _contextMenu.Items[5].Visible = !isRoot;     // Delete
    }

    #endregion

    #region Search

    private readonly List<TreeNode> _searchResults = new();
    private int _searchIndex;
    private string _lastSearchQuery = "";

    private readonly List<List<string>> _searchPaths = new();

    private void OnSearch()
    {
        string query = _searchBox.Text.Trim();
        if (string.IsNullOrEmpty(query)) return;

        // If the query hasn't changed and we have results, advance to next
        if (query == _lastSearchQuery && _searchPaths.Count > 0)
        {
            FindNext();
            return;
        }

        _lastSearchQuery = query;
        ClearHighlights();
        _searchResults.Clear();
        _searchPaths.Clear();
        _searchIndex = 0;

        if (_saveData == null) return;

        // Search the JSON data structure directly (not the tree nodes)
        // to avoid force-expanding the entire tree.
        var path = new List<string>();
        SearchJsonData(_saveData, query.ToLowerInvariant(), path);

        if (_searchPaths.Count > 0)
        {
            // Navigate to first result by expanding the tree path
            NavigateToSearchResult(0);
            _statusLabel.Text = UiStrings.Format("raw_json.search_found", _searchPaths.Count);
            _statusLabel.ForeColor = Color.Green;
        }
        else
        {
            _statusLabel.Text = UiStrings.Get("raw_json.no_matches_found");
            _statusLabel.ForeColor = Color.Red;
        }
    }

    private void FindNext()
    {
        if (_searchPaths.Count == 0) return;

        // Dim previous result
        if (_searchIndex >= 0 && _searchIndex < _searchResults.Count)
            _searchResults[_searchIndex].BackColor = Color.LightYellow;

        _searchIndex = (_searchIndex + 1) % _searchPaths.Count;
        NavigateToSearchResult(_searchIndex);
        _statusLabel.Text = UiStrings.Format("raw_json.match_position", _searchIndex + 1, _searchPaths.Count);
    }

    private void SearchJsonData(object? value, string query, List<string> path)
    {
        const int maxResults = 500;
        if (_searchPaths.Count >= maxResults) return;

        if (value is JsonObject obj)
        {
            var names = obj.Names();
            for (int i = 0; i < names.Count; i++)
            {
                if (_searchPaths.Count >= maxResults) return;
                string key = names[i];
                object? child = obj.Get(key);
                path.Add(key);

                // Check if key matches
                if (key.ToLowerInvariant().Contains(query))
                    _searchPaths.Add(new List<string>(path));

                // Check leaf value
                if (child is not JsonObject && child is not JsonArray)
                {
                    string display = FormatValue(child);
                    if (display.ToLowerInvariant().Contains(query))
                        if (_searchPaths.Count == 0 || !PathsEqual(_searchPaths[^1], path))
                            _searchPaths.Add(new List<string>(path));
                }

                // Recurse into children
                if (child is JsonObject || child is JsonArray)
                    SearchJsonData(child, query, path);

                path.RemoveAt(path.Count - 1);
            }
        }
        else if (value is JsonArray arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                if (_searchPaths.Count >= maxResults) return;
                object? child = arr.Get(i);
                path.Add($"[{i}]");

                if (child is not JsonObject && child is not JsonArray)
                {
                    string display = FormatValue(child);
                    if (display.ToLowerInvariant().Contains(query))
                        _searchPaths.Add(new List<string>(path));
                }

                if (child is JsonObject || child is JsonArray)
                    SearchJsonData(child, query, path);

                path.RemoveAt(path.Count - 1);
            }
        }
    }

    private static bool PathsEqual(List<string> a, List<string> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    private void NavigateToSearchResult(int index)
    {
        if (index < 0 || index >= _searchPaths.Count) return;

        var path = _searchPaths[index];
        TreeNode? current = _treeView.Nodes.Count > 0 ? _treeView.Nodes[0] : null; // Root node

        _treeView.BeginUpdate();
        for (int p = 0; p < path.Count && current != null; p++)
        {
            // Ensure lazy children are expanded
            if (current.Nodes.Count == 1 && current.Nodes[0].Tag is LazyTag)
            {
                var tag = current.Tag as NodeTag;
                current.Nodes.Clear();
                if (tag?.Value is JsonObject obj)
                    PopulateObjectNode(current, obj, maxDepth: 2, currentDepth: 0);
                else if (tag?.Value is JsonArray arr)
                    PopulateArrayNode(current, arr, maxDepth: 2, currentDepth: 0);
            }

            current.Expand();
            string segment = path[p];

            // Find matching child node
            TreeNode? found = null;
            foreach (TreeNode child in current.Nodes)
            {
                if (child.Tag is NodeTag childTag && childTag.Key == segment)
                {
                    found = child;
                    break;
                }
            }
            current = found;
        }
        _treeView.EndUpdate();

        if (current != null)
        {
            _searchResults.Add(current);
            current.BackColor = Color.Yellow;
            _treeView.SelectedNode = current;
            current.EnsureVisible();
        }
    }

    private void ClearHighlights()
    {
        foreach (var node in _searchResults)
            node.BackColor = Color.Empty;
        _searchResults.Clear();
    }

    private void OnTreeKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete)
        {
            DeleteSelectedNode();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.F2)
        {
            BeginEditSelectedNode();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.F3 || (e.KeyCode == Keys.Enter && _searchPaths.Count > 0))
        {
            FindNext();
            e.Handled = true;
        }
    }

    #endregion

    #region Text View Handlers

    private void OnFormat(object? sender, EventArgs e)
    {
        try
        {
            _jsonTextBox.Text = RawJsonLogic.FormatJson(_jsonTextBox.Text);
            _statusLabel.Text = UiStrings.Get("raw_json.formatted");
            _statusLabel.ForeColor = Color.Green;
        }
        catch (JsonException ex)
        {
            _statusLabel.Text = $"Error: {ex.Message}";
            _statusLabel.ForeColor = Color.Red;
        }
    }

    private void OnValidate(object? sender, EventArgs e)
    {
        try
        {
            RawJsonLogic.ParseJson(_jsonTextBox.Text);
            _statusLabel.Text = UiStrings.Get("raw_json.json_valid");
            _statusLabel.ForeColor = Color.Green;
        }
        catch (JsonException ex)
        {
            _statusLabel.Text = UiStrings.Format("raw_json.invalid_json", ex.Message);
            _statusLabel.ForeColor = Color.Red;
        }
    }

    #endregion

    #region Helper Types

    private class NodeTag
    {
        public object? Value { get; set; }
        public object? Parent { get; }
        public string? Key { get; set; }

        public NodeTag(object? value, object? parent, string? key)
        {
            Value = value;
            Parent = parent;
            Key = key;
        }
    }

    private class LazyTag
    {
        public static readonly LazyTag Instance = new();
    }

    #endregion

    public void ApplyUiLocalisation()
    {
        _titleLabel.Text = UiStrings.Get("raw_json.title");
        _treeViewButton.Text = UiStrings.Get("raw_json.tree_view");
        _textViewButton.Text = UiStrings.Get("raw_json.text_view");
        _formatButton.Text = UiStrings.Get("raw_json.format");
        _validateButton.Text = UiStrings.Get("raw_json.validate");
        _expandAllButton.Text = UiStrings.Get("raw_json.expand_all");
        _stopExpandBtn.Text = UiStrings.Get("raw_json.stop");
        _collapseAllButton.Text = UiStrings.Get("raw_json.collapse_all");
        _searchBox.PlaceholderText = UiStrings.Get("raw_json.search_placeholder");
        _searchButton.Text = UiStrings.Get("raw_json.find");

        // Context menu items (by position, skipping separators)
        if (_contextMenu.Items.Count >= 10)
        {
            _contextMenu.Items[0].Text = UiStrings.Get("raw_json.ctx_edit_value");
            _contextMenu.Items[2].Text = UiStrings.Get("raw_json.ctx_add_property");
            _contextMenu.Items[3].Text = UiStrings.Get("raw_json.ctx_add_array_item");
            _contextMenu.Items[5].Text = UiStrings.Get("raw_json.ctx_delete");
            _contextMenu.Items[7].Text = UiStrings.Get("raw_json.ctx_copy_key");
            _contextMenu.Items[8].Text = UiStrings.Get("raw_json.ctx_copy_value");
            _contextMenu.Items[9].Text = UiStrings.Get("raw_json.ctx_copy_path");
        }
    }
}
