namespace ResourceExtractor.Extractors.Container;

[Flags]
public enum ContainerDataType {
    Lock = 0x01,
    Dialog = 0x02,
    Shop = 0x04,
    Encounter = 0x08,
    Timestamp = 0x10,
    GlobalState = 0x20,
    HoldsProtectedItem = 0x40,
    SelfSpawn = 0x80
}