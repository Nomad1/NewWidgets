using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

using NewWidgets.UI;
using NewWidgets.UI.Styles;
using NewWidgets.Utility;
using NewWidgets.Widgets;

namespace NewWidgets.Test
{
    /// <summary>
    /// Test groups for the CSS profile of D132/D134: the twenty-odd standard properties a
    /// browser and NewWidgets must both understand.
    ///
    /// Test 52 is the one that matters. It loads the project's own conformance stylesheet,
    /// <c>Conformance/login.css</c>, from disk through the real <see cref="WidgetManager.LoadCSS"/>
    /// and asserts it produces no exception and no log output at all. Nothing in the suite did
    /// that before, which is exactly how that file and the parser were free to drift apart --
    /// every other group here builds its CSS from a string literal that was written to match
    /// whatever the parser already did.
    ///
    /// The remaining groups pin one cause each, so a regression names itself rather than
    /// showing up as "login.css stopped loading".
    /// </summary>
    internal static class ConformanceCssTests
    {
        private const string ConformanceLoginCssPath = "Conformance/login.css";

        // login.css declares @font-face with src: url("font5.png"). Font's constructor walks
        // the sprite's frames looking for a space glyph and throws if there is none, so the
        // resource has to be a registered test font sheet before the stylesheet is read --
        // the same requirement CorpusTests.EnsureTestFontsRegistered documents.
        private const string LoginFontResource = "font5.png";

        public static void Register()
        {
            TestRunner.Add("Test 52: Conformance/login.css loads clean", Test52_ConformanceLoginStylesheet);
            TestRunner.Add("Test 53: position is a CSS keyword", Test53_PositionKeyword);
            TestRunner.Add("Test 54: background-position keywords, percentages and pixels", Test54_BackgroundPosition);
            TestRunner.Add("Test 55: background-size keywords", Test55_BackgroundSize);
            TestRunner.Add("Test 56: background-repeat CSS keywords", Test56_BackgroundRepeat);
            TestRunner.Add("Test 57: border-image longhands are parsed and stored", Test57_BorderImage);
            TestRunner.Add("Test 58: @font-face registers a font and a font stack resolves", Test58_FontFace);
            TestRunner.Add("Test 59: deliberately ignored properties, clip-path and display", Test59_IgnoredAndMapped);
            TestRunner.Add("Test 73: an alpha on background-color reaches the renderer", Test73_BackgroundColorAlpha);
            TestRunner.Add("Test 74: @font-face and @font.name register the same font", Test74_FontFaceMatchesFontAtRule);

            TestRunner.AddKnownFailure("Test 59b: display:none hides the widget",
                "the display property is parsed and stored (Test 59 covers that) but nothing reads it: Widget.UpdateStyle turns the declared box properties into a position and a size and never touches Visible, so a widget carrying display:none still draws. Applying it belongs in Widget.cs/Widget.UpdateStyle, which D144 puts off limits to a parser change",
                Test59b_DisplayNoneHidesWidget);
        }

