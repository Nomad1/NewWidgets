using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RunMobile.Graphics;
using SpritePacker;

namespace NewWidgets.BrowserPreview
{
    /// <summary>
    /// Generates a browser-viewable preview of the NewWidgets login form (Sample/Sample/TestWindow.cs)
    /// using exactly the assets the game ships: NewWidgets.RunMobileSample/assets/ui.rle (the atlas)
    /// and ui.bin (the sprite rectangles inside it). No sprite is exported on its own -- every
    /// nine-patch, three-patch, tiled background and single image in the output is ONE element
    /// carrying ONE `border-image` cut out of the one ui.png.
    ///
    /// One element, not nine: `border-image-slice` crops a sprite out of the atlas AND subdivides
    /// it 3x3 at the same time, once `border-image-width` and `border-image-outset` are chosen so
    /// the eight regions full of neighbouring-sprite pixels land outside the element's box, where
    /// `clip-path: inset(0)` removes them. NinePatch.cs derives the three longhands in full. None
    /// of the numbers involved mentions the size of the element, so the same sprite at any panel
    /// size produces byte-identical CSS; CheckSizeIndependence below is the acceptance test for
    /// that claim.
    ///
    /// Reuse, per the task: SpriteData.cs (RunMobile/Graphics) and Png.cs (Tools/SpritePacker) are
    /// both read-only originals, compiled directly into this project (see the .csproj) rather than
    /// copied -- neither has any dependency this console tool cannot already satisfy. The RLE decode
    /// algorithm (Texture.RLE.cs) is copied instead, because its own file's public entry point is
    /// wired into the RunMobile engine's TextureManager/TextureData; see RleDecoder.cs for exactly
    /// what was copied and why. TextureAtlas.cs (RunMobile.Utility/Math) was not needed: it packs new
    /// atlases, it does not read existing ones, and ui.bin's own header (Signature13) carries no
    /// atlas-wide size at all -- the atlas size comes from ui.rle's own header instead.
    /// </summary>
    internal static class Program
    {
        // Relative to this project's own directory, which is where the tool is run from -- the
        // same convention NewWidgets.Test uses for its asset paths. Both can be overridden by
        // the two command-line arguments. Nothing here may name a path on one machine.
        private const string DefaultAssetsDir = "../NewWidgets.RunMobileSample/assets";
        private const string DefaultOutputDir = "../NewWidgets.Test/Conformance/preview";
        private const string AtlasFileName = "ui.png";

        private static int Main(string[] args)
        {
            string assetsDir = args.Length > 0 ? args[0] : DefaultAssetsDir;
            string outputDir = args.Length > 1 ? args[1] : DefaultOutputDir;

            Directory.CreateDirectory(outputDir);

            int atlasWidth;
            int atlasHeight;
            RgbaImage atlas = DecodeAtlas(Path.Combine(assetsDir, "ui.rle"), out atlasWidth, out atlasHeight);

            CheckNotBlank(atlas);

            string pngPath = Path.Combine(outputDir, AtlasFileName);
            Png.Save(pngPath, atlas);

            CheckRoundTrip(pngPath, atlas);

            SpriteData[] sprites = SpriteData.ParseBinaryAtlas(Path.Combine(assetsDir, "ui.bin"), "ui");
            if (sprites == null)
                throw new InvalidOperationException("Failed to parse ui.bin");

            Dictionary<string, SpriteData> spritesByName = new Dictionary<string, SpriteData>(sprites.Length);
            foreach (SpriteData sprite in sprites)
                spritesByName[sprite.SpriteId] = sprite;

            CheckSizeIndependence(RequireSprite(spritesByName, "window_9"), atlasWidth, atlasHeight);

            IReadOnlyList<PatchBox> boxes = BuildLoginFormBoxes(spritesByName, atlasWidth, atlasHeight);

            WriteCss(Path.Combine(outputDir, "login.css"), boxes);
            WriteXhtml(Path.Combine(outputDir, "login.xhtml"), boxes);

            Report(assetsDir, outputDir, pngPath, sprites.Length, boxes, atlasWidth, atlasHeight);

            return 0;
        }

