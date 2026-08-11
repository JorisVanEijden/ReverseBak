namespace GameData.Resources.Animation;

public record SceneTag(int Number, string? Name, bool UnknownBool) {
    public bool UnknownBool { get; } = UnknownBool;
    public string? Name { get; } = Name;
    public int Number { get; } = Number;
};