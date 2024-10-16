namespace ResourceExtraction.Assemblers;

using GameData.Resources.Animation;
using GameData.Resources.Animation.Commands;
using ResourceExtractor.Compression;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

public class TtmAssembler {
    private const string DosCodePage = "ibm437";

    public static void Assemble(AnimatorScene scene, string filePath) {
        using var fileStream = new FileStream(filePath, FileMode.Create);
        using var writer = new BinaryWriter(fileStream, Encoding.GetEncoding(DosCodePage));

        // Write VER tag
        WriteTag(writer, "VER");
        var versionBytes = Encoding.ASCII.GetBytes(scene.Version);
        writer.Write((ushort)(versionBytes.Length + 1)); // +1 for null terminator
        writer.Write((ushort)0); // Unknown
        writer.Write(versionBytes);
        writer.Write((byte)0); // Null terminator for version string

        // Write PAG tag
        WriteTag(writer, "PAG");
        writer.Write((ushort)2); // Size of the PAG section
        writer.Write((ushort)0); // Unknown
        writer.Write(scene.NumberOfFrames);

        // Write TT3 tag
        WriteTag(writer, "TT3");
        var scriptData = ConstructScriptData(scene);
        writer.Write((ushort)(scriptData.Length + 5)); // Size of the TT3 section including header
        writer.Write((ushort)0); // Unknown
        writer.Write((byte)CompressionType.None); // Compression type
        writer.Write(scriptData.Length); // Decompressed size is the actual size since no compression
        writer.Write(scriptData);

        // Length TTI section
        WriteTag(writer, "TTI");
        long ttiSectionSizePosition = writer.BaseStream.Position;
        writer.Write((ushort)0); // Placeholder for TTI section size
        writer.Write((ushort)0x8000); // Unknown

        // Write TAG tag once for all entries
        WriteTag(writer, "TAG");
        long tagSectionSizePosition = writer.BaseStream.Position;
        writer.Write((ushort)0); // Placeholder for TAG section size
        writer.Write((ushort)0); // Unknown
        writer.Write((ushort)scene.Tags.Count); // Number of tags

        foreach (var tagEntry in scene.Tags) {
            writer.Write((ushort)tagEntry.Key); // Tag ID
            var tagNameBytes = Encoding.ASCII.GetBytes(tagEntry.Value + "\0");
            writer.Write(tagNameBytes);
        }

        // Update TAG section size
        long tagSectionEndPosition = writer.BaseStream.Position;
        writer.BaseStream.Position = tagSectionSizePosition;
        writer.Write((ushort)(tagSectionEndPosition - tagSectionSizePosition - 2));
        writer.BaseStream.Position = tagSectionEndPosition;

        // Update TTI section size
        long ttiSectionEndPosition = writer.BaseStream.Position;
        writer.BaseStream.Position = ttiSectionSizePosition;
        writer.Write((ushort)(ttiSectionEndPosition - ttiSectionSizePosition - 4));
        writer.BaseStream.Position = ttiSectionEndPosition;

        // Finalize
        writer.Flush();
    }

    private static void WriteTag(BinaryWriter writer, string tag) {
        if (tag.Length != 3) throw new ArgumentException("Tag must be exactly 3 characters long.");
        writer.Write(Encoding.ASCII.GetBytes(tag + ':'));
    }

    public static byte[] ConstructScriptData(AnimatorScene scene) {
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream, Encoding.UTF8, true);

