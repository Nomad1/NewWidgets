using NewWidgets.Widgets;

namespace NewWidgets.Test
{
    /// <summary>
    /// Tests for the CSS cascade: specificity, the widget's own style, inheritance, source
    /// order, !important and the inherit/initial/unset keywords, and pseudo-class state
    /// resolution. CSS 2.1 section 6.4 ("The cascade") is the reference behaviour asserted
    /// here; where NewWidgets disagrees with it, the group is registered as a known failure.
    /// </summary>
    internal static class CascadeTests
    {
        /// <summary>
        /// WidgetPanel subclass that carries its own element type, so every scenario below can
        /// use a bare-element selector (needed for Group A) without ever registering a rule for
        /// the real "panel" element type shared by every WidgetPanel in the process.
        /// </summary>
        private sealed class CascadeWidget : WidgetPanel
        {
            public CascadeWidget(string elementType, WidgetStyle style)
                : base(elementType, style)
            {
            }
        }

        public static void Register()
        {
            TestRunner.Add("Test 20: cascade by specificity", TestSpecificity);
            TestRunner.Add("Test 21: the widget's own style wins", TestOwnStyleWins);
            TestRunner.Add("Test 22: inheritance", TestInheritance);
            TestRunner.AddKnownFailure("Test 23: source order breaks specificity ties",
                "StyleNode.CompareTo (NewWidgets/Styles/StyleNode.cs) returns -1 for both a.CompareTo(b) and b.CompareTo(a) whenever Specificity is equal, so it is not a valid total order; it is used as the key comparer of the SortedDictionary<StyleNode, StyleNodeMatch> built in StyleCollection.GetStyleData (NewWidgets/Styles/StyleCollection.cs), so which of two equal-specificity rules from different selectors wins is a function of tree-insertion order, not the CSS 2.1 6.4.1 rule that the later declaration wins. No StyleNode or StyleSheetData field records a declaration index anywhere, so source order is not merely mis-implemented here -- it cannot currently be represented at all.",
                TestSourceOrder);
            TestRunner.AddKnownFailure("Test 24: !important",
                "grepping NewWidgets/ for \"important\" (case-insensitive) returns zero matches: CSSParser (NewWidgets/Styles/CSSParser.cs) never tokenizes the !important suffix, StyleSheetData/IStyleData (NewWidgets/Widgets/WidgetStyleSheet.cs) has no field to mark a declaration important, and WidgetStyleSheet.Get ranks candidates purely by StyleNode.Specificity with the own-style node unconditionally checked first, so CSS 2.1 6.4.2 !important cannot be represented or honoured anywhere in the cascade.",
                TestImportant);
            TestRunner.AddKnownFailure("Test 25: the inherit, initial and unset keywords",
                "inherit/initial/unset are CSS-wide property VALUES, not per-property metadata, and nothing in ConversionHelper, WidgetParameterMap or WidgetStyleSheet.Get special-cases them: ConversionHelper.FloatParse and ConversionHelper.UintParse (NewWidgets/Utility/ConversionHelper.cs) try to parse the literal token as a number or a color and throw FormatException, which WidgetManager.InitCssParameters (NewWidgets/Widgets/WidgetManager.Styles.cs) wraps and rethrows as WidgetException, so LoadCSS cannot even accept a rule that uses one of these keywords. WidgetParameterInheritance.Unset and .Revert (NewWidgets/Widgets/Enums.cs) exist as enum members but nothing reads them.",
                TestCascadeKeywords);
            TestRunner.Add("Test 26: pseudo-class state resolution", TestPseudoClassState);
        }

        private static CascadeWidget CreateWidget(string elementType, string[] classes, string id)
        {
            WidgetStyle style = new WidgetStyle(classes, id);
            return new CascadeWidget(elementType, style);
        }

