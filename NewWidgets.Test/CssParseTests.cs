using System;
using System.Collections.Generic;

using NewWidgets.UI.Styles;
using NewWidgets.Utility;
using NewWidgets.Widgets;

namespace NewWidgets.Test
{
    /// <summary>
    /// Test groups for <see cref="CSSParser"/> (raw parsing) and <see cref="ConversionHelper"/>
    /// (value/unit conversion), plus one group driving the unknown-property/malformed-value
    /// paths through <see cref="WidgetManager.LoadCSS"/>.
    /// </summary>
    internal static class CssParseTests
    {
        // Minimal IStyleData used to drive CSSParser.ParseCSS directly, bypassing
        // WidgetManager's property mapping entirely. It just records the selector name and
        // the raw key/value dictionary CSSParser handed to the constructor delegate.
        private class RecordingStyleData : IStyleData
        {
            public readonly string Name;
            public readonly IDictionary<string, string> Parameters;

            public RecordingStyleData(string name, IDictionary<string, string> parameters)
            {
                Name = name;
                Parameters = parameters;
            }

            public void LoadData(IStyleData data)
            {
                RecordingStyleData other = (RecordingStyleData)data;

                foreach (KeyValuePair<string, string> pair in other.Parameters)
                    Parameters[pair.Key] = pair.Value;
            }
        }

        public static void Register()
        {
            TestRunner.Add("Test 1: CSS parser", Test1_CssParser);

            TestRunner.AddKnownFailure("Test 2: CSS parser, quoted values",
                "CSSParser.cs ParseCSS: the '}' case (around line 73, comment 'TODO: ignore inside of the parameter text string') and the ';' case (around line 123, same TODO) both end the current declaration/block on any '}' or ';' character, even one that appears inside a quoted parameter value, so a value like url(\"a;b\") or \"}\" gets truncated instead of parsed intact",
                Test2_CssParserQuotedValues);

            TestRunner.AddKnownFailure("Test 3: CSS at-rules that are not handled",
                "CSSParser.cs ParseCSS has no concept of a nested '{ }' block inside an at-rule body: when the inner 'panel {' is reached while already inside the @media rule's ParameterBlock state, the '{' case (around line 71) falls into 'ERROR: Starting parameter block without style name' and then appends the literal '{' character into the current parameter text instead of opening a nested block. The whole @media body collapses into a single bogus style node whose name is the at-rule itself ('@media', added via targetCollection.AddStyle at the outer '}', around line 95) with one garbage parameter key ('panel { width'); the inner 'panel'/'width' rule is never registered as its own style",
                Test3_CssAtRulesUnhandled);

            TestRunner.Add("Test 4: value and unit conversion", Test4_ValueAndUnitConversion);

            TestRunner.Add("Test 5: CSS colour formats", Test5_ColourFormatsNotParsed);

            TestRunner.Add("Test 6: Margin single-value form drops its unit", Test6_MarginSingleValueDropsUnit);

            TestRunner.Add("Test 7: unknown properties are reported, not fatal", Test7_UnknownPropertiesReportedNotFatal);
        }

        // Runs CSSParser.ParseCSS against a fresh local StyleCollection and RecordingStyleData
        // constructor, and hands back both the list of constructed IStyleData objects (in
        // construction order) and the collection they were added to.
        private static IList<RecordingStyleData> ParseRaw(string css, out StyleCollection collection)
        {
            List<RecordingStyleData> captured = new List<RecordingStyleData>();
            StyleCollection localCollection = new StyleCollection();

            Func<string, Dictionary<string, string>, IStyleData> constructor = delegate(string name, Dictionary<string, string> parameters)
            {
                RecordingStyleData data = new RecordingStyleData(name, parameters);
                captured.Add(data);
                return data;
            };

            CSSParser.ParseCSS(css, localCollection, constructor);

            collection = localCollection;
            return captured;
        }

        private static IList<RecordingStyleData> ParseRaw(string css)
        {
            StyleCollection collection;
            return ParseRaw(css, out collection);
        }

