using NMSE.Data;
using NMSE.Models;

namespace NMSE.UI.ViewModels.Panels;

public abstract class PanelViewModelBase : ViewModelBase
{
    public virtual void LoadData(JsonObject saveData, GameItemDatabase database, IconManager? iconManager) { }
    public virtual void SaveData(JsonObject saveData) { }
}