        private static void TestSpecificity(TestContext context)
        {
            TestEnvironment.Setup();

            // A1: an #id rule beats a .class rule; a property only the class rule sets still
            // cascades in when the winning #id rule does not mention it (this is the cascade,
            // not a wholesale replacement); a rule for a class the element does not carry
            // contributes nothing.
            CascadeWidget a1 = CreateWidget("c1elemA1", new string[] { "c1cls1" }, "c1id1");
            TestEnvironment.LoadCss(
                "#c1id1 { background-color: #ff0000; }" +
                ".c1cls1 { background-color: #00ff00; color: #0000ff; }" +
                ".c1nomatch1 { background-color: #ffff00; }");
            a1.Relayout();

            uint a1Back = a1.GetProperty(WidgetParameterIndex.BackColor, (uint)0);
            context.AreEqual((uint)0xff0000, a1Back,
                "an #id rule must beat a .class rule for the same property; expected 0x{0:x6}, got 0x{1:x6}", (uint)0xff0000, a1Back);

            uint a1Text = a1.GetProperty(WidgetParameterIndex.TextColor, (uint)0);
            context.AreEqual((uint)0x0000ff, a1Text,
                "a property only the class rule sets must still cascade in when the winning #id rule does not mention it; expected 0x{0:x6}, got 0x{1:x6}", (uint)0x0000ff, a1Text);

            context.IsFalse(a1Back == 0xffff00,
                "a rule for a class the element does not carry (.c1nomatch1) must contribute nothing; back_color was 0x{0:x6}", a1Back);

            // A2: a .class rule beats a bare element-type rule.
            CascadeWidget a2 = CreateWidget("c1elemA2", new string[] { "c1cls2" }, null);
            TestEnvironment.LoadCss(
                "c1elemA2 { background-color: #0000ff; }" +
                ".c1cls2 { background-color: #00ff00; }");
            a2.Relayout();

            uint a2Back = a2.GetProperty(WidgetParameterIndex.BackColor, (uint)0);
            context.AreEqual((uint)0x00ff00, a2Back,
                "a .class rule must beat a bare element-type rule; expected 0x{0:x6}, got 0x{1:x6}", (uint)0x00ff00, a2Back);

            // A3: a two-class rule beats a single-class rule.
            CascadeWidget a3 = CreateWidget("c1elemA3", new string[] { "c1cls3a", "c1cls3b" }, null);
            TestEnvironment.LoadCss(
                ".c1cls3b { background-color: #0000ff; }" +
                ".c1cls3a.c1cls3b { background-color: #00ff00; }");
            a3.Relayout();

            uint a3Back = a3.GetProperty(WidgetParameterIndex.BackColor, (uint)0);
            context.AreEqual((uint)0x00ff00, a3Back,
                "a two-class rule (.c1cls3a.c1cls3b) must beat a single-class rule (.c1cls3b); expected 0x{0:x6}, got 0x{1:x6}", (uint)0x00ff00, a3Back);
        }

        private static void TestOwnStyleWins(TestContext context)
        {
            TestEnvironment.Setup();

            CascadeWidget b1 = CreateWidget("c2elemB1", new string[] { "c2cls1" }, "c2id1");
            TestEnvironment.LoadCss(
                "#c2id1 { background-color: #ff0000; color: #ff0000; }" +
                ".c2cls1 { background-color: #00ff00; }");
            b1.Relayout();

            // SetProperty writes straight into the own-style dictionary that is already part of
            // the live cascade list, so GetProperty sees it immediately without another Relayout.
            b1.SetProperty(WidgetParameterIndex.BackColor, (uint)0x00ffff);

            uint b1Back = b1.GetProperty(WidgetParameterIndex.BackColor, (uint)0);
            context.AreEqual((uint)0x00ffff, b1Back,
                "the widget's own style must beat every stylesheet rule, including #id; expected 0x{0:x6}, got 0x{1:x6}", (uint)0x00ffff, b1Back);

            uint b1Text = b1.GetProperty(WidgetParameterIndex.TextColor, (uint)0);
            context.AreEqual((uint)0xff0000, b1Text,
                "setting one own-style property must not stop an unrelated property from still cascading from the #id rule; expected 0x{0:x6}, got 0x{1:x6}", (uint)0xff0000, b1Text);
        }