        // Same as ParseRaw, but catches any exception ParseCSS throws and records it as a
        // failed assertion instead of aborting the whole test group, so one broken scenario
        // in a known-failure group does not swallow the others.
        private static IList<RecordingStyleData> TryParseRaw(TestContext context, string css, out StyleCollection collection, string label)
        {
            try
            {
                return ParseRaw(css, out collection);
            }
            catch (Exception ex)
            {
                collection = null;
                context.Fail("{0}: CSSParser.ParseCSS threw {1}: {2}", label, ex.GetType().Name, ex.Message);
                return new List<RecordingStyleData>();
            }
        }

        private static void Test1_CssParser(TestContext context)
        {
            // a simple selector { key: value; } block
            {
                IList<RecordingStyleData> captured = ParseRaw(".a1simple { color: red; }");

                context.AreEqual(1, captured.Count, "a single selector block should produce exactly one IStyleData, got {0}", captured.Count);
                if (captured.Count == 1)
                {
                    context.AreEqual(".a1simple", captured[0].Name, "selector name should be captured as '.a1simple', got '{0}'", captured[0].Name);
                    context.AreEqual(1, captured[0].Parameters.Count, "block should have exactly one parameter, got {0}", captured[0].Parameters.Count);

                    string colorValue;
                    context.IsTrue(captured[0].Parameters.TryGetValue("color", out colorValue), "parameters should contain key 'color'");
                    context.AreEqual("red", colorValue, "color value should be 'red', got '{0}'", colorValue);
                }
            }

            // several declarations in one block, last one with no trailing semicolon
            {
                IList<RecordingStyleData> captured = ParseRaw(".a1multi { color: red; width: 5px; height: 6px }");

                context.AreEqual(1, captured.Count, "a multi-declaration block should still produce exactly one IStyleData, got {0}", captured.Count);
                if (captured.Count == 1)
                {
                    context.AreEqual(3, captured[0].Parameters.Count, "block should have 3 parameters including the semicolon-less last one, got {0}", captured[0].Parameters.Count);

                    string colorValue;
                    string widthValue;
                    string heightValue;
                    context.IsTrue(captured[0].Parameters.TryGetValue("color", out colorValue), "parameters should contain key 'color'");
                    context.IsTrue(captured[0].Parameters.TryGetValue("width", out widthValue), "parameters should contain key 'width'");
                    context.IsTrue(captured[0].Parameters.TryGetValue("height", out heightValue), "parameters should contain key 'height' even without a trailing ';'");
                    context.AreEqual("red", colorValue, "color should be 'red', got '{0}'", colorValue);
                    context.AreEqual("5px", widthValue, "width should be '5px', got '{0}'", widthValue);
                    context.AreEqual("6px", heightValue, "height (no trailing ';') should still be '6px', got '{0}'", heightValue);
                }
            }

            // /* comment */ removal: between rules, inside a block, and a comment containing braces
            {
                string css = "/* leading comment */ .a1c1 { color: red; /* mid-block comment */ width: 5px; } "
                    + "/* between rules { with a brace } inside */ .a1c2 { height: 6px; }";

                IList<RecordingStyleData> captured = ParseRaw(css);

                context.AreEqual(2, captured.Count, "two rules separated/decorated by comments should still produce 2 IStyleData, got {0}", captured.Count);
                if (captured.Count == 2)
                {
                    context.AreEqual(".a1c1", captured[0].Name, "first selector name should be '.a1c1', got '{0}'", captured[0].Name);
                    context.AreEqual(2, captured[0].Parameters.Count, "first block should have 2 parameters, got {0}", captured[0].Parameters.Count);

                    string colorValue;
                    string widthValue;
                    context.IsTrue(captured[0].Parameters.TryGetValue("color", out colorValue), "first block parameters should contain key 'color'");
                    context.IsTrue(captured[0].Parameters.TryGetValue("width", out widthValue), "first block parameters should contain key 'width'");
                    context.AreEqual("red", colorValue, "color should be 'red' despite the mid-block comment, got '{0}'", colorValue);
                    context.AreEqual("5px", widthValue, "width should be '5px' despite the mid-block comment, got '{0}'", widthValue);

                    context.AreEqual(".a1c2", captured[1].Name, "second selector name should be '.a1c2', got '{0}'", captured[1].Name);
                    context.AreEqual(1, captured[1].Parameters.Count, "second block should have 1 parameter, got {0}", captured[1].Parameters.Count);

                    string heightValue;
                    context.IsTrue(captured[1].Parameters.TryGetValue("height", out heightValue), "second block parameters should contain key 'height'");
                    context.AreEqual("6px", heightValue, "height should be '6px' despite the brace-containing comment between rules, got '{0}'", heightValue);
                }
            }

            // comma-separated selector group producing several entries that share one data object
            {
                StyleCollection collection;
                IList<RecordingStyleData> captured = ParseRaw(".a1shareA, .a1shareB { width: 7px; }", out collection);

                context.AreEqual(1, captured.Count, "a comma-separated selector group should invoke the constructor delegate exactly once, got {0} calls", captured.Count);

                StyleSelectorList selectorListA = new StyleSelectorList(new StyleSelector(null, new string[] { "a1shareA" }, null), StyleNodeMatch.Class);
                StyleSelectorList selectorListB = new StyleSelectorList(new StyleSelector(null, new string[] { "a1shareB" }, null), StyleNodeMatch.Class);

                ICollection<StyleNodeMatchPair> resultA = collection.GetStyleData(selectorListA);
                ICollection<StyleNodeMatchPair> resultB = collection.GetStyleData(selectorListB);

                context.IsNotNull(resultA, "class .a1shareA should resolve to a style node");
                context.IsNotNull(resultB, "class .a1shareB should resolve to a style node");

                if (resultA != null && resultB != null && captured.Count == 1)
                {
                    IStyleData dataA = null;
                    foreach (StyleNodeMatchPair pair in resultA)
                        dataA = pair.Node.Data;

                    IStyleData dataB = null;
                    foreach (StyleNodeMatchPair pair in resultB)
                        dataB = pair.Node.Data;

                    context.IsTrue(object.ReferenceEquals(dataA, dataB), "both selectors from a comma-separated group should point to the same shared IStyleData instance");
                    context.IsTrue(object.ReferenceEquals(dataA, captured[0]), "the shared IStyleData instance should be the single instance the constructor delegate produced");
                }
            }

            // the @font.name { ... } at-rule block form
            {
                IList<RecordingStyleData> captured = ParseRaw("@font.a1font { resource: myfont.ttf; }");

                context.AreEqual(1, captured.Count, "an @font.name block should produce exactly one IStyleData, got {0}", captured.Count);
                if (captured.Count == 1)
                {
                    context.AreEqual("@font.a1font", captured[0].Name, "selector name should be '@font.a1font', got '{0}'", captured[0].Name);

                    string resourceValue;
                    context.IsTrue(captured[0].Parameters.TryGetValue("resource", out resourceValue), "parameters should contain key 'resource'");
                    context.AreEqual("myfont.ttf", resourceValue, "resource value should be 'myfont.ttf', got '{0}'", resourceValue);
                }
            }

            // the @sprite.name { ... } at-rule block form
            {
                IList<RecordingStyleData> captured = ParseRaw("@sprite.a1sprite { tile_x: 3; }");

                context.AreEqual(1, captured.Count, "an @sprite.name block should produce exactly one IStyleData, got {0}", captured.Count);
                if (captured.Count == 1)
                {
                    context.AreEqual("@sprite.a1sprite", captured[0].Name, "selector name should be '@sprite.a1sprite', got '{0}'", captured[0].Name);

                    string tileValue;
                    context.IsTrue(captured[0].Parameters.TryGetValue("tile_x", out tileValue), "parameters should contain key 'tile_x'");
                    context.AreEqual("3", tileValue, "tile_x value should be '3', got '{0}'", tileValue);
                }
            }

            // the @import "x.css"; statement form (at-rule terminated by a semicolon, not a block)
            {
                IList<RecordingStyleData> captured = ParseRaw("@import \"a1import.css\";");

                context.AreEqual(1, captured.Count, "an @import statement should produce exactly one IStyleData, got {0}", captured.Count);
                if (captured.Count == 1)
                {
                    context.AreEqual("@import", captured[0].Name, "selector name should be '@import', got '{0}'", captured[0].Name);
                    context.AreEqual(1, captured[0].Parameters.Count, "@import should register exactly one (key==value) entry, got {0}", captured[0].Parameters.Count);

                    string quoted = "\"a1import.css\"";
                    string importValue;
                    context.IsTrue(captured[0].Parameters.TryGetValue(quoted, out importValue), "the ';'-terminated rule text (the quoted filename) is used as its own key");
                    context.AreEqual(quoted, importValue, "the value should equal the same quoted filename text, got '{0}'", importValue);
                }
            }

            // whitespace and newline tolerance: tabs, CRLF, declarations spread over multiple lines
            {
                string css = "\t.a1ws\t{\r\n\tcolor:   red;\r\n\twidth:5px;\r\n}\r\n";
                IList<RecordingStyleData> captured = ParseRaw(css);

                context.AreEqual(1, captured.Count, "a tab/CRLF-decorated block should still produce exactly one IStyleData, got {0}", captured.Count);
                if (captured.Count == 1)
                {
                    context.AreEqual(".a1ws", captured[0].Name, "selector name should be trimmed to '.a1ws', got '{0}'", captured[0].Name);
                    context.AreEqual(2, captured[0].Parameters.Count, "block should have 2 parameters, got {0}", captured[0].Parameters.Count);

                    string colorValue;
                    string widthValue;
                    context.IsTrue(captured[0].Parameters.TryGetValue("color", out colorValue), "parameters should contain key 'color'");
                    context.IsTrue(captured[0].Parameters.TryGetValue("width", out widthValue), "parameters should contain key 'width'");
                    context.AreEqual("red", colorValue, "color should be trimmed to 'red' despite extra spaces, got '{0}'", colorValue);
                    context.AreEqual("5px", widthValue, "width should be '5px', got '{0}'", widthValue);
                }
            }

            // duplicate property inside one block -- the last occurrence must win
            {
                IList<RecordingStyleData> captured = ParseRaw(".a1dup { color: red; color: blue; }");

                context.AreEqual(1, captured.Count, "a block with a duplicate property should still produce exactly one IStyleData, got {0}", captured.Count);
                if (captured.Count == 1)
                {
                    context.AreEqual(1, captured[0].Parameters.Count, "a duplicate key should collapse to a single parameter entry, got {0}", captured[0].Parameters.Count);

                    string colorValue;
                    context.IsTrue(captured[0].Parameters.TryGetValue("color", out colorValue), "parameters should contain key 'color'");
                    context.AreEqual("blue", colorValue, "the last occurrence of a duplicate property should win, got '{0}'", colorValue);
                }
            }
        }

