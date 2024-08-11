namespace GameData.Resources.Image;

public class ImageSet : IResource {
    public ImageSet(string id) {
        Id = id;
    }

    public List<BmImage> Images { get; set; } = [];

    public ResourceType Type { get => ResourceType.BMX; }
    public string Id { get; }
}