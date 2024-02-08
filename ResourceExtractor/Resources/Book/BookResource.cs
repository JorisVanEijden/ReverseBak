namespace ResourceExtractor.Resources.Book;

using ResourceExtractor.Extractors;

public class BookResource : IResource {
    public ResourceType Type { get => ResourceType.BOK; }
    public List<Page> Pages { get; set; } = [];
}