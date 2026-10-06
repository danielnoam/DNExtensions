# Changelog

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