        /// <summary>
        /// Loads the real conformance stylesheet from disk. The style collection is reset
        /// first: login.css declares ordinary names like <c>.window</c> and <c>.label</c>,
        /// and WidgetManager's collection is process-wide and merges same-named rules, so
        /// leaving the corpus stylesheets in place would blend the two.
        /// </summary>
        private static void Test52_ConformanceLoginStylesheet(TestContext context)
        {
            TestController controller = PrepareIsolatedLoad();

            context.IsTrue(File.Exists(ConformanceLoginCssPath), "the conformance stylesheet {0} should be readable from the test working directory", ConformanceLoginCssPath);

            if (!File.Exists(ConformanceLoginCssPath))
                return;

            string css = File.ReadAllText(ConformanceLoginCssPath);

            context.DoesNotThrow(delegate
            {
                WidgetManager.LoadCSS(css);
            }, "loading the conformance stylesheet should not throw");

            context.AreEqual(0, controller.Errors.Count, "the conformance stylesheet should log no errors, got: {0}", Join(controller.Errors));
            context.AreEqual(0, controller.Messages.Count, "every declaration should either apply or be deliberately ignored without a message, got: {0}", Join(controller.Messages));

            // A spot check per cause, so a silent no-op cannot pass the two counts above.

            WidgetStyleSheet window = GetClassStyle("window");
            context.AreEqual(StyleLength.Pixels(600), window.Get<StyleLength>(WidgetParameterIndex.Width, StyleLength.Unset), "the window rule's width should have applied");
            context.AreEqual(new StyleLength(StyleUnit.Percent, 0.5f), window.Get<StyleLength>(WidgetParameterIndex.Left, StyleLength.Unset), "left: 50% should stay a percentage");
            context.AreEqual("ui.png#window_9", window.Get<string>(WidgetParameterIndex.BorderImageSource, null), "border-image-source should have applied, and be stored as authored so SaveCSS keeps the fragment");
            context.AreEqualFloat(0.3333f, window.Get<Margin>(WidgetParameterIndex.BorderImageSlice, Margin.Empty).Left, 0.001f, "border-image-slice: 33.33% should have applied");
            context.IsTrue(window.Get<bool>(WidgetParameterIndex.BorderImageFill, false), "the fill keyword on border-image-slice should have been recorded");
            context.AreEqualFloat(0.75f, window.Get<Margin>(WidgetParameterIndex.BorderImageWidth, Margin.Empty).Left, 0.001f, "border-image-width: 75% should have applied");
            context.AreEqual(WidgetBorderImageRepeat.Stretch, window.Get<WidgetBorderImageRepeat>(WidgetParameterIndex.BorderImageRepeat, WidgetBorderImageRepeat.Stretch), "border-image-repeat: stretch should have applied");

            WidgetStyleSheet pattern = GetClassStyle("back_pattern");
            context.AreEqual(WidgetBackgroundStyle.ImageTiled, pattern.Get<WidgetBackgroundStyle>(WidgetParameterIndex.BackStyle, WidgetBackgroundStyle.None), "background-repeat: repeat should tile");

            WidgetStyleSheet checkbox = GetClassStyle("checkbox");
            context.AreEqual(WidgetBackgroundStyle.ImageFit, checkbox.Get<WidgetBackgroundStyle>(WidgetParameterIndex.BackStyle, WidgetBackgroundStyle.None), "background-size: contain should aspect-fit");
            context.AreEqual(new Vector2(0.5f, 0.5f), checkbox.Get<Vector2>(WidgetParameterIndex.BackPivot, Vector2.Zero), "background-position: center should be the centre pivot");

            WidgetStyleSheet textedit = GetClassStyle("textedit");
            context.AreEqual(new Margin(2, 1, 2, 3), textedit.Get<Margin>(WidgetParameterIndex.ClipMargin, Margin.Empty), "clip-path: inset() should become the clip margin");

            WidgetStyleSheet hidden = GetClassStyle("hidden");
            context.AreEqual(WidgetDisplay.None, hidden.Get<WidgetDisplay>(WidgetParameterIndex.Display, WidgetDisplay.Block), "display: none should have applied");

            // The eight `position: absolute` declarations must not have written left/top --
            // that is what the obsolete Vector2 shorthand used to do with two numbers.
            WidgetStyleSheet label = GetClassStyle("label");
            context.AreEqual(StyleLength.Unset, label.Get<StyleLength>(WidgetParameterIndex.Left, StyleLength.Unset), "position: absolute must not write a left offset");

            Font uiFont = WidgetManager.GetFont("uifont");
            context.IsNotNull(uiFont, "@font-face should have registered a font called uifont");

            WidgetStyleSheet body = WidgetManager.GetStyle(new StyleSelector("body", null, null));
            context.AreEqual(uiFont, body.Get<Font>(WidgetParameterIndex.Font, null), "font-family: \"uifont\", monospace should resolve to the registered font");
        }

