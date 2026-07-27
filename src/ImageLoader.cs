// ImageLoader.cs — 图片加载、文件夹浏览、导航
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using static NativeConstants;
using static NativeMethods;

static class ImageLoader
{
    public static bool Load(ImageWindow w, string path)
    {
        w.SrcBitmap?.Dispose(); w.SrcBitmap = null;
        try { w.SrcBitmap = new Bitmap(path); w.OrigW = w.SrcBitmap.Width; w.OrigH = w.SrcBitmap.Height; }
        catch (Exception ex) { MessageBoxW(IntPtr.Zero, $"无法打开图片:\n{ex.Message}", "SnapView", 0); return false; }

        RECT wa = default;
        SystemParametersInfoW(SPI_GETWORKAREA, 0, ref wa, 0);
        double mw = (wa.Right - wa.Left) * 0.8, mh = (wa.Bottom - wa.Top) * 0.8;
        w.Scale = 1.0;
        if (w.OrigW > mw || w.OrigH > mh)
            w.Scale = Math.Min(mw / w.OrigW, mh / w.OrigH);

        BuildList(w, path);
        return true;
    }

    public static void BuildList(ImageWindow w, string currentFile)
    {
        w.ImageFiles.Clear();
        var dir = Path.GetDirectoryName(Path.GetFullPath(currentFile)) ?? "";
        w.CurrentFolder = dir;
        try
        {
            w.ImageFiles.AddRange(Directory.EnumerateFiles(dir)
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(f => f, StringComparer.CurrentCultureIgnoreCase));
            w.CurrentIndex = w.ImageFiles.FindIndex(f =>
                string.Equals(f, currentFile, StringComparison.OrdinalIgnoreCase));
        }
        catch { w.ImageFiles.Add(currentFile); w.CurrentIndex = 0; }
    }

    public static void Navigate(ImageWindow w, int delta)
    {
        if (w.ImageFiles.Count == 0 || w.CurrentIndex < 0) return;
        int i = w.CurrentIndex + delta;
        if (i < 0) i = w.ImageFiles.Count - 1;
        if (i >= w.ImageFiles.Count) i = 0;
        if (i == w.CurrentIndex) return;
        Load(w, w.ImageFiles[i]);
        Rendering.SetScale(w, w.Scale);
        Rendering.Render(w);
    }
}
