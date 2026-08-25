using System;
using System.Collections.Generic;

using NewWidgets.UI.Styles;

namespace NewWidgets.Test
{
    /// <summary>
    /// Test groups for the CSS selector engine: <see cref="StyleSelector"/> parsing and
    /// comparison, <see cref="StyleSelectorList"/> combinators and specificity, and
    /// <see cref="StyleCollection"/> matching.
    /// </summary>
    internal static class SelectorTests
    {
        /// <summary>
        /// Trivial <see cref="IStyleData"/> carrying only an identifying marker string, so
        /// tests can check which rule's data made it into a GetStyleData() result.
        /// </summary>
        private sealed class MarkerStyleData : IStyleData
        {
            private readonly string m_marker;

            public string Marker
            {
                get { return m_marker; }
            }

            public MarkerStyleData(string marker)
            {
                m_marker = marker;
            }

            public void LoadData(IStyleData data)
            {
                // ponytail: tests never trigger a rule merge (each rule string is unique
                // within its collection), so there is nothing to combine here.
            }
        }

        public static void Register()
        {
            TestRunner.Add("Test 10: selector parsing", Test10_SelectorParsing);

            TestRunner.AddKnownFailure("Test 11: attribute selectors are parsed then discarded",
                "StyleSelector's string constructor (StyleSelector.cs) matches an attribute condition into the regex's 'attributes' group but never reads match.Groups[\"attributes\"] -- the comment there reads '// ignoring attributes for now' -- so 'input[type=text]' parses identically to bare 'input'",
                Test11_AttributeSelectorsDiscarded);

            TestRunner.Add("Test 12: selector lists and combinators", Test12_SelectorListsAndCombinators);

            TestRunner.Add("Test 13: specificity", Test13_Specificity);

            TestRunner.AddKnownFailure("Test 14: specificity defects",
                "StyleSelectorList.Analyze()'s final line 'return complex || universal ? 0 : countA * 100000 + countB * 100 + countC;' zeroes the specificity of the WHOLE chain whenever any sibling/adjacent-sibling combinator is present (complex==true) or a universal selector appears ANYWHERE in the chain (universal==true), instead of that compound selector merely contributing (0,0,0) per CSS 2.1 6.4.3; separately, Analyze()'s '// TODO: count of attributes' means attribute conditions never add to countB",
                Test14_SpecificityDefects);

            TestRunner.Add("Test 15: descendant matching", Test15_DescendantMatching);

            TestRunner.AddKnownFailure("Test 16: child combinator behaves as descendant",
                "StyleSelectorList.Analyze() folds StyleSelectorCombinator.Child into the same switch case as Descendant (comment: 'Nomad: temporary tread child as descendands, TODO'), and StyleSelectorList.AppliesTo()'s tail-scan loop only ever reads m_selectors, never m_combinators, so a '>' rule matches at any descendant depth instead of requiring direct adjacency",
                Test16_ChildCombinatorBehavesAsDescendant);

            // AppliesTo()'s own TODO comment claims the worked example below "is not working
            // properly", but empirically it already matches correctly: its tail-scan loop
            // (StyleCollection.cs is not involved here, this is pure StyleSelectorList
            // matching) walks 'other' backward and, for each rule selector, keeps consuming
            // positions until one matches -- which correctly skips arbitrary intervening
            // ancestors for a pure descendant chain and preserves left-to-right order. So
            // this is registered as a normal passing group, not a known failure; see the
            // task report for the verification trace.
            TestRunner.Add("Test 17: deep descendant matching", Test17_DeepDescendantMatching);
        }

        // ----------------------------------------------------------------
        // Test 10: selector parsing
        // ----------------------------------------------------------------