        private static void Test53_PositionKeyword(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();
            controller.ClearLog();

            context.DoesNotThrow(delegate
            {
                TestEnvironment.LoadCss(".c53abs { position: absolute; width: 42px; }");
            }, "position: absolute should not throw");

            context.AreEqual(0, controller.Messages.Count, "position: absolute is the whole profile of D134 and should be accepted silently, got: {0}", Join(controller.Messages));
            context.AreEqual(0, controller.Errors.Count, "position: absolute should log no error, got: {0}", Join(controller.Errors));

            WidgetStyleSheet absolute = GetClassStyle("c53abs");
            context.AreEqual(StyleLength.Unset, absolute.Get<StyleLength>(WidgetParameterIndex.Left, StyleLength.Unset), "position must no longer be a Vector2 shorthand writing left");
            context.AreEqual(StyleLength.Unset, absolute.Get<StyleLength>(WidgetParameterIndex.Top, StyleLength.Unset), "position must no longer be a Vector2 shorthand writing top");
            context.AreEqual(StyleLength.Pixels(42), absolute.Get<StyleLength>(WidgetParameterIndex.Width, StyleLength.Unset), "the rest of the rule should still apply");

            controller.ClearLog();

            context.DoesNotThrow(delegate
            {
                TestEnvironment.LoadCss(".c53rel { position: relative; }");
            }, "a positioning scheme this engine has no concept of should be reported, not thrown");

            context.IsTrue(HasEntryMentioning(controller.Messages, "position"), "position: relative is outside the D134 profile and should be reported once, got: {0}", Join(controller.Messages));

            controller.ClearLog();

            context.DoesNotThrow(delegate
            {
                TestEnvironment.LoadCss(".c53static { position: static; } .c53fixed { position: fixed; }");
            }, "position: static and position: fixed should not throw either");
        }

        private static void Test54_BackgroundPosition(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();
            controller.ClearLog();

            context.DoesNotThrow(delegate
            {
                TestEnvironment.LoadCss(
                    ".c54center { background-position: center; }" +
                    ".c54corner { background-position: left top; }" +
                    ".c54swap { background-position: bottom right; }" +
                    ".c54pct { background-position: 25% 75%; }" +
                    ".c54sprite { background-position: -804px -225px; }");
            }, "the keyword, percentage and pixel forms of background-position should all parse");

            context.AreEqual(0, controller.Errors.Count, "background-position should log no error, got: {0}", Join(controller.Errors));

            context.AreEqual(new Vector2(0.5f, 0.5f), GetClassStyle("c54center").Get<Vector2>(WidgetParameterIndex.BackPivot, Vector2.Zero), "a single 'center' should fill both axes");
            context.AreEqual(new Vector2(0.0f, 0.0f), GetClassStyle("c54corner").Get<Vector2>(WidgetParameterIndex.BackPivot, Vector2.One), "'left top' should be the top left corner");
            context.AreEqual(new Vector2(1.0f, 1.0f), GetClassStyle("c54swap").Get<Vector2>(WidgetParameterIndex.BackPivot, Vector2.Zero), "'bottom right' names the axes in the other order and should still be the bottom right corner");
            context.AreEqual(new Vector2(0.25f, 0.75f), GetClassStyle("c54pct").Get<Vector2>(WidgetParameterIndex.BackPivot, Vector2.Zero), "a percentage pair should stay a 0..1 fraction pair");

            // D133's sprite-sheet idiom. Today this lands as a pivot of minus 804 and minus 225
            // *fractions* with no complaint, which is the worst failure in the file.
            WidgetStyleSheet sprite = GetClassStyle("c54sprite");

            context.AreEqual(Vector2.Zero, sprite.Get<Vector2>(WidgetParameterIndex.BackPivot, Vector2.Zero), "a pixel offset must not be mistaken for a pivot fraction");
            context.AreEqual(new Vector2(-804, -225), sprite.Get<Vector2>(WidgetParameterIndex.BackOffset, Vector2.Zero), "a pixel offset should be stored in pixels, on its own property");
        }

