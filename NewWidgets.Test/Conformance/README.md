# Conformance copies

`amalthea/` and `siegewars/` are byte copies of the two games' shipped stylesheets,
taken from `Resources/Shared/ui/` in each game's own checkout and refreshed on
2026-08-25. No path outside this repository is named anywhere in the suite, so the
tests run on a machine that has neither game.

`CorpusTests` (tests 40-43) reads these copies, not the games. They are the input to
the golden masters in `Baselines/`. Refresh them from a game checkout when its UI
changes -- and if a baseline then moves, that movement is the finding: report it,
do not regenerate the baseline to absorb it.

The originals stay untouched.

These copies get converted to standard CSS. The purpose is a test of equality:

    original stylesheet  -> computed styles A
    converted stylesheet -> computed styles B
    assert A == B

If the two match, the conversion says the same thing in standard words. If they
differ, either the conversion is wrong or an alias is wrong. Either way the test
names which selector moved.

## What converts

| Custom, today | Count | Standard CSS |
|---|---|---|
| `background-repeat: nineimage` | 49 | `border-image-source` plus `border-image-slice: 33.33% fill` |
| `background-repeat: imagefit` | 40 | `background-size: contain` plus `background-position: center` |
| `background-repeat: threeimage` | 19 | `border-image-slice: 0 33.33%` |
| `@sprite.x { --sprite-tile-x: 3; --sprite-tile-y: 3 }` | 62 | deleted, the slice moves into the rule that uses the sprite |
| `--button-image-padding` | 19 | `padding` on the button's image child |
| `--button-text-padding` | 13 | `padding` on the button's label child |
| `background-repeat: imagetiled` | 1 | `background-repeat: repeat` |
| `background-repeat: imagestretch` | 1 | `background-size: 100% 100%` |
| `background-repeat: image` | 1 | `background-repeat: no-repeat` plus `background-position: center` |

`background-repeat: no-repeat` appears 15 times and is already correct.

The two `border-image-slice` entries above assume the source is a standalone
image, where a 33.33% cut is the sprite's own third. When the source is an atlas
the same cut has to be written as four insets measured from the *atlas* edges
instead; see "One element per patch" at the end of this file for the arithmetic.
The count of declarations to convert does not change.

## What stays custom

These have no standard equivalent. A `--` prefix is correct CSS for them, and a
browser ignores them without an error:

`--background-depth`, `--background-rotation`, `--clip-margin`, `--richtext`,
`--cursor-color`, `--button-layout`, and the five font metrics.

## Two notes on the originals

`--button-image-padding` and `--button-text-padding` name properties that
NewWidgets removed. The library discards all 32 declarations today, so Amalthea's
buttons already lay out differently from what its stylesheet asks for. The
conversion is the moment to correct that.

`--abackground-image` appears 3 times. The leading `a` looks like a manual
disable, not a typo. The conversion keeps it disabled.

## Every non-standard keyword, and whether CSS already has it

A second target: remove non-standard keywords wherever CSS has a direct equal.
This table covers the whole property table, not only the background group.

### Direct equal exists. These should go.

| Custom | Standard CSS | Note |
|---|---|---|
| `--cursor-color` | `caret-color` | Same meaning, same value type |
| `--font-spacing` | `letter-spacing` | `Font.cs` adds it between glyphs, which is what `letter-spacing` does |
| `--clip-margin` | `clip-path: inset(t r b l)` | Same four-sided inset |
| `@font.x { --font-resource: url() }` | `@font-face { font-family: x; src: url() }` | `@font` is not a CSS at-rule, so a browser skips the whole block |
| `background-color-opacity` | `opacity` on the element that carries the background | **Corrected 2026-08-24.** It is an opacity on the drawn *image*, not an alpha on a colour: `WidgetBackground.Update` (lines 384-392) folds `BackgroundAlpha` into `ImageObject.Sprite.Alpha`, which multiplies every pixel the sprite draws. `background-color: rgba(...)` paints a colour *behind* the image and leaves the image at full strength, so it is the wrong mapping. Exact for the generated patch elements because they have no children; on an element with children CSS `opacity` would fade those too, and the engine does not |
| `--sprite-tile-x` and `-y` | `border-image-slice` | Covered above |
| `--button-image-padding` | `padding` on the image child | The library already dropped this property |
| `--button-text-padding` | `padding` on the label child | The library already dropped this property |
| `x`, `y`, `z` | `left`, `top`, `z-index` | Already aliased in the parser |
| `--clip` | `overflow` | Already aliased in the parser |

