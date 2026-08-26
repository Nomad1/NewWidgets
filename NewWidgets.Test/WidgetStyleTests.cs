using System;
using System.Numerics;
using NewWidgets.UI;
using NewWidgets.Utility;
using NewWidgets.Widgets;

namespace NewWidgets.Test
{
    /// <summary>
    /// Test groups covering how NewWidgets turns CSS declarations into widget geometry:
    /// absolute sizes, percentages, auto/min/max sizing, positioning, restyling and text
    /// layout. Every class and id used here is prefixed "sty&lt;letter&gt;" so it cannot
    /// collide with styles loaded by any other test file, since WidgetManager's style
    /// collection is a shared, never-cleared, process-wide static field.
    /// </summary>
    internal static class WidgetStyleTests
    {
        private const float Tolerance = 0.01f;

        public static void Register()
        {
            TestRunner.Add("Test 30: absolute width and height", Test30_AbsoluteWidthAndHeight);

            TestRunner.Add("Test 31: percentage sizes", Test31_PercentageSizes);

            TestRunner.AddKnownFailure("Test 31b: a percentage-sized child follows a parent resize",
                "Widget.Resize (Widgets/Widget.cs) invalidates layout on the widget it was called on and never " +
                "touches that widget's descendants, so a percentage stays resolved against the containing block size " +
                "the child last saw; only an explicit InvalidateLayout on the child itself makes it re-resolve. " +
                "Propagating the invalidation down the tree costs a full cascade rebuild per child, which D144's " +
                "performance constraint argues against, so this is an open design decision for the owner and not a " +
                "defect to be patched here",
                Test31b_PercentageFollowsParentResize);

            TestRunner.Add("Test 32: auto, min and max sizing", Test32_AutoMinMaxSizing);

            TestRunner.Add("Test 33: left and top positioning", Test33_LeftTopPositioning);

            TestRunner.Add("Test 34: anchoring with right and bottom", Test34_AnchoringRightBottom);

            TestRunner.Add("Test 35: restyling after first layout", Test35_RestylingAfterFirstLayout);

            TestRunner.Add("Test 36: text alignment", Test36_TextAlignment);

            TestRunner.AddKnownFailure("Test 37: text-align must not move text vertically",
                "WidgetAlign (Widgets/Enums.cs) defines Center as Left | Right | Top | Bottom, so " +
                "ConversionHelper.EnumParse turns CSS 'text-align: center' into a value that also carries the Top/Bottom " +
                "bits; WidgetLabel.UpdateLayout (Widgets/Controls/WidgetLabel.cs) reads those same bits to compute both the " +
                "horizontal AND vertical offset from the single TextAlign property, so text-align: center vertically " +
                "centres the text as a side effect. There is also no separate vertical-align property in " +
                "WidgetParameterIndex.cs, so the vertical axis cannot be controlled independently",
                Test37_TextAlignVertical);

            TestRunner.Add("Test 38: overflow and clipping", Test38_OverflowAndClipping);

            TestRunner.Add("Test 39: background properties", Test39_BackgroundProperties);
        }

        // ----------------------------------------------------------------
        // Group A: Test 30 -- absolute width and height
        // ----------------------------------------------------------------

