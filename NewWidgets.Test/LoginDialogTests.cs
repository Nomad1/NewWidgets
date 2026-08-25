using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;

using NewWidgets.Sample;
using NewWidgets.UI;
using NewWidgets.Utility;
using NewWidgets.Widgets;

namespace NewWidgets.Test
{
    /// <summary>
    /// The sample login dialog, which used to compute roughly twenty positions and sizes in
    /// C# and now reads them from assets/login.css by #id.
    ///
    /// Test 71 is the proof that the rewrite changed nothing: it builds the real
    /// <see cref="TestWindow"/> -- the sample's own source file, compiled into this project --
    /// and asserts every resolved box against the number the old constructor computed. Every
    /// expected value below was read out of the pre-change TestWindow.cs, not out of the new
    /// stylesheet, so the two cannot agree by construction.
    ///
    /// Test 72 is the D146 experiment: what a parent resize does to a dialog written in
    /// percentages and anchors, and what the two candidate fixes cost.
    ///
    /// The stylesheets are referenced at their real path in the sample rather than copied
    /// here, following D128: this group is meant to break when the sample's CSS changes.
    /// </summary>
    internal static class LoginDialogTests
    {
        private const float Tolerance = 0.01f;

        private const string SampleAssetRoot = "../NewWidgets.RunMobileSample/assets";

        // ui.css names its font resource "font|font5"; Font's constructor walks the sprite for
        // a space glyph and throws if the resource is not registered, so it has to exist before
        // the stylesheet is read. Same requirement CorpusTests.EnsureTestFontsRegistered has.
        private const string SampleFontResource = "font|font5";

        // Pinned so the window's own 2048-unit box, and therefore the panel's centred
        // position, are the same number on every machine.
        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        private static readonly int s_resolveWarmupCount = 200;
        private static readonly int s_resolveCount = 2000;

        // Loose, like Test 70's: they catch a hundredfold regression, not a busy machine.
        private static readonly double s_resolveCeilingMicroseconds = 2000.0;

        public static void Register()
        {
            TestRunner.Add("Test 71: the sample login dialog resolves to its hand-computed geometry", Test71_LoginDialogGeometry);
            TestRunner.Add("Test 72: what a parent resize does to the dialog, and what fixing it costs", Test72_ParentResizeCost);
            TestRunner.Add("Test 89: the dialog resolves the same skin it did when it was built in code", Test89_LoginDialogSkin);
        }

        // ----------------------------------------------------------------
        // Test 89 -- the appearance, which Test 71's geometry cannot see
        // ----------------------------------------------------------------

