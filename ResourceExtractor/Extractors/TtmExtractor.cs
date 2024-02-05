namespace ResourceExtractor.Extractors;

using ResourceExtractor.Compression;
using ResourceExtractor.Extensions;
using ResourceExtractor.Resources.Animation;

using System.Text;
using System.Text.Json.Serialization;

internal class TtmExtractor : ExtractorBase {
    public static AnimatorScript Extract(string filePath) {
        Log($"Extracting {filePath}");
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));

        var animator = new AnimatorScript {
            Name = Path.GetFileNameWithoutExtension(filePath)
        };
        string tag = ReadTag(resourceReader);
        if (tag != "VER") {
            throw new InvalidDataException($"Expected VER tag, got {tag}");
        }
        int verSize = resourceReader.ReadUInt16();
        if (resourceReader.ReadUInt16() != 0x0000) {
            throw new InterestingDataException("UInt16 after size is not 0x0000 in VER tag");
        }
        animator.Version = resourceReader.ReadZeroTerminatedString();
        tag = ReadTag(resourceReader);
        if (tag != "PAG") {
            throw new InvalidDataException($"Expected PAG tag, got {tag}");
        }
        int pagSize = resourceReader.ReadUInt16();
        if (resourceReader.ReadUInt16() != 0x0000) {
            throw new InterestingDataException("UInt16 after size is not 0x0000 in PAG tag");
        }
        animator.NumberOfFrames = resourceReader.ReadUInt16();
        tag = ReadTag(resourceReader);
        if (tag != "TT3") {
            throw new InvalidDataException($"Expected TT3 tag, got {tag}");
        }
        ushort tt3Size = resourceReader.ReadUInt16();
        if (resourceReader.ReadUInt16() != 0x0000) {
            throw new InterestingDataException("UInt16 after size is not 0x0000 in TT3 tag");
        }
        long endPosition = resourceReader.BaseStream.Position + tt3Size;
        var compressionType = (CompressionType)resourceReader.ReadByte();
        int decompressedSize = (int)resourceReader.ReadUInt32();
        ICompression compression = CompressionFactory.Create(compressionType);
        Stream scriptStream = compression.Decompress(resourceReader.BaseStream, endPosition);
        if (scriptStream.Length != decompressedSize) {
            throw new InvalidOperationException($"Decompressed TT3 size {scriptStream.Length} does not match expected size {decompressedSize} in {filePath}");
        }
        byte[] scriptBytes = new byte[scriptStream.Length];
        scriptStream.ReadExactly(scriptBytes);

        tag = ReadTag(resourceReader);
        if (tag != "TTI") {
            throw new InvalidDataException($"Expected TTI tag, got {tag}");
        }
        ushort ttiSize = resourceReader.ReadUInt16();
        if (resourceReader.ReadUInt16() != 0x8000) {
            throw new InterestingDataException("UInt16 after size is not 0x8000 in TTI tag");
        }
        tag = ReadTag(resourceReader);
        if (tag != "TAG") {
            throw new InvalidDataException($"Expected TAG tag, got {tag}");
        }
        ushort tagSize = resourceReader.ReadUInt16();
        if (resourceReader.ReadUInt16() != 0x0000) {
            throw new InterestingDataException("UInt16 after size is not 0x0000 in TAG tag");
        }
        ushort numberOfTags = resourceReader.ReadUInt16();
        animator.Tags = new Dictionary<int, string>(numberOfTags);
        for (int i = 0; i < numberOfTags; i++) {
            animator.Tags[resourceReader.ReadUInt16()] = resourceReader.ReadZeroTerminatedString();
        }

        animator.Script = DecodeScript(scriptBytes, animator.Tags);

        return animator;
    }

    private static AnimationScript DecodeScript(byte[] scriptBytes, Dictionary<int, string> animatorTags) {
        var resultScript = new AnimationScript();
        using var scriptStream = new MemoryStream(scriptBytes);
        using var scriptReader = new BinaryReader(scriptStream, Encoding.GetEncoding(DosCodePage));
        var pageCommands = new List<AnimatorCommand>();
        while (scriptStream.Position < scriptStream.Length) {
            AnimatorCommand command;
            ushort type = scriptReader.ReadUInt16();
            switch (type) {
                case 0x0020:
                    command = new StoreBackgroundCommand();
                    break;
                case 0x0070:
                    command = new UnknownCommand0070();
                    break;
                case 0x0080:
                    command = new UnknownCommand0080();
                    break;
                case 0x00C0:
                    command = new UnknownCommand00C0();
                    break;
                case 0x0110:
                    command = new UnknownCommand0110();
                    break;
                case 0x0400:
                    command = new UnknownCommand0400();
                    break;
                case 0x0500:
                    command = new UnknownCommand0500();
                    break;
                case 0x0510:
                    command = new UnknownCommand0510();
                    break;
                case 0x0FF0:
                    resultScript.Pages.Add(pageCommands);
                    pageCommands = new List<AnimatorCommand>();
                    continue;
                case 0x1021:
                    command = new UnknownCommand1021(scriptReader);
                    break;
                case 0x1051:
                    command = new SelectImageCommand(scriptReader);
                    break;
                case 0x1061:
                    command = new SelectPaletteCommand(scriptReader);
                    break;
                case 0x1071:
                    command = new SelectFontCommand(scriptReader);
                    break;
                case 0x1101:
                    command = new UnknownCommand1101(scriptReader);
                    break;
                case 0x1111:
                    command = new TagCommand(scriptReader, animatorTags);
                    break;
                case 0x1121:
                    command = new UnknownCommand1121(scriptReader);
                    break;
                case 0x1201:
                    command = new UnknownCommand1201(scriptReader);
                    break;
                case 0x2002:
                    command = new UnknownCommand2002(scriptReader);
                    break;
                case 0x2012:
                    command = new UnknownCommand2012(scriptReader);
                    break;
                case 0x2302:
                    command = new UnknownCommand2302(scriptReader);
                    break;
                case 0x2312:
                    command = new UnknownCommand2312(scriptReader);
                    break;
                case 0x2322:
                    command = new UnknownCommand2322(scriptReader);
                    break;
                case 0x2402:
                    command = new UnknownCommand2402(scriptReader);
                    break;
                case 0x4004:
                    command = new UnknownCommand4004(scriptReader);
                    break;
                case 0x4114:
                    command = new FadeOutCommand(scriptReader);
                    break;
                case 0x4124:
                    command = new FadeInCommand(scriptReader);
                    break;
                case 0x4204:
                    command = new StoreAreaCommand(scriptReader);
                    break;
                case 0x4214:
                    command = new UnknownCommand4214(scriptReader);
                    break;
                case 0xA014:
                    command = new UnknownCommandA014(scriptReader);
                    break;
                case 0xA034:
                    command = new UnknownCommandA034(scriptReader);
                    break;
                case 0xA094:
                    command = new UnknownCommandA094(scriptReader);
                    break;
                case 0xA0B5:
                    command = new UnknownCommandA0B5(scriptReader);
                    break;
                case 0xA104:
                    command = new ClearAreaCommand(scriptReader);
                    break;
                case 0xA114:
                    command = new UnknownCommandA114(scriptReader);
                    break;
                case 0xA504:
                    command = new DrawImageCommand(scriptReader);
                    break;
                case 0xA506:
                    command = new DrawImageCommand2(scriptReader);
                    break;
                case 0xA514:
                    command = new UnknownCommandA514(scriptReader);
                    break;
                case 0xA516:
                    command = new UnknownCommandA516(scriptReader);
                    break;
                case 0xA524:
                    command = new UnknownCommandA524(scriptReader);
                    break;
                case 0xA526:
                    command = new UnknownCommandA526(scriptReader);
                    break;
                case 0xA534:
                    command = new UnknownCommandA534(scriptReader);
                    break;
                case 0xA536:
                    command = new UnknownCommandA536(scriptReader);
                    break;
                case 0xA5A7:
                    command = new UnknownCommandA5A7(scriptReader);
                    break;
                case 0xA601:
                    command = new UnknownCommandA601(scriptReader);
                    break;
                case 0xB606:
                    command = new UnknownCommandB606(scriptReader);
                    break;
                case 0xC02F:
                    string soundFilename = ReadAlignedString(scriptReader);
                    command = new LoadSoundResource(soundFilename);
                    break;
                case 0xC031:
                    command = new UnknownCommandC031(scriptReader);
                    break;
                case 0xC041:
                    command = new UnknownCommandC041(scriptReader);
                    break;
                case 0xC051:
                    command = new UnknownCommandC051(scriptReader);
                    break;
                case 0xC061:
                    command = new UnknownCommandC061(scriptReader);
                    break;
                case 0xF01F:
                    string screenFilename = ReadAlignedString(scriptReader);
                    command = new LoadScreenResourceCommand(screenFilename);
                    break;
                case 0xF02F:
                    string imageFilename = ReadAlignedString(scriptReader);
                    command = new LoadImageResourceCommand(imageFilename);
                    break;
                case 0xF04F:
                    string fontFilename = ReadAlignedString(scriptReader);
                    command = new LoadFontResourceCommand(fontFilename);
                    break;
                case 0xF05F:
                    string paletteFilename = ReadAlignedString(scriptReader);
                    command = new LoadPaletteResourceCommand(paletteFilename);
                    break;
                default:
                    throw new InterestingDataException($"Unknown command type 0x{type:X4}");
            }
            command.Id = $"{type:X4}";
            Console.WriteLine($"0x{type:X4}: {command}");
            pageCommands.Add(command);
        }
        return resultScript;
    }

    public static string ReadAlignedString(BinaryReader reader) {
        string text = reader.ReadZeroTerminatedString();
        if ((text.Length & 1) == 0) {
            reader.ReadByte();
        }
        return text;
    }
}

