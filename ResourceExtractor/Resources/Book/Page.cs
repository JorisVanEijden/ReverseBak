namespace ResourceExtractor.Resources.Book;

using ResourceExtractor.Extractors;

public class Page {
    public ushort PageDisplayNumber { get; set; }
    public int XOffset { get; set; }
    public int YOffset { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int PageNumber { get; set; }
    public int PreviousPageNumber { get; set; }
    public int NextPageNumber { get; set; }
    public int NextPagePointer { get; set; }
    public int NumberOfImages { get; set; }
    public int NumberOfReservedAreas { get; set; }
    public bool ShowPageNumber { get; set; }
    public List<ReservedArea> ReservedAreas { get; set; } = [];
    public List<BookImage> Images { get; set; } = [];
    public List<Paragraph> Paragraphs { get; set; } = [];
}