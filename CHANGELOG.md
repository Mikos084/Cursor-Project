# Changelog

## v0.8.3

- Preserves the no-flash cursor handoff from v0.8.2.
- Prevents multiple app instances from running at once.
- Adds last-chance `ClipCursor(NULL)` cleanup on exit and unhandled errors.
- Handles Windows display-topology changes and releases stale cursor confinement.
- Prevents monitor-role selection changes while an active cursor session is running.
- Makes settings replacement safer and keeps a last-known-good `.bak`.
- Removes recursive installer cleanup that could delete a user-selected install directory.
- Adds GitHub Actions CI on Windows.
- Adds automated Portable ZIP + Setup EXE + SHA256 GitHub Releases.

## v0.8.2

- Removes the intentional blank frame during cursor handoff.
- Paints the parked cursor before moving the real cursor away.
- Uses atomic icon-handle replacement in the fake cursor overlay.

## v0.8.1

- Added reliable user-editable global hotkeys.
- Added responsive UI, DPI scaling, scrolling and rounded controls.
- Preserved the two independent monitor cursor positions.

## v0.8

- Initial configurable-hotkey implementation.
