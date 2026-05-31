namespace ResourceExtraction.Extractors.Animation;

using GameData.Resources.Animation;
using GameData.Resources.Animation.FrameCommands;
using ResourceExtraction.Compression;
using ResourceExtraction.Extensions;
using ResourceExtractor.Compression;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class TtmExtractor : ExtractorBase<AnimationResource> {
    public override AnimationResource Extract(string id, Stream resourceStream) {
        using var resourceReader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));

        Log($"Extracting {id}");

        string tag = resourceReader.ReadTag();
        if (tag != "VER") {
            throw new InvalidDataException($"Expected VER tag, got {tag}");
        }
        int verSize = resourceReader.ReadUInt16();
        if (resourceReader.ReadUInt16() != 0x0000) {
            throw new InterestingDataException("UInt16 after size is not 0x0000 in VER tag");
        }
        string version = resourceReader.ReadZeroTerminatedString();
        tag = resourceReader.ReadTag();
        if (tag != "PAG") {
            throw new InvalidDataException($"Expected PAG tag, got {tag}");
        }
        int pagSize = resourceReader.ReadUInt16();
        if (resourceReader.ReadUInt16() != 0x0000) {
            throw new InterestingDataException("UInt16 after size is not 0x0000 in PAG tag");
        }
        int numberOfFrames = resourceReader.ReadUInt16();
        tag = resourceReader.ReadTag();
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
        var commandStream = compression.Decompress(resourceReader.BaseStream, tt3Size - 5);
        if (commandStream.Length != decompressedSize) {
            throw new InvalidOperationException($"Decompressed TT3 size {commandStream.Length} does not match expected size {decompressedSize} in {id}");
        }
        var commandBytes = new byte[commandStream.Length];
        commandStream.ReadExactly(commandBytes);

        tag = resourceReader.ReadTag();
        if (tag != "TTI") {
            throw new InvalidDataException($"Expected TTI tag, got {tag}");
        }
        ushort ttiSize = resourceReader.ReadUInt16();
        if (resourceReader.ReadUInt16() != 0x8000) {
            throw new InterestingDataException("UInt16 after size is not 0x8000 in TTI tag");
        }
        tag = resourceReader.ReadTag();
        if (tag != "TAG") {
            throw new InvalidDataException($"Expected TAG tag, got {tag}");
        }

        Dictionary<int, string> tags =  resourceReader.ReadTags();
        List<Frame> frames = ExtractFrames(commandBytes, id);

        return new AnimationResource(id, version, tags, frames);
    }

    private static List<Frame> ExtractFrames(byte[] commandBytes, string id) {
        var frames = new List<Frame>();
        using var commandStream = new MemoryStream(commandBytes);
        using var commandReader = new BinaryReader(commandStream, Encoding.GetEncoding(DosCodePage));
        var frame = new Frame();
        while (commandStream.Position < commandStream.Length) {
            ushort commandType = commandReader.ReadUInt16();

            // Detect end of frame
            if (commandType is 0x0FF0) {
                frames.Add(frame);
                frame = new Frame();

                continue;
            }
            var command = GetFrameCommand(commandType, commandReader);
            frame.Commands.Add(command);
            if (command is TagFrame tagFrame) {
                frame.Tag = tagFrame.TagNumber;
            }
            Log($"0x{commandType:X4}: {command} [{id}]");
        }
        // Add any trailing frame
        if (frame.Commands.Count > 0) {
            frames.Add(frame);
        }

        return frames;
    }

    public static FrameCommand GetFrameCommand(ushort type, BinaryReader commandReader) {
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
                command = new EndScene();

                break;
            case 0x0400:
                command = new UnknownCommand0400();

                break;
            case 0x0500:
                command = new ObsoleteCommand0500();

                break;
            case 0x0510:
                command = new ObsoleteCommand0510();

                break;

            case 0x1021:
                command = new SetFramesDuration {
                    Amount = commandReader.ReadInt16()
                };

                break;
            case 0x1051:
                command = new SelectImageSlot {
                    SlotNumber = commandReader.ReadUInt16()
                };

                break;
            case 0x1061:
                command = new SelectPaletteSlot {
                    SlotNumber = commandReader.ReadUInt16()
                };

                break;
            case 0x1071:
                command = new SelectFontSlot {
                    SlotNumber = commandReader.ReadInt16()
                };

                break;
            case 0x1101:
            case 0x1111:
                command = new TagFrame {
                    TagNumber = commandReader.ReadInt16(),
                    UnknownBool = type == 0x1111
                };

                break;
            case 0x1121:
                command = new SetTargetBuffer {
                    BufferNumber = commandReader.ReadInt16()
                };

                break;
            case 0x1201:
                command = new GotoFrame {
                    NextFrame = commandReader.ReadInt16()
                };

                break;
            case 0x2002:
                command = new SetColors {
                    ForegroundColor = commandReader.ReadInt16(),
                    BackgroundColor = commandReader.ReadInt16()
                };

                break;
            case 0x2012:
                command = new DialogCommand {
                    Dialog16Id = commandReader.ReadInt16(),
                    Arg2 = commandReader.ReadInt16()
                };

                break;
            case 0x2302:
                command = new SetRange1 {
                    Start = commandReader.ReadInt16(),
                    End = commandReader.ReadInt16()
                };

                break;
            case 0x2312:
                command = new SetRange2 {
                    Start = commandReader.ReadInt16(),
                    End = commandReader.ReadInt16()
                };

                break;
            case 0x2322:
                command = new SetRange3 {
                    Start = commandReader.ReadInt16(),
                    End = commandReader.ReadInt16()
                };

                break;
            case 0x2402:
                command = new StartPaletteCycle {
                    Range = (Ranges)commandReader.ReadInt16(),
                    Step = commandReader.ReadInt16()
                };

                break;
            case 0x4004:
                command = new SetClipArea {
                    X = commandReader.ReadInt16(),
                    Y = commandReader.ReadInt16(),
                    Width = commandReader.ReadInt16(),
                    Height = commandReader.ReadInt16()
                };

                break;
            case 0x4114:
                command = new FadeOut {
                    Start = commandReader.ReadInt16(),
                    Length = commandReader.ReadInt16(),
                    Color = commandReader.ReadInt16(),
                    Speed = commandReader.ReadInt16()
                };

                break;
            case 0x4124:
                command = new FadeIn {
                    Start = commandReader.ReadInt16(),
                    Length = commandReader.ReadInt16(),
                    Color = commandReader.ReadInt16(),
                    Speed = commandReader.ReadInt16()
                };

                break;
            case 0x4204:
                command = new StoreArea {
                    X = commandReader.ReadInt16(),
                    Y = commandReader.ReadInt16(),
                    Width = commandReader.ReadInt16(),
                    Height = commandReader.ReadInt16()
                };

                break;
            case 0x4214:
                command = new CopyToTargetBuffer {
                    X = commandReader.ReadInt16(),
                    Y = commandReader.ReadInt16(),
                    Width = commandReader.ReadInt16(),
                    Height = commandReader.ReadInt16()
                };

                break;
            case 0xA014:
                command = new UnknownCommandA014 {
                    Arg1 = commandReader.ReadInt16(),
                    Arg2 = commandReader.ReadInt16(),
                    Arg3 = commandReader.ReadInt16(),
                    Arg4 = commandReader.ReadInt16()
                };

                break;
            case 0xA034:
                command = new UnknownCommandA034 {
                    Arg1 = commandReader.ReadInt16(),
                    Arg2 = commandReader.ReadInt16(),
                    Arg3 = commandReader.ReadInt16(),
                    Arg4 = commandReader.ReadInt16()
                };

                break;
            case 0xA094:
                command = new UnknownCommandA094 {
                    Arg1 = commandReader.ReadInt16(),
                    Arg2 = commandReader.ReadInt16(),
                    Arg3 = commandReader.ReadInt16(),
                    Arg4 = commandReader.ReadInt16()
                };

                break;
            case 0xA0B5:
                command = new UnknownCommandA0B5 {
                    Arg1 = commandReader.ReadInt16(),
                    Arg2 = commandReader.ReadInt16(),
                    Arg3 = commandReader.ReadInt16(),
                    Arg4 = commandReader.ReadInt16(),
                    Arg5 = commandReader.ReadInt16()
                };

                break;
            case 0xA104:
                command = new FillArea {
                    X = commandReader.ReadInt16(),
                    Y = commandReader.ReadInt16(),
                    Width = commandReader.ReadInt16(),
                    Height = commandReader.ReadInt16()
                };

                break;
            case 0xA114:
                command = new DrawBorder {
                    X = commandReader.ReadInt16(),
                    Y = commandReader.ReadInt16(),
                    Width = commandReader.ReadInt16(),
                    Height = commandReader.ReadInt16()
                };

                break;
            case 0xA504:
                command = new DrawImage {
                    X = commandReader.ReadInt16(),
                    Y = commandReader.ReadInt16(),
                    ImageNumber = commandReader.ReadInt16(),
                    ImageSlot = commandReader.ReadInt16(),
                };

                break;
            case 0xA506:
                command = new DrawImageScaled {
                    X = commandReader.ReadInt16(),
                    Y = commandReader.ReadInt16(),
                    ImageNumber = commandReader.ReadInt16(),
                    ImageSlot = commandReader.ReadInt16(),
                    Width = commandReader.ReadInt16(),
                    Height = commandReader.ReadInt16()
                };

                break;
            case 0xA514:
                command = new DrawImageFlippedVertically {
                    X = commandReader.ReadInt16(),
                    Y = commandReader.ReadInt16(),
                    ImageNumber = commandReader.ReadInt16(),
                    ImageSlot = commandReader.ReadInt16()
                };

                break;
            case 0xA516:
                command = new DrawImageFlippedVerticallyScaled {
                    X = commandReader.ReadInt16(),
                    Y = commandReader.ReadInt16(),
                    ImageNumber = commandReader.ReadInt16(),
                    ImageSlot = commandReader.ReadInt16(),
                    Width = commandReader.ReadInt16(),
                    Height = commandReader.ReadInt16()
                };

                break;
            case 0xA524:
                command = new DrawImageFlippedHorizontally {
                    X = commandReader.ReadInt16(),
                    Y = commandReader.ReadInt16(),
                    ImageNumber = commandReader.ReadInt16(),
                    ImageSlot = commandReader.ReadInt16()
                };

                break;
            case 0xA526:
                command = new DrawImageFlippedHorizontallyScaled {
                    X = commandReader.ReadInt16(),
                    Y = commandReader.ReadInt16(),
                    ImageNumber = commandReader.ReadInt16(),
                    ImageSlot = commandReader.ReadInt16(),
                    Width = commandReader.ReadInt16(),
                    Height = commandReader.ReadInt16()
                };

                break;
            case 0xA534:
                command = new DrawImageRotated180 {
                    X = commandReader.ReadInt16(),
                    Y = commandReader.ReadInt16(),
                    ImageNumber = commandReader.ReadInt16(),
                    ImageSlot = commandReader.ReadInt16()
                };

                break;
            case 0xA536:
                command = new DrawImageRotated180Scaled {
                    X = commandReader.ReadInt16(),
                    Y = commandReader.ReadInt16(),
                    ImageNumber = commandReader.ReadInt16(),
                    ImageSlot = commandReader.ReadInt16(),
                    Width = commandReader.ReadInt16(),
                    Height = commandReader.ReadInt16()
                };

                break;
            case 0xA5A7:
                command = new DrawImageRotated {
                    X = commandReader.ReadInt16(),
                    Y = commandReader.ReadInt16(),
                    ImageNumber = commandReader.ReadInt16(),
                    ImageSlot = commandReader.ReadInt16(),
                    Width = commandReader.ReadInt16(),
                    Height = commandReader.ReadInt16(),
                    Angle = commandReader.ReadUInt16()
                };

                break;
            case 0xA601:
                command = new DrawAreaFromBuffer {
                    BufferNumber = commandReader.ReadInt16()
                };

                break;
            case 0xB606:
                command = new CopyAreaBetweenBuffers {
                    X = commandReader.ReadInt16(),
                    Y = commandReader.ReadInt16(),
                    Width = commandReader.ReadInt16(),
                    Height = commandReader.ReadInt16(),
                    SourceBuffer = commandReader.ReadInt16(),
                    DestinationBuffer = commandReader.ReadInt16()
                };

                break;
            case 0xC02F:
                string soundFilename = ReadAlignedString(commandReader);
                command = new LoadSoundResource {
                    Filename = soundFilename
                };

                break;
            case 0xC031:
                command = new LoadSound {
                    SoundId = commandReader.ReadInt16()
                };

                break;
            case 0xC041:
                command = new StopSound {
                    SoundId = commandReader.ReadInt16()
                };

                break;
            case 0x1301:
            case 0xC051:
                command = new PlaySound {
                    SoundId = commandReader.ReadInt16()
                };

                break;
            case 0x1311:
            case 0xC061:
                command = new UnknownCommandC061 {
                    SoundId = commandReader.ReadInt16()
                };

                break;
            case 0xF01F:
                string screenFilename = ReadAlignedString(commandReader);
                command = new LoadScreenResource {
                    Filename = screenFilename
                };

                break;
            case 0xF02F:
                string imageFilename = ReadAlignedString(commandReader);
                command = new LoadImageResource {
                    Filename = imageFilename
                };

                break;
            case 0xF04F:
                string fontFilename = ReadAlignedString(commandReader);
                command = new LoadFontResource {
                    Filename = fontFilename
                };

                break;
            case 0xF05F:
                string paletteFilename = ReadAlignedString(commandReader);
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