        private static void TestInheritance(TestContext context)
        {
            TestEnvironment.Setup();

            // C1: color (Inherit in WidgetParameterIndex) reaches the child; width (Initial)
            // does not.
            CascadeWidget parent1 = CreateWidget("c3elemP1", new string[] { "c3parent1" }, null);
            CascadeWidget child1 = CreateWidget("c3elemC1", new string[] { "c3child1" }, null);
            TestEnvironment.LoadCss(".c3parent1 { color: #ff0000; width: 300px; }");
            parent1.Relayout();
            parent1.AddChild(child1);
            child1.Relayout();

            uint child1Color = child1.GetProperty(WidgetParameterIndex.TextColor, (uint)0);
            context.AreEqual((uint)0xff0000, child1Color,
                "color is Inherit, so a parent's color rule must reach the child; expected 0x{0:x6}, got 0x{1:x6}", (uint)0xff0000, child1Color);

            float child1Width = child1.Size.X;
            context.AreEqualFloat(0f, child1Width, 0.01f,
                "width is Initial, so a parent's width rule must NOT reach the child; expected the child's own initial width 0, got {0}", child1Width);

            // C2: a child's own rule for an inherited property beats the inherited value.
            CascadeWidget child2 = CreateWidget("c3elemC2", new string[] { "c3child2" }, null);
            TestEnvironment.LoadCss(".c3child2 { color: #0000ff; }");
            parent1.AddChild(child2);
            child2.Relayout();

            uint child2Color = child2.GetProperty(WidgetParameterIndex.TextColor, (uint)0);
            context.AreEqual((uint)0x0000ff, child2Color,
                "a child's own matching rule must beat the value it would otherwise inherit; expected 0x{0:x6}, got 0x{1:x6}", (uint)0x0000ff, child2Color);

            // C3: inheritance crosses more than one ancestor level when the level in between
            // sets nothing of its own.
            CascadeWidget grandparent = CreateWidget("c3elemGP", new string[] { "c3gp" }, null);
            CascadeWidget middle = CreateWidget("c3elemMid", new string[] { "c3mid" }, null);
            CascadeWidget grandchild = CreateWidget("c3elemGC", new string[] { "c3gc" }, null);
            TestEnvironment.LoadCss(".c3gp { color: #00ff00; }");
            grandparent.AddChild(middle);
            middle.AddChild(grandchild);
            grandchild.Relayout();

            uint grandchildColor = grandchild.GetProperty(WidgetParameterIndex.TextColor, (uint)0);
            context.AreEqual((uint)0x00ff00, grandchildColor,
                "inheritance must cross more than one ancestor level (grandparent to grandchild); expected 0x{0:x6}, got 0x{1:x6}", (uint)0x00ff00, grandchildColor);
        }

        private static void TestSourceOrder(TestContext context)
        {
            TestEnvironment.Setup();

            // D1: two DIFFERENT selectors of equal specificity (one class each, 100 apiece).
            // CSS 2.1 6.4.1 says the later DECLARATION wins. The widget's class list is
            // deliberately given in the opposite order (c4b before c4a) so a pass here can only
            // be explained by declaration order, never by class-list order.
            CascadeWidget d1 = CreateWidget("c4elemD1", new string[] { "c4b", "c4a" }, null);
            TestEnvironment.LoadCss(
                ".c4a { background-color: #ff0000; }" +
                ".c4b { background-color: #00ff00; }");
            d1.Relayout();

            uint d1Back = d1.GetProperty(WidgetParameterIndex.BackColor, (uint)0);
            context.AreEqual((uint)0x00ff00, d1Back,
                "of two equal-specificity rules from different selectors, the later-declared one (.c4b) must win regardless of class-list order; expected 0x{0:x6}, got 0x{1:x6}", (uint)0x00ff00, d1Back);

            // D2: the identical selector declared twice -- the later declaration must win.
            CascadeWidget d2 = CreateWidget("c4elemD2", new string[] { "c4dup" }, null);
            TestEnvironment.LoadCss(
                ".c4dup { background-color: #ff0000; }" +
                ".c4dup { background-color: #00ff00; }");
            d2.Relayout();

            uint d2Back = d2.GetProperty(WidgetParameterIndex.BackColor, (uint)0);
            context.AreEqual((uint)0x00ff00, d2Back,
                "the later of two declarations of the identical selector must win; expected 0x{0:x6}, got 0x{1:x6}", (uint)0x00ff00, d2Back);
        }