internal class UnknownCommandA516 : AnimatorCommand {
    public UnknownCommandA516(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
        Arg2 = scriptReader.ReadInt16();
        Arg3 = scriptReader.ReadInt16();
        Arg4 = scriptReader.ReadInt16();
        Arg5 = scriptReader.ReadInt16();
        Arg6 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public int Arg2 { get; set; }

    public int Arg3 { get; set; }

    public int Arg4 { get; set; }

    public int Arg5 { get; set; }

    public int Arg6 { get; set; }

    public override string ToString() {
        return $"UnknownCommandA516({Arg1}, {Arg2}, {Arg3}, {Arg4}, {Arg5}, {Arg6});";
    }
}

internal class UnknownCommand0070 : AnimatorCommand {
    public override string ToString() {
        return "UnknownCommand0070();";
    }
}

internal class UnknownCommandA536 : AnimatorCommand {
    public UnknownCommandA536(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
        Arg2 = scriptReader.ReadInt16();
        Arg3 = scriptReader.ReadInt16();
        Arg4 = scriptReader.ReadInt16();
        Arg5 = scriptReader.ReadInt16();
        Arg6 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public int Arg2 { get; set; }

    public int Arg3 { get; set; }

    public int Arg4 { get; set; }

    public int Arg5 { get; set; }

    public int Arg6 { get; set; }

    public override string ToString() {
        return $"UnknownCommandA536({Arg1}, {Arg2}, {Arg3}, {Arg4}, {Arg5}, {Arg6});";
    }
}

internal class UnknownCommandA114 : AnimatorCommand {
    public UnknownCommandA114(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
        Arg2 = scriptReader.ReadInt16();
        Arg3 = scriptReader.ReadInt16();
        Arg4 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public int Arg2 { get; set; }

    public int Arg3 { get; set; }

    public int Arg4 { get; set; }

    public override string ToString() {
        return $"UnknownCommandA114({Arg1}, {Arg2}, {Arg3}, {Arg4});";
    }
}

internal class UnknownCommandA526 : AnimatorCommand {
    public UnknownCommandA526(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
        Arg2 = scriptReader.ReadInt16();
        Arg3 = scriptReader.ReadInt16();
        Arg4 = scriptReader.ReadInt16();
        Arg5 = scriptReader.ReadInt16();
        Arg6 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public int Arg2 { get; set; }

    public int Arg3 { get; set; }

    public int Arg4 { get; set; }

    public int Arg5 { get; set; }

    public int Arg6 { get; set; }

    public override string ToString() {
        return $"UnknownCommandA526({Arg1}, {Arg2}, {Arg3}, {Arg4}, {Arg5}, {Arg6});";
    }
}

internal class UnknownCommandA5A7 : AnimatorCommand {
    public UnknownCommandA5A7(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
        Arg2 = scriptReader.ReadInt16();
        Arg3 = scriptReader.ReadInt16();
        Arg4 = scriptReader.ReadInt16();
        Arg5 = scriptReader.ReadInt16();
        Arg6 = scriptReader.ReadInt16();
        Arg7 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public int Arg2 { get; set; }

    public int Arg3 { get; set; }

    public int Arg4 { get; set; }

    public int Arg5 { get; set; }

    public int Arg6 { get; set; }

    public int Arg7 { get; set; }

    public override string ToString() {
        return $"UnknownCommandA5A7({Arg1}, {Arg2}, {Arg3}, {Arg4}, {Arg5}, {Arg6}, {Arg7});";
    }
}

internal class UnknownCommandA034 : AnimatorCommand {
    public UnknownCommandA034(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
        Arg2 = scriptReader.ReadInt16();
        Arg3 = scriptReader.ReadInt16();
        Arg4 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public int Arg2 { get; set; }

    public int Arg3 { get; set; }

    public int Arg4 { get; set; }

    public override string ToString() {
        return $"UnknownCommandA034({Arg1}, {Arg2}, {Arg3}, {Arg4});";
    }
}

internal class UnknownCommandA514 : AnimatorCommand {
    public UnknownCommandA514(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
        Arg2 = scriptReader.ReadInt16();
        Arg3 = scriptReader.ReadInt16();
        Arg4 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public int Arg2 { get; set; }

    public int Arg3 { get; set; }

    public int Arg4 { get; set; }

    public override string ToString() {
        return $"UnknownCommandA514({Arg1}, {Arg2}, {Arg3}, {Arg4});";
    }
}

internal class UnknownCommand0510 : AnimatorCommand {
    public override string ToString() {
        return "UnknownCommand0510();";
    }
}

internal class UnknownCommand0500 : AnimatorCommand {
    public override string ToString() {
        return "UnknownCommand0500();";
    }
}

internal class UnknownCommandA534 : AnimatorCommand {
    public UnknownCommandA534(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
        Arg2 = scriptReader.ReadInt16();
        Arg3 = scriptReader.ReadInt16();
        Arg4 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public int Arg2 { get; set; }

    public int Arg3 { get; set; }

    public int Arg4 { get; set; }

    public override string ToString() {
        return $"UnknownCommandA534({Arg1}, {Arg2}, {Arg3}, {Arg4});";
    }
}

internal class UnknownCommandA0B5 : AnimatorCommand {
    public UnknownCommandA0B5(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
        Arg2 = scriptReader.ReadInt16();
        Arg3 = scriptReader.ReadInt16();
        Arg4 = scriptReader.ReadInt16();
        Arg5 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public int Arg2 { get; set; }

    public int Arg3 { get; set; }

    public int Arg4 { get; set; }

    public int Arg5 { get; set; }

    public override string ToString() {
        return $"UnknownCommandA0B5({Arg1}, {Arg2}, {Arg3}, {Arg4}, {Arg5});";
    }
}

internal class UnknownCommandA014 : AnimatorCommand {
    public UnknownCommandA014(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
        Arg2 = scriptReader.ReadInt16();
        Arg3 = scriptReader.ReadInt16();
        Arg4 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public int Arg2 { get; set; }

    public int Arg3 { get; set; }

    public int Arg4 { get; set; }

    public override string ToString() {
        return $"UnknownCommandA014({Arg1}, {Arg2}, {Arg3}, {Arg4});";
    }
}

internal class UnknownCommand2322 : AnimatorCommand {
    public UnknownCommand2322(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
        Arg2 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public int Arg2 { get; set; }

    public override string ToString() {
        return $"UnknownCommand2322({Arg1}, {Arg2});";
    }
}

internal class UnknownCommand2312 : AnimatorCommand {
    public UnknownCommand2312(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
        Arg2 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public int Arg2 { get; set; }

    public override string ToString() {
        return $"UnknownCommand2312({Arg1}, {Arg2});";
    }
}

internal class UnknownCommandC061 : AnimatorCommand {
    public UnknownCommandC061(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public override string ToString() {
        return $"UnknownCommandC061({Arg1});";
    }
}

internal class UnknownCommand0400 : AnimatorCommand {
    public override string ToString() {
        return "UnknownCommand0400();";
    }
}

internal class UnknownCommand2402 : AnimatorCommand {
    public UnknownCommand2402(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
        Arg2 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public int Arg2 { get; set; }

    public override string ToString() {
        return $"UnknownCommand2402({Arg1}, {Arg2});";
    }
}

internal class UnknownCommand2302 : AnimatorCommand {
    public UnknownCommand2302(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
        Arg2 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public int Arg2 { get; set; }

    public override string ToString() {
        return $"UnknownCommand2302({Arg1}, {Arg2});";
    }
}

internal class UnknownCommand1201 : AnimatorCommand {
    public UnknownCommand1201(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public override string ToString() {
        return $"UnknownCommand1201({Arg1});";
    }
}

internal class UnknownCommandA094 : AnimatorCommand {
    public UnknownCommandA094(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
        Arg2 = scriptReader.ReadInt16();
        Arg3 = scriptReader.ReadInt16();
        Arg4 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public int Arg2 { get; set; }

    public int Arg3 { get; set; }

    public int Arg4 { get; set; }

    public override string ToString() {
        return $"UnknownCommandA094({Arg1}, {Arg2}, {Arg3}, {Arg4});";
    }
}

internal class DrawImageCommand2 : AnimatorCommand {
    public DrawImageCommand2(BinaryReader scriptReader) {
        X = scriptReader.ReadInt16();
        Y = scriptReader.ReadInt16();
        ImageNumber = scriptReader.ReadInt16();
        ImageResource = scriptReader.ReadInt16();
        Arg5 = scriptReader.ReadInt16();
        Arg6 = scriptReader.ReadInt16();
    }

