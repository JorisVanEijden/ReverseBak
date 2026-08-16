namespace GameData.Resources.Location;

/// <summary>
/// The one-slot queue a teleport is handed off through — the port of the engine's
/// <c>teleportationData</c> global (0x3dc31), filled by <c>LoadTeleport_Dat</c> @0x4eba0 and drained
/// by two different consumers.
/// </summary>
/// <remarks>
/// <b>A destination is two independent things, and they are taken at different moments.</b> A row of
/// <c>TELEPORT.DAT</c> carries a GDS scene <i>and</i> a world location, and each half is consumed by
/// whichever loop gets there first:
/// <list type="bullet">
///   <item>the <b>scene</b> half is taken by the location loop (<c>GDS_RunScene</c> @0x4de9d), which
///   switches to that scene rather than closing — so a temple teleport lands you <i>inside</i> the
///   destination temple, still in the location loop;</item>
///   <item>the <b>world</b> half is taken later by <c>ProcessTeleportation</c> @0x4ebe7, once the
///   location loop has finally exited.</item>
/// </list>
///
/// <para>Both halves are real in the shipped data: all twelve temple rows carry a scene (rows 0-9
/// are GDS 70 letters 1-10, and the two in-town shrines are their towns' own scenes), while 18 of
/// the 40 rows carry none at all — those are the dialog teleports, the ladders and tunnels that only
/// move you. <b>Applying the world move straight away would be wrong for every temple teleport</b>,
/// dropping the party outside the destination instead of arriving inside it.</para>
///
/// <para>Each half is cleared as it is taken. That is what stops the scene switch from repeating
/// forever, and it is why a scene that queues a teleport of its own can be seen by the loop that
/// comes back around.</para>
/// </remarks>
public sealed class PendingTeleport {
    private TeleportDestination? _destination;

    /// <summary>Queues a destination, replacing anything already waiting.</summary>
    /// <remarks>
    /// Replacing rather than refusing is the original's behaviour: the slot is a single global, and
    /// the last writer before a drain wins. A scene that queues a teleport during another one is
    /// redirecting it, not stacking a second.
    /// </remarks>
    public void Queue(TeleportDestination? destination) {
        _destination = destination;
    }

    /// <summary>Whether anything is waiting at all.</summary>
    public bool HasAnything => _destination != null;

    /// <summary>Whether the waiting destination sends the location loop to another scene.</summary>
    public bool HasScene => _destination != null && ZoneTransition.RunsAScene(_destination.GdsNumber);

    /// <summary>
    /// Takes the scene half, clearing it so the switch happens once.
    /// </summary>
    /// <param name="number">The GDS scene to switch to.</param>
    /// <param name="letter">Which sub-scene of it.</param>
    /// <returns>False when nothing is waiting, leaving the outputs at zero.</returns>
    public bool TryTakeScene(out int number, out int letter) {
        number = 0;
        letter = 0;
        if (!HasScene) {
            return false;
        }

        number = _destination!.GdsNumber;
        letter = _destination.GdsLetter;

        // Cleared, not consumed whole: the world half is still waiting for ProcessTeleportation.
        _destination = new TeleportDestination {
            Location = _destination.Location,
            GdsNumber = 0,
            GdsLetter = 0,
            Id = _destination.Id,
        };

        return true;
    }

    /// <summary>
    /// Takes the world half — where the party ends up — and empties the slot.
    /// </summary>
    /// <returns>Null when nothing is waiting.</returns>
    public Location? TakeLocation() {
        Location? location = _destination?.Location;
        _destination = null;
        return location;
    }

    /// <summary>Empties the slot without acting on it.</summary>
    public void Clear() {
        _destination = null;
    }
}
