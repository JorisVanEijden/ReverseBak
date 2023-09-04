namespace ResourceExtractor.Resources;

using ResourceExtractor.Extractors;

public class AnimationResource : IResource {
    public string Version { get; set; }
    public ushort Unknown0 { get; set; }
    public ushort Unknown1 { get; set; }
    public string ResourceFileName { get; set; }
    public byte[] ScriptBytes { get; set; }
    public ResourceType Type { get => ResourceType.ADS; }
}