using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMSE.Core;
using NMSE.Data;
using NMSE.Models;
using NMSE.UI.ViewModels.Controls;

namespace NMSE.UI.ViewModels.Panels;

public partial class BaseViewModel : PanelViewModelBase
{
    private JsonObject? _playerState;
    private GameItemDatabase? _database;
    private IconManager? _iconManager;

    [ObservableProperty] private int _selectedTabIndex;

    [ObservableProperty] private ObservableCollection<BaseInfoViewModel> _bases = new();
    [ObservableProperty] private BaseInfoViewModel? _selectedBase;
    [ObservableProperty] private string _baseName = "";
    [ObservableProperty] private string _baseItemCount = "";
    [ObservableProperty] private bool _hasBaseSelection;

    [ObservableProperty] private ObservableCollection<NpcWorkerViewModel> _npcWorkers = new();
    [ObservableProperty] private NpcWorkerViewModel? _selectedNpc;
    [ObservableProperty] private string _npcSeed = "";
    [ObservableProperty] private string _npcRace = "";

    [ObservableProperty] private ObservableCollection<InventoryGridViewModel> _chestGrids = new();

    [ObservableProperty] private ObservableCollection<StorageTabViewModel> _storageTabs = new();

    partial void OnSelectedBaseChanged(BaseInfoViewModel? value)
    {
        HasBaseSelection = value != null;
        if (value == null) return;
        BaseName = value.Data?.GetString("Name") ?? "";
        int objectCount = 0;
        try
        {
            var objects = value.Data?.GetArray("Objects");
            if (objects != null) objectCount = objects.Length;
        }
        catch { }
        BaseItemCount = objectCount.ToString();
    }

    public override void LoadData(JsonObject saveData, GameItemDatabase database, IconManager? iconManager)
    {
        _database = database;
        _iconManager = iconManager;

        Bases.Clear();
        NpcWorkers.Clear();
        ChestGrids.Clear();
        StorageTabs.Clear();

        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;
            _playerState = playerState;

            LoadBases(playerState);
            LoadNpcWorkers(playerState);
            LoadChests(playerState);
            LoadStorage(playerState);
        }
        catch { }
    }

    private void LoadBases(JsonObject playerState)
    {
        var bases = playerState.GetArray("PersistentPlayerBases");
        if (bases == null) return;

        for (int i = 0; i < bases.Length; i++)
        {
            try
            {
                var baseObj = bases.GetObject(i);
                string? baseType = null;
                try { baseType = baseObj.GetString("BaseType.PersistentBaseTypes") ?? baseObj.GetString("BaseType"); }
                catch { try { baseType = baseObj.GetString("BaseType"); } catch { } }

                int baseVersion = 0;
                try { baseVersion = baseObj.GetInt("BaseVersion"); } catch { }

                if ("HomePlanetBase".Equals(baseType, StringComparison.OrdinalIgnoreCase) && baseVersion >= 3)
                {
                    string name = baseObj.GetString("Name") ?? $"Base {i + 1}";
                    int objectCount = 0;
                    try
                    {
                        var objects = baseObj.GetArray("Objects");
                        if (objects != null) objectCount = objects.Length;
                    }
                    catch { }

                    Bases.Add(new BaseInfoViewModel
                    {
                        DisplayName = name,
                        Data = baseObj,
                        DataIndex = i,
                        ObjectCount = objectCount
                    });
                }
            }
            catch { }
        }

        if (Bases.Count > 0)
            SelectedBase = Bases[0];
    }

    private void LoadNpcWorkers(JsonObject playerState)
    {
        var npcWorkers = playerState.GetArray("NPCWorkers");
        if (npcWorkers == null) return;

        string[] workerNames = { "Armorer", "Farmer", "Overseer", "Technician", "Scientist" };

        for (int i = 0; i < npcWorkers.Length && i < 5; i++)
        {
            try
            {
                var npc = npcWorkers.GetObject(i);
                bool hired = false;
                try { hired = npc.GetBool("HiredWorker"); } catch { }
                if (hired)
                {
                    NpcWorkers.Add(new NpcWorkerViewModel
                    {
                        Name = workerNames[i],
                        Data = npc,
                        Index = i
                    });
                }
            }
            catch { }
        }

        if (NpcWorkers.Count > 0)
            SelectedNpc = NpcWorkers[0];
    }

    private void LoadChests(JsonObject playerState)
    {
        for (int i = 0; i < 10; i++)
        {
            string key = $"Chest{i + 1}Inventory";
            var inv = playerState.GetObject(key);

            var grid = new InventoryGridViewModel();
            grid.SetIsCargoInventory(true);
            grid.SetInventoryOwnerType("Chest");
            grid.SetInventoryGroup($"Chest {i + 1}");
            grid.SetSuperchargeDisabled(true);
            if (_database != null) grid.SetDatabase(_database);
            grid.SetIconManager(_iconManager);
            grid.LoadInventory(inv);

            ChestGrids.Add(grid);
        }
    }

    private void LoadStorage(JsonObject playerState)
    {
        (string Label, string Key)[] storageKeys =
        {
            ("Ingredient Storage", "CookingIngredientsInventory"),
            ("Corvette Parts", "CorvetteStorageInventory"),
            ("Salvage Capsule", "ChestMagicInventory"),
            ("Rocket Locker", "RocketLockerInventory"),
            ("Fishing Platform", "FishPlatformInventory"),
            ("Fish Bait Box", "FishBaitBoxInventory"),
            ("Food Unit", "FoodUnitInventory"),
            ("Freighter Refund", "ChestMagic2Inventory"),
        };

        foreach (var (label, key) in storageKeys)
        {
            var inv = playerState.GetObject(key);
            var grid = new InventoryGridViewModel();
            grid.SetIsCargoInventory(true);
            grid.SetInventoryOwnerType("Storage");
            grid.SetInventoryGroup(label);
            grid.SetSuperchargeDisabled(true);
            if (_database != null) grid.SetDatabase(_database);
            grid.SetIconManager(_iconManager);
            grid.LoadInventory(inv);

            StorageTabs.Add(new StorageTabViewModel
            {
                Label = label,
                Grid = grid
            });
        }
    }

    [RelayCommand]
    private void SaveBaseName()
    {
        if (SelectedBase?.Data == null) return;
        SelectedBase.Data.Set("Name", BaseName);
        SelectedBase.DisplayName = BaseName;
    }

    [RelayCommand]
    private void GenerateNpcSeed()
    {
        byte[] bytes = new byte[8];
        Random.Shared.NextBytes(bytes);
        NpcSeed = "0x" + BitConverter.ToString(bytes).Replace("-", "");
    }

    public override void SaveData(JsonObject saveData)
    {
        if (SelectedBase?.Data != null && !string.IsNullOrEmpty(BaseName))
            SelectedBase.Data.Set("Name", BaseName);
    }
}

public partial class BaseInfoViewModel : ObservableObject
{
    [ObservableProperty] private string _displayName = "";
    public JsonObject? Data { get; set; }
    public int DataIndex { get; set; }
    public int ObjectCount { get; set; }
    public override string ToString() => DisplayName;
}

public partial class NpcWorkerViewModel : ObservableObject
{
    [ObservableProperty] private string _name = "";
    public JsonObject? Data { get; set; }
    public int Index { get; set; }
    public override string ToString() => Name;
}

public partial class StorageTabViewModel : ObservableObject
{
    [ObservableProperty] private string _label = "";
    [ObservableProperty] private InventoryGridViewModel _grid = new();
}