    public int Arg6 { get; set; }

    public int Arg5 { get; set; }

    public int ImageResource { get; set; }

    public int ImageNumber { get; set; }

    public int Y { get; set; }

    public int X { get; set; }

    public override string ToString() {
        return $"DrawImage2({X}, {Y}, {ImageNumber}, {ImageResource}, {Arg5}, {Arg6});";
    }
}

internal class UnknownCommand4004 : AnimatorCommand {
    public UnknownCommand4004(BinaryReader scriptReader) {
        X = scriptReader.ReadInt16();
        Y = scriptReader.ReadInt16();
        Width = scriptReader.ReadInt16();
        Height = scriptReader.ReadInt16();
    }

    public int Height { get; set; }

    public int Width { get; set; }

    public int Y { get; set; }

    public int X { get; set; }

    public override string ToString() {
        return $"UnknownCommand4004({X}, {Y}, {Width}, {Height});";
    }
}

internal class LoadSoundResource : AnimatorCommand {
    public LoadSoundResource(string soundFilename) {
        Filename = soundFilename;
    }

    public string Filename { get; set; }

    public override string ToString() {
        return $"LoadSoundResource('{Filename}');";
    }
}

internal class UnknownCommand1101 : AnimatorCommand {
    public UnknownCommand1101(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public override string ToString() {
        return $"UnknownCommand1101({Arg1});";
    }
}

internal class UnknownCommandC041 : AnimatorCommand {
    public UnknownCommandC041(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public override string ToString() {
        return $"UnknownCommandC041({Arg1});";
    }
}

internal class UnknownCommand0080 : AnimatorCommand {
    public override string ToString() {
        return "UnknownCommand0080();";
    }
}

internal class UnknownCommand00C0 : AnimatorCommand {
    public override string ToString() {
        return "UnknownCommand00C0();";
    }
}

internal class UnknownCommand1021 : AnimatorCommand {
    public UnknownCommand1021(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public override string ToString() {
        return $"UnknownCommand1021({Arg1});";
    }
}

internal class UnknownCommandA524 : AnimatorCommand {
    public UnknownCommandA524(BinaryReader scriptReader) {
        X = scriptReader.ReadInt16();
        Y = scriptReader.ReadInt16();
        ImageNumber = scriptReader.ReadInt16();
        ImageResource = scriptReader.ReadInt16();
    }

