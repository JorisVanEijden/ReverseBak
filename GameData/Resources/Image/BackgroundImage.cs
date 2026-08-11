namespace GameData.Resources.Image;

public class BackgroundImage(string id) : ImageResource(id) {
    public override ResourceType Type {
        get => ResourceType.SCX;
    }

    public override double ScaleX {
        get => 1.0;
    }

    public override double ScaleY {
        get => 1.0;
    }
}