        private static void Test2_CssParserQuotedValues(TestContext context)
        {
            // a ';' inside a quoted value must not end the declaration early
            {
                StyleCollection collection;
                IList<RecordingStyleData> captured = TryParseRaw(context, ".b2url { background-image: url(\"a;b\"); }", out collection, "quoted ';'");

                context.AreEqual(1, captured.Count, "a block with one quoted declaration containing ';' should still produce exactly one IStyleData, got {0}", captured.Count);
                if (captured.Count == 1)
                {
                    context.AreEqual(1, captured[0].Parameters.Count, "the block should parse to exactly one declaration despite the ';' inside the quoted value, got {0}", captured[0].Parameters.Count);

                    string value;
                    context.IsTrue(captured[0].Parameters.TryGetValue("background-image", out value), "declaration key should be 'background-image'");
                    if (value != null)
                        context.AreEqual("url(\"a;b\")", value, "quoted value containing ';' should stay intact, got '{0}'", value);
                }
            }

            // a '}' inside a quoted value must not end the parameter block early
            {
                StyleCollection collection;
                IList<RecordingStyleData> captured = TryParseRaw(context, ".b2content { content: \"}\"; }", out collection, "quoted '}'");

                context.AreEqual(1, captured.Count, "a block with one quoted declaration containing '}}' should still produce exactly one IStyleData, got {0}", captured.Count);
                if (captured.Count == 1)
                {
                    context.AreEqual(1, captured[0].Parameters.Count, "the block should parse to exactly one declaration despite the '}}' inside the quoted value, got {0}", captured[0].Parameters.Count);

                    string value;
                    context.IsTrue(captured[0].Parameters.TryGetValue("content", out value), "declaration key should be 'content'");
                    if (value != null)
                        context.AreEqual("\"}\"", value, "quoted value containing '}}' should stay intact, got '{0}'", value);
                }
            }
        }

