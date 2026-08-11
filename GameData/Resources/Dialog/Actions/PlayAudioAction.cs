namespace GameData.Resources.Dialog.Actions;

public class PlayAudioAction : DialogActionBase {
    // Source id: < 1000 = sound effect, >= 1000 = song (the dialog handler branches on this).
    public int AudioId { get; set; }

    // Which of ExecuteDialog's three action passes plays the audio — see PlayAudioTiming.
    public PlayAudioTiming Timing { get; set; }
}