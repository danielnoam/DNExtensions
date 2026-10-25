# DNExtensions: notes for Claude

- **Menu items** go under the DNExtensions menu in the editor's top bar, never under Tools: build every path from
  `DNExtensionsMenu.Root`, e.g. `[MenuItem(DNExtensionsMenu.Root + "My Tool")]`. Utilities has the public
  `DNExtensions.Utilities.DNExtensionsMenu`; a package that depends on nothing (Helpful Editor, Build Tool) keeps its
  own internal copy in its root namespace. A new package of that kind adds one too.
- Project Settings pages stay at `Project/DNExtensions/<Name>`, and GameObject create items at `GameObject/DNExtensions/<Name>`.
