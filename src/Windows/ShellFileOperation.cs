using DesktopPet.Core;
using System;

namespace DesktopPet.Windows;

public sealed record ShellDeleteResult(DeleteOutcome Outcome, int ShellCode);

public sealed class ShellFileOperation
{
    private readonly nint _owner;

    public ShellFileOperation(nint owner) => _owner = owner;

    public ShellDeleteResult MoveToRecycleBin(DropBatch batch)
    {
        if (!OperatingSystem.IsWindows())
            return new ShellDeleteResult(DeleteOutcome.Failed, -1);

        var from = string.Join('\0', batch.Paths) + "\0\0";
        var operation = new NativeMethods.ShFileOpStruct
        {
            Hwnd = _owner,
            Func = NativeMethods.FoDelete,
            From = from,
            To = null,
            Flags = NativeMethods.FofAllowUndo |
                    NativeMethods.FofSilent |
                    NativeMethods.FofNoConfirmation |
                    NativeMethods.FofNoConfirmMkdir |
                    NativeMethods.FofWantNukeWarning,
            AnyOperationsAborted = false,
            NameMappings = 0,
            ProgressTitle = null
        };

        var code = NativeMethods.SHFileOperation(ref operation);
        return new ShellDeleteResult(
            ShellDeleteResultMapper.Map(code, operation.AnyOperationsAborted), code);
    }
}