        private static RgbaImage DecodeAtlas(string rlePath, out int atlasWidth, out int atlasHeight)
        {
            byte[] compressed = File.ReadAllBytes(rlePath);

            int channels;
            byte[] raw = RleDecoder.Decode(compressed, out atlasWidth, out atlasHeight, out channels);

            RgbaImage image = new RgbaImage(atlasWidth, atlasHeight);

            if (channels == 4)
            {
                Array.Copy(raw, image.Pixels, raw.Length);
            }
            else if (channels == 3)
            {
                // RGB source with no alpha channel of its own: expand to RGBA, fully opaque.
                for (int i = 0, o = 0; i < raw.Length; i += 3, o += 4)
                {
                    image.Pixels[o + 0] = raw[i + 0];
                    image.Pixels[o + 1] = raw[i + 1];
                    image.Pixels[o + 2] = raw[i + 2];
                    image.Pixels[o + 3] = 255;
                }
            }
            else
            {
                throw new NotSupportedException(string.Format("ui.rle has {0} channels; only 3 (RGB) and 4 (RGBA) are handled", channels));
            }

            return image;
        }

        // Verify #3: the decoded atlas is not blank -- alpha channel is not uniformly zero, and
        // the colour channels are not all one value (which would mean a solid-colour or fully
        // transparent decode, both signs of a decoder bug rather than real sprite art).
        private static void CheckNotBlank(RgbaImage image)
        {
            byte firstR = image.Pixels[0];
            byte firstG = image.Pixels[1];
            byte firstB = image.Pixels[2];
            bool alphaAllZero = true;
            bool colorAllSame = true;

            for (int i = 0; i < image.Pixels.Length; i += 4)
            {
                if (image.Pixels[i + 3] != 0)
                    alphaAllZero = false;

                if (image.Pixels[i] != firstR || image.Pixels[i + 1] != firstG || image.Pixels[i + 2] != firstB)
                    colorAllSame = false;

                if (!alphaAllZero && !colorAllSame)
                    return;
            }

            if (alphaAllZero)
                throw new InvalidOperationException("Decoded ui.png alpha channel is all zero -- decode produced a blank image");
            if (colorAllSame)
                throw new InvalidOperationException("Decoded ui.png colour channels are all one value -- decode produced a blank image");
        }

        // Verify #3 continued: the saved ui.png decodes back (via the same Png.cs this tool
        // links, read-only, from SpritePacker) to the size ui.rle's header claims, with the
        // exact pixels that were encoded.
        private static void CheckRoundTrip(string pngPath, RgbaImage original)
        {
            RgbaImage reloaded = Png.Load(pngPath);

            if (reloaded.Width != original.Width || reloaded.Height != original.Height)
                throw new InvalidOperationException(string.Format(
                    "ui.png round-trip size mismatch: saved {0}x{1}, reloaded {2}x{3}",
                    original.Width, original.Height, reloaded.Width, reloaded.Height));

            for (int i = 0; i < original.Pixels.Length; i++)
            {
                if (reloaded.Pixels[i] != original.Pixels[i])
                    throw new InvalidOperationException(string.Format(
                        "ui.png round-trip pixel mismatch at byte {0}: wrote {1}, read back {2}",
                        i, original.Pixels[i], reloaded.Pixels[i]));
            }
        }

        /// <summary>
        /// The acceptance test for the border-image technique, and the one check that would fail
        /// if the derivation in NinePatch.cs were wrong. The same sprite is built into two
        /// nine-patches of deliberately unrelated sizes -- one the login window's own 600x760, one
        /// a tall thin 137x941 that shares no factor with it -- and all three border-image
        /// longhands must come out byte-identical. They can only do that if the box size has
        /// genuinely cancelled out of the arithmetic, which is what lets one element carry a
        /// nine-patch that resizes.
        /// </summary>
        private static void CheckSizeIndependence(SpriteData sprite, int atlasWidth, int atlasHeight)
        {
            PatchBox wide = NinePatch.BuildNinePatch("size_check", sprite, 0.75, 0, 0, 600, 760, atlasWidth, atlasHeight);
            PatchBox tall = NinePatch.BuildNinePatch("size_check", sprite, 0.75, 0, 0, 137, 941, atlasWidth, atlasHeight);

            if (wide.Slice != tall.Slice)
                throw new InvalidOperationException(string.Format(
                    "border-image-slice depends on the panel size -- 600x760 gives '{0}', 137x941 gives '{1}'",
                    wide.Slice, tall.Slice));

            if (wide.BorderWidth != tall.BorderWidth)
                throw new InvalidOperationException(string.Format(
                    "border-image-width depends on the panel size -- 600x760 gives '{0}', 137x941 gives '{1}'",
                    wide.BorderWidth, tall.BorderWidth));

            if (wide.Outset != tall.Outset)
                throw new InvalidOperationException(string.Format(
                    "border-image-outset depends on the panel size -- 600x760 gives '{0}', 137x941 gives '{1}'",
                    wide.Outset, tall.Outset));

            Console.WriteLine("size check: window_9 at 600x760 and at 137x941, 3 string comparisons, all equal");
            Console.WriteLine("  border-image-slice:  {0}", wide.Slice);
            Console.WriteLine("  border-image-width:  {0}", wide.BorderWidth);
            Console.WriteLine("  border-image-outset: {0}", wide.Outset);
        }

