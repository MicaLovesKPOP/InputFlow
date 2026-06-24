# InputFlow

InputFlow is a small utility for Windows that makes switching between keyboard
layouts and input methods seamless.  It is designed for people who juggle
multiple languages (for example English and Korean) and want a single hotkey
that “does the right thing”.  With InputFlow you no longer need to remember
different key combinations for toggling between languages or between the
Hangul and Latin modes within the Korean IME.  Instead, one key brings you
into Korean and back out again, while a secondary key combination lets you
return to your previous layout.

## Features

- **Single key to enter Korean** – press the **Right Alt** key (also known
  as `AltGr`) to instantly switch to the Korean IME.  When you’re already
  using the Korean IME, pressing Right Alt toggles between Hangul and
  English modes just like the built‑in Korean input does.
- **Return to your previous layout** – press **Right Ctrl + Right Alt** to
  jump back to whatever layout you were using before you switched to Korean.
  If you invoke the hotkey while not using Korean, it will switch you into
  Korean instead.
- **Respects your existing keyboard settings** – InputFlow never rewrites
  Windows hotkeys or language settings.  It uses the Windows API to load
  input layouts on demand and leaves the list of installed languages
  untouched.
- **Visual feedback** – brief tooltips appear when InputFlow switches your
  layout or toggles between Hangul and English.  You can disable these
  notifications by removing the `ToolTip` lines in the script.

## Installation

1. Download and install [AutoHotkey](https://www.autohotkey.com/) if you
   haven’t already.
2. Download `InputFlow.ahk` from this repository and save it somewhere on
   your computer.
3. Double‑click `InputFlow.ahk` to run the script.  An “H” icon will
   appear in your system tray indicating that InputFlow is running.  You can
   right‑click this icon to pause or exit the script.
4. Optionally, use `Compile Script` from AutoHotkey to create a standalone
   executable so you can run InputFlow without installing AutoHotkey.

## Usage

- **Switch to Korean / Toggle Hangul** – press the **Right Alt** key.
  - If you are using any other input layout, InputFlow saves your current
    layout and activates the Korean IME.
  - If the Korean IME is already active, InputFlow sends the built‑in
    Hangul/English toggle key (`{Hangul}`) to switch between Hangul and
    English modes within the IME.
- **Return to previous layout / Activate Korean** – press
  **Right Ctrl + Right Alt**.
  - If you are currently using the Korean IME, InputFlow restores the
    previously saved layout.  If no layout has been saved yet (for
    example the first time you run the script), it falls back to the US
    International layout (`0x00020409`).
  - If you are using another layout, InputFlow saves your current layout
    and activates the Korean IME.

## How it Works

InputFlow is implemented as a simple AutoHotkey script.  It queries the
current thread’s input locale identifier (HKL) using the Windows API and
compares the low word of the handle against `0x0412`, which is the locale
identifier for Korean.  When switching layouts, the script calls
`LoadKeyboardLayout` to load the requested layout and `SystemParametersInfo`
with `SPI_SETDEFAULTINPUTLANG` to apply it globally.  It then posts a
`WM_INPUTLANGCHANGEREQUEST` (`0x50`) message to every top‑level window to
ensure that all open applications adopt the new layout immediately.  See
the comments in `InputFlow.ahk` for the implementation details.

Because InputFlow operates at the input locale level, it does not alter
your language bar or the configured hotkeys in Windows.  You can continue
using `Alt+Shift`, `Ctrl+Shift` or any other system hotkeys for language
switching as normal.  InputFlow merely offers an additional, more
streamlined workflow for users who frequently move between two specific
layouts.

## License

This project is released under the MIT License.  See the [LICENSE](LICENSE)
file for details.
