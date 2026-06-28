namespace ResourceExtraction.Extractors.Audio;

using GameData.Resources.Audio;
using ResourceExtraction.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class AudioExtractor : ExtractorBase<AudioResource> {
    /// <summary>
    /// Extracts a single sound by its ID from the sound archive.
    /// </summary>
    /// <param name="resourceId"></param>
    /// <param name="resourceStream">The stream containing the sound archive</param>
    /// <returns>An AudioResource containing the extracted sound</returns>
    public override AudioResource Extract(string resourceId, Stream resourceStream) {
        using var resourceReader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage), leaveOpen: true);
        string tag;
        var audioResource = new AudioResource(resourceId);
        tag = resourceReader.ReadTag();
        if (tag != "DAT") {
            throw new InvalidDataException($"Expected DAT tag, got '{tag}' at position {resourceReader.BaseStream.Position:X8}");
        }
        uint dataBlockSize = resourceReader.ReadUInt32();
        ushort soundId = resourceReader.ReadUInt16();
        audioResource.AudioType = soundId >= 1000 ? AudioType.Music : AudioType.SoundEffect;
        // Log($"Sound ID: {soundId} (0x{soundId:X4})");
        byte fieldC = resourceReader.ReadByte();
        // Per-sound flags (RE audio_playSound_sub_seg067_D @0x35e6d): 0x01 = music, 0x02 = looping,
        // 0x10 = runtime "active looping" state (not stored in data). The original keeps a sound
        // looping while its 0x02 bit is set — that's how the intro theme repeats through the credits.
        byte field12Flag = resourceReader.ReadByte();
        audioResource.IsLooping = (field12Flag & 0x02) != 0;

        Log($"SoundId: {soundId} field_C: {fieldC:X2} field_12_flag: {field12Flag:X2}");
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

        return audioResource;
    }
}