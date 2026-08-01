# Configuration

Marity reads `config.json` from the same folder as `Marity.exe`. If the file is
missing, it's created automatically with the defaults below on first launch.

| Key | Default | Meaning |
|---|---|---|
| `MoveStepPixels` | `8` | Starting cursor speed, in pixels per tick |
| `TickIntervalMs` | `15` | How often the cursor moves while a key is held, in ms |
| `Acceleration` | `1.08` | Speed multiplier applied per tick while a direction is held |
| `MaxStepPixels` | `40` | Speed cap, in pixels per tick |
| `NormalizeDiagonalSpeed` | `true` | Keep diagonal movement the same speed as straight movement |
| `StartEnabled` | `true` | Whether Marity starts active or paused |
| `ShowTrayNotifications` | `true` | Show a balloon tip when toggling enabled/paused |
| `RunAtWindowsStartup` | `false` | Registers/unregisters Marity in the current user's Run key |
| `ToggleKey` | `"F8"` | Key that pauses/resumes keyboard control |
| `LeftClickKey` | `"RShiftKey"` | Key that simulates a left click |
| `RightClickKey` | `"RControlKey"` | Key that simulates a right click |

## Key names

`ToggleKey`, `LeftClickKey` and `RightClickKey` accept any
[`System.Windows.Forms.Keys`](https://learn.microsoft.com/dotnet/api/system.windows.forms.keys)
enum name, for example:

- `"F9"`, `"F10"`, ... for a different toggle key
- `"LShiftKey"` / `"LControlKey"` if you'd rather repurpose the *left* modifiers instead
  of the right ones (note this also frees up the right modifiers for normal typing)
- `"OemPipe"`, `"CapsLock"`, `"Scroll"`, etc. for anything else on the keyboard

If a value can't be parsed, Marity logs a warning to `marity.log` next to the exe and
falls back to the default for that key.

## Movement feel

Cursor speed ramps up the longer a direction is held: each tick, speed is multiplied
by `Acceleration` until it hits `MaxStepPixels`. With the defaults (`MoveStepPixels: 8`,
`Acceleration: 1.08`, `TickIntervalMs: 15`), the cursor reaches max speed in about a
third of a second. To make movement:

- **Slower to start** — lower `MoveStepPixels`
- **Snap to full speed faster** — raise `Acceleration`
- **Feel more linear (no ramp-up)** — set `Acceleration` to `1.0`
- **Have a higher top speed** — raise `MaxStepPixels`
- **Feel smoother/choppier** — lower/raise `TickIntervalMs` (lower = smoother, more CPU wakeups)

## Applying changes

Edit `config.json`, then use the tray icon's **Reload Config** menu item to apply
changes without restarting the app. **Open Config Folder** opens Explorer with
`config.json` pre-selected.
