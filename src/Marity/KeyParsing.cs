using System.Windows.Forms;

namespace Marity;

internal static class KeyParsing
{
    public static Keys Parse(string? value, Keys fallback)
    {
        if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<Keys>(value, ignoreCase: true, out var key))
        {
            return key;
        }

        ConfigManager.Log($"Could not parse key '{value}' from config.json, using default '{fallback}'.");
        return fallback;
    }
}