        // The login form's background boxes, laid out exactly as Sample/Sample/TestWindow.cs
        // positions its widgets, and NewWidgets.RunMobileSample/assets/ui.css declares their
        // styles (background-image / background-size / --sprite-tile-x / -y). Every position,
        // size, texture name and scale value below is taken directly from those two files --
        // see NinePatch.cs for the box-shape rules themselves.
        private static IReadOnlyList<PatchBox> BuildLoginFormBoxes(IReadOnlyDictionary<string, SpriteData> sprites, int atlasWidth, int atlasHeight)
        {
            List<PatchBox> boxes = new List<PatchBox>();

            // .window (panel.Size = 600x760, style "window": background-image url("window_9"),
            // background-size: 75%)
            boxes.Add(NinePatch.BuildNinePatch("login_window", RequireSprite(sprites, "window_9"), 0.75,
                0, 0, 600, 760, atlasWidth, atlasHeight));

            // .back_pattern (back.Size = panel.Size = 600x760, Position = (0,0), style
            // "back_pattern": background-image url("back_pattern"), background-repeat: imagetiled,
            // background-size: 100%). Clipped by .window's own overflow: hidden, same as the
            // engine clips it with the widget's Overflow property.
            // --background-opacity: 4% -- an opacity on the sprite draw itself, see
            // NinePatch.FormatCss for why that is CSS `opacity` and not an rgba background-color.
            boxes.Add(NinePatch.BuildTiled("login_back", RequireSprite(sprites, "back_pattern"), 1.0, 0.04,
                0, 0, 600, 760, atlasWidth, atlasHeight));

            // login_edit / pass_edit / local_edit (WidgetTextEdit, Size 500x45, style "textedit":
            // background-image url("panel_white_hovered_9") in its default/unfocused state,
            // background-size: 25%)
            SpriteData textEditSprite = RequireSprite(sprites, "panel_white_hovered_9");
            boxes.Add(NinePatch.BuildNinePatch("login_edit", textEditSprite, 0.25, 50, 200, 500, 45, atlasWidth, atlasHeight));
            boxes.Add(NinePatch.BuildNinePatch("pass_edit", textEditSprite, 0.25, 50, 300, 500, 45, atlasWidth, atlasHeight));
            boxes.Add(NinePatch.BuildNinePatch("local_edit", textEditSprite, 0.25, 50, 100, 500, 45, atlasWidth, atlasHeight));

            // local_check (WidgetCheckBox, Size 40x40, style "checkbox": background-repeat:
            // imagefit, background-image url("checkbox_back_normal")). Checked == false in
            // TestWindow.cs, so the check_icon glyph never draws -- omitted here too.
            boxes.Add(NinePatch.BuildFit("local_check", RequireSprite(sprites, "checkbox_back_normal"), 50, 360, 40, 40, atlasWidth, atlasHeight));

            // login_button ("Connect", WidgetButton, Size 160x48, default "button" style:
            // background-repeat: threeimage, background-image url("button_white_normal_3"))
            boxes.Add(NinePatch.BuildThreePatch("login_button", RequireSprite(sprites, "button_white_normal_3"),
                220, 460, 160, 48, atlasWidth, atlasHeight));

            // logo_image (WidgetImage(ImageFit, "settings_icon"), Size 64x64, Position (20,15))
            boxes.Add(NinePatch.BuildFit("logo_image", RequireSprite(sprites, "settings_icon"), 20, 15, 64, 64, atlasWidth, atlasHeight));

            // text_field (WidgetTextField, Size 500x225, Position (50,520)) uses the same
            // nine-patch source and scale as textedit -- login.css (the hand-written reference)
            // made the same choice for the same reason: WidgetTextField has no style of its own
            // in ui.css, and TestWindow.cs constructs it with the default style.
            boxes.Add(NinePatch.BuildNinePatch("text_field", textEditSprite, 0.25, 50, 520, 500, 225, atlasWidth, atlasHeight));

            return boxes;
        }

