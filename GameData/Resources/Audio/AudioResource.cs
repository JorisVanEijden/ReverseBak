namespace GameData.Resources.Audio;

public class AudioResource(string id) : IResource {
    public ResourceType Type { get => ResourceType.SND; }
    public string Id { get; } = id;
    public AudioType AudioType { get; set; }

    /// <summary>
    /// Whether this sound loops while it plays. From the per-sound <c>field_12_flag</c> bit 0x02
    /// (RE: audio_playSound_sub_seg067_D @0x35e6d sets struc_audio_378.is_looping from it). The intro
    /// theme has it set, so the original repeats it continuously — e.g. through the long credits scroll.
    /// </summary>
    public bool IsLooping { get; set; }

    public string Name { get; set; } = string.Empty;
    public Dictionary<byte, AudioDataResource> Variants { get; } = new();
}