        /// <summary>
        /// Test 71 pins where every control sits. It cannot see what a control looks like, and
        /// the move from a code-built dialog to a document changes exactly that: a widget the
        /// document built advertises its own tag, so it is matched by a different set of rules
        /// than the one the constructor produced. A dialog that lands in the right place wearing
        /// the wrong skin passes Test 71.
        ///
        /// So this group fingerprints every drawing property the cascade decides -- the
        /// background sprite and how it is cut, scaled, tinted and clipped, the text colour and
        /// size, and the two text-edit specifics -- and compares it against what the code-built
        /// dialog resolved. Every expected line below was captured from the dialog as
        /// TestWindow.cs built it before the rewrite, not written by hand from the stylesheet.
        /// </summary>
        private static readonly string[] s_expectedSkin = new string[]
        {
            "login_window WidgetWindow image=window_9 repeat=NineImage scale=0.75 backcolor=000000 backopacity=0 depth=Back color=000000 fontsize=1 align=Left overflow=Hidden clip=[Left:0 Top:0 Right:0 Bottom:0] padding=[Left:0 Top:0 Right:0 Bottom:0] caret=000000 visible=True enabled=True",
            "login_back WidgetPanel image=back_pattern repeat=ImageTiled scale=1 backcolor=000000 backopacity=0.04 depth=BackClipped color=000000 fontsize=1 align=Left overflow=Hidden clip=[Left:2 Top:2 Right:2 Bottom:2] padding=[Left:0 Top:0 Right:0 Bottom:0] caret=000000 visible=True enabled=True",
            "login_title WidgetLabel image= repeat=None scale=1 backcolor=000000 backopacity=0 depth=Back color=ffffff fontsize=1.5 align=HorizontalCenter, Top overflow=Visible clip=[Left:0 Top:0 Right:0 Bottom:0] padding=[Left:0 Top:0 Right:0 Bottom:0] caret=000000 visible=True enabled=True",
            "login_label WidgetLabel image= repeat=None scale=1 backcolor=000000 backopacity=0 depth=Back color=ffffff fontsize=1.25 align=Left overflow=Visible clip=[Left:0 Top:0 Right:0 Bottom:0] padding=[Left:0 Top:0 Right:0 Bottom:0] caret=000000 visible=True enabled=True",
            "login_edit WidgetTextEdit image=panel_white_normal_9 repeat=NineImage scale=0.25 backcolor=000000 backopacity=0 depth=Back color=ffffff fontsize=1.25 align=Left overflow=Hidden clip=[Left:0 Top:0 Right:0 Bottom:0] padding=[Left:0 Top:0 Right:0 Bottom:0] caret=ffffff visible=True enabled=True",
            "pass_label WidgetLabel image= repeat=None scale=1 backcolor=000000 backopacity=0 depth=Back color=ffffff fontsize=1.25 align=Left overflow=Visible clip=[Left:0 Top:0 Right:0 Bottom:0] padding=[Left:0 Top:0 Right:0 Bottom:0] caret=000000 visible=True enabled=True",
            "pass_edit WidgetTextEdit image=panel_white_hovered_9 repeat=NineImage scale=0.25 backcolor=000000 backopacity=0 depth=Back color=aaaaaa fontsize=1.25 align=Left overflow=Hidden clip=[Left:0 Top:0 Right:0 Bottom:0] padding=[Left:0 Top:0 Right:0 Bottom:0] caret=ffffff visible=True enabled=True",
            "local_label WidgetLabel image= repeat=None scale=1 backcolor=000000 backopacity=0 depth=Back color=cceeff fontsize=1 align=Left overflow=Visible clip=[Left:0 Top:0 Right:0 Bottom:0] padding=[Left:0 Top:0 Right:0 Bottom:0] caret=000000 visible=True enabled=True",
            "local_check WidgetCheckBox image=checkbox_back_normal repeat=ImageFit scale=1 backcolor=000000 backopacity=0 depth=Back color=cceeff fontsize=1 align=Left overflow=Visible clip=[Left:0 Top:0 Right:0 Bottom:0] padding=[Left:0 Top:0 Right:0 Bottom:0] caret=000000 visible=True enabled=True",
            "local_edit WidgetTextEdit image=panel_white_hovered_9 repeat=NineImage scale=0.25 backcolor=000000 backopacity=0 depth=Back color=aaaaaa fontsize=1.25 align=Left overflow=Hidden clip=[Left:0 Top:0 Right:0 Bottom:0] padding=[Left:0 Top:0 Right:0 Bottom:0] caret=ffffff visible=False enabled=True",
            "website_button WidgetButton image=button_white_normal_3 repeat=None scale=1 backcolor=000000 backopacity=0 depth=Back color=cceeff fontsize=0.6 align=Left overflow=Visible clip=[Left:0 Top:0 Right:0 Bottom:0] padding=[Left:0 Top:0 Right:0 Bottom:0] caret=000000 visible=True enabled=True",
            "login_button WidgetButton image=button_white_normal_3 repeat=ThreeImage scale=1 backcolor=aaaaaa backopacity=0.5 depth=Back color=808080 fontsize=0.6 align=Left overflow=Visible clip=[Left:0 Top:0 Right:0 Bottom:0] padding=[Left:0 Top:0 Right:0 Bottom:0] caret=000000 visible=True enabled=False",
            "logo_image WidgetImage image=settings_icon repeat=ImageFit scale=1 backcolor=000000 backopacity=0 depth=Back color=000000 fontsize=1 align=Left overflow=Visible clip=[Left:0 Top:0 Right:0 Bottom:0] padding=[Left:0 Top:0 Right:0 Bottom:0] caret=000000 visible=True enabled=True",
            "text_field WidgetTextField image=panel_white_hovered_9 repeat=NineImage scale=0.25 backcolor=000000 backopacity=0 depth=Back color=aaaaaa fontsize=1.25 align=Left overflow=Hidden clip=[Left:0 Top:0 Right:0 Bottom:0] padding=[Left:0 Top:0 Right:0 Bottom:0] caret=ffffff visible=True enabled=True",
            "fps_label WidgetLabel image= repeat=None scale=1 backcolor=000000 backopacity=0 depth=Back color=ffffff fontsize=0.75 align=Left overflow=Visible clip=[Left:0 Top:0 Right:0 Bottom:0] padding=[Left:0 Top:0 Right:0 Bottom:0] caret=000000 visible=True enabled=True",
        };

