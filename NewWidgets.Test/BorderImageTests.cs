using System;
using System.Drawing;
using System.Numerics;

using NewWidgets.UI;
using NewWidgets.UI.Styles;
using NewWidgets.Widgets;

namespace NewWidgets.Test
{
    /// <summary>
    /// Tests 80-84: the nine-patch renderer cut at an arbitrary <c>border-image-slice</c>
    /// instead of always at thirds (D130), with the edges stretching and never repeating
    /// (D139). Tests 95-96: the stylesheet scan that reads a thirds slice back as the uniform
    /// cut the two patch renderers draw with, which is what replaced <c>@sprite</c>.
    ///
    /// Every class, id and sprite name here starts "nine8" or "nine9" so it cannot collide
    /// with the other groups, which share one process-wide style collection.
    /// </summary>
    internal static class BorderImageTests
    {
        private const float Tolerance = 0.01f;

        /// <summary>
        /// Reaches the background pieces <see cref="WidgetBackground"/> builds, which are
        /// protected, and builds them on demand rather than through a frame update.
        /// </summary>
        private class BackgroundProbe : WidgetPanel
        {
            public new const string ElementType = "panel";
            //

            public WindowObject[] Pieces
            {
                get { return m_background.List; }
            }

            public BackgroundProbe(WidgetStyle style)
                : base(ElementType, style)
            {
            }

            public void BuildBackground()
            {
                Relayout();
                UpdateBackground();
            }
        }

        public static void Register()
        {
            TestRunner.Add("Test 80: an asymmetric border-image-slice cuts the source exactly", Test80_AsymmetricSliceCutsSource);
            TestRunner.Add("Test 81: corners keep their size, edges and centre stretch", Test81_CornersEdgesAndCentre);
            TestRunner.Add("Test 82: the fill keyword controls the centre piece", Test82_FillControlsCentre);
            TestRunner.Add("Test 83: a percentage slice and a number slice differ", Test83_PercentageAndNumberSlices);
            TestRunner.Add("Test 84: nineimage without a slice keeps the thirds it has today", Test84_NoSliceKeepsThirds);
            TestRunner.Add("Test 95: many rules naming one sprite cut it exactly once", Test95_OneCutPerSprite);
            TestRunner.Add("Test 96: a conflicting or unsupported slice is reported, not cut", Test96_ConflictIsReported);
        }

        // ------------------------------------------------------------------
        // Test 80: the rectangles handed to the host seam
        // ------------------------------------------------------------------

