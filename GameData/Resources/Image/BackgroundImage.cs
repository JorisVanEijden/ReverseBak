namespace GameData.Resources.Image;

public class BackgroundImage(string id) : ImageResource(id) {
    public override ResourceType Type {
        get => ResourceType.SCX;
    }
}