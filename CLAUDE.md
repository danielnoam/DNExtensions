# DNExtensions: notes for Claude

- **Menu items** go under the DNExtensions menu in the editor's top bar, never under Tools: build every path from
  `DNExtensionsMenu.Root`, e.g. `[MenuItem(DNExtensionsMenu.Root + "My Tool")]`. Utilities has the public
  `DNExtensions.Utilities.DNExtensionsMenu`; a package that depends on nothing (Helpful Editor, Build Tool) keeps its
  own internal copy in its root namespace. A new package of that kind adds one too.
- Project Settings pages stay at `Project/DNExtensions/<Name>`, and GameObject create items at `GameObject/DNExtensions/<Name>`.
- **Versions** are shared: every package carries the same `version`, and the `com.danielnoam.*` dependency versions match
  it. To release, set them all (and the same entries in `Packages/packages-lock.json`) to the new version, move
  `[Unreleased]` in CHANGELOG.md into a dated section, update the `#vX.Y.Z` in the README URLs, then tag the merged
  commit `vX.Y.Z` and push the tag. Never move or reuse a tag. The Release Tag Check workflow fails a tag that
  disagrees with the package versions.