        private static void Test30_AbsoluteWidthAndHeight(TestContext context)
        {
            TestEnvironment.Setup();

            TestEnvironment.LoadCss(
                ".stya1px { width: 120px; height: 40px; }" +
                ".stya2pt { width: 30pt; height: 96pt; }");

            WidgetPanel root = new WidgetPanel();
            root.Size = new Vector2(500.0f, 500.0f);

            WidgetPanel pxPanel = new WidgetPanel(WidgetManager.GetStyle("stya1px"));
            root.AddChild(pxPanel);
            pxPanel.Relayout();

            context.AreEqualFloat(120.0f, pxPanel.Size.X, Tolerance, "width: 120px should resolve to Size.X == 120, got {0}", pxPanel.Size.X);
            context.AreEqualFloat(40.0f, pxPanel.Size.Y, Tolerance, "height: 40px should resolve to Size.Y == 40, got {0}", pxPanel.Size.Y);

            WidgetPanel ptPanel = new WidgetPanel(WidgetManager.GetStyle("stya2pt"));
            root.AddChild(ptPanel);
            ptPanel.Relayout();

            float expectedPtWidth = 30.0f * 96.0f / 72.0f;
            float expectedPtHeight = 96.0f * 96.0f / 72.0f;

            context.AreEqualFloat(expectedPtWidth, ptPanel.Size.X, Tolerance, "width: 30pt should convert at 96/72 to {0}px, got {1}", expectedPtWidth, ptPanel.Size.X);
            context.AreEqualFloat(expectedPtHeight, ptPanel.Size.Y, Tolerance, "height: 96pt should convert at 96/72 to {0}px, got {1}", expectedPtHeight, ptPanel.Size.Y);

            WidgetPanel noRulePanel = new WidgetPanel(WidgetManager.GetStyle("stya3norule"));
            root.AddChild(noRulePanel);
            noRulePanel.Size = new Vector2(77.0f, 55.0f);
            noRulePanel.Relayout();

            context.AreEqualFloat(77.0f, noRulePanel.Size.X, Tolerance, "a widget with no width rule should keep the Size.X it was given in code, got {0}", noRulePanel.Size.X);
            context.AreEqualFloat(55.0f, noRulePanel.Size.Y, Tolerance, "a widget with no height rule should keep the Size.Y it was given in code, got {0}", noRulePanel.Size.Y);

            noRulePanel.StyleClasses = noRulePanel.StyleClasses; // public way to force a restyle, mirroring a style-affecting change
            noRulePanel.Relayout();

            context.AreEqualFloat(77.0f, noRulePanel.Size.X, Tolerance, "Size set in code before the first relayout should survive a second relayout with no competing rule, got {0}", noRulePanel.Size.X);
            context.AreEqualFloat(55.0f, noRulePanel.Size.Y, Tolerance, "Size set in code before the first relayout should survive a second relayout with no competing rule, got {0}", noRulePanel.Size.Y);
        }

        // ----------------------------------------------------------------
        // Group B: Test 31 -- percentage sizes
        // ----------------------------------------------------------------

        private static void Test31_PercentageSizes(TestContext context)
        {
            TestEnvironment.Setup();

            TestEnvironment.LoadCss(
                ".styb1half { width: 50%; }" +
                ".styb2quarter { height: 25%; }" +
                ".styb3full { width: 100%; }");

            WidgetPanel root = new WidgetPanel();
            root.Size = new Vector2(400.0f, 200.0f);

            WidgetPanel halfWidth = new WidgetPanel(WidgetManager.GetStyle("styb1half"));
            root.AddChild(halfWidth);
            halfWidth.Relayout();

            context.AreEqualFloat(200.0f, halfWidth.Size.X, Tolerance, "width: 50% inside a 400px-wide parent should resolve to 200, got {0}", halfWidth.Size.X);

            WidgetPanel quarterHeight = new WidgetPanel(WidgetManager.GetStyle("styb2quarter"));
            root.AddChild(quarterHeight);
            quarterHeight.Relayout();

            context.AreEqualFloat(50.0f, quarterHeight.Size.Y, Tolerance, "height: 25% inside a 200px-tall parent should resolve to 50, got {0}", quarterHeight.Size.Y);

            WidgetPanel fullWidth = new WidgetPanel(WidgetManager.GetStyle("styb3full"));
            root.AddChild(fullWidth);
            fullWidth.Relayout();

            context.AreEqualFloat(400.0f, fullWidth.Size.X, Tolerance, "width: 100% should exactly fill the 400px-wide parent, got {0}", fullWidth.Size.X);
        }

        // ----------------------------------------------------------------
        // Group B2: Test 31b -- a percentage-sized child follows a parent resize (KNOWN FAILURE)
        // ----------------------------------------------------------------

        private static void Test31b_PercentageFollowsParentResize(TestContext context)
        {
            TestEnvironment.Setup();

            TestEnvironment.LoadCss(".styb5reflow { width: 50%; }");

            WidgetPanel root = new WidgetPanel();
            root.Size = new Vector2(400.0f, 200.0f);

            WidgetPanel reflowing = new WidgetPanel(WidgetManager.GetStyle("styb5reflow"));
            root.AddChild(reflowing);
            reflowing.Relayout();

            context.AreEqualFloat(200.0f, reflowing.Size.X, Tolerance, "width: 50% should resolve to 200 against the 400px-wide parent before it is resized, got {0}", reflowing.Size.X);

            // No forced restyle and no InvalidateLayout on the child: resizing the parent and
            // running an ordinary frame is exactly what a game or an editor does, and it is the
            // behaviour the assertion below is written to.
            root.Size = new Vector2(800.0f, 200.0f);
            root.Update();

            context.AreEqualFloat(400.0f, reflowing.Size.X, Tolerance,
                "resizing the parent to 800px and running one normal update frame should re-resolve width: 50% to 400, " +
                "got {0}; a width still reading 200 means the child was never marked for layout, so its percentage " +
                "stays resolved against the parent's old size until something else happens to invalidate it",
                reflowing.Size.X);
        }

