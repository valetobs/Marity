using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Marity;

/// <summary>
/// Owns the movement timer (arrow keys held down move the cursor on a fixed tick,
/// independent of the OS key-repeat rate) and the click simulation helpers.
/// </summary>
internal sealed class MouseMover
{
    private static readonly double Sqrt2 = Math.Sqrt(2);

    private readonly System.Windows.Forms.Timer _timer;
    private readonly HashSet<Keys> _pressedDirections = new();
    private AppConfig _config;
    private int _ticksHeld;

    public MouseMover(AppConfig config)
    {
        _config = config;
        _timer = new System.Windows.Forms.Timer { Interval = Math.Max(1, config.TickIntervalMs) };
        _timer.Tick += (_, _) => Tick();
    }

    public void UpdateConfig(AppConfig config)
    {
        _config = config;
        _timer.Interval = Math.Max(1, config.TickIntervalMs);
    }

    public void SetDirectionPressed(Keys key, bool pressed)
    {
        if (pressed)
        {
            _pressedDirections.Add(key);
        }
        else
        {
            _pressedDirections.Remove(key);
        }

        if (_pressedDirections.Count > 0)
        {
            if (!_timer.Enabled)
            {
                _ticksHeld = 0;
                _timer.Start();
            }
        }
        else
        {
            _timer.Stop();
            _ticksHeld = 0;
        }
    }

    public void StopAll()
    {
        _pressedDirections.Clear();
        _timer.Stop();
        _ticksHeld = 0;
    }

    private void Tick()
    {
        int dx = 0, dy = 0;
        if (_pressedDirections.Contains(Keys.Left)) dx -= 1;
        if (_pressedDirections.Contains(Keys.Right)) dx += 1;
        if (_pressedDirections.Contains(Keys.Up)) dy -= 1;
        if (_pressedDirections.Contains(Keys.Down)) dy += 1;

        if (dx == 0 && dy == 0) return;

        double speed = Math.Min(_config.MaxStepPixels, _config.MoveStepPixels * Math.Pow(_config.Acceleration, _ticksHeld));
        _ticksHeld++;

        double mx = dx * speed;
        double my = dy * speed;
        if (_config.NormalizeDiagonalSpeed && dx != 0 && dy != 0)
        {
            mx /= Sqrt2;
            my /= Sqrt2;
        }

        MoveCursorBy((int)Math.Round(mx), (int)Math.Round(my));
    }

    private static void MoveCursorBy(int dx, int dy)
    {
        if (!NativeMethods.GetCursorPos(out var p)) return;

        int minX = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
        int minY = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
        int width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
        int height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);

        int newX = Math.Clamp(p.X + dx, minX, minX + width - 1);
        int newY = Math.Clamp(p.Y + dy, minY, minY + height - 1);

        NativeMethods.SetCursorPos(newX, newY);
    }

    public static void LeftDown() => SendMouse(NativeMethods.MOUSEEVENTF_LEFTDOWN);
    public static void LeftUp() => SendMouse(NativeMethods.MOUSEEVENTF_LEFTUP);
    public static void RightDown() => SendMouse(NativeMethods.MOUSEEVENTF_RIGHTDOWN);
    public static void RightUp() => SendMouse(NativeMethods.MOUSEEVENTF_RIGHTUP);

    private static void SendMouse(uint flags)
    {
        var input = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_MOUSE,
            U = new NativeMethods.InputUnion { mi = new NativeMethods.MOUSEINPUT { dwFlags = flags } },
        };

        NativeMethods.SendInput(1, new[] { input }, Marshal.SizeOf<NativeMethods.INPUT>());
    }
}