### Close, but not exact. Each needs a decision.

| Custom | Nearest CSS | Why it is not exact |
|---|---|---|
| `--background-padding` | `background-origin: content-box` with `padding`, or `border-image-outset` | The right answer differs between a plain background and a nine-patch |
| `--button-animate-time` | `transition-duration` | CSS transitions are a different model from the tween system |
| `--button-animate-scale` | `transform: scale()` on `:hover` | Same |
| `--button-animate-pivot` | `transform-origin` | Same |
| `--font-baseline`, `--font-leading`, `--font-shift` | none that fit | These are atlas metrics. `--font-leading` shifts every glyph, so `text-indent` is wrong, because that applies to the first line only |

### No equal. These stay custom, and that is correct CSS.

`--background-depth`, `--background-rotation`, `--richtext`, `--button-layout`,
`--cursor_char`, `--mask_char`.

Two of these use an underscore: `--cursor_char` and `--mask_char`. Every other
custom name uses a hyphen. If they stay, they should be renamed for consistency.

### Already dead

`--image-style`, `--image-rotation`, `--image-position`, `--image-padding`,
`--image-color`, `--image-opacity` and `--text-padding` are commented out in
`WidgetParameterIndex.cs`. They are not live properties. SiegeWars still uses
four of them and loses those declarations silently.

## The reverse test: login.xhtml and login.css

`login.xhtml` and `login.css` are the sample login form from
`Sample/Sample/TestWindow.cs`, written by hand in standard CSS only. The form is
a good test case because every position in it is absolute, which is the case the
two renderers can agree on.

The class on each element is its NewWidgets element type. A browser reads
`.window`, NewWidgets reads `window`, and one stylesheet can serve both.

Writing it found four problems. The first one was thought to be blocking; it is
not. It is kept below as written, with the correction attached, because the
reasoning that led to it is the reasoning a future reader will repeat.

### 1. ~~`border-image` cannot address a sprite inside an atlas~~ — SUPERSEDED

**This section was wrong. It can. See "One element per patch" at the end of this
file for the method and the evidence. What follows is the original entry.**

> This is the blocking one.
>
> `background-image` can point into an atlas, because `background-position` moves
> the image behind a window of the element's size. That is the sprite sheet idiom,
> and it works.
>
> `border-image-source` has no equivalent. It takes a whole image. CSS gives no way
> to say "slice the nine-patch out of the rectangle at x, y, w, h inside this
> atlas". The `image()` function was specified with a sub-rectangle form, but no
> shipping browser supports it, so it cannot be used.
>
> So the two mappings do not compose. Sprites map to an atlas. Nine-patches do not.
>
> For a browser to preview a nine-patch panel, each patch source must be its own
> image file. That is an export step, not a CSS feature. NewWidgets itself is not
> affected, because it slices from the atlas directly.
>
> This does not stop the plan. It adds a requirement: the preview export writes one
> small PNG per nine-patch source, and the atlas PNG for everything else.

What was true in it: `border-image-source` really does take a whole image, and
`image()` with a sub-rectangle really is unimplemented. What was wrong is the
step from there to "so it cannot name a sprite". `border-image-slice` measures
its four offsets from the image's own edges, which is exactly enough to name any
rectangle inside the image; the remaining problem — that the eight regions
outside that rectangle are then full of neighbouring artwork — is solved by
`border-image-width` and `border-image-outset` pushing those eight regions
outside the element's box and `clip-path: inset(0)` deleting them.

So there is no per-nine-patch PNG export. `NewWidgets.BrowserPreview` writes one
`ui.png` and nothing else, and every patch in the form is one `<div>`.

