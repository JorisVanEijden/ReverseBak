namespace ResourceExtractor.Extractors.Container;

using GameData;

using System.Text;

public class ObjFixedExtractor : ExtractorBase {
    public List<Container> Extract(string filePath) {
        using FileStream resourceFile = File.OpenRead(filePath);
        using var reader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));

        var containerList = new List<Container>();

        while (reader.BaseStream.Position < reader.BaseStream.Length - 1) {
            ushort numberOfContainers = reader.ReadUInt16();
            for (int i = 0; i < numberOfContainers; i++) {
                byte zone = reader.ReadByte();
                byte minMaxChapter = reader.ReadByte();
                ushort worldItemId = reader.ReadUInt16();
                uint xPosition = reader.ReadUInt32();
                uint yPosition = reader.ReadUInt32();
                byte containerType = reader.ReadByte();
                byte numberOfItems = reader.ReadByte();
                byte capacity = reader.ReadByte();

                var container = new Container {
                    Zone = zone,
                    MinChapter = minMaxChapter >> 4,
                    MaxChapter = minMaxChapter & 0x0F,
                    WorldItemId = worldItemId,
                    XPosition = xPosition,
                    YPosition = yPosition,
                    ContainerType = (ContainerTypes)containerType,
                    NumberOfItems = numberOfItems,
                    Capacity = capacity
                };

                var dataTypes = (ContainerDataType)reader.ReadByte();

                container.Items = new List<InventoryItem>();
                for (int j = 0; j < capacity; j++) {
                    container.Items.Add(new InventoryItem {
                        ObjectId = reader.ReadByte(),
                        Quantity = reader.ReadByte(),
                        Flags = (ItemFlags)reader.ReadUInt16()
                    });
                }

                if (dataTypes.HasFlag(ContainerDataType.Lock)) {
                    container.LockData = new LockData {
                        Flags = (LockFlag)reader.ReadByte(),
                        Difficulty = reader.ReadByte(),
                        PuzzleChest = reader.ReadByte(),
                        TrapDamage = reader.ReadByte()
                    };
                }
                if (dataTypes.HasFlag(ContainerDataType.Dialog)) {
                    container.DialogData = new DialogData {
                        Field_0 = reader.ReadByte(),
                        Field_1 = reader.ReadByte(),
                        DialogId = reader.ReadUInt32()
                    };
                }
                if (dataTypes.HasFlag(ContainerDataType.Shop)) {
                    container.ShopData = new ShopData {
                        Field_0 = reader.ReadByte(),
                        MarkupPercentage = reader.ReadByte(),
                        Field_2 = reader.ReadByte(),
                        MarkDownPercentage = reader.ReadByte(),
                        Field_4 = reader.ReadByte(),
                        Field_5 = reader.ReadByte(),
                        BardingDifficulty = reader.ReadByte(),
                        BardingReward = reader.ReadByte(),
                        Field_8 = reader.ReadByte(),
                        Field_9 = reader.ReadByte(),
                        Field_A = reader.ReadByte(),
                        Field_B = reader.ReadByte(),
                        Field_C = reader.ReadByte(),
                        Field_D = reader.ReadByte(),
                        ShopItemCategories = (ShopItemCategories)reader.ReadUInt16()
                    };
                }
                if (dataTypes.HasFlag(ContainerDataType.Encounter)) {
                    container.EncounterData = new EncounterData {
                        GlobalDataKey1 = reader.ReadUInt16(),
                        GlobalDataKey2 = reader.ReadUInt16(),
                        GdsNumber = reader.ReadByte(),
                        GdsLetter = reader.ReadByte(),
                        Field_6 = reader.ReadByte() > 0,
                        X = reader.ReadByte(),
                        Y = reader.ReadByte()
                    };
                }
                if (dataTypes.HasFlag(ContainerDataType.Timestamp)) {
                    container.Timestamp = reader.ReadUInt32();
                }
                if (dataTypes.HasFlag(ContainerDataType.Unknown20)) {
                    container.Unknown20 = reader.ReadUInt16();
                }

                containerList.Add(container);
            }
        }
        return containerList;
    }
}

