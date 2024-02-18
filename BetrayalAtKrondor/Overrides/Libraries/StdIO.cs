namespace BetrayalAtKrondor.Overrides.Libraries;

using Serilog.Events;

using Spice86.Core.CLI;
using Spice86.Core.Emulator.Function;
using Spice86.Core.Emulator.OperatingSystem.Structures;
using Spice86.Core.Emulator.ReverseEngineer;
using Spice86.Core.Emulator.VM;
using Spice86.Shared.Emulator.Memory;
using Spice86.Shared.Interfaces;
using Spice86.Shared.Utils;

using System.Collections;
using System.Diagnostics;
using System.Text;

using File = System.IO.File;

public class StdIO : CSharpOverrideHelper {
    private readonly List<Stream> _openStreams = new();
    private readonly Dictionary<int, string> _openFiles = new();
    private readonly string _mountPoint;
    private readonly ArgumentFetcher _args;
    private readonly List<IEnumerator> _fileSearchLists = new();
    private readonly CFunctions _cFunctions;
    private string CurrentDir { get; set; }

    public StdIO(Dictionary<SegmentedAddress, FunctionInformation> functionsInformation, Machine machine, ILoggerService loggerService, Configuration configuration)
        : base(functionsInformation, machine, loggerService, configuration) {
        _args = new ArgumentFetcher(machine.Cpu, machine.Memory);
        string configurationExe = configuration.Exe ?? throw new InvalidOperationException("Missing configuration exe");
        DirectoryInfo parent = Directory.GetParent(configurationExe) ?? throw new InvalidOperationException("Could not determine parent directory");
        _mountPoint = parent.FullName;
        _cFunctions = new CFunctions(machine.Cpu, machine.Memory);
        CurrentDir = "";
        DefineFunctions();
        Initialize();
    }

    private void Initialize() {
        _openStreams.Add(Console.OpenStandardInput());
        _openStreams.Add(Console.OpenStandardOutput());
    }

    private void DefineFunctions() {
        DefineFunction(0x1000, 0x04F6, ReadFile, true, nameof(ReadFile));
        DefineFunction(0x1000, 0x04FD, WriteFile, true, nameof(WriteFile));
        DefineFunction(0x1000, 0x14CD, ChDir, true, nameof(ChDir));
        DefineFunction(0x1000, 0x1A10, MkDir, true, nameof(MkDir));
        DefineFunction(0x1000, 0x1B0D, Unlink, true, nameof(Unlink));
        DefineFunction(0x1000, 0x3487, Close, true, nameof(Close));
        DefineFunction(0x1000, 0x3544, FClose, true, nameof(FClose));
        DefineFunction(0x1000, 0x3646, FindFirst, true, nameof(FindFirst));
        DefineFunction(0x1000, 0x3679, FindNext, true, nameof(FindNext));
        DefineFunction(0x1000, 0x3861, FOpen, true, nameof(FOpen));
        DefineFunction(0x1000, 0x3957, FRead, true, nameof(FRead));
        DefineFunction(0x1000, 0x3A1C, FSeek, true, nameof(FSeek));
        DefineFunction(0x1000, 0x3A84, FTell, true, nameof(FTell));
        DefineFunction(0x1000, 0x3B4B, FWrite, true, nameof(FWrite));
        DefineFunction(0x1000, 0x3C2E, FGetC, true, nameof(FGetC));
        DefineFunction(0x1000, 0x3DFB, Open, true, nameof(Open));
        DefineFunction(0x1000, 0x3FD0, FPutC, true, nameof(FPutC));
        DefineFunction(0x1000, 0x42C0, Read, true, nameof(Read));
        DefineFunction(0x1000, 0x4391, Rewind, true, nameof(Rewind));
        DefineFunction(0x1000, 0x44B3, SprintF, true, nameof(SprintF));
        DefineFunction(0x1000, 0x44EC, StrCat, true, nameof(StrCat));
        DefineFunction(0x1000, 0x455B, StrCmp, true, nameof(StrCmp));
        DefineFunction(0x1000, 0x458A, StrCpy, true, nameof(StrCpy));
        DefineFunction(0x1000, 0x45AC, StriCmp, true, nameof(StriCmp));
        DefineFunction(0x1000, 0x47F2, Write, true, nameof(Write));
    }

