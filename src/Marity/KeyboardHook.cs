using System.Runtime.InteropServices;

namespace Marity;

/// <summary>
/// Thin wrapper around a WH_KEYBOARD_LL hook. Handlers return true to swallow the
/// keystroke system-wide (it never reaches the focused window), or false to let it
/// pass through untouched.
/// </summary>
internal sealed class KeyboardHook : IDisposable
{
    // Keeping a reference to the delegate for the lifetime of the hook is required -
    // otherwise the GC can collect it while native code still holds a function pointer to it.
    private readonly NativeMethods.LowLevelKeyboardProc _proc;
    private IntPtr _hookId = IntPtr.Zero;

    public event Func<int, bool, bool>? KeyEvent;

    public KeyboardHook()
    {
        _proc = HookCallback;
    }

    public void Install()
    {
        if (_hookId != IntPtr.Zero) return;

        IntPtr hModule = NativeMethods.GetModuleHandle(null);
        _hookId = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _proc, hModule, 0);

        if (_hookId == IntPtr.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"Failed to install the global keyboard hook (Win32 error {error}).");
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            bool isDown = msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN;
            bool isUp = msg == NativeMethods.WM_KEYUP || msg == NativeMethods.WM_SYSKEYUP;

            if (isDown || isUp)
            {
                try
                {
                    var data = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
                    if (KeyEvent?.Invoke(data.vkCode, isDown) == true)
                    {
                        return (IntPtr)1;
                    }
                }
                catch (Exception ex)
                {
                    // A hook callback must never throw across the native boundary - Windows will
                    // silently unhook us (or worse) if we take too long or blow up. Log and pass through.
                    ConfigManager.Log($"Keyboard hook callback error: {ex}");
                }
            }
        }

        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }
}
