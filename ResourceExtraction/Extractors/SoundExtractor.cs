namespace ResourceExtraction.Extractors;

using GameData.Resources.Audio;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class SoundExtractor : ExtractorBase<SoundEffectList> {
    public override SoundEffectList Extract(string id, Stream resourceStream) {
        var soundEffectList = new SoundEffectList();
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
            var soundEffect = new SoundEffect(soundOffset.Key.ToString());
            var offset = soundOffset.Value;
            resourceReader.BaseStream.Seek(offset, SeekOrigin.Begin);

            tag = ReadTag(resourceReader);
            if (tag != "DAT") {
                throw new InvalidDataException($"Expected DAT tag, got '{tag}'");
            }
            uint dataBlockSize = resourceReader.ReadUInt32();
            ushort soundId = resourceReader.ReadUInt16();
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
                // Log($"Sound format: {soundFormat} (0x{soundFormat:X2})");
                soundEffect.SoundFormats[soundFormat] = [];
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
                    var parsedSound = AudioParser.ParseSound(resourceReader, soundFormat, dataSize);
                    var rawData = parsedSound.Data;
                    soundEffect.SoundFormats[soundFormat].Add(rawData);
                    resourceReader.BaseStream.Seek(savedPosition, SeekOrigin.Begin);
                    // Log($"{soundId}: {soundFormat:X2}_{soundEffect.SoundFormats[soundFormat].Count - 1}");
                }
            }
            soundEffectList.SoundEffects.Add(soundEffect);
        }

        return soundEffectList;
    }
}

public class AudioParser {
    public static ParsedSound ParseSound(BinaryReader reader, int soundFormat, ushort dataSize) {
        var flags = reader.ReadByte();
        var channel = flags & 0x0F;

        return flags == 0xFE ? ParseWave(reader, soundFormat) : ParseMidi(reader, soundFormat, channel, dataSize);
    }

    private static ParsedSound ParseMidi(BinaryReader reader, int soundFormat, int channel, int dataSize) {
        // Read and discard the unknown byte
        byte unknownByte = reader.ReadByte();

        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);

        // SMF Header Chunk
        writer.Write("MThd"u8.ToArray()); // Chunk type
        WriteBigEndianUInt32(writer, 6); // Chunk length (always 6 for header)
        // Write format, tracks, and division in Big-Endian format
        WriteBigEndianUInt16(writer, 0); // Format 0 (single track)
        WriteBigEndianUInt16(writer, 1); // Number of tracks (1)
        WriteBigEndianUInt16(writer, 32); // Ticks per quarter note (32 for this game)

        // Track Chunk
        long trackStartPos = output.Position;
        writer.Write("MTrk"u8.ToArray()); // Track chunk type
        writer.Write(0); // Placeholder for track length

        // Process MIDI events
        byte lastStatus = 0;
        bool endOfTrack = false;

        while (!endOfTrack && reader.BaseStream.Position < reader.BaseStream.Length) {
            // Read delta time (variable length)
            int deltaTime = 0;
            while (true) {
                byte delayByte = reader.ReadByte();
                if (delayByte == 0xF8) {
                    deltaTime += 240;
                } else {
                    deltaTime += delayByte;

                    break;
                }
            }

            // Write variable-length delta time
            WriteVariableLength(writer, (uint)deltaTime);

            // Read status byte
            byte status = reader.ReadByte();

            // Handle running status
            if ((status & 0x80) == 0) {
                // Running status - use previous status and this is the first data byte
                // writer.Write(lastStatus);
                writer.Write(status);

                // Write second data byte if needed (except for program change and channel aftertouch)
                if ((lastStatus & 0xE0) != 0xC0 && (lastStatus & 0xE0) != 0xD0) {
                    writer.Write(reader.ReadByte());
                }

                continue;
            }

            // Update last status for running status
            // lastStatus = status;

            // Handle different MIDI events
            byte eventType = (byte)(status & 0xF0);
            switch (eventType) {
                case 0x80: // Note Off
                case 0x90: // Note On
                case 0xA0: // Polyphonic Key Pressure
                case 0xB0: // Control Change
                case 0xE0: // Pitch Bend
                    writer.Write(status);
                    writer.Write(reader.ReadBytes(2)); // 2 data bytes
                    break;

                case 0xC0: // Program Change
                case 0xD0: // Channel Pressure
                    writer.Write(status);
                    writer.Write(reader.ReadByte()); // 1 data byte

                    break;

                case 0xF0: // System messages
                    if (status == 0xFC) {
                        // STOP
                        writer.Write((byte)0xFF); // End of track event
                        writer.Write((byte)0x2F); // End of track event
                        writer.Write((byte)0x00); // End of track event
                        endOfTrack = true;
                    } else {
                        writer.Write(status); // Write the status byte
                    }

                    // 0xFF (SYSTEM RESET) and others are just passed through as single-byte messages
                    break;
            }
        }