        private static void Test80_AsymmetricSliceCutsSource(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();

            controller.RegisterSprite("nine80sprite", 100, 100);

            // border-image-slice is top right bottom left, so this is top 10, right 20,
            // bottom 30, left 40, in source pixels of a 100x100 source.
            TestEnvironment.LoadCss(
                ".nine80asym { width: 300px; height: 200px; background-image: url(\"nine80sprite\"); background-repeat: nineimage; border-image-slice: 10 20 30 40 fill; }");

            int callsBefore = controller.PartSubdivisionCount;

            BackgroundProbe probe = new BackgroundProbe(WidgetManager.GetStyle("nine80asym"));
            probe.BuildBackground();

            context.AreEqual(callsBefore + 1, controller.PartSubdivisionCount,
                "a nineimage background with a border-image-slice should hand the host exactly one arbitrary subdivision, got {0} call(s)", controller.PartSubdivisionCount - callsBefore);

            string target = controller.LastPartSubdivisionTarget;
            context.IsNotNull(target, "the seam should have been called, so a target sprite id should have been recorded");

            if (target == null)
                return;

            context.AreEqual("nine80sprite", controller.GetSpritePartsSource(target),
                "the cut sprite should be cut from the background-image sprite, got {0}", controller.GetSpritePartsSource(target));

            RectangleF[] parts = controller.GetSpriteParts(target);
            context.IsNotNull(parts, "the seam should have been handed a rectangle array");

            if (parts == null)
                return;

            context.AreEqual(9, parts.Length, "a nine-patch is nine parts, got {0}", parts.Length);

            if (parts.Length != 9)
                return;

            // Columns are 40, 40 and 20 source pixels wide; rows are 10, 60 and 30 tall.
            // Normalized against a 100x100 source that is 0.4/0.4/0.2 and 0.1/0.6/0.3.
            float[] expectedX = new float[] { 0.0f, 0.4f, 0.8f };
            float[] expectedWidth = new float[] { 0.4f, 0.4f, 0.2f };
            float[] expectedY = new float[] { 0.0f, 0.1f, 0.7f };
            float[] expectedHeight = new float[] { 0.1f, 0.6f, 0.3f };

            for (int row = 0; row < 3; row++)
                for (int column = 0; column < 3; column++)
                {
                    int index = row * 3 + column;
                    RectangleF part = parts[index];

                    context.AreEqualFloat(expectedX[column], part.X, 0.001f, "part {0} should start at x {1}, got {2}", index, expectedX[column], part.X);
                    context.AreEqualFloat(expectedY[row], part.Y, 0.001f, "part {0} should start at y {1}, got {2}", index, expectedY[row], part.Y);
                    context.AreEqualFloat(expectedWidth[column], part.Width, 0.001f, "part {0} should be {1} wide, got {2}", index, expectedWidth[column], part.Width);
                    context.AreEqualFloat(expectedHeight[row], part.Height, 0.001f, "part {0} should be {1} tall, got {2}", index, expectedHeight[row], part.Height);
                }

            // The nine parts must tile the source: no gaps, no overlaps, full coverage.
            float area = 0;
            for (int i = 0; i < parts.Length; i++)
                area += parts[i].Width * parts[i].Height;

            context.AreEqualFloat(1.0f, area, 0.001f, "the nine parts should cover the whole source, total area is {0}", area);

            for (int i = 0; i < parts.Length; i++)
                for (int j = i + 1; j < parts.Length; j++)
                    context.IsFalse(Overlaps(parts[i], parts[j]), "parts {0} and {1} should not overlap, got {2} and {3}", i, j, parts[i], parts[j]);
        }

        // ------------------------------------------------------------------
        // Test 81: where the nine pieces land and how far each one stretches
        // ------------------------------------------------------------------