        private static void Test10_SelectorParsing(TestContext context)
        {
            StyleSelector bareElement = new StyleSelector("button");
            context.AreEqual("button", bareElement.Element, "bare element 'button' should parse Element as 'button', got '{0}'", bareElement.Element);
            context.AreEqual(0, bareElement.Classes.Length, "bare element 'button' should have no classes, got {0}", bareElement.Classes.Length);
            context.AreEqual("", bareElement.Id, "bare element 'button' should have no id, got '{0}'", bareElement.Id);
            context.IsNull(bareElement.PseudoClasses, "bare element 'button' should have no pseudo-classes");
            context.AreEqual("button", bareElement.ToString(), "ToString() of 'button' should round-trip, got '{0}'", bareElement.ToString());

            StyleSelector idOnly = new StyleSelector("#myid");
            context.AreEqual("", idOnly.Element, "'#myid' should have empty Element, got '{0}'", idOnly.Element);
            context.AreEqual("myid", idOnly.Id, "'#myid' should parse Id as 'myid' with the leading # stripped, got '{0}'", idOnly.Id);
            context.AreEqual("#myid", idOnly.ToString(), "ToString() of '#myid' should round-trip, got '{0}'", idOnly.ToString());

            StyleSelector twoClasses = new StyleSelector(".one.two");
            context.AreEqual(2, twoClasses.Classes.Length, "'.one.two' should parse to 2 classes, got {0}", twoClasses.Classes.Length);
            context.AreEqual("one", twoClasses.Classes[0], "'.one.two' first class should be 'one', got '{0}'", twoClasses.Classes[0]);
            context.AreEqual("two", twoClasses.Classes[1], "'.one.two' second class should be 'two', got '{0}'", twoClasses.Classes[1]);
            context.AreEqual(".one.two", twoClasses.ToString(), "ToString() of '.one.two' should round-trip, got '{0}'", twoClasses.ToString());

            StyleSelector full = new StyleSelector("button#id.one.two:hover");
            context.AreEqual("button", full.Element, "full form Element should be 'button', got '{0}'", full.Element);
            context.AreEqual("id", full.Id, "full form Id should be 'id', got '{0}'", full.Id);
            context.AreEqual(2, full.Classes.Length, "full form should have 2 classes, got {0}", full.Classes.Length);
            context.AreEqual("one", full.Classes[0], "full form first class should be 'one', got '{0}'", full.Classes[0]);
            context.AreEqual("two", full.Classes[1], "full form second class should be 'two', got '{0}'", full.Classes[1]);
            context.AreEqual(1, full.PseudoClasses.Length, "full form should have 1 pseudo-class, got {0}", full.PseudoClasses.Length);
            context.AreEqual(":hover", full.PseudoClasses[0], "full form pseudo-class should be ':hover', got '{0}'", full.PseudoClasses[0]);
            context.AreEqual("button#id.one.two:hover", full.ToString(), "ToString() of the full form should round-trip, got '{0}'", full.ToString());

            StyleSelector universal = new StyleSelector("*");
            context.AreEqual("*", universal.Element, "'*' should parse Element as '*', got '{0}'", universal.Element);
            context.AreEqual("*", universal.ToString(), "ToString() of '*' should round-trip, got '{0}'", universal.ToString());

            StyleSelector pseudoElement = new StyleSelector("::first-line");
            context.AreEqual(1, pseudoElement.PseudoClasses.Length, "'::first-line' should parse to 1 pseudo-class token, got {0}", pseudoElement.PseudoClasses.Length);
            context.AreEqual("::first-line", pseudoElement.PseudoClasses[0], "'::first-line' pseudo-class token should be '::first-line', got '{0}'", pseudoElement.PseudoClasses[0]);
            context.AreEqual("::first-line", pseudoElement.ToString(), "ToString() of '::first-line' should round-trip, got '{0}'", pseudoElement.ToString());

            StyleSelector functionalPseudo = new StyleSelector(":not(disabled)");
            context.AreEqual(1, functionalPseudo.PseudoClasses.Length, "':not(disabled)' should parse to 1 pseudo-class token, got {0}", functionalPseudo.PseudoClasses.Length);
            context.AreEqual(":not(disabled)", functionalPseudo.PseudoClasses[0], "':not(disabled)' pseudo-class token should be ':not(disabled)', got '{0}'", functionalPseudo.PseudoClasses[0]);
            context.AreEqual(":not(disabled)", functionalPseudo.ToString(), "ToString() of ':not(disabled)' should round-trip, got '{0}'", functionalPseudo.ToString());

            StyleSelector stackedPseudo = new StyleSelector("a:hover:focus");
            context.AreEqual(2, stackedPseudo.PseudoClasses.Length, "'a:hover:focus' should parse to 2 pseudo-class tokens, got {0}", stackedPseudo.PseudoClasses.Length);
            context.AreEqual(":hover", stackedPseudo.PseudoClasses[0], "'a:hover:focus' first pseudo-class should be ':hover', got '{0}'", stackedPseudo.PseudoClasses[0]);
            context.AreEqual(":focus", stackedPseudo.PseudoClasses[1], "'a:hover:focus' second pseudo-class should be ':focus', got '{0}'", stackedPseudo.PseudoClasses[1]);
            context.AreEqual("a:hover:focus", stackedPseudo.ToString(), "ToString() of 'a:hover:focus' should round-trip, got '{0}'", stackedPseudo.ToString());

            // Equals
            StyleSelector buttonA = new StyleSelector("button#id.one.two:hover");
            StyleSelector buttonB = new StyleSelector("button#id.one.two:hover");
            context.IsTrue(buttonA.Equals(buttonB), "two selectors parsed from the same string should be Equals()");

            StyleSelector buttonDifferentId = new StyleSelector("button#other.one.two:hover");
            context.IsFalse(buttonA.Equals(buttonDifferentId), "selectors differing only by id should not be Equals()");

            // IsSubset -- doc comment example: 'button' is a subset match for 'button.foo:hover',
            // but 'button#id' is not, because 'button.foo:hover' has no id.
            StyleSelector bareButton = new StyleSelector("button");
            StyleSelector decoratedButton = new StyleSelector("button.foo:hover");
            StyleSelector idButton = new StyleSelector("button#id");

            context.IsTrue(bareButton.IsSubset(decoratedButton), "'button' should be a subset match for 'button.foo:hover'");
            context.IsFalse(idButton.IsSubset(decoratedButton), "'button#id' should NOT be a subset match for 'button.foo:hover' (it has no id to satisfy)");

            // IsChild -- true when only pseudo-classes differ; asymmetric, since the reverse
            // direction requires this selector's own pseudo-classes to be present in other's.
            StyleSelector plainButton = new StyleSelector("button");
            StyleSelector hoverButton = new StyleSelector("button:hover");

            context.IsTrue(plainButton.IsChild(hoverButton), "'button' should be IsChild() of 'button:hover' (only pseudo-classes differ)");
            context.IsFalse(hoverButton.IsChild(plainButton), "'button:hover' should NOT be IsChild() of 'button' (asymmetric: 'button' has no ':hover' to satisfy)");
        }

