namespace ResourceExtraction.Extractors;

using GameData.Resources.Audio;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Common;
using System.Collections.Generic;
using System.IO;
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
                audioResource.Variants[soundFormat] = new Dictionary<AudioFormat, byte[]>();
                var midiTrackChunks = new List<TrackChunk>();
                // Log($"Sound format: {soundFormat} (0x{soundFormat:X2})");
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

                    if (parsedSound is {AudioFileType: AudioFileType.Midi, MidiTrackChunk: not null}) {
                        // Collect MIDI track chunks for later combination
                        midiTrackChunks.Add(parsedSound.MidiTrackChunk);
                    } else if (parsedSound is {AudioFileType: AudioFileType.Wave, Data: not null}) {
                        // Store WAV data directly
                        audioResource.Variants[soundFormat][AudioFormat.Wav] = parsedSound.Data;
                    }

                    resourceReader.BaseStream.Seek(savedPosition, SeekOrigin.Begin);
                    // Log($"{soundId}: {soundFormat:X2}_{soundEffect.SoundFormats[soundFormat].Count - 1}");
                }
                // If we collected any MIDI tracks, combine them into a single MIDI file
                if (midiTrackChunks.Count > 0) {
                    byte[] midiData = AudioParser.CombineMidiTracks(midiTrackChunks);
                    audioResource.Variants[soundFormat][AudioFormat.Midi] = midiData;
                }
            }

            audioResourceList.AudioResources.Add(audioResource);
        }

        return audioResourceList;
    }
}

public class AudioParser {
    public static ParsedSound ParseSound(BinaryReader reader, int soundFormat, ushort dataSize) {
        var flags = reader.ReadByte();
        var channel = flags & 0x0F;

        return flags == 0xFE ? ParseWave(reader, soundFormat) : ParseMidiTrack(reader, soundFormat, channel);
    }

