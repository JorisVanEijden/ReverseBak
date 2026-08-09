namespace ResourceExtractor.Extractors;

using GameData;
using GameData.Resources.Object;

using System.Text;

internal class ObjectExtractor : ExtractorBase {
    public List<ObjectInfo> Extract(string filePath) {
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));

        var objectInfoList = new List<ObjectInfo>();
        for (int i = 0; i < 138; i++) {
            var objectInfo = new ObjectInfo(Path.GetFileName(filePath)) {
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
                MaxCharges = resourceReader.ReadByte(),               // +0x37 limited-use max charges
                Race = (Race)resourceReader.ReadUInt16(),
                ShopType = resourceReader.ReadUInt16(),
                ObjectType = (ObjectType)resourceReader.ReadUInt16(),
                EffectArgA = resourceReader.ReadUInt16(),               // +0x3E per-category effect arg A
                EffectArgB = resourceReader.ReadUInt16(),               // +0x40 per-category effect arg B
                UseEffectAmount = resourceReader.ReadUInt16(),        // +0x42 effect magnitude
                EffectDurationHours = resourceReader.ReadUInt16(),    // +0x44 timed-effect duration (hours)
                EquipAttributeMask = (ActorAttributeFlag)resourceReader.ReadUInt16(), // +0x46 passive-modifier attribute mask
                EquipModifierAmount = resourceReader.ReadInt16(),     // +0x48 passive-modifier amount (signed)
                DegradeChancePercent = resourceReader.ReadUInt16(),   // +0x4A
                MaxWearPerDegrade = resourceReader.ReadUInt16(),      // +0x4C
                MinimumQuality = resourceReader.ReadUInt16()          // +0x4E
            };

            objectInfoList.Add(objectInfo);
        }

        return objectInfoList;
    }
}