        // ----------------------------------------------------------------
        // Test 11: attribute selectors are parsed then discarded (KNOWN FAILURE)
        // ----------------------------------------------------------------

        private static void Test11_AttributeSelectorsDiscarded(TestContext context)
        {
            StyleSelector withAttribute = new StyleSelector("input[type=text]");
            StyleSelector bareInput = new StyleSelector("input");

            context.IsFalse(withAttribute.Equals(bareInput), "'input[type=text]' must not Equals() the bare 'input' selector -- the attribute condition should still distinguish them");
            context.AreEqual("input[type=text]", withAttribute.ToString(), "ToString() of 'input[type=text]' should retain the attribute condition, got '{0}'", withAttribute.ToString());
        }

        // ----------------------------------------------------------------
        // Test 12: selector lists and combinators
        // ----------------------------------------------------------------

        private static void Test12_SelectorListsAndCombinators(TestContext context)
        {
            StyleSelectorList descendantChain = new StyleSelectorList("a b c");
            context.AreEqual(3, descendantChain.Count, "'a b c' should parse to 3 selectors, got {0}", descendantChain.Count);
            context.AreEqual(StyleSelectorCombinator.Descendant, descendantChain.Operators[0], "'a b c' first combinator should be Descendant, got {0}", descendantChain.Operators[0]);
            context.AreEqual(StyleSelectorCombinator.Descendant, descendantChain.Operators[1], "'a b c' second combinator should be Descendant, got {0}", descendantChain.Operators[1]);
            context.IsTrue(descendantChain.IsSingleChain, "'a b c' should be a single chain");
            context.AreEqual("a b c", descendantChain.ToString(), "ToString() of 'a b c' should round-trip, got '{0}'", descendantChain.ToString());

            StyleSelectorList commaList = new StyleSelectorList("a, b");
            context.AreEqual(2, commaList.Count, "'a, b' should parse to 2 selectors, got {0}", commaList.Count);
            context.IsFalse(commaList.IsSingleChain, "'a, b' should not be a single chain");
            IList<StyleSelectorList> split = commaList.Split();
            context.AreEqual(2, split.Count, "Split() of 'a, b' should yield 2 chains, got {0}", split.Count);
            context.IsTrue(split[0].IsSingleChain, "each split chain of 'a, b' should itself be a single chain");
            context.IsTrue(split[1].IsSingleChain, "each split chain of 'a, b' should itself be a single chain");
            context.AreEqual("a, b", commaList.ToString(), "ToString() of 'a, b' should round-trip, got '{0}'", commaList.ToString());

            StyleSelectorList childCombinator = new StyleSelectorList("a > b");
            context.AreEqual(StyleSelectorCombinator.Child, childCombinator.Operators[0], "'a > b' combinator should be Child, got {0}", childCombinator.Operators[0]);
            context.AreEqual("a > b", childCombinator.ToString(), "ToString() of 'a > b' should round-trip, got '{0}'", childCombinator.ToString());

            StyleSelectorList adjacentSibling = new StyleSelectorList("a + b");
            context.AreEqual(StyleSelectorCombinator.AdjacentSibling, adjacentSibling.Operators[0], "'a + b' combinator should be AdjacentSibling, got {0}", adjacentSibling.Operators[0]);
            context.AreEqual("a + b", adjacentSibling.ToString(), "ToString() of 'a + b' should round-trip, got '{0}'", adjacentSibling.ToString());

            StyleSelectorList sibling = new StyleSelectorList("a ~ b");
            context.AreEqual(StyleSelectorCombinator.Sibling, sibling.Operators[0], "'a ~ b' combinator should be Sibling, got {0}", sibling.Operators[0]);
            context.AreEqual("a ~ b", sibling.ToString(), "ToString() of 'a ~ b' should round-trip, got '{0}'", sibling.ToString());

            context.Throws(typeof(ArgumentException), ThrowTrailingCombinator, "a selector string ending with a combinator ('a >') should throw ArgumentException");

            // IsSimple / IsSingleChain / IsComplex
            context.IsTrue(new StyleSelectorList("a").IsSimple, "'a' alone should be IsSimple (a single selector)");
            context.IsFalse(new StyleSelectorList("a b").IsSimple, "'a b' should not be IsSimple (more than one selector)");
            context.IsTrue(new StyleSelectorList("a b").IsSingleChain, "'a b' should be IsSingleChain (no comma)");
            context.IsFalse(new StyleSelectorList("a b").IsComplex, "'a b' (descendant only) should not be IsComplex");
            context.IsFalse(new StyleSelectorList("a > b").IsComplex, "'a > b' (child combinator) should not be IsComplex");
            context.IsTrue(new StyleSelectorList("a + b").IsComplex, "'a + b' (adjacent sibling) should be IsComplex");
            context.IsTrue(new StyleSelectorList("a ~ b").IsComplex, "'a ~ b' (sibling) should be IsComplex");
        }

