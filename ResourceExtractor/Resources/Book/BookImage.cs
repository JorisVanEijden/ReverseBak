namespace ResourceExtractor.Resources.Book;

using ResourceExtractor.Extractors;

public class BookImage {
    public int X { get; set; }
    public int Y { get; set; }

    /// <summary>
    /// Refers to images from BOOK.BMX
    /// </summary>
    public int ImageNumber { get; set; }

    public Mirroring Mirroring { get; set; }
}