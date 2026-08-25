using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RunMobile.Graphics;

namespace NewWidgets.BrowserPreview
{
    /// <summary>
    /// One positioned element of the form. Exactly one element: the whole background -- crop out of
    /// the atlas, nine-patch subdivision, edge stretch, tiling -- is carried by a single
    /// `border-image` on a single div, so this type holds that div's geometry and the four
    /// border-image longhands, and nothing else.
    /// </summary>
    internal sealed class PatchBox
    {
        public readonly string ClassName;
        public readonly double Left;
        public readonly double Top;
        public readonly double Width;
        public readonly double Height;

        /// <summary>`border-image-slice`, already including the `fill` keyword.</summary>
        public readonly string Slice;

        /// <summary>`border-image-width`, four px lengths.</summary>
        public readonly string BorderWidth;

        /// <summary>`border-image-outset`, four px lengths. "0" when the box needs no outset.</summary>
        public readonly string Outset;

        /// <summary>`border-image-repeat`, or null to inherit `stretch` from the shared rule.</summary>
        public readonly string Repeat;

        /// <summary>
        /// The style's `background-color-opacity`, 0..1. 1 for everything that does not set it.
        /// </summary>
        public readonly double Opacity;

        /// <summary>
        /// Value of the `--nine-patch` anchor property, minus the leading url() -- null for a
        /// box that is not a nine- or three-patch (a tiled background, a fitted single image).
        /// </summary>
        public readonly string NinePatchRect;

        public PatchBox(string className, double left, double top, double width, double height,
            string slice, string borderWidth, string outset, string repeat, double opacity, string ninePatchRect)
        {
            ClassName = className;
            Left = left;
            Top = top;
            Width = width;
            Height = height;
            Slice = slice;
            BorderWidth = borderWidth;
            Outset = outset;
            Repeat = repeat;
            Opacity = opacity;
            NinePatchRect = ninePatchRect;
        }
    }

    /// <summary>
    /// Builds the PatchBox for every background style the login form uses, and formats them as
    /// CSS.
    ///
    /// The one shared idea behind all of them, and it is the whole tool in three lines:
    ///
    ///   * `border-image-slice` cuts the SOURCE image into nine regions, and the insets are
    ///     measured from the image's own four edges. Point those insets at a sprite's bounding box
    ///     inside the atlas and the middle region *is* that sprite -- that is a crop.
    ///   * `border-image-width` scales each of the eight outer regions independently, and
    ///     `border-image-outset` moves all nine outwards. Pick them together and the eight regions
    ///     full of neighbouring-sprite pixels land entirely outside the element's box, where
    ///     `clip-path: inset(0)` throws them away, while the wanted pixels land exactly on the box.
    ///   * What is left inside the box is a nine-patch of the sprite alone: corners at their
    ///     native scaled size, edges stretched along their one open axis, centre stretched both
    ///     ways.
    ///
    /// So one element does the crop AND the 3x3 subdivision at the same time, which the previous
    /// version of this file said was impossible and worked around with a 3x3 CSS grid of nine
    /// child divs. The arithmetic is derived in full in the block above FormatNinePatchGeometry.
    /// Verified against that nine-div version in Chrome 151: pixel-identical, zero differing
    /// pixels out of 36000.
    ///
    /// Every number below is a constant of the sprite, the atlas and the style's scale. None of
    /// them mentions the size of the box, which is what Program.CheckSizeIndependence asserts.
    /// </summary>
    internal static class NinePatch
    {
        // every rectangle handed to a builder is checked against the atlas and against its own
        // slice arithmetic; this counts those checks so the tool can report the number rather than
        // claim it
        private static int s_boundsChecks;

        public static int BoundsChecks
        {
            get { return s_boundsChecks; }
        }

