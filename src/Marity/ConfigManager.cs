using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marity;

internal static class ConfigManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static string ConfigPath { get; } = Path.Combine(AppContext.BaseDirectory, "config.json");

    private static string LogPath { get; } = Path.Combine(AppContext.BaseDirectory, "marity.log");

    public static AppConfig Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                var defaults = new AppConfig();
                Save(defaults);
                return defaults;
            }

            var json = File.ReadAllText(ConfigPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
            return Sanitize(config);
        }
        catch (Exception ex)
        {
            Log($"Failed to load config.json, falling back to defaults: {ex.Message}");
            return new AppConfig();
        }
    }

    public static void Save(AppConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            Log($"Failed to save config.json: {ex.Message}");
        }
    }

    private static AppConfig Sanitize(AppConfig config)
    {
        // Guard against nonsensical values in a hand-edited config.json so the app never
        // ends up with a zero/negative timer interval or a runaway speed multiplier.
        config.TickIntervalMs = Math.Max(1, config.TickIntervalMs);
        config.MoveStepPixels = Math.Max(1, config.MoveStepPixels);
        config.MaxStepPixels = Math.Max(config.MoveStepPixels, config.MaxStepPixels);
        config.Acceleration = config.Acceleration <= 0 ? 1.0 : config.Acceleration;
        return config;
    }

    public static void Log(string message)
    {
        try
        {
            File.AppendAllText(LogPath, $"{DateTime.Now:u} {message}{Environment.NewLine}");
        }
        catch
        {
            // Best effort only - logging must never crash the app.
        }
    }
}