### 2. There is no `ui.png` — DONE

The sample ships `ui.rle` and `ui.bin`. A browser reads neither. A preview needs
the atlas as a PNG.

The parts to build this already exist. `RunMobile/Graphics/Textures/Texture.RLE.cs`
decodes the format, and `Tools/SpritePacker/Png.cs` encodes PNG with no
dependencies.

Built. `NewWidgets.BrowserPreview` does exactly that and writes
`preview/ui.png`, 512x512, 27257 bytes, from the shipped `ui.rle`.

### 3. Bitmap fonts are not `@font-face`

The `@font-face` rule in `login.css` names `font5.png`, and that is not valid. A
PNG is not a font file. A browser cannot load a glyph atlas as a font.

This was called out at the start as the thing that will never match. A preview has
to substitute a real font, so line breaks will differ from the game.

### 4. `background-size: 75%` on a nine-patch is not `border-image-width: 75%` — ANSWERED

The doubt was correct, and the answer is that neither percentage form is right.

In the original, `background-size` is a factor on the *source* patch pieces. A
percentage on `border-image-width` resolves against the border image area, so it
is a factor on the *destination*, and the two only coincide when the panel
happens to be the sprite's own size. There is no percentage that says what the
engine means.

What does say it is a length. `border-image-width` in `px`, computed as the
slice offset times the style's scale, makes the scale factor of every outer
region exactly that scale, which is the engine's own definition. That is what the
generator emits: `background-size: 75%` becomes
`border-image-width: 213px 83.25px 111px 240.75px` for `window_9`, and none of
those four numbers mentions the panel size.

## One element per patch

Added 2026-08-24, after section 1 above turned out to be wrong.

A sprite at `(X, Y, W, H)` in an `AW` by `AH` atlas, drawn as a nine-patch at
scale `k`, is one element:

    border-image-source: url(atlas.png);
    border-image-slice:  Y+H/3   AW-(X+2W/3)   AH-(Y+2H/3)   X+W/3   fill
    border-image-width:  <each of those four> * k, in px
    border-image-outset: Y*k   (AW-X-W)*k   (AH-Y-H)*k   X*k
    border-image-repeat: stretch;
    clip-path: inset(0);

with `border-width: 0` so none of it reaches layout. `NinePatch.cs` derives it in
full; the short version is that the left column of regions maps source column `s`
to `-outsetLeft + s * borderWidthLeft / leftSlice`, and the three numbers above
are the unique choice that puts the sprite's left edge on the box's left edge and
the first cut line one corner-width in.

### What the specification requires, and what only Chrome happens to do

Required by CSS Backgrounds and Borders Level 3, section 5:

* "The corner images are scaled to be as wide and as tall as their respective
  border image regions", and the left and right edge images "are made as wide as
  the left and right border image regions". Both give the same horizontal factor
  `borderWidthLeft / leftSlice` for the whole left column, which is what the
  derivation stands on.
* `border-image-outset` moves the border image area outward from the border box,
  and portions drawn outside it "are ink overflow".
* Ink overflow is not clipped by `overflow`. Measured, because it is the one step
  that sounds too convenient to take on trust: with a 64px outset, `overflow:
  hidden` leaves 41984 painted pixels outside the border box — the same number as
  no clipping at all — and `clip-path: inset(0)` leaves 0.

Not in the specification: Chrome rounds `border-image-slice` to whole source
pixels. Measured in Chrome 151 — a slice of `284.6` renders identically to `285`
(0 differing pixels of 40000) and `284.4` identically to `284` (2 pixels, both
one level apart). Nothing in the spec asks for this, so it is the one place where
another engine could legitimately differ. The generator sidesteps it by rounding
the slices itself and deriving the border widths from the rounded value, which is
self-consistent whether or not the engine rounds. **Everything below was measured
in Chrome 151 only. No other engine has been tested.**

### Why the rounding matters

`window_9` is 238x238 at (242, 205), so its cut line is at 242 + 79.333 = 321.333.
Feed that un-rounded into `borderWidthLeft = leftSlice * k` and Chrome rounds the
slice to 321 while keeping the width at 241, making the factor 241/321 = 0.750779
instead of 0.75. The sprite's left edge then lands at +0.19px, and the panel's
first column is the sprite packed to its left — `ui.png` has no gutter, so that is
real artwork.

