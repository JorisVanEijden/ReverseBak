namespace ResourceExtraction.Extractors.Audio;

using GameData.Resources.Audio;
using ResourceExtraction.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class SoundExtractor : ExtractorBase<AudioResourceList> {
    public override AudioResourceList Extract(string id, Stream resourceStream) {
        var audioResourceList = new AudioResourceList();
        using var resourceReader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));

        Log($"Extracting {id}");

        string tag = ReadTag(resourceReader);
        if (tag != "SND") {
            throw new InvalidDataException($"Expected SND tag, got {tag}");
        }
        var fileSize = (int)(resourceReader.ReadUInt32() & 0x00FFFFFF);
        tag = ReadTag(resourceReader);
        if (tag != "INF") {
            throw new InvalidDataException($"Expected INF tag, got {tag}");
        }
        uint infBlockSize = resourceReader.ReadUInt32();
        ushort version = resourceReader.ReadUInt16();
        ushort nrOfSounds = resourceReader.ReadUInt16();
        byte unknownByte = resourceReader.ReadByte();
        Log($"Unknown byte: {unknownByte} (0x{unknownByte:X2})");
        var soundOffsetsMap = new Dictionary<int, long>();
        for (var i = 0; i < nrOfSounds; i++) {
            soundOffsetsMap[resourceReader.ReadUInt16()] = resourceReader.ReadInt32();
        }

        foreach (var soundOffset in soundOffsetsMap) {
            var audioResource = new AudioResource(soundOffset.Key.ToString());
            var offset = soundOffset.Value;
            resourceReader.BaseStream.Seek(offset, SeekOrigin.Begin);

            tag = ReadTag(resourceReader);
            if (tag != "DAT") {
                throw new InvalidDataException($"Expected DAT tag, got '{tag}'");
            }
            uint dataBlockSize = resourceReader.ReadUInt32();
            ushort soundId = resourceReader.ReadUInt16();
            audioResource.AudioType = soundId >= 1000 ? AudioType.Music : AudioType.SoundEffect;
            // Log($"Sound ID: {soundId} (0x{soundId:X4})");
            byte unknownByte2 = resourceReader.ReadByte();
            // Log($"Unknown byte 2: {unknownByte2} (0x{unknownByte2:X2})");
            byte unknownByte3 = resourceReader.ReadByte();
            // Log($"Unknown byte 3: {unknownByte3} (0x{unknownByte3:X2})");

            Log($"SoundId: {soundId} field_C: {unknownByte2:X2} field_12_flag: {unknownByte3:X2}");
            byte dataTypeAndFlags = resourceReader.ReadByte(); // always 00
            uint uncompressedSize = resourceReader.ReadUInt32();
            byte magicByte = resourceReader.ReadByte();
            if (magicByte != 0x84) {
                throw new InvalidDataException($"Expected magic byte 0x84, got {magicByte:X2}");
            }
            byte skipBytes = resourceReader.ReadByte(); // always 00, the code uses this to skip this amount of bytes before continuing the parsing
            long basePosition = resourceReader.BaseStream.Position;

            byte soundFormat;
            while ((soundFormat = resourceReader.ReadByte()) != 0xFF) {
                audioResource.Variants[soundFormat] = new AudioDataResource();
                var midiTrackChunks = new List<List<MidiEvent>>();
                byte markerByte = resourceReader.ReadByte();
                while (markerByte != 0xFF) {
                    byte unknownByte5 = resourceReader.ReadByte(); // always 00
                    ushort dataOffset = resourceReader.ReadUInt16();
                    ushort dataSize = resourceReader.ReadUInt16();
                    markerByte = resourceReader.ReadByte();
                    long savedPosition = resourceReader.BaseStream.Position;
                    if (basePosition + dataOffset > resourceReader.BaseStream.Length) {
                        throw new InvalidDataException(
                            $"unknownByte5: {unknownByte5} (0x{unknownByte5:X2}) dataOffset: {dataOffset} (0x{dataOffset:X4}) dataSize: {dataSize} (0x{dataSize:X4}) savedPosition: {savedPosition} basePosition: {basePosition} fileSize: {fileSize}");
                    }
                    resourceReader.BaseStream.Seek(basePosition + dataOffset, SeekOrigin.Begin);
                    byte channel = resourceReader.ReadByte();
                    byte flags = resourceReader.ReadByte();
                    audioResource.Variants[soundFormat].ChannelFlags[channel] = flags;
                    if (channel == 0xFE) {
                        byte[] wavData = AudioParser.ParseWave(resourceReader);
                        audioResource.Variants[soundFormat].WavData = wavData;
                    } else {
                        List<MidiEvent> trackChunk = AudioParser.ParseMidiTrack(resourceReader);
                        midiTrackChunks.Add(trackChunk);
                    }

                    resourceReader.BaseStream.Seek(savedPosition, SeekOrigin.Begin);
                    // Log($"{soundId}: {soundFormat:X2}_{soundEffect.SoundFormats[soundFormat].Count - 1}");
                }
                // If we collected any MIDI tracks, combine them into a single MIDI file
                if (midiTrackChunks.Count > 0) {
                    byte[] midiData = AudioParser.CombineMidiTracks(midiTrackChunks);
                    audioResource.Variants[soundFormat].MidiData = midiData;
                }
            }

            audioResourceList.AudioResources.Add(audioResource);
        }

        // At the end of the file there is a section of names that does not appear to be used by the game.
        while (resourceReader.ReadByte() != 0xFF) { } // Skip to next section
        Log($"{resourceReader.BaseStream.Position:X8}");
        tag = ReadTag(resourceReader);
        if (tag != "TAG") {
            throw new InvalidDataException($"Expected TAG tag, got {tag}");
        }
        var tagsLength = resourceReader.ReadUInt16();
        var unknown = resourceReader.ReadUInt16();
        var tagCount = resourceReader.ReadUInt16();
        for (var i = 0; i < tagCount; i++) {
            var tagId = resourceReader.ReadUInt16();
            var tagValue = resourceReader.ReadZeroTerminatedString();
            audioResourceList.AudioResources.Single(a => a.Id == tagId.ToString()).Name = tagValue;
            Log($"Tag {tagId}: {tagValue}");
        }

        return audioResourceList;
    }
}