        private static void Test55_BackgroundSize(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();
            controller.ClearLog();

            context.DoesNotThrow(delegate
            {
                TestEnvironment.LoadCss(
                    ".c55contain { background-size: contain; }" +
                    ".c55cover { background-size: cover; }" +
                    ".c55stretch { background-size: 100% 100%; }" +
                    ".c55scale { background-size: 75%; }" +
                    ".c55auto { background-size: auto; }");
            }, "the contain, cover and two-value forms of background-size should all parse");

            context.AreEqual(0, controller.Errors.Count, "background-size should log no error, got: {0}", Join(controller.Errors));

            context.AreEqual(WidgetBackgroundStyle.ImageFit, GetClassStyle("c55contain").Get<WidgetBackgroundStyle>(WidgetParameterIndex.BackStyle, WidgetBackgroundStyle.None), "contain is aspect fit");
            context.AreEqual(WidgetBackgroundStyle.ImageFill, GetClassStyle("c55cover").Get<WidgetBackgroundStyle>(WidgetParameterIndex.BackStyle, WidgetBackgroundStyle.None), "cover is aspect fill");
            context.AreEqual(WidgetBackgroundStyle.ImageStretch, GetClassStyle("c55stretch").Get<WidgetBackgroundStyle>(WidgetParameterIndex.BackStyle, WidgetBackgroundStyle.None), "100% 100% is a stretch");

            // The single-percentage form is what both shipped games use, 92 times between them,
            // and it must keep landing on the scale factor untouched.
            WidgetStyleSheet scale = GetClassStyle("c55scale");
            context.AreEqualFloat(0.75f, scale.Get<float>(WidgetParameterIndex.BackScale, -1f), 0.0001f, "a single percentage should stay the background scale factor");
            context.AreEqual(WidgetBackgroundStyle.None, scale.Get<WidgetBackgroundStyle>(WidgetParameterIndex.BackStyle, WidgetBackgroundStyle.None), "a single percentage should not choose a background style");
        }

        private static void Test56_BackgroundRepeat(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();
            controller.ClearLog();

            context.DoesNotThrow(delegate
            {
                TestEnvironment.LoadCss(
                    ".c56repeat { background-repeat: repeat; }" +
                    ".c56repeatx { background-repeat: repeat-x; }" +
                    ".c56norepeat { background-repeat: no-repeat; }" +
                    ".c56nine { background-repeat: nineimage; }");
            }, "the CSS keywords of background-repeat should parse alongside the engine's own names");

            context.AreEqual(0, controller.Errors.Count, "background-repeat should log no error, got: {0}", Join(controller.Errors));

            context.AreEqual(WidgetBackgroundStyle.ImageTiled, GetClassStyle("c56repeat").Get<WidgetBackgroundStyle>(WidgetParameterIndex.BackStyle, WidgetBackgroundStyle.None), "repeat should tile");
            context.AreEqual(WidgetBackgroundStyle.ImageTiled, GetClassStyle("c56repeatx").Get<WidgetBackgroundStyle>(WidgetParameterIndex.BackStyle, WidgetBackgroundStyle.None), "repeat-x should tile too, this engine has no single-axis tiling");
            context.AreEqual(WidgetBackgroundStyle.No_Repeat, GetClassStyle("c56norepeat").Get<WidgetBackgroundStyle>(WidgetParameterIndex.BackStyle, WidgetBackgroundStyle.Image), "no-repeat should keep the meaning both shipped games rely on");
            context.AreEqual(WidgetBackgroundStyle.NineImage, GetClassStyle("c56nine").Get<WidgetBackgroundStyle>(WidgetParameterIndex.BackStyle, WidgetBackgroundStyle.None), "the legacy nineimage alias must keep working, the golden master guards it");
        }

