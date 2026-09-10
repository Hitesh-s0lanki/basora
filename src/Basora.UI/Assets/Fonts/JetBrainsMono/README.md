# JetBrains Mono

The monospace face for SQL, grid cell values, identifiers, type names and numbers, per
[`docs/07-design-system.md`](../../../../../docs/07-design-system.md) section 5.

| | |
|---|---|
| Version | 2.304 |
| Source | <https://github.com/JetBrains/JetBrainsMono/releases/tag/v2.304> |
| Licence | SIL Open Font License 1.1, in `OFL.txt` |
| Authors | `AUTHORS.txt` |

## Why these four files and not the other fifty

The release ships eight weights, each with an italic, plus a no-ligature variant and web
formats. Basora renders code and data at one weight, and uses bold only for SQL keyword
highlighting, so it carries regular, italic, bold and bold italic and nothing else. That
is about 1.1 MB rather than about 10 MB in a repository and in every installer.

## Why it is bundled rather than assumed

Data alignment is the whole reason the design system specifies a monospace face for cell
values. A proportional fallback puts a column of numbers out of line, which is the one
thing a person reading a result set cannot work around. Falling back to whatever the
machine happens to have would make that a property of the machine.

## Updating

Replace the four `.ttf` files and both text files from a single upstream release, and
update the version above. `ThemeFontTests` asserts the family still resolves to a
monospaced face, so a mismatched or partial update fails the build rather than silently
falling back.