        private static void Test89_LoginDialogSkin(TestContext context)
        {
            WidgetPanel panel = BuildDialog(context);

            if (panel == null)
                return;

            List<Widget> widgets = CollectChildren(panel);
            widgets.Insert(0, panel);

            context.AreEqual(s_expectedSkin.Length, widgets.Count, "the dialog should still hold {0} widgets, got {1}", s_expectedSkin.Length, widgets.Count);

            for (int i = 0; i < widgets.Count && i < s_expectedSkin.Length; i++)
            {
                string actual = DescribeSkin(widgets[i]);

                context.AreEqual(s_expectedSkin[i], actual, "widget {0} should resolve the same skin it did when it was built in code:{1}  expected {2}{3}  got      {4}",
                    i, Environment.NewLine, s_expectedSkin[i], Environment.NewLine, actual);
            }
        }

        /// <summary>
        /// One line per widget, holding every property that decides what is drawn. Formatted
        /// invariantly so the fixture reads the same on a machine whose decimal separator is a
        /// comma.
        /// </summary>
        private static string DescribeSkin(Widget widget)
        {
            StringBuilder result = new StringBuilder(160);

            result.AppendFormat(CultureInfo.InvariantCulture, "{0} {1}", widget.StyleId, widget.GetType().Name);
            // the sprite, not the spelling: a stylesheet may name it "window_9" or "ui.svg#window_9"
            // and both resolve to the same sprite, which is what "the same skin" means here
            result.AppendFormat(CultureInfo.InvariantCulture, " image={0}",
                ConversionHelper.UrlToSpriteName(widget.GetProperty(WidgetParameterIndex.BackImage, string.Empty)));
            result.AppendFormat(CultureInfo.InvariantCulture, " repeat={0}", widget.GetProperty(WidgetParameterIndex.BackStyle, WidgetBackgroundStyle.None));
            result.AppendFormat(CultureInfo.InvariantCulture, " scale={0}", widget.GetProperty(WidgetParameterIndex.BackScale, 1.0f));
            result.AppendFormat(CultureInfo.InvariantCulture, " backcolor={0:x6}", widget.GetProperty(WidgetParameterIndex.BackColor, 0u));
            result.AppendFormat(CultureInfo.InvariantCulture, " backopacity={0}", widget.GetProperty(WidgetParameterIndex.BackOpacity, 0.0f));
            result.AppendFormat(CultureInfo.InvariantCulture, " depth={0}", widget.GetProperty(WidgetParameterIndex.BackDepth, WidgetBackgroundDepth.Back));
            result.AppendFormat(CultureInfo.InvariantCulture, " color={0:x6}", widget.GetProperty(WidgetParameterIndex.TextColor, 0u));
            result.AppendFormat(CultureInfo.InvariantCulture, " fontsize={0}", widget.GetProperty(WidgetParameterIndex.FontSize, 1.0f));
            result.AppendFormat(CultureInfo.InvariantCulture, " align={0}", widget.GetProperty(WidgetParameterIndex.TextAlign, WidgetAlign.Left));
            result.AppendFormat(CultureInfo.InvariantCulture, " overflow={0}", widget.GetProperty(WidgetParameterIndex.Overflow, WidgetOverflow.Visible));
            result.AppendFormat(CultureInfo.InvariantCulture, " clip={0}", widget.GetProperty(WidgetParameterIndex.ClipMargin, Margin.Empty));
            result.AppendFormat(CultureInfo.InvariantCulture, " padding={0}", widget.GetProperty(WidgetParameterIndex.Padding, Margin.Empty));
            result.AppendFormat(CultureInfo.InvariantCulture, " caret={0:x6}", widget.GetProperty(WidgetParameterIndex.CursorColor, 0u));
            result.AppendFormat(CultureInfo.InvariantCulture, " visible={0}", widget.Visible);
            result.AppendFormat(CultureInfo.InvariantCulture, " enabled={0}", widget.Enabled);

            return result.ToString();
        }

