namespace ResourceExtractor.Extractors;

using GameData;

using ResourceExtractor.Resources.Object;

using System.Text;

internal class ObjectExtractor : ExtractorBase {
    public List<ObjectInfo> Extract(string filePath) {
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));

        var objectInfoList = new List<ObjectInfo>();
        for (int i = 0; i < 138; i++) {
            var objectInfo = new ObjectInfo {
                Number = i,
                Name = new string(resourceReader.ReadChars(30)).Replace('\0', ' ').Trim(),
                Field1E = resourceReader.ReadUInt16(),
                Flags = (ObjectFlags)resourceReader.ReadUInt16(),
                WordWrap = resourceReader.ReadUInt16(),
                ChapterNumber = resourceReader.ReadUInt16(),
                Price = resourceReader.ReadUInt16(),
                SwingBaseDamage = resourceReader.ReadInt16(),
                ThrustBaseDamage = resourceReader.ReadInt16(),
                SwingAccuracy_ArmorMod_BowAccuracy = resourceReader.ReadInt16(),
                ThrustAccuracy = resourceReader.ReadInt16(),
                Icon = resourceReader.ReadUInt16(),
                InventorySlots = resourceReader.ReadUInt16(),
                SoundId = resourceReader.ReadByte(),
                SoundRepeat = resourceReader.ReadByte(),
                MaxAmount = resourceReader.ReadByte(),
                Field37 = resourceReader.ReadByte(),
                Race = (Race)resourceReader.ReadUInt16(),
                ShopType = resourceReader.ReadUInt16(),
                ObjectType = (ObjectType)resourceReader.ReadUInt16(),
                Attributes = (ActorAttributeFlag)resourceReader.ReadUInt16(),
                Field40 = resourceReader.ReadUInt16(),
                Field42 = resourceReader.ReadUInt16(),
                Book1Potion8 = resourceReader.ReadUInt16(),
                CanEffect = resourceReader.ReadInt16(),
                Field48 = resourceReader.ReadUInt16(),
                Field4A = resourceReader.ReadUInt16(),
                Field4C = resourceReader.ReadUInt16(),
                Field4E = resourceReader.ReadUInt16()
            };

            objectInfoList.Add(objectInfo);
        }

        return objectInfoList;
    }
}