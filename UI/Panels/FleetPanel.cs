using NMSE.Data;
using NMSE.Models;

namespace NMSE.UI.Panels;

/// <summary>
/// Container panel that groups Freighter, Frigates, and Squadron as sub-tabs.
/// </summary>
public partial class FleetPanel : UserControl
{
    private readonly FreighterPanel _freighterPanel;
    private readonly FrigatePanel _frigatePanel;
    private readonly SquadronPanel _squadronPanel;

    public FleetPanel(FreighterPanel freighterPanel, FrigatePanel frigatePanel, SquadronPanel squadronPanel)
    {
        _freighterPanel = freighterPanel;
        _frigatePanel = frigatePanel;
        _squadronPanel = squadronPanel;

        InitializeComponent();

        _freighterPanel.Dock = DockStyle.Fill;
        _freighterTab.Controls.Add(_freighterPanel);

        _frigatePanel.Dock = DockStyle.Fill;
        _frigateTab.Controls.Add(_frigatePanel);

        _squadronPanel.Dock = DockStyle.Fill;
        _squadronTab.Controls.Add(_squadronPanel);
    }

    public void SetDatabase(GameItemDatabase? database)
    {
        _freighterPanel.SetDatabase(database);
        _frigatePanel.SetDatabase(database);
    }

    public void SetIconManager(IconManager? iconManager)
    {
        _freighterPanel.SetIconManager(iconManager);
    }

    public void LoadData(JsonObject saveData)
    {
        _freighterPanel.LoadData(saveData);
        _frigatePanel.LoadData(saveData);
        _squadronPanel.LoadData(saveData);
    }

    public void SaveData(JsonObject saveData)
    {
        _freighterPanel.SaveData(saveData);
        _frigatePanel.SaveData(saveData);
        _squadronPanel.SaveData(saveData);
    }

    public void ApplyUiLocalisation()
    {
        _freighterTab.Text = UiStrings.Get("fleet.tab_freighter");
        _frigateTab.Text = UiStrings.Get("fleet.tab_frigates");
        _squadronTab.Text = UiStrings.Get("fleet.tab_squadron");

        _freighterPanel.ApplyUiLocalisation();
        _frigatePanel.ApplyUiLocalisation();
        _squadronPanel.ApplyUiLocalisation();
    }
}
