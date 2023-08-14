namespace ResourceExtractor.Compression;

public class Lzw : IDecompress {
    private int _bitBuffer;
    private int _dataIndex;
    private int _bitsProcessed;
    private readonly Dictionary<int, List<byte>> _dictionary = new();
    private int _bitLength = 9;

    public byte[] Decompress(byte[] compressedData) {
        var decompressedBytes = new List<byte>();

        _bitBuffer = 0;
        _dataIndex = 0;
        _bitsProcessed = 0;
        _bitLength = 9;

        // Initialize the dictionary with single bytes
        Reset();
        int resetCode = _dictionary.Count;
        int dictionaryIndex = _dictionary.Count + 1;
        int bitPosition = 0;

        // Read the first code
        int oldCode = ReadNextCode(_bitLength, compressedData);
        byte character = (byte)oldCode;
        decompressedBytes.Add(character);

        var previousEntry = new List<byte> {
            character
        };

        while (oldCode >= 0) {
            int newCode = ReadNextCode(_bitLength, compressedData);
            if (newCode < 0) {
                break;
            }
            bitPosition += _bitLength;

            if (newCode == resetCode) {
                // Krondor uses a 9-12 byte bitBuffer (to reduce I/O?) and it discards the whole buffer when it encounters a reset code.
                // Calculate the number of bytes to skip
                int byteLength = _bitLength * 8;
                int skip = bitPosition - 1 + (byteLength - (bitPosition - 1 + byteLength) % byteLength) - bitPosition >> 3;
                if (_bitsProcessed < _bitLength && _bitsProcessed > 0) {
                    skip += 1;
                }
                _dataIndex += skip;
                Reset();
                dictionaryIndex = 256;
                bitPosition = 0;
                continue;
            }

            // Get or create the value based on the code.
            if (!_dictionary.TryGetValue(newCode, out List<byte>? value)) {
                value = new List<byte>(previousEntry) {
                    character
                };
            }

            // Add the value to the output
            decompressedBytes.AddRange(value);
            character = value[0];
            var entry = new List<byte>(previousEntry) {
                character
            };
            _dictionary.Add(dictionaryIndex++, entry);
            previousEntry = value;
            oldCode = newCode;

            if (dictionaryIndex == 1 << _bitLength && _bitLength < 12) {
                _bitLength++;
                bitPosition = 0;
            }
        }

        return decompressedBytes.ToArray();
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

    private int ReadNextCode(int bitLength, IReadOnlyList<byte> compressedData) {
        while (_bitsProcessed < bitLength) {
            if (_dataIndex >= compressedData.Count) {
                return -1;
            }
            _bitBuffer |= compressedData[_dataIndex] << _bitsProcessed;
            _bitsProcessed += 8;
            _dataIndex++;
        }

        int currentCode = _bitBuffer & (1 << bitLength) - 1;
        _bitBuffer >>= bitLength;
        _bitsProcessed -= bitLength;
        return currentCode;
    }
}