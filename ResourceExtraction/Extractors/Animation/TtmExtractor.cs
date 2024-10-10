namespace ResourceExtraction.Extractors.Animation;

using GameData.Resources.Animation;
using GameData.Resources.Animation.Commands;
using ResourceExtraction.Compression;
using ResourceExtraction.Extensions;
using ResourceExtractor.Compression;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class TtmExtractor : ExtractorBase<AnimatorScene> {
    public override AnimatorScene Extract(string id, Stream resourceStream) {
        using var resourceReader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));

        var scene = new AnimatorScene(id);

        string tag = ReadTag(resourceReader);
        if (tag != "VER") {
            throw new InvalidDataException($"Expected VER tag, got {tag}");
        }
        int verSize = resourceReader.ReadUInt16();
        if (resourceReader.ReadUInt16() != 0x0000) {
            throw new InterestingDataException("UInt16 after size is not 0x0000 in VER tag");
        }
        scene.Version = resourceReader.ReadZeroTerminatedString();
        tag = ReadTag(resourceReader);
        if (tag != "PAG") {
            throw new InvalidDataException($"Expected PAG tag, got {tag}");
        }
        int pagSize = resourceReader.ReadUInt16();
        if (resourceReader.ReadUInt16() != 0x0000) {
            throw new InterestingDataException("UInt16 after size is not 0x0000 in PAG tag");
        }
        scene.NumberOfFrames = resourceReader.ReadUInt16();
        tag = ReadTag(resourceReader);
        if (tag != "TT3") {
            throw new InvalidDataException($"Expected TT3 tag, got {tag}");
        }
        ushort tt3Size = resourceReader.ReadUInt16();
        if (resourceReader.ReadUInt16() != 0x0000) {
            throw new InterestingDataException("UInt16 after size is not 0x0000 in TT3 tag");
        }
        var compressionType = (CompressionType)resourceReader.ReadByte();
        var decompressedSize = (int)resourceReader.ReadUInt32();
        var compression = CompressionFactory.Create(compressionType);
        var scriptStream = compression.Decompress(resourceReader.BaseStream, tt3Size - 5);
        if (scriptStream.Length != decompressedSize) {
            throw new InvalidOperationException($"Decompressed TT3 size {scriptStream.Length} does not match expected size {decompressedSize} in {id}");
        }
        var scriptBytes = new byte[scriptStream.Length];
        scriptStream.ReadExactly(scriptBytes);

        File.WriteAllBytes("scriptBytes.bin", scriptBytes);

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
        scene.Tags = ReadTags(resourceReader);
        scene.Frames = DecodeScript(scriptBytes, scene.Tags, id);

        return scene;
    }

    private static List<List<FrameCommand>> DecodeScript(byte[] scriptBytes, Dictionary<int, string> animatorTags, string id) {
        var frames = new List<List<FrameCommand>>();
        using var scriptStream = new MemoryStream(scriptBytes);
        using var scriptReader = new BinaryReader(scriptStream, Encoding.GetEncoding(DosCodePage));
        var frameCommands = new List<FrameCommand>();
        while (scriptStream.Position < scriptStream.Length) {
            ushort type = scriptReader.ReadUInt16();
            if (type == 0x0FF0) {
                frames.Add(frameCommands);
                frameCommands = [];

                continue;
            }
            var command = GetFrameCommand(animatorTags, type, scriptReader);
            Log($"0x{type:X4}: {command} [{id}]");
            frameCommands.Add(command);
        }

        return frames;
    }

    public static FrameCommand GetFrameCommand(Dictionary<int, string> animatorTags, ushort type, BinaryReader scriptReader) {
        string id;
        FrameCommand command;
        switch (type) {
            case 0x0020:
                command = new StoreScreen();

                break;
            case 0x0070:
                command = new DisposeCurrentPalette();

                break;
            case 0x0080:
                command = new DisposeCurrentBitmap();

                break;
            case 0x00C0:
                command = new DisposeTargetBuffer();

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

            case 0x1021:
                command = new SetFramesDuration {
                    Amount = scriptReader.ReadInt16()
                };

                break;
            case 0x1051:
                command = new SelectImageSlot {
                    SlotNumber = scriptReader.ReadUInt16()
                };

                break;
            case 0x1061:
                command = new SelectPaletteSlot {
                    SlotNumber = scriptReader.ReadUInt16()
                };

                break;
            case 0x1071:
                command = new SelectFontSlot {
                    SlotNumber = scriptReader.ReadInt16()
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
                command = new TagFrame {
                    SceneNumber = tagNumber,
                    SceneTag = tagName
                };

                break;
            case 0x1121:
                command = new SetTargetBuffer {
                    BufferNumber = scriptReader.ReadInt16()
                };

                break;
            case 0x1201:
                command = new GotoFrame {
                    NextFrame = scriptReader.ReadInt16()
                };

                break;
            case 0x2002:
                command = new SetColors {
                    ForegroundColor = scriptReader.ReadInt16(),
                    BackgroundColor = scriptReader.ReadInt16()
                };

                break;
            case 0x2012:
                command = new DialogCommand {
                    Dialog16Id = scriptReader.ReadInt16(),
                    Arg2 = scriptReader.ReadInt16()
                };

                break;
            case 0x2302:
                command = new SetRange1 {
                    Start = scriptReader.ReadInt16(),
                    End = scriptReader.ReadInt16()
                };

                break;
            case 0x2312:
                command = new SetRange2 {
                    Start = scriptReader.ReadInt16(),
                    End = scriptReader.ReadInt16()
                };

                break;
            case 0x2322:
                command = new SetRange3 {
                    Start = scriptReader.ReadInt16(),
                    End = scriptReader.ReadInt16()
                };

                break;
            case 0x2402:
                command = new UnknownCommand2402 {
                    Arg1 = scriptReader.ReadInt16(),
                    Arg2 = scriptReader.ReadInt16()
                };

                break;
            case 0x4004:
                command = new SetClipArea {
                    X = scriptReader.ReadInt16(),
                    Y = scriptReader.ReadInt16(),
                    Width = scriptReader.ReadInt16(),
                    Height = scriptReader.ReadInt16()
                };

                break;
            case 0x4114:
                command = new FadeOut {
                    Start = scriptReader.ReadInt16(),
                    Length = scriptReader.ReadInt16(),
                    Color = scriptReader.ReadInt16(),
                    Speed = scriptReader.ReadInt16()
                };

                break;
            case 0x4124:
                command = new FadeIn {
                    Start = scriptReader.ReadInt16(),
                    Length = scriptReader.ReadInt16(),
                    Color = scriptReader.ReadInt16(),
                    Speed = scriptReader.ReadInt16()
                };

                break;
            case 0x4204:
                command = new StoreArea {
                    X = scriptReader.ReadInt16(),
                    Y = scriptReader.ReadInt16(),
                    Width = scriptReader.ReadInt16(),
                    Height = scriptReader.ReadInt16()
                };

                break;
            case 0x4214:
                command = new CopyToTargetBuffer {
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
                command = new FillArea {
                    X = scriptReader.ReadInt16(),
                    Y = scriptReader.ReadInt16(),
                    Width = scriptReader.ReadInt16(),
                    Height = scriptReader.ReadInt16()
                };

                break;
            case 0xA114:
                command = new DrawBorder {
                    X = scriptReader.ReadInt16(),
                    Y = scriptReader.ReadInt16(),
                    Width = scriptReader.ReadInt16(),
                    Height = scriptReader.ReadInt16()
                };

                break;
            case 0xA504:
                command = new DrawImage {
                    X = scriptReader.ReadInt16(),
                    Y = scriptReader.ReadInt16(),
                    ImageNumber = scriptReader.ReadInt16(),
                    ImageSlot = scriptReader.ReadInt16(),
                };

                break;
            case 0xA506:
                command = new DrawImageScaled {
                    X = scriptReader.ReadInt16(),
                    Y = scriptReader.ReadInt16(),
                    ImageNumber = scriptReader.ReadInt16(),
                    ImageSlot = scriptReader.ReadInt16(),
                    Width = scriptReader.ReadInt16(),
                    Height = scriptReader.ReadInt16()
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
                command = new DrawImageFlippedHorizontally {
                    X = scriptReader.ReadInt16(),
                    Y = scriptReader.ReadInt16(),
                    ImageNumber = scriptReader.ReadInt16(),
                    ImageSlot = scriptReader.ReadInt16()
                };

                break;
            case 0xA526:
                command = new DrawImageFlippedHorizontallyScaled {
                    X = scriptReader.ReadInt16(),
                    Y = scriptReader.ReadInt16(),
                    ImageNumber = scriptReader.ReadInt16(),
                    ImageSlot = scriptReader.ReadInt16(),
                    Width = scriptReader.ReadInt16(),
                    Height = scriptReader.ReadInt16()
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
                command = new DrawAreaFromBuffer {
                    BufferNumber = scriptReader.ReadInt16()
                };

                break;
            case 0xB606:
                command = new CopyAreaBetweenBuffers {
                    X = scriptReader.ReadInt16(),
                    Y = scriptReader.ReadInt16(),
                    Width = scriptReader.ReadInt16(),
                    Height = scriptReader.ReadInt16(),
                    SourceBuffer = scriptReader.ReadInt16(),
                    DestinationBuffer = scriptReader.ReadInt16()
                };

                break;
            case 0xC02F:
                string soundFilename = ReadAlignedString(scriptReader);
                command = new LoadSoundResource {
                    Filename = soundFilename
                };

                break;
            case 0xC031:
                command = new LoadSound {
                    SoundId = scriptReader.ReadInt16()
                };

                break;
            case 0xC041:
                command = new StopSound {
                    SoundId = scriptReader.ReadInt16()
                };

                break;
            case 0x1301:
            case 0xC051:
                command = new PlaySound {
                    SoundId = scriptReader.ReadInt16()
                };

                break;
            case 0x1311:
            case 0xC061:
                command = new UnknownCommandC061 {
                    SoundId = scriptReader.ReadInt16()
                };

                break;
            case 0xF01F:
                string screenFilename = ReadAlignedString(scriptReader);
                command = new LoadScreenResource {
                    Filename = screenFilename
                };

                break;
            case 0xF02F:
                string imageFilename = ReadAlignedString(scriptReader);
                command = new LoadImageResource {
                    Filename = imageFilename
                };

                break;
            case 0xF04F:
                string fontFilename = ReadAlignedString(scriptReader);
                command = new LoadFontResource {
                    Filename = fontFilename
                };

                break;
            case 0xF05F:
                string paletteFilename = ReadAlignedString(scriptReader);
                command = new LoadPaletteResource {
                    Filename = paletteFilename
                };

                break;
            default:
                throw new InterestingDataException($"Unknown command type 0x{type:X4}");
        }
        command.Id = $"{type:X4}";

        return command;
    }
}