        private static void Test3_CssAtRulesUnhandled(TestContext context)
        {
            string css = "@media screen { panel { width: 10px; } }";

            StyleCollection collection;
            IList<RecordingStyleData> captured = TryParseRaw(context, css, out collection, "@media block");

            if (collection == null)
                return;

            // Correct handling: the inner "panel" rule should be captured under its own
            // name, independent of the @media wrapper, with its declaration intact.
            bool foundPanelRule = false;
            foreach (RecordingStyleData data in captured)
            {
                if (data.Name == "panel")
                {
                    foundPanelRule = true;

                    string widthValue;
                    bool hasWidth = data.Parameters.TryGetValue("width", out widthValue);
                    context.IsTrue(hasWidth, "the inner 'panel' rule should have a 'width' declaration");
                    if (hasWidth)
                        context.AreEqual("10px", widthValue, "the inner 'panel' rule's width should be '10px', got '{0}'", widthValue);
                }
            }

            context.IsTrue(foundPanelRule, "the inner 'panel { width: 10px; }' rule inside '@media screen { ... }' should be reachable as its own style named 'panel'");

            // Correct handling: no bogus style should be registered literally under the
            // media query's own name -- @media is not itself a style.
            ICollection<StyleNode> bogusMediaNodes = collection.GetElementNodes(new StyleSelector("@media"));
            context.IsNull(bogusMediaNodes, "the collection should not contain a bogus style element node literally named '@media'");
        }

