using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using System.Threading;

using NewWidgets.UI;
using NewWidgets.UI.Styles;
using NewWidgets.Utility;
using NewWidgets.Widgets;

#if AMALTHEA_SOURCE
using SpaceAdventure.BusinessLogic.Controls;
using SpaceAdventure.BusinessLogic.Controls.Buttons;
#endif

namespace NewWidgets.Test
{
    /// <summary>
    /// Golden-master (characterization) regression suite driving two real, shipped games'
    /// CSS through the real <see cref="WidgetManager"/> pipeline. This does not assert what is
    /// CORRECT -- it records what IS, so that a deliberate change to the CSS parser/cascade can
    /// be told apart from an accidental one. Everything here must be all-green against today's
    /// unmodified library; a "bug" found while writing this suite belongs in a report, not in an
    /// assertion of corrected behaviour.
    /// </summary>
    internal static class CorpusTests
    {
        // DialogWindow's own constructor is protected; DialogWindow.Show(...) is the only
        // public entry point, but it cannot be used here: Show() calls Move(...), which starts
        // an AnimationManager animation. AnimationManager reschedules itself every "frame" via
        // WindowController.Instance.ScheduleAction(Update, 1) (NewWidgets/UI/AnimationManager.cs),
        // and TestController.ScheduleAction runs that callback *synchronously* instead of
        // deferring it (TestController.cs) -- and since TestController's clock never advances
        // on its own, the animation never completes, so ReSchedule keeps calling itself.
        // Confirmed by actually running it: a real stack overflow, about 29000 frames deep.
        // This is a genuine TestController/AnimationManager incompatibility that predates this
        // suite and is unrelated to CSS; it is reported separately rather than "fixed" here by
        // reaching into library code. This test-only subclass calls straight through to
        // DialogWindow's protected constructor, skipping Show() (and its animation) entirely.
#if AMALTHEA_SOURCE
        private sealed class TestDialogWindow : DialogWindow
        {
            public TestDialogWindow(string title, string text, params string[] options)
                : base(title, text, options)
            {
            }
        }
#endif

        // Minimal IStyleData for parsing a game's own CSS into an isolated, throwaway
        // StyleCollection just to learn its selector names (see ComputeOwnSelectorHeaders) --
        // the property values are never read back, so LoadData has nothing to do.
        private sealed class SelectorOnlyStyleData : IStyleData
        {
            public void LoadData(IStyleData data)
            {
            }
        }

        // Byte copies of the two games' shipped stylesheets, cached inside this repository so the
        // suite is self-contained and names no path on anyone's machine. They were taken from
        // Resources/Shared/ui in each game's checkout; refresh them from there (and expect the
        // baselines below to move, which is the finding, not a nuisance) when a game's UI changes.
        private const string AmaltheaUiRoot = "Conformance/amalthea";
        private const string SiegeWarsUiRoot = "Conformance/siegewars";

        private const string AmaltheaCssBaselinePath = "Baselines/amalthea-css.txt";
        private const string SiegeWarsCssBaselinePath = "Baselines/siegewars-css.txt";
        private const string AmaltheaWidgetsBaselinePath = "Baselines/amalthea-widgets.txt";

        // Loaded by SpaceAdventure.Client's GameController.SpaceAdventure.cs (around lines
        // 500-506) in exactly this order; order matters for the cascade.
        private static readonly string[] AmaltheaCssFiles = { "fonts.css", "tiles.css", "defaults.css", "ui.css", "buttons.css", "frames.css", "talents.css" };

        // Loaded by SiegeWars.Client's GameController.SiegeWars.cs (around lines 181-185).
        private static readonly string[] SiegeWarsCssFiles = { "defaults.css", "ui.css", "editor.css" };

