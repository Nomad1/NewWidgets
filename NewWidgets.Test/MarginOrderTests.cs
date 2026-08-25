using System;

using NewWidgets.Utility;

namespace NewWidgets.Test
{
    /// <summary>
    /// CSS orders the four sides of a box shorthand clockwise from the top:
    /// top, right, bottom, left. It also accepts one, two and three value forms.
    ///
    /// ConversionHelper.MarginParse reads four values as left, top, right, bottom,
    /// which is the CSS order rotated by one, and it rejects the two and three
    /// value forms outright.
    /// </summary>
    internal static class MarginOrderTests
    {
        public static void Register()
        {
            TestRunner.Add("Test 63: margin shorthand follows CSS order", Test63_FourValueOrder);
            TestRunner.Add("Test 64: margin shorthand accepts two and three values", Test64_TwoAndThreeValues);
            TestRunner.Add("Test 65: rotating a legacy margin preserves its meaning", Test65_RotationPreservesMeaning);
        }

        private static void Test63_FourValueOrder(TestContext context)
        {
            // CSS: top right bottom left
            Margin margin = ConversionHelper.MarginParse("1px 2px 3px 4px", UnitType.Length);

            context.AreEqualFloat(1.0f, margin.Top, 0.001f, "first value is top, got {0}", margin.Top);
            context.AreEqualFloat(2.0f, margin.Right, 0.001f, "second value is right, got {0}", margin.Right);
            context.AreEqualFloat(3.0f, margin.Bottom, 0.001f, "third value is bottom, got {0}", margin.Bottom);
            context.AreEqualFloat(4.0f, margin.Left, 0.001f, "fourth value is left, got {0}", margin.Left);
        }

        private static void Test64_TwoAndThreeValues(TestContext context)
        {
            // Two values: vertical, then horizontal.
            context.DoesNotThrow(ParseTwoValues, "a two value margin must parse");

            // Three values: top, then horizontal, then bottom.
            context.DoesNotThrow(ParseThreeValues, "a three value margin must parse");

            // One value stays as it is: all four sides.
            Margin one = ConversionHelper.MarginParse("5px", UnitType.Length);
            context.AreEqualFloat(5.0f, one.Top, 0.001f, "a single value sets every side, got top {0}", one.Top);
            context.AreEqualFloat(5.0f, one.Left, 0.001f, "a single value sets every side, got left {0}", one.Left);
        }

        private static void ParseTwoValues()
        {
            Margin margin = ConversionHelper.MarginParse("1px 2px", UnitType.Length);

            if (margin.Top != 1.0f || margin.Bottom != 1.0f || margin.Left != 2.0f || margin.Right != 2.0f)
                throw new ApplicationException(string.Format("two value form should be vertical then horizontal, got {0}", margin));
        }

        private static void ParseThreeValues()
        {
            Margin margin = ConversionHelper.MarginParse("1px 2px 3px", UnitType.Length);

            if (margin.Top != 1.0f || margin.Right != 2.0f || margin.Left != 2.0f || margin.Bottom != 3.0f)
                throw new ApplicationException(string.Format("three value form should be top, horizontal, bottom, got {0}", margin));
        }

        /// <summary>
        /// The migration for stylesheets written against the old order. A legacy
        /// declaration "a b c d" meant left a, top b, right c, bottom d. Rotating it
        /// left by one gives "b c d a", which under CSS order means top b, right c,
        /// bottom d, left a. The same four sides get the same four numbers, so the
        /// rendering is unchanged and the hand-set values are preserved.
        /// </summary>
        private static void Test65_RotationPreservesMeaning(TestContext context)
        {
            // Real declarations taken from Amalthea's stylesheets.
            string[] legacy = new string[] { "1px 2px 3px 2px", "6px 2px 6px 0px", "26px 38px 7px 3px", "-30px 12px 0px 0px" };

            for (int i = 0; i < legacy.Length; i++)
            {
                string[] parts = legacy[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                // What the old parser produced: left, top, right, bottom in written order.
                float oldLeft = ConversionHelper.FloatParse(parts[0], UnitType.Length);
                float oldTop = ConversionHelper.FloatParse(parts[1], UnitType.Length);
                float oldRight = ConversionHelper.FloatParse(parts[2], UnitType.Length);
                float oldBottom = ConversionHelper.FloatParse(parts[3], UnitType.Length);

                string rotated = string.Format("{0} {1} {2} {3}", parts[1], parts[2], parts[3], parts[0]);
                Margin migrated = ConversionHelper.MarginParse(rotated, UnitType.Length);

                context.AreEqualFloat(oldTop, migrated.Top, 0.001f, "{0} rotated to {1}: top should stay {2}, got {3}", legacy[i], rotated, oldTop, migrated.Top);
                context.AreEqualFloat(oldRight, migrated.Right, 0.001f, "{0} rotated to {1}: right should stay {2}, got {3}", legacy[i], rotated, oldRight, migrated.Right);
                context.AreEqualFloat(oldBottom, migrated.Bottom, 0.001f, "{0} rotated to {1}: bottom should stay {2}, got {3}", legacy[i], rotated, oldBottom, migrated.Bottom);
                context.AreEqualFloat(oldLeft, migrated.Left, 0.001f, "{0} rotated to {1}: left should stay {2}, got {3}", legacy[i], rotated, oldLeft, migrated.Left);
            }
        }
    }
}
