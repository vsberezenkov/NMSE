namespace NMSE.Core;

/// <summary>
/// Defines constants for exosuit inventory export filenames, size labels, and JSON keys.
/// </summary>
internal static class ExosuitLogic
{
    /// <summary>Default export filename for the exosuit cargo inventory.</summary>
    internal const string CargoExportFileName = "exosuit_cargo_inv.json";
    /// <summary>Default export filename for the exosuit tech inventory.</summary>
    internal const string TechExportFileName = "exosuit_tech_inv.json";
    /// <summary>Maximum supported cargo inventory dimensions.</summary>
    internal const string CargoDimensions = "10x12";
    /// <summary>Maximum supported tech inventory dimensions.</summary>
    internal const string TechDimensions = "10x6";
    /// <summary>Label describing the maximum supported cargo inventory size.</summary>
    internal static string CargoMaxLabel => NMSE.Data.UiStrings.Format("common.max_supported", CargoDimensions);
    /// <summary>Label describing the maximum supported tech inventory size.</summary>
    internal static string TechMaxLabel => NMSE.Data.UiStrings.Format("common.max_supported", TechDimensions);
    /// <summary>JSON key for the exosuit cargo inventory in player state.</summary>
    internal const string CargoInventoryKey = "Inventory";
    /// <summary>JSON key for the exosuit tech inventory in player state.</summary>
    internal const string TechInventoryKey = "Inventory_TechOnly";
}
