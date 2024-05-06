namespace ResourceExtraction.Extractors;

using GameData.Resources.Book;

using System;
using System.IO;
using System.Text;

public class BokExtractor : ExtractorBase<BookResource> {
    private const byte EndOfPage = 0xF0;
    private const byte StartOfParagraph = 0xF1;
    private const byte StartOfTextSegment = 0xF4;
    private const int UpperCharacterLimit = 0xB1;

    public override BookResource Extract(string id, Stream resourceStream) {
        using var resourceReader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));

        var book = new BookResource(id);

        int dataSize = resourceReader.ReadInt32();
        int nrOfPages = resourceReader.ReadUInt16();
        var pageOffsets = new int[nrOfPages];
        for (var i = 0; i < nrOfPages; i++) {
            pageOffsets[i] = resourceReader.ReadInt32() + 4;
        }
        foreach (int pageOffset in pageOffsets) {
            Log($"Reading page at offset {pageOffset}");
            resourceReader.BaseStream.Seek(pageOffset, SeekOrigin.Begin);
            Page page = ExtractPage(resourceReader);
            book.Pages.Add(page);
        }

        return book;
    }

    private static Page ExtractPage(BinaryReader resourceReader) {
        Page page = InitializePage(resourceReader);
        AddReservedAreasToPage(resourceReader, page);
        AddImagesToPage(resourceReader, page);
        AddParagraphsWithTextSegmentsToPage(resourceReader, page);

        return page;
    }

    private static void AddParagraphsWithTextSegmentsToPage(BinaryReader resourceReader, Page page) {
        var pageDone = false;
        Paragraph? currentParagraph = null;
        while (!pageDone) {
            byte type = resourceReader.ReadByte();
            switch (type) {
                case EndOfPage:
                    {
                        pageDone = true;

                        break;
                    }
                case StartOfParagraph:
                    {
                        if (currentParagraph != null) {
                            page.Paragraphs.Add(currentParagraph);
                        }
                        currentParagraph = StartNewParagraph(resourceReader);

                        break;
                    }
                case StartOfTextSegment:
                    {
                        TextSegment textSegment = StartNewTextSegment(resourceReader);
                        currentParagraph?.TextSegments.Add(textSegment);

                        break;
                    }
                default:
                    throw new InvalidOperationException($"Unknown type {type:X2}");
            }
        }
        // Add the last paragraph if it exists
        if (currentParagraph != null) {
            page.Paragraphs.Add(currentParagraph);
        }
    }

    private static TextSegment StartNewTextSegment(BinaryReader resourceReader) {
        var textSegment = new TextSegment {
            Font = resourceReader.ReadUInt16(),
            YOffset = resourceReader.ReadInt16(),
            Color = resourceReader.ReadUInt16()
        };
        _ = resourceReader.ReadUInt16(); // Always 0. Game uses a byte from this field, but it's not clear what for. Messing with it doesn't seem to change anything.
        textSegment.FontStyle = (FontStyle)resourceReader.ReadUInt16();
        var sb = new StringBuilder();
        while (resourceReader.PeekChar() < UpperCharacterLimit) {
            sb.Append(resourceReader.ReadChar());
        }
        textSegment.Text = sb.ToString();

        return textSegment;
    }

    private static Paragraph StartNewParagraph(BinaryReader resourceReader) {
        var paragraph = new Paragraph {
            XOffset = resourceReader.ReadInt16(),
            Width = resourceReader.ReadInt16(),
            LineSpacing = resourceReader.ReadInt16(),
            WordSpacing = resourceReader.ReadInt16(),
            StartIndent = resourceReader.ReadInt16()
        };
        _ = resourceReader.ReadInt16(); // Always 0. Code seems to indicate something with horizontal spacing but messing with it doesn't seem to change anything.
        paragraph.YOffset = resourceReader.ReadInt16();
        paragraph.Alignment = (TextAlignment)resourceReader.ReadInt16();

        return paragraph;
    }

    private static void AddImagesToPage(BinaryReader resourceReader, Page page) {
        for (var i = 0; i < page.NumberOfImages; i++) {
            page.Images.Add(new BookImage {
                X = resourceReader.ReadInt16(),
                Y = resourceReader.ReadInt16(),
                ImageNumber = resourceReader.ReadInt16(),
                Mirroring = (Mirroring)resourceReader.ReadInt16()
            });
        }
    }

    private static void AddReservedAreasToPage(BinaryReader resourceReader, Page page) {
        for (var i = 0; i < page.NumberOfReservedAreas; i++) {
            page.ReservedAreas.Add(new ReservedArea {
                X = resourceReader.ReadInt16(),
                Y = resourceReader.ReadInt16(),
                Width = resourceReader.ReadInt16(),
                Height = resourceReader.ReadInt16()
            });
        }
    }

    private static Page InitializePage(BinaryReader resourceReader) {
        var page = new Page {
            XOffset = resourceReader.ReadInt16(),
            YOffset = resourceReader.ReadInt16(),
            Width = resourceReader.ReadInt16(),
            Height = resourceReader.ReadInt16(),
            PageDisplayNumber = resourceReader.ReadUInt16(), // When 469 or 476, the song is changed
            PageNumber = resourceReader.ReadInt16(),
            PreviousPageNumber = resourceReader.ReadInt16(),
            NextPageNumber = resourceReader.ReadInt16(),
            NextPagePointer = resourceReader.ReadInt16()
        };
        _ = resourceReader.ReadUInt16(); // Always 0 in data, code seems to do something with how paragraphs are spread over the pages 
        page.NumberOfImages = resourceReader.ReadUInt16();
        page.NumberOfReservedAreas = resourceReader.ReadUInt16();
        page.ShowPageNumber = resourceReader.ReadUInt16() > 0;

        _ = resourceReader.ReadUInt32(); // Placeholder for pointer
        _ = resourceReader.ReadBytes(16); // Placeholder for paragraph data
        _ = resourceReader.ReadBytes(10); // Placeholder for text segment data

        return page;
    }
}