        // ----------------------------------------------------------------
        // Group C: Test 32 -- auto, min and max sizing
        // ----------------------------------------------------------------

        private static void Test32_AutoMinMaxSizing(TestContext context)
        {
            TestEnvironment.Setup();

            context.DoesNotThrow(delegate
            {
                TestEnvironment.LoadCss(".styc1auto { width: auto; }");
            }, "width: auto is a valid CSS 10.3.3 declaration and the stylesheet holding it should load without error");

            TestEnvironment.LoadCss(
                ".styc2minwidth { width: 50px; min-width: 100px; }" +
                ".styc3maxwidth { width: 500px; max-width: 200px; }" +
                ".styc4minheight { height: 50px; min-height: 100px; }" +
                ".styc5maxheight { height: 500px; max-height: 200px; }" +
                ".styc6conflict { width: 500px; min-width: 300px; max-width: 100px; }");

            WidgetPanel root = new WidgetPanel();
            root.Size = new Vector2(300.0f, 300.0f);

            WidgetPanel minWidth = new WidgetPanel(WidgetManager.GetStyle("styc2minwidth"));
            root.AddChild(minWidth);
            minWidth.Relayout();

            context.AreEqualFloat(100.0f, minWidth.Size.X, Tolerance, "min-width: 100px should raise a smaller width: 50px up to 100, got {0}", minWidth.Size.X);

            WidgetPanel maxWidth = new WidgetPanel(WidgetManager.GetStyle("styc3maxwidth"));
            root.AddChild(maxWidth);
            maxWidth.Relayout();

            context.AreEqualFloat(200.0f, maxWidth.Size.X, Tolerance, "max-width: 200px should clamp a larger width: 500px down to 200, got {0}", maxWidth.Size.X);

            WidgetPanel minHeight = new WidgetPanel(WidgetManager.GetStyle("styc4minheight"));
            root.AddChild(minHeight);
            minHeight.Relayout();

            context.AreEqualFloat(100.0f, minHeight.Size.Y, Tolerance, "min-height: 100px should raise a smaller height: 50px up to 100, got {0}", minHeight.Size.Y);

            WidgetPanel maxHeight = new WidgetPanel(WidgetManager.GetStyle("styc5maxheight"));
            root.AddChild(maxHeight);
            maxHeight.Relayout();

            context.AreEqualFloat(200.0f, maxHeight.Size.Y, Tolerance, "max-height: 200px should clamp a larger height: 500px down to 200, got {0}", maxHeight.Size.Y);

            WidgetPanel conflict = new WidgetPanel(WidgetManager.GetStyle("styc6conflict"));
            root.AddChild(conflict);
            conflict.Relayout();

            context.AreEqualFloat(300.0f, conflict.Size.X, Tolerance, "per CSS 2.1 10.4, min-width: 300px must win over a conflicting max-width: 100px, got {0}", conflict.Size.X);
        }

        // ----------------------------------------------------------------
        // Group D: Test 33 -- left and top positioning
        // ----------------------------------------------------------------

        private static void Test33_LeftTopPositioning(TestContext context)
        {
            TestEnvironment.Setup();

            TestEnvironment.LoadCss(
                ".styd1pos { left: 10px; top: 20px; }" +
                ".styd2percent { left: 25%; }" +
                ".styd3zindex { z-index: 42; }");

            WidgetPanel root = new WidgetPanel();
            root.Size = new Vector2(400.0f, 300.0f);

            WidgetPanel positioned = new WidgetPanel(WidgetManager.GetStyle("styd1pos"));
            root.AddChild(positioned);
            positioned.Relayout();

            context.AreEqualFloat(10.0f, positioned.Position.X, Tolerance, "left: 10px should place Position.X at 10, got {0}", positioned.Position.X);
            context.AreEqualFloat(20.0f, positioned.Position.Y, Tolerance, "top: 20px should place Position.Y at 20, got {0}", positioned.Position.Y);

            WidgetPanel percentLeft = new WidgetPanel(WidgetManager.GetStyle("styd2percent"));
            root.AddChild(percentLeft);
            percentLeft.Relayout();

            context.AreEqualFloat(100.0f, percentLeft.Position.X, Tolerance, "left: 25% inside a 400px-wide parent should place Position.X at 100, got {0}", percentLeft.Position.X);

            WidgetPanel zIndexed = new WidgetPanel(WidgetManager.GetStyle("styd3zindex"));
            root.AddChild(zIndexed);
            zIndexed.Relayout();

            context.AreEqual(42, zIndexed.ZIndex, "z-index: 42 from CSS should reach ZIndex, got {0}", zIndexed.ZIndex);
        }

