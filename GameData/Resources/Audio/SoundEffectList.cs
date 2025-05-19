namespace GameData.Resources.Audio;

using GameData.Resources;

public class SoundEffectList : IResource, IHaveSubResource<SoundEffect> {
    public ResourceType Type { get => ResourceType.DAT; }
    public string Id { get; }
    public List<SoundEffect> SoundEffects { get; } = [];

    public SoundEffect GetSubResource(int index) {
        if (index < 0 || index >= SoundEffects.Count)
            throw new IndexOutOfRangeException($"SoundEffectList {Id} does not have index {index}");

        return SoundEffects[index];
    }
}

public class SoundEffect(string id) : IResource {
    public ResourceType Type { get; }
    public string Id { get; } = id;
    public Dictionary<int, List<byte[]>> SoundFormats { get; } = new();
}