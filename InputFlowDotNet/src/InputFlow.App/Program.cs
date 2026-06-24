using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using InputFlow.Core;

namespace InputFlow.App
{
    /// <summary>
    /// Entry point for the InputFlow tray application.  This is a minimal stub
    /// illustrating how to register a global hotkey and delegate to
    /// <see cref="InputSwitcher"/>.  A complete implementation should include
    /// a settings UI, configuration loading, error handling, and tray menu
    /// interactions.
    /// </summary>
    internal static class Program
    {
        private const int HOTKEY_ID = 0xBEEF;
        private static NotifyIcon _notifyIcon;
        private static InputSwitcher _switcher;

        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Initialize the input switcher for Korean (0x0412) with fallback to US English (0x0409).
            _switcher = new InputSwitcher(0x0412, 0x0409);

            // Create a simple tray icon.
            _notifyIcon = new NotifyIcon
            {
                Text = "InputFlow",
                Icon = System.Drawing.SystemIcons.Application,
                Visible = true,
                ContextMenuStrip = BuildContextMenu()
            };

            // Register a global hotkey: Ctrl+Shift+Space (example).  In production,
            // make this configurable and avoid common conflicts.
            RegisterHotKey(IntPtr.Zero, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, (int)Keys.Space);

            Application.ApplicationExit += OnExit;
            Application.Run();
        }

        private static ContextMenuStrip BuildContextMenu()
        {
            var menu = new ContextMenuStrip();
            var exitItem = new ToolStripMenuItem("Exit", null, (s, e) => Application.Exit());
            menu.Items.Add(exitItem);
            return menu;
        }

        private static void OnExit(object sender, EventArgs e)
        {
            UnregisterHotKey(IntPtr.Zero, HOTKEY_ID);
            _notifyIcon?.Dispose();
        }

        protected static void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                _switcher.Toggle();
            }
        }

        // P/Invoke definitions for registering and unregistering hotkeys.
        private const int MOD_CONTROL = 0x0002;
        private const int MOD_SHIFT = 0x0004;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
