namespace GameData.Resources.Book;

public class BookResource : IResource {
    public ResourceType Type { get => ResourceType.BOK; }
    public List<Page> Pages { get; set; } = [];
}