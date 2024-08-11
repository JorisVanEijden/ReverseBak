namespace GameData.Resources.Palette;

public class PaletteResource : IResource {
    public PaletteResource(string id) {
        Id = id;
        // Default palette is grayscale
        Colors = new Color[256];
        for (var i = 0; i < 255; i++) {
            var grey = (byte)i;
            Colors[i] = new Color(grey, grey, grey);
        }
    }

    public Color[] Colors { get; set; }

    public ResourceType Type {
        get => ResourceType.PAL;
    }

    public string Id { get; }
}