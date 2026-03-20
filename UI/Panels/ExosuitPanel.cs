using NMSE.Data;
using NMSE.Models;
using NMSE.Core;

namespace NMSE.UI.Panels;

public partial class ExosuitPanel : UserControl
{
    private JsonObject? _playerState;

    /// <summary>Raised when inventory data is modified by the user.</summary>
    public event EventHandler? DataModified;

    public ExosuitPanel()
    {
        InitializeComponent();
        SetupLayout();
    }

    public void SetDatabase(GameItemDatabase? database)
    {
        _generalGrid.SetDatabase(database);
        _techGrid.SetDatabase(database);
    }

    public void SetIconManager(IconManager? iconManager)
    {
        _generalGrid.SetIconManager(iconManager);
        _techGrid.SetIconManager(iconManager);
    }

    public void LoadData(JsonObject saveData)
    {
        try
        {
            _playerState = saveData.GetObject("PlayerStateData");
            if (_playerState == null) return;

            _generalGrid.LoadInventory(_playerState.GetObject(ExosuitLogic.CargoInventoryKey));
            _techGrid.LoadInventory(_playerState.GetObject(ExosuitLogic.TechInventoryKey));
        }
        catch { /* Save structure varies between versions */ }
    }

    public void SaveData(JsonObject saveData)
    {
        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            _generalGrid.SaveInventory(playerState.GetObject(ExosuitLogic.CargoInventoryKey));
            _techGrid.SaveInventory(playerState.GetObject(ExosuitLogic.TechInventoryKey));
        }
        catch { }
    }

    public void ApplyUiLocalisation()
    {
        _titleLabel.Text = UiStrings.Get("exosuit.title");
        _generalPage.Text = UiStrings.Get("common.cargo");
        _techPage.Text = UiStrings.Get("common.technology");
        _generalGrid.SetMaxSupportedLabel(ExosuitLogic.CargoMaxLabel);
        _techGrid.SetMaxSupportedLabel(ExosuitLogic.TechMaxLabel);
        _generalGrid.ApplyUiLocalisation();
        _techGrid.ApplyUiLocalisation();
    }
}
