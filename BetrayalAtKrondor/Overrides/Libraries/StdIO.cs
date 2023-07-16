namespace BetrayalAtKrondor.Overrides.Libraries;

using Serilog.Events;

using Spice86.Core.Emulator.Function;
using Spice86.Core.Emulator.OperatingSystem.Structures;
using Spice86.Core.Emulator.ReverseEngineer;
using Spice86.Core.Emulator.VM;
using Spice86.Shared.Emulator.Memory;
using Spice86.Shared.Interfaces;
using Spice86.Shared.Utils;

using System.Diagnostics;
using System.Text;

using File = System.IO.File;

public class StdIO : CSharpOverrideHelper {
    private readonly List<Stream> _openStreams = new();
    private readonly Dictionary<int, string> _openFiles = new();
    private readonly string _mountPoint;
    private string CurrentDir { get; set; }

    public StdIO(Dictionary<SegmentedAddress, FunctionInformation> functionsInformation, Machine machine, ILoggerService loggerService)
        : base(functionsInformation, machine, loggerService) {
        _mountPoint = Directory.GetParent(Machine.Configuration.Exe).FullName;
        CurrentDir = "";
        DefineFunctions();
        Initialize();
    }

    private void Initialize() {
        _openStreams.Add(Console.OpenStandardInput());
        _openStreams.Add(Console.OpenStandardOutput());
    }

    private void DefineFunctions() {
        DefineFunction(0x1000, 0x14CD, ChDir, true, nameof(ChDir));
        DefineFunction(0x1000, 0x1A10, MkDir, true, nameof(MkDir));
        DefineFunction(0x1000, 0x1B0D, Unlink, true, nameof(Unlink));
        DefineFunction(0x1000, 0x3487, Close, true, nameof(Close));
        DefineFunction(0x1000, 0x3544, FClose, true, nameof(FClose));
        DefineFunction(0x1000, 0x3646, FindFirst, true, nameof(FindFirst));
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
    }

    private Action FindFirst(int _) {
        GetArguments(out string searchPath, out ushort ffblkPointerOffset, out short attrib);
        short result;

        var ffblkPointer = MemoryUtils.ToPhysicalAddress(DS, ffblkPointerOffset);
        var ffblk = new DosDiskTransferArea(Memory, ffblkPointer);
        var searchAttributes = (FileAttributes)attrib;

        if (_loggerService.IsEnabled(LogEventLevel.Debug)) {
            _loggerService.Debug("{Library}:findfirst(path: {FilePath}, ffblk: 0x{FFBlkPointer:X4}, attrib: {Attrib})",
                nameof(StdIO), searchPath, ffblkPointer, searchAttributes);
        }

        result = 0;
        var currentHostDirectory = Path.Combine(_mountPoint, CurrentDir);
        EnumerationOptions searchOptions = new() {
            AttributesToSkip = ~searchAttributes,
            IgnoreInaccessible = true,
            RecurseSubdirectories = false
        };

        var matchingPaths = Directory.GetFiles(currentHostDirectory, searchPath, searchOptions);
        
        
        SetResult(result);
        return FarRet();
    }

    private Action ChDir(int _) {
        GetArguments(out string dosPath);
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
        GetArguments(out string dosPath);
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
        GetArguments(out string dosPath);
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
        GetArguments(out ushort fileDescriptor);
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

        if (_loggerService.IsEnabled(LogEventLevel.Verbose)) {
            _loggerService.Verbose("{Library}:close(fd: {FileDescriptor} [{FileName}]) => 0x{Result:X4}",
                nameof(StdIO), fileDescriptor, _openFiles[fileDescriptor], result);
        }
        _openFiles.Remove(fileDescriptor);
        SetResult(result);
        return FarRet();
    }

    private Action FWrite(int arg) {
        throw new NotImplementedException("FWrite");
    }

    private Action FGetC(int arg) {
        GetArguments(out ushort fileDescriptor);
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
        GetArguments(out ushort bufferOffset, out ushort elementSize, out ushort count, out ushort fileDescriptor);
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
        GetArguments(out ushort fileDescriptor);
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
        GetArguments(out ushort fileDescriptor);
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

        if (_loggerService.IsEnabled(LogEventLevel.Verbose)) {
            _loggerService.Verbose("{Library}:fclose(fd: {FileDescriptor} [{FileName}]) => 0x{Result:X4}",
                nameof(StdIO), fileDescriptor, _openFiles[fileDescriptor], result);
        }
        _openFiles.Remove(fileDescriptor);
        SetResult(result);
        return FarRet();
    }

    private Action FOpen(int _) {
        GetArguments(out string dosPath, out string mode);
        short result;

        string? path = Machine.Dos.FileManager.ToHostCaseSensitiveFileName(dosPath, false);
        if (path == null) {
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

        if (_loggerService.IsEnabled(LogEventLevel.Debug)) {
            _loggerService.Debug("{Library}:fopen(filename: '{FileName}', mode: '{Mode}') => 0x{Result:X4}",
                nameof(StdIO), dosPath, mode, result);
        }
        _openFiles.Add(result, dosPath);
        SetResult(result);
        return FarRet();
    }

    private Action Open(int _) {
        GetArguments(out string dosPath, out short flags, out ushort mode);
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

        if (_loggerService.IsEnabled(LogEventLevel.Debug)) {
            _loggerService.Debug("{Library}:open(filename: '{FileName}', flags: {Flags:X4},  mode: {Mode:X4}) => 0x{Result:X4} [{HostPath}, {@FileStreamOptions}]",
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
        GetArguments(out ushort fileDescriptor, out int offset, out ushort whence);
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
        stream.Seek(offset, origin);
        result = 0;

        if (_loggerService.IsEnabled(LogEventLevel.Verbose)) {
            _loggerService.Verbose("{Library}:fseek(fd: {FileDescriptor} [{FileName}], offset: {Offset}, whence: {Origin}) => 0x{Result:X4}",
                nameof(StdIO), fileDescriptor, _openFiles[fileDescriptor], offset, origin, result);
        }
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