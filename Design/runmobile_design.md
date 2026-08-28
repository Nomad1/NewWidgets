# `assets/runmobile_design.js` and `assets/runmobile_design.css` — the contract

**Home.** This file, `runmobile_design.js` and `runmobile_design.css` live here, in
`NewWidgets/Design/`, and only here. This is the single source of truth for all three; every
consumer (`NewWidgets.RunMobileSample/assets/`, `NewWidgets.Test/Conformance/`,
`SpaceAdventure.Client/Resources/Shared/`) reaches them through a symlink back to this
directory, never through a hand-maintained copy. Edit the files here; never edit a symlink
target. `Design/check_sync.sh` fails if a copy ever drifts back into existing as a real file.

This file lists everything `runmobile_design.js` and `runmobile_design.css` are allowed to
do. It is the whole list.

`ui.css` and `login.xhtml` are read by this engine and by a browser. Where a browser needs
something the engine's own stylesheet cannot state, the script supplies it at run time and
the stylesheet supplies it at load time. Between them they supply nothing else.

**Which of the two.** A STATIC reset -- true the moment the tag exists, nothing to compute --
belongs in `runmobile_design.css`: it sits in the normal cascade, so an author rule of equal
specificity overrides it without `!important`, and it applies before first paint. Anything
that needs a DOM API, a computed style read, or state that changes later (hover, a sprite's
own size from its atlas) can only be `runmobile_design.js`.

**Rule for anyone editing either file.** If a behaviour is not a row in this table, it must
be deleted. If you believe a new behaviour is needed, add the row first and get it approved.
Do not add code and document it afterwards.

## The table

| # | Feature | What the script does | Engine source |
|---|---|---|---|
| 1 | Root font size | `runmobile_design.css`'s `html` rule sets `font-size: 30px` (row 10) -- moved from the script's `applyPageDefaults()` once measured static: nothing here is computed at run time | One engine font unit is 30 browser pixels |
| 2 | Page background | `runmobile_design.css`'s `html`/`body` rule sets `background-color: #000000` and `margin: 0` (row 10) -- moved from the script's `applyPageDefaults()`, kept a section of its own rather than folded into `dialog`'s reset, since the file otherwise only neutralises adopted-tag UA chrome | None. This engine has no page |
| 3 | Tint | Reads `--background-color` and `--background-opacity` for each element. Builds one SVG `feColorMatrix` for each distinct colour. Applies it to the frame only, never to the text | `--background-color`, `--background-opacity` |
| 4 | State | Re-reads the computed style on hover, focus, checked and disabled, so the tint and the sprite both follow | The engine's own state styles |
| 5 | Checkbox tick | Wraps the `<input>` in a `<checkbox-frame class="checkbox">` and builds a `<checkbox-tick id="checkbox_image">` inside it, so CSS resolves `.checkbox #checkbox_image`'s sprite, its colour and its `:hover`. Hyphenated custom tags, not `<div>`: see row 11's note for why (`.checkbox`'s own class match is unaffected; the bare `panel, div { }` rule's is what this avoids). The tick is drawn by row 3's construction, a tinted clone, not by a mask | `checkbox #checkbox_image`, `--image-color` |
| 7 | Label colour | Mirrors a checkbox's colour onto the `label[for]` that names it | `WidgetCheckBox.LinkedLabel` |
| 9 | Browser quirks (script) | Applies the one workaround a browser needs that cannot be static CSS: appends the `border-box` reference box to any `clip-path: inset(...)`, without which Chrome silently drops the whole `border-image`. Forcing `<dialog>` to render without `open` used to live here too, as an inline attribute-set; it is now row 10's job, a stylesheet declaration, since a normal author-origin rule overrides a normal UA rule regardless of selector specificity -- no attribute to force, so nothing dialog-related is left in the script | none, these are browser defects |
| 8 | Sprite size | Loads every SVG atlas named by the stylesheet through a hidden `<object>`, receives each sprite's size from the atlas's own reporter script by `postMessage`, and sets `background-size` where a browser would otherwise guess: a repeating background, and an image fitted inside a larger box such as the checkbox tick | the atlas's `<view>` viewBox |
| 10 | Browser quirks (stylesheet) | `runmobile_design.css` resets the STATIC UA chrome a browser adds to a tag this preview adopts -- `dialog`, `button` and, as of the `input[type="text"], input[type="password"]` block, `input` today -- one selector block per tag so `textarea`/`hr`/`progress`/`select` can still follow. Measured in Chrome from a `file://` address by diffing a bare tag against a bare `<div>`, cross-checked against that widget's own unstyled engine defaults; the classification is in the file's own header. One property in the `button` and `input` blocks, `outline: none`, is measured a different way: it is not a rest-state diff against a bare `<div>` (a bare tag's outline is `none` at rest too), it is Chrome's own `:focus` ring, reset only on the selectors `defaults.css` already draws an engine `:focus` style for (`button`; `textedit, textfield, textarea, input[type="text"], input[type="password"]`), so the preview shows one focus indicator, the engine's, not two stacked. Linked before every author stylesheet, so equal-specificity author rules win by cascade order, no `!important`. The engine skips the whole file: `@runmobile_ignore {}` is its first rule, and `CSSParser.ParseCSS` stops reading there, the same asymmetry a browser has for an unknown at-rule in reverse (a browser ignores just that rule and reads on). Its `dialog` block also carries `display: block`, which renders a `<dialog>` whether or not it carries `open` -- moved here from row 9's script once measured that a normal author-origin declaration beats the UA's `dialog:not([open]) { display: none }` by cascade origin, not by selector specificity, so no `!important` and no attribute-forcing are needed. Its separate `html`/`body` block carries rows 1 and 2 -- root font size and page background -- moved here from the script's `applyPageDefaults()` once measured both are static; not a UA-chrome reset (`html`/`body` adopt no widget tag, so there is no bare tag to diff), so it is kept its own section rather than folded into `dialog`'s | none, these are browser defects, except `margin` -- `Widget.cs`'s `ResolveBox`/`StyleAxis` genuinely reads it (CSS 2.1 10.3.7), which is why each game's own stylesheet keeps its own `margin` line rather than relying on this file's -- except `dialog`'s `display`, STANDARD, OVERRIDDEN ON PURPOSE: this engine has no closed-window state, so a `WidgetWindow` that exists renders regardless of `[open]` (COMPATIBILITY.md) -- and except `html`'s `font-size`, which is `WidgetManager`'s own font-unit-to-pixel scale, not a browser default at all |
| 11 | Button structure | Builds a `<button-image class="button_image">` and a `<button-label class="button_label">` inside every `<button>`, so `button > .button_label` and class rules like `.button_steam > .button_image` resolve a real child the way the engine's own two widgets do. Supersedes this row's earlier form (a `<button>`-side padding copy, since removed): once the button has its real children there is no bare button text left for that copy to approximate the position of. Hyphenated custom tags, not `<span>`: `label, text, span, h1..h6 { }` and, in the sample, a bare `img { }` are real rules in all three stylesheets, and an injected `<span>` would be styled by them unintentionally; the same reasoning retagged row 5's checkbox wrapper and tick away from `<div>`, which a bare `panel, div { }` rule reaches the same way (SpaceAdventure's own login.xhtml has no `.checkbox` override for the properties that rule sets, so an actual `<div>` there drew a window-panel frame around the checkbox). A hyphenated name is a valid HTML5 custom element -- no user-agent style, matched by no bare element rule -- while `color`/`font-size`/`font-family` still inherit from the button normally (`WidgetParameterInheritance.Inherit`, `WidgetParameterIndex.cs:203,205`), so `all: initial`/`unset` is never used: that would fix the element-rule leak by breaking inheritance the engine's own label depends on. Each child is positioned to match `WidgetButton.UpdateLayout`, which reads `Padding` off the child and sizes it as `Size - imagePadding.Size`: `padding` and `inset` share the same 1-to-4-value syntax, so the child's own computed padding, read once it is a real child and the real selectors have resolved it, becomes its `position: absolute; inset: ...` directly, and the padding itself is zeroed so it is not also kept as literal box padding. These elements exist only at run time: the engine's markup loader rejects children under `<button>` (`WidgetManager.Markup.cs:597`, "Element `<button>` cannot have children, N node(s) inside it skipped"), so they are never written into `login.xhtml` or any other document -- only injected here | `WidgetButton`'s `m_label` (class `button_label`) and `m_image` (class `button_image`), and `UpdateLayout`'s per-child `Padding` read |