    // Renamed from ParseMidi to ParseMidiTrack to better reflect its purpose
    private static ParsedSound ParseMidiTrack(BinaryReader reader, int soundFormat, int channel) {
        // Read and discard the unknown byte
        byte unknownByte = reader.ReadByte();

        // Create a track chunk for the MIDI events
        var trackChunk = new TrackChunk();

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

            // Update absolute ticks position

            // Read status byte
            byte status = reader.ReadByte();

            // Handle running status
            if ((status & 0x80) == 0) {
                // This is actually a data byte, not a status byte
                byte dataByte1 = status;

                // Use the last status byte
                status = lastStatus;

                // Create MIDI event based on the event type
                byte eventType = (byte)(status & 0xF0);
                byte channelByte = (byte)(status & 0x0F);

                switch (eventType) {
                    case 0x80: // Note Off
                        trackChunk.Events.Add(new NoteOffEvent((SevenBitNumber)dataByte1, (SevenBitNumber)reader.ReadByte()) {
                            Channel = (FourBitNumber)channelByte,
                            DeltaTime = deltaTime
                        });

                        break;

                    case 0x90: // Note On
                        byte velocity = reader.ReadByte();
                        trackChunk.Events.Add(new NoteOnEvent((SevenBitNumber)dataByte1, (SevenBitNumber)velocity) {
                            Channel = (FourBitNumber)channelByte,
                            DeltaTime = deltaTime
                        });

                        break;

                    case 0xA0: // Polyphonic Key Pressure
                        trackChunk.Events.Add(new NoteAftertouchEvent((SevenBitNumber)dataByte1, (SevenBitNumber)reader.ReadByte()) {
                            Channel = (FourBitNumber)channelByte,
                            DeltaTime = deltaTime
                        });

                        break;

                    case 0xB0: // Control Change
                        trackChunk.Events.Add(new ControlChangeEvent((SevenBitNumber)dataByte1, (SevenBitNumber)reader.ReadByte()) {
                            Channel = (FourBitNumber)channelByte,
                            DeltaTime = deltaTime
                        });

                        break;

                    case 0xC0: // Program Change
                        trackChunk.Events.Add(new ProgramChangeEvent((SevenBitNumber)dataByte1) {
                            Channel = (FourBitNumber)channelByte,
                            DeltaTime = deltaTime
                        });

                        break;

                    case 0xD0: // Channel Pressure
                        trackChunk.Events.Add(new ChannelAftertouchEvent((SevenBitNumber)dataByte1) {
                            Channel = (FourBitNumber)channelByte,
                            DeltaTime = deltaTime
                        });

                        break;

                    case 0xE0: // Pitch Bend
                        byte lsb = dataByte1;
                        byte msb = reader.ReadByte();
                        ushort pitchBendValue = (ushort)((msb << 7) | lsb);
                        trackChunk.Events.Add(new PitchBendEvent(pitchBendValue) {
                            Channel = (FourBitNumber)channelByte,
                            DeltaTime = deltaTime
                        });

                        break;
                }

                continue;
            }

            // Update last status for running status
            lastStatus = status;

            // Handle different MIDI events
            byte statusEventType = (byte)(status & 0xF0);
            byte statusChannel = (byte)(status & 0x0F);

            switch (statusEventType) {
                case 0x80: // Note Off
                    trackChunk.Events.Add(new NoteOffEvent((SevenBitNumber)reader.ReadByte(), (SevenBitNumber)reader.ReadByte()) {
                        Channel = (FourBitNumber)statusChannel,
                        DeltaTime = deltaTime
                    });

                    break;

                case 0x90: // Note On
                    byte noteNumber = reader.ReadByte();
                    byte velocity = reader.ReadByte();
                    trackChunk.Events.Add(new NoteOnEvent((SevenBitNumber)noteNumber, (SevenBitNumber)velocity) {
                        Channel = (FourBitNumber)statusChannel,
                        DeltaTime = deltaTime
                    });

                    break;

                case 0xA0: // Polyphonic Key Pressure
                    trackChunk.Events.Add(new NoteAftertouchEvent((SevenBitNumber)reader.ReadByte(), (SevenBitNumber)reader.ReadByte()) {
                        Channel = (FourBitNumber)statusChannel,
                        DeltaTime = deltaTime
                    });

                    break;

                case 0xB0: // Control Change
                    trackChunk.Events.Add(new ControlChangeEvent((SevenBitNumber)reader.ReadByte(), (SevenBitNumber)reader.ReadByte()) {
                        Channel = (FourBitNumber)statusChannel,
                        DeltaTime = deltaTime
                    });

                    break;

                case 0xC0: // Program Change
                    trackChunk.Events.Add(new ProgramChangeEvent((SevenBitNumber)reader.ReadByte()) {
                        Channel = (FourBitNumber)statusChannel,
                        DeltaTime = deltaTime
                    });

                    break;

                case 0xD0: // Channel Pressure
                    trackChunk.Events.Add(new ChannelAftertouchEvent((SevenBitNumber)reader.ReadByte()) {
                        Channel = (FourBitNumber)statusChannel,
                        DeltaTime = deltaTime
                    });

                    break;

                case 0xE0: // Pitch Bend
                    byte lsb = reader.ReadByte();
                    byte msb = reader.ReadByte();
                    ushort pitchBendValue = (ushort)((msb << 7) | lsb);
                    trackChunk.Events.Add(new PitchBendEvent(pitchBendValue) {
                        Channel = (FourBitNumber)statusChannel,
                        DeltaTime = deltaTime
                    });

                    break;

                case 0xF0: // System messages
                    if (status == 0xFC) {
                        // STOP
                        endOfTrack = true;
                    } else {
                        // For other system messages, add them as meta events or sysex
                        if (status == 0xF0) {
                            // SysEx
                            var sysExData = new List<byte>();
                            byte b;
                            while ((b = reader.ReadByte()) != 0xF7) {
                                sysExData.Add(b);
                            }
                            trackChunk.Events.Add(new NormalSysExEvent(sysExData.ToArray()) {
                                DeltaTime = deltaTime
                            });
                        } else {
                            // Other system common messages
                            // These are typically not stored in a MIDI file, but we'll handle them as meta events
                            // For simplicity, we'll just add a marker event
                            trackChunk.Events.Add(new MarkerEvent($"System message 0x{status:X2}") {
                                DeltaTime = deltaTime
                            });
                        }
                    }

                    break;
            }
        }

        // Return the track chunk as part of a ParsedSound object
        return new ParsedSound {
            SoundFormat = soundFormat,
            Channel = channel,
            MidiTrackChunk = trackChunk, // Store the track chunk instead of raw bytes
            AudioFileType = AudioFileType.Midi
        };
    }

    // This method combines multiple MIDI track chunks into a single MIDI file
    public static byte[] CombineMidiTracks(IEnumerable<TrackChunk> trackChunks) {
        // Create a new MIDI file
        var midiFile = new MidiFile {
            // Set the time division (ticks per quarter note)
            TimeDivision = new TicksPerQuarterNoteTimeDivision(32)
        };

        // Add all track chunks to the MIDI file
        foreach (var trackChunk in trackChunks) {
            midiFile.Chunks.Add(trackChunk);
        }

        // Convert the MIDI file to a byte array
        using var memoryStream = new MemoryStream();
        midiFile.Write(memoryStream);

        return memoryStream.ToArray();
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
        writer.Write((int)sampleRate); // Sample rate
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
    public byte[]? Data { get; set; }
    public AudioFileType AudioFileType { get; set; }
    public TrackChunk? MidiTrackChunk { get; set; }
}

public enum AudioFileType {
    Unknown,
    Midi,
    Wave
}