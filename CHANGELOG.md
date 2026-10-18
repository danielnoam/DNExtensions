# Changelog

## [Unreleased]

### Added
- MCP tools for AI agents: `MCP/Editor` assemblies in every package, built only when `com.unity.pipeline` is installed, exposing Game View capture, inspector buttons, AutoGet population, Quick Navigation history, scene notes, audio IDs, object pools and ScriptableObject values/events as `unity` CLI commands and MCP tools.
- MCP Game View recording: `dn_start_recording`, `dn_stop_recording` and `dn_recording_status` drive the Helpful Editor recorder, in Real Time or Unity Recorder mode, with optional size, fps, quality, a duration that stops the take by itself, and starting (and leaving) play mode around it.
- MCP Asset Store library: `dn_store_refresh`, `dn_store_categories`, `dn_store_list`, `dn_store_download`, `dn_store_download_status` and `dn_store_import` browse everything the signed-in user owns (filed under its deepest category, cached in `Library/DNExtensionsMCP`), download single packages, whole categories, search matches or everything missing through the Package Manager's download manager, and import downloaded packages.
- Game View recording takes per-take options (`GameViewRecordingOptions`) that fall back to the Game View settings, and reports where the last take was saved.

### Fixed
- Saved recordings outside `Assets` (the default `Recordings` folder among them) no longer try to import and ping a file the AssetDatabase cannot see.

## [1.1.0] - 2026-10-06

### Added
- `[Foldout("Name")]` / `[EndFoldout]` collapsible inspector groups, nestable with `"Parent/Child"`.
- `[EndIf]`: `ShowIf`, `HideIf`, `EnableIf` and `DisableIf` now act as blocks over the following fields when an `[EndIf]` closes them. Without one they still affect only their own field.

### Fixed
- Enum conditions compared the enum index instead of its value, so enums with explicit values never matched.
- Conditions can now point at plain non-serialized fields.
- `[ReadOnly]` re-enabled the GUI after drawing instead of restoring the previous state.
- Top-level lists with a conditional attribute are hidden as a whole instead of only their elements.

## [1.0.0] - 2026-03-04

### Added
- Initial public release