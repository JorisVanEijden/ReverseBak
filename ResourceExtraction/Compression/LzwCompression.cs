namespace ResourceExtraction.Compression;

using System;
using System.Collections.Generic;
using System.IO;

public class LzwCompression : ICompression {
    private readonly Dictionary<int, List<byte>> _dictionary = new();
    private int _bitBuffer;
    private int _bitLength = 9;
    private int _bitsProcessed;
    private long _endPosition;

    public Stream Compress(Stream inputStream) {
        throw new NotImplementedException();
    }

    public Stream Decompress(Stream inputStream, long endPosition = 0) {
        var outputStream = new MemoryStream();
        var inputReader = new BinaryReader(inputStream);
        var outputWriter = new BinaryWriter(outputStream);

        _endPosition = endPosition == 0 ? inputStream.Length : endPosition;
        _bitBuffer = 0;
        _bitsProcessed = 0;
        _bitLength = 9;

        // Console.WriteLine($"Performing LZW decompression from 0x{inputStream.Position:X4} to 0x{endPosition:X4} = {endPosition - inputStream.Position} bytes");

        // Initialize the dictionary with single bytes
        Reset();
        int resetCode = _dictionary.Count;
        int dictionaryIndex = _dictionary.Count + 1;
        int bitPosition = 0;

        // Read the first code
        int currentCode = ReadNextCode(inputReader);
        byte character = (byte)currentCode;
        outputWriter.Write(character);

        var previousEntry = new List<byte> {
            character
        };

        while (currentCode >= 0) {
            currentCode = ReadNextCode(inputReader);
            if (currentCode < 0) {
                break;
            }
            bitPosition += _bitLength;

            if (currentCode == resetCode) {
                // Krondor uses a 9-12 byte bitBuffer (to reduce I/O?) and it discards the whole buffer when it encounters a reset code.
                int krondorBufferLength = _bitLength;
                int bytesRead = (bitPosition + 7) / 8;
                int bytePositionInBuffer = bytesRead % krondorBufferLength;
                int skip = krondorBufferLength - bytePositionInBuffer;
                inputStream.Seek(skip, SeekOrigin.Current);
                Reset();
                dictionaryIndex = 256;
                bitPosition = 0;
                continue;
            }

            // Get or create the value based on the code.
            if (!_dictionary.TryGetValue(currentCode, out List<byte>? value)) {
                value = new List<byte>(previousEntry) {
                    character
                };
            }

            // Add the value to the output
            outputWriter.Write(value.ToArray());

            // Create a new dictionary entry
            character = value[0];
            var entry = new List<byte>(previousEntry) {
                character
            };
            _dictionary.Add(dictionaryIndex++, entry);

            previousEntry = value;

            if (dictionaryIndex == 1 << _bitLength && _bitLength < 12) {
                _bitLength++;
                bitPosition = 0;
            }
        }

        outputStream.Seek(0, SeekOrigin.Begin);
        return outputStream;
    }

    private void Reset() {
        _bitsProcessed = 0;
        _bitBuffer = 0;
        _bitLength = 9;
        _dictionary.Clear();
        for (int i = 0; i < 256; i++) {
            _dictionary.Add(i, new List<byte> {
                (byte)i
            });
        }
    }

    private int ReadNextCode(BinaryReader compressedData) {
        if (compressedData.BaseStream.Position >= _endPosition) {
            return -1;
        }
        while (_bitsProcessed < _bitLength) {
            _bitBuffer |= compressedData.ReadByte() << _bitsProcessed;
            _bitsProcessed += 8;
        }

        int currentCode = _bitBuffer & (1 << _bitLength) - 1;
        _bitBuffer >>= _bitLength;
        _bitsProcessed -= _bitLength;
        return currentCode;
    }
}