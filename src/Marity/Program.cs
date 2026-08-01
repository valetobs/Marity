using System.Windows.Forms;

namespace Marity;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using var mutex = new Mutex(true, "Marity.SingleInstance.Mutex", out bool isNew);
        if (!isNew)
        {
            MessageBox.Show("Marity is already running (check your system tray).", "Marity",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            Application.Run(new TrayContext());
        }
        catch (Exception ex)
        {
            ConfigManager.Log($"Fatal error: {ex}");
            MessageBox.Show($"Marity failed to start:\n\n{ex.Message}", "Marity",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