        // The three font sprite resources fonts.css/defaults.css declare via
        // "src: url(...)" for both games. Must be registered as test fonts before any CSS that
        // declares an @font-face rule is loaded, or Font's constructor throws
        // KeyNotFoundException looking up the space glyph (TestController's default,
        // unregistered sprite has a single frame tagged 0, not one covering ASCII 32).
        private static readonly string[] FontResourceIds = { "font5", "font4", "font6" };

        private const int TestGlyphWidth = 10;
        private const int TestGlyphHeight = 16;

        private const float PinnedUIScale = 1.0f;

        private static bool s_fontsRegistered;

        // Set by Program.cs from the "--update-baselines" command-line flag. Kept on this
        // class rather than threaded through TestRunner, per the brief: TestRunner has no
        // concept of this flag and is not the right place to add one.
        public static bool UpdateBaselines;

        public static void Register()
        {
            TestRunner.Add("Test 40: Amalthea stylesheet loads clean", Test40_AmaltheaLoadsClean);
            TestRunner.Add("Test 41: SiegeWars stylesheet loads clean", Test41_SiegeWarsLoadsClean);
            TestRunner.Add("Test 42: Amalthea computed-style baseline", Test42_AmaltheaComputedStyleBaseline);
            TestRunner.Add("Test 43: SiegeWars computed-style baseline", Test43_SiegeWarsComputedStyleBaseline);
            TestRunner.Add("Test 44: Amalthea widget-tree baseline", Test44_AmaltheaWidgetTreeBaseline);
        }

        // ------------------------------------------------------------------
        // Test 40 / 41: clean load, pinned unknown-property set
        // ------------------------------------------------------------------

        private static void Test40_AmaltheaLoadsClean(TestContext context)
        {
            TestController controller = PrepareForCssLoad();

            LoadCssFiles(context, AmaltheaUiRoot, AmaltheaCssFiles);

            context.IsTrue(controller.Errors.Count == 0, "TestController.Errors must be empty after loading Amalthea CSS, got {0}: {1}",
                controller.Errors.Count, JoinMessages(controller.Errors));

            // Known live defect in Amalthea's shipped CSS, pinned rather than fixed: these four
            // properties are declared in the stylesheets but WidgetParameterMap has no
            // processor for them, so WidgetManager.InitCssParameters logs "unknown attribute"
            // and drops each declaration. Counts were verified against the corpus directly
            // (grep -o -- '--button-image-padding' *.css | wc -l, etc.) and match exactly.
            Dictionary<string, int> expectedUnknown = new Dictionary<string, int>();
            expectedUnknown["--button-image-padding"] = 19;
            expectedUnknown["--button-text-padding"] = 13;
            expectedUnknown["--abackground-image"] = 3;
            expectedUnknown["visibility"] = 2;

            AssertUnknownAttributesExactly(context, controller.Messages, expectedUnknown, "Amalthea");
        }

        private static void Test41_SiegeWarsLoadsClean(TestContext context)
        {
            TestController controller = PrepareForCssLoad();

            LoadCssFiles(context, SiegeWarsUiRoot, SiegeWarsCssFiles);

            context.IsTrue(controller.Errors.Count == 0, "TestController.Errors must be empty after loading SiegeWars CSS, got {0}: {1}",
                controller.Errors.Count, JoinMessages(controller.Errors));

            // Unlike Amalthea's pinned set, these counts were not looked up by hand in the CSS
            // source -- they were observed by running this test once and pasting the actual
            // failure output back in as the pin. If SiegeWars' CSS changes, this pin is
            // expected to need updating along with it; that is the point of a golden master.
            Dictionary<string, int> expectedUnknown = new Dictionary<string, int>();
            expectedUnknown["--button-image-padding"] = 4;
            expectedUnknown["--image-position"] = 1;
            expectedUnknown["--image-color"] = 6;
            expectedUnknown["--text-padding"] = 4;
            expectedUnknown["--button-text-padding"] = 3;
            expectedUnknown["image"] = 3;

            AssertUnknownAttributesExactly(context, controller.Messages, expectedUnknown, "SiegeWars");
        }