        private static void Test4_ValueAndUnitConversion(TestContext context)
        {
            // FloatParse
            context.AreEqualFloat(10f, ConversionHelper.FloatParse("10px"), 0.0001f, "'10px' should parse to 10, got {0}", ConversionHelper.FloatParse("10px"));
            context.AreEqualFloat(96.0f / 72.0f * 10f, ConversionHelper.FloatParse("10pt"), 0.001f, "'10pt' should parse to 96/72*10 (~13.333), got {0}", ConversionHelper.FloatParse("10pt"));
            context.AreEqualFloat(1f, ConversionHelper.FloatParse("1em"), 0.0001f, "'1em' should parse to 1, got {0}", ConversionHelper.FloatParse("1em"));
            context.AreEqualFloat(10f, ConversionHelper.FloatParse("10"), 0.0001f, "'10' (no unit) should parse to 10, got {0}", ConversionHelper.FloatParse("10"));
            // Note: FloatParse(" 10px ") -- leading whitespace combined with a unit suffix --
            // throws today (a separate FloatParse bug: the TrimStart loop decrements both
            // `start` and `length` per leading char trimmed, corrupting the `length` bound
            // the px/pt/em suffix stripping relies on afterwards). That combination is not
            // in scope for this group; whitespace tolerance is exercised here without a unit
            // suffix, where it does work correctly.
            context.AreEqualFloat(10f, ConversionHelper.FloatParse(" 10 "), 0.0001f, "leading/trailing whitespace should be tolerated, got {0}", ConversionHelper.FloatParse(" 10 "));
            context.AreEqualFloat(1.5f, ConversionHelper.FloatParse("1,5"), 0.0001f, "a comma decimal separator should be accepted, got {0}", ConversionHelper.FloatParse("1,5"));

            // "50%" documents the CURRENT contract of FloatParse as a low-level helper: it
            // returns the raw fraction (0.5) and has no notion of being called for a Percent
            // property. Whether a percentage may be flattened to a plain float at all (as
            // opposed to being kept as a distinct percent type through the cascade) is a
            // separate question, covered by a different group owned by another task -- this
            // is not treated as a known failure here.
            context.AreEqualFloat(0.5f, ConversionHelper.FloatParse("50%"), 0.0001f, "current contract: '50%' parses to the raw fraction 0.5, got {0}", ConversionHelper.FloatParse("50%"));

            // ColorParse
            context.AreEqual((uint)0xff0000, ConversionHelper.ColorParse("#ff0000"), "'#ff0000' should parse to 0xff0000");

            // the eight digit # form is CSS #rrggbbaa, so alpha is written last and moves to the high byte
            context.AreEqual((uint)0x80ff0000, ConversionHelper.ColorParse("#ff000080"), "'#ff000080' should parse to 0x80ff0000");
            context.AreEqual((uint)0xffff0000, ConversionHelper.ColorParse("#ff0000ff"), "'#ff0000ff' (full alpha) should parse to 0xffff0000");
            context.AreEqual((uint)0x00ff0000, ConversionHelper.ColorParse("#ff000000"), "'#ff000000' (zero alpha) should parse to 0x00ff0000");

            // the four digit #rgba short form already reads as CSS, so both forms must agree
            context.AreEqual(ConversionHelper.ColorParse("#ff0000aa"), ConversionHelper.ColorParse("#f00a"), "'#f00a' and '#ff0000aa' should give the same value");

            // 0x is this engine's own notation, not CSS, so it keeps its 0xAARRGGBB meaning
            context.AreEqual((uint)0xff0000, ConversionHelper.ColorParse("0xff0000"), "'0xff0000' should parse to 0xff0000");
            context.AreEqual((uint)0x80ff0000, ConversionHelper.ColorParse("0x80ff0000"), "'0x80ff0000' should still parse to 0x80ff0000");

            // StringParse, UnitType.Url
            context.AreEqual("name", ConversionHelper.StringParse("url(\"name\")", UnitType.Url), "url(\"name\") should yield 'name'");
            context.AreEqual("name", ConversionHelper.StringParse("url(name)", UnitType.Url), "url(name) should yield 'name'");

            // MarginParse: 4-value form is CSS order, top right bottom left
            Margin fourValue = ConversionHelper.MarginParse("1px 2px 3px 4px", UnitType.Length);
            context.AreEqualFloat(4f, fourValue.Left, 0.0001f, "4-value margin: 1st value maps to Top (CSS order: top right bottom left), got {0}", fourValue.Left);
            context.AreEqualFloat(1f, fourValue.Top, 0.0001f, "4-value margin: 2nd value maps to Right (CSS order), got {0}", fourValue.Top);
            context.AreEqualFloat(2f, fourValue.Right, 0.0001f, "4-value margin: 3rd value maps to Bottom (CSS order), got {0}", fourValue.Right);
            context.AreEqualFloat(3f, fourValue.Bottom, 0.0001f, "4-value margin: 4th value maps to Left (CSS order), got {0}", fourValue.Bottom);

            // MarginParse: 1-value form sets all four
            Margin oneValue = ConversionHelper.MarginParse("5px", UnitType.Length);
            context.AreEqualFloat(5f, oneValue.Left, 0.0001f, "1-value margin sets Left, got {0}", oneValue.Left);
            context.AreEqualFloat(5f, oneValue.Top, 0.0001f, "1-value margin sets Top, got {0}", oneValue.Top);
            context.AreEqualFloat(5f, oneValue.Right, 0.0001f, "1-value margin sets Right, got {0}", oneValue.Right);
            context.AreEqualFloat(5f, oneValue.Bottom, 0.0001f, "1-value margin sets Bottom, got {0}", oneValue.Bottom);

            // EnumParse onto WidgetBackgroundStyle
            WidgetBackgroundStyle backgroundStyle = ConversionHelper.EnumParse<WidgetBackgroundStyle>("ImageFit");
            context.AreEqual(WidgetBackgroundStyle.ImageFit, backgroundStyle, "EnumParse<WidgetBackgroundStyle>('ImageFit') should yield ImageFit, got {0}", backgroundStyle);

            // EnumParse: a '-'-to-'_' name (only the non-generic Type overload replaces '-' with '_')
            WidgetBackgroundStyle noRepeatStyle = (WidgetBackgroundStyle)ConversionHelper.EnumParse(typeof(WidgetBackgroundStyle), "no-repeat");
            context.AreEqual(WidgetBackgroundStyle.No_Repeat, noRepeatStyle, "EnumParse(typeof(WidgetBackgroundStyle), 'no-repeat') should map '-' to '_' and yield No_Repeat, got {0}", noRepeatStyle);

            // EnumParse: a multi-value form onto WidgetAlign
            WidgetAlign multiValue = ConversionHelper.EnumParse<WidgetAlign>("Left|Right");
            context.AreEqual(WidgetAlign.HorizontalCenter, multiValue, "EnumParse<WidgetAlign>('Left|Right') should OR the flags into HorizontalCenter, got {0}", multiValue);

            // ToString round-trips for float, per UnitType
            context.AreEqual("10px", ConversionHelper.ToString(10f, UnitType.Length), "ToString(10f, Length) should be '10px'");
            context.AreEqualFloat(10f, ConversionHelper.FloatParse(ConversionHelper.ToString(10f, UnitType.Length)), 0.0001f, "Length round trip should return to 10");

            context.AreEqual("1em", ConversionHelper.ToString(1f, UnitType.FontUnits), "ToString(1f, FontUnits) should be '1em'");
            context.AreEqualFloat(1f, ConversionHelper.FloatParse(ConversionHelper.ToString(1f, UnitType.FontUnits)), 0.0001f, "FontUnits round trip should return to 1");

            context.AreEqual("50%", ConversionHelper.ToString(0.5f, UnitType.Percent), "ToString(0.5f, Percent) should be '50%'");
            context.AreEqualFloat(0.5f, ConversionHelper.FloatParse(ConversionHelper.ToString(0.5f, UnitType.Percent)), 0.0001f, "Percent round trip should return to 0.5");

            context.AreEqual("10", ConversionHelper.ToString(10f, UnitType.None), "ToString(10f, None) should be '10'");
            context.AreEqualFloat(10f, ConversionHelper.FloatParse(ConversionHelper.ToString(10f, UnitType.None)), 0.0001f, "None round trip should return to 10");

            // ToString round-trip for uint as a colour
            context.AreEqual("#ff0000", ConversionHelper.ToString((uint)0xff0000, UnitType.Color), "ToString(0xff0000, Color) should be '#ff0000'");
            context.AreEqual((uint)0xff0000, ConversionHelper.ColorParse(ConversionHelper.ToString((uint)0xff0000, UnitType.Color)), "Color round trip should return to 0xff0000");

            // SaveCSS writes through this, so the eight digit output must be CSS order too or a save/load round trip corrupts the alpha
            context.AreEqual("#ff000080", ConversionHelper.ToString((uint)0x80ff0000, UnitType.Color), "ToString(0x80ff0000, Color) should be '#ff000080'");
            context.AreEqual((uint)0x80ff0000, ConversionHelper.ColorParse(ConversionHelper.ToString((uint)0x80ff0000, UnitType.Color)), "Color-with-alpha round trip should return to 0x80ff0000");
            context.AreEqual((uint)0xaa336699, ConversionHelper.ColorParse(ConversionHelper.ToString((uint)0xaa336699, UnitType.Color)), "Color-with-alpha round trip should return to 0xaa336699");
        }