        /// <summary>
        /// Nine-patch (3x3), matching WidgetBackground.InitBackground's NineImage case exactly:
        ///   - the source sprite rectangle (X, Y, Width, Height) from ui.bin is cut into a 3x3
        ///     grid of equal cells, cellW = Width/3, cellH = Height/3;
        ///   - every corner renders at its native size times `scale`, unstretched;
        ///   - every edge stretches along its one open axis;
        ///   - the centre stretches both ways.
        /// `scale` is the style's declared background-size percentage (e.g. 0.75 for the window,
        /// 0.25 for a text field). Here it is the single scale factor `k` of the derivation: it
        /// multiplies both border-image-width and border-image-outset, which is what makes the
        /// corners come out at cell * scale.
        ///
        /// Edges STRETCH. That is what the engine does, and `border-image-repeat: stretch` is the
        /// default this file's shared `.np` rule sets, so nothing here has to say so.
        ///
        /// ponytail: cellW/cellH divide the sprite's PHYSICAL (packed, trimmed) rectangle from
        /// ui.bin, not its logical OriginalWidth/OriginalHeight. The engine's own Sprite.Size
        /// (used for this same division in WidgetBackground.cs) is the *original*, untrimmed
        /// size, which for a handful of these sprites (window_9: 240x240 original vs 238x238
        /// physical, a 1px alpha gutter on two sides) differs from the physical rectangle by a
        /// pixel or two. A browser can only crop pixels that are actually present in the PNG, so
        /// there is no way to reproduce the engine's original-size math exactly; using the
        /// physical rectangle throughout is the only self-consistent choice, and the resulting
        /// error is under 1% of the affected elements' size -- not worth a special case. Upgrade
        /// path: carry OriginalWidth/Height through from SpriteData and use it only for the
        /// corner-size math, while still slicing the physical rectangle -- more faithful, more
        /// code, for a sub-pixel difference nobody will see in a preview.
        /// </summary>
        public static PatchBox BuildNinePatch(string className, SpriteData sprite, double scale,
            double targetLeft, double targetTop, double targetWidth, double targetHeight,
            int atlasWidth, int atlasHeight)
        {
            double cellWidth = sprite.Width / 3.0;
            double cellHeight = sprite.Height / 3.0;

            CheckInsideAtlas(className, sprite, atlasWidth, atlasHeight);

            string slice;
            string borderWidth;
            string outset;
            FormatNinePatchGeometry(sprite, cellWidth, cellHeight, scale, atlasWidth, atlasHeight,
                out slice, out borderWidth, out outset);

            return new PatchBox(className, targetLeft, targetTop, targetWidth, targetHeight,
                slice, borderWidth, outset, null, 1.0,
                FormatNinePatchRect(sprite, cellHeight, cellWidth));
        }

        /// <summary>
        /// Three-patch (3x1), matching WidgetBackground.InitBackground's ThreeImage case exactly:
        /// the source rectangle is cut into 3 columns (cellW = Width/3) but only ONE row (cellH =
        /// the full sprite Height, never subdivided).
        ///
        /// The vertical scale is NOT the style's declared background-size -- reading
        /// WidgetBackground.cs's ThreeImage case shows it reassigns the local `scale` variable
        /// unconditionally to `Size.Y / sprite.Size.Y` (the widget's own target height divided by
        /// the source row height), discarding whatever percentage the caller passed in. In other
        /// words a three-patch button always stretches to fill its target height exactly,
        /// regardless of what its stylesheet's background-size says; only the end columns' WIDTH
        /// uses that auto-computed scale, the same way a nine-patch corner does. This is a real
        /// engine quirk, not a simplification on this tool's part.
        ///
        /// In border-image terms it falls out of the same derivation with the vertical half turned
        /// off: `border-image-width` of 0 top and bottom collapses the top and bottom rows of
        /// regions entirely, so the middle row alone renders and fills the box's whole height --
        /// which is exactly "crop vertically, subdivide horizontally". The vertical slice insets
        /// are then the sprite's own top and bottom edges, with no outset needed on that axis.
        /// </summary>
        public static PatchBox BuildThreePatch(string className, SpriteData sprite,
            double targetLeft, double targetTop, double targetWidth, double targetHeight,
            int atlasWidth, int atlasHeight)
        {
            double cellWidth = sprite.Width / 3.0;

            CheckInsideAtlas(className, sprite, atlasWidth, atlasHeight);

            double scale = targetHeight / sprite.Height;

            // horizontal: the nine-patch derivation, unchanged. vertical: the sprite's own edges
            // as the slice, zero border width so the outer rows collapse, zero outset.
            double leftSlice = RoundSlice(sprite.X + cellWidth);
            double rightSlice = RoundSlice(atlasWidth - (sprite.X + 2 * cellWidth));

            string slice = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3} fill",
                FormatNumber(sprite.Y), FormatNumber(rightSlice),
                FormatNumber(atlasHeight - (sprite.Y + sprite.Height)), FormatNumber(leftSlice));