    public int ImageResource { get; set; }

    public int ImageNumber { get; set; }

    public int Y { get; set; }

    public int X { get; set; }

    public override string ToString() {
        return $"UnknownCommandA524({X}, {Y}, {ImageNumber}, {ImageResource});";
    }
}

internal class UnknownCommandC031 : AnimatorCommand {
    public UnknownCommandC031(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public override string ToString() {
        return $"UnknownCommandC031({Arg1});";
    }
}

internal class UnknownCommandC051 : AnimatorCommand {
    public UnknownCommandC051(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public override string ToString() {
        return $"UnknownCommandC051({Arg1});";
    }
}

internal class UnknownCommand0110 : AnimatorCommand {
    public override string ToString() {
        return "UnknownCommand0110();";
    }
}

internal class UnknownCommand1121 : AnimatorCommand {
    public UnknownCommand1121(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public override string ToString() {
        return $"UnknownCommand1121({Arg1});";
    }
}

internal class UnknownCommandA601 : AnimatorCommand {
    public UnknownCommandA601(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
    }

    public int Arg1 { get; set; }

    public override string ToString() {
        return $"UnknownCommandA601({Arg1});";
    }
}

internal class UnknownCommandB606 : AnimatorCommand {
    public UnknownCommandB606(BinaryReader scriptReader) {
        X = scriptReader.ReadInt16();
        Y = scriptReader.ReadInt16();
        Width = scriptReader.ReadInt16();
        Height = scriptReader.ReadInt16();
        Arg5 = scriptReader.ReadInt16();
        Arg6 = scriptReader.ReadInt16();
    }

