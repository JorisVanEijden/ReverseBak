namespace ResourceExtractor.Resources;

using ResourceExtractor.Extractors;

public class AnimatorScript : IResource {
    public ResourceType Type { get => ResourceType.TTM; }
    public string Version { get; set; }
    public ushort NumberOfFrames { get; set; }
    public byte[] ScriptBytes { get; set; }
    public Dictionary<int, string> Tags { get; set; }
    public AnimationScript Script { get; set; }
    public string Name { get; set; }
}