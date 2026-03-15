namespace ResourceExtraction.Extractors.Audio;

using ResourceExtraction.Extensions;
using System.Collections.Generic;
using System.IO;

public class AudioParser {
    private const byte NoteOn = 0x90;
    private const byte ControlChange = 0xB0;
    private const byte PitchBend = 0xE0;
    private const byte ProgramChange = 0xC0;

    public static List<MidiEvent> ParseMidiTrack(BinaryReader reader) {
        var midiEvents = new List<MidiEvent>();
        byte lastStatus = 0;
        while (reader.BaseStream.Position < reader.BaseStream.Length) {
            int deltaTime = ReadDeltaTime(reader);
            byte peekResult = reader.PeekByte();
            byte status = (peekResult & 0x80) == 0 ? lastStatus : reader.ReadByte();
            if (status == 0xFC) {
                break;
            }
            lastStatus = status;
            byte[]? data = ReadMidiEventData(reader, status);

            if (data == null)
                continue;
            var midiEvent = new MidiEvent {
                DeltaTime = deltaTime,
                Status = status,
                Data = data
            };
            midiEvents.Add(midiEvent);
        }

        return midiEvents;
    }

    private static int ReadDeltaTime(BinaryReader reader) {
        var deltaTime = 0;
        while (true) {
            byte delayByte = reader.ReadByte();
            if (delayByte == 0xF8) {
                deltaTime += 240;
            } else {
                deltaTime += delayByte;

                break;
            }
        }

        return deltaTime;
    }

    private static byte[]? ReadMidiEventData(BinaryReader reader, byte status) {
        var statusEventType = (byte)(status & 0xF0);

        return statusEventType switch {
            NoteOn or ControlChange or PitchBend => [reader.ReadByte(), reader.ReadByte()],
            ProgramChange => [reader.ReadByte()],
            _ => null
        };
    }

    public static byte[] CombineMidiTracks(List<List<MidiEvent>> trackChunks) {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        // Write MIDI header: MThd, header length, format type, nTracks, division
        writer.Write("MThd"u8.ToArray());
        writer.Write(SwapEndian(6)); // header length
        writer.Write(SwapEndian((short)1)); // format type 1
        writer.Write(SwapEndian((short)trackChunks.Count)); // number of tracks
        writer.Write(SwapEndian((short)32)); // division (ticks per quarter note)
        foreach (List<MidiEvent>? events in trackChunks) {
            WriteTrackChunk(writer, events);
        }

        return ms.ToArray();
    }

    private static void WriteTrackChunk(BinaryWriter writer, List<MidiEvent> events) {
        using var trackMs = new MemoryStream();
        using var trackWriter = new BinaryWriter(trackMs);
        byte lastStatus = 0;
        foreach (var midiEvent in events) {
            WriteVariableLength(trackWriter, midiEvent.DeltaTime);
            if (midiEvent.Status != lastStatus) {
                trackWriter.Write(midiEvent.Status);
                lastStatus = midiEvent.Status;
            }
            trackWriter.Write(midiEvent.Data);
        }
        // Write End of Track meta event
        trackWriter.Write((byte)0x00);
        trackWriter.Write((byte)0xFF);
        trackWriter.Write((byte)0x2F);
        trackWriter.Write((byte)0x00);
        // Write chunk header
        writer.Write("MTrk"u8.ToArray());
        writer.Write(SwapEndian((int)trackMs.Length));
        writer.Write(trackMs.ToArray());
    }

    private static void WriteVariableLength(BinaryWriter writer, int value) {
        var buffer = (uint)value;
        var bytes = new byte[4];
        var count = 0;

        bytes[count++] = (byte)(buffer & 0x7F);
        buffer >>= 7;
        while (buffer > 0) {
            bytes[count++] = (byte)(buffer & 0x7F | 0x80);
            buffer >>= 7;
        }
        // Write bytes in reverse order
        for (int i = count - 1; i >= 0; i--) {
            writer.Write(bytes[i]);
        }
    }

    private static int SwapEndian(int value) => (value & 0xFF) << 24 | (value & 0xFF00) << 8 | (value & 0xFF0000) >> 8 | value >> 24 & 0xFF;
    private static short SwapEndian(short value) => (short)((value & 0xFF) << 8 | value >> 8 & 0xFF);

    public static byte[] ParseWave(BinaryReader reader) {
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
        writer.Write((int)sampleRate); // Sample rate
        writer.Write(sampleRate * 1 * 8 / 8); // Byte rate (SampleRate * NumChannels * BitsPerSample/8)
        writer.Write((ushort)(1 * 8 / 8)); // Block align (NumChannels * BitsPerSample/8)
        writer.Write((ushort)8); // Bits per sample (8-bit PCM)

        // data chunk
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);
        writer.Write(waveData);

        return output.ToArray();
    }
}