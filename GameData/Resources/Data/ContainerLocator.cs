namespace GameData.Resources.Data;

/// <summary>
/// Faithful port of the DOS engine's GetContainerAtLocation (KRONDOR.EXE 0x5ac4a): finds the
/// container placed at an exact world location for the current chapter. Match rule: same zone,
/// exact fine X and Y, and current chapter within the container's [MinChapter, MaxChapter].
/// <para>The engine reads <b>two sources in order</b> and so does this: the save's own
/// zone-container section first, then OBJFIXED.DAT. The save SHADOWS the shipped file, so a
/// placement the player has changed wins, and everything untouched — which is most of the world —
/// still resolves from the shipped copy. Consulting only the save finds almost nothing.</para>
/// </summary>
public static class ContainerLocator {
    /// <summary>
    /// The placement at a location, checking the save first and the shipped file second — the full
    /// two-pass lookup <c>actorspawn_objfixed</c> performs.
    /// </summary>
    /// <param name="shipped">
    /// OBJFIXED.DAT. Null falls back to save-only behaviour, which is what every caller got before
    /// this source existed.
    /// </param>
    public static SaveGameContainerData? FindContainerAtLocation(
        SaveGameZoneContainerStateData state, FixedObjectSet? shipped,
        int zone, int x, int y, int chapter) =>
        FindContainerAtLocation(state, zone, x, y, chapter)
        ?? shipped?.FindAtLocation(zone, x, y, chapter);

    public static SaveGameContainerData? FindContainerAtLocation(
        SaveGameZoneContainerStateData state, int zone, int x, int y, int chapter) {
        if (state == null) {
            return null;
        }
        foreach (SaveGameZoneContainerEntryData entry in state.Zones) {
            if (entry.ZoneNumber != zone) {
                continue;
            }
            foreach (SaveGameContainerData container in entry.Containers) {
                SaveGameContainerLocationData loc = container.Location;
                if (loc.Zone == zone && loc.X == x && loc.Y == y
                    && chapter >= loc.MinChapter && chapter <= loc.MaxChapter) {
                    return container;
                }
            }
        }
        return null;
    }
}