        private static void ThrowTrailingCombinator()
        {
            new StyleSelectorList("a >");
        }

        // ----------------------------------------------------------------
        // Test 13: specificity
        // ----------------------------------------------------------------

        private static void Test13_Specificity(TestContext context)
        {
            // '*' alone cannot go through the public string constructor: StyleSelectorList's
            // own tokenizer regex (s_selectorParser) has no '*' in its token character class,
            // so a bare "*" fails to tokenize at all and the constructor throws. Build it
            // directly from a StyleSelector instead, bypassing that tokenizer.
            int universalSpecificity = BuildSelectorList(new StyleSelector("*")).Specificity;
            context.AreEqual(0, universalSpecificity, "'*' should have specificity 0, got {0}", universalSpecificity);

            int liSpecificity = new StyleSelectorList("li").Specificity;
            context.AreEqual(1, liSpecificity, "'li' should have specificity 1, got {0}", liSpecificity);

            int twoElementSpecificity = new StyleSelectorList("ul li").Specificity;
            context.AreEqual(2, twoElementSpecificity, "'ul li' should have specificity 2, got {0}", twoElementSpecificity);

            int classSpecificity = new StyleSelectorList(".cls").Specificity;
            context.IsTrue(classSpecificity > twoElementSpecificity, "a single class '.cls' ({0}) should outrank two element names 'ul li' ({1})", classSpecificity, twoElementSpecificity);

            int idSpecificity = new StyleSelectorList("#id").Specificity;
            int threeClassSpecificity = new StyleSelectorList(".a.b.c").Specificity;
            context.IsTrue(idSpecificity > threeClassSpecificity, "an id '#id' ({0}) should outrank three classes '.a.b.c' ({1})", idSpecificity, threeClassSpecificity);

            int childSpecificity = new StyleSelectorList("ul > li").Specificity;
            context.AreEqual(twoElementSpecificity, childSpecificity, "'ul > li' ({0}) should have the same specificity as 'ul li' ({1}) -- a child combinator does not change specificity", childSpecificity, twoElementSpecificity);

            int hoverSpecificity = new StyleSelectorList("a:hover").Specificity;
            context.AreEqual(101, hoverSpecificity, "'a:hover' should count the pseudo-class in the b bucket: 1 element (c) + 1 pseudo-class (b) = 101, got {0}", hoverSpecificity);
        }

