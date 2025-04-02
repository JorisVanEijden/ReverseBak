namespace GameData.Resources.Audio;

using GameData.Resources;

public class SoundEffect : IResource {
    public ResourceType Type { get => ResourceType.SND; }
    public string Id { get; }
}