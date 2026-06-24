using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using InputFlow.Windows;

namespace InputFlow.Core
{
    /// <summary>
    /// Coordinates the toggling logic, profile selection, verification, and exclusion
    /// handling for InputFlow.  This class encapsulates the state machine described in
    /// the design document.  It supports multiple hotkeys, each of which can have its
    /// own target profile, return behaviour and fallback profile.
    /// </summary>
    public class InputFlowManager
    {
        private readonly IReadOnlyList<InputProfile> _installedProfiles;
        private readonly ILogger _logger;
        private readonly Dictionary<int, HotkeyState> _hotkeyStates = new();
        private readonly HashSet<string> _excludedProcessNames;

        /// <summary>
        /// Indicates whether InputFlow is currently paused.  When paused, toggle
        /// requests are ignored.
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// Creates a new <see cref="InputFlowManager"/>.
        /// </summary>
        /// <param name="installedProfiles">Installed input profiles as enumerated by <see cref="InputProfileManager"/>.</param>
        /// <param name="excludedProcesses">Process names in which toggling should be disabled.</param>
        /// <param name="logger">Logger for diagnostics.</param>
        public InputFlowManager(IReadOnlyList<InputProfile> installedProfiles, IEnumerable<string> excludedProcesses, ILogger logger)
        {
            _installedProfiles = installedProfiles;
            _logger = logger;
            _excludedProcessNames = new HashSet<string>(excludedProcesses ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Registers a hotkey with the manager.  The <paramref name="id"/> must match the
        /// identifier used when registering the hotkey with Windows.  The specified
        /// <paramref name="state"/> must contain a valid target profile; fallback may be null.
        /// </summary>
        public void RegisterHotkey(int id, InputProfile target, InputProfile? fallback, string returnBehavior)
        {
            _hotkeyStates[id] = new HotkeyState
            {
                Target = target,
                Fallback = fallback,
                ReturnBehavior = ParseReturnBehavior(returnBehavior)
            };
        }

        /// <summary>
        /// Called when a registered hotkey is pressed.  Determines whether to switch
        /// into or out of the target profile based on the current active profile.
        /// </summary>
        /// <param name="hotkeyId">Identifier of the pressed hotkey.</param>
        public void OnHotkeyPressed(int hotkeyId)
        {
            if (IsPaused)
            {
                _logger.Info("Toggle ignored because InputFlow is paused.");
                return;
            }

            // Determine the foreground process.  If excluded, ignore.
            string foregroundProcess = GetForegroundProcessName();
            if (!string.IsNullOrEmpty(foregroundProcess) && _excludedProcessNames.Contains(foregroundProcess))
            {
                _logger.Info($"Toggle ignored in excluded process: {foregroundProcess}");
                return;
            }

            if (!_hotkeyStates.TryGetValue(hotkeyId, out var state))
            {
                _logger.Error($"No hotkey state found for ID {hotkeyId}");
                return;
            }

            InputProfile current = GetCurrentProfile();

            if (ProfilesEqual(current, state.Target))
            {
                // We are currently in the target.  Determine the destination based on return behaviour.
                InputProfile? dest = null;
                switch (state.ReturnBehavior)
                {
                    case ReturnBehavior.LastNonTarget:
                        dest = state.PreviousNonTarget ?? state.Fallback;
                        break;
                    case ReturnBehavior.AlwaysSpecificLayout:
                        dest = state.Fallback;
                        break;
                    case ReturnBehavior.ManualOnly:
                        // In manualOnly, we never automatically switch back.  Do nothing.
                        _logger.Info("ManualOnly return behaviour: not switching back.");
                        return;
                }
                if (dest == null)
                {
                    _logger.Warning("No fallback or previous profile available; not switching.");
                    return;
                }
                bool success = SwitchTo(dest);
                if (success)
                {
                    _logger.Info($"Switched back to {dest.FriendlyName}");
                    state.PreviousNonTarget = null;
                }
                else
                {
                    _logger.Error($"Failed to switch back to {dest.FriendlyName}");
                }
            }
            else
            {
                // Not in target; remember current and switch to target.
                state.PreviousNonTarget = current;
                bool success = SwitchTo(state.Target);
                if (success)
                {
                    _logger.Info($"Switched to target {state.Target.FriendlyName}");
                }
                else
                {
                    _logger.Error($"Failed to switch to target {state.Target.FriendlyName}");
                }
            }
        }

        /// <summary>
        /// Sets the paused state.  When paused, hotkey presses are ignored except for
        /// a resume/disabled override.  Logging is produced to indicate the
        /// transition.
        /// </summary>
        /// <param name="paused">True to pause; false to resume.</param>
        public void SetPaused(bool paused)
        {
            if (IsPaused != paused)
            {
                IsPaused = paused;
                _logger.Info(paused ? "InputFlow paused." : "InputFlow resumed.");
            }
        }

        /// <summary>
        /// Determines whether two profiles represent the same input method by comparing
        /// their KLIDs.  If either profile is null, returns false.
        /// </summary>
        private static bool ProfilesEqual(InputProfile? a, InputProfile? b)
        {
            if (a == null || b == null) return false;
            return string.Equals(a.KLID, b.KLID, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Attempts to switch to the specified profile using the Win32 API and
        /// verifies the switch by reading back the current profile.  Returns
        /// true if the switch succeeded; false otherwise.
        /// </summary>
        private bool SwitchTo(InputProfile profile)
        {
            try
            {
                // Load the layout if not already loaded; returns a handle.
                IntPtr newHKL = InputApis.LoadKeyboardLayout(profile.KLID, 0);
                // Activate the layout for the current thread.
                IntPtr result = InputApis.ActivateKeyboardLayout(newHKL, 0);
                // Optionally set as system default.  Some users prefer not to
                // change the default layout; design document suggests we
                // broadcast the change via SystemParametersInfo to encourage
                // system-wide adoption.  Note: This might require admin
                // privileges in some contexts.  We trap exceptions and ignore.
                try
                {
                    var handle = System.Runtime.InteropServices.GCHandle.Alloc(profile.LangId, System.Runtime.InteropServices.GCHandleType.Pinned);
                    try
                    {
                        InputApis.SystemParametersInfo(InputApis.SPI_SETDEFAULTINPUTLANG, 0, handle.AddrOfPinnedObject(), InputApis.SPIF_SENDWININICHANGE);
                    }
                    finally
                    {
                        handle.Free();
                    }
                }
                catch
                {
                    // Non-critical; ignore failure to set default language.
                }

                // Verify by checking the current profile.
                InputProfile after = GetCurrentProfile();
                return ProfilesEqual(after, profile);
            }
            catch (Exception ex)
            {
                _logger.Error($"Exception during switch: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Determines the current input profile for the active thread.  If no
        /// matching installed profile is found, returns a placeholder profile.
        /// </summary>
        private InputProfile GetCurrentProfile()
        {
            uint threadId = InputApis.GetWindowThreadProcessId(InputApis.GetForegroundWindow(), out _);
            IntPtr currentHkl = InputApis.GetKeyboardLayout(threadId);
            // Derive KLID from HKL by taking low word and converting to 8-digit hex.
            string klid = ((ulong)currentHkl.ToInt64() & 0xFFFFFFFF).ToString("X8");
            foreach (var profile in _installedProfiles)
            {
                if (string.Equals(profile.KLID, klid, StringComparison.OrdinalIgnoreCase))
                    return profile;
            }
            // Unknown profile; return a placeholder with minimal info.
            return new InputProfile(currentHkl, klid, klid, false);
        }

        /// <summary>
        /// Retrieves the name of the foreground process.  Returns empty string if
        /// retrieval fails.  Only the executable name (without path) is
        /// returned.
        /// </summary>
        private static string GetForegroundProcessName()
        {
            IntPtr hwnd = InputApis.GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return string.Empty;
            uint pid;
            InputApis.GetWindowThreadProcessId(hwnd, out pid);
            if (pid == 0) return string.Empty;
            IntPtr hProcess = InputApis.OpenProcess(InputApis.PROCESS_QUERY_INFORMATION | InputApis.PROCESS_VM_READ, false, pid);
            if (hProcess == IntPtr.Zero)
            {
                return string.Empty;
            }
            try
            {
                var builder = new System.Text.StringBuilder(260);
                if (InputApis.GetModuleFileNameEx(hProcess, IntPtr.Zero, builder, builder.Capacity) != 0)
                {
                    string fullPath = builder.ToString();
                    return Path.GetFileName(fullPath);
                }
                return string.Empty;
            }
            finally
            {
                InputApis.CloseHandle(hProcess);
            }
        }

        /// <summary>
        /// Enumeration of supported return behaviours.  More behaviours can be added
        /// in future versions.  See design document section 13 for semantics.
        /// </summary>
        private enum ReturnBehavior
        {
            LastNonTarget,
            AlwaysSpecificLayout,
            ManualOnly
        }

        /// <summary>
        /// Internal state for each hotkey.  Stores the target profile, fallback,
        /// previous non-target and return behaviour.
        /// </summary>
        private class HotkeyState
        {
            public InputProfile Target = null!;
            public InputProfile? Fallback;
            public InputProfile? PreviousNonTarget;
            public ReturnBehavior ReturnBehavior;
        }

        /// <summary>
        /// Parses the return behaviour string into an enum value.  Defaults to
        /// LastNonTarget when unknown.
        /// </summary>
        private static ReturnBehavior ParseReturnBehavior(string behavior)
        {
            if (string.IsNullOrEmpty(behavior)) return ReturnBehavior.LastNonTarget;
            return behavior.ToLowerInvariant() switch
            {
                "lastnontarget" => ReturnBehavior.LastNonTarget,
                "alwaysspecificlayout" => ReturnBehavior.AlwaysSpecificLayout,
                "manualonly" => ReturnBehavior.ManualOnly,
                _ => ReturnBehavior.LastNonTarget,
            };
        }
    }
}