        // ----------------------------------------------------------------
        // Group E: Test 34 -- anchoring with right and bottom
        // ----------------------------------------------------------------

        private static void Test34_AnchoringRightBottom(TestContext context)
        {
            TestEnvironment.Setup();

            TestEnvironment.LoadCss(
                ".stye1left { left: 10px; width: 100px; }" +
                ".stye2right { right: 10px; width: 100px; }" +
                ".stye3stretch { left: 10px; right: 10px; }" +
                ".stye4center { left: 0px; right: 0px; width: 100px; margin-left: auto; margin-right: auto; }" +
                ".stye5top { top: 10px; height: 100px; }" +
                ".stye6bottom { bottom: 10px; height: 100px; }" +
                ".stye7stretch { top: 10px; bottom: 10px; }" +
                ".stye8center { top: 0px; bottom: 0px; height: 100px; margin-top: auto; margin-bottom: auto; }");

            WidgetPanel root = new WidgetPanel();
            root.Size = new Vector2(400.0f, 300.0f);

            WidgetPanel leftAnchored = new WidgetPanel(WidgetManager.GetStyle("stye1left"));
            root.AddChild(leftAnchored);
            leftAnchored.Relayout();
            context.AreEqualFloat(10.0f, leftAnchored.Position.X, Tolerance, "left: 10px; width: 100px should anchor at x == 10, got {0}", leftAnchored.Position.X);

            WidgetPanel rightAnchored = new WidgetPanel(WidgetManager.GetStyle("stye2right"));
            root.AddChild(rightAnchored);
            rightAnchored.Relayout();
            context.AreEqualFloat(290.0f, rightAnchored.Position.X, Tolerance, "right: 10px; width: 100px in a 400px-wide parent should anchor at x == 400 - 10 - 100 == 290, got {0}", rightAnchored.Position.X);

            WidgetPanel stretched = new WidgetPanel(WidgetManager.GetStyle("stye3stretch"));
            root.AddChild(stretched);
            stretched.Relayout();
            context.AreEqualFloat(10.0f, stretched.Position.X, Tolerance, "left: 10px; right: 10px with width auto should anchor at x == 10, got {0}", stretched.Position.X);
            context.AreEqualFloat(380.0f, stretched.Size.X, Tolerance, "left: 10px; right: 10px with width auto should stretch to width == 380, got {0}", stretched.Size.X);

            WidgetPanel centered = new WidgetPanel(WidgetManager.GetStyle("stye4center"));
            root.AddChild(centered);
            centered.Relayout();
            context.AreEqualFloat(150.0f, centered.Position.X, Tolerance, "left: 0; right: 0; width: 100px; margin-left/right: auto should centre at x == 150, got {0}", centered.Position.X);

            WidgetPanel topAnchored = new WidgetPanel(WidgetManager.GetStyle("stye5top"));
            root.AddChild(topAnchored);
            topAnchored.Relayout();
            context.AreEqualFloat(10.0f, topAnchored.Position.Y, Tolerance, "top: 10px; height: 100px should anchor at y == 10, got {0}", topAnchored.Position.Y);

            WidgetPanel bottomAnchored = new WidgetPanel(WidgetManager.GetStyle("stye6bottom"));
            root.AddChild(bottomAnchored);
            bottomAnchored.Relayout();
            context.AreEqualFloat(190.0f, bottomAnchored.Position.Y, Tolerance, "bottom: 10px; height: 100px in a 300px-tall parent should anchor at y == 300 - 10 - 100 == 190, got {0}", bottomAnchored.Position.Y);

            WidgetPanel stretchedVertical = new WidgetPanel(WidgetManager.GetStyle("stye7stretch"));
            root.AddChild(stretchedVertical);
            stretchedVertical.Relayout();
            context.AreEqualFloat(10.0f, stretchedVertical.Position.Y, Tolerance, "top: 10px; bottom: 10px with height auto should anchor at y == 10, got {0}", stretchedVertical.Position.Y);
            context.AreEqualFloat(280.0f, stretchedVertical.Size.Y, Tolerance, "top: 10px; bottom: 10px with height auto should stretch to height == 280, got {0}", stretchedVertical.Size.Y);

            WidgetPanel centeredVertical = new WidgetPanel(WidgetManager.GetStyle("stye8center"));
            root.AddChild(centeredVertical);
            centeredVertical.Relayout();
            context.AreEqualFloat(100.0f, centeredVertical.Position.Y, Tolerance, "top: 0; bottom: 0; height: 100px; margin-top/bottom: auto should centre at y == 100, got {0}", centeredVertical.Position.Y);
        }