        private static TestController PrepareForCssLoad()
        {
            TestController controller = TestEnvironment.Setup();
            EnsureTestFontsRegistered();
            controller.ClearLog();
            return controller;
        }

        private static void LoadCssFiles(TestContext context, string root, string[] files)
        {
            for (int i = 0; i < files.Length; i++)
            {
                string path = Path.Combine(root, files[i]);
                string css = File.ReadAllText(path);
                string fileName = files[i];

                context.DoesNotThrow(delegate
                {
                    WidgetManager.LoadCSS(css);
                }, "Loading {0} must not throw", fileName);
            }
        }

        private static void EnsureTestFontsRegistered()
        {
            if (s_fontsRegistered)
                return;

            TestController controller = TestEnvironment.Setup();

            for (int i = 0; i < FontResourceIds.Length; i++)
                controller.RegisterTestFont(FontResourceIds[i], TestGlyphWidth, TestGlyphHeight);

            s_fontsRegistered = true;
        }

        // Parses every entry in `messages` as the fixed "Got unknown attribute {0} in CSS style
        // sheet" shape WidgetManager.InitCssParameters logs (Widgets/WidgetManager.Styles.cs),
        // tallies occurrences per property name, and asserts the tally matches `expectedCounts`
        // exactly -- same properties, same counts, nothing extra. A fifth unrecognized property
        // showing up, or a message of a different shape entirely, fails loudly instead of being
        // silently absorbed.
        private static void AssertUnknownAttributesExactly(TestContext context, IList<string> messages, IDictionary<string, int> expectedCounts, string gameLabel)
        {
            const string Prefix = "Got unknown attribute ";
            const string Suffix = " in CSS style sheet";

            Dictionary<string, int> actualCounts = new Dictionary<string, int>();

            for (int i = 0; i < messages.Count; i++)
            {
                string message = messages[i];
                bool isKnownShape = message.StartsWith(Prefix, StringComparison.Ordinal) && message.EndsWith(Suffix, StringComparison.Ordinal);

                context.IsTrue(isKnownShape, "{0}: every logged message must be the known 'unknown attribute' message, got '{1}'", gameLabel, message);

                if (!isKnownShape)
                    continue;

                string propertyName = message.Substring(Prefix.Length, message.Length - Prefix.Length - Suffix.Length);

                int count;
                actualCounts.TryGetValue(propertyName, out count);
                actualCounts[propertyName] = count + 1;
            }

            int expectedTotal = 0;

            foreach (KeyValuePair<string, int> pair in expectedCounts)
            {
                expectedTotal += pair.Value;

                int actual;
                actualCounts.TryGetValue(pair.Key, out actual);
                context.AreEqual(pair.Value, actual, "{0}: expected {1} occurrence(s) of unknown property {2}, got {3}", gameLabel, pair.Value, pair.Key, actual);
            }

            foreach (KeyValuePair<string, int> pair in actualCounts)
                context.IsTrue(expectedCounts.ContainsKey(pair.Key), "{0}: unexpected unknown property {1} seen {2} time(s) -- not in the pinned set, a new live defect appeared", gameLabel, pair.Key, pair.Value);

            context.AreEqual(expectedTotal, messages.Count, "{0}: total logged message count must match the pinned total", gameLabel);
        }

        private static string JoinMessages(IList<string> messages)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < messages.Count; i++)
            {
                if (i != 0)
                    builder.Append(" | ");
                builder.Append(messages[i]);
            }