        // ----------------------------------------------------------------
        // Test 71 -- the geometry is unchanged
        // ----------------------------------------------------------------

        private static void Test71_LoginDialogGeometry(TestContext context)
        {
            WidgetPanel panel = BuildDialog(context);

            if (panel == null)
                return;

            // The panel itself. Its size now comes from #login_window; its position is still
            // computed in TestWindow.cs, and deliberately so -- a Window is not a Widget, so
            // this panel's containing block is the screen and not the window's 2048-unit box.
            context.AreEqualFloat(600.0f, panel.Size.X, Tolerance, "the panel should still be 600 wide, got {0}", panel.Size.X);
            context.AreEqualFloat(760.0f, panel.Size.Y, Tolerance, "the panel should still be 760 tall, got {0}", panel.Size.Y);

            // 2048 wide, 2048 * 1080 / 1920 == 1152 tall, panel 600x760 at UIScale 1
            context.AreEqualFloat(724.0f, panel.Position.X, Tolerance, "the panel should still be centred at x == 1024 - 300, got {0}", panel.Position.X);
            context.AreEqualFloat(196.0f, panel.Position.Y, Tolerance, "the panel should still be centred at y == 576 - 380, got {0}", panel.Position.Y);

            // Every one of these was a literal in the old constructor. Left column is the
            // expression it replaced.

            AssertBox(context, panel, "login_back", 0, 0, 600, 760);        // Size = panel.Size
            AssertBox(context, panel, "login_title", 0, 50, 600, 60);       // new Vector2(panel.Size.X, 60)
            AssertBox(context, panel, "login_label", 50, 160, 100, 35);     // new Vector2(50, 160)
            AssertBox(context, panel, "login_edit", 50, 200, 500, 45);      // new Vector2(500, 45) at (50, 200)
            AssertBox(context, panel, "pass_label", 50, 260, 100, 35);      // new Vector2(50, 260)
            AssertBox(context, panel, "pass_edit", 50, 300, 500, 45);       // new Vector2(500, 45) at (50, 300)
            AssertBox(context, panel, "local_label", 90, 360, 100, 35);     // new Vector2(90, 360)
            AssertBox(context, panel, "local_check", 50, 360, 40, 40);      // new Vector2(50, 360)
            AssertBox(context, panel, "local_edit", 50, 100, 500, 45);      // new Vector2(500, 45) at (50, 100)
            AssertBox(context, panel, "website_button", 50, 400, 300, 20);  // new Vector2(50, 360 + 40)
            AssertBox(context, panel, "login_button", 220, 460, 160, 48);   // panel.Size.X / 2 - Size.X / 2
            AssertBox(context, panel, "logo_image", 20, 15, 64, 64);        // new Vector2(20, 15), 64x64
            AssertBox(context, panel, "text_field", 50, 520, 500, 225);     // new Vector2(500, 225) at (50, 520)
            AssertBox(context, panel, "fps_label", 440, 20, 100, 35);       // new Vector2(440, 20)

            // localLabel.Color = 0xcceeff became a CSS declaration on #local_label
            WidgetLabel localLabel = FindById(panel, "local_label") as WidgetLabel;
            context.IsNotNull(localLabel, "#local_label should be a WidgetLabel");

            if (localLabel != null)
                context.AreEqual(0xcceeffu, localLabel.Color, "#local_label should still be 0xcceeff, got {0:x}", localLabel.Color);
        }