        foreach (List<FrameCommand>? frame in scene.Frames) {
            foreach (var command in frame) {
                var id = ushort.Parse(command.Id, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                writer.Write(id); // Write command ID

                switch (command) {
                    // These commands have no arguments
                    case StoreScreen:
                    case DisposeCurrentPalette:
                    case DisposeCurrentBitmap:
                    case DisposeTargetBuffer:
                    case EndScene:
                    case UnknownCommand0400:
                    case ObsoleteCommand0500:
                    case ObsoleteCommand0510:

                        break;
                    case SetFramesDuration setDelay:
                        writer.Write((ushort)setDelay.Amount);

                        break;
                    case SelectImageSlot selectImageSlot:
                        writer.Write((ushort)selectImageSlot.SlotNumber);

                        break;
                    case SelectPaletteSlot selectPaletteSlot:
                        writer.Write((ushort)selectPaletteSlot.SlotNumber);

                        break;
                    case SelectFontSlot selectFontSlot:
                        writer.Write((ushort)selectFontSlot.SlotNumber);

                        break;
                    case TagFrame tagFrame:
                        writer.Write((ushort)tagFrame.SceneNumber);

                        break;
                    case SetTargetBuffer setActiveBuffer:
                        writer.Write((ushort)setActiveBuffer.BufferNumber);

                        break;
                    case GotoFrame gotoFrame:
                        writer.Write((ushort)gotoFrame.NextFrame);

                        break;
                    case SetColors setColors:
                        writer.Write((ushort)setColors.ForegroundColor);
                        writer.Write((ushort)setColors.BackgroundColor);

                        break;
                    case DialogCommand dialogCommand:
                        writer.Write((ushort)dialogCommand.Dialog16Id);
                        writer.Write((ushort)dialogCommand.Arg2);

                        break;
                    case SetRange1 setRange1:
                        writer.Write((ushort)setRange1.Start);
                        writer.Write((ushort)setRange1.End);

                        break;
                    case SetRange2 setRange2:
                        writer.Write((ushort)setRange2.Start);
                        writer.Write((ushort)setRange2.End);

                        break;
                    case SetRange3 setRange3:
                        writer.Write((ushort)setRange3.Start);
                        writer.Write((ushort)setRange3.End);

                        break;
                    case UnknownCommand2402 unknownCommand2402:
                        writer.Write((ushort)unknownCommand2402.Range);
                        writer.Write((ushort)unknownCommand2402.Arg2);

                        break;
                    case SetClipArea setClipArea:
                        writer.Write((ushort)setClipArea.X);
                        writer.Write((ushort)setClipArea.Y);
                        writer.Write((ushort)setClipArea.Width);
                        writer.Write((ushort)setClipArea.Height);

                        break;
                    case FadeOut fadeOut:
                        writer.Write((ushort)fadeOut.Length);
                        writer.Write((ushort)fadeOut.Start);
                        writer.Write((ushort)fadeOut.Color);
                        writer.Write((ushort)fadeOut.Speed);

                        break;
                    case FadeIn fadeIn:
                        writer.Write((ushort)fadeIn.Start);
                        writer.Write((ushort)fadeIn.Length);
                        writer.Write((ushort)fadeIn.Color);
                        writer.Write((ushort)fadeIn.Speed);

                        break;
                    case StoreArea storeArea:
                        writer.Write((ushort)storeArea.X);
                        writer.Write((ushort)storeArea.Y);
                        writer.Write((ushort)storeArea.Width);
                        writer.Write((ushort)storeArea.Height);

                        break;
                    case CopyToTargetBuffer copyToCurrentBuffer:
                        writer.Write((ushort)copyToCurrentBuffer.X);
                        writer.Write((ushort)copyToCurrentBuffer.Y);
                        writer.Write((ushort)copyToCurrentBuffer.Width);
                        writer.Write((ushort)copyToCurrentBuffer.Height);

                        break;
                    case UnknownCommandA014 unknownCommandA014:
                        writer.Write((ushort)unknownCommandA014.Arg1);
                        writer.Write((ushort)unknownCommandA014.Arg2);
                        writer.Write((ushort)unknownCommandA014.Arg3);
                        writer.Write((ushort)unknownCommandA014.Arg4);

                        break;
                    case UnknownCommandA034 unknownCommandA034:
                        writer.Write((ushort)unknownCommandA034.Arg1);
                        writer.Write((ushort)unknownCommandA034.Arg2);
                        writer.Write((ushort)unknownCommandA034.Arg3);
                        writer.Write((ushort)unknownCommandA034.Arg4);

                        break;
                    case UnknownCommandA094 unknownCommandA094:
                        writer.Write((ushort)unknownCommandA094.Arg1);
                        writer.Write((ushort)unknownCommandA094.Arg2);
                        writer.Write((ushort)unknownCommandA094.Arg3);
                        writer.Write((ushort)unknownCommandA094.Arg4);

                        break;
                    case UnknownCommandA0B5 unknownCommandA0B5:
                        writer.Write((ushort)unknownCommandA0B5.Arg1);
                        writer.Write((ushort)unknownCommandA0B5.Arg2);
                        writer.Write((ushort)unknownCommandA0B5.Arg3);
                        writer.Write((ushort)unknownCommandA0B5.Arg4);

                        break;

                    case FillArea clearArea:
                        writer.Write((ushort)clearArea.X);
                        writer.Write((ushort)clearArea.Y);
                        writer.Write((ushort)clearArea.Width);
                        writer.Write((ushort)clearArea.Height);

                        break;
                    case DrawBorder unknownCommandA114:
                        writer.Write((ushort)unknownCommandA114.X);
                        writer.Write((ushort)unknownCommandA114.Y);
                        writer.Write((ushort)unknownCommandA114.Width);
                        writer.Write((ushort)unknownCommandA114.Height);

                        break;

                    case DrawImage drawImage:
                        writer.Write((ushort)drawImage.X);
                        writer.Write((ushort)drawImage.Y);
                        writer.Write((ushort)drawImage.ImageNumber);
                        writer.Write((ushort)drawImage.ImageSlot);

                        break;
                    case DrawImageScaled drawImageScaled:
                        writer.Write((ushort)drawImageScaled.X);
                        writer.Write((ushort)drawImageScaled.Y);
                        writer.Write((ushort)drawImageScaled.ImageNumber);
                        writer.Write((ushort)drawImageScaled.ImageSlot);
                        writer.Write((ushort)drawImageScaled.Width);
                        writer.Write((ushort)drawImageScaled.Height);

                        break;
                    case DrawImageFlippedVertically drawImageFlippedVertically:
                        writer.Write((ushort)drawImageFlippedVertically.X);
                        writer.Write((ushort)drawImageFlippedVertically.Y);
                        writer.Write((ushort)drawImageFlippedVertically.ImageNumber);
                        writer.Write((ushort)drawImageFlippedVertically.ImageSlot);

                        break;
                    case DrawImageFlippedVerticallyScaled drawImageFlippedVerticallyScaled:
                        writer.Write((ushort)drawImageFlippedVerticallyScaled.X);
                        writer.Write((ushort)drawImageFlippedVerticallyScaled.Y);
                        writer.Write((ushort)drawImageFlippedVerticallyScaled.ImageNumber);
                        writer.Write((ushort)drawImageFlippedVerticallyScaled.ImageSlot);
                        writer.Write((ushort)drawImageFlippedVerticallyScaled.Width);
                        writer.Write((ushort)drawImageFlippedVerticallyScaled.Height);

                        break;
                    case DrawImageFlippedHorizontally drawImageFlippedHorizontally:
                        writer.Write((ushort)drawImageFlippedHorizontally.X);
                        writer.Write((ushort)drawImageFlippedHorizontally.Y);
                        writer.Write((ushort)drawImageFlippedHorizontally.ImageNumber);
                        writer.Write((ushort)drawImageFlippedHorizontally.ImageSlot);

                        break;
                    case DrawImageFlippedHorizontallyScaled drawImageFlippedHorizontallyScaled:
                        writer.Write((ushort)drawImageFlippedHorizontallyScaled.X);
                        writer.Write((ushort)drawImageFlippedHorizontallyScaled.Y);
                        writer.Write((ushort)drawImageFlippedHorizontallyScaled.ImageNumber);
                        writer.Write((ushort)drawImageFlippedHorizontallyScaled.ImageSlot);
                        writer.Write((ushort)drawImageFlippedHorizontallyScaled.Width);
                        writer.Write((ushort)drawImageFlippedHorizontallyScaled.Height);

                        break;
                    case DrawImageRotated180 drawImageRotated180:
                        writer.Write((ushort)drawImageRotated180.X);
                        writer.Write((ushort)drawImageRotated180.Y);
                        writer.Write((ushort)drawImageRotated180.ImageNumber);
                        writer.Write((ushort)drawImageRotated180.ImageSlot);

                        break;
                    case DrawImageRotated180Scaled drawImageRotated180Scaled:
                        writer.Write((ushort)drawImageRotated180Scaled.X);
                        writer.Write((ushort)drawImageRotated180Scaled.Y);
                        writer.Write((ushort)drawImageRotated180Scaled.ImageNumber);
                        writer.Write((ushort)drawImageRotated180Scaled.ImageSlot);
                        writer.Write((ushort)drawImageRotated180Scaled.Width);
                        writer.Write((ushort)drawImageRotated180Scaled.Height);

                        break;
                    case UnknownCommandA5A7 unknownCommandA5A7:
                        writer.Write((ushort)unknownCommandA5A7.Arg1);
                        writer.Write((ushort)unknownCommandA5A7.Arg2);
                        writer.Write((ushort)unknownCommandA5A7.Arg3);
                        writer.Write((ushort)unknownCommandA5A7.Arg4);
                        writer.Write((ushort)unknownCommandA5A7.Arg5);
                        writer.Write((ushort)unknownCommandA5A7.Arg6);
                        writer.Write((ushort)unknownCommandA5A7.Arg7);

                        break;
                    case DrawAreaFromBuffer drawBuffer:
                        writer.Write((ushort)drawBuffer.BufferNumber);

                        break;
                    case CopyAreaBetweenBuffers copyAreaBetweenBuffers:
                        writer.Write((ushort)copyAreaBetweenBuffers.X);
                        writer.Write((ushort)copyAreaBetweenBuffers.Y);
                        writer.Write((ushort)copyAreaBetweenBuffers.Width);
                        writer.Write((ushort)copyAreaBetweenBuffers.Height);
                        writer.Write((ushort)copyAreaBetweenBuffers.SourceBuffer);
                        writer.Write((ushort)copyAreaBetweenBuffers.DestinationBuffer);

                        break;
                    case LoadSoundResource loadSoundResource:
                        WriteAlignedString(loadSoundResource.Filename, writer);

                        break;
                    case LoadSound loadSound:
                        writer.Write((ushort)loadSound.SoundId);

                        break;
                    case StopSound stopSound:
                        writer.Write((ushort)stopSound.SoundId);

                        break;
                    case PlaySound playSound:
                        writer.Write((ushort)playSound.SoundId);

                        break;
                    case UnknownCommandC061 unknownCommandC061:
                        writer.Write((ushort)unknownCommandC061.SoundId);

                        break;
                    case LoadScreenResource loadScreenResource:
                        WriteAlignedString(loadScreenResource.Filename, writer);

                        break;
                    case LoadImageResource loadImageResource:
                        WriteAlignedString(loadImageResource.Filename, writer);

                        break;
                    case LoadFontResource loadFontResource:
                        WriteAlignedString(loadFontResource.Filename, writer);

                        break;
                    case LoadPaletteResource loadPaletteResource:
                        WriteAlignedString(loadPaletteResource.Filename, writer);

                        break;
                }
            }
            // Start of frame marker
            writer.Write((ushort)0x0FF0);
        }

        return memoryStream.ToArray();
    }

    private static void WriteAlignedString(string s, BinaryWriter writer) {
        byte[] bytes = Encoding.ASCII.GetBytes(s);
        writer.Write(bytes);
        writer.Write((byte)0);
        if (bytes.Length % 2 == 0) {
            writer.Write((byte)0);
        }
    }
}