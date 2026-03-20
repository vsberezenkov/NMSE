namespace NMSE.Data;

/// <summary>
/// Static database of procedural product stubs.
/// These items use ^-prefixed IDs in save data (e.g. ^UP_FRHYP) and are not
/// included in the JSON game item files. This class provides lookup data so
/// the editor can resolve their names, icons, and descriptions.
/// </summary>
public static class ProceduralStubs
{
    /// <summary>A procedural product stub with display metadata.</summary>
    public record Entry(
        string Id,
        string Name,
        string Icon,
        string Category,
        string Subtitle,
        string Description
    );

    /// <summary>All known procedural product stub entries.</summary>
    public static readonly IReadOnlyList<Entry> Items = new Entry[]
    {
        new("PROC_LOOT", "Unearthed Treasure", "PROC_LOOT.png", "PROCEDURAL", "Unearthed Treasure", "Unearthed Treasure"),
        new("PROC_HIST", "Historical Document", "WAR_CURIO3.png", "PROCEDURAL", "Historical Document", "Historical Document"),
        new("PROC_BIO", "Biological Sample", "PROC_BIO.png", "PROCEDURAL", "Biological Sample", "Biological Sample"),
        new("PROC_FOSS", "Fossil Sample", "PROC_FOSS.png", "PROCEDURAL", "Fossil Sample", "Fossil Sample"),
        new("PROC_PLNT", "Delicate Flora", "PLANTPOT3.png", "PROCEDURAL", "Delicate Flora", "Delicate Flora"),
        new("PROC_TOOL", "Lost Artifact", "TRA_COMPONENT4.png", "PROCEDURAL", "Lost Artifact", "Lost Artifact"),
        new("PROC_FARM", "Delicate Flora", "PLANTPOT3.png", "PROCEDURAL", "Delicate Flora", "A genetically engineered plant, ready for propagation."),
        new("PROC_SEA", "Aquatic Treasure", "PROC_SEA.png", "PROCEDURAL", "Aquatic Treasure", "A well-preserved marine treasure, found deep under the ocean."),
        new("PROC_FEAR", "Terrifying Sample", "PROC_FEAR.png", "PROCEDURAL", "Terrifying Sample", "An appalling relic, the haunted remains of some abyssal horror."),
        new("PROC_SALV", "Salvaged Scrap", "PROC_SALV.png", "PROCEDURAL", "Salvaged Scrap", "Salvaged Scrap"),
        new("PROC_BONE", "Excavated Bones", "PROC_BONE.png", "PROCEDURAL", "Excavated Bones", "Excavated Bones"),
        new("PROC_DARK", "Terrifying Sample", "PROC_FEAR.png", "PROCEDURAL", "Terrifying Sample", "An appalling relic, the haunted remains of some abyssal horror."),
        new("PROC_STAR", "Ancient Skeleton", "PROC_STAR.png", "PROCEDURAL", "Ancient Skeleton", "The remains of a true deep-space leviathan."),
        new("PROC_PASS", "Mainframe Access Card", "PROC_PASS.png", "PROCEDURAL", "Mainframe Access Card", ""),
        new("PROC_CAPT", "Official Record", "PROC_CAPT.png", "PROCEDURAL", "Official Record", ""),
        new("PROC_CREW", "Official Record", "PROC_CREW.png", "PROCEDURAL", "Official Record", ""),
        new("UP_FRHYP", "Salvaged Fleet Hyperdrive Upgrade", "U_HYPER1.png", "CONSUMABLE", "Deployable Salvage", "A deployable freighter upgrade. Can be re-deployed into your own capital ship to improve its Hyperdrive."),
        new("UP_FRSPE", "Salvaged Fleet Beacon", "U_PULSE1.png", "CONSUMABLE", "Deployable Salvage", "A deployable freighter upgrade. Can be re-deployed into your own capital ship to improve the speed of your fleet."),
        new("UP_FRFUE", "Salvaged Fleet Fuel Unit", "U_JETBOOST1.png", "CONSUMABLE", "Deployable Salvage", "A deployable freighter upgrade. Can be re-deployed into your own capital ship to improve the fuel efficiency of your fleet."),
        new("UP_FRTRA", "Salvaged Fleet Trade Unit", "U_RAIL1.png", "CONSUMABLE", "Deployable Salvage", "A deployable freighter upgrade. Can be re-deployed into your own capital ship to improve the trading abilities of your fleet."),
        new("UP_FRCOM", "Salvaged Fleet Combat Unit", "U_SHIPGUN1.png", "CONSUMABLE", "Deployable Salvage", "A deployable freighter upgrade. Can be re-deployed into your own capital ship to improve the combat performance of your fleet."),
        new("UP_FRMIN", "Salvaged Fleet Mining Unit", "U_LASER1.png", "CONSUMABLE", "Deployable Salvage", "A deployable freighter upgrade. Can be re-deployed into your own capital ship to improve the mining capabilities of your fleet."),
        new("UP_FREXP", "Salvaged Fleet Exploration Unit", "U_SCANNER1.png", "CONSUMABLE", "Deployable Salvage", "A deployable freighter upgrade. Can be re-deployed into your own capital ship to improve the exploration abilities of your fleet."),
        new("PROC_LUMP", "Recovered Item", "PROC_LUMP.png", "PROCEDURAL", "Recovered Item", "Recovered Item"),
        new("PROC_COG", "Recovered Item", "PROC_COG.png", "PROCEDURAL", "Recovered Item", "Recovered Item"),
        new("PROC_DATA", "Recovered Item", "PROC_DATA.png", "PROCEDURAL", "Recovered Item", "Recovered Item"),
        new("PROC_BOTT", "Collected Flotsam", "S15_MESSAGE0.png", "PROCEDURAL", "Collected Flotsam", "Collected Flotsam"),
        new("PROC_EXH", "Fossil Sample", "PROC_FOSS.png", "PROCEDURAL", "Fossil Sample", "Fossil Sample"),
    };

    /// <summary>
    /// Lookup by Id (without ^ prefix), case-insensitive.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Entry> ById =
        Items.ToDictionary(e => e.Id, e => e, StringComparer.OrdinalIgnoreCase);
}