        // ----------------------------------------------------------------
        // Test 72 -- D146: a parent resize, and the cost of following it
        // ----------------------------------------------------------------

        private static void Test72_ParentResizeCost(TestContext context)
        {
            WidgetPanel panel = BuildDialog(context);

            if (panel == null)
                return;

            List<Widget> children = CollectChildren(panel);

            context.IsTrue(children.Count >= 14, "the dialog should have at least 14 children to measure, got {0}", children.Count);

            // 1. What actually happens. Widen the panel and run ordinary frames.

            Widget title = FindById(panel, "login_title");        // width: 100%
            Widget loginEdit = FindById(panel, "login_edit");     // left + right
            Widget loginButton = FindById(panel, "login_button"); // auto margins
            Widget fpsLabel = FindById(panel, "fps_label");       // right anchored

            panel.Size = new Vector2(800.0f, 760.0f);

            for (int i = 0; i < 3; i++)
                panel.Update();

            Console.WriteLine("    after widening the panel from 600 to 800 and running 3 frames:");
            Console.WriteLine("      #login_title  width  {0} (a browser would say 800)", title.Size.X);
            Console.WriteLine("      #login_edit   width  {0} (a browser would say 700)", loginEdit.Size.X);
            Console.WriteLine("      #login_button x      {0} (a browser would say 320)", loginButton.Position.X);
            Console.WriteLine("      #fps_label    x      {0} (a browser would say 640)", fpsLabel.Position.X);

            // Characterization, not specification. Test 31b already carries the defect as a
            // known failure; these assertions exist so this group reports the change rather
            // than quietly agreeing with whatever the engine starts doing.
            context.AreEqualFloat(600.0f, title.Size.X, Tolerance,
                "width: 100% still reads against the panel's old 600, got {0}. If this is now 800, propagation landed and Test 31b should be promoted", title.Size.X);
            context.AreEqualFloat(500.0f, loginEdit.Size.X, Tolerance,
                "left + right still stretch against the old 600, got {0}", loginEdit.Size.X);
            context.AreEqualFloat(220.0f, loginButton.Position.X, Tolerance,
                "the auto-margin centring still reads the old 600, got {0}", loginButton.Position.X);
            context.AreEqualFloat(440.0f, fpsLabel.Position.X, Tolerance,
                "the right anchor still reads the old 600, got {0}", fpsLabel.Position.X);

            // The child does re-resolve the moment anything invalidates it on its own account.
            title.InvalidateLayout();
            title.Relayout();

            context.AreEqualFloat(800.0f, title.Size.X, Tolerance,
                "an explicit invalidation on the child alone should re-resolve width: 100% to 800, got {0}", title.Size.X);

            // 2. What propagation would cost. Two candidates, measured on this same tree.

            double fullResolve = MeasureFullResolve(children);
            double boxOnly = MeasureBoxOnlyResolve(children);

            Console.WriteLine("    full style resolve (UpdateStyle + ResolveBox): {0:F4} us per widget", fullResolve);
            Console.WriteLine("    box-only re-resolve (14 reads + 2 axes):       {0:F4} us per widget", boxOnly);
            Console.WriteLine("    this dialog is {0} widgets deep in one panel", children.Count);
            double fullSubtree = fullResolve * children.Count;
            double boxOnlySubtree = boxOnly * children.Count;

            // D144 states the budget as 0.1 ms at 120 fps, which is 100 microseconds, so a
            // microsecond figure and a percentage of the budget are the same number here.
            Console.WriteLine("    propagating a panel resize would cost {0:F1} us full, {1:F1} us box-only", fullSubtree, boxOnlySubtree);
            Console.WriteLine("    against the 100 us frame budget that is {0:F0}% full, {1:F0}% box-only",
                fullSubtree, boxOnlySubtree);

            context.IsTrue(fullResolve < s_resolveCeilingMicroseconds,
                "a full style resolve costs {0:F4} us, ceiling is {1:F1} us", fullResolve, s_resolveCeilingMicroseconds);

            context.IsTrue(boxOnly < fullResolve,
                "re-resolving only the box must be cheaper than rebuilding the cascade, got {0:F4} us against {1:F4} us", boxOnly, fullResolve);
        }