    public int Arg6 { get; set; }

    public int Arg5 { get; set; }

    public int Height { get; set; }

    public int Width { get; set; }

    public int Y { get; set; }

    public int X { get; set; }

    public override string ToString() {
        return $"UnknownCommandB606({X}, {Y}, {Width}, {Height}, {Arg5}, {Arg6});";
    }
}

internal class UnknownCommand4214 : AnimatorCommand {
    public UnknownCommand4214(BinaryReader scriptReader) {
        X = scriptReader.ReadInt16();
        Y = scriptReader.ReadInt16();
        Width = scriptReader.ReadInt16();
        Height = scriptReader.ReadInt16();
    }

    public int Height { get; set; }

    public int Width { get; set; }

    public int Y { get; set; }

    public int X { get; set; }

    public override string ToString() {
        return $"UnknownCommand4214({X}, {Y}, {Width}, {Height});";
    }
}

internal class UnknownCommand2002 : AnimatorCommand {
    public UnknownCommand2002(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
        Arg2 = scriptReader.ReadInt16();
    }

    public int Arg2 { get; set; }

    public int Arg1 { get; set; }

    public override string ToString() {
        return $"UnknownCommand2002({Arg1}, {Arg2});";
    }
}

internal class UnknownCommand2012 : AnimatorCommand {
    public UnknownCommand2012(BinaryReader scriptReader) {
        Arg1 = scriptReader.ReadInt16();
        Arg2 = scriptReader.ReadInt16();
    }

