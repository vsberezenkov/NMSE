using NMSE.Core;

namespace NMSE.UI.Panels;

partial class MilestonePanel
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

    private void InitializeComponent()
    {
        DoubleBuffered = true;
        SuspendLayout();

        _tabControl = new DoubleBufferedTabControl { Dock = DockStyle.Fill };

        // === Tab 1: Main Stats (Milestones+Kills | Alien Factions | Guilds) ===
        var tab1 = new TabPage("Main Stats");
        var scroll1 = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

        var section1 = CreateThreeColumnSection();
        var s1c1 = GetColumnPanel(section1, 0);
        var s1c2 = GetColumnPanel(section1, 1);
        var s1c3 = GetColumnPanel(section1, 2);

        // Column 1: Milestones + Kills
        AddSectionTitle(s1c1, "Milestones", "milestone.section_milestones");
        AddField(s1c1, "milestone.on_foot_exploration", "^DIST_WALKED");
        AddField(s1c1, "milestone.alien_encounters", "^ALIENS_MET");
        AddField(s1c1, "milestone.words_collected", "^WORDS_LEARNT");
        AddField(s1c1, "milestone.most_units_accrued", "^MONEY");
        AddField(s1c1, "milestone.ships_destroyed", "^ENEMIES_KILLED");
        AddField(s1c1, "milestone.sentinels_destroyed", "^SENTINEL_KILLS");
        AddField(s1c1, "milestone.space_exploration", "^DIST_WARP");
        AddField(s1c1, "milestone.planet_zoology_scanned", "^DISC_ALL_CREATU");
        AddSectionTitle(s1c1, "Kills", "milestone.section_kills");
        AddField(s1c1, "milestone.predators", "^PREDS_KILLED");
        AddField(s1c1, "milestone.sentinel_drones", "^DRONES_KILLED");
        AddField(s1c1, "milestone.sentinel_quads", "^QUADS_KILLED");
        AddField(s1c1, "milestone.sentinel_walkers", "^WALKERS_KILLED");
        AddField(s1c1, "milestone.pirates", "^PIRATES_KILLED");
        AddField(s1c1, "milestone.police", "^POLICE_KILLED");

        // Column 2: Alien Factions
        AddSectionTitle(s1c2, "Alien Factions", "milestone.section_alien_factions");
        AddSectionTitle(s1c2, "Gek", "milestone.gek");
        AddField(s1c2, "milestone.standing", "^TRA_STANDING");
        AddField(s1c2, "milestone.missions", "^TDONE_MISSIONS");
        AddField(s1c2, "milestone.systems_visited", "^TSEEN_SYSTEMS");
        AddSectionTitle(s1c2, "Vy'keen", "milestone.vykeen");
        AddField(s1c2, "milestone.standing", "^WAR_STANDING");
        AddField(s1c2, "milestone.missions", "^WDONE_MISSIONS");
        AddField(s1c2, "milestone.systems_visited", "^WSEEN_SYSTEMS");
        AddSectionTitle(s1c2, "Korvax", "milestone.korvax");
        AddField(s1c2, "milestone.standing", "^EXP_STANDING");
        AddField(s1c2, "milestone.missions", "^EDONE_MISSIONS");
        AddField(s1c2, "milestone.systems_visited", "^ESEEN_SYSTEMS");
        AddSectionTitle(s1c2, "Autophage", "milestone.autophage");
        AddField(s1c2, "milestone.standing", "^BUI_STANDING");
        AddField(s1c2, "milestone.missions", "^BDONE_MISSIONS");

        // Column 3: Guilds
        AddSectionTitle(s1c3, "Guilds", "milestone.section_guilds");
        AddSectionTitle(s1c3, "Traders", "milestone.traders");
        AddField(s1c3, "milestone.standing", "^TGUILD_STAND");
        AddField(s1c3, "milestone.missions", "^TGDONE_MISSIONS");
        AddField(s1c3, "milestone.plants_farmed", "^PLANTS_PLANTED");
        AddSectionTitle(s1c3, "Warriors", "milestone.warriors");
        AddField(s1c3, "milestone.standing", "^WGUILD_STAND");
        AddField(s1c3, "milestone.missions", "^WGDONE_MISSIONS");
        AddSectionTitle(s1c3, "Explorers", "milestone.explorers");
        AddField(s1c3, "milestone.standing", "^EGUILD_STAND");
        AddField(s1c3, "milestone.missions", "^EGDONE_MISSIONS");
        AddField(s1c3, "milestone.rare_creatures", "^RARE_SCANNED");
        AddSectionTitle(s1c3, "Pirate", "milestone.pirate");
        AddField(s1c3, "milestone.standing", "^PIRATE_STAND");
        AddField(s1c3, "milestone.missions", "^PDONE_MISSIONS");
        AddField(s1c3, "milestone.systems_visited", "^PIRATE_SYSTEMS");

        scroll1.Controls.Add(section1);
        tab1.Controls.Add(scroll1);
        _tabControl.TabPages.Add(tab1);

        // === Tab 2: Other Stats (Other Milestones/Stats | Discoveries | Activities) ===
        var tab2 = new TabPage("Other Stats");
        var scroll2 = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

        var section2 = CreateThreeColumnSection();
        var s2c1 = GetColumnPanel(section2, 0);
        var s2c2 = GetColumnPanel(section2, 1);
        var s2c3 = GetColumnPanel(section2, 2);

        // Column 1: Other Milestones / Stats
        AddSectionTitle(s2c1, "Other Milestones / Stats", "milestone.section_other");
        AddField(s2c1, "milestone.total_play_time", "^TIME");
        AddField(s2c1, "milestone.play_sessions", "^PLAY_SESSIONS");
        AddField(s2c1, "milestone.total_deaths", "^DEATHS");
        AddField(s2c1, "milestone.longest_life", "^LONGEST_LIFE");
        AddField(s2c1, "milestone.units_all_time", "^MONEY_EVER");
        AddField(s2c1, "milestone.nanites", "^NANITES");
        AddField(s2c1, "milestone.nanites_all_time", "^NANITES_EVER");
        AddField(s2c1, "milestone.ships_bought", "^SHIPS_BOUGHT");
        AddField(s2c1, "milestone.distance_jetpack", "^DIST_JETPACK");
        AddField(s2c1, "milestone.distance_flying", "^DIST_FLY");
        AddField(s2c1, "milestone.distance_exocraft", "^DIST_EXO");
        AddField(s2c1, "milestone.distance_pulse", "^DIST_PULSE");
        AddField(s2c1, "milestone.distance_submarine", "^DIST_SUB");
        AddField(s2c1, "milestone.distance_in_space", "^DIST_SPACE");

        // Column 2: Discoveries
        AddSectionTitle(s2c2, "");
        AddField(s2c2, "milestone.planets_discovered", "^DISC_PLANETS");
        AddField(s2c2, "milestone.systems_discovered", "^DISC_SYSTEMS");
        AddField(s2c2, "milestone.creatures_discovered", "^DISC_CREATURES");
        AddField(s2c2, "milestone.flora_discovered", "^DISC_FLORA");
        AddField(s2c2, "milestone.minerals_discovered", "^DISC_MINERALS");
        AddField(s2c2, "milestone.waypoints_discovered", "^DISC_WAYPOINTS");
        AddField(s2c2, "milestone.planets_visited", "^VISIT_PLANETS");
        AddField(s2c2, "milestone.creatures_fed", "^CREATURES_FED");
        AddField(s2c2, "milestone.creatures_killed", "^CREATURES_KILL");
        AddField(s2c2, "milestone.extreme_survival", "^EXTREME_WALK");
        AddField(s2c2, "milestone.storm_survival", "^STORM_WALK");
        AddField(s2c2, "milestone.cave_exploration", "^CAVE_WALK");
        AddField(s2c2, "milestone.time_in_space", "^SPACE_TIME");
        AddField(s2c2, "milestone.space_battles", "^SPACE_BATTLES");

        // Column 3: Activities
        AddSectionTitle(s2c3, "");
        AddField(s2c3, "milestone.fish_caught", "^FISH_CAUGHT");
        AddField(s2c3, "milestone.fish_released", "^FISH_RELEASED");
        AddField(s2c3, "milestone.bones_found", "^BONES_FOUND");
        AddField(s2c3, "milestone.fossils_made", "^FOS_MADE");
        AddField(s2c3, "milestone.salvage_looted", "^SALVAGE_LOOTED");
        AddField(s2c3, "milestone.ruins_looted", "^RUINS_LOOTED");
        AddField(s2c3, "milestone.bounties", "^BOUNTIES");
        AddField(s2c3, "milestone.gifts_given", "^GIFTS_GIVEN");
        AddField(s2c3, "milestone.parts_placed", "^PARTS_PLACED");
        AddField(s2c3, "milestone.base_parts_got", "^BASEPARTS_GOT");
        AddField(s2c3, "milestone.pets_adopted", "^PETS_ADOPTED");
        AddField(s2c3, "milestone.photo_mode_used", "^PHOTO_MODE_USED");
        AddField(s2c3, "milestone.portal_warps", "^PORTAL_WARPS");
        AddField(s2c3, "milestone.items_teleported", "^ITEMS_TELEPRT");

        scroll2.Controls.Add(section2);
        tab2.Controls.Add(scroll2);
        _tabControl.TabPages.Add(tab2);

        Controls.Add(_tabControl);

        ResumeLayout(false);
        PerformLayout();
    }

    private DoubleBufferedTabControl _tabControl = null!;
}
