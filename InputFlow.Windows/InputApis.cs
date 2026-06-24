using System;
using System.Runtime.InteropServices;
using System.Text;

namespace InputFlow.Windows
{
    /// <summary>
    /// Exposes native Win32 APIs used for enumerating and switching keyboard layouts and input methods.
    /// </summary>
    internal static class InputApis
    {
        /// <summary>
        /// Retrieves the keyboard layouts available for the system or the calling thread.
        /// </summary>
        /// <param name="nBuff">Number of handles that <paramref name="list"/> can contain.</param>
        /// <param name="list">An array that receives the input locale identifiers (HKLs).</param>
        /// <returns>The number of handles copied to the buffer or, if nBuff is zero, the number of locales available.</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetKeyboardLayoutList(int nBuff, [Out] IntPtr[] list);

        /// <summary>
        /// Retrieves the name of the active keyboard layout.
        /// </summary>
        /// <param name="pwszKLID">A pointer to a buffer that receives the 8‑character KLID string.</param>
        /// <returns>True if successful; otherwise false.</returns>
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetKeyboardLayoutName(StringBuilder pwszKLID);

        /// <summary>
        /// Retrieves the active input locale identifier (HKL) for the specified thread.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetKeyboardLayout(uint idThread);

        /// <summary>
        /// Loads the specified keyboard layout into the system.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

        /// <summary>
        /// Activates a loaded keyboard layout for the calling thread.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint Flags);

        /// <summary>
        /// Sets the default input language for the system or the current thread.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        public const uint SPI_SETDEFAULTINPUTLANG = 0x005A;
        public const uint SPIF_SENDWININICHANGE = 0x02;

        /// <summary>
        /// Retrieves a handle to the foreground window.
        /// </summary>
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Retrieves the identifier of the thread that created the specified window and the identifier of the process that created the window.
        /// </summary>
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        /// <summary>
        /// Obtains the full path of the executable file of the specified process.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("psapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpFilename, int nSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        public const uint PROCESS_QUERY_INFORMATION = 0x0400;
        public const uint PROCESS_VM_READ = 0x0010;
    }
}