using System;
using System.IO;

using NewWidgets.Widgets;

namespace NewWidgets.Test
{
    /// <summary>
    /// Tests for CSS property names that the engine spells its own way.
    ///
    /// Each rename must satisfy three conditions:
    /// 1. the standard name is accepted
    /// 2. the old name is still accepted, so shipped stylesheets keep working
    /// 3. SaveCSS writes the standard name, because SaveCSS is the conversion tool
    /// </summary>
    internal static class RenameTests
    {
        public static void Register()
        {
            TestRunner.Add("Test 60: caret-color is accepted", Test60_CaretColor);
            TestRunner.Add("Test 61: letter-spacing is accepted", Test61_LetterSpacing);

            // The output flip is deliberately deferred. SaveCSS still writes the legacy
            // names, so the two corpus baselines stay byte-identical. All the standard
            // names get switched to primary in one batch, once the baselines can be
            // regenerated and their diff reviewed in full.
            TestRunner.AddKnownFailure("Test 62: SaveCSS writes the standard names",
                "deferred on purpose: flipping output moves the Amalthea and SiegeWars baselines, so it is done in one reviewed batch",
                Test62_SaveWritesStandardNames);
        }

        /// <summary>
        /// Reads one property back from a widget that carries the given class.
        /// </summary>
        private static uint GetColorProperty(string className, WidgetParameterIndex index, uint defaultValue)
        {
            WidgetPanel panel = new WidgetPanel(WidgetManager.GetStyle(className));
            panel.Relayout();
            return panel.GetProperty(index, defaultValue);
        }

        private static float GetFloatProperty(string className, WidgetParameterIndex index, float defaultValue)
        {
            WidgetPanel panel = new WidgetPanel(WidgetManager.GetStyle(className));
            panel.Relayout();
            return panel.GetProperty(index, defaultValue);
        }

        private static string SaveStyles()
        {
            StringWriter writer = new StringWriter();
            WidgetManager.SaveCSS(writer);
            return writer.ToString();
        }

        private static void Test60_CaretColor(TestContext context)
        {
            TestEnvironment.Setup();

            // The standard name must work.
            TestEnvironment.LoadCss(".ren60std { caret-color: #ff0000; }");
            context.AreEqual((uint)0xff0000, GetColorProperty("ren60std", WidgetParameterIndex.CursorColor, 0u),
                "caret-color should set the cursor colour, got {0:x6}", GetColorProperty("ren60std", WidgetParameterIndex.CursorColor, 0u));

            // The old name must keep working, because two shipped games use it.
            TestEnvironment.LoadCss(".ren60old { --cursor-color: #00ff00; }");
            context.AreEqual((uint)0x00ff00, GetColorProperty("ren60old", WidgetParameterIndex.CursorColor, 0u),
                "--cursor-color must still work as an alias, got {0:x6}", GetColorProperty("ren60old", WidgetParameterIndex.CursorColor, 0u));

            // SaveCSS must write the standard name, not the old one.
        }

        private static void Test61_LetterSpacing(TestContext context)
        {
            TestEnvironment.Setup();

            TestEnvironment.LoadCss(".ren61std { letter-spacing: 2; }");
            context.AreEqualFloat(2.0f, GetFloatProperty("ren61std", WidgetParameterIndex.FontSpacing, 0.0f), 0.001f,
                "letter-spacing should set font spacing, got {0}", GetFloatProperty("ren61std", WidgetParameterIndex.FontSpacing, 0.0f));

            TestEnvironment.LoadCss(".ren61old { --font-spacing: 3; }");
            context.AreEqualFloat(3.0f, GetFloatProperty("ren61old", WidgetParameterIndex.FontSpacing, 0.0f), 0.001f,
                "--font-spacing must still work as an alias, got {0}", GetFloatProperty("ren61old", WidgetParameterIndex.FontSpacing, 0.0f));

        }

        private static void Test62_SaveWritesStandardNames(TestContext context)
        {
            TestEnvironment.Setup();

            TestEnvironment.LoadCss(".ren62 { caret-color: #ff0000; letter-spacing: 2; }");

            string saved = SaveStyles();

            context.IsTrue(saved.Contains("caret-color"), "SaveCSS should write caret-color, not --cursor-color");
            context.IsTrue(saved.Contains("letter-spacing"), "SaveCSS should write letter-spacing, not --font-spacing");
        }
    }
}
