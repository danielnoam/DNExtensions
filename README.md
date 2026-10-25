# DNExtensions

A suite of Unity editor tools, inspector utilities, components and gameplay systems, distributed as four UPM packages.

Requires **Unity 2022.3** or newer.

---

## Installation

Each package installs from this repository via a git URL with a `?path=` query. In Unity: **Window → Package Manager → + → Add package from git URL**.

### Install order matters

UPM does **not** resolve git dependencies transitively. A package that depends on another will only resolve if that other package is *already in the project*, so add them top to bottom:

| # | Package | Depends on | Prerequisites |
|---|---------|-----------|---------------|
| — | `helpfuleditor` | nothing | none — install any time |
| — | `buildtool` | nothing | Unity 6 (Build Profiles) — install any time |
| 1 | `utilities` | nothing | none |
| 2 | `components` | utilities | PrimeTween |
| 3 | `systems` | utilities, components | PrimeTween |

Adding `systems` to an empty project will fail. Add `utilities` first.

### URLs

```
https://github.com/danielnoam/DNExtensions.git?path=/Packages/com.danielnoam.helpfuleditor
```
```
https://github.com/danielnoam/DNExtensions.git?path=/Packages/com.danielnoam.buildtool
```
```
https://github.com/danielnoam/DNExtensions.git?path=/Packages/com.danielnoam.utilities
```
```
https://github.com/danielnoam/DNExtensions.git?path=/Packages/com.danielnoam.components
```
```
https://github.com/danielnoam/DNExtensions.git?path=/Packages/com.danielnoam.systems
```

### PrimeTween