    public int Arg2 { get; set; }

    public int Arg1 { get; set; }

    public override string ToString() {
        return $"UnknownCommand2012({Arg1}, {Arg2});";
    }
}

internal class SelectFontCommand : AnimatorCommand {
    public SelectFontCommand(BinaryReader scriptReader) {
        FontNumber = scriptReader.ReadInt16();
    }

    public int FontNumber { get; set; }

    public override string ToString() {
        return $"SelectFont({FontNumber});";
    }
}

internal class LoadFontResourceCommand : AnimatorCommand {
    public LoadFontResourceCommand(string filename) {
        Filename = filename;
    }

    public string Filename { get; set; }

    public override string ToString() {
        return $"LoadFontResource('{Filename}');";
    }
}

internal class ClearAreaCommand : AnimatorCommand {
    public ClearAreaCommand(BinaryReader scriptReader) {
        X = scriptReader.ReadInt16();
        Y = scriptReader.ReadInt16();
        Width = scriptReader.ReadInt16();
        Height = scriptReader.ReadInt16();
    }

    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public override string ToString() {
        return $"ClearArea({X}, {Y}, {Width}, {Height});";
    }
}

internal class FadeOutCommand : AnimatorCommand {
    public FadeOutCommand(BinaryReader scriptReader) {
        End = scriptReader.ReadInt16();
        Start = scriptReader.ReadInt16();
        Step = scriptReader.ReadInt16();
        Delay = scriptReader.ReadInt16();
    }

    public int Delay { get; set; }

    public int Step { get; set; }

    public int End { get; set; }

    public int Start { get; set; }

    public override string ToString() {
        return $"FadeOut({End}, {Start}, {Step}, {Delay});";
    }
}

internal class StoreAreaCommand : AnimatorCommand {
    public StoreAreaCommand(BinaryReader scriptReader) {
        X = scriptReader.ReadInt16();
        Y = scriptReader.ReadInt16();
        Width = scriptReader.ReadInt16();
        Height = scriptReader.ReadInt16();
    }

    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public override string ToString() {
        return $"StoreArea({X}, {Y}, {Width}, {Height});";
    }
}

internal class NextFrameCommand : AnimatorCommand {
    public override string ToString() {
        return "NextFrame();";
    }
}

internal class FadeInCommand : AnimatorCommand {
    public FadeInCommand(BinaryReader scriptReader) {
        Start = scriptReader.ReadInt16();
        End = scriptReader.ReadInt16();
        Step = scriptReader.ReadInt16();
        Delay = scriptReader.ReadInt16();
    }

