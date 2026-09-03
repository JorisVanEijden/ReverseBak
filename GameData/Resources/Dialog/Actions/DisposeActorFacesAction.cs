namespace GameData.Resources.Dialog.Actions;

/// <summary>
/// Action type 15 — <b>frees the six-slot actor-portrait cache.</b> No payload; the 8 bytes in the
/// DDX record are unused (<c>call j_disposeActorFaces</c> at 0x4a20a in <c>ExecuteDialog</c>).
/// </summary>
/// <remarks>
/// <b>Deliberately not ported.</b> <c>askabout_free_paged_image_table</c> releases the cache that
/// <see cref="ChangeFaceAction"/> warms — memory management for a 640K machine, not game behaviour.
/// Nothing a player can see depends on it: the drawing path reloads whatever it needs.
///
/// <para>Reproducing it would be actively worse than ignoring it. Our portraits live in
/// <c>IResourceCache</c>, which memoizes across the session; "freeing" them on a dialog's say-so
/// would throw away sprites the next dialog is about to ask for again.</para>
///
/// <para><b>286 instances ship — the single most common non-null action</b>, which is why this
/// matters to say plainly. A coverage count that treats them as missing work reports a 310-instance
/// hole (with <see cref="ChangeFaceAction"/>) that nobody should ever fill. See TASK-313.</para>
/// </remarks>
public class DisposeActorFacesAction : DialogActionBase {
}
