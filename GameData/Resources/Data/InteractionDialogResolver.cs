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
        // Left-click (act): no container here -> examine.
        if (container == null) {
            return profile.ExamineDialogId;
        }
        // Actionable container type -> its own dialog if any, else the action dialog.
        if (profile.ActionableContainerTypes.Contains(container.ContainerType)) {
            uint? dialogId = container.DialogData?.DialogId;
            return dialogId is > 0 ? (int)dialogId.Value : profile.ActionDialogId;
        }
        return profile.NotActionableDialogId;
    }
}
