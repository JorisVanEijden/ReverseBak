namespace GameData.Resources.Image;

public class ImageSet : IResource {
    public ImageSet(string id) {
        Id = id;
    }

    public ResourceType Type { get => ResourceType.BMX; }
    public string Id { get; }
    
    public List<BmImage> Images { get; set; } = [];
}