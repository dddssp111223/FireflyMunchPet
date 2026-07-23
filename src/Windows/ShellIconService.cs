using Godot;
using System;
using System.Runtime.InteropServices;

namespace DesktopPet.Windows;

internal static class ShellIconService
{
    private const int IconSize = 32;

    internal static Texture2D? LoadIcon(string path)
    {
        if (!OperatingSystem.IsWindows())
            return null;

        var info = new NativeMethods.ShFileInfo();
        var result = NativeMethods.SHGetFileInfo(
            path, 0, ref info, (uint)Marshal.SizeOf<NativeMethods.ShFileInfo>(),
            NativeMethods.ShgfiIcon | NativeMethods.ShgfiLargeIcon);
        if (result == 0 || info.Icon == 0)
            return null;

        try
        {
            return RenderIcon(info.Icon);
        }
        finally
        {
            NativeMethods.DestroyIcon(info.Icon);
        }
    }

    private static Texture2D? RenderIcon(nint icon)
    {
        var dc = NativeMethods.CreateCompatibleDC(0);
        if (dc == 0)
            return null;

        nint bitmap = 0;
        nint old = 0;
        try
        {
            var bitmapInfo = new NativeMethods.BitmapInfo
            {
                Header = new NativeMethods.BitmapInfoHeader
                {
                    Size = (uint)Marshal.SizeOf<NativeMethods.BitmapInfoHeader>(),
                    Width = IconSize,
                    Height = -IconSize,
                    Planes = 1,
                    BitCount = 32,
                    Compression = 0,
                    SizeImage = IconSize * IconSize * 4
                }
            };

            bitmap = NativeMethods.CreateDIBSection(
                dc, ref bitmapInfo, NativeMethods.DibRgbColors, out var bits, 0, 0);
            if (bitmap == 0 || bits == 0)
                return null;

            old = NativeMethods.SelectObject(dc, bitmap);
            if (!NativeMethods.DrawIconEx(
                    dc, 0, 0, icon, IconSize, IconSize, 0, 0, NativeMethods.DiNormal))
                return null;

            var pixels = new byte[IconSize * IconSize * 4];
            Marshal.Copy(bits, pixels, 0, pixels.Length);
            for (var offset = 0; offset < pixels.Length; offset += 4)
                (pixels[offset], pixels[offset + 2]) = (pixels[offset + 2], pixels[offset]);

            var image = Image.CreateFromData(
                IconSize, IconSize, false, Image.Format.Rgba8, pixels);
            return ImageTexture.CreateFromImage(image);
        }
        finally
        {
            if (old != 0)
                NativeMethods.SelectObject(dc, old);
            if (bitmap != 0)
                NativeMethods.DeleteObject(bitmap);
            NativeMethods.DeleteDC(dc);
        }
    }
}