        // ----------------------------------------------------------------
        // Test 14: specificity defects (KNOWN FAILURE)
        // ----------------------------------------------------------------

        private static void Test14_SpecificityDefects(TestContext context)
        {
            // A sibling combinator must not zero the specificity: 'a + b' is two element
            // names, so its specificity should be 2, exactly like 'a b' or 'a > b'.
            int siblingSpecificity = new StyleSelectorList("a + b").Specificity;
            context.AreEqual(2, siblingSpecificity, "'a + b' should have specificity 2 (2 element names), got {0}", siblingSpecificity);

            // A universal selector anywhere in the chain must not zero the rest of the
            // chain: '#id *' still has an id in it, so it must outrank a bare class.
            // Same tokenizer limitation as Test 13 -- '*' cannot appear in a string passed
            // to the public StyleSelectorList(string) constructor, so this is built directly
            // from StyleSelector objects instead of parsing "#id *" as a string.
            int idStarSpecificity = BuildSelectorList(new StyleSelector("#id"), new StyleSelector("*")).Specificity;
            int classSpecificity = new StyleSelectorList(".cls").Specificity;
            context.IsTrue(idStarSpecificity > classSpecificity, "'#id *' ({0}) should still outrank '.cls' ({1})", idStarSpecificity, classSpecificity);

            // An attribute condition counts in the b bucket, so it must outrank a bare
            // element with no conditions at all.
            int attributeSpecificity = new StyleSelectorList("input[type=text]").Specificity;
            int bareInputSpecificity = new StyleSelectorList("input").Specificity;
            context.IsTrue(attributeSpecificity > bareInputSpecificity, "'input[type=text]' ({0}) should outrank bare 'input' ({1})", attributeSpecificity, bareInputSpecificity);
        }

        // ----------------------------------------------------------------
        // Test 15: descendant matching
        // ----------------------------------------------------------------

        private static void Test15_DescendantMatching(TestContext context)
        {
            // 'panel label' matches a label inside a panel, but not a bare top-level label.
            StyleCollection nestedCollection = new StyleCollection();
            nestedCollection.AddStyle("panel label", new MarkerStyleData("panel-label-rule"));

            StyleSelectorList labelInsidePanel = new StyleSelectorList("panel label");
            StyleSelectorList bareTopLevelLabel = new StyleSelectorList("label");

            context.IsTrue(ContainsMarker(nestedCollection.GetStyleData(labelInsidePanel), "panel-label-rule"), "'panel label' rule should match a label nested inside a panel");
            context.IsFalse(ContainsMarker(nestedCollection.GetStyleData(bareTopLevelLabel), "panel-label-rule"), "'panel label' rule should NOT match a bare top-level label");

            // A bare 'label' rule matches both a top-level label and a label nested inside a panel.
            StyleCollection bareRuleCollection = new StyleCollection();
            bareRuleCollection.AddStyle("label", new MarkerStyleData("bare-label-rule"));

            context.IsTrue(ContainsMarker(bareRuleCollection.GetStyleData(bareTopLevelLabel), "bare-label-rule"), "bare 'label' rule should match a top-level label");
            context.IsTrue(ContainsMarker(bareRuleCollection.GetStyleData(labelInsidePanel), "bare-label-rule"), "bare 'label' rule should also match a label nested inside a panel");

            // '.target' matches an element carrying that class among several others.
            StyleCollection classCollection = new StyleCollection();
            classCollection.AddStyle(".target", new MarkerStyleData("class-rule"));

            StyleSelectorList multiClassElement = new StyleSelectorList(new StyleSelector("button", new[] { "foo", "bar", "target" }, ""), StyleNodeMatch.None);
            context.IsTrue(ContainsMarker(classCollection.GetStyleData(multiClassElement), "class-rule"), "'.target' rule should match an element carrying classes foo, bar, target");

            // '#uniqueId123' matches only the element with that id.
            StyleCollection idCollection = new StyleCollection();
            idCollection.AddStyle("#uniqueId123", new MarkerStyleData("id-rule"));

            StyleSelectorList elementWithMatchingId = new StyleSelectorList(new StyleSelector("span", (string[])null, "uniqueId123"), StyleNodeMatch.None);
            StyleSelectorList elementWithOtherId = new StyleSelectorList(new StyleSelector("span", (string[])null, "otherId456"), StyleNodeMatch.None);

            context.IsTrue(ContainsMarker(idCollection.GetStyleData(elementWithMatchingId), "id-rule"), "'#uniqueId123' rule should match the element carrying that id");
            context.IsFalse(ContainsMarker(idCollection.GetStyleData(elementWithOtherId), "id-rule"), "'#uniqueId123' rule should not match an element with a different id");

            // A rule that cannot match anything in the queried path yields no data at all.
            StyleCollection noMatchCollection = new StyleCollection();
            noMatchCollection.AddStyle("nomatchelementtype", new MarkerStyleData("unused"));

            StyleSelectorList unrelatedElement = new StyleSelectorList(new StyleSelector("somethingcompletelyunrelated", (string[])null, ""), StyleNodeMatch.None);
            context.IsNull(noMatchCollection.GetStyleData(unrelatedElement), "a rule that cannot match anything should yield no style data");
        }