        // ----------------------------------------------------------------
        // Group F: Test 35 -- restyling after first layout
        // ----------------------------------------------------------------

        private static void Test35_RestylingAfterFirstLayout(TestContext context)
        {
            TestEnvironment.Setup();

            TestEnvironment.LoadCss(
                ".styf1base { width: 100px; height: 40px; }" +
                ".styf1base.styf1override { width: 250px; }" +
                ".styf2base { width: 80px; height: 30px; }" +
                ".styf2base:hover { width: 220px; }");

            WidgetPanel root = new WidgetPanel();
            root.Size = new Vector2(500.0f, 500.0f);

            // Restyle via an added, higher-specificity class
            WidgetPanel classPanel = new WidgetPanel(WidgetManager.GetStyle("styf1base"));
            root.AddChild(classPanel);
            classPanel.Relayout();

            context.AreEqualFloat(100.0f, classPanel.Size.X, Tolerance, "the first layout should pick up width: 100px from .styf1base, got {0}", classPanel.Size.X);

            // Read the own style alone, not GetProperty: GetProperty walks the whole cascade,
            // where .styf1base legitimately declares a width, so it cannot tell a stale own-style
            // entry from the class rule the widget is supposed to be resolving.
            StyleLength ownStyleWidthAfterFirstLayout = classPanel.GetOwnProperty(WidgetParameterIndex.Width, StyleLength.Unset);
            context.IsTrue(ownStyleWidthAfterFirstLayout.IsUnset,
                "resolving a class rule should leave the widget's own style carrying no width at all, so the cascade " +
                "stays free to answer the next lookup; found {0} in the own style instead, which means the resolved " +
                "size was written back there, and the own style sits at the head of the cascade and will shadow every " +
                "later rule",
                ownStyleWidthAfterFirstLayout);

            classPanel.AddStyleClass("styf1override");
            classPanel.Relayout();

            context.AreEqualFloat(250.0f, classPanel.Size.X, Tolerance,
                "adding the higher-specificity class .styf1override (width: 250px) and relaying out should re-resolve " +
                "the width to 250, got {0}; a width still reading 100 means the re-resolved cascade result never " +
                "reached Size, because a stale entry left in the widget's own style by an earlier Resize answered the " +
                "lookup first",
                classPanel.Size.X);

            // Restyle via a :hover pseudo-class becoming active
            WidgetPanel hoverPanel = new WidgetPanel(WidgetManager.GetStyle("styf2base"));
            root.AddChild(hoverPanel);
            hoverPanel.Relayout();

            context.AreEqualFloat(80.0f, hoverPanel.Size.X, Tolerance, "the first layout should pick up width: 80px from .styf2base, got {0}", hoverPanel.Size.X);

            hoverPanel.Hovered = true;
            hoverPanel.Relayout();

            context.AreEqualFloat(220.0f, hoverPanel.Size.X, Tolerance,
                "becoming :hover (width: 220px) and relaying out should re-resolve the width to 220, got {0}; a width " +
                "still reading 80 means an own-style entry frozen by the first layout is answering the lookup ahead of " +
                "the :hover rule, which blocks a pseudo-class exactly as it would block an added class and is what an " +
                "interface editor hits constantly",
                hoverPanel.Size.X);
        }

        // ----------------------------------------------------------------
        // Group G: Test 36 -- text alignment
        // ----------------------------------------------------------------

