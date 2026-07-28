# SnapView

A minimal, keyboard-driven image viewer for Windows. Displays images in a borderless layered window that can be pinned above other applications, scaled beyond the physical screen resolution, and made translucent for overlay use.

## Feature Overview

**Arbitrary zoom range (10%–500%).** Images scale continuously via scroll wheel or corner-drag. Window dimensions are not clamped to the display resolution, and the rendering path avoids the WPF `DrawImage` clipping issue by composing frames through `UpdateLayeredWindow` with a GDI+ back buffer.

**Per-window transparency.** Opacity is adjustable with Ctrl+Scroll and implemented via the `SourceConstantAlpha` field of `BLENDFUNCTION`, avoiding `SetLayeredWindowAttributes` which interferes with per-pixel alpha blending.

**Always-on-top mode.** Toggled by middle-click. Uses `HWND_TOPMOST` / `HWND_NOTOPMOST` with `SetWindowPos`.

**GIF animation playback.** Frame delays are extracted from the GIF metadata (property tag `0x5100`). Frame advancement is driven by a Win32 timer (`WM_TIMER`).

**Ctrl+Click white background.** A convenience for images with alpha channels; fills the image rect with white before drawing the source bitmap.

**Directory browsing.** Left/Right arrow keys navigate alphabetically through sibling image files in the same directory. A 500 ms overlay in the top-left corner shows the current filename, zoom level, and opacity after each change.

**File format support** via GDI+: JPEG, PNG, BMP, GIF, WebP.

## Controls

| Input                    | Action                          |
| ------------------------ | ------------------------------- |
| Left-drag                | Move window                     |
| Scroll wheel             | Zoom                            |
| Drag bottom-right corner | Resize                          |
| Ctrl + Scroll            | Opacity                         |
| Middle-click             | Toggle always-on-top            |
| Ctrl + Left-click        | Toggle white background         |
| Left / Right arrow       | Previous / next image in folder |
| Esc                      | Close window                    |

## Build

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
# Run directly
dotnet run -- "path\to\image.jpg"

# Publish standalone executable (no runtime required on target machine)
dotnet publish -c Release -o publish
```

### Installer

An [Inno Setup 6](https://jrsoftware.org/isdl.php) script is provided (`installer.iss`). It produces a setup executable offering optional file associations for JPEG, PNG, BMP, GIF, and WebP.

```powershell
powershell -ExecutionPolicy Bypass -File .\build_installer.ps1
```

Output: `installer/SnapView_Setup.exe`.

## Implementation Notes

The project is a .NET 8 console application compiled as `WinExe`. It does not depend on WPF, WinForms, or any UI framework. Window creation and message handling use P/Invoke calls to `user32.dll` and `gdi32.dll` directly.

A single-threaded message loop (`GetMessage` / `DispatchMessage`) processes input. Image rendering uses `System.Drawing` (GDI+) to composite the scaled source image with a radial border onto a 32-bit ARGB bitmap, which is then passed to `UpdateLayeredWindow` with the `ULW_ALPHA` flag for per-pixel compositing by the DWM.

Source files are organized under `src/`:

| File               | Purpose                                                  |
| ------------------ | -------------------------------------------------------- |
| `Program.cs`       | Entry point, window class registration, message loop     |
| `NativeMethods.cs` | All Win32 structs, constants, and P/Invoke declarations  |
| `ImageWindow.cs`   | Per-window state                                         |
| `WindowManager.cs` | `WndProc`—message dispatch                               |
| `Rendering.cs`     | Scale computation, GDI+ rendering, `UpdateLayeredWindow` |
| `ImageLoader.cs`   | File I/O, directory enumeration, GIF metadata parsing    |

## License

MIT
