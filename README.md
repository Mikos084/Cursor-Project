# Multiple Pointers

Multiple Pointers is a Windows tray utility for presentations and screen sharing. It can leave a stationary visual arrow on the presentation display while the real Windows cursor is moved to and confined to a private display. Each logical display remembers its own last cursor position.

## Current release: v0.8.3

### Core behavior

- explicit **presentation** and **private** monitor roles,
- independent saved cursor X/Y for both monitors,
- instant switching between the two saved positions,
- stationary normal-arrow overlay on the presentation monitor,
- configurable global hotkeys,
- monitor identification using DISPLAY number, model name, resolution and desktop position,
- scrollable DPI-aware UI,
- tray-only operation.

### Reliability in v0.8.3

- single-instance guard prevents duplicate tray apps and hotkey conflicts,
- cursor confinement is released on normal shutdown and unhandled application errors,
- Windows display-layout changes are detected and stale confinement is reset,
- monitor selectors are locked while a presentation session is active,
- `settings.json` is replaced more safely and a backup is retained,
- the installer no longer recursively deletes the entire chosen installation directory,
- the no-flash cursor handoff from v0.8.2 remains in place.

## Default hotkeys

These are only defaults; the user can change or disable them in **Ustaw skróty**.

- `Ctrl + Shift + F7` — presentation ↔ private monitor
- `Ctrl + Shift + F8` — park / remove visual arrow
- `Ctrl + Shift + F9` — start / stop presentation mode
- `Ctrl + Shift + F10` — reset / unlock cursor
- `Ctrl + Shift + F11` — open dashboard

Additional actions can also receive user-defined hotkeys: refresh monitors, swap monitor roles, open instructions and open hotkey settings.

## Build locally

Run `BUILD_RELEASE.bat` on Windows. The build machine needs .NET 8 SDK; Inno Setup is needed for Setup EXE. End users receive a self-contained build and do not need the SDK.

## GitHub Actions

- `.github/workflows/ci.yml` builds the project on Windows for pushes and pull requests.
- `.github/workflows/release.yml` is triggered when `RELEASE_VERSION` changes on `main` (or manually). It builds a self-contained Windows x64 package, creates Portable ZIP and Setup EXE, computes SHA256 hashes, uploads workflow artifacts and publishes a GitHub Release.

The release workflow verifies that `RELEASE_VERSION` exactly matches `<Version>` in `MultiplePointers.csproj` before publishing.

### Publishing a release

1. Put the contents of this project, including `.github`, at the repository root. The default release branch is `main`.
2. Set `RELEASE_VERSION` and the csproj `<Version>` to the same stable `MAJOR.MINOR.PATCH` version (each component 0–65535). Update `<FileVersion>` and `<AssemblyVersion>` and write `RELEASE_NOTES.md` for that version.
3. Push these changes together to `main`. A change to `RELEASE_VERSION` starts the release workflow automatically. For the initial release, you can also select **Actions → Build and publish release → Run workflow → main**.
4. After compilation and packaging succeed, the workflow uploads Portable ZIP, Setup EXE and SHA256SUMS.txt to a draft release, then publishes it. It creates tag `v<version>` at the exact built commit. Inno Setup receives the version from the workflow, so no installer version edit is needed for CI.

The built-in `GITHUB_TOKEN` uses `contents: write`; no personal access token is needed. Repository/organization policy must allow Actions and release creation. The Windows 2025 runner includes Inno Setup; if the compiler is unavailable, the workflow fails instead of publishing an incomplete release.

Release runs are serialized. An existing release (including a draft) is never overwritten. If upload or publication fails, inspect the draft: finish it manually if all assets are present, or delete the incomplete draft before rerunning the same commit. Use a new version for subsequent releases. CI builds on ordinary pushes/PRs do not publish releases.

The installer is unsigned unless you separately configure code signing.

## Distribution files

For v0.8.3 the release workflow produces:

- `MultiplePointers_Portable_v0.8.3.zip`
- `MultiplePointers_Setup_v0.8.3.exe`
- `SHA256SUMS.txt`

## Safety / privacy

Multiple Pointers does not install a driver, inject code into other apps, capture the screen, record keystrokes, or require administrator access for the default installer.
