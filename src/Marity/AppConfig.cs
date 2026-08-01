namespace Marity;

/// <summary>Maps 1:1 onto config.json. Values are validated/clamped by ConfigManager after load.</summary>
public sealed class AppConfig
{
    public int MoveStepPixels { get; set; } = 8;
    public int TickIntervalMs { get; set; } = 15;
    public double Acceleration { get; set; } = 1.08;
    public int MaxStepPixels { get; set; } = 40;
    public bool NormalizeDiagonalSpeed { get; set; } = true;
    public bool StartEnabled { get; set; } = true;
    public bool ShowTrayNotifications { get; set; } = true;
    public bool RunAtWindowsStartup { get; set; } = false;
    public string ToggleKey { get; set; } = "F8";
    public string LeftClickKey { get; set; } = "RShiftKey";
    public string RightClickKey { get; set; } = "RControlKey";
}
