namespace GameData.Resources;

public interface IResource {
    ResourceType Type { get; }
    string Id { get; }
}