        private static void TestImportant(TestContext context)
        {
            TestEnvironment.Setup();

            // E1: as if ".c5low1 { color: red !important; }" -- CSS 2.1 6.4.2 says an
            // !important declaration in a low-specificity rule beats a normal declaration in a
            // higher-specificity rule. NewWidgets cannot mark a declaration important anywhere
            // (see the group's reason), so this sets up exactly the specificity conflict a real
            // stylesheet would rely on and checks the outcome CSS requires.
            CascadeWidget e1 = CreateWidget("c5elemE1", new string[] { "c5low1" }, "c5id1");
            TestEnvironment.LoadCss(
                ".c5low1 { color: #ff0000; }" +
                "#c5id1 { color: #0000ff; }");
            e1.Relayout();

            uint e1Color = e1.GetProperty(WidgetParameterIndex.TextColor, (uint)0);
            context.AreEqual((uint)0xff0000, e1Color,
                "an !important low-specificity declaration must beat a normal higher-specificity one; expected 0x{0:x6} (the class rule, as if it carried !important), got 0x{1:x6}", (uint)0xff0000, e1Color);

            // E2: as if BOTH declarations carried !important -- CSS 2.1 6.4.2 then falls back to
            // ordinary specificity, so the #id rule should win. This already holds today,
            // because nothing is ever important so plain specificity is the only thing that
            // ever decides; kept for completeness, not as evidence of a fix.
            CascadeWidget e2 = CreateWidget("c5elemE2", new string[] { "c5low2" }, "c5id2");
            TestEnvironment.LoadCss(
                ".c5low2 { color: #ff0000; }" +
                "#c5id2 { color: #0000ff; }");
            e2.Relayout();

            uint e2Color = e2.GetProperty(WidgetParameterIndex.TextColor, (uint)0);
            context.AreEqual((uint)0x0000ff, e2Color,
                "between two (hypothetically) important declarations specificity must still decide; expected 0x{0:x6}, got 0x{1:x6}", (uint)0x0000ff, e2Color);

            // E3: as if ".c5low3 { color: red !important; }" -- CSS 2.1 6.4.2 says a stylesheet
            // !important declaration beats even the element's own inline style. WidgetStyleSheet
            // puts the own-style node unconditionally first with nothing able to outrank it, so
            // the own-style green stays instead of the "important" red.
            CascadeWidget e3 = CreateWidget("c5elemE3", new string[] { "c5low3" }, null);
            TestEnvironment.LoadCss(".c5low3 { color: #ff0000; }");
            e3.Relayout();
            e3.SetProperty(WidgetParameterIndex.TextColor, (uint)0x00ff00);

            uint e3Color = e3.GetProperty(WidgetParameterIndex.TextColor, (uint)0);
            context.AreEqual((uint)0xff0000, e3Color,
                "an !important stylesheet declaration must beat the element's own inline style; expected 0x{0:x6} (the class rule, as if it carried !important), got 0x{1:x6}", (uint)0xff0000, e3Color);
        }