    public int Delay { get; set; }

    public int Step { get; set; }

    public int End { get; set; }

    public int Start { get; set; }

    public override string ToString() {
        return $"FadeIn({Start}, {End}, {Step}, {Delay});";
    }
}

internal class StoreBackgroundCommand : AnimatorCommand {
    public override string ToString() {
        return "StoreBackground();";
    }
}

internal class DrawImageCommand : AnimatorCommand {
    public DrawImageCommand(BinaryReader scriptReader) {
        X = scriptReader.ReadInt16();
        Y = scriptReader.ReadInt16();
        ImageNumber = scriptReader.ReadInt16();
        ImageResource = scriptReader.ReadInt16();
    }

    public int ImageResource { get; set; }

    public int ImageNumber { get; set; }

    public int Y { get; set; }

    public int X { get; set; }

    public override string ToString() {
        return $"DrawImage({X}, {Y}, {ImageNumber}, {ImageResource});";
    }
}

internal class LoadScreenResourceCommand : AnimatorCommand {
    public LoadScreenResourceCommand(string screenFilename) {
        Filename = screenFilename;
    }

    public string Filename { get; set; }

    public override string ToString() {
        return $"LoadScreenResource('{Filename}');";
    }
}

internal class LoadImageResourceCommand : AnimatorCommand {
    public LoadImageResourceCommand(string filename) {
        Filename = filename;
    }

    public string Filename { get; set; }

    public override string ToString() {
        return $"LoadImageResource('{Filename}');";
    }
}

internal class SelectImageCommand : AnimatorCommand {
    public SelectImageCommand(BinaryReader scriptReader) {
        ImageNumber = scriptReader.ReadUInt16();
    }

    public int ImageNumber { get; set; }

    public override string ToString() {
        return $"SelectImage({ImageNumber});";
    }
}

internal class LoadPaletteResourceCommand : AnimatorCommand {
    public LoadPaletteResourceCommand(string filename) {
        Filename = filename;
    }

    public string Filename { get; set; }

    public override string ToString() {
        return $"LoadPaletteResource('{Filename}');";
    }
}

internal class SelectPaletteCommand : AnimatorCommand {
    public SelectPaletteCommand(BinaryReader scriptReader) {
        PaletteNumber = scriptReader.ReadUInt16();
    }

    public int PaletteNumber { get; set; }

    public override string ToString() {
        return $"SelectPalette({PaletteNumber});";
    }
}

internal class TagCommand : AnimatorCommand {
    public TagCommand(BinaryReader scriptReader, IReadOnlyDictionary<int, string> animatorTags) {
        TagNumber = scriptReader.ReadUInt16();
        if (animatorTags.TryGetValue(TagNumber, out string? tagName)) {
            TagName = tagName;
        }
    }

    public string? TagName { get; set; }

    public int TagNumber { get; set; }

