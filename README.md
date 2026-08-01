<img src="assets/marity-logo.svg" width="96" height="96" alt="Marity logo" />

# Marity

An open source Windows utility that lets your keyboard act as your mouse, lightweight
and easily customizable. Move the cursor with the arrow keys, click with the
right-hand modifier keys.

- **Arrow keys**: move the cursor (held keys accelerate, diagonals work too)
- **Right Shift**: left click
- **Right Ctrl**: right click
- **F8**: toggle the app on/off without closing it

The left Shift/Ctrl keys and every other key are left completely alone, so normal
typing is unaffected. Marity runs from the system tray with no visible window.

## Requirements

- Windows 10 or later
- [.NET 10 SDK](https://dotnet.microsoft.com/download) to build/run

## Run it

```sh
dotnet run --project src/Marity/Marity.csproj
```

A tray icon appears (green = active, grey = paused). Right-click it for options:
Enabled, Start with Windows, Reload Config, Open Config Folder, Exit. Double-clicking
the icon also toggles enabled/paused, same as F8.

## Build a standalone exe

```sh
dotnet publish src/Marity/Marity.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
```

This produces `publish/Marity.exe`, which runs without a .NET install on the target
machine. `config.json` is read from the same folder as the exe, and is created there
automatically (with defaults) the first time it's missing.

## Configuration (`config.json`)

See [config.md](config.md) for the full reference, including every key, what it does,
accepted key names, and how to tune movement feel.

## Logo / icon

`assets/marity-logo.svg` is the source of truth for Marity's badge, a gradient
rounded square with a cursor kite. `src/Marity/marity.ico` (the app/taskbar icon) and
the live tray icon in `TrayIconFactory.cs` both reproduce the same design in code.

## Notes / limitations

- Marity intercepts arrow keys and the two click keys **only while enabled**, and only
  those specific keys. Everything else always passes through untouched, and the left
  Shift/Ctrl keys are never touched.
- It uses a low-level global keyboard hook (`WH_KEYBOARD_LL`), which does not require
  administrator privileges. However, Windows' UIPI security blocks non-elevated
  processes from sending focus-affecting input to windows running elevated ("Run as
  Administrator"). If you need mouse control inside an elevated app, run Marity
  elevated too.
- Only one instance runs at a time (enforced via a named mutex). Launching a second
  copy shows a message box and exits.

## License

[Mozilla Public License 2.0](LICENSE)