        // Update track length in the header
        long endPos = output.Position;
        output.Position = trackStartPos + 4; // Position of track length field
        WriteBigEndianUInt32(writer, (uint)(endPos - trackStartPos - 8)); // Length of track data in Big-Endian
        output.Position = endPos;

        // byte[] midiData = reader.ReadBytes(dataSize - 5);
        // writer.Write(midiData);
        // writer.Write((byte)0x04); // End of track event
        // writer.Write((byte)0xFF); // End of track event
        // writer.Write((byte)0x2F); // End of track event
        // writer.Write((byte)0x00); // End of track event

        return new ParsedSound {
            SoundFormat = soundFormat,
            Channel = channel,
            Data = output.ToArray(),
            AudioFileType = AudioFileType.Midi
        };
    }

    // Helper method to write a UInt16 in Big-Endian format
    private static void WriteBigEndianUInt16(BinaryWriter writer, ushort value) {
        writer.Write((byte)((value >> 8) & 0xFF)); // High byte
        writer.Write((byte)(value & 0xFF)); // Low byte
    }

    // Helper method to write a UInt32 in Big-Endian format
    private static void WriteBigEndianUInt32(BinaryWriter writer, uint value) {
        writer.Write((byte)((value >> 24) & 0xFF)); // Highest byte
        writer.Write((byte)((value >> 16) & 0xFF));
        writer.Write((byte)((value >> 8) & 0xFF));
        writer.Write((byte)(value & 0xFF)); // Lowest byte
    }

    private static void WriteVariableLength(BinaryWriter writer, uint value) {
        var buffer = new byte[4];
        int index = 0;

        buffer[index] = (byte)(value & 0x7F);
        value >>= 7;

        while (value != 0) {
            index++;
            buffer[index] = (byte)((value & 0x7F) | 0x80);
            value >>= 7;
        }

        // Write in reverse order (MSB first)
        for (int i = index; i >= 0; i--) {
            writer.Write(buffer[i]);
        }
    }

    private static ParsedSound ParseWave(BinaryReader reader, int soundFormat) {
        // Read the unknown byte
        byte unknownByte = reader.ReadByte();

        // Read the wave parameters
        ushort sampleRate = reader.ReadUInt16();
        uint dataSize = reader.ReadUInt32();
        ushort unknown1 = reader.ReadUInt16();

        // Read the wave data
        byte[] waveData = reader.ReadBytes((int)dataSize);

        // Create the output stream
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);

        // RIFF header
        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataSize); // File size - 8
        writer.Write("WAVE"u8.ToArray());

        // fmt chunk
        writer.Write("fmt "u8.ToArray());
        writer.Write(16); // Chunk size
        writer.Write((ushort)1); // Audio format (1 = PCM)
        writer.Write((ushort)1); // Num channels (1 = mono)
        writer.Write(sampleRate); // Sample rate
        writer.Write(sampleRate * 1 * 8 / 8); // Byte rate (SampleRate * NumChannels * BitsPerSample/8)
        writer.Write((ushort)(1 * 8 / 8)); // Block align (NumChannels * BitsPerSample/8)
        writer.Write((ushort)8); // Bits per sample (8-bit PCM)

        // data chunk
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);
        writer.Write(waveData);

        return new ParsedSound {
            SoundFormat = soundFormat,
            Channel = 0, // Not applicable for WAV
            Data = output.ToArray(),
            AudioFileType = AudioFileType.Wave
        };
    }
}

public class ParsedSound {
    public int SoundFormat { get; set; }
    public int Channel { get; set; }
    public byte[] Data { get; set; }
    public AudioFileType AudioFileType { get; set; }
}

public enum AudioFileType {
    Unknown,
    Midi,
    Wave
}