        private static void Test36_TextAlignment(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();

            const int GlyphWidth = 10;
            const int GlyphHeight = 16;

            controller.RegisterTestFont("stygsprite", GlyphWidth, GlyphHeight);

            TestEnvironment.LoadCss(
                "@font.stygfont { --font-resource: url(\"stygsprite\"); --font-spacing: 0; }" +
                ".styg1left { font-family: stygfont; width: 200px; text-align: left; }" +
                ".styg2center { font-family: stygfont; width: 200px; text-align: center; }" +
                ".styg3right { font-family: stygfont; width: 200px; text-align: right; }" +
                ".styg4auto { font-family: stygfont; }");

            WidgetPanel root = new WidgetPanel();
            root.Size = new Vector2(400.0f, 200.0f);

            string text = "ABCD";
            float textWidth = text.Length * GlyphWidth;

            WidgetLabel leftLabel = new WidgetLabel(WidgetManager.GetStyle("styg1left"), text);
            root.AddChild(leftLabel);
            leftLabel.Relayout();

            Vector2 leftPosition = leftLabel.InnerLabelPosition;
            context.AreEqualFloat(0.0f, leftPosition.X, Tolerance, "text-align: left should place the text at x == 0, got {0}", leftPosition.X);

            WidgetLabel centerLabel = new WidgetLabel(WidgetManager.GetStyle("styg2center"), text);
            root.AddChild(centerLabel);
            centerLabel.Relayout();

            Vector2 centerPosition = centerLabel.InnerLabelPosition;
            float expectedCenterX = (200.0f - textWidth) / 2.0f;
            context.AreEqualFloat(expectedCenterX, centerPosition.X, Tolerance, "text-align: center should place the text at x == {0}, got {1}", expectedCenterX, centerPosition.X);

            WidgetLabel rightLabel = new WidgetLabel(WidgetManager.GetStyle("styg3right"), text);
            root.AddChild(rightLabel);
            rightLabel.Relayout();

            Vector2 rightPosition = rightLabel.InnerLabelPosition;
            float expectedRightX = 200.0f - textWidth;
            context.AreEqualFloat(expectedRightX, rightPosition.X, Tolerance, "text-align: right should place the text at x == {0}, got {1}", expectedRightX, rightPosition.X);

            WidgetLabel autoLabel = new WidgetLabel(WidgetManager.GetStyle("styg4auto"), text);
            root.AddChild(autoLabel);
            autoLabel.Relayout();

            context.AreEqualFloat(textWidth, autoLabel.Size.X, Tolerance, "a label with no explicit width should auto-size Size.X to the text width {0}, got {1}", textWidth, autoLabel.Size.X);
            context.AreEqualFloat(GlyphHeight, autoLabel.Size.Y, Tolerance, "a label with no explicit height should auto-size Size.Y to the text height {0}, got {1}", GlyphHeight, autoLabel.Size.Y);
        }

        // ----------------------------------------------------------------
        // Group H: Test 37 -- text-align must not move text vertically (KNOWN FAILURE)
        // ----------------------------------------------------------------

        private static void Test37_TextAlignVertical(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();

            const int GlyphWidth = 10;
            const int GlyphHeight = 16;

            controller.RegisterTestFont("styhsprite", GlyphWidth, GlyphHeight);

            TestEnvironment.LoadCss(
                "@font.styhfont { --font-resource: url(\"styhsprite\"); --font-spacing: 0; }" +
                ".styh1left { font-family: styhfont; width: 200px; height: 100px; text-align: left; }" +
                ".styh2center { font-family: styhfont; width: 200px; height: 100px; text-align: center; }" +
                ".styh3vertical { font-family: styhfont; width: 200px; height: 100px; text-align: left; vertical-align: middle; }");

            WidgetPanel root = new WidgetPanel();
            root.Size = new Vector2(400.0f, 200.0f);

            string text = "AB";
            float textWidth = text.Length * GlyphWidth;

            WidgetLabel leftLabel = new WidgetLabel(WidgetManager.GetStyle("styh1left"), text);
            root.AddChild(leftLabel);
            leftLabel.Relayout();

            Vector2 leftPosition = leftLabel.InnerLabelPosition;
            context.AreEqualFloat(0.0f, leftPosition.Y, Tolerance, "text-align: left should leave the text at y == 0, got {0}", leftPosition.Y);

            WidgetLabel centerLabel = new WidgetLabel(WidgetManager.GetStyle("styh2center"), text);
            root.AddChild(centerLabel);
            centerLabel.Relayout();

            Vector2 centerPosition = centerLabel.InnerLabelPosition;
            float expectedCenterX = (200.0f - textWidth) / 2.0f;
            context.AreEqualFloat(expectedCenterX, centerPosition.X, Tolerance, "text-align: center should still move the text horizontally to x == {0}, got {1}", expectedCenterX, centerPosition.X);
            context.AreEqualFloat(0.0f, centerPosition.Y, Tolerance, "text-align: center should leave the text at the SAME y == 0 as text-align: left, since text-align only controls the horizontal axis, got {0}", centerPosition.Y);

            WidgetLabel verticalLabel = new WidgetLabel(WidgetManager.GetStyle("styh3vertical"), text);
            root.AddChild(verticalLabel);
            verticalLabel.Relayout();

            Vector2 verticalPosition = verticalLabel.InnerLabelPosition;
            float expectedMiddleY = (100.0f - GlyphHeight) / 2.0f;
            context.AreEqualFloat(0.0f, verticalPosition.X, Tolerance, "text-align: left should keep x == 0 even with vertical-align also set, got {0}", verticalPosition.X);
            context.AreEqualFloat(expectedMiddleY, verticalPosition.Y, Tolerance, "a separate vertical-align: middle should move the text to y == {0} while text-align stays horizontal-only, got {1}", expectedMiddleY, verticalPosition.Y);
        }