    private Action StrCat(int arg) {
        _args.Get(out ushort string1Pointer, out ushort string2Pointer);

        uint address1 = MemoryUtils.ToPhysicalAddress(DS, string1Pointer);
        string string1 = Memory.GetZeroTerminatedString(address1, int.MaxValue);
        uint address2 = MemoryUtils.ToPhysicalAddress(DS, string2Pointer);
        string string2 = Memory.GetZeroTerminatedString(address2, int.MaxValue);

        uint destinationAddress = MemoryUtils.ToPhysicalAddress(DS, string1Pointer);
        string resultString = string1 + string2;
        Memory.SetZeroTerminatedString(destinationAddress, resultString, int.MaxValue);

        if (_loggerService.IsEnabled(LogEventLevel.Debug)) {
            _loggerService.Debug("{Library}:strcat(s1: '{String1}', s2: '{String2}') => {DestinationSegment:X4}:{Result:X4} ['{ResultString}']",
                nameof(StdIO), string1, string2, DS, string1Pointer, resultString);
        }
        ES = DS;
        SetResult(string1Pointer);
        return FarRet();
    }

    private Action StrCmp(int arg) {
        _args.Get(out string s1, out string s2);
        int result = string.Compare(s1, s2, StringComparison.Ordinal);
        if (_loggerService.IsEnabled(LogEventLevel.Debug)) {
            _loggerService.Debug("{Library}:strcmp(s1: {String1}, s2: '{String2}') => 0x{Result:X4}",
                nameof(StdIO), s1, s2, result);
        }
        ES = DS;
        SetResult(result);
        return FarRet();
    }

    private Action StrCpy(int _) {
        _args.Get(out ushort destination, out ushort source);

        ES = DS;

        uint sourceAddress = MemoryUtils.ToPhysicalAddress(ES, source);
        string sourceString = Memory.GetZeroTerminatedString(sourceAddress, int.MaxValue);

        uint destinationAddress = MemoryUtils.ToPhysicalAddress(DS, destination);
        Memory.SetZeroTerminatedString(destinationAddress, sourceString, int.MaxValue);

        if (_loggerService.IsEnabled(LogEventLevel.Debug)) {
            _loggerService.Debug("{Library}:strcpy(dest: {DestinationSegment:X4}:{Destination:X4}, src: {SourceSegment:X4}:{Source:X4}) => 0x{Result:X4} ['{SourceString}']",
                nameof(StdIO), DS, destination, DS, source, destination, sourceString);
        }
        SetResult(destination);
        return FarRet();
    }

