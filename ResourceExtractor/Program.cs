using System.Text;

string filePath = args.Length == 1
    ? args[0]
    : Directory.GetCurrentDirectory();

const string resourceFileName = "KRONDOR.001";
const int fileNameLength = 13;
const int dosCodePage = 437;
CodePagesEncodingProvider.Instance.GetEncoding(dosCodePage);
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

string resourceFilePath = Path.Combine(filePath, resourceFileName);
using FileStream resourceFile = File.OpenRead(resourceFilePath);
long resourceFileLength = resourceFile.Length;
using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(dosCodePage));

while (resourceFile.Position < resourceFileLength) {
    string fileName = new(resourceReader.ReadChars(fileNameLength));
    fileName = fileName.Trim('\0');
    uint fileSize = resourceReader.ReadUInt32();
    byte[] buffer = resourceReader.ReadBytes((int)fileSize);

    string path = Path.Combine(filePath, fileName);
    Console.WriteLine($"Writing `{fileName}` with a length of {fileSize} bytes.");
    File.WriteAllBytes(path, buffer);
}
Console.WriteLine("Done.");