            return builder.ToString();
        }

        // ------------------------------------------------------------------
        // Test 42 / 43: computed-style golden master
        // ------------------------------------------------------------------

        private static void Test42_AmaltheaComputedStyleBaseline(TestContext context)
        {
            PrepareForCssLoad();
            LoadCssFiles(context, AmaltheaUiRoot, AmaltheaCssFiles);

            CompareOrUpdateCssBaseline(context, AmaltheaCssBaselinePath, "Amalthea", AmaltheaUiRoot, AmaltheaCssFiles);
        }

        private static void Test43_SiegeWarsComputedStyleBaseline(TestContext context)
        {
            PrepareForCssLoad();
            LoadCssFiles(context, SiegeWarsUiRoot, SiegeWarsCssFiles);

            CompareOrUpdateCssBaseline(context, SiegeWarsCssBaselinePath, "SiegeWars", SiegeWarsUiRoot, SiegeWarsCssFiles);
        }

        // Dumps the whole (process-wide, shared, ever-growing) style collection via
        // WidgetManager.SaveCSS -- which, by the time this runs, contains every selector any
        // of the other 34 groups plus BOTH games have ever added, none of it removable -- then
        // keeps only the records for selectors `root`/`files` themselves declare, splits what
        // remains into one record per "selector { ... }" node, and sorts the records.
        // StyleCollection.Dump iterates a Dictionary<string, StyleNode> whose enumeration
        // order the BCL does not contractually guarantee, so comparing raw Dump output
        // line-by-line would risk failing on ordering alone rather than on an actual
        // behaviour change; sorting first makes the comparison order-independent.
        private static void CompareOrUpdateCssBaseline(TestContext context, string baselinePath, string gameLabel, string root, string[] files)
        {
            HashSet<string> ownSelectors = ComputeOwnSelectorHeaders(root, files);
            string[] actualLines = DumpSortedCssRecords(ownSelectors);

            if (UpdateBaselines)
            {
                WriteBaseline(baselinePath, actualLines);
                Console.WriteLine("    {0}: baseline written to {1} ({2} line(s))", gameLabel, baselinePath, actualLines.Length);
                return;
            }

            if (!File.Exists(baselinePath))
            {
                context.Fail("{0}: baseline file {1} does not exist -- run with --update-baselines to create it", gameLabel, baselinePath);
                return;
            }

            string[] expectedLines = File.ReadAllLines(baselinePath);

            CompareLines(context, gameLabel, expectedLines, actualLines);
        }

        // Parses `files` into a StyleCollection of their own, entirely separate from
        // WidgetManager's shared static one, purely to learn which selector strings this game
        // declares -- the property VALUES coming out of this parse are never used (and never
        // compared), only the "<selector> {" header line each resulting node produces.
        private static HashSet<string> ComputeOwnSelectorHeaders(string root, string[] files)
        {
            StyleCollection localCollection = new StyleCollection();

            for (int i = 0; i < files.Length; i++)
            {
                string css = File.ReadAllText(Path.Combine(root, files[i]));
                CSSParser.ParseCSS(css, localCollection, BuildSelectorOnlyStyleData);
            }

            string[] records = DumpToRecords(localCollection.Dump);

            HashSet<string> headers = new HashSet<string>();

            for (int i = 0; i < records.Length; i++)
                headers.Add(RecordHeader(records[i]));

            return headers;
        }

        private static IStyleData BuildSelectorOnlyStyleData(string name, Dictionary<string, string> parameters)
        {
            return new SelectorOnlyStyleData();
        }

        private static string RecordHeader(string record)
        {
            int newline = record.IndexOf('\n');
            return newline < 0 ? record : record.Substring(0, newline);
        }

        // ConversionHelper.ToString(float, ...) already formats every registered value type
        // (float, Margin, Vector2/3/4, uint, string) with CultureInfo.InvariantCulture
        // explicitly, so a "0.5" becoming "0,5" is not reachable through that path today.
        // It IS reachable for any value type NOT in that formatter table -- FormatValue's
        // fallback is "value.ToString().ToLower()" with no culture argument at all -- so we
        // still pin the thread culture defensively for the duration of the dump, rather than
        // relying on every current and future property type going through the
        // explicitly-invariant path.
        private static string[] DumpToRecords(Action<TextWriter> dump)
        {
            CultureInfo previousCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            string dumped;

            try
            {
                using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture))
                {
                    dump(writer);
                    dumped = writer.ToString();
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previousCulture;
            }

            dumped = dumped.Replace("\r\n", "\n");

            string[] records = dumped.Split(new string[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < records.Length; i++)
                records[i] = records[i].Trim('\n');

            return records;
        }

        private static string[] DumpSortedCssRecords(HashSet<string> ownSelectors)
        {
            string[] allRecords = DumpToRecords(WidgetManager.SaveCSS);

            List<string> ownRecords = new List<string>();

            for (int i = 0; i < allRecords.Length; i++)
                if (ownSelectors.Contains(RecordHeader(allRecords[i])))
                    ownRecords.Add(allRecords[i]);

            string[] records = ownRecords.ToArray();
            Array.Sort(records, StringComparer.Ordinal);

            List<string> lines = new List<string>();

            for (int i = 0; i < records.Length; i++)
            {
                if (i != 0)
                    lines.Add(string.Empty);

                lines.AddRange(records[i].Split('\n'));
            }

            return lines.ToArray();
        }

        private static void CompareLines(TestContext context, string label, string[] expectedLines, string[] actualLines)
        {
            const int MaxReportedDiffs = 5;

            int reported = 0;
            int minLength = Math.Min(expectedLines.Length, actualLines.Length);

            for (int i = 0; i < minLength; i++)
            {
                if (!string.Equals(expectedLines[i], actualLines[i], StringComparison.Ordinal))
                {
                    if (reported < MaxReportedDiffs)
                    {
                        context.Fail("{0}: baseline mismatch at line {1}: expected '{2}', got '{3}'", label, i + 1, expectedLines[i], actualLines[i]);
                        reported++;
                    }
                }
            }

            if (expectedLines.Length != actualLines.Length)
                context.Fail("{0}: baseline line count changed: expected {1} line(s), got {2}", label, expectedLines.Length, actualLines.Length);

            context.IsTrue(reported == 0 && expectedLines.Length == actualLines.Length, "{0}: baseline must match exactly ({1} differing line(s) reported above, capped at {2})", label, reported, MaxReportedDiffs);
        }

        private static void WriteBaseline(string path, string[] lines)
        {
            string directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllLines(path, lines);
        }

        // ------------------------------------------------------------------
        // Test 44: widget-tree golden master
        // ------------------------------------------------------------------