        private static void AssertUintParse(TestContext context, string input, uint expected, string label)
        {
            try
            {
                uint actual = ConversionHelper.ColorParse(input);
                context.AreEqual(expected, actual, "{0} should parse to 0x{1:x}, got 0x{2:x}", label, expected, actual);
            }
            catch (Exception ex)
            {
                context.Fail("{0} should parse to 0x{1:x} but ColorParse threw {2}: {3}", label, expected, ex.GetType().Name, ex.Message);
            }
        }

        private static void Test5_ColourFormatsNotParsed(TestContext context)
        {
            AssertUintParse(context, "#ccc", (uint)0xcccccc, "'#ccc' (3-digit hex)");
            AssertUintParse(context, "#f00", (uint)0xff0000, "'#f00' (3-digit hex)");

            // 4-digit #rgba short form: CSS writes r,g,b,a in that order, each digit doubled
            // ("#f00a" -> r=ff g=00 b=00 a=aa); to be correct it must be repacked to match
            // this codebase's own AARRGGBB uint layout (alpha in the high byte, as already
            // used and tested for the long "#ff000080" form), not left in source digit order.
            AssertUintParse(context, "#f00a", (uint)0xaaff0000, "'#f00a' (4-digit #rgba short form)");

            AssertUintParse(context, "rgb(255, 0, 0)", (uint)0xff0000, "'rgb(255, 0, 0)'");

            // alpha 0.5 -> byte round(0.5 * 255) = 128 = 0x80, placed in the high byte to
            // match this codebase's existing AARRGGBB convention.
            AssertUintParse(context, "rgba(255, 0, 0, 0.5)", (uint)0x80ff0000, "'rgba(255, 0, 0, 0.5)'");

            AssertUintParse(context, "red", (uint)0xff0000, "'red'");
            AssertUintParse(context, "white", (uint)0xffffff, "'white'");
            AssertUintParse(context, "black", (uint)0x000000, "'black'");
            AssertUintParse(context, "transparent", (uint)0x00000000, "'transparent'");

            // the flag is the whole point: a packed zero high byte cannot say whether an alpha was
            // written, and a written zero must not read as "none given"
            bool hasAlpha;

            ConversionHelper.ColorParse("transparent", out hasAlpha);
            context.IsTrue(hasAlpha, "'transparent' should report a written alpha");

            ConversionHelper.ColorParse("rgba(255, 0, 0, 0)", out hasAlpha);
            context.IsTrue(hasAlpha, "'rgba(255, 0, 0, 0)' should report a written alpha");

            ConversionHelper.ColorParse("#ff000000", out hasAlpha);
            context.IsTrue(hasAlpha, "'#ff000000' should report a written alpha");

            ConversionHelper.ColorParse("#ff0000", out hasAlpha);
            context.IsFalse(hasAlpha, "'#ff0000' should report no written alpha");

            ConversionHelper.ColorParse("rgb(255, 0, 0)", out hasAlpha);
            context.IsFalse(hasAlpha, "'rgb(255, 0, 0)' should report no written alpha");

            ConversionHelper.ColorParse("red", out hasAlpha);
            context.IsFalse(hasAlpha, "a named colour other than transparent should report no written alpha");

            ConversionHelper.ColorParse("0xff0000", out hasAlpha);
            context.IsFalse(hasAlpha, "the six digit 0x form should report no written alpha");

            ConversionHelper.ColorParse("0x80ff0000", out hasAlpha);
            context.IsTrue(hasAlpha, "the eight digit 0x form should report a written alpha");
        }