    private Action StriCmp(int _) {
        _args.Get(out string s1, out string s2);
        int result = string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase);
        if (_loggerService.IsEnabled(LogEventLevel.Debug)) {
            _loggerService.Debug("{Library}:stricmp(s1: {String1}, s2: '{String2}') => 0x{Result:X4}",
                nameof(StdIO), s1, s2, result);
        }
        ES = DS;
        SetResult(result);
        return FarRet();
    }

    private Action SprintF(int _) {
        _args.Get(out ushort bufferPointer, out string format);
        string formatted = _cFunctions._sprintf(format);
        int result = formatted.Length;

        if (_loggerService.IsEnabled(LogEventLevel.Debug)) {
            _loggerService.Debug("{Library}:sprintf(str: {BufferPointer:X4}, format: '{Format}', ...) => 0x{Result:X4} ['{Formatted}']",
                nameof(StdIO), bufferPointer, format, result, formatted);
        }

        uint address = MemoryUtils.ToPhysicalAddress(DS, bufferPointer);
        Memory.SetZeroTerminatedString(address, formatted, int.MaxValue);

        SetResult(result);
        return FarRet();
    }

    private Action Write(int _) {
        _args.Get(out ushort fileHandle, out ushort bufferPointer, out ushort count);
        short result;

        if (fileHandle < 1 || fileHandle > _openStreams.Count) {
            result = -1;
            if (_loggerService.IsEnabled(LogEventLevel.Error)) {
                _loggerService.Error("{Library}:write(fd: {FileDescriptor}, buf: 0x{BufferPointer:X4}, len: {Count}) => 0x{Result:X4}",
                    nameof(StdIO), fileHandle, bufferPointer, count, result);
            }
            SetResult(result);
            return FarRet();
        }

        Stream stream = _openStreams[fileHandle];
        uint address = MemoryUtils.ToPhysicalAddress(DS, bufferPointer);
        byte[] buffer = Memory.GetData(address, count);
        try {
            stream.Write(buffer);
        } catch (Exception e) {
            result = -1;
            if (_loggerService.IsEnabled(LogEventLevel.Error)) {
                _loggerService.Error(e, "{Library}:write(fd: {FileDescriptor}, buf: 0x{BufferPointer:X4}, len: {Count}) => 0x{Result:X4}",
                    nameof(StdIO), fileHandle, bufferPointer, count, result);
            }
            SetResult(result);
            return FarRet();
        }

        result = (short)count;
        SetResult(result);
        return FarRet();
    }

    private Action WriteFile(int _) {
        _args.Get(out uint bufferPointer, out uint count, out ushort fileHandle);
        uint result;

        ushort bufferSegment = (ushort)(bufferPointer >> 16);
        ushort bufferOffset = (ushort)bufferPointer;

        if (fileHandle < 1 || fileHandle > _openStreams.Count) {
            result = 0;
            if (_loggerService.IsEnabled(LogEventLevel.Warning)) {
                _loggerService.Warning("{Library}:writeFile(buf: {BufferSegment:X4}:{BufferOffset:X4}, len: {Count}, fd: {FileDescriptor}) => 0x{Result:X8}",
                    nameof(StdIO), bufferSegment, bufferOffset, count, fileHandle, result);
            }
            SetResult(result);
            return FarRet();
        }

        Stream stream = _openStreams[fileHandle];
        try {
            uint bufferAddress = MemoryUtils.ToPhysicalAddress(bufferSegment, bufferOffset);
            byte[] buffer = Memory.GetData(bufferAddress, count);
            stream.Write(buffer);
            result = count;
        } catch (Exception e) {
            result = 0;
            if (_loggerService.IsEnabled(LogEventLevel.Error)) {
                _loggerService.Error(e, "{Library}:writeFile(buf: {BufferSegment:X4}:{BufferOffset:X4}, len: {Count}, fd: {FileDescriptor}) => 0x{Result:X8}",
                    nameof(StdIO), bufferSegment, bufferOffset, count, fileHandle, result);
            }
            SetResult(result);
            return FarRet();
        }

        if (_loggerService.IsEnabled(LogEventLevel.Debug)) {
            _loggerService.Debug("{Library}:writeFile(buf: {BufferSegment:X4}:{BufferOffset:X4}, len: {Count}, fd: {FileDescriptor} [{FileName}]) => 0x{Result:X8}",
                nameof(StdIO), bufferSegment, bufferOffset, count, fileHandle, _openFiles[fileHandle], result);
        }

        SetResult(result);
        return FarRet();
    }

    private Action ReadFile(int _) {
        _args.Get(out uint bufferPointer, out uint count, out ushort fileHandle);
        uint result;

        ushort bufferSegment = (ushort)(bufferPointer >> 16);
        ushort bufferOffset = (ushort)bufferPointer;

        if (fileHandle < 1 || fileHandle > _openStreams.Count) {
            result = 0;
            if (_loggerService.IsEnabled(LogEventLevel.Warning)) {
                _loggerService.Warning("{Library}:readFile(buf: {BufferSegment:X4}:{BufferOffset:X4}, len: {Count}, fd: {FileDescriptor}) => 0x{Result:X8}",
                    nameof(StdIO), bufferSegment, bufferOffset, count, fileHandle, result);
            }
            SetResult(result);
            return FarRet();
        }

        Stream stream = _openStreams[fileHandle];
        byte[] buffer = new byte[count];
        try {
            result = (uint)stream.Read(buffer);
            uint bufferAddress = MemoryUtils.ToPhysicalAddress(bufferSegment, bufferOffset);
            Memory.LoadData(bufferAddress, buffer);
        } catch (Exception e) {
            result = 0;
            if (_loggerService.IsEnabled(LogEventLevel.Error)) {
                _loggerService.Error(e, "{Library}:readFile(buf: {BufferSegment:X4}:{BufferOffset:X4}, len: {Count}, fd: {FileDescriptor}) => 0x{Result:X8}",
                    nameof(StdIO), bufferSegment, bufferOffset, count, fileHandle, result);
            }
            SetResult(result);
            return FarRet();
        }

        if (_loggerService.IsEnabled(LogEventLevel.Debug)) {
            _loggerService.Debug("{Library}:readFile(buf: {BufferSegment:X4}:{BufferOffset:X4}, len: {Count}, fd: {FileDescriptor} [{FileName}]) => 0x{Result:X8}",
                nameof(StdIO), bufferSegment, bufferOffset, count, fileHandle, _openFiles[fileHandle], result);
        }

        SetResult(result);
        return FarRet();
    }

    private Action FindFirst(int _) {
        _args.Get(out string searchPath, out ushort ffblkPointerOffset, out short attrib);
        short result;

        uint ffblkPointer = MemoryUtils.ToPhysicalAddress(DS, ffblkPointerOffset);
        var searchAttributes = (FileAttributes)attrib;

        string currentHostDirectory = Path.Combine(_mountPoint, CurrentDir);
        EnumerationOptions searchOptions = GetSearchOptions(searchAttributes);

        if (_loggerService.IsEnabled(LogEventLevel.Debug)) {
            _loggerService.Debug("{Library}:findfirst() Searching for '{Pattern}' in '{Directory}' with options '{@SearchOptions}'",
                nameof(StdIO), searchPath, currentHostDirectory, searchOptions);
        }

        IEnumerator matchingPaths = Directory.EnumerateFileSystemEntries(currentHostDirectory, searchPath, searchOptions).GetEnumerator();
        if (matchingPaths.MoveNext() && matchingPaths.Current is string firstFile) {
            _fileSearchLists.Add(matchingPaths);
            var ffblk = new DosDiskTransferArea(Memory, ffblkPointer);
            ffblk.ResultId = (ushort)(_fileSearchLists.Count - 1);
            ffblk.FileName = Path.GetFileName(firstFile);
            result = 0;
            if (_loggerService.IsEnabled(LogEventLevel.Debug)) {
                _loggerService.Debug("{Library}:findfirst(path: {FilePath}, ffblk: 0x{FFBlkPointer:X4}, attrib: {Attrib}) => 0x{Result:X4} [filename: {FileName}]",
                    nameof(StdIO), searchPath, ffblkPointer, searchAttributes, result, firstFile);
            }
        } else {
            result = -1;
            if (_loggerService.IsEnabled(LogEventLevel.Debug)) {
                _loggerService.Debug("{Library}:findfirst(path: {FilePath}, ffblk: 0x{FFBlkPointer:X4}, attrib: {Attrib}) => 0x{Result:X4}",
                    nameof(StdIO), searchPath, ffblkPointer, searchAttributes, result);
            }
        }

        SetResult(result);
        return FarRet();
    }

    private static EnumerationOptions GetSearchOptions(FileAttributes searchAttributes) {
        FileAttributes attributesToSkip = 0;
        if (!searchAttributes.HasFlag(FileAttributes.Directory)) {
            attributesToSkip |= FileAttributes.Directory;
        }
        if (!searchAttributes.HasFlag(FileAttributes.Hidden)) {
            attributesToSkip |= FileAttributes.Hidden;
        }
        if (!searchAttributes.HasFlag(FileAttributes.System)) {
            attributesToSkip |= FileAttributes.System;
        }

        EnumerationOptions searchOptions = new() {
            AttributesToSkip = attributesToSkip,
            IgnoreInaccessible = true,
            RecurseSubdirectories = false,
            MatchCasing = MatchCasing.CaseInsensitive,
            MatchType = MatchType.Simple
        };
        return searchOptions;
    }

    private Action FindNext(int _) {
        _args.Get(out ushort ffblkPointerOffset);
        short result;

        var ffblkPointer = MemoryUtils.ToPhysicalAddress(DS, ffblkPointerOffset);
        var ffblk = new DosDiskTransferArea(Memory, ffblkPointer);

        IEnumerator matchingPaths = _fileSearchLists[ffblk.ResultId];
        if (matchingPaths.MoveNext() && matchingPaths.Current is string currentFile) {
            ffblk.FileName = Path.GetFileName(currentFile);
            result = 0;
            if (_loggerService.IsEnabled(LogEventLevel.Debug)) {
                _loggerService.Debug("{Library}:findnext(ffblk: 0x{FFBlkPointer:X4}) => 0x{Result:X4} [filename: {FileName}]",
                    nameof(StdIO), ffblkPointer, result, currentFile);
            }
        } else {
            result = -1;
            if (_loggerService.IsEnabled(LogEventLevel.Information)) {
                _loggerService.Information("{Library}:findnext(ffblk: 0x{FFBlkPointer:X4}) => 0x{Result:X4}",
                    nameof(StdIO), ffblkPointer, result);
            }
        }

        SetResult(result);
        return FarRet();
    }

    private Action ChDir(int _) {
        _args.Get(out string dosPath);
        short result;

        CurrentDir = dosPath;

        result = 0;
        if (_loggerService.IsEnabled(LogEventLevel.Debug)) {
            _loggerService.Debug("{Library}:chdir(path: {FilePath}) => 0x{Result:X4}",
                nameof(StdIO), dosPath, result);
        }

        SetResult(result);
        return FarRet();
    }

    private Action MkDir(int _) {
        _args.Get(out string dosPath);
        short result;

        string hostPath = Path.Combine(_mountPoint, CurrentDir, dosPath);

        try {
            Directory.CreateDirectory(hostPath);
            result = 0;
            if (_loggerService.IsEnabled(LogEventLevel.Debug)) {
                _loggerService.Debug("{Library}:mkdir(path: {FilePath}) => 0x{Result:X4} [{FullPath}]",
                    nameof(StdIO), hostPath, result, hostPath);
            }
        } catch (Exception ex) {
            result = -1;
            if (_loggerService.IsEnabled(LogEventLevel.Error)) {
                _loggerService.Error(ex, "{Library}:mkdir(path: {FilePath}) => 0x{Result:X4} [{FullPath}]",
                    nameof(StdIO), hostPath, result, hostPath);
            }
        }

        SetResult(result);
        return FarRet();
    }

    private Action Unlink(int arg) {
        _args.Get(out string dosPath);
        short result;

        string hostPath = Path.Combine(_mountPoint, CurrentDir, dosPath);

        try {
            File.Delete(hostPath);
            result = 0;
            if (_loggerService.IsEnabled(LogEventLevel.Debug)) {
                _loggerService.Debug("{Library}:unlink(path: {FilePath}) => 0x{Result:X4} [{FullPath}]",
                    nameof(StdIO), dosPath, result, hostPath);
            }
        } catch (Exception ex) {
            result = -1;
            if (_loggerService.IsEnabled(LogEventLevel.Error)) {
                _loggerService.Error(ex, "{Library}:unlink(path: {FilePath}) => 0x{Result:X4} [{FullPath}]",
                    nameof(StdIO), hostPath, result, hostPath);
            }
        }

        SetResult(result);
        return FarRet();
    }

    private Action Rewind(int arg) {
        throw new NotImplementedException("Rewind");
    }

    private Action FPutC(int arg) {
        throw new NotImplementedException("FPutC");
    }

    private Action Read(int arg) {
        throw new NotImplementedException("Read");
    }

    private Action Close(int arg) {
        _args.Get(out ushort fileDescriptor);
        short result;
        if (fileDescriptor < 1 || fileDescriptor > _openStreams.Count) {
            result = Constants.EOF;
            if (_loggerService.IsEnabled(LogEventLevel.Error)) {
                _loggerService.Error("{Library}:close(fd: {FileDescriptor}) => 0x{Result:X4}",
                    nameof(StdIO), fileDescriptor, result);
            }
            SetResult(result);
            return FarRet();
        }

        Stream stream = _openStreams[fileDescriptor];
        stream.Dispose();
        _openStreams.RemoveAt(fileDescriptor);
        result = 0;

        if (_loggerService.IsEnabled(LogEventLevel.Debug)) {
            _loggerService.Debug("{Library}:close(fd: {FileDescriptor} [{FileName}]) => 0x{Result:X4}",
                nameof(StdIO), fileDescriptor, _openFiles[fileDescriptor], result);
        }
        _openFiles.Remove(fileDescriptor);
        SetResult(result);
        return FarRet();
    }

    private Action FWrite(int arg) {
        _args.Get(out ushort bufferOffset, out ushort elementSize, out ushort count, out ushort fileDescriptor);
        int result;

        if (fileDescriptor < 1 || fileDescriptor > _openStreams.Count) {
            result = Constants.EOF;
            if (_loggerService.IsEnabled(LogEventLevel.Error)) {
                _loggerService.Error("{Library}:fwrite(bufferOffset: 0x{BufferOffset:X4}, elementSize: 0x{ElementSize:X4}, count: 0x{Count:X4}, fd: {FileDescriptor}) => 0x{Result:X8}",
                    nameof(StdIO), bufferOffset, elementSize, count, fileDescriptor, result);
            }
            SetResult(result);
            return FarRet();
        }

        Stream stream = _openStreams[fileDescriptor];
        uint address = MemoryUtils.ToPhysicalAddress(DS, bufferOffset);
        uint size = (uint)(elementSize * count);
        byte[] buffer = Memory.GetData(address, size);
        stream.Write(buffer, 0, buffer.Length);
        result = count;

        if (_loggerService.IsEnabled(LogEventLevel.Verbose)) {
            // Log byte, word and int data writes
            string dataWritten = string.Empty;
            if (count == 1) {
                if (elementSize == 1) {
                    dataWritten = $" (0x{buffer[0]:X2}";
                    if (buffer[0] >= 0x20 && buffer[0] <= 0x7E) {
                        dataWritten += $" '{(char)buffer[0]}'";
                    }
                    dataWritten += ")";
                } else if (elementSize == 2) {
                    int data = buffer[1] << 8 | buffer[0];
                    dataWritten = $" (0x{data:X4})";
                } else if (elementSize == 4) {
                    int data = buffer[3] << 24 | buffer[2] << 16 | buffer[1] << 8 | buffer[0];
                    dataWritten = $" (0x{data:X8})";
                } else if (elementSize == 13) {
                    // filenames
                    dataWritten = $" ({Encoding.ASCII.GetString(buffer)})";
                }
            }
            _loggerService.Verbose("{Library}:fwrite(bufferOffset: 0x{BufferOffset:X4}, elementSize: 0x{ElementSize:X4}, count: 0x{Count:X4}, fd: {FileDescriptor} [{FileName}]) => 0x{Result:X8}{DataWritten}",
                nameof(StdIO), bufferOffset, elementSize, count, fileDescriptor, _openFiles[fileDescriptor], result, dataWritten);
        }
        SetResult(result);
        return FarRet();
    }

    private Action FGetC(int arg) {
        _args.Get(out ushort fileDescriptor);
        short result;

        if (fileDescriptor < 1 || fileDescriptor > _openStreams.Count) {
            result = Constants.EOF;
            if (_loggerService.IsEnabled(LogEventLevel.Warning)) {
                _loggerService.Warning("{Library}:fgetc(fd: {FileDescriptor} [{FileName}]) => 0x{Result:X4}",
                    nameof(StdIO), fileDescriptor, _openFiles[fileDescriptor], result);
            }
            SetResult(result);
            return FarRet();
        }

        Stream s = _openStreams[fileDescriptor];
        try {
            result = (short)s.ReadByte();
        } catch (EndOfStreamException e) {
            result = Constants.EOF;
            if (_loggerService.IsEnabled(LogEventLevel.Error)) {
                _loggerService.Error(e, "{Library}:fgetc(fd: {FileDescriptor} [{FileName}]) => 0x{Result:X4} ({Exception})",
                    nameof(StdIO), fileDescriptor, _openFiles[fileDescriptor], result, e.Demystify());
            }
        }

        if (_loggerService.IsEnabled(LogEventLevel.Verbose)) {
            _loggerService.Verbose("{Library}:fgetc(fd: {FileDescriptor} [{FileName}]) => 0x{Result:X4}",
                nameof(StdIO), fileDescriptor, _openFiles[fileDescriptor], result);
        }

        SetResult(result);
        return FarRet();
    }

    private Action FRead(int _) {
        _args.Get(out ushort bufferOffset, out ushort elementSize, out ushort count, out ushort fileDescriptor);
        int result;

        if (fileDescriptor < 1 || fileDescriptor > _openStreams.Count) {
            result = Constants.EOF;
            if (_loggerService.IsEnabled(LogEventLevel.Error)) {
                _loggerService.Error("{Library}:fread(bufferOffset: 0x{BufferOffset:X4}, elementSize: 0x{ElementSize:X4}, count: 0x{Count:X4}, fd: {FileDescriptor}) => 0x{Result:X8}",
                    nameof(StdIO), bufferOffset, elementSize, count, fileDescriptor, result);
            }
            SetResult(result);
            return FarRet();
        }

        Stream stream = _openStreams[fileDescriptor];
        byte[] buffer = new byte[elementSize * count];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        uint address = MemoryUtils.ToPhysicalAddress(DS, bufferOffset);
        Memory.LoadData(address, buffer, bytesRead);
        result = bytesRead / elementSize;

        if (_loggerService.IsEnabled(LogEventLevel.Verbose)) {
            // Log byte, word and int data reads
            string dataRead = string.Empty;
            if (count == 1) {
                if (elementSize == 1) {
                    dataRead = $" (0x{buffer[0]:X2}";
                    if (buffer[0] >= 0x20 && buffer[0] <= 0x7E) {
                        dataRead += $" '{(char)buffer[0]}'";
                    }
                    dataRead += ")";
                } else if (elementSize == 2) {
                    int data = buffer[1] << 8 | buffer[0];
                    dataRead = $" (0x{data:X4})";
                } else if (elementSize == 4) {
                    int data = buffer[3] << 24 | buffer[2] << 16 | buffer[1] << 8 | buffer[0];
                    dataRead = $" (0x{data:X8})";
                } else if (elementSize == 13) {
                    // filenames
                    dataRead = $" ({Encoding.ASCII.GetString(buffer)})";
                }
            }
            _loggerService.Verbose("{Library}:fread(bufferOffset: 0x{BufferOffset:X4}, elementSize: 0x{ElementSize:X4}, count: 0x{Count:X4}, fd: {FileDescriptor} [{FileName}]) => 0x{Result:X8}{DataRead}",
                nameof(StdIO), bufferOffset, elementSize, count, fileDescriptor, _openFiles[fileDescriptor], result, dataRead);
        }
        SetResult(result);
        return FarRet();
    }

    private Action FTell(int _) {
        _args.Get(out ushort fileDescriptor);
        int result;

        if (fileDescriptor < 1 || fileDescriptor > _openStreams.Count) {
            result = -1;
            if (_loggerService.IsEnabled(LogEventLevel.Error)) {
                _loggerService.Error("{Library}:ftell(fd: {FileDescriptor}) => 0x{Result:X8}",
                    nameof(StdIO), fileDescriptor, result);
            }
            SetResult(result);
            return FarRet();
        }

        Stream stream = _openStreams[fileDescriptor];
        result = (int)stream.Position;

        if (_loggerService.IsEnabled(LogEventLevel.Verbose)) {
            _loggerService.Verbose("{Library}:ftell(fd: {FileDescriptor} [{FileName}]) => 0x{Result:X8}",
                nameof(StdIO), fileDescriptor, _openFiles[fileDescriptor], result);
        }
        SetResult(result);
        return FarRet();
    }

    private Action FClose(int _) {
        _args.Get(out ushort fileDescriptor);
        short result;

        if (fileDescriptor < 1 || fileDescriptor > _openStreams.Count) {
            result = Constants.EOF;
            if (_loggerService.IsEnabled(LogEventLevel.Error)) {
                _loggerService.Error("{Library}:fclose(fd: {FileDescriptor}) => 0x{Result:X4}",
                    nameof(StdIO), fileDescriptor, result);
            }
            SetResult(result);
            return FarRet();
        }

        Stream stream = _openStreams[fileDescriptor];
        stream.Dispose();
        _openStreams.RemoveAt(fileDescriptor);
        result = 0;

        if (_loggerService.IsEnabled(LogEventLevel.Debug)) {
            _loggerService.Debug("{Library}:fclose(fd: {FileDescriptor} [{FileName}]) => 0x{Result:X4}",
                nameof(StdIO), fileDescriptor, _openFiles[fileDescriptor], result);
        }
        _openFiles.Remove(fileDescriptor);
        SetResult(result);
        return FarRet();
    }

    private Action FOpen(int _) {
        _args.Get(out string dosPath, out string mode);
        short result;

        string path = Path.Combine(_mountPoint, dosPath);
        if (!File.Exists(path)) {
            result = 0;
            if (_loggerService.IsEnabled(LogEventLevel.Warning)) {
                _loggerService.Warning("{Library}:fopen(filename: '{FileName}', mode: '{Mode}') => 0x{Result:X4}",
                    nameof(StdIO), dosPath, mode, result);
            }
            SetResult(result);
            return FarRet();
        }
        FileStreamOptions fileStreamOptions = ModeStringToFileStreamOptions(mode);
        FileStream stream = File.Open(path, fileStreamOptions);

        _openStreams.Add(stream);
        result = (short)(_openStreams.Count - 1);

        if (_loggerService.IsEnabled(LogEventLevel.Information)) {
            _loggerService.Information("{Library}:fopen(filename: '{FileName}', mode: '{Mode}') => 0x{Result:X4}",
                nameof(StdIO), dosPath, mode, result);
        }
        _openFiles.Add(result, dosPath);
        SetResult(result);
        return FarRet();
    }

    private Action Open(int _) {
        _args.Get(out string dosPath, out short flags, out ushort mode);
        short result;

        string hostPath = Path.Combine(_mountPoint, CurrentDir, dosPath);
        FileStreamOptions fileStreamOptions = ModeFlagsToFileStreamOptions((OpenFlags)flags, (StatFlags)mode);
        try {
            FileStream stream = File.Open(hostPath, fileStreamOptions);
            _openStreams.Add(stream);
            result = (short)(_openStreams.Count - 1);
        } catch (Exception ex) {
            result = -1;
            if (_loggerService.IsEnabled(LogEventLevel.Warning)) {
                _loggerService.Warning(ex, "{Library}:open(filename: '{FileName}', flags: {Flags:X4},  mode: {Mode:X4}) => 0x{Result:X4} [{HostPath}, {@FileStreamOptions}]",
                    nameof(StdIO), dosPath, flags, mode, result, hostPath, fileStreamOptions);
            }
            SetResult(result);
            return FarRet();
        }

        if (_loggerService.IsEnabled(LogEventLevel.Information)) {
            _loggerService.Information("{Library}:open(filename: '{FileName}', flags: {Flags:X4},  mode: {Mode:X4}) => 0x{Result:X4} [{HostPath}, {@FileStreamOptions}]",
                nameof(StdIO), dosPath, flags, mode, result, hostPath, fileStreamOptions);
        }
        _openFiles.Add(result, dosPath);

        SetResult(result);
        return FarRet();
    }

    private static FileStreamOptions ModeFlagsToFileStreamOptions(OpenFlags flags, StatFlags mode) {
        var options = new FileStreamOptions {
            BufferSize = 0
        };

        if (flags.HasFlag(OpenFlags.O_RDONLY)) {
            options.Access = FileAccess.Read;
            options.Mode = FileMode.Open;
        } else if (flags.HasFlag(OpenFlags.O_WRONLY)) {
            options.Access = FileAccess.Write;
            options.Mode = FileMode.Create;
        } else if (flags.HasFlag(OpenFlags.O_RDWR)) {
            options.Access = FileAccess.ReadWrite;
            options.Mode = FileMode.OpenOrCreate;
        }

        return options;
    }

    private static FileStreamOptions ModeStringToFileStreamOptions(string mode, int bufferSize = 512) {
        var options = new FileStreamOptions {
            BufferSize = bufferSize
        };

        switch (mode) {
            case "r":
            case "rb":
                options.Mode = FileMode.Open;
                options.Access = FileAccess.Read;
                break;
            case "w":
            case "wb":
                options.Mode = FileMode.Create;
                options.Access = FileAccess.Write;
                break;
            case "a":
            case "ab":
                options.Mode = FileMode.Append;
                options.Access = FileAccess.Write;
                break;
            case "r+":
            case "rb+":
            case "r+b":
                options.Mode = FileMode.OpenOrCreate;
                options.Access = FileAccess.ReadWrite;
                break;
            case "w+":
            case "wb+":
            case "w+b":
                options.Mode = FileMode.Truncate;
                options.Access = FileAccess.ReadWrite;
                break;
            default:
                throw new ArgumentException("Invalid fopen mode.", nameof(mode));
        }

        return options;
    }

    private Action FSeek(int _) {
        _args.Get(out ushort fileDescriptor, out int offset, out ushort whence);
        short result;

        SeekOrigin origin = whence switch {
            0 => SeekOrigin.Begin,
            1 => SeekOrigin.Current,
            2 => SeekOrigin.End,
            _ => throw new ArgumentException("Invalid fseek whence.", nameof(whence))
        };

        if (fileDescriptor < 1 || fileDescriptor > _openStreams.Count) {
            result = -1;
            if (_loggerService.IsEnabled(LogEventLevel.Error)) {
                _loggerService.Error("{Library}:fseek(fd: {FileDescriptor}, offset: {Offset}, whence: {Origin}) => 0x{Result:X4}",
                    nameof(StdIO), fileDescriptor, offset, origin, result);
            }
            SetResult(result);
            return FarRet();
        }

        Stream stream = _openStreams[fileDescriptor];
        try {
            stream.Seek(offset, origin);
            result = 0;
        } catch (IOException ioException) {
            result = -1;
            if (_loggerService.IsEnabled(LogEventLevel.Warning)) {
                _loggerService.Warning(ioException, "{Library}:fseek(fd: {FileDescriptor}, offset: {Offset:X4}, whence: {Origin}) => 0x{Result:X4}",
                    nameof(StdIO), fileDescriptor, offset, origin, result);
            }
            SetResult(result);
            return FarRet();
        }

        // if (_loggerService.IsEnabled(LogEventLevel.Verbose)) {
        _loggerService.Debug("{Library}:fseek(fd: {FileDescriptor} [{FileName}], offset: {Offset:X4}, whence: {Origin}) => 0x{Result:X4}",
            nameof(StdIO), fileDescriptor, _openFiles[fileDescriptor], offset, origin, result);
        // }
        SetResult(result);

        return FarRet();
    }

    private void SetResult(ushort result) {
        AX = result;
    }

    private void SetResult(uint result) {
        AX = (ushort)result;
        DX = (ushort)(result >> 16);
    }

    private void SetResult(int result) {
        SetResult((uint)result);
    }

    private void SetResult(short result) {
        SetResult((ushort)result);
    }
}