        private static SpriteData RequireSprite(IReadOnlyDictionary<string, SpriteData> sprites, string name)
        {
            SpriteData sprite;
            if (!sprites.TryGetValue(name, out sprite))
                throw new InvalidOperationException(string.Format("ui.bin has no sprite named '{0}'", name));

            return sprite;
        }

        private static void WriteCss(string path, IReadOnlyList<PatchBox> boxes)
        {
            StringBuilder text = new StringBuilder();

            text.AppendLine("/* Generated by NewWidgets.BrowserPreview -- do not hand-edit.");
            text.AppendLine("   Every .login_window, .login_back, .login_edit, .pass_edit, .local_edit, .local_check,");
            text.AppendLine("   .login_button, .logo_image and .text_field rule below is ONE element carrying ONE");
            text.AppendLine("   border-image cut out of ui.png -- see NinePatch.cs for the exact nine-patch /");
            text.AppendLine("   three-patch / tiled / fit rules used to build each one, and for the derivation of");
            text.AppendLine("   the slice, width and outset every rule is made of. Those three are constants of the");
            text.AppendLine("   sprite, the atlas and the style scale: no rule mentions a panel size. */");
            text.AppendLine();
            text.AppendLine("body { margin: 0; background-color: #202020; font-family: monospace; font-size: 30px; }");
            text.AppendLine();
            text.AppendLine(".window { position: absolute; left: 50%; top: 50%; margin-left: -300px; margin-top: -380px; width: 600px; height: 760px; overflow: hidden; }");
            text.AppendLine();
            text.AppendLine(".label { position: absolute; color: #ffffff; font-size: 0.6em; height: 35px; overflow: visible; }");
            text.AppendLine(".hidden { display: none; }");
            text.AppendLine();
            text.AppendLine("#login_title  { left: 0;    top: 50px;  width: 600px; text-align: center; font-size: 0.9em; }");
            text.AppendLine("#login_label  { left: 50px; top: 160px; font-size: 0.75em; }");
            text.AppendLine("#pass_label   { left: 50px; top: 260px; font-size: 0.75em; }");
            text.AppendLine("#local_label  { left: 90px; top: 360px; color: #cceeff; }");
            text.AppendLine("#website_button { position: absolute; left: 50px; top: 400px; width: 300px; height: 20px; color: #cceeff; font-size: 0.6em; padding: 4px 2px; }");
            text.AppendLine("#fps_label    { left: 440px; top: 20px;  font-size: 0.45em; }");
            text.AppendLine();
            // ids, not classes: the two spans below carry an id and no class, so a `.login_edit_text`
            // selector matched nothing and both fell out of absolute positioning into the page flow
            text.AppendLine("#login_edit_text, #pass_edit_text { position: absolute; color: #aaaaaa; font-size: 0.5em; padding: 6px 2px 6px 0; }");
            text.AppendLine("#login_edit_text { left: 50px; top: 200px; }");
            text.AppendLine("#pass_edit_text  { left: 50px; top: 300px; }");
            text.AppendLine();
            text.AppendLine("#login_button_text { position: absolute; left: 220px; top: 460px; width: 160px; height: 48px; color: #ffffff; font-size: 0.6em; text-align: center; line-height: 48px; }");
            text.AppendLine();
            text.Append(NinePatch.FormatSharedCss(AtlasFileName));
            text.AppendLine();
            text.Append(NinePatch.FormatCss(boxes, AtlasFileName));

            File.WriteAllText(path, text.ToString());
        }

