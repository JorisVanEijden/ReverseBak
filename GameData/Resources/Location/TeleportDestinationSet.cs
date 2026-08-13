namespace GameData.Resources.Location;

using GameData;
using System.Collections.Generic;

/// <summary>
/// <c>TELEPORT.DAT</c> — the forty destinations the game can put the party at.
///
/// <para>Two consumers share this one table and do not overlap: <b>ids 0-11 are the temple nodes</b>
/// the rift-map screen offers, and they are exactly the twelve that carry a GDS scene reference;
/// <b>ids 12-39 are what dialog <c>Teleport</c> actions name</b> for ladders, tunnels and scripted
/// moves. So a destination id out of a dialog is not a temple, and the teleport screen's twelve
/// buttons are the first twelve rows rather than the whole file.</para>
/// </summary>
public class TeleportDestinationSet : IResource {
    public TeleportDestinationSet(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;

    /// <summary>The destinations, in file order; each one's <c>Id</c> is its index.</summary>
    public List<TeleportDestination> Destinations { get; set; } = new List<TeleportDestination>();

    /// <summary>Highest id the temple screen offers; above this the destinations are dialog-only.</summary>
    public const int LastTempleDestinationId = 11;

    /// <summary>A destination by id, or null when the id is outside the table.</summary>
    public TeleportDestination? ById(int id) =>
        id >= 0 && id < Destinations.Count ? Destinations[id] : null;
}