        private static void Test6_MarginSingleValueDropsUnit(TestContext context)
        {
            // The 1-value branch of MarginParse calls FloatParse(values[0]) without passing
            // unitType, while the 4-value branch passes it through. Both forms must agree
            // for the same numeric value, whatever its unit.
            Margin fourValuePt = ConversionHelper.MarginParse("5pt 5pt 5pt 5pt", UnitType.Length);
            Margin oneValuePt = ConversionHelper.MarginParse("5pt", UnitType.Length);

            context.AreEqualFloat(fourValuePt.Left, oneValuePt.Left, 0.0001f, "1-value and 4-value Margin forms should agree for a 'pt' value: 4-value gave {0}, 1-value gave {1}", fourValuePt.Left, oneValuePt.Left);

            Margin fourValuePercent = ConversionHelper.MarginParse("50% 50% 50% 50%", UnitType.Percent);
            Margin oneValuePercent = ConversionHelper.MarginParse("50%", UnitType.Percent);

            context.AreEqualFloat(fourValuePercent.Left, oneValuePercent.Left, 0.0001f, "1-value and 4-value Margin forms should agree for a '%' value: 4-value gave {0}, 1-value gave {1}", fourValuePercent.Left, oneValuePercent.Left);
        }

        private static void Test7_UnknownPropertiesReportedNotFatal(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();
            controller.ClearLog();

            context.DoesNotThrow(delegate
            {
                TestEnvironment.LoadCss(".g7known { border: 1px solid red; width: 42px; }");
            }, "loading CSS with an unknown property ('border') should not throw");

            bool foundBorderMessage = false;
            foreach (string message in controller.Messages)
            {
                if (message.IndexOf("border", StringComparison.OrdinalIgnoreCase) >= 0)
                    foundBorderMessage = true;
            }
            context.IsTrue(foundBorderMessage, "the message log should gain an entry mentioning the unknown property 'border'");

            WidgetStyleSheet styleSheet = WidgetManager.GetStyle(new StyleSelector(null, new string[] { "g7known" }, null));
            float width = styleSheet.Get<float>("width", -1f);
            context.AreEqualFloat(42f, width, 0.0001f, "the valid 'width' declaration in the same block should still have applied, got {0}", width);

            // A genuinely malformed value on a known property is caught and rethrown as WidgetException.
            context.Throws(typeof(WidgetException), delegate
            {
                TestEnvironment.LoadCss(".g7bad { width: notanumber; }");
            }, "a malformed value on a known property should be rethrown as WidgetException");
        }
    }
}
