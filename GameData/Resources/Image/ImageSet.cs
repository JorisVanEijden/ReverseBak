namespace GameData.Resources.Image;

public class ImageSet(string id) : IResource, IHaveSubResource<BmImage> {
    public List<BmImage> Images { get; set; } = [];

    public ResourceType Type { get => ResourceType.BMX; }
    public string Id { get; } = id;

    public BmImage GetSubResource(int index) {
        if (index < 0 || index >= Images.Count)
            throw new IndexOutOfRangeException($"Image {Id} does not have index {index}");
        return Images[index];
    }
}