    public override string ToString() {
        return $"Tag({TagNumber});";
    }
}

[JsonDerivedType(typeof(TagCommand), nameof(TagCommand))]
[JsonDerivedType(typeof(SelectPaletteCommand), nameof(SelectPaletteCommand))]
[JsonDerivedType(typeof(LoadPaletteResourceCommand), nameof(LoadPaletteResourceCommand))]
[JsonDerivedType(typeof(SelectImageCommand), nameof(SelectImageCommand))]
[JsonDerivedType(typeof(LoadImageResourceCommand), nameof(LoadImageResourceCommand))]
[JsonDerivedType(typeof(LoadScreenResourceCommand), nameof(LoadScreenResourceCommand))]
[JsonDerivedType(typeof(DrawImageCommand), nameof(DrawImageCommand))]
[JsonDerivedType(typeof(StoreBackgroundCommand), nameof(StoreBackgroundCommand))]
[JsonDerivedType(typeof(FadeInCommand), nameof(FadeInCommand))]
[JsonDerivedType(typeof(NextFrameCommand), nameof(NextFrameCommand))]
[JsonDerivedType(typeof(StoreAreaCommand), nameof(StoreAreaCommand))]
[JsonDerivedType(typeof(FadeOutCommand), nameof(FadeOutCommand))]
[JsonDerivedType(typeof(ClearAreaCommand), nameof(ClearAreaCommand))]
[JsonDerivedType(typeof(LoadFontResourceCommand), nameof(LoadFontResourceCommand))]
[JsonDerivedType(typeof(SelectFontCommand), nameof(SelectFontCommand))]
[JsonDerivedType(typeof(UnknownCommand2012), nameof(UnknownCommand2012))]
[JsonDerivedType(typeof(UnknownCommand2002), nameof(UnknownCommand2002))]
[JsonDerivedType(typeof(UnknownCommand4214), nameof(UnknownCommand4214))]
[JsonDerivedType(typeof(UnknownCommandB606), nameof(UnknownCommandB606))]
[JsonDerivedType(typeof(UnknownCommandA601), nameof(UnknownCommandA601))]
[JsonDerivedType(typeof(UnknownCommand1121), nameof(UnknownCommand1121))]
[JsonDerivedType(typeof(UnknownCommand0110), nameof(UnknownCommand0110))]
[JsonDerivedType(typeof(UnknownCommandC051), nameof(UnknownCommandC051))]
[JsonDerivedType(typeof(UnknownCommandC031), nameof(UnknownCommandC031))]
[JsonDerivedType(typeof(LoadSoundResource), nameof(LoadSoundResource))]
[JsonDerivedType(typeof(UnknownCommand2312), nameof(UnknownCommand2312))]
[JsonDerivedType(typeof(UnknownCommand2322), nameof(UnknownCommand2322))]
[JsonDerivedType(typeof(UnknownCommandA014), nameof(UnknownCommandA014))]
[JsonDerivedType(typeof(UnknownCommandA0B5), nameof(UnknownCommandA0B5))]
[JsonDerivedType(typeof(UnknownCommandA534), nameof(UnknownCommandA534))]
[JsonDerivedType(typeof(UnknownCommand0500), nameof(UnknownCommand0500))]
[JsonDerivedType(typeof(UnknownCommand0510), nameof(UnknownCommand0510))]
[JsonDerivedType(typeof(UnknownCommandA516), nameof(UnknownCommandA516))]
[JsonDerivedType(typeof(UnknownCommandA536), nameof(UnknownCommandA536))]
[JsonDerivedType(typeof(UnknownCommandA524), nameof(UnknownCommandA524))]
[JsonDerivedType(typeof(UnknownCommand1021), nameof(UnknownCommand1021))]
[JsonDerivedType(typeof(UnknownCommand00C0), nameof(UnknownCommand00C0))]
[JsonDerivedType(typeof(UnknownCommand0080), nameof(UnknownCommand0080))]
[JsonDerivedType(typeof(UnknownCommandC041), nameof(UnknownCommandC041))]
[JsonDerivedType(typeof(UnknownCommand1101), nameof(UnknownCommand1101))]
[JsonDerivedType(typeof(UnknownCommand4004), nameof(UnknownCommand4004))]
[JsonDerivedType(typeof(DrawImageCommand2), nameof(DrawImageCommand2))]
[JsonDerivedType(typeof(UnknownCommandA094), nameof(UnknownCommandA094))]
[JsonDerivedType(typeof(UnknownCommand1201), nameof(UnknownCommand1201))]
[JsonDerivedType(typeof(UnknownCommand2302), nameof(UnknownCommand2302))]
[JsonDerivedType(typeof(UnknownCommand2402), nameof(UnknownCommand2402))]
[JsonDerivedType(typeof(UnknownCommand0400), nameof(UnknownCommand0400))]
[JsonDerivedType(typeof(UnknownCommandA5A7), nameof(UnknownCommandA5A7))]
[JsonDerivedType(typeof(UnknownCommandA526), nameof(UnknownCommandA526))]
[JsonDerivedType(typeof(UnknownCommandA514), nameof(UnknownCommandA514))]
[JsonDerivedType(typeof(UnknownCommandA114), nameof(UnknownCommandA114))]
[JsonDerivedType(typeof(UnknownCommandA034), nameof(UnknownCommandA034))]
[JsonDerivedType(typeof(UnknownCommandC061), nameof(UnknownCommandC061))]
[JsonDerivedType(typeof(UnknownCommand0070), nameof(UnknownCommand0070))]
public abstract class AnimatorCommand {
    public string Id { get; set; }
}

public class AnimationScript {
    public List<List<AnimatorCommand>> Pages { get; set; } = new();
}