        private static void TestCascadeKeywords(TestContext context)
        {
            TestEnvironment.Setup();

            // The literal tokens cannot even be parsed: ConversionHelper.FloatParse/UintParse
            // try to read them as a number or a color and throw, which
            // WidgetManager.InitCssParameters wraps as a WidgetException, so LoadCSS itself
            // throws for a rule using any of the four keywords.
            context.Throws(typeof(WidgetException), delegate { TestEnvironment.LoadCss(".c6throw_width_inherit { width: inherit; }"); },
                "width: inherit must be a legal CSS Cascade 3 declaration, but LoadCSS throws because ConversionHelper.FloatParse cannot parse the literal token \"inherit\" as a float");
            context.Throws(typeof(WidgetException), delegate { TestEnvironment.LoadCss(".c6throw_color_initial { color: initial; }"); },
                "color: initial must be a legal CSS Cascade 3 declaration, but LoadCSS throws because ConversionHelper.UintParse cannot parse the literal token \"initial\" as a color");
            context.Throws(typeof(WidgetException), delegate { TestEnvironment.LoadCss(".c6throw_color_unset { color: unset; }"); },
                "color: unset must be a legal CSS Cascade 3 declaration, but LoadCSS throws because ConversionHelper.UintParse cannot parse the literal token \"unset\" as a color");
            context.Throws(typeof(WidgetException), delegate { TestEnvironment.LoadCss(".c6throw_width_unset { width: unset; }"); },
                "width: unset must be a legal CSS Cascade 3 declaration, but LoadCSS throws because ConversionHelper.FloatParse cannot parse the literal token \"unset\" as a float");

            // As if the child below carried "width: inherit;" -- CSS Cascade 3 says width
            // should then take the parent's USED width (300) even though width is Initial
            // (not inherited). Since the keyword cannot be written at all, the child simply has
            // no width rule, which resolves to its own initial 0, not the parent's 300.
            CascadeWidget parent = CreateWidget("c6elemFP", new string[] { "c6parent" }, null);
            CascadeWidget child = CreateWidget("c6elemFC", new string[] { "c6child" }, null);
            TestEnvironment.LoadCss(".c6parent { width: 300px; color: #ff0000; }");
            parent.Relayout();
            parent.AddChild(child);
            child.Relayout();

            float childWidth = child.Size.X;
            context.AreEqualFloat(300f, childWidth, 0.01f,
                "width: inherit must take the parent's used width even though width is not inherited; expected 300, got {0}", childWidth);

            // As if the same child also carried "color: initial;" -- CSS Cascade 3 says it
            // should reset to the property's initial value instead of the value it would
            // otherwise inherit. Since the keyword cannot be written at all, the child just
            // keeps inheriting the parent's red.
            uint childColor = child.GetProperty(WidgetParameterIndex.TextColor, (uint)0x123456);
            context.AreEqual((uint)0x123456, childColor,
                "color: initial must reset to the initial value instead of the inherited one; expected the initial sentinel 0x{0:x6}, got 0x{1:x6} (the parent's inherited color)", (uint)0x123456, childColor);
        }