#if AMALTHEA_SOURCE
        private static void Test44_AmaltheaWidgetTreeBaseline(TestContext context)
        {
            TestController controller = PrepareForCssLoad();
            LoadCssFiles(context, AmaltheaUiRoot, AmaltheaCssFiles);

            // Localized strings: Widget.Text starting with '@' resolves through
            // ResourceLoader.Instance.GetString, which affects auto-sized widgets' computed
            // Size in this baseline. We register deterministic stubs for the handful of keys
            // the constructed classes actually use, rather than loading the real
            // Resources/Shared/xml/en-us/strings.xml, so this baseline stays stable if the
            // game's translations change -- only its CSS/layout is in scope here.
            ResourceLoader.Instance.RegisterString("button_close", "Close");
            ResourceLoader.Instance.RegisterString("button_ok", "OK");

            // WindowController.UIScale is pinned explicitly rather than left at
            // TestController's constructor default (also 1.0), so this baseline does not
            // silently move if that default ever changes.
            controller.SetUIScale(PinnedUIScale);

            StringBuilder output = new StringBuilder();

            AppendWidgetTree(output, "DialogPanel", BuildDialogPanel());
            AppendWidgetTree(output, "DialogWindow", BuildDialogWindow());
            AppendWidgetTree(output, "WidgetItemButton", BuildWidgetItemButton());
            AppendWidgetTree(output, "FactionLogo", BuildFactionLogo());
            AppendWidgetTree(output, "FeatureButtonsPanel", BuildFeatureButtonsPanel());

            string[] actualLines = output.ToString().Replace("\r\n", "\n").Split('\n');

            CompareOrUpdateWidgetBaseline(context, actualLines);
        }

        private static WindowObject BuildDialogPanel()
        {
            DialogPanel panel = new DialogPanel("Test Panel Title");
            panel.Relayout();
            panel.Size = new Vector2(800, 600);
            return panel;
        }

        private static WindowObject BuildDialogWindow()
        {
            TestDialogWindow dialog = new TestDialogWindow("Test Dialog Title", "Test dialog body text for the baseline snapshot.", "@button_ok");

            // The one non-animated side effect Show() has that matters for layout: it scales
            // the dialog to the (pinned, 1920-wide) screen.
            dialog.Scale = WindowController.Instance.ScreenWidth / 2048.0f;

            return dialog;
        }

        private static WindowObject BuildWidgetItemButton()
        {
            WidgetItemButton button = new WidgetItemButton(WidgetManager.GetStyle("item_button"), "Test Item");
            button.Relayout();
            return button;
        }

        private static WindowObject BuildFactionLogo()
        {
            FactionLogo logo = new FactionLogo();
            logo.Relayout();
            return logo;
        }

        private static WindowObject BuildFeatureButtonsPanel()
        {
            FeatureButtonsPanel panel = new FeatureButtonsPanel();
            panel.Relayout();
            return panel;
        }

        private static void AppendWidgetTree(StringBuilder output, string label, WindowObject root)
        {
            output.AppendFormat(CultureInfo.InvariantCulture, "=== {0} ===", label).AppendLine();
            WalkWidgetTree(output, root, 0);
        }

        private static void WalkWidgetTree(StringBuilder output, WindowObject node, int depth)
        {
            Widget widget = node as Widget;

            string elementType = widget != null ? widget.StyleElementType : node.GetType().Name;
            string id = widget != null && widget.StyleId != null ? widget.StyleId : string.Empty;
            string classes = widget != null && widget.StyleClasses != null ? string.Join(".", widget.StyleClasses) : string.Empty;

            output.AppendFormat(CultureInfo.InvariantCulture, "depth={0} type={1} id={2} classes={3} pos=({4:F2},{5:F2}) size=({6:F2},{7:F2})",
                depth, elementType, id, classes, node.Position.X, node.Position.Y, node.Size.X, node.Size.Y).AppendLine();

            IWindowContainer container = node as IWindowContainer;

            if (container == null)
                return;

            foreach (WindowObject child in container.Children)
                WalkWidgetTree(output, child, depth + 1);
        }

        private static void CompareOrUpdateWidgetBaseline(TestContext context, string[] actualLines)
        {
            if (UpdateBaselines)
            {
                WriteBaseline(AmaltheaWidgetsBaselinePath, actualLines);
                Console.WriteLine("    Amalthea widgets: baseline written to {0} ({1} line(s))", AmaltheaWidgetsBaselinePath, actualLines.Length);
                return;
            }

            if (!File.Exists(AmaltheaWidgetsBaselinePath))
            {
                context.Fail("Amalthea widgets: baseline file {0} does not exist -- run with --update-baselines to create it", AmaltheaWidgetsBaselinePath);
                return;
            }

            string[] expectedLines = File.ReadAllLines(AmaltheaWidgetsBaselinePath);

            CompareLines(context, "Amalthea widgets", expectedLines, actualLines);
        }
#else
        // Amalthea's widget classes are proprietary and are compiled in only when the csproj is
        // given -p:AmaltheaRoot=<checkout> (see NewWidgets.Test.csproj). Without them the tree
        // cannot be built at all, so the group reports itself skipped rather than asserting
        // nothing and passing. The cached stylesheets tests 40-43 use are unaffected.
        private static void Test44_AmaltheaWidgetTreeBaseline(TestContext context)
        {
            context.Skip("Amalthea widget classes not compiled in -- build with -p:AmaltheaRoot=<SpaceAdventure.Client checkout> to run this group");
        }
#endif
    }
}