        /// <summary>
        /// The cost of the propagation that Test 31b asks for, as the engine could implement it
        /// today: mark the child and let Relayout run, which rebuilds the selector chain, queries
        /// the style collection and then resolves the box.
        /// </summary>
        private static double MeasureFullResolve(IList<Widget> children)
        {
            for (int i = 0; i < s_resolveWarmupCount; i++)
                ResolveAll(children);

            Stopwatch watch = Stopwatch.StartNew();

            for (int i = 0; i < s_resolveCount; i++)
                ResolveAll(children);

            watch.Stop();

            return watch.Elapsed.TotalMilliseconds * 1000.0 / (s_resolveCount * children.Count);
        }

        private static void ResolveAll(IList<Widget> children)
        {
            for (int i = 0; i < children.Count; i++)
            {
                children[i].InvalidateLayout();
                children[i].Relayout();
            }
        }

        /// <summary>
        /// The cheaper candidate: leave the resolved style alone and redo the box arithmetic
        /// only. This is exactly what Widget.ResolveBox does -- fourteen reads off the already
        /// resolved style, then one StyleAxis per axis -- with the cascade rebuild left out.
        /// Reproduced here rather than called, because ResolveBox is private and Widget.cs is
        /// not this task's to change.
        /// </summary>
        private static double MeasureBoxOnlyResolve(IList<Widget> children)
        {
            float sink = 0.0f;

            for (int i = 0; i < s_resolveWarmupCount; i++)
                sink += ResolveBoxesOnly(children);

            Stopwatch watch = Stopwatch.StartNew();

            for (int i = 0; i < s_resolveCount; i++)
                sink += ResolveBoxesOnly(children);

            watch.Stop();

            if (sink == float.MaxValue)
                Console.WriteLine("sink guard, keeps the arithmetic from being removed as dead code");

            return watch.Elapsed.TotalMilliseconds * 1000.0 / (s_resolveCount * children.Count);
        }

        private static float ResolveBoxesOnly(IList<Widget> children)
        {
            float sink = 0.0f;

            for (int i = 0; i < children.Count; i++)
            {
                Widget child = children[i];
                Vector2 containingBlock = child.Parent.Size;

                Vector2 size = child.Size;
                Vector2 position = child.Position;

                StyleAxis horizontal = new StyleAxis(
                    child.GetProperty(WidgetParameterIndex.Left, StyleLength.Unset),
                    child.GetProperty(WidgetParameterIndex.Right, StyleLength.Unset),
                    child.GetProperty(WidgetParameterIndex.Width, StyleLength.Unset),
                    child.GetProperty(WidgetParameterIndex.MarginLeft, StyleLength.Unset),
                    child.GetProperty(WidgetParameterIndex.MarginRight, StyleLength.Unset),
                    child.GetProperty(WidgetParameterIndex.MinWidth, StyleLength.Unset),
                    child.GetProperty(WidgetParameterIndex.MaxWidth, StyleLength.Unset));

                horizontal.Resolve(containingBlock.X, ref position.X, ref size.X);

                StyleAxis vertical = new StyleAxis(
                    child.GetProperty(WidgetParameterIndex.Top, StyleLength.Unset),
                    child.GetProperty(WidgetParameterIndex.Bottom, StyleLength.Unset),
                    child.GetProperty(WidgetParameterIndex.Height, StyleLength.Unset),
                    child.GetProperty(WidgetParameterIndex.MarginTop, StyleLength.Unset),
                    child.GetProperty(WidgetParameterIndex.MarginBottom, StyleLength.Unset),
                    child.GetProperty(WidgetParameterIndex.MinHeight, StyleLength.Unset),
                    child.GetProperty(WidgetParameterIndex.MaxHeight, StyleLength.Unset));

                vertical.Resolve(containingBlock.Y, ref position.Y, ref size.Y);

                sink += size.X + position.Y;
            }

            return sink;
        }

