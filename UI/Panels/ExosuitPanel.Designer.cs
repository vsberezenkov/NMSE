using NMSE.Core;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

partial class ExosuitPanel
{
    private System.ComponentModel.IContainer components = null;

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
        this._layout = new System.Windows.Forms.TableLayoutPanel();
        this._titleLabel = new System.Windows.Forms.Label();
        this._generalGrid = new NMSE.UI.Panels.InventoryGridPanel();
        this._techGrid = new NMSE.UI.Panels.InventoryGridPanel();
        this._invTabs = new NMSE.UI.Panels.DoubleBufferedTabControl();
        this._generalPage = new System.Windows.Forms.TabPage();
        this._techPage = new System.Windows.Forms.TabPage();
        this._layout.SuspendLayout();
        this._invTabs.SuspendLayout();
        this._generalPage.SuspendLayout();
        this._techPage.SuspendLayout();
        this.SuspendLayout();
        //
        // _layout
        //
        this._layout.Dock = System.Windows.Forms.DockStyle.Fill;
        this._layout.ColumnCount = 1;
        this._layout.RowCount = 2;
        this._layout.Padding = new System.Windows.Forms.Padding(10);
        this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._layout.Controls.Add(this._titleLabel, 0, 0);
        this._layout.Controls.Add(this._invTabs, 0, 1);
        //
        // _titleLabel
        //
        this._titleLabel.Text = "Exosuit Inventory";
        FontManager.ApplyHeadingFont(_titleLabel, 14);
        this._titleLabel.AutoSize = true;
        this._titleLabel.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
        //
        // _generalGrid
        //
        this._generalGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        //
        // _techGrid
        //
        this._techGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        //
        // _invTabs
        //
        this._invTabs.Dock = System.Windows.Forms.DockStyle.Fill;
        this._invTabs.TabPages.Add(this._generalPage);
        this._invTabs.TabPages.Add(this._techPage);
        //
        // _generalPage
        //
        this._generalPage.Text = "Cargo";
        this._generalPage.Controls.Add(this._generalGrid);
        //
        // _techPage
        //
        this._techPage.Text = "Technology";
        this._techPage.Controls.Add(this._techGrid);
        //
        // ExosuitPanel
        //
        this.DoubleBuffered = true;
        this.Controls.Add(this._layout);
        this._layout.ResumeLayout(false);
        this._invTabs.ResumeLayout(false);
        this._generalPage.ResumeLayout(false);
        this._techPage.ResumeLayout(false);
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private void SetupLayout()
    {
        _techGrid.SetIsTechInventory(true);
        _generalGrid.SetIsCargoInventory(true);
        _generalGrid.SetSortingEnabled(true);
        _techGrid.SetSortingEnabled(false);
        _techGrid.SetInventoryOwnerType("Suit");
        _generalGrid.SetInventoryOwnerType("Suit");
        _generalGrid.SetInventoryGroup("PersonalCargo");
        _generalGrid.SetPinSlotFeatureEnabled(true);
        _techGrid.SetInventoryGroup("Personal");
        _generalGrid.DataModified += (s, e) => DataModified?.Invoke(this, e);
        _techGrid.DataModified += (s, e) => DataModified?.Invoke(this, e);
        _generalGrid.PinnedSlotsChanged += OnPinnedSlotsChanged;
        _generalGrid.AutoStackToStorageRequested += OnAutoStackToStorageRequested;
        _generalGrid.AutoStackToStarshipRequested += OnAutoStackToStarshipRequested;
        _generalGrid.AutoStackToFreighterRequested += OnAutoStackToFreighterRequested;
        _generalGrid.AutoStackSelectedSlotToStorageRequested += OnAutoStackSelectedSlotToStorageRequested;
        _generalGrid.AutoStackSelectedSlotToStarshipRequested += OnAutoStackSelectedSlotToStarshipRequested;
        _generalGrid.AutoStackSelectedSlotToFreighterRequested += OnAutoStackSelectedSlotToFreighterRequested;
        _generalGrid.RefreshToolbarActions();
        _techGrid.RefreshToolbarActions();
        var cfg = ExportConfig.Instance;
        _generalGrid.SetExportFileName($"exosuit_cargo_inv{cfg.ExosuitExt}");
        _techGrid.SetExportFileName($"exosuit_tech_inv{cfg.ExosuitExt}");
        string cargoExportFilter = ExportConfig.BuildDialogFilter(cfg.ExosuitExt, "Exosuit cargo");
        string cargoImportFilter = ExportConfig.BuildImportFilter(cfg.ExosuitExt, "Exosuit cargo");
        _generalGrid.SetExportFileFilter(cargoExportFilter, cargoImportFilter, cfg.ExosuitExt.TrimStart('.'));
        string techExportFilter = ExportConfig.BuildDialogFilter(cfg.ExosuitExt, "Exosuit tech");
        string techImportFilter = ExportConfig.BuildImportFilter(cfg.ExosuitExt, "Exosuit tech");
        _techGrid.SetExportFileFilter(techExportFilter, techImportFilter, cfg.ExosuitExt.TrimStart('.'));
        _generalGrid.SetMaxSupportedLabel(ExosuitLogic.CargoMaxLabel);
        _techGrid.SetMaxSupportedLabel(ExosuitLogic.TechMaxLabel);
        _generalGrid.SetSuperchargeDisabled(true);
    }

    private System.Windows.Forms.TableLayoutPanel _layout;
    private System.Windows.Forms.Label _titleLabel;
    private NMSE.UI.Panels.DoubleBufferedTabControl _invTabs;
    private System.Windows.Forms.TabPage _generalPage;
    private System.Windows.Forms.TabPage _techPage;
    private InventoryGridPanel _generalGrid;
    private InventoryGridPanel _techGrid;
}