**The rule behind rows 9 and 10.** A declaration that exists only to satisfy a browser does not
belong in `ui.css`. A designer does not know what it is for, the engine has no use for it, and
the next person to read the file removes it as noise or resents it as one more rule for a simple
thing. Every such workaround goes to one of these two files instead, never the stylesheet a
designer edits: row 10 (`runmobile_design.css`) if it is static, row 9 (`runmobile_design.js`) if
it needs a DOM API or a value that changes later. When you find one, add it to the row for the
file it belongs in, rather than to `ui.css`.

## Explicitly not the script's work

- **Addressing a sprite.** `url("ui.svg#name")` and `src="ui.svg#name"` already resolve
  in both readers. `ui.svg` carries one `<view id="...">` for each sprite. Measured in
  Chrome from a `file://` address on 2026-08-26, for `background-image`,
  `border-image-source` and `<img src>`. The script must never rewrite a sprite
  reference, and must hold no table of sprite rectangles.
  The one exception, measured the same day: `mask-image` does NOT resolve the
  fragment, and an element masked with one renders nothing at all. That is why row 5
  draws the tick as a tinted clone rather than as a mask.
- **Sizing a tiled sprite by hand.** No pixel size may be written in `ui.css` or in this
  script. Row 8 obtains it from the atlas at run time. `background-size: auto` is what the
  ENGINE reads as "the sprite's own size"; a browser cannot, because a `<view>` fragment
  carries a ratio and no size.

## How row 8 gets the size

A page cannot read an SVG's DOM from a `file://` address: `fetch`, `XMLHttpRequest` and
`<object>.contentDocument` are all blocked by the opaque origin a local file receives. But a
script INSIDE the SVG reads its own DOM freely, and `postMessage` crosses origins by design.
So `SpritePacker` writes a one-line reporter into every atlas, and the page listens.

Two details are load-bearing. The reporter must be the LAST child, immediately before
`</svg>`, because an SVG classic script runs at parse time and would otherwise see a
two-element document. And it must collect with `getElementsByTagName("*")` filtered on
`localName`, because `querySelectorAll("view")` matches nothing in an XML document.

**This is a workaround with a known end.** Inkscape and Illustrator are likely to strip the
script when they save the file. When that happens the sizes stop arriving and every row-8
consumer must fall back without breaking the rest of the preview.
- **Anything that CSS already resolves.** If a shared rule can state it, it belongs in
  `ui.css`, not here.

## Style

Small and readable. No framework, no build step, no dependencies. Plain ES2020. A
helper with one caller is inlined. A branch for an input that `ui.css` and
`login.xhtml` cannot produce is deleted. A comment states the reason, never the code.
