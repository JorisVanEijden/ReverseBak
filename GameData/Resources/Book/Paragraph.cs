namespace GameData.Resources.Book;

public class Paragraph {
    public int XOffset { get; set; }
    public int Width { get; set; }
    public int LineSpacing { get; set; }
    public int WordSpacing { get; set; }
    public int StartIndent { get; set; }
    public int YOffset { get; set; }
    public TextAlignment Alignment { get; set; }
    public List<TextSegment> TextSegments { get; set; } = [];
}