        // ----------------------------------------------------------------
        // Group I: Test 38 -- overflow and clipping
        // ----------------------------------------------------------------

        private static void Test38_OverflowAndClipping(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();

            TestEnvironment.LoadCss(
                ".styi1hidden { width: 100px; height: 50px; overflow: hidden; }" +
                ".styi2visible { width: 100px; height: 50px; overflow: visible; }" +
                ".styi3margin { width: 100px; height: 50px; overflow: hidden; --clip-margin: 5px 10px 15px 20px; }" +
                ".styi4a { width: 60px; height: 20px; overflow: hidden; }" +
                ".styi4b { width: 90px; height: 45px; overflow: hidden; }");

            WidgetPanel root = new WidgetPanel();
            root.Size = new Vector2(400.0f, 300.0f);

            WidgetPanel hidden = new WidgetPanel(WidgetManager.GetStyle("styi1hidden"));
            root.AddChild(hidden);
            hidden.Relayout();

            int countBeforeHidden = controller.ClipRectCount;
            hidden.Draw();

            context.AreEqual(countBeforeHidden + 1, controller.ClipRectCount, "overflow: hidden should issue exactly one clip rect on Draw");
            context.AreEqual(0, controller.LastClipX, "overflow: hidden clip rect should start at x == 0, got {0}", controller.LastClipX);
            context.AreEqual(0, controller.LastClipY, "overflow: hidden clip rect should start at y == 0, got {0}", controller.LastClipY);
            context.AreEqual(100, controller.LastClipWidth, "overflow: hidden clip rect width should match the widget's box, got {0}", controller.LastClipWidth);
            context.AreEqual(50, controller.LastClipHeight, "overflow: hidden clip rect height should match the widget's box, got {0}", controller.LastClipHeight);

            WidgetPanel visible = new WidgetPanel(WidgetManager.GetStyle("styi2visible"));
            root.AddChild(visible);
            visible.Relayout();

            int countBeforeVisible = controller.ClipRectCount;
            visible.Draw();

            context.AreEqual(countBeforeVisible, controller.ClipRectCount, "overflow: visible should not issue a clip rect on Draw");

            WidgetPanel margined = new WidgetPanel(WidgetManager.GetStyle("styi3margin"));
            root.AddChild(margined);
            margined.Relayout();
            margined.Draw();

            context.AreEqual(20, controller.LastClipX, "--clip-margin: 5px 10px 15px 20px is top right bottom left, so the left edge shrinks by 20, got {0}", controller.LastClipX);
            context.AreEqual(5, controller.LastClipY, "--clip-margin: 5px 10px 15px 20px is top right bottom left, so the top edge shrinks by 5, got {0}", controller.LastClipY);
            context.AreEqual(70, controller.LastClipWidth, "--clip-margin should shrink the clip rect's width to 100 - 20 - 10 == 70, got {0}", controller.LastClipWidth);
            context.AreEqual(30, controller.LastClipHeight, "--clip-margin should shrink the clip rect's height to 50 - 5 - 15 == 30, got {0}", controller.LastClipHeight);

            WidgetPanel sequentialA = new WidgetPanel(WidgetManager.GetStyle("styi4a"));
            root.AddChild(sequentialA);
            sequentialA.Relayout();

            WidgetPanel sequentialB = new WidgetPanel(WidgetManager.GetStyle("styi4b"));
            root.AddChild(sequentialB);
            sequentialB.Relayout();

            int countBeforeSequence = controller.ClipRectCount;
            sequentialA.Draw();

            context.AreEqual(60, controller.LastClipWidth, "the clip rect after drawing the first widget should match its own box, got {0}", controller.LastClipWidth);
            context.AreEqual(20, controller.LastClipHeight, "the clip rect after drawing the first widget should match its own box, got {0}", controller.LastClipHeight);

            sequentialB.Draw();

            context.AreEqual(90, controller.LastClipWidth, "the clip rect after drawing the second widget should be cancelled and replaced by its own box, not left over from the first, got {0}", controller.LastClipWidth);
            context.AreEqual(45, controller.LastClipHeight, "the clip rect after drawing the second widget should be cancelled and replaced by its own box, not left over from the first, got {0}", controller.LastClipHeight);
            context.AreEqual(countBeforeSequence + 2, controller.ClipRectCount, "each Draw() call with overflow: hidden should issue exactly one new clip rect, got a running count of {0}", controller.ClipRectCount);
        }