Measured on a synthetic atlas with `window_9`'s exact rectangle, panel 600x760:
un-rounded slices put **760** neighbouring-sprite pixels inside the panel — one
full column, the panel's whole height — and rounded slices put **0**. On the real
preview the same change moves 1805 pixels by more than 8 levels, all of them in
columns 50-56, which is the window panel's left edge. The fix is causal, not a
coincidence.

### What holds and what breaks

| Case | Result |
|---|---|
| Sprite flush against an atlas edge, so an outset is zero | Holds. Top-left, bottom-right and "the sprite is the whole atlas" all render with 0 stray pixels and exact corners |
| Very small sprite (3x3, 1px cells) | Holds at `k = 1`. At `k = 1/3` the corners round to 0px and the panel becomes a stretch of the middle cell — degraded, but not wrong pixels |
| Non-integer scale (`k` = 0.7, 0.375, 1/3, 0.75) | Holds. 0 stray pixels in every case; the corner lands within the pixel that the browser's own rounding allows |
| Panel narrower than the sum of its two corners | **Breaks**, and the spec says why |
| Scale above 1 | **Degrades**, and the spec does not say why |

The panel-too-small case is `border-image-width`'s proportional reduction:
`f = min(Lwidth/(Wleft+Wright), Lheight/(Wtop+Wbottom))`, measured against the
border image *area*. Substituting the formula above, `f < 1` exactly when
`B < (cornerLeft + cornerRight) * k` — a panel narrower than its own corners,
which is degenerate for a nine-patch anyway. It fails gently: the two corners
still sum to exactly the box width (that falls straight out of the definition of
`f`), but the whole mapping compresses, so the panel loses its outermost source
pixels instead of squeezing them. Measured at a 20px box with 16px corners:
corners came out 10px each, no neighbouring-sprite pixels. It never bleeds — `f <
1` always moves the sprite's edge further outside the box, so the box can only
show source that is further *inside* the sprite.

Scale above 1 is Chrome's smoothing filter reaching across the crop boundary:
a one-pixel ring of the panel picks up neighbouring atlas colour at `k = 2`, two
pixels at `k = 4`, none at `k <= 1`. `image-rendering: pixelated` removes it
entirely. Every scale the login form uses is `<= 1` (0.75 and 0.25), so it does
not bite today, but a preview that ever magnifies a sprite will need that
declaration.

### Evidence

* One element versus the nine-div version it replaces, same sprite, same
  geometry, Chrome 151: **0 differing pixels of 36000**. Reproduced
  independently. A deliberately broken control (outset off by one pixel) gives
  2886 differing pixels of 36000, so the comparison is sensitive.
* One element versus a per-sprite PNG export — the thing section 1 said was
  mandatory — at `window_9`'s real 600x760, `k = 0.75`: **443913 of 456000 pixels
  byte-identical**, every remaining difference inside a 6px ring at the panel
  border, **none anywhere in the interior**. The ring is Chrome resampling a
  321px-wide source strip rather than a 79px one; it is the price of not
  exporting, and it is confined to the border.

### What it costs in elements

| Background | Before, as divs | Now |
|---|---|---|
| Nine-patch | 10 (nine cells plus a grid container) | 1 |
| Three-patch | 4 | 1 |
| Tiled background, 600x760 of a 135x117 tile | 36 (5x7 tiles plus a container) | 1 |
| The whole login form | ~92 | **9** |

`preview/login.css` is 5094 bytes and `preview/login.xhtml` holds 9 `.np` divs;
the generator reports "9 backgrounds, 9 divs, 9 css rules" and 41 passed bounds
assertions. The ~92 is a reconstruction — the nine-div generator no longer exists
to be re-run — and it is 92 or 94 depending on whether each `imagefit` element is
counted as one div or as a wrapper plus a child. Every other number in the table
was recomputed from the shipped `ui.bin` rectangles.
