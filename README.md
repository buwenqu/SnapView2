# SnapView

A lightweight, borderless image viewer for Windows. Pin images to your desktop like sticky notes — zoom infinitely, adjust opacity, toggle always-on-top, and navigate folders with keyboard shortcuts.

## Features

- **Borderless & Pinnable** — No title bar, no window chrome. Image floats on your desktop.
- **Infinite Zoom** — Scale images far beyond your screen resolution without clipping.
- **Outer Glow Border** — Blue border when focused, green when always-on-top. Fades elegantly outward.
- **Opacity Control** — Ctrl + Scroll to adjust transparency.
- **Always-on-Top** — Middle-click to toggle. Keep reference images visible above other windows.
- **Folder Navigation** — Left / Right arrow keys to browse images in the same folder.
- **Corner Resize** — Drag the bottom-right corner to scale.
- **Zero .NET Dependency** — Fully self-contained single executable. Runs on any Windows 10/11 machine.

## Usage

| Action          | How                                                         |
| --------------- | ----------------------------------------------------------- |
| Open image      | Double-click an associated image file, or drag onto the exe |
| Move            | Left-click and drag anywhere on the image                   |
| Zoom            | Scroll wheel                                                |
| Resize          | Drag the bottom-right corner                                |
| Opacity         | Ctrl + Scroll wheel                                         |
| Always-on-top   | Middle-click                                                |
| Next / Previous | Right / Left arrow keys                                     |
| Close           | Esc                                                         |

## Build

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Quick Build

```powershell
dotnet run -- "path\to\image.jpg"
```

### Publish (standalone exe)

```powershell
dotnet publish -c Release -o publish
```

### Create Installer

Requires [Inno Setup 6](https://jrsoftware.org/isdl.php). Then:

```powershell
powershell -ExecutionPolicy Bypass -File .\build_installer.ps1
```

Output: `installer\SnapView_Setup.exe` — distributable installer with optional file associations.

## Architecture

Pure Win32 window via P/Invoke + GDI+ rendering + `UpdateLayeredWindow`. No WPF, no WinForms. The message loop is a standard `GetMessage` / `DispatchMessage` loop, and all input is handled in a custom `WndProc`.

## License

MIT