        // ----------------------------------------------------------------
        // Group J: Test 39 -- background properties
        // ----------------------------------------------------------------

        private static void Test39_BackgroundProperties(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();

            controller.RegisterSprite("styj1sprite", 64, 64);
            controller.RegisterSprite("styj2sprite", 90, 90);

            TestEnvironment.LoadCss(
                ".styj1back { width: 100px; height: 50px; background-image: url(\"styj1sprite\"); background-repeat: nineimage; background-size: 50%; --background-color: #ff0000; --background-padding: 1px 2px 3px 4px; }" +
                ".styj2back { border-image-source: url(\"styj2sprite\"); border-image-slice: 33.3333% fill; }");

            WidgetPanel root = new WidgetPanel();
            root.Size = new Vector2(400.0f, 300.0f);

            WidgetPanel background = new WidgetPanel(WidgetManager.GetStyle("styj1back"));
            root.AddChild(background);
            background.Relayout();

            context.AreEqual("styj1sprite", background.BackgroundTexture, "background-image: url(\"styj1sprite\") should reach BackgroundTexture, got {0}", background.BackgroundTexture);
            context.AreEqual(WidgetBackgroundStyle.NineImage, background.BackgroundStyle, "background-repeat: nineimage should reach BackgroundStyle == NineImage, got {0}", background.BackgroundStyle);
            context.AreEqualFloat(0.5f, background.BackgroundScale, Tolerance, "background-size: 50% should reach BackgroundScale == 0.5, got {0}", background.BackgroundScale);
            context.AreEqual((uint)0xff0000, background.BackgroundColor, "--background-color: #ff0000 should reach BackgroundColor, got {0:x6}", background.BackgroundColor);

            Margin padding = background.BackgroundPadding;
            context.AreEqualFloat(4.0f, padding.Left, Tolerance, "--background-padding: 1px 2px 3px 4px is top right bottom left, so Left == 4, got {0}", padding.Left);
            context.AreEqualFloat(1.0f, padding.Top, Tolerance, "--background-padding: 1px 2px 3px 4px is top right bottom left, so Top == 1, got {0}", padding.Top);
            context.AreEqualFloat(2.0f, padding.Right, Tolerance, "--background-padding: 1px 2px 3px 4px is top right bottom left, so Right == 2, got {0}", padding.Right);
            context.AreEqualFloat(3.0f, padding.Bottom, Tolerance, "--background-padding: 1px 2px 3px 4px is top right bottom left, so Bottom == 3, got {0}", padding.Bottom);

            ISprite subdividedSprite = controller.CreateSprite("styj2sprite");

            context.AreEqual(9, subdividedSprite.FrameCount, "a rule with border-image-source: url(\"styj2sprite\") and border-image-slice: 33.3333% fill should make the controller record a 3x3 == 9 frame subdivision, got {0}", subdividedSprite.FrameCount);
            context.AreEqualFloat(30.0f, subdividedSprite.FrameSize.X, Tolerance, "a 90px sprite subdivided 3x3 should have frames 30px wide, got {0}", subdividedSprite.FrameSize.X);
            context.AreEqualFloat(30.0f, subdividedSprite.FrameSize.Y, Tolerance, "a 90px sprite subdivided 3x3 should have frames 30px tall, got {0}", subdividedSprite.FrameSize.Y);
        }
    }
}
