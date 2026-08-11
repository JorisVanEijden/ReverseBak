namespace ResourceExtraction.Providers;

using GameData.Resources;
using GameData.Resources.Audio;
using ResourceExtraction;
using ResourceExtraction.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ResourceType = ResourceExtraction.ResourceType;

public class AudioResourceProvider : IResourceProvider {
    private const string ResourceFileName = "FRP.SX";
    private const int DosCodePage = 437;

    private readonly string _audioFilePath;
    private Dictionary<string, (long, uint)> _audioDictionary = [];
    private Dictionary<string, string> _namesDictionary = [];
    private bool _isInitialized;

    public ResourceExtraction.ResourceType ResourceType {
        get => ResourceExtraction.ResourceType.Audio;
    }

    public AudioResourceProvider(string gameDirectory) {
        _audioFilePath = Path.Combine(gameDirectory, ResourceFileName);
        Initialize();
    }

    private void Initialize() {
        if (!_isInitialized) {
            _audioDictionary = BuildAudioDictionary();
            _namesDictionary = BuildNamesDictionary();
            _isInitialized = true;
        }
    }

    private Dictionary<string, string> BuildNamesDictionary() {
        long? position = FindTagPosition(File.OpenRead(_audioFilePath));
        if (position == null) {
            return new Dictionary<string, string>();
        }
        var namesDict = new Dictionary<string, string>();
        using var stream = File.OpenRead(_audioFilePath);
        stream.Seek(position.Value, SeekOrigin.Begin);
        var reader = new BinaryReader(stream, Encoding.GetEncoding(DosCodePage));
        string tag = reader.ReadTag();
        if (tag != "TAG") {
            throw new InvalidDataException($"Expected TAG tag, got {tag} at position {stream.Position:X8}");
        }
        Dictionary<int, string> names = reader.ReadTags();
        foreach (KeyValuePair<int, string> name in names) {
            namesDict[name.Key.ToString()] = name.Value;
        }
        return namesDict;
    }

    public bool CanProvideResource(string resourceId) {
        if (!_isInitialized) Initialize();

        return _audioDictionary.ContainsKey(resourceId);
    }

    public Stream GetResourceStream(string resourceId) {
        if (!_isInitialized) Initialize();

        if (!_audioDictionary.TryGetValue(resourceId, out var entry)) {
            throw new KeyNotFoundException($"Audio resource {resourceId} not found.");
        }

        using var stream = File.OpenRead(_audioFilePath);
        stream.Seek(entry.Item1, SeekOrigin.Begin);
        var buffer = new byte[entry.Item2];
        stream.ReadExactly(buffer);

        return new MemoryStream(buffer);
    }

    public IDictionary<string, (long, uint)> GetDictionary(ResourceType? type = null) {
        if (type.HasValue && type.Value != ResourceType.Audio) {
            throw new ArgumentException($"Invalid resource type: {type.Value}. Expected: {ResourceType.Audio}");
        }
        if (!_isInitialized) Initialize();

        return new Dictionary<string, (long, uint)>(_audioDictionary);
    }

    public T GetResource<T>(string resourceId) where T : IResource {
        if (!_isInitialized) Initialize();

        if (typeof(T) != typeof(AudioResource)) {
            throw new InvalidOperationException($"AudioResourceProvider can only provide resources of type AudioResource, not {typeof(T).Name}");
        }

        // Get the extractor for audio resources
        var extractor = ExtractorFactory.GetExtractor<AudioResource>();

        // Get the resource stream
        using var stream = GetResourceStream(resourceId);

        // Extract and return the resource
        var audioResource = extractor.Extract(resourceId, stream);
        audioResource.Name = _namesDictionary[resourceId];
        return (T)(object)audioResource;
    }

    private Dictionary<string, (long, uint)> BuildAudioDictionary() {
        var dict = new Dictionary<string, (long, uint)>();

        if (!File.Exists(_audioFilePath)) {
            return dict;
        }

        using var stream = File.OpenRead(_audioFilePath);
        using var reader = new BinaryReader(stream, Encoding.GetEncoding(DosCodePage));

        // Read SND header (4-byte tag + 4-byte size)
        string tag = reader.ReadTag();
        if (tag != "SND") {
            throw new InvalidDataException($"Expected SND tag, got {tag} at position {stream.Position:X8}");
        }

        // Skip file size (we don't need it for the dictionary)
        reader.ReadUInt32();

        // Read INF block (4-byte tag + 4-byte size)
        tag = reader.ReadTag();
        if (tag != "INF") {
            throw new InvalidDataException($"Expected INF tag, got {tag} at position {stream.Position:X8}");
        }

        uint infBlockSize = reader.ReadUInt32();
        ushort version = reader.ReadUInt16();
        ushort nrOfSounds = reader.ReadUInt16();
        byte unknownByte = reader.ReadByte();

        // Read sound offsets (2-byte ID + 4-byte offset for each sound)
        var offsets = new List<(ushort soundId, int offset)>();
        for (int i = 0; i < nrOfSounds; i++) {
            ushort soundId = reader.ReadUInt16();
            int offset = reader.ReadInt32();
            offsets.Add((soundId, offset));
        }

        // Sort by offset to process in file order
        offsets.Sort((a, b) => a.offset.CompareTo(b.offset));

        // Process each sound to find its size
        for (int i = 0; i < offsets.Count; i++) {
            var (soundId, offset) = offsets[i];

            // Seek to the sound's data
            stream.Seek(offset, SeekOrigin.Begin);

            // Read the DAT tag (4 bytes) and size (4 bytes)
            tag = reader.ReadTag();
            if (tag != "DAT") {
                throw new InvalidDataException($"Expected DAT tag at offset {offset}, got {tag}");
            }

            // Read the size
            uint size = reader.ReadUInt32();

            dict[soundId.ToString()] = (offset, size + 8);
        }

        return dict;
    }

    /// <summary>
    /// Finds the position of the TAG section in the stream.
    /// </summary>
    /// <param name="stream">The stream to search in</param>
    /// <returns>The position of the TAG section if found, null otherwise</returns>
    private static long? FindTagPosition(Stream stream) {
        // Start with a 4KB buffer and increase if needed
        var bufferSize = 4096;
        long initialPosition = stream.Position;

        try {
            // Keep increasing the buffer size until we find the TAG or reach the start of the file
            while (bufferSize <= stream.Length) {
                try {
                    // Search from the end of the file minus the current buffer size
                    stream.Seek(-bufferSize, SeekOrigin.End);
                    var buffer = new byte[bufferSize];
                    int bytesRead = stream.Read(buffer, 0, bufferSize);

                    // Convert to string for easier searching
                    string fileEnd = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    int tagIndex = fileEnd.IndexOf("TAG:", StringComparison.Ordinal);

                    if (tagIndex >= 0) {
                        // Found the TAG section, calculate its position
                        return stream.Length - bytesRead + tagIndex;
                    }

                    // If we've read the entire file and still no TAG, give up
                    if (bufferSize >= stream.Length) {
                        return null;
                    }

                    // Double the buffer size for next iteration (with a reasonable max to avoid OOM)
                    bufferSize = Math.Min(bufferSize * 2, (int)Math.Min(int.MaxValue, stream.Length));
                } catch (ArgumentOutOfRangeException) {
                    // If seeking before start of stream, read from the beginning
                    stream.Position = 0;
                    bufferSize = (int)stream.Length;
                }
            }
        } finally {
            // Restore the original position
            stream.Position = initialPosition;
        }

        return null;
    }
}