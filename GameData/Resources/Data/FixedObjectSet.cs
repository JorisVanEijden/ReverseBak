namespace GameData.Resources.Data;

using System.Collections.Generic;

/// <summary>
/// <c>OBJFIXED.DAT</c> — the shipped placement of every fixed world object, and the <b>second</b> of
/// the engine's two sources for them.
///
/// <para><c>actorspawn_objfixed</c> looks a placement up in two passes: the save's own copy in
/// <c>TEMP.GAM</c> first, then this file. The save shadows it, so this is the pristine fallback for
/// every object the player has never touched — which is most of them. A lookup that consults only
/// the save finds almost nothing.</para>
///
/// <para>The records are <b>the same layout the save uses</b>, so they parse into the same
/// <see cref="SaveGameContainerData"/> rather than a parallel model.</para>
/// </summary>
public class FixedObjectSet : IResource {
    public FixedObjectSet(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;

    /// <summary>Every shipped placement, in file order.</summary>
    public List<SaveGameContainerData> Containers { get; set; } = new List<SaveGameContainerData>();

    /// <summary>
    /// The shipped placement at an exact location for a chapter, or null — the same match rule
    /// <see cref="ContainerLocator"/> applies to the save: exact fine x/y, never nearest, and the
    /// chapter inside the record's own two-nibble band.
    /// </summary>
    public SaveGameContainerData? FindAtLocation(int zone, int x, int y, int chapter) {
        foreach (SaveGameContainerData container in Containers) {
            SaveGameContainerLocationData loc = container.Location;
            if (loc.Zone == zone && loc.X == x && loc.Y == y
                && chapter >= loc.MinChapter && chapter <= loc.MaxChapter) {
                return container;
            }
        }
        return null;
    }
}