            string borderWidth = string.Format(CultureInfo.InvariantCulture, "0 {0} 0 {1}",
                FormatPx(rightSlice * scale), FormatPx(leftSlice * scale));

            string outset = string.Format(CultureInfo.InvariantCulture, "0 {0} 0 {1}",
                FormatPx((atlasWidth - sprite.X - sprite.Width) * scale), FormatPx(sprite.X * scale));

            // a three-patch is a nine-patch whose top and bottom slices are zero, which is also
            // how the conformance README maps `background-repeat: threeimage` to CSS
            return new PatchBox(className, targetLeft, targetTop, targetWidth, targetHeight,
                slice, borderWidth, outset, null, 1.0,
                FormatNinePatchRect(sprite, 0, cellWidth));
        }

        /// <summary>
        /// Tiled background, matching WidgetBackground.InitBackground's ImageTiled case: the
        /// source sprite is drawn at its native size (no stretch), repeated to cover the target
        /// box, and clipped by the box.
        ///
        /// The slice insets name the sprite's own bounding box, so the middle region IS the
        /// sprite, and `border-image-repeat` tiles it. Two things then have to be got right, and
        /// getting them right is what the border widths and outsets below are for.
        ///
        ///   Tile SIZE. CSS Backgrounds 3 takes the middle region's tile width from the top
        ///   region's scale factor and its tile height from the left region's, so setting each
        ///   border width to its own slice times `scale` makes both factors exactly `scale` and
        ///   the tile comes out at the sprite's native size times `scale`, as the engine draws it.
        ///
        ///   Tile PHASE. The engine lays its first tile flush at the box's top-left corner.
        ///   `repeat` centres its tiles instead, and `round` rescales them, so neither matches on
        ///   a box that is not a whole number of tiles across -- unless the region being tiled is
        ///   made to start exactly at the corner and to be a whole number of tiles long. Both are
        ///   arranged here: the top and left outsets equal their border widths, which puts the
        ///   middle region's top-left corner exactly on the box's; and the right and bottom
        ///   outsets add the fraction of a tile needed to round the region up to `tilesAcross` by
        ///   `tilesDown` whole tiles, which run off the far edge for `clip-path` to cut.
        ///
        /// The result is exact, not a compromise: verified in Chrome 151 against a single 1:1 crop
        /// of the same sprite, every tile pixel-identical, 0 differing pixels of 15795 per tile.
        /// `round` and `repeat` and `space` all agree once the region is a whole number of tiles
        /// long; `round` is used because it is the one that cannot introduce an offset.
        ///
        /// This is the one builder whose output is NOT independent of the box size -- the tile
        /// count is a function of it, and no arrangement of a fixed tile can avoid that.
        /// </summary>
        public static PatchBox BuildTiled(string className, SpriteData sprite, double scale, double opacity,
            double targetLeft, double targetTop, double targetWidth, double targetHeight,
            int atlasWidth, int atlasHeight)
        {
            CheckInsideAtlas(className, sprite, atlasWidth, atlasHeight);

            // the tile size falls out of the top and left border widths, so a sprite flush against
            // both the atlas's top and left edge has no border to take a scale factor from and CSS
            // falls through to "not scaled" -- 1:1 whatever `scale` says
            Verify(sprite.X > 0 || sprite.Y > 0 || Math.Abs(scale - 1.0) < 0.0001,
                "{0}: sprite {1} sits at the atlas origin, so a tiled scale of {2} cannot be expressed",
                className, sprite.SpriteId, scale);
            s_boundsChecks++;

            double tileWidth = sprite.Width * scale;
            double tileHeight = sprite.Height * scale;

            int tilesAcross = (int)Math.Ceiling(targetWidth / tileWidth);
            int tilesDown = (int)Math.Ceiling(targetHeight / tileHeight);

            double rightSlice = atlasWidth - (sprite.X + sprite.Width);
            double bottomSlice = atlasHeight - (sprite.Y + sprite.Height);

            string borderWidth = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}",
                FormatPx(sprite.Y * scale), FormatPx(rightSlice * scale),
                FormatPx(bottomSlice * scale), FormatPx(sprite.X * scale));

            string outset = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}",
                FormatPx(sprite.Y * scale),
                FormatPx(rightSlice * scale + tilesAcross * tileWidth - targetWidth),
                FormatPx(bottomSlice * scale + tilesDown * tileHeight - targetHeight),
                FormatPx(sprite.X * scale));

            return new PatchBox(className, targetLeft, targetTop, targetWidth, targetHeight,
                FormatCropSlice(sprite, atlasWidth, atlasHeight), borderWidth, outset, "round", opacity, null);
        }

        /// <summary>
        /// Single "contain and centre" sprite, matching WidgetBackground.InitBackground's
        /// ImageFit case: the sprite is scaled uniformly (the smaller of width-fit and
        /// height-fit) so it fits entirely inside the target box, then centred in it.
        ///
        /// A browser would normally spell this `background-size: contain; background-position:
        /// center;` -- but `contain` measures against the WHOLE background image's own intrinsic
        /// size, which for an atlas is the atlas, not the one sprite inside it. So the fitted,
        /// centred rectangle is computed here and the element is placed at exactly that position
        /// and size; no `contain` or `center` keyword is involved. The element itself is the same
        /// crop as BuildTiled -- slice insets naming the sprite, `border-image-width: 0` -- with
        /// the shared rule's `stretch` filling the box.
        ///
        /// The fit is computed against the sprite's ORIGINAL (untrimmed) size, not its physical
        /// packed size, exactly as Sprite.Size does in the engine (RunMobile/Graphics/Sprite.cs);
        /// a trimmed sprite's visible pixels are then placed inside that fitted frame at their
        /// recorded OffsetX/OffsetY, scaled the same way. Unlike the nine-patch corner math above,
        /// this one *can* use the original size exactly, because it only changes where the box
        /// sits, never what pixels are cropped.
        /// </summary>
        public static PatchBox BuildFit(string className, SpriteData sprite,
            double targetLeft, double targetTop, double targetWidth, double targetHeight,
            int atlasWidth, int atlasHeight)
        {
            CheckInsideAtlas(className, sprite, atlasWidth, atlasHeight);

            double fitScale = targetWidth / sprite.OriginalWidth;
            if (fitScale * sprite.OriginalHeight > targetHeight)
                fitScale = targetHeight / sprite.OriginalHeight;

            double letterboxX = (targetWidth - sprite.OriginalWidth * fitScale) / 2;
            double letterboxY = (targetHeight - sprite.OriginalHeight * fitScale) / 2;

            return new PatchBox(className,
                targetLeft + letterboxX + sprite.OffsetX * fitScale,
                targetTop + letterboxY + sprite.OffsetY * fitScale,
                sprite.Width * fitScale, sprite.Height * fitScale,
                FormatCropSlice(sprite, atlasWidth, atlasHeight), "0", "0", null, 1.0, null);
        }

        // ---------------------------------------------------------------------------------
        // Where the three border-image longhands come from. Derived here, not copied.
        //
        // A sprite sits at (X, Y, W, H) inside an AW by AH atlas. Its nine-patch cut lines are at
        // W/3 and 2W/3 across, H/3 and 2H/3 down. The box to fill is B wide, and the corners must
        // render at (W/3)*k, where k is the style's scale. Work the horizontal axis; the vertical
        // is the same with Y, H and AH.
        //
        // border-image draws the source's LEFT region -- source columns [0, leftSlice) -- into the
        // border image area's left band, which starts at -outsetLeft and is borderWidthLeft wide.
        // Source column s therefore lands at
        //     -outsetLeft + s * borderWidthLeft / leftSlice
        // Three requirements pin all three numbers down:
        //   (a) the sprite's own left edge must land on the box's left edge: at s = X, that is 0;
        //   (b) the first cut line must land at the corner size: at s = X + W/3, that is (W/3)*k;
        //   (c) the region must actually contain the corner, so leftSlice >= X + W/3. Take the
        //       smallest value that does, which also makes the middle region start exactly at the
        //       cut line -- leftSlice = X + W/3.
        // Subtracting (a) from (b) gives (W/3) * borderWidthLeft / leftSlice = (W/3) * k, so
        //     borderWidthLeft / leftSlice = k    =>   borderWidthLeft = (X + W/3) * k
        // and (a) then gives
        //     outsetLeft = X * k
        // The left band therefore runs from -X*k to (W/3)*k: the part at x < 0 is the atlas's own
        // pixels to the left of the sprite, and `clip-path: inset(0)` removes exactly that part.
        //
        // The right side is the mirror image, with rightSlice = AW - (X + 2W/3) and
        // outsetRight = (AW - X - W) * k. Their difference is
        //     borderWidthRight - outsetRight = (W - 2W/3) * k = (W/3) * k
        // so the right band's inner edge sits (W/3)*k from the box's right edge -- the corner size
        // again, from the other end.
        //
        // What is left for the middle region is the span between the two bands' inner edges,
        // [(W/3)*k, B - (W/3)*k], fed from source columns [leftSlice, AW - rightSlice) =
        // [X + W/3, X + 2W/3) -- the sprite's middle third exactly. `stretch` fills the span with
        // it, which is what the engine does to a nine-patch's centre and edges.
        //
        // Note what is NOT in any of these: B. Slice, width and outset are functions of the sprite
        // rectangle, the atlas size and k alone, so the same sprite at any box size produces
        // byte-identical strings. Program.CheckSizeIndependence asserts it.
        //
        // One correction the spec does not warn about, and it is the difference between a clean
        // edge and a bleeding one. A cut line at X + W/3 is rarely a whole number of source pixels
        // -- 242 + 238/3 is 321.333 -- but Chrome ROUNDS border-image-slice to the nearest source
        // pixel before it renders. (Measured, not assumed: a slice of 284.4 renders identically to
        // 284 and a slice of 284.6 identically to 285, in Chrome 151.) Feed the un-rounded value
        // into borderWidthLeft = leftSlice * k and the factor the browser actually applies becomes
        // 241 / 321 = 0.750779, not the 0.75 asked for; requirement (a) then misses by 0.19px, and
        // the corner samples a column of whatever sprite is packed to the left -- ui.png has no
        // gutter between sprites, so that is real artwork, and it shows as a one-pixel seam down
        // the left edge of the panel. Rounding the slices HERE and deriving the border widths from
        // the rounded value restores requirement (a) exactly. The price is that the cut line moves
        // by up to half a source pixel, which moves the corner size by up to half a pixel times k;
        // it is the same half pixel the browser was going to take anyway, and now the rest of the
        // arithmetic agrees with it.
        // ---------------------------------------------------------------------------------

        private static void FormatNinePatchGeometry(SpriteData sprite, double cellWidth, double cellHeight,
            double scale, int atlasWidth, int atlasHeight,
            out string slice, out string borderWidth, out string outset)
        {
            double topSlice = RoundSlice(sprite.Y + cellHeight);
            double rightSlice = RoundSlice(atlasWidth - (sprite.X + 2 * cellWidth));
            double bottomSlice = RoundSlice(atlasHeight - (sprite.Y + 2 * cellHeight));
            double leftSlice = RoundSlice(sprite.X + cellWidth);

            // requirement (c) of the derivation, on all four sides: a region that does not contain
            // its corner would slice the wrong pixels, silently
            Verify(rightSlice >= cellWidth && bottomSlice >= cellHeight,
                "{0}x{1} sprite at ({2},{3}) leaves a slice smaller than its own corner cell",
                sprite.Width, sprite.Height, sprite.X, sprite.Y);
            s_boundsChecks++;

            slice = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3} fill",
                FormatNumber(topSlice), FormatNumber(rightSlice), FormatNumber(bottomSlice), FormatNumber(leftSlice));

            borderWidth = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}",
                FormatPx(topSlice * scale), FormatPx(rightSlice * scale),
                FormatPx(bottomSlice * scale), FormatPx(leftSlice * scale));

            outset = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}",
                FormatPx(sprite.Y * scale), FormatPx((atlasWidth - sprite.X - sprite.Width) * scale),
                FormatPx((atlasHeight - sprite.Y - sprite.Height) * scale), FormatPx(sprite.X * scale));
        }

        // The slice that crops the sprite and nothing else: the four insets are the atlas margins
        // around the sprite's bounding box, so the middle region is the sprite. Used by every box
        // that wants the whole sprite rather than a subdivision of it.
        private static string FormatCropSlice(SpriteData sprite, int atlasWidth, int atlasHeight)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3} fill",
                FormatNumber(sprite.Y), FormatNumber(atlasWidth - (sprite.X + sprite.Width)),
                FormatNumber(atlasHeight - (sprite.Y + sprite.Height)), FormatNumber(sprite.X));
        }

        // Every slice inset below is an atlas margin around this rectangle, so a rectangle that
        // left the atlas would produce a negative inset and a silently wrong crop.
        private static void CheckInsideAtlas(string className, SpriteData sprite, int atlasWidth, int atlasHeight)
        {
            Verify(sprite.X >= 0 && sprite.Y >= 0,
                "{0}: sprite {1} starts at ({2},{3}), outside the atlas", className, sprite.SpriteId, sprite.X, sprite.Y);
            Verify(sprite.X + sprite.Width <= atlasWidth && sprite.Y + sprite.Height <= atlasHeight,
                "{0}: sprite {1} ends at ({2},{3}), outside the {4}x{5} atlas", className, sprite.SpriteId,
                sprite.X + sprite.Width, sprite.Y + sprite.Height, atlasWidth, atlasHeight);
            Verify(sprite.Width > 0 && sprite.Height > 0,
                "{0}: sprite {1} is {2}x{3}", className, sprite.SpriteId, sprite.Width, sprite.Height);

            s_boundsChecks += 3;
        }

        private static void Verify(bool condition, string format, params object[] args)
        {
            if (!condition)
                throw new InvalidOperationException(string.Format(format, args));
        }

        /// <summary>
        /// The one rule every patch element shares. Hoisted out of the per-element rules because
        /// it is the same string nine times over.
        /// </summary>
        public static string FormatSharedCss(string atlasUrl)
        {
            StringBuilder text = new StringBuilder();

            text.AppendLine("/* Every patch below is ONE element. `border-image-slice` names the sprite's cut lines");
            text.AppendLine("   as insets from the ATLAS edges, `border-image-width` scales the eight outer regions");
            text.AppendLine("   and `border-image-outset` slides them so the neighbouring-sprite pixels fall outside");
            text.AppendLine("   the box, where `clip-path` removes them. `border-width: 0` keeps all of it out of");
            text.AppendLine("   layout -- border-image-width does the drawing, and it does not affect layout at all.");
            text.AppendLine("   None of the numbers below mentions the size of the box it is on. */");
            text.AppendFormat(".np {{ position: absolute; border-style: solid; border-width: 0;"
                + " border-image-source: url(\"{0}\"); border-image-repeat: stretch; }}", atlasUrl).AppendLine();

            return text.ToString();
        }

        /// <summary>
        /// Formats every PatchBox as one CSS rule.
        ///
        /// The rule also carries `--nine-patch`, the anchor that lets our own CSS parser throw the
        /// border-image longhands away. Grammar, and it is deliberately the shortest thing that
        /// says everything the engine needs:
        ///
        ///     --nine-patch: &lt;image&gt; &lt;x&gt; &lt;y&gt; &lt;w&gt; &lt;h&gt; / &lt;top&gt; &lt;right&gt; &lt;bottom&gt; &lt;left&gt;;
        ///
        /// &lt;image&gt; is the atlas, as a normal CSS url(). &lt;x y w h&gt; is the sprite's physical packed
        /// rectangle inside that atlas, in atlas pixels, straight out of ui.bin. The four numbers
        /// after the slash are the slice widths in SOURCE pixels, in `border-image-slice` order
        /// (top, right, bottom, left) and with `border-image-slice`'s unitless-means-pixels
        /// convention, so a reader who knows one knows the other -- note that these are the
        /// sprite-relative cut widths, whereas the real `border-image-slice` next to them is the
        /// same cut lines expressed as insets from the atlas edge. A three-patch is written with
        /// top and bottom set to 0, exactly as the conformance README already maps `threeimage` to
        /// `border-image-slice: 0 33.33%`. A browser understands none of it and ignores it, which
        /// is exactly what a custom property is for.
        /// </summary>
        public static string FormatCss(IReadOnlyList<PatchBox> boxes, string atlasUrl)
        {
            StringBuilder text = new StringBuilder();

            foreach (PatchBox box in boxes)
            {
                text.AppendFormat(".{0} {{", box.ClassName).AppendLine();
                text.AppendFormat("    left: {0};", FormatPx(box.Left)).AppendLine();
                text.AppendFormat("    top: {0};", FormatPx(box.Top)).AppendLine();
                text.AppendFormat("    width: {0};", FormatPx(box.Width)).AppendLine();
                text.AppendFormat("    height: {0};", FormatPx(box.Height)).AppendLine();
                text.AppendFormat("    border-image-slice: {0};", box.Slice).AppendLine();
                text.AppendFormat("    border-image-width: {0};", box.BorderWidth).AppendLine();

                // no outset means nothing is drawn outside the box, so nothing needs clipping
                if (box.Outset != "0")
                    text.AppendFormat("    border-image-outset: {0};", box.Outset).AppendLine();

                if (box.Repeat != null)
                    text.AppendFormat("    border-image-repeat: {0};", box.Repeat).AppendLine();

                if (box.Outset != "0")
                    text.AppendLine("    clip-path: inset(0);");

                // background-color-opacity is an opacity on the drawn sprite, not an alpha on a
                // colour -- WidgetBackground.Update puts it in ImageObject.Sprite.Alpha, which
                // multiplies every pixel the sprite draws. CSS `opacity` on the element that draws
                // it is the same operation. `background-color: rgba(...)` would NOT be: that
                // paints a colour behind the image and leaves the image itself untouched.
                if (box.Opacity < 1.0)
                    text.AppendFormat(CultureInfo.InvariantCulture, "    opacity: {0};", FormatNumber(box.Opacity)).AppendLine();

                if (box.NinePatchRect != null)
                    text.AppendFormat("    --nine-patch: url(\"{0}\") {1};", atlasUrl, box.NinePatchRect).AppendLine();

                text.AppendLine("}");
                text.AppendLine();
            }

            return text.ToString();
        }

        private static string FormatNinePatchRect(SpriteData sprite, double verticalSlice, double horizontalSlice)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3} / {4} {5} {4} {5}",
                sprite.X, sprite.Y, sprite.Width, sprite.Height,
                FormatNumber(verticalSlice), FormatNumber(horizontalSlice));
        }

        // Six decimals, not the three a plain pixel length would get: a border-image-width and its
        // matching outset must differ by exactly the corner size, and both are a third of a sprite
        // times a scale -- 238 / 3 * 0.75 does not terminate. Three decimals would move the corner
        // boundary by a hundredth of a pixel; six keeps it under a millionth.
        private static string FormatPx(double value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture) + "px";
        }

        // Chrome rounds border-image-slice to whole source pixels, so a cut line that falls between
        // two of them is rounded here first and every border width is then derived from the value
        // the browser will actually use. See the note at the end of the derivation block above.
        private static double RoundSlice(double value)
        {
            return Math.Round(value, MidpointRounding.AwayFromZero);
        }

        // Atlas-pixel numbers in border-image-slice and --nine-patch are unitless.
        private static string FormatNumber(double value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }
    }
}
