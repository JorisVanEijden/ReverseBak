namespace GameData.Resources.Data;

using System.Linq;

/// <summary>
/// Profile-driven port of a DOS container-interaction handler's dialog selection (the shared
/// shape of handle_Corpse @0x76a0a, handle_Well @0x78b7e, …). Given the interaction profile,
/// the container resolved at the clicked location (or null), and which mouse button was used,
/// returns the DDX dialog id to show.
/// </summary>
public static class InteractionDialogResolver {
    public static int Resolve(InteractionProfile profile, SaveGameContainerData? container, bool isPrimary) {
        // Right-click (examine) is always the examine dialog.
        if (!isPrimary) {
            return profile.ExamineDialogId;
        }
        // Left-click (act) with no container here: the same answer as a container of the wrong
        // type. Every handler jumps to its not-actionable label from the null test and from the
        // type test alike — handle_Corpse @0x76a98/@0x76aa9, handle_Bag @0x7696d/@0x76977,
        // handle_Well @0x78be6/@0x78bf0 — so the two cases are one case.
        //
        // This used to return the EXAMINE dialog, which is a different string in every handler
        // and simply is not what the original shows: a corpse whose container has been looted
        // away answered "It's a body, we might want to look it over" instead of "@0 shrugged.
        // This must not be very important." The difference is invisible while only Corpse and
        // Container are wired, because a chest resolves its dialogs in the handler's lock branch
        // and never reaches here.
        if (container == null) {
            return profile.NotActionableDialogId;
        }
        // Actionable container type -> its own dialog if any, else the action dialog.
        if (profile.ActionableContainerTypes.Contains(container.ContainerType)) {
            uint? dialogId = container.DialogData?.DialogId;
            return dialogId is > 0 ? (int)dialogId.Value : profile.ActionDialogId;
        }
        return profile.NotActionableDialogId;
    }
}