        private static void Test81_CornersEdgesAndCentre(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();

            controller.RegisterSprite("nine81sprite", 100, 100);

            // The sprite also carries the old uniform 3x3 cut, which is what a style being
            // migrated looks like: the slice must win over it. Without the slice path this
            // group fails on every geometry assertion with the thirds of a 100px source.
            // Registered through the host seam directly, because a stylesheet can no longer
            // ask for a cut without also asking for the patch that reads it.
            controller.SetSpriteSubdivision("nine81sprite", 3, 3);

            TestEnvironment.LoadCss(
                ".nine81asym { width: 300px; height: 200px; background-image: url(\"nine81sprite\"); background-repeat: nineimage; border-image-slice: 10 20 30 40 fill; }");

            BackgroundProbe probe = new BackgroundProbe(WidgetManager.GetStyle("nine81asym"));
            probe.BuildBackground();

            WindowObject[] pieces = probe.Pieces;

            context.AreEqual(9, pieces.Length, "a filled nine-patch should draw nine pieces, got {0}", pieces.Length);

            if (pieces.Length != 9)
                return;

            // Source cells are 40/40/20 by 10/60/30. In a 300x200 box the corners keep those
            // sizes, so the middle column is 300 - 40 - 20 == 240 wide and the middle row is
            // 200 - 10 - 30 == 160 tall.
            float[] expectedX = new float[] { 0.0f, 40.0f, 280.0f };
            float[] expectedY = new float[] { 0.0f, 10.0f, 170.0f };
            float[] expectedWidth = new float[] { 40.0f, 240.0f, 20.0f };
            float[] expectedHeight = new float[] { 10.0f, 160.0f, 30.0f };
            float[] sourceWidth = new float[] { 40.0f, 40.0f, 20.0f };
            float[] sourceHeight = new float[] { 10.0f, 60.0f, 30.0f };

            for (int row = 0; row < 3; row++)
                for (int column = 0; column < 3; column++)
                {
                    int index = row * 3 + column;
                    ImageObject piece = (ImageObject)pieces[index];

                    context.AreEqualFloat(expectedX[column], piece.Position.X, Tolerance, "piece {0} should sit at x {1}, got {2}", index, expectedX[column], piece.Position.X);
                    context.AreEqualFloat(expectedY[row], piece.Position.Y, Tolerance, "piece {0} should sit at y {1}, got {2}", index, expectedY[row], piece.Position.Y);

                    context.AreEqualFloat(sourceWidth[column], piece.Sprite.FrameSize.X, Tolerance, "piece {0} should come from a source cell {1} wide, got {2}", index, sourceWidth[column], piece.Sprite.FrameSize.X);
                    context.AreEqualFloat(sourceHeight[row], piece.Sprite.FrameSize.Y, Tolerance, "piece {0} should come from a source cell {1} tall, got {2}", index, sourceHeight[row], piece.Sprite.FrameSize.Y);

                    Vector2 drawn = piece.Sprite.FrameSize * piece.Transform.FlatScale;

                    context.AreEqualFloat(expectedWidth[column], drawn.X, Tolerance, "piece {0} should draw {1} wide, got {2}", index, expectedWidth[column], drawn.X);
                    context.AreEqualFloat(expectedHeight[row], drawn.Y, Tolerance, "piece {0} should draw {1} tall, got {2}", index, expectedHeight[row], drawn.Y);
                }

            // Read back as the three cases the layout exists for.
            ImageObject topLeft = (ImageObject)pieces[0];
            context.AreEqualFloat(1.0f, topLeft.Transform.FlatScale.X, Tolerance, "a corner must not stretch horizontally, got a scale of {0}", topLeft.Transform.FlatScale.X);
            context.AreEqualFloat(1.0f, topLeft.Transform.FlatScale.Y, Tolerance, "a corner must not stretch vertically, got a scale of {0}", topLeft.Transform.FlatScale.Y);

            ImageObject topEdge = (ImageObject)pieces[1];
            context.AreEqualFloat(6.0f, topEdge.Transform.FlatScale.X, Tolerance, "the top edge should stretch 240/40 == 6 horizontally, got {0}", topEdge.Transform.FlatScale.X);
            context.AreEqualFloat(1.0f, topEdge.Transform.FlatScale.Y, Tolerance, "the top edge must not stretch vertically, got a scale of {0}", topEdge.Transform.FlatScale.Y);

            ImageObject leftEdge = (ImageObject)pieces[3];
            context.AreEqualFloat(1.0f, leftEdge.Transform.FlatScale.X, Tolerance, "the left edge must not stretch horizontally, got a scale of {0}", leftEdge.Transform.FlatScale.X);
            context.AreEqualFloat(160.0f / 60.0f, leftEdge.Transform.FlatScale.Y, Tolerance, "the left edge should stretch 160/60 vertically, got {0}", leftEdge.Transform.FlatScale.Y);

            ImageObject centre = (ImageObject)pieces[4];
            context.AreEqualFloat(6.0f, centre.Transform.FlatScale.X, Tolerance, "the centre should stretch horizontally, got a scale of {0}", centre.Transform.FlatScale.X);
            context.AreEqualFloat(160.0f / 60.0f, centre.Transform.FlatScale.Y, Tolerance, "the centre should stretch vertically, got a scale of {0}", centre.Transform.FlatScale.Y);
        }

        // ------------------------------------------------------------------
        // Test 82: the fill keyword
        // ------------------------------------------------------------------

