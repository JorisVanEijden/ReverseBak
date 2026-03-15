namespace GameData.Resources.Audio;

using GameData.Resources;

public class AudioResourceList : IResource, IHaveSubResource<AudioResource> {
    public ResourceType Type { get => ResourceType.DAT; }
    public string Id { get; }
    public List<AudioResource> AudioResources { get; } = [];

    public AudioResource GetSubResource(int index) {
        if (index < 0 || index >= AudioResources.Count)
            throw new IndexOutOfRangeException($"SoundEffectList {Id} does not have index {index}");

        return AudioResources[index];
    }
}