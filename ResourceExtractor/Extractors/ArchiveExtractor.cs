namespace ResourceExtractor.Extractors;

using System.Text;

public class ArchiveExtractor : ExtractorBase {
    public static void ExtractResourceArchive(string filePath) {
        const string resourceFileName = "KRONDOR.001";

        string resourceFilePath = Path.Combine(filePath, resourceFileName);
        using FileStream resourceFile = File.OpenRead(resourceFilePath);
        long resourceFileLength = resourceFile.Length;
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));

        while (resourceFile.Position < resourceFileLength) {
            string fileName = new(resourceReader.ReadChars(FileNameLength));
            fileName = fileName.Trim('\0');
            uint fileSize = resourceReader.ReadUInt32();
            byte[] buffer = resourceReader.ReadBytes((int)fileSize);

            string path = Path.Combine(filePath, fileName);
            Console.WriteLine($"Writing `{fileName}` with a length of {fileSize} bytes.");
            File.WriteAllBytes(path, buffer);
        }
        Console.WriteLine("Done.");
    }
}