namespace GameData.Resources.Data;

/// <summary>
/// Faithful port of the DOS engine's GetContainerAtLocation (KRONDOR.EXE 0x5ac4a): finds the
/// container placed at an exact world location for the current chapter. Match rule: same zone,
/// exact fine X and Y, and current chapter within the container's [MinChapter, MaxChapter].
/// Operates over the save/TEMP.GAM zone-container section; OBJFIXED.DAT (the engine's second
/// source) is not yet included here.
/// </summary>
public static class ContainerLocator {
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