        private static void Test57_BorderImage(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();
            controller.ClearLog();

            context.DoesNotThrow(delegate
            {
                TestEnvironment.LoadCss(
                    ".c57nine { border-image-source: url(\"ui.png\"); border-image-slice: 33.33% fill; border-image-width: 75%; border-image-repeat: stretch; }" +
                    ".c57three { border-image-slice: 0 33.33%; }" +
                    ".c57none { border-image-source: none; }");
            }, "the border-image longhands should parse");

            context.AreEqual(0, controller.Errors.Count, "border-image should log no error, got: {0}", Join(controller.Errors));
            context.AreEqual(0, controller.Messages.Count, "border-image is the standard's own nine-patch vocabulary and should not be reported as unknown, got: {0}", Join(controller.Messages));

            WidgetStyleSheet nine = GetClassStyle("c57nine");

            context.AreEqual("ui.png", nine.Get<string>(WidgetParameterIndex.BorderImageSource, null), "border-image-source should unwrap the url()");

            Margin slice = nine.Get<Margin>(WidgetParameterIndex.BorderImageSlice, Margin.Empty);
            context.AreEqualFloat(0.3333f, slice.Left, 0.001f, "a single percentage slice should fill all four sides");
            context.AreEqualFloat(0.3333f, slice.Top, 0.001f, "a single percentage slice should fill all four sides");
            context.IsTrue(nine.Get<bool>(WidgetParameterIndex.BorderImageFill, false), "the fill keyword should be recorded");

            context.AreEqualFloat(0.75f, nine.Get<Margin>(WidgetParameterIndex.BorderImageWidth, Margin.Empty).Top, 0.001f, "border-image-width should be a percentage box");
            context.AreEqual(WidgetBorderImageRepeat.Stretch, nine.Get<WidgetBorderImageRepeat>(WidgetParameterIndex.BorderImageRepeat, WidgetBorderImageRepeat.Repeat), "border-image-repeat: stretch");

            WidgetStyleSheet three = GetClassStyle("c57three");
            Margin threeSlice = three.Get<Margin>(WidgetParameterIndex.BorderImageSlice, new Margin(-1, -1, -1, -1));
            context.AreEqualFloat(0.0f, threeSlice.Top, 0.001f, "a two-value slice is vertical then horizontal, so the top is zero");
            context.AreEqualFloat(0.3333f, threeSlice.Left, 0.001f, "a two-value slice is vertical then horizontal, so the sides carry the third");
            context.IsFalse(three.Get<bool>(WidgetParameterIndex.BorderImageFill, false), "no fill keyword means no fill");

            context.AreEqual("none", GetClassStyle("c57none").Get<string>(WidgetParameterIndex.BorderImageSource, null), "border-image-source: none should be kept, not dropped");
        }

        private static void Test58_FontFace(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();
            controller.RegisterTestFont("c58font.png", 8, 16);
            controller.ClearLog();

            context.DoesNotThrow(delegate
            {
                TestEnvironment.LoadCss(
                    "@font-face { font-family: \"c58font\"; src: url(\"c58font.png\"); --font-baseline: 30; }" +
                    ".c58stack { font-family: \"c58font\", monospace; }" +
                    ".c58inherit { font-family: inherit; }");
            }, "@font-face should register a font rather than throw or be dropped");

            context.AreEqual(0, controller.Errors.Count, "a resolvable font stack should log no error, got: {0}", Join(controller.Errors));

            Font registered = WidgetManager.GetFont("c58font");
            context.IsNotNull(registered, "@font-face should register the family it names");

            context.AreEqual(registered, GetClassStyle("c58stack").Get<Font>(WidgetParameterIndex.Font, null), "a quoted name followed by a fallback should resolve to the registered font");

            // inherit is the CSS-wide keyword. font-family already inherits, so the right answer
            // is to store nothing and let the cascade do it -- not to look up a font called
            // "inherit" and log an error when there is none.
            context.IsNull(GetClassStyle("c58inherit").Get<Font>(WidgetParameterIndex.Font, null), "font-family: inherit should store nothing so the cascade inherits");

            controller.ClearLog();

            context.DoesNotThrow(delegate
            {
                TestEnvironment.LoadCss(".c58missing { font-family: \"nosuchfont\"; }");
            }, "an unresolvable font stack should be reported, not thrown");

            context.IsTrue(HasEntryMentioning(controller.Errors, "nosuchfont"), "an unresolvable font stack should name the family it could not find, got: {0}", Join(controller.Errors));
        }

