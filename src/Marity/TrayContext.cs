using System.Windows.Forms;
using Microsoft.Win32;

namespace Marity;

/// <summary>
/// Owns the tray icon, the keyboard hook and the mouse mover, and wires them together.
/// This is the only place that knows about the current enabled/disabled state.
/// </summary>
internal sealed class TrayContext : ApplicationContext
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "Marity";

    private readonly NotifyIcon _trayIcon;
    private readonly KeyboardHook _hook;
    private readonly MouseMover _mouseMover;

    private AppConfig _config;
    private bool _enabled;
    private Icon? _currentIcon;

    private bool _toggleKeyPhysicallyDown;
    private bool _leftClickPhysicallyDown;
    private bool _rightClickPhysicallyDown;

    private Keys _toggleKey;
    private Keys _leftClickKey;
    private Keys _rightClickKey;

    private ToolStripMenuItem _enabledMenuItem = null!;
    private ToolStripMenuItem _startupMenuItem = null!;

    public TrayContext()
    {
        _config = ConfigManager.Load();
        ApplyKeyBindings();
        _mouseMover = new MouseMover(_config);
        _enabled = _config.StartEnabled;

        _trayIcon = new NotifyIcon
        {
            Visible = true,
            ContextMenuStrip = BuildMenu(),
        };
        SetTrayIcon(_enabled);
        _trayIcon.DoubleClick += (_, _) => ToggleEnabled();

        ApplyStartupSetting(_config.RunAtWindowsStartup);

        _hook = new KeyboardHook();
        _hook.KeyEvent += OnKeyEvent;
        _hook.Install();

        ShowBalloon(_enabled
            ? $"Marity is active. Arrow keys move the mouse. Press {_toggleKey} to pause."
            : $"Marity is paused. Press {_toggleKey} to enable.");
    }

    private void ApplyKeyBindings()
    {
        _toggleKey = KeyParsing.Parse(_config.ToggleKey, Keys.F8);
        _leftClickKey = KeyParsing.Parse(_config.LeftClickKey, Keys.RShiftKey);
        _rightClickKey = KeyParsing.Parse(_config.RightClickKey, Keys.RControlKey);
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        _enabledMenuItem = new ToolStripMenuItem("Enabled", null, (_, _) => ToggleEnabled())
        {
            Checked = _enabled,
        };
        menu.Items.Add(_enabledMenuItem);

        menu.Items.Add(new ToolStripSeparator());

        _startupMenuItem = new ToolStripMenuItem("Start with Windows", null, (_, _) => ToggleStartup())
        {
            Checked = _config.RunAtWindowsStartup,
        };
        menu.Items.Add(_startupMenuItem);

        menu.Items.Add(new ToolStripMenuItem("Reload Config", null, (_, _) => ReloadConfig()));
        menu.Items.Add(new ToolStripMenuItem("Open Config Folder", null, (_, _) => OpenConfigFolder()));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => ExitApp()));

        return menu;
    }

    /// <summary>Returns true to swallow the key so it never reaches the focused window.</summary>
    private bool OnKeyEvent(int vkCode, bool isDown)
    {
        var key = (Keys)vkCode;

        if (key == _toggleKey)
        {
            if (isDown)
            {
                if (!_toggleKeyPhysicallyDown)
                {
                    _toggleKeyPhysicallyDown = true;
                    ToggleEnabled();
                }
            }
            else
            {
                _toggleKeyPhysicallyDown = false;
            }
            return true;
        }

        if (!_enabled) return false;

        switch (key)
        {
            case Keys.Up:
            case Keys.Down:
            case Keys.Left:
            case Keys.Right:
                _mouseMover.SetDirectionPressed(key, isDown);
                return true;
        }

        if (key == _leftClickKey)
        {
            if (isDown && !_leftClickPhysicallyDown)
            {
                _leftClickPhysicallyDown = true;
                MouseMover.LeftDown();
            }
            else if (!isDown)
            {
                _leftClickPhysicallyDown = false;
                MouseMover.LeftUp();
            }
            return true;
        }

        if (key == _rightClickKey)
        {
            if (isDown && !_rightClickPhysicallyDown)
            {
                _rightClickPhysicallyDown = true;
                MouseMover.RightDown();
            }
            else if (!isDown)
            {
                _rightClickPhysicallyDown = false;
                MouseMover.RightUp();
            }
            return true;
        }

        return false;
    }

    private void ToggleEnabled()
    {
        _enabled = !_enabled;

        if (!_enabled)
        {
            _mouseMover.StopAll();
            if (_leftClickPhysicallyDown)
            {
                MouseMover.LeftUp();
                _leftClickPhysicallyDown = false;
            }
            if (_rightClickPhysicallyDown)
            {
                MouseMover.RightUp();
                _rightClickPhysicallyDown = false;
            }
        }

        _enabledMenuItem.Checked = _enabled;
        SetTrayIcon(_enabled);
        ShowBalloon(_enabled ? "Marity enabled" : "Marity paused");
    }

    private void ToggleStartup()
    {
        _config.RunAtWindowsStartup = !_config.RunAtWindowsStartup;
        _startupMenuItem.Checked = _config.RunAtWindowsStartup;
        ApplyStartupSetting(_config.RunAtWindowsStartup);
        ConfigManager.Save(_config);
    }

    private void ApplyStartupSetting(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key == null) return;

            if (enable)
            {
                string exePath = Environment.ProcessPath ?? Application.ExecutablePath;
                key.SetValue(RunValueName, $"\"{exePath}\"");
            }
            else if (key.GetValue(RunValueName) != null)
            {
                key.DeleteValue(RunValueName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex)
        {
            ConfigManager.Log($"Failed to update startup registry entry: {ex.Message}");
        }
    }

    private void ReloadConfig()
    {
        _config = ConfigManager.Load();
        ApplyKeyBindings();
        _mouseMover.UpdateConfig(_config);
        _startupMenuItem.Checked = _config.RunAtWindowsStartup;
        ShowBalloon("Config reloaded");
    }

    private void OpenConfigFolder()
    {
        try
        {
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{ConfigManager.ConfigPath}\"");
        }
        catch (Exception ex)
        {
            ConfigManager.Log($"Failed to open config folder: {ex.Message}");
        }
    }

    private void ExitApp()
    {
        _hook.Dispose();
        _mouseMover.StopAll();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _currentIcon?.Dispose();
        ExitThread();
    }

    private void SetTrayIcon(bool enabled)
    {
        var newIcon = TrayIconFactory.Create(enabled);
        var old = _currentIcon;
        _trayIcon.Icon = newIcon;
        _trayIcon.Text = $"Marity - {(enabled ? "Active" : "Paused")}";
        _currentIcon = newIcon;
        old?.Dispose();
    }

    private void ShowBalloon(string text)
    {
        if (!_config.ShowTrayNotifications) return;
        _trayIcon.BalloonTipTitle = "Marity";
        _trayIcon.BalloonTipText = text;
        _trayIcon.ShowBalloonTip(1500);
    }
}