        // ----------------------------------------------------------------
        // Shared setup
        // ----------------------------------------------------------------

        /// <summary>
        /// Loads the sample's two stylesheets into an empty style collection and builds the real
        /// TestWindow, pumping frames until every widget has resolved. Returns the dialog panel,
        /// or null when the group has to be skipped.
        /// </summary>
        private static WidgetPanel BuildDialog(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();

            controller.SetScreenSize(ScreenWidth, ScreenHeight);
            controller.SetUIScale(1.0f);
            controller.RegisterTestFont(SampleFontResource, 10, 16);

            // login.css declares ordinary ids and WidgetManager's collection is process-wide,
            // so the corpus and conformance rules loaded by earlier groups have to go first.
            WidgetManager.ResetStyles();

            string skinPath = Path.Combine(SampleAssetRoot, "ui.css");
            string documentPath = Path.Combine(SampleAssetRoot, "login.xhtml");

            context.IsTrue(File.Exists(skinPath), "the sample skin {0} should be readable", skinPath);
            context.IsTrue(File.Exists(documentPath), "the sample dialog {0} should be readable", documentPath);

            if (!File.Exists(skinPath) || !File.Exists(documentPath))
                return null;

            // Only the skin. The dialog's geometry sheet is linked from the document and is
            // loaded by TestWindow along with it, exactly as it is in the running sample.
            WidgetManager.LoadCSS(File.ReadAllText(skinPath));

            TestWindow window = new TestWindow(SampleAssetRoot);

            // Three frames: the panel settles on the first, its children read the settled
            // containing block on the same pass, and the rest is slack.
            for (int i = 0; i < 3; i++)
                window.Update();

            WidgetPanel panel = null;

            foreach (WindowObject child in window.Children)
            {
                WidgetPanel candidate = child as WidgetPanel;

                if (candidate != null && candidate.StyleId == "login_window")
                    panel = candidate;
            }

            context.IsNotNull(panel, "the dialog should hold a panel with id login_window");

            return panel;
        }

        private static List<Widget> CollectChildren(WidgetPanel panel)
        {
            List<Widget> result = new List<Widget>(16);

            foreach (WindowObject child in panel.Children)
            {
                Widget widget = child as Widget;

                if (widget != null)
                    result.Add(widget);
            }

            return result;
        }

        private static Widget FindById(WidgetPanel panel, string id)
        {
            foreach (WindowObject child in panel.Children)
            {
                Widget widget = child as Widget;

                if (widget != null && widget.StyleId == id)
                    return widget;
            }

            return null;
        }

        private static void AssertBox(TestContext context, WidgetPanel panel, string id, float x, float y, float width, float height)
        {
            Widget widget = FindById(panel, id);

            context.IsNotNull(widget, "the dialog should hold a widget with id {0}", id);

            if (widget == null)
                return;

            context.AreEqualFloat(x, widget.Position.X, Tolerance, "#{0} should sit at x == {1}, got {2}", id, x, widget.Position.X);
            context.AreEqualFloat(y, widget.Position.Y, Tolerance, "#{0} should sit at y == {1}, got {2}", id, y, widget.Position.Y);
            context.AreEqualFloat(width, widget.Size.X, Tolerance, "#{0} should be {1} wide, got {2}", id, width, widget.Size.X);
            context.AreEqualFloat(height, widget.Size.Y, Tolerance, "#{0} should be {1} tall, got {2}", id, height, widget.Size.Y);
        }
    }
}
