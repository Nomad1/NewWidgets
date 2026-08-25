using System.IO;

using NewWidgets.Utility;
using NewWidgets.Widgets;

namespace NewWidgets.Test
{
    /// <summary>
    /// Tests 92-94: D186's sprite reference, <c>url("ui.svg#window_9")</c>. The file names the
    /// atlas, which only a browser needs, and the fragment names the sprite, which is all this
    /// engine wants.
    ///
    /// The fragment is taken at the sprite lookup, not at the parse, so the style store keeps
    /// the value as it was authored and <c>SaveCSS</c> writes the SVG reference back out
    /// intact -- Test 94 is what holds that in place.
    ///
    /// Every class and sprite name here starts "svg9" so it cannot collide with the other
    /// groups, which share one process-wide style collection.
    /// </summary>
    internal static class SpriteUrlTests
    {
        public static void Register()
        {
            TestRunner.Add("Test 92: an SVG fragment url() resolves to the sprite the fragment names", Test92_FragmentNamesTheSprite);
            TestRunner.Add("Test 93: a plain sprite name url() is unchanged", Test93_PlainNameUnchanged);
            TestRunner.Add("Test 94: saving a stylesheet keeps the SVG reference whole", Test94_SaveKeepsTheFragment);
        }

        /// <summary>
        /// Builds the background of a panel carrying <paramref name="className"/> and hands back
        /// the sprite name the host was asked to cut. A nineimage background with a slice goes
        /// through <c>SetSpriteSubdivision</c>, which the test controller records by source id,
        /// so the name the engine resolved is readable without reaching into the widget.
        /// </summary>
        private static string ResolveBackgroundSprite(TestController controller, string className)
        {
            WidgetPanel panel = new WidgetPanel(WidgetManager.GetStyle(className));
            panel.Relayout();
            panel.Update();

            string target = controller.LastPartSubdivisionTarget;

            if (target == null)
                return null;

            return controller.GetSpritePartsSource(target);
        }

        private static void Test92_FragmentNamesTheSprite(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();

            controller.RegisterSprite("svg92window", 90, 90);

            TestEnvironment.LoadCss(
                ".svg92frag { width: 300px; height: 200px; background-image: url(\"ui.svg#svg92window\"); background-repeat: nineimage; border-image-slice: 30 fill; }");

            context.AreEqual("svg92window", ResolveBackgroundSprite(controller, "svg92frag"),
                "url(\"ui.svg#svg92window\") should look up the sprite named by the fragment");

            // The other property that becomes a sprite lookup: a @font rule's own resource.
            // Loaded separately, so a regression in one of the two paths cannot mask the other.
            controller.RegisterTestFont("svg92glyphs", 10, 16);

            TestEnvironment.LoadCss("@font.svg92font { --font-resource: url(\"ui.svg#svg92glyphs\"); --font-spacing: 0; }");

            context.AreEqual("svg92glyphs", ((TestSprite)WidgetManager.GetFont("svg92font").Sprite).Id,
                "a font resource written as an SVG reference should be cut from the sprite the fragment names");

            // The atlas file must survive in the store, or SaveCSS below would have nothing to write.
            WidgetPanel panel = new WidgetPanel(WidgetManager.GetStyle("svg92frag"));
            panel.Relayout();

            context.AreEqual("ui.svg#svg92window", panel.GetProperty(WidgetParameterIndex.BackImage, ""),
                "the stored background-image should still be the value as authored, fragment and all");
        }

        private static void Test93_PlainNameUnchanged(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();

            controller.RegisterSprite("svg93window", 90, 90);

            TestEnvironment.LoadCss(
                ".svg93plain { width: 300px; height: 200px; background-image: url(\"svg93window\"); background-repeat: nineimage; border-image-slice: 30 fill; }");

            context.AreEqual("svg93window", ResolveBackgroundSprite(controller, "svg93plain"),
                "a url() with no fragment is already a sprite name and must look up unchanged");

            context.AreEqual("svg93window", ConversionHelper.UrlToSpriteName("svg93window"),
                "UrlToSpriteName must leave a fragmentless value alone");
        }

        private static void Test94_SaveKeepsTheFragment(TestContext context)
        {
            TestEnvironment.Setup();

            TestEnvironment.LoadCss(".svg94save { background-image: url(\"ui.svg#svg94window\"); }");

            StringWriter writer = new StringWriter();
            WidgetManager.SaveCSS(writer);
            string saved = writer.ToString();

            context.IsTrue(saved.Contains("url(\"ui.svg#svg94window\")"),
                "SaveCSS must write the SVG reference back whole; stripping the fragment at parse time would have written url(\"svg94window\") and destroyed a stylesheet a browser could read");
        }
    }
}