        private static void WriteXhtml(string path, IReadOnlyList<PatchBox> boxes)
        {
            StringBuilder text = new StringBuilder();

            text.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            text.AppendLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\"");
            text.AppendLine("          \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");
            text.AppendLine("<html xmlns=\"http://www.w3.org/1999/xhtml\">");
            text.AppendLine("<head>");
            text.AppendLine("    <title>NewWidgets login form -- atlas-sourced preview</title>");
            text.AppendLine("    <link rel=\"stylesheet\" href=\"login.css\" />");
            text.AppendLine("</head>");
            text.AppendLine("<body>");
            text.AppendLine("    <div class=\"window\" id=\"login_window\">");

            AppendPatch(text, FindBox(boxes, "login_window"));
            AppendPatch(text, FindBox(boxes, "login_back"));

            text.AppendLine("        <span class=\"label\" id=\"login_title\">Connect to server</span>");
            text.AppendLine("        <span class=\"label\" id=\"login_label\">Login</span>");

            AppendPatch(text, FindBox(boxes, "login_edit"));
            text.AppendLine("        <span id=\"login_edit_text\">login</span>");

            text.AppendLine("        <span class=\"label\" id=\"pass_label\">Password</span>");
            AppendPatch(text, FindBox(boxes, "pass_edit"));
            text.AppendLine("        <span id=\"pass_edit_text\">********</span>");

            text.AppendLine("        <span class=\"label\" id=\"local_label\">Custom server</span>");
            AppendPatch(text, FindBox(boxes, "local_check"));

            AppendPatch(text, FindBox(boxes, "local_edit"), "hidden");

            text.AppendLine("        <span id=\"website_button\">Register new account</span>");

            AppendPatch(text, FindBox(boxes, "login_button"));
            text.AppendLine("        <span id=\"login_button_text\">Connect</span>");

            AppendPatch(text, FindBox(boxes, "logo_image"));

            AppendPatch(text, FindBox(boxes, "text_field"));

            text.AppendLine("        <span class=\"label\" id=\"fps_label\">FPS: 55.7/61.1</span>");

            text.AppendLine("    </div>");
            text.AppendLine("</body>");
            text.AppendLine("</html>");

            File.WriteAllText(path, text.ToString());
        }

        // One element per background, children included: there are none. The whole nine-patch --
        // atlas crop, 3x3 subdivision, stretched edges -- is one `border-image` on this one div.
        private static void AppendPatch(StringBuilder text, PatchBox box, string extraClass = null)
        {
            string classAttr = extraClass == null ? box.ClassName : box.ClassName + " " + extraClass;

            text.AppendFormat("        <div class=\"np {0}\"></div>", classAttr).AppendLine();
        }

        private static PatchBox FindBox(IReadOnlyList<PatchBox> boxes, string className)
        {
            foreach (PatchBox box in boxes)
            {
                if (box.ClassName == className)
                    return box;
            }

            throw new InvalidOperationException(string.Format("No box named '{0}'", className));
        }

        private static void Report(string assetsDir, string outputDir, string pngPath, int spriteCount,
            IReadOnlyList<PatchBox> boxes, int atlasWidth, int atlasHeight)
        {
            long rleSize = new FileInfo(Path.Combine(assetsDir, "ui.rle")).Length;
            long binSize = new FileInfo(Path.Combine(assetsDir, "ui.bin")).Length;
            long pngSize = new FileInfo(pngPath).Length;

            Console.WriteLine("assets:    {0}", assetsDir);
            Console.WriteLine("output:    {0}", outputDir);
            Console.WriteLine("ui.rle:    {0} bytes", rleSize);
            Console.WriteLine("ui.bin:    {0} bytes, {1} sprites", binSize, spriteCount);
            Console.WriteLine("ui.png:    {0} bytes, {1}x{2}", pngSize, atlasWidth, atlasHeight);
            Console.WriteLine("css:       {0} bytes", new FileInfo(Path.Combine(outputDir, "login.css")).Length);
            Console.WriteLine("xhtml:     {0} bytes", new FileInfo(Path.Combine(outputDir, "login.xhtml")).Length);
            Console.WriteLine("boxes:     {0} backgrounds, {0} divs, {0} css rules -- one element each", boxes.Count);
            Console.WriteLine("bounds:    {0} assertions on sprite rectangles and slice insets, all passed", NinePatch.BoundsChecks);
        }
    }
}
