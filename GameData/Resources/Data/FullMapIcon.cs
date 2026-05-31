namespace GameData.Resources.Data;

/// <summary>
/// Where to draw the party's marker on the full world map at chapter start.
/// Coordinates are percentages (0..100) of the map image so downstream consumers
/// stay resolution-independent (see memory: project-aspect-correction).
///
/// In the original game these positions are not stored in any data file — they are
/// hard-coded per chapter in <c>StartGameOrLoadSave</c> (KRONDOR.EXE 0x20835) as
/// pixel coordinates on the 320x200 FULLMAP.SCX. Chapter 8 sets no position, so its
/// map shows without a marker (<see cref="Visible"/> = false).
/// </summary>
public sealed class FullMapIcon {
    public FullMapIcon(bool visible, float xPercent, float yPercent, int iconIndex) {
        Visible = visible;
        XPercent = xPercent;
        YPercent = yPercent;
        IconIndex = iconIndex;
    }

    /// <summary>False when the chapter shows the world map without a party marker.</summary>
    public bool Visible { get; }

    /// <summary>Marker centre as a percentage (0..100) of the map width.</summary>
    public float XPercent { get; }

    /// <summary>Marker centre as a percentage (0..100) of the map height.</summary>
    public float YPercent { get; }

    /// <summary>Sprite index into the map-icon image set (fmap_icn.bmp).</summary>
    public int IconIndex { get; }
}
