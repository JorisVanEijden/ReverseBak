namespace ResourceExtractor.Extractors.Container;

[Flags]
public enum ContainerDataType {
    Lock = 0x01,
    Dialog = 0x02,
    Shop = 0x04,
    Encounter = 0x08,
    Timestamp = 0x10,
    Unknown20 = 0x20,
    Flag1 = 0x40,
    Flag2 = 0x80
}