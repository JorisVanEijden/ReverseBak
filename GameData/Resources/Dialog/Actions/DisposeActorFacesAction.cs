namespace GameData.Resources.Dialog.Actions;

/// <summary>
/// Action type 15. No payload; frees the cached actor-face bitmaps used by
/// <c>DrawDialogBubble</c> / <c>ShowKeywordDialog</c>. The 8 payload bytes
/// in the DDX file are unused by the engine (`call j_disposeActorFaces` at
/// 0x4a20a in <c>ExecuteDialog</c>).
/// </summary>
public class DisposeActorFacesAction : DialogActionBase {
}