        private static void Test82_FillControlsCentre(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();

            controller.RegisterSprite("nine82sprite", 100, 100);

            TestEnvironment.LoadCss(
                ".nine82filled { width: 300px; height: 200px; background-image: url(\"nine82sprite\"); background-repeat: nineimage; border-image-slice: 10 20 30 40 fill; }" +
                ".nine82hollow { width: 300px; height: 200px; background-image: url(\"nine82sprite\"); background-repeat: nineimage; border-image-slice: 10 20 30 40; }");

            BackgroundProbe filled = new BackgroundProbe(WidgetManager.GetStyle("nine82filled"));
            filled.BuildBackground();

            context.AreEqual(9, filled.Pieces.Length, "with the fill keyword the centre is drawn, so nine pieces, got {0}", filled.Pieces.Length);

            BackgroundProbe hollow = new BackgroundProbe(WidgetManager.GetStyle("nine82hollow"));
            hollow.BuildBackground();

            context.AreEqual(8, hollow.Pieces.Length, "without the fill keyword the centre is not drawn, so eight pieces, got {0}", hollow.Pieces.Length);

            // The eight that remain are the border, so nothing may sit inside it: the frame
            // the centre would have used is 40,10 to 260,170.
            for (int i = 0; i < hollow.Pieces.Length; i++)
            {
                Vector2 position = hollow.Pieces[i].Position;
                bool isInsideTheBorder = position.X > 40.0f && position.X < 260.0f && position.Y > 10.0f && position.Y < 170.0f;

                context.IsFalse(isInsideTheBorder, "piece {0} of an unfilled nine-patch sits inside the border at {1}", i, position);
            }
        }

        // ------------------------------------------------------------------
        // Test 83: number versus percentage
        // ------------------------------------------------------------------

        private static void Test83_PercentageAndNumberSlices(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();

            // A 200x200 source, so a quarter of it is 50 pixels and cannot be confused with
            // the bare number 25.
            controller.RegisterSprite("nine83sprite", 200, 200);

            TestEnvironment.LoadCss(
                ".nine83percent { width: 400px; height: 400px; background-image: url(\"nine83sprite\"); background-repeat: nineimage; border-image-slice: 25% fill; }" +
                ".nine83number { width: 400px; height: 400px; background-image: url(\"nine83sprite\"); background-repeat: nineimage; border-image-slice: 25 fill; }");

            BackgroundProbe percent = new BackgroundProbe(WidgetManager.GetStyle("nine83percent"));
            percent.BuildBackground();

            ImageObject percentCorner = (ImageObject)percent.Pieces[0];

            context.AreEqualFloat(50.0f, percentCorner.Sprite.FrameSize.X, Tolerance,
                "border-image-slice: 25% of a 200px source is a 50px corner, got {0}", percentCorner.Sprite.FrameSize.X);
            context.AreEqualFloat(50.0f, percentCorner.Sprite.FrameSize.Y, Tolerance,
                "border-image-slice: 25% of a 200px source is a 50px corner, got {0}", percentCorner.Sprite.FrameSize.Y);

            BackgroundProbe number = new BackgroundProbe(WidgetManager.GetStyle("nine83number"));
            number.BuildBackground();

            ImageObject numberCorner = (ImageObject)number.Pieces[0];

            context.AreEqualFloat(25.0f, numberCorner.Sprite.FrameSize.X, Tolerance,
                "border-image-slice: 25 is 25 source pixels whatever the source measures, got {0}", numberCorner.Sprite.FrameSize.X);
            context.AreEqualFloat(25.0f, numberCorner.Sprite.FrameSize.Y, Tolerance,
                "border-image-slice: 25 is 25 source pixels whatever the source measures, got {0}", numberCorner.Sprite.FrameSize.Y);

            // Both are laid out in a 400x400 box, so the two must not land in the same place.
            context.AreEqualFloat(350.0f, ((ImageObject)percent.Pieces[2]).Position.X, Tolerance,
                "a 50px corner puts the top right piece at 400 - 50, got {0}", ((ImageObject)percent.Pieces[2]).Position.X);
            context.AreEqualFloat(375.0f, ((ImageObject)number.Pieces[2]).Position.X, Tolerance,
                "a 25px corner puts the top right piece at 400 - 25, got {0}", ((ImageObject)number.Pieces[2]).Position.X);
        }

        // ------------------------------------------------------------------
        // Test 84: regression, the thirds path two shipped games render with
        // ------------------------------------------------------------------

