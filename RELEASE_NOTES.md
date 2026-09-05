# Multiple Pointers v0.8.3

Reliability release based on v0.8.2.

## Highlights

- keeps the no-flash cursor handoff introduced in v0.8.2,
- prevents multiple copies of Multiple Pointers from running simultaneously,
- releases `ClipCursor` on normal exit and on unhandled application errors,
- automatically reacts to Windows display-topology changes,
- resets cursor confinement safely when a selected monitor disappears,
- disables monitor-selection controls while an active presentation session owns cursor state,
- makes `settings.json` replacement safer and keeps a last-known-good backup,
- removes an installer uninstall rule that could recursively delete a custom installation directory,
- adds GitHub Actions CI and automated Windows release packaging.

## Downloads

- **Portable ZIP** — extract and run `MultiplePointers.exe`.
- **Setup EXE** — normal per-user Windows installer.

Both builds are self-contained for Windows x64; end users do not need to install the .NET SDK.