        private static bool ContainsMarker(ICollection<StyleNodeMatchPair> results, string marker)
        {
            if (results == null)
                return false;

            foreach (StyleNodeMatchPair pair in results)
            {
                MarkerStyleData data = pair.Node.Data as MarkerStyleData;

                if (data != null && data.Marker == marker)
                    return true;
            }

            return false;
        }

        // ----------------------------------------------------------------
        // Test 16: child combinator behaves as descendant (KNOWN FAILURE)
        // ----------------------------------------------------------------

        private static void Test16_ChildCombinatorBehavesAsDescendant(TestContext context)
        {
            StyleSelectorList rule = new StyleSelectorList("panel > label");

            StyleSelectorList directChild = BuildPath("panel", "label");
            context.IsTrue(rule.AppliesTo(directChild), "'panel > label' should match a label that is a direct child of a panel");

            // A negative control built as 'panel' > 'panel' > 'label' would NOT expose the
            // bug: the label's immediate ancestor there is itself a 'panel', so it is a
            // genuine direct-child match under correct CSS semantics too. The negative
            // control needs the label's *immediate* parent to be something other than
            // 'panel' -- an intervening 'box' makes label a grandchild, not a child, of panel.
            StyleSelectorList grandchildThroughBox = BuildPath("panel", "box", "label");
            context.IsFalse(rule.AppliesTo(grandchildThroughBox), "'panel > label' should NOT match a label nested one level deeper than a direct child (panel containing box containing label)");
        }

        // Builds a single-chain StyleSelectorList directly from already-parsed
        // StyleSelector objects, bypassing StyleSelectorList's own string tokenizer (whose
        // regex cannot represent every valid selector, e.g. '*' -- see Test 13/14).
        private static StyleSelectorList BuildSelectorList(params StyleSelector[] selectors)
        {
            List<StyleNodeMatch> types = new List<StyleNodeMatch>(selectors.Length);

            for (int i = 0; i < selectors.Length; i++)
                types.Add(StyleNodeMatch.None);

            return new StyleSelectorList(selectors, types);
        }

        private static StyleSelectorList BuildPath(params string[] elementNames)
        {
            List<StyleSelector> selectors = new List<StyleSelector>(elementNames.Length);
            List<StyleNodeMatch> types = new List<StyleNodeMatch>(elementNames.Length);

            for (int i = 0; i < elementNames.Length; i++)
            {
                selectors.Add(new StyleSelector(elementNames[i], (string[])null, ""));
                types.Add(StyleNodeMatch.None);
            }

            return new StyleSelectorList(selectors, types);
        }

        // ----------------------------------------------------------------
        // Test 17: deep descendant matching (KNOWN FAILURE)
        // ----------------------------------------------------------------

        private static void Test17_DeepDescendantMatching(TestContext context)
        {
            StyleSelectorList rule = new StyleSelectorList("ul li b");
            StyleSelectorList worked = new StyleSelectorList("html ul li ul li b#b");

            context.IsTrue(rule.AppliesTo(worked), "'ul li b' should match 'html ul li ul li b#b' (the worked example from AppliesTo's own TODO comment)");

            StyleSelectorList outOfOrderRule = new StyleSelectorList("ul ul b");
            StyleSelectorList outOfOrder = new StyleSelectorList("ul b ul");

            context.IsFalse(outOfOrderRule.AppliesTo(outOfOrder), "'ul ul b' should NOT match 'ul b ul' -- the two 'ul' ancestors are not both ancestors of 'b' in the right order");
        }
    }
}