        private static void Test84_NoSliceKeepsThirds(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();

            controller.RegisterSprite("nine84sprite", 90, 90);

            // The thirds cut this group is about, registered through the host seam directly:
            // the rule below is the legacy spelling, which names no slice for the stylesheet
            // scan to read.
            controller.SetSpriteSubdivision("nine84sprite", 3, 3);

            TestEnvironment.LoadCss(
                ".nine84thirds { width: 300px; height: 200px; background-image: url(\"nine84sprite\"); background-repeat: nineimage; }");

            int callsBefore = controller.PartSubdivisionCount;

            BackgroundProbe probe = new BackgroundProbe(WidgetManager.GetStyle("nine84thirds"));
            probe.BuildBackground();

            context.AreEqual(callsBefore, controller.PartSubdivisionCount,
                "a nineimage background without a border-image-slice must not reach the arbitrary-rectangle seam at all, got {0} extra call(s)", controller.PartSubdivisionCount - callsBefore);

            WindowObject[] pieces = probe.Pieces;

            context.AreEqual(9, pieces.Length, "the thirds path draws nine pieces including the centre, got {0}", pieces.Length);

            if (pieces.Length != 9)
                return;

            // 90x90 cut in thirds is 30x30 a cell. In a 300x200 box that leaves 240 and 140
            // for the middle column and row, exactly as this path has always computed them.
            float[] expectedX = new float[] { 0.0f, 30.0f, 270.0f };
            float[] expectedY = new float[] { 0.0f, 30.0f, 170.0f };
            float[] expectedScaleX = new float[] { 1.0f, 8.0f, 1.0f };
            float[] expectedScaleY = new float[] { 1.0f, 140.0f / 30.0f, 1.0f };

            for (int row = 0; row < 3; row++)
                for (int column = 0; column < 3; column++)
                {
                    int index = row * 3 + column;
                    ImageObject piece = (ImageObject)pieces[index];

                    context.AreEqualFloat(expectedX[column], piece.Position.X, Tolerance, "piece {0} should sit at x {1}, got {2}", index, expectedX[column], piece.Position.X);
                    context.AreEqualFloat(expectedY[row], piece.Position.Y, Tolerance, "piece {0} should sit at y {1}, got {2}", index, expectedY[row], piece.Position.Y);
                    context.AreEqualFloat(expectedScaleX[column], piece.Transform.FlatScale.X, Tolerance, "piece {0} should scale {1} horizontally, got {2}", index, expectedScaleX[column], piece.Transform.FlatScale.X);
                    context.AreEqualFloat(expectedScaleY[row], piece.Transform.FlatScale.Y, Tolerance, "piece {0} should scale {1} vertically, got {2}", index, expectedScaleY[row], piece.Transform.FlatScale.Y);
                    context.AreEqual(index, piece.Sprite.Frame, "piece {0} should draw frame {0} of the uniformly subdivided sprite, got {1}", index, piece.Sprite.Frame);
                }
        }

        // ------------------------------------------------------------------
        // Test 95: the stylesheet scan cuts each sprite once
        // ------------------------------------------------------------------

        /// <summary>
        /// border-image-slice is declared per rule, and a skin names the same sprite from a
        /// handful of rules -- a control and its hover, focus and disabled states. The host
        /// seam registers the pieces under the source sprite's own name, so a second cut reads
        /// the already-cut sprite, takes frame 0 (the top-left ninth) and cuts that into nine.
        /// One warning line, and the nine-patch is gone. So the scan must recognise the repeat
        /// rather than obey it.
        ///
        /// The three rules also spell the third three different ways, and one of them names
        /// the sprite through an SVG fragment, because all four are the same cut.
        /// </summary>
        private static void Test95_OneCutPerSprite(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();
            controller.ClearLog();

            controller.RegisterSprite("nine95sprite", 90, 90);

            int callsBefore = controller.SubdivisionCount;

            TestEnvironment.LoadCss(
                ".nine95a { border-image-source: url(\"nine95sprite\"); border-image-slice: 33.3333% fill; }" +
                ".nine95a:hover { border-image-source: url(\"nine95sprite\"); border-image-slice: 33.33% fill; }" +
                ".nine95b { border-image-source: url(\"ui.svg#nine95sprite\"); border-image-slice: 33.4% fill; }");

            context.AreEqual(callsBefore + 1, controller.SubdivisionCount,
                "three rules naming one sprite should cut it exactly once, got {0} call(s)", controller.SubdivisionCount - callsBefore);

            context.AreEqual(0, controller.Errors.Count, "three rules asking for the same 3x3 cut is the normal case and must be silent, got: {0}", Join(controller.Errors));

            ISprite sprite = WindowController.Instance.CreateSprite("nine95sprite");

            context.AreEqual(9, sprite.FrameCount, "the sprite should be cut 3x3 into nine frames, got {0}", sprite.FrameCount);
            context.AreEqualFloat(30.0f, sprite.FrameSize.X, Tolerance, "a 90px sprite cut in thirds has 30px frames, got {0}", sprite.FrameSize.X);

            // and the cut a rule asked for is the one the renderer draws with
            WidgetStyleSheet style = WidgetManager.GetStyle(new StyleSelector(null, new string[] { "nine95a" }, null));

            context.AreEqual(WidgetBackgroundStyle.NineImage, style.Get<WidgetBackgroundStyle>(WidgetParameterIndex.BackStyle, WidgetBackgroundStyle.None),
                "border-image-slice: 33.3333% fill should select the nine-patch renderer");
            context.AreEqual("nine95sprite", style.Get<string>(WidgetParameterIndex.BackImage, ""),
                "border-image-source should name the background sprite, with no background-image declared anywhere");
        }

