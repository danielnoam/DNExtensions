# Changelog

## [Unreleased]

### Added
- MCP tools for AI agents: `MCP/Editor` assemblies in every package, built only when `com.unity.pipeline` is installed, exposing Game View capture, inspector buttons, AutoGet population, Quick Navigation history, scene notes, audio IDs, object pools and ScriptableObject values/events as `unity` CLI commands and MCP tools.

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