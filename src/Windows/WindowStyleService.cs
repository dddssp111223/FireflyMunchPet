using Godot;
using System;

namespace DesktopPet.Windows;

internal static class WindowStyleService
{
    internal static nint GetHwnd(Window window) =>
        (nint)DisplayServer.WindowGetNativeHandle(
            DisplayServer.HandleType.WindowHandle, window.GetWindowId());

    internal static void ApplyDesktopPetStyles(Window window)
    {
        if (!OperatingSystem.IsWindows())
            return;

        var hwnd = GetHwnd(window);
        if (hwnd == 0)
            return;

        var style = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GwlExStyle).ToInt64();
        style |= NativeMethods.WsExToolWindow | NativeMethods.WsExNoActivate;
        style &= ~NativeMethods.WsExAppWindow;
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GwlExStyle, (nint)style);
    }
}
