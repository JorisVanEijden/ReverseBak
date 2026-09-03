namespace GameData.Resources.Dialog.Actions;

/// <summary>
/// Action type 5 — <b>a PRELOAD of up to four actor portraits, not a change of face.</b>
/// </summary>
/// <remarks>
/// DIALOG.C's <c>case 5</c> walks <c>nA1..nA4</c>, stops at the first zero, and calls
/// <c>askabout_actor_spr_cache_get(id, 0)</c> for each — <b>discarding the return value</b>. That
/// function (ASKABOUT.C:100) is a six-slot sprite cache: it answers the slot if the actor is already
/// there, and otherwise loads <c>ACT###.BMP</c> and <c>ACT###.PAL</c> into a free one. Calling it
/// for the side effect alone is a warm-up, and the name this port gave the action describes
/// something it does not do.
///
/// <para><b>Deliberately not ported, and that is the rule rather than a gap.</b> The drawing path
/// calls the same function to get its slot (ASKABOUT.C:149), so a portrait renders identically with
/// or without the preload — the only difference is when the load happens. Reproducing it would be
/// porting the 1993 engine's memory model, which this project's guidance forbids in as many words:
/// port the DATA and the GAME LOGIC, never the engine architecture. Our portraits come through
/// <c>IResourceCache</c>, which memoizes.</para>
///
/// <para>24 instances ship. See <see cref="DisposeActorFacesAction"/> for the other half of the
/// pair, and TASK-313 for why they are counted apart from the actions that DO something.</para>
/// </remarks>
public class ChangeFaceAction : DialogActionBase {
    public int Actor1 { get; set; }
    public int Actor2 { get; set; }
    public int Actor3 { get; set; }
    public int Actor4 { get; set; }
}
