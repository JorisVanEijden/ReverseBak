namespace GameData.Resources.Book;

public class BookResource : IResource {
    public BookResource(string id) {
        Id = id;
    }

    public ResourceType Type { get => ResourceType.BOK; }
    public string Id { get; }
    public List<Page> Pages { get; set; } = [];
}