        // ------------------------------------------------------------------
        // Test 96: what the scan refuses to do
        // ------------------------------------------------------------------

        /// <summary>
        /// Two grids for one sprite cannot both be honoured, and picking one in silence is how
        /// a nine-patch turns into nine slivers of its own corner. The first cut stands and the
        /// disagreement is reported.
        ///
        /// A vertical slice -- <c>33.3333% 0</c> -- is the other refusal: <c>ThreeImage</c>
        /// walks three frames along x and there is no vertical variant (D193), so the sprite is
        /// left whole rather than cut into a grid nothing can draw.
        /// </summary>
        private static void Test96_ConflictIsReported(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();
            controller.ClearLog();

            controller.RegisterSprite("nine96sprite", 90, 90);
            controller.RegisterSprite("nine96vertical", 90, 90);

            int callsBefore = controller.SubdivisionCount;

            TestEnvironment.LoadCss(
                ".nine96a { border-image-source: url(\"nine96sprite\"); border-image-slice: 33.3333% fill; }" +
                ".nine96b { border-image-source: url(\"nine96sprite\"); border-image-slice: 0 33.3333% fill; }" +
                ".nine96v { border-image-source: url(\"nine96vertical\"); border-image-slice: 33.3333% 0 fill; }");

            context.AreEqual(callsBefore + 1, controller.SubdivisionCount,
                "the conflicting second grid and the vertical slice must not reach the host, so exactly one cut, got {0}", controller.SubdivisionCount - callsBefore);

            ISprite conflicted = WindowController.Instance.CreateSprite("nine96sprite");
            context.AreEqual(9, conflicted.FrameCount, "the first cut of a disputed sprite stands, so it is still 3x3, got {0} frame(s)", conflicted.FrameCount);

            ISprite vertical = WindowController.Instance.CreateSprite("nine96vertical");
            context.AreEqual(1, vertical.FrameCount, "a vertical slice is a patch this engine cannot draw, so the sprite is left whole, got {0} frame(s)", vertical.FrameCount);

            context.AreEqual(2, controller.Errors.Count, "both refusals must be reported, got: {0}", Join(controller.Errors));

            context.IsTrue(Join(controller.Errors).Contains("nine96sprite"), "the conflict report must name the sprite, got: {0}", Join(controller.Errors));
            context.IsTrue(Join(controller.Errors).Contains("nine96vertical"), "the vertical-slice report must name the sprite, got: {0}", Join(controller.Errors));
        }

        private static string Join(System.Collections.Generic.IList<string> lines)
        {
            return string.Join(" | ", lines);
        }

        // Overlap by more than a normalized thousandth of the source, i.e. a real overlap
        // rather than the last bit of a float: 0.1f + 0.6f is not exactly 0.7f, so two rows
        // that meet exactly still cross by about 6e-8.
        private static bool Overlaps(RectangleF first, RectangleF second)
        {
            return first.X + 0.001f < second.X + second.Width && second.X + 0.001f < first.X + first.Width &&
                   first.Y + 0.001f < second.Y + second.Height && second.Y + 0.001f < first.Y + first.Height;
        }
    }
}