        private static void TestPseudoClassState(TestContext context)
        {
            TestEnvironment.Setup();

            // G1: :hover applies while hovered, stops applying once not hovered, and inherits
            // the non-pseudo rule's other properties while it is active.
            CascadeWidget g1 = CreateWidget("c7elemG1", new string[] { "c7btn1" }, null);
            TestEnvironment.LoadCss(
                ".c7btn1 { background-color: #ff0000; color: #0000ff; }" +
                ".c7btn1:hover { background-color: #00ff00; }");

            g1.Relayout();
            uint g1BaseBack = g1.GetProperty(WidgetParameterIndex.BackColor, (uint)0);
            context.AreEqual((uint)0xff0000, g1BaseBack,
                "without :hover the base rule must apply; expected 0x{0:x6}, got 0x{1:x6}", (uint)0xff0000, g1BaseBack);

            g1.Hovered = true;
            g1.Relayout();
            uint g1HoverBack = g1.GetProperty(WidgetParameterIndex.BackColor, (uint)0);
            context.AreEqual((uint)0x00ff00, g1HoverBack,
                ":hover must apply once the widget is hovered; expected 0x{0:x6}, got 0x{1:x6}", (uint)0x00ff00, g1HoverBack);

            uint g1HoverText = g1.GetProperty(WidgetParameterIndex.TextColor, (uint)0);
            context.AreEqual((uint)0x0000ff, g1HoverText,
                "the :hover rule must still inherit the base rule's other properties (color); expected 0x{0:x6}, got 0x{1:x6}", (uint)0x0000ff, g1HoverText);

            g1.Hovered = false;
            g1.Relayout();
            uint g1AfterBack = g1.GetProperty(WidgetParameterIndex.BackColor, (uint)0);
            context.AreEqual((uint)0xff0000, g1AfterBack,
                ":hover must stop applying once the widget is no longer hovered; expected 0x{0:x6}, got 0x{1:x6}", (uint)0xff0000, g1AfterBack);

            // G2: Enabled = false brings in a :disabled rule.
            CascadeWidget g2 = CreateWidget("c7elemG2", new string[] { "c7btn2" }, null);
            TestEnvironment.LoadCss(
                ".c7btn2 { background-color: #ff0000; }" +
                ".c7btn2:disabled { background-color: #00ff00; }");

            g2.Relayout();
            uint g2BeforeBack = g2.GetProperty(WidgetParameterIndex.BackColor, (uint)0);
            context.AreEqual((uint)0xff0000, g2BeforeBack,
                "an enabled widget must not match :disabled; expected 0x{0:x6}, got 0x{1:x6}", (uint)0xff0000, g2BeforeBack);

            g2.Enabled = false;
            g2.Relayout();
            uint g2AfterBack = g2.GetProperty(WidgetParameterIndex.BackColor, (uint)0);
            context.AreEqual((uint)0x00ff00, g2AfterBack,
                "Enabled = false must bring in the :disabled rule; expected 0x{0:x6}, got 0x{1:x6}", (uint)0x00ff00, g2AfterBack);

            // G3: combined pseudo-classes (:hover:focus) resolve, and require BOTH states
            // together -- neither state alone is enough.
            CascadeWidget g3 = CreateWidget("c7elemG3", new string[] { "c7btn3" }, null);
            TestEnvironment.LoadCss(
                ".c7btn3 { background-color: #ff0000; }" +
                ".c7btn3:hover:focus { background-color: #00ff00; }");

            g3.Relayout();
            g3.Hovered = true;
            g3.Relayout();
            uint g3HoverOnlyBack = g3.GetProperty(WidgetParameterIndex.BackColor, (uint)0);
            context.AreEqual((uint)0xff0000, g3HoverOnlyBack,
                ":hover alone must not match a :hover:focus rule; expected 0x{0:x6}, got 0x{1:x6}", (uint)0xff0000, g3HoverOnlyBack);

            g3.Selected = true;
            g3.Relayout();
            uint g3HoverFocusBack = g3.GetProperty(WidgetParameterIndex.BackColor, (uint)0);
            context.AreEqual((uint)0x00ff00, g3HoverFocusBack,
                "combined :hover:focus must resolve once both states are active; expected 0x{0:x6}, got 0x{1:x6}", (uint)0x00ff00, g3HoverFocusBack);

            g3.Hovered = false;
            g3.Relayout();
            uint g3FocusOnlyBack = g3.GetProperty(WidgetParameterIndex.BackColor, (uint)0);
            context.AreEqual((uint)0xff0000, g3FocusOnlyBack,
                ":focus alone must not match a :hover:focus rule; expected 0x{0:x6}, got 0x{1:x6}", (uint)0xff0000, g3FocusOnlyBack);
        }
    }
}
