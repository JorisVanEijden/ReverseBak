namespace GameData.Resources.Image;

[Flags]
public enum ImageFlags {
    VerticalFlip = 0x01,
    HorizontalFlip = 0x02,
    Unknown4 = 0x04,
    Unknown8 = 0x08,
    Unknown16 = 0x10,
    ReversedRowColumn = 0x20,
    Unknown64 = 0x40,
    Compressed = 0x80
}