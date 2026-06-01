namespace GameData.Resources.Animation.FrameCommands;

// TTM 0xC061 (also reached via 0x1311): stop playback of the sound/music with
// the given id and clear its looping-SFX flag. Verified in audio_stopSound
// (IDA @ 0x3609f): for music it stops and frees the entry, for a sound effect
// it halts the active channel. Distinct from 0xC041 (StopSound) which unloads
// the loaded sound resource.
public class StopSoundPlayback : FrameCommand {
    public int SoundId { get; set; }

    public override string ToString() {
        return $"{nameof(StopSoundPlayback)}({SoundId});";
    }
}
