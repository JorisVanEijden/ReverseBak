namespace ResourceExtractor.Resources.Book;

using ResourceExtractor.Extractors;

public class TextSegment {
    public int Font { get; set; }
    public int YOffset { get; set; }
    public FontStyle FontStyle { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Color { get; set; }
}