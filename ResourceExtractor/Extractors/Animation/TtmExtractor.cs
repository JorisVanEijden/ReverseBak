namespace ResourceExtractor.Extractors.Animation;

using GameData.Resources.Animation;
using GameData.Resources.Animation.Commands;

using ResourceExtractor.Compression;
using ResourceExtractor.Extensions;

using System.Text;

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
                    command = new UnknownCommand1021 {
                        Arg1 = scriptReader.ReadInt16()
                    };
                    break;
                case 0x1051:
                    command = new SelectImageCommand {
                        ImageNumber = scriptReader.ReadUInt16()
                    };
                    break;
                case 0x1061:
                    command = new SelectPaletteCommand {
                        PaletteNumber = scriptReader.ReadUInt16()
                    };
                    break;
                case 0x1071:
                    command = new SelectFontCommand {
                        FontNumber = scriptReader.ReadInt16()
                    };
                    break;
                case 0x1101:
                    command = new UnknownCommand1101 {
                        Arg1 = scriptReader.ReadInt16()
                    };
                    break;
                case 0x1111:
                    ushort tagNumber = scriptReader.ReadUInt16();
                    animatorTags.TryGetValue(tagNumber, out string? tagName);
                    command = new TagCommand {
                        TagNumber = tagNumber,
                        TagName = tagName
                    };
                    break;
                case 0x1121:
                    command = new UnknownCommand1121 {
                        Arg1 = scriptReader.ReadInt16()
                    };
                    break;
                case 0x1201:
                    command = new UnknownCommand1201 {
                        Arg1 = scriptReader.ReadInt16()
                    };
                    break;
                case 0x2002:
                    command = new UnknownCommand2002 {
                        Arg1 = scriptReader.ReadInt16(),
                        Arg2 = scriptReader.ReadInt16()
                    };
                    break;
                case 0x2012:
                    command = new UnknownCommand2012 {
                        Arg1 = scriptReader.ReadInt16(),
                        Arg2 = scriptReader.ReadInt16()
                    };
                    break;
                case 0x2302:
                    command = new UnknownCommand2302 {
                        Arg1 = scriptReader.ReadInt16(),
                        Arg2 = scriptReader.ReadInt16()
                    };
                    break;
                case 0x2312:
                    command = new UnknownCommand2312 {
                        Arg1 = scriptReader.ReadInt16(),
                        Arg2 = scriptReader.ReadInt16()
                    };
                    break;
                case 0x2322:
                    command = new UnknownCommand2322 {
                        Arg1 = scriptReader.ReadInt16(),
                        Arg2 = scriptReader.ReadInt16()
                    };
                    break;
                case 0x2402:
                    command = new UnknownCommand2402 {
                        Arg1 = scriptReader.ReadInt16(),
                        Arg2 = scriptReader.ReadInt16()
                    };
                    break;
                case 0x4004:
                    command = new UnknownCommand4004 {
                        X = scriptReader.ReadInt16(),
                        Y = scriptReader.ReadInt16(),
                        Width = scriptReader.ReadInt16(),
                        Height = scriptReader.ReadInt16()
                    };
                    break;
                case 0x4114:
                    command = new FadeOutCommand {
                        End = scriptReader.ReadInt16(),
                        Start = scriptReader.ReadInt16(),
                        Step = scriptReader.ReadInt16(),
                        Delay = scriptReader.ReadInt16()
                    };
                    break;
                case 0x4124:
                    command = new FadeInCommand {
                        Start = scriptReader.ReadInt16(),
                        End = scriptReader.ReadInt16(),
                        Step = scriptReader.ReadInt16(),
                        Delay = scriptReader.ReadInt16()
                    };
                    break;
                case 0x4204:
                    command = new StoreAreaCommand {
                        X = scriptReader.ReadInt16(),
                        Y = scriptReader.ReadInt16(),
                        Width = scriptReader.ReadInt16(),
                        Height = scriptReader.ReadInt16()
                    };
                    break;
                case 0x4214:
                    command = new UnknownCommand4214 {
                        X = scriptReader.ReadInt16(),
                        Y = scriptReader.ReadInt16(),
                        Width = scriptReader.ReadInt16(),
                        Height = scriptReader.ReadInt16()
                    };
                    break;
                case 0xA014:
                    command = new UnknownCommandA014 {
                        Arg1 = scriptReader.ReadInt16(),
                        Arg2 = scriptReader.ReadInt16(),
                        Arg3 = scriptReader.ReadInt16(),
                        Arg4 = scriptReader.ReadInt16()
                    };
                    break;
                case 0xA034:
                    command = new UnknownCommandA034 {
                        Arg1 = scriptReader.ReadInt16(),
                        Arg2 = scriptReader.ReadInt16(),
                        Arg3 = scriptReader.ReadInt16(),
                        Arg4 = scriptReader.ReadInt16()
                    };
                    break;
                case 0xA094:
                    command = new UnknownCommandA094 {
                        Arg1 = scriptReader.ReadInt16(),
                        Arg2 = scriptReader.ReadInt16(),
                        Arg3 = scriptReader.ReadInt16(),
                        Arg4 = scriptReader.ReadInt16()
                    };
                    break;
                case 0xA0B5:
                    command = new UnknownCommandA0B5 {
                        Arg1 = scriptReader.ReadInt16(),
                        Arg2 = scriptReader.ReadInt16(),
                        Arg3 = scriptReader.ReadInt16(),
                        Arg4 = scriptReader.ReadInt16(),
                        Arg5 = scriptReader.ReadInt16()
                    };
                    break;
                case 0xA104:
                    command = new ClearAreaCommand {
                        X = scriptReader.ReadInt16(),
                        Y = scriptReader.ReadInt16(),
                        Width = scriptReader.ReadInt16(),
                        Height = scriptReader.ReadInt16()
                    };
                    break;
                case 0xA114:
                    command = new UnknownCommandA114 {
                        Arg1 = scriptReader.ReadInt16(),
                        Arg2 = scriptReader.ReadInt16(),
                        Arg3 = scriptReader.ReadInt16(),
                        Arg4 = scriptReader.ReadInt16()
                    };
                    break;
                case 0xA504:
                    command = new DrawImageCommand {
                        X = scriptReader.ReadInt16(),
                        Y = scriptReader.ReadInt16(),
                        ImageNumber = scriptReader.ReadInt16(),
                        ImageResource = scriptReader.ReadInt16(),
                    };
                    break;
                case 0xA506:
                    command = new DrawImageCommand2 {
                        X = scriptReader.ReadInt16(),
                        Y = scriptReader.ReadInt16(),
                        ImageNumber = scriptReader.ReadInt16(),
                        ImageResource = scriptReader.ReadInt16(),
                        Arg5 = scriptReader.ReadInt16(),
                        Arg6 = scriptReader.ReadInt16()
                    };
                    break;
                case 0xA514:
                    command = new UnknownCommandA514 {
                        Arg1 = scriptReader.ReadInt16(),
                        Arg2 = scriptReader.ReadInt16(),
                        Arg3 = scriptReader.ReadInt16(),
                        Arg4 = scriptReader.ReadInt16()
                    };
                    break;
                case 0xA516:
                    command = new UnknownCommandA516 {
                        Arg1 = scriptReader.ReadInt16(),
                        Arg2 = scriptReader.ReadInt16(),
                        Arg3 = scriptReader.ReadInt16(),
                        Arg4 = scriptReader.ReadInt16(),
                        Arg5 = scriptReader.ReadInt16(),
                        Arg6 = scriptReader.ReadInt16()
                    };
                    break;
                case 0xA524:
                    command = new UnknownCommandA524 {
                        X = scriptReader.ReadInt16(),
                        Y = scriptReader.ReadInt16(),
                        ImageNumber = scriptReader.ReadInt16(),
                        ImageResource = scriptReader.ReadInt16()
                    };
                    break;
                case 0xA526:
                    command = new UnknownCommandA526 {
                        Arg1 = scriptReader.ReadInt16(),
                        Arg2 = scriptReader.ReadInt16(),
                        Arg3 = scriptReader.ReadInt16(),
                        Arg4 = scriptReader.ReadInt16(),
                        Arg5 = scriptReader.ReadInt16(),
                        Arg6 = scriptReader.ReadInt16()
                    };
                    break;
                case 0xA534:
                    command = new UnknownCommandA534 {
                        Arg1 = scriptReader.ReadInt16(),
                        Arg2 = scriptReader.ReadInt16(),
                        Arg3 = scriptReader.ReadInt16(),
                        Arg4 = scriptReader.ReadInt16()
                    };
                    break;
                case 0xA536:
                    command = new UnknownCommandA536 {
                        Arg1 = scriptReader.ReadInt16(),
                        Arg2 = scriptReader.ReadInt16(),
                        Arg3 = scriptReader.ReadInt16(),
                        Arg4 = scriptReader.ReadInt16(),
                        Arg5 = scriptReader.ReadInt16(),
                        Arg6 = scriptReader.ReadInt16()
                    };
                    break;
                case 0xA5A7:
                    command = new UnknownCommandA5A7 {
                        Arg1 = scriptReader.ReadInt16(),
                        Arg2 = scriptReader.ReadInt16(),
                        Arg3 = scriptReader.ReadInt16(),
                        Arg4 = scriptReader.ReadInt16(),
                        Arg5 = scriptReader.ReadInt16(),
                        Arg6 = scriptReader.ReadInt16(),
                        Arg7 = scriptReader.ReadInt16()
                    };
                    break;
                case 0xA601:
                    command = new UnknownCommandA601 {
                        Arg1 = scriptReader.ReadInt16()
                    };
                    break;
                case 0xB606:
                    command = new UnknownCommandB606 {
                        X = scriptReader.ReadInt16(),
                        Y = scriptReader.ReadInt16(),
                        Width = scriptReader.ReadInt16(),
                        Height = scriptReader.ReadInt16(),
                        Arg5 = scriptReader.ReadInt16(),
                        Arg6 = scriptReader.ReadInt16()
                    };
                    break;
                case 0xC02F:
                    string soundFilename = ReadAlignedString(scriptReader);
                    command = new LoadSoundResource(soundFilename);
                    break;
                case 0xC031:
                    command = new UnknownCommandC031 {
                        Arg1 = scriptReader.ReadInt16()
                    };
                    break;
                case 0xC041:
                    command = new UnknownCommandC041 {
                        Arg1 = scriptReader.ReadInt16()
                    };
                    break;
                case 0xC051:
                    command = new UnknownCommandC051 {
                        Arg1 = scriptReader.ReadInt16()
                    };
                    break;
                case 0xC061:
                    command = new UnknownCommandC061 {
                        Arg1 = scriptReader.ReadInt16()
                    };
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