        private static void Test59_IgnoredAndMapped(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();
            controller.ClearLog();

            context.DoesNotThrow(delegate
            {
                TestEnvironment.LoadCss(".c59 { box-sizing: border-box; white-space: pre-wrap; border: none; clip-path: inset(1px 2px 3px 4px); display: none; }");
            }, "the properties this engine has no concept of should be accepted and ignored, not thrown");

            context.AreEqual(0, controller.Errors.Count, "an ignored property should log no error, got: {0}", Join(controller.Errors));
            context.AreEqual(0, controller.Messages.Count, "the value each of these properties is written with is the one this engine already behaves as, so nothing should be reported, got: {0}", Join(controller.Messages));

            WidgetStyleSheet style = GetClassStyle("c59");

            // clip-path: inset() is the CSS spelling of this engine's --clip-margin, which is
            // what the textedit rule in login.css was written with before it was migrated.
            context.AreEqual(new Margin(4, 1, 2, 3), style.Get<Margin>(WidgetParameterIndex.ClipMargin, Margin.Empty), "clip-path: inset(top right bottom left) should become the clip margin");
            context.AreEqual(WidgetDisplay.None, style.Get<WidgetDisplay>(WidgetParameterIndex.Display, WidgetDisplay.Block), "display: none should be stored");

            controller.ClearLog();

            context.DoesNotThrow(delegate
            {
                TestEnvironment.LoadCss(".c59other { box-sizing: content-box; white-space: nowrap; border: 1px solid red; }");
            }, "a value this engine cannot honour should still not throw");

            context.IsTrue(HasEntryMentioning(controller.Messages, "box-sizing"), "box-sizing: content-box is not what this engine does and should be reported, got: {0}", Join(controller.Messages));
            context.IsTrue(HasEntryMentioning(controller.Messages, "white-space"), "white-space: nowrap is not what this engine does and should be reported, got: {0}", Join(controller.Messages));
            context.IsTrue(HasEntryMentioning(controller.Messages, "border"), "a real border is not something this engine draws and should be reported, got: {0}", Join(controller.Messages));
        }

        /// <summary>
        /// The rendering half of display, which this task does not implement -- see the
        /// registration reason.
        /// </summary>
        private static void Test59b_DisplayNoneHidesWidget(TestContext context)
        {
            TestEnvironment.Setup();

            TestEnvironment.LoadCss(".c59bhidden { display: none; }");

            WidgetPanel panel = new WidgetPanel(WidgetManager.GetStyle(".c59bhidden"));
            panel.ForceUpdateStyle();

            context.IsFalse(panel.Visible, "a widget carrying display: none should not be visible");
        }

        /// <summary>
        /// A colour's alpha and this engine's <c>background-color-opacity</c> are the same
        /// quantity written two ways, so the standard spelling has to arrive where the private
        /// one does. The renderer is the referee: <see cref="WidgetBackground"/>'s Update
        /// hands <c>BackgroundColor</c> to <c>Sprite.Color</c>, whose setter masks the value
        /// with 0x00ffffff and keeps the alpha byte it already had, and hands
        /// <c>BackgroundAlpha</c> to <c>Sprite.Alpha</c> separately. So an alpha left in the
        /// colour is an alpha thrown away, and the byte asserted below is the whole of what a
        /// background's strength does on screen.
        ///
        /// The last two cases are the ones neither shipped game could catch: the default
        /// colour 0xffffff and every plain #rrggbb have a top byte of zero, so reading that
        /// byte as an alpha without care would make every widget in both games invisible.
        /// </summary>
        private static void Test73_BackgroundColorAlpha(TestContext context)
        {
            TestController controller = PrepareIsolatedLoad();

            context.DoesNotThrow(delegate
            {
                TestEnvironment.LoadCss(
                    ".c73pair { background-color: #000000; background-color-opacity: 4%; }" +
                    ".c73rgba { background-color: rgba(0, 0, 0, 0.04); }" +
                    ".c73hex8 { background-color: #0000000a; }" +
                    ".c73both { background-color: rgba(0, 0, 0, 0.5); background-color-opacity: 50%; }" +
                    ".c73bothswapped { background-color-opacity: 50%; background-color: rgba(0, 0, 0, 0.5); }" +
                    ".c73plain { background-color: #000000; }" +
                    ".c73none { --clip: true; }");
            }, "an alpha-bearing background-color should be read, not rejected");

            context.AreEqual(0, controller.Errors.Count, "none of these rules should log an error, got: {0}", Join(controller.Errors));

            WidgetPanel pair = StyledPanel(".c73pair");
            WidgetPanel rgba = StyledPanel(".c73rgba");

            // the pair is what both shipped skins write; rgba() is what a browser reads
            context.AreEqual(pair.BackgroundColor, rgba.BackgroundColor, "rgba() should store the same colour the #rrggbb form does");
            context.AreEqual(RenderedAlpha(pair), RenderedAlpha(rgba), "rgba(0, 0, 0, 0.04) should paint the same background as #000000 plus background-color-opacity: 4%");
            context.AreEqual(10, RenderedAlpha(rgba), "4% of 255 is 10");

            context.AreEqual(RenderedAlpha(pair), RenderedAlpha(StyledPanel(".c73hex8")), "the eight digit hex form carries the same alpha as rgba()");

            // composition, not precedence: the renderer already multiplies every alpha it
            // holds, so two declared alphas multiply too, whichever order they are written in
            context.AreEqualFloat(0.25f, StyledPanel(".c73both").BackgroundAlpha, 0.005f, "a colour alpha of 0.5 and an opacity of 50% should compose to a quarter");
            context.AreEqualFloat(0.25f, StyledPanel(".c73bothswapped").BackgroundAlpha, 0.005f, "the same two declarations in the other order should compose to the same quarter");

            // the two cases the corpus cannot catch, because a top byte of zero is what every
            // colour in both games and the un-declared default alike look like
            WidgetPanel plain = StyledPanel(".c73plain");
            context.AreEqualFloat(1.0f, plain.BackgroundAlpha, 0.001f, "a background-color with no alpha must leave the background at full strength");
            context.AreEqual(255, RenderedAlpha(plain), "a background-color with no alpha must still paint opaque");

            WidgetPanel none = StyledPanel(".c73none");
            context.AreEqual((uint)0xffffff, none.BackgroundColor, "a widget declaring no background-color keeps the default colour");
            context.AreEqual(255, RenderedAlpha(none), "a widget declaring no background-color must still paint opaque");
        }

        /// <summary>
        /// The standard <c>@font-face</c> and this engine's <c>@font.&lt;name&gt;</c> are two
        /// spellings of one registration, so they must produce the same font from the same
        /// metrics -- including the "default" family, which is the one the sample's stylesheet
        /// needs a browser to be able to read.
        /// </summary>
        private static void Test74_FontFaceMatchesFontAtRule(TestContext context)
        {
            TestController controller = PrepareIsolatedLoad();
            controller.RegisterTestFont("c74sprite", 8, 16);
            controller.ClearLog();

            context.DoesNotThrow(delegate
            {
                TestEnvironment.LoadCss(
                    "@font.c74legacy { --font-resource: url(\"c74sprite\"); --font-spacing: 2; --font-leading: 3; --font-baseline: 30; --font-shift: 1; }" +
                    "@font-face { font-family: \"c74standard\"; src: url(\"c74sprite\"); letter-spacing: 2; --font-leading: 3; --font-baseline: 30; --font-shift: 1; }" +
                    "@font-face { font-family: \"default\"; src: url(\"c74sprite\"); --font-baseline: 30; }");
            }, "both at-rules should register a font rather than throw");

            context.AreEqual(0, controller.Errors.Count, "neither at-rule should log an error, got: {0}", Join(controller.Errors));

            Font legacy = WidgetManager.GetFont("c74legacy");
            Font standard = WidgetManager.GetFont("c74standard");

            context.IsNotNull(legacy, "@font.c74legacy should register a font");
            context.IsNotNull(standard, "@font-face naming c74standard should register a font");

            if (legacy == null || standard == null)
                return;

            context.AreEqualFloat(legacy.Spacing, standard.Spacing, 0.001f, "letter-spacing should reach the same field --font-spacing does");
            context.AreEqual(legacy.Leading, standard.Leading, "both spellings should carry the same leading");
            context.AreEqual(legacy.Baseline, standard.Baseline, "both spellings should carry the same baseline");
            context.AreEqual(legacy.Shift, standard.Shift, "both spellings should carry the same shift");
            context.AreEqual(legacy.Height, standard.Height, "both spellings should cut the same sprite, so the glyph height matches");
            context.AreEqual(legacy.SpaceWidth, standard.SpaceWidth, "both spellings should cut the same sprite, so the space width matches");

            // the sample's own case: @font.default is what sets the main font today, and the
            // standard spelling has to set it too or a stylesheet a browser can read has no text
            context.IsNotNull(WidgetManager.MainFont, "@font-face for the default family should set the main font");
            context.AreEqual(WidgetManager.MainFont, WidgetManager.GetFont("default"), "the default family should be reachable by name as well");
        }

        // The byte WidgetBackground.Update hands to Sprite.Alpha, which is the only route by
        // which a background's strength reaches the screen. Copied from there deliberately:
        // if that expression changes, this test should be re-read rather than silently follow.
        private static int RenderedAlpha(WidgetPanel panel)
        {
            return MathHelper.Clamp((int)(panel.OpacityValue * panel.BackgroundAlpha * 255 + float.Epsilon), 0, 255);
        }

        private static WidgetPanel StyledPanel(string selector)
        {
            WidgetPanel panel = new WidgetPanel(WidgetManager.GetStyle(selector));
            panel.ForceUpdateStyle();

            return panel;
        }

        // Resets the process-wide style collection and font registry, and makes sure the
        // resource login.css names is a usable test font sheet.
        private static TestController PrepareIsolatedLoad()
        {
            TestController controller = TestEnvironment.Setup();

            controller.RegisterTestFont(LoginFontResource, 8, 16);

            WidgetManager.ResetStyles();

            controller.ClearLog();

            return controller;
        }

        private static WidgetStyleSheet GetClassStyle(string className)
        {
            return WidgetManager.GetStyle(new StyleSelector(null, new string[] { className }, null));
        }

        private static bool HasEntryMentioning(IList<string> entries, string needle)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

            return false;
        }

        private static string Join(IList<string> entries)
        {
            if (entries.Count == 0)
                return "(nothing)";

            string[] copy = new string[entries.Count];
            entries.CopyTo(copy, 0);

            return string.Join(" | ", copy);
        }
    }
}