`components` and `systems` require [PrimeTween](https://assetstore.unity.com/packages/tools/animation/primetween-high-performance-animations-and-sequences-252960). It is a paid asset and cannot be resolved from a registry, so it is **not declared as a dependency** — install it into the project yourself before adding either package. Without it, those packages will not compile.

### Unity dependencies

Installed automatically:

- **utilities** — Mathematics, uGUI
- **components** — Input System, uGUI
- **systems** — Cinemachine, Input System, URP, Timeline, uGUI, VFX Graph

### Optional integrations

Two assemblies inside `utilities` are gated on packages you may not have, and are simply not built if they are absent — no errors, nothing to configure:

| Assembly | Appears when installed |
|---|---|
| `DNExtensions.Utilities.CinemachineExtensions` | `com.unity.cinemachine` |
| `DNExtensions.Utilities.SplineToTerrain` | `com.unity.splines` |

Install either package later and the corresponding assembly compiles itself into existence.

### MCP tools for AI agents

Each package also ships an `MCP/Editor` assembly gated on `com.unity.pipeline`, the package the Unity CLI uses to drive a running Editor. With it installed, the commands below show up in `unity list` and, through `unity mcp`, as MCP tools for Claude Code or any other MCP client. Without it nothing is built.

```bash
unity pipeline install            # once per project
unity mcp configure claude-code   # register the Unity MCP server with the client
```

| Command | Package | What it does |
|---|---|---|
| `dn_capture_game_view` | helpfuleditor | Saves a PNG of the Game View (optionally re-rendered at a size, or without UI) and returns its path |
| `dn_start_recording` / `dn_stop_recording` / `dn_recording_status` | helpfuleditor | Records the Game View to an MP4 through the Game View recorder: size, fps, quality and mode default to its settings, `play_mode` starts the game first, `duration` stops the take by itself, and stopping returns the file's path |
| `dn_store_refresh` / `dn_store_categories` / `dn_store_list` | helpfuleditor | Fetches the signed-in user's whole Asset Store library (owned, downloaded or not) into a local cache, and lists it by category, search text or downloaded state |
| `dn_store_download` / `dn_store_download_status` / `dn_store_import` | helpfuleditor | Downloads owned packages (by id, a whole category, a search, or everything missing) into the Asset Store cache like the Package Manager's Download button, tracks progress, and imports a downloaded package into the project |
| `dn_list_buttons` / `dn_invoke_button` | utilities | Lists and presses `[Button]` methods on a GameObject's components or an asset |
| `dn_autoget_populate` | utilities | Populates `[AutoGet]` fields in a scene or under a GameObject and reports the ones still empty |
| `dn_recent_assets` | utilities | The user's Quick Navigation history and favorites |
| `dn_list_notes` / `dn_set_note` | components | Reads the `Note` components and `NoteField`s left in the loaded scenes, and writes notes |
| `dn_list_audio_ids` | systems | The IDs registered for `AudioLibrary` and `AudioTrack` |
| `dn_list_pools` | systems | Configured object pools, or live active/inactive counts in play mode |
| `dn_list_scriptables` / `dn_set_so_value` / `dn_invoke_so_event` | systems | Reads and tunes `SOValue` assets, raises `SOEvent`s in play mode |

Screenshots and recordings go to `Temp/DNExtensionsMCP` unless a `path` or `folder` is given, so they are never imported or committed. For example, four seconds of gameplay that starts and stops play mode by itself:

```bash
unity command dn_start_recording --play_mode true --duration 4 --exit_play_mode true
unity command dn_recording_status   # repeat until it reports the saved take
```

The Asset Store commands use the account signed in to the editor and reach the Package Manager's internal Asset Store services by reflection, as Unity has no public API for a user's purchases. If a Unity update renames those internals, the commands say so instead of failing halfway. A typical session:

```bash
unity command dn_store_refresh                                   # once, then whenever new assets are bought
unity command dn_store_list --category audio --downloaded no     # owned audio that was never downloaded
unity command dn_store_download --category audio/sound-fx
unity command dn_store_download_status
unity command dn_store_import --id 115488
```

### Download size

UPM clones the whole repository for each package and then uses only the subfolder. This repo is a full Unity project, so expect a large download regardless of which package you are installing.

---

## Packages

### `com.danielnoam.helpfuleditor` — Helpful Editor

Editor-only quality-of-life for Unity's own windows. No dependencies, nothing to reference from your code — install it and it works.

- **Hierarchy** — zebra striping, tree guides, a component icon strip with quick-edit, child-count badges, and keybinds for toggling active, expanding, and isolating.
- **Inspector** — an object header bar with per-component buttons, component isolation, drag-to-reorder, a multi-component clipboard, better Transform and RectTransform rows, save-in-play-mode, and per-component header buttons (camera alignment, colour reset, TextMeshPro material duplication, RectTransform row copy/paste and reset).
- **Project** — hover highlighting, zebra striping, tree guides, folder content type-icons, file extensions, a create-folder button, folder history, drag-conflict resolution, symlinked-folder badges, and middle-click to open a folder in a second Project window.
- **Game View** — rulers and draggable guides, held against the render target so they survive resizing and zooming.

Everything is toggleable at **Project Settings → DNExtensions → Helpful Editor**, stored as JSON under `ProjectSettings/HelpfulEditor/` so it travels with source control.

---

### `com.danielnoam.buildtool` — Build Tool

A release window at **Tools → DNExtensions → Build**. Editor-only, no dependencies.

- **Build** tab: the version (with the Android version code derived from it), Android APK and Windows targets built from Build Profiles one after another, then uploads to a folder, GitHub Releases (via the `gh` CLI, with release notes from commit subjects), itch.io (via `butler`, one channel per target) and Steam (via `steamcmd` and a generated SteamPipe script).
- **Settings** tab: the project's setup, saved in `Assets/Settings/Build/BuildConfig.asset` (file name, default build folder, Android target API pin, GitHub repository, itch.io project and channels, Steam App and depot IDs), and this computer's own, kept in EditorPrefs per project (build and copy folders, release keystore and its password encrypted for the Windows user, butler and steamcmd paths, itch.io API key, Steam user).
- **Project checks**: mark a static method `[BuildCheck("Name")]` returning `IEnumerable<string>`; each message it yields stops the build.
- **Headless and agents**: `-executeMethod DNExtensions.BuildTool.BuildRunner.BuildFromCommandLine`, or `BuildRequestWatcher.Queue` / `ReadStatus` to build in the open editor.

---

### `com.danielnoam.utilities` — Utilities

The foundation the rest of the suite builds on, and the most reusable part on its own. Almost entirely inspector and workflow tooling.

- **Attributes** — `AutoGet` reference population, inspector `Button`s, conditional show/hide/enable (per field or as `[EndIf]` blocks), `Foldout` groups, info boxes, inline ScriptableObject editing, asset previews, read-only fields, scene pickers, separators, asset selectors, weighted chance lists, unique ScriptableObject enforcement.
- **Custom fields** — ranged values, optional values, position fields with scene picking, and dropdown-backed fields for scenes, tags, sorting layers, animator states and animator parameters.
- **SerializableSelector** — a dropdown type picker for `[SerializeReference]` fields, so polymorphic data is editable in the inspector.
- **SerializedInterface** — interface references that survive serialization.
- **Extensions** — extension methods across Vector, Transform, GameObject, Rigidbody, Material, Color, Camera, string and collection types.
- **Editor tools** — audio preview, a better UnityEvent drawer, box collider handles, quick navigation, ScriptableObject editing, animation state events, local git exclude, multi-TextMeshPro editing, and toolbar additions including play-from-camera.

---

### `com.danielnoam.components` — Components

Runtime MonoBehaviours built on Utilities. Grab-and-go pieces rather than a framework.

Billboard, terrain alignment, collision relaying, a debug overlay, FPS counter, flying agent, free-form camera controller, material property tweening, transform effectors (position, rotation and scale effects composed via SerializeReference), a simple animator, tube renderer, inspector notes, and UI pieces — carousel view, fill bar, radial layout group.

---

### `com.danielnoam.systems` — Systems

The largest and most opinionated package: whole subsystems rather than individual components, plus the shaders and effects that go with them. This is the one that pulls in URP, Timeline and VFX Graph.

- **Audio** — audio library, tracks, and mixer track control.
- **Player** — a first person controller with movement, camera, interaction and pickup.
- **Gameplay** — object pooling, scheduling, grid, radar, radial menu, damage numbers, springs for smooth motion, SDF shapes.
- **UI and feedback** — menu system with animated screen transitions, controller rumble, mobile haptics.
- **Data** — ScriptableObject-backed events, audio events and typed value assets.
- **Visuals** — a fullscreen VFX and transition stack, plus the shader and effect assets it drives.

Ships an importable sample (**DNExtensions Example**) demonstrating the attributes, custom fields and selectors across the suite.
