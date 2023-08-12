using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using NewWidgets.Utility;

namespace NewWidgets.UI.Styles
{
    /// <summary>
    /// Helper class processed from CSS string. Can contain one or many selector chains
    /// </summary>
    public class StyleSelectorList
    {
        /// <summary>
        /// Regular expression to separate selectors to different groups
        /// </summary>
        private static readonly Regex s_selectorParser = new Regex(@"(@{0,1}[\w#:_\-\[\]()\.\='\^\/]+)([\s,+>~]+)", RegexOptions.Compiled);

        private readonly IList<StyleSelector> m_selectors;
        private readonly IList<StyleSelectorCombinator> m_combinators;
        private readonly IList<StyleNodeMatch> m_types;
        private readonly int m_chainCount;
        private readonly bool m_complex;
        private readonly int m_specificity;

        public bool IsEmpty
        {
            get { return m_selectors.Count == 0; }
        }

        /// <summary>
        /// Returns true if there is only one selector without inheritance
        /// </summary>
        public bool IsSimple
        {
            get { return m_selectors.Count == 1; }
        }

        /// <summary>
        /// Returns true if there is only one selector chain without extra SelectorOperator.None
        /// </summary>
        public bool IsSingleChain
        {
            get { return m_chainCount == 1; }
        }

        /// <summary>
        /// Returns true if there are complex operators in selector
        /// </summary>
        public bool IsComplex
        {
            get { return m_complex; }
        }

        public int Count
        {
            get { return m_selectors.Count; }
        }

        public int Specificity
        {
            get { return m_specificity; }
        }

        public IList<StyleSelector> Selectors
        {
            get { return m_selectors; }
        }

        public IList<StyleSelectorCombinator> Operators
        {
            get { return m_combinators; }
        }

        internal IList<StyleNodeMatch> Types
        {
            get { return m_types; }
        }

        /// <summary>
        /// Creates style selectors from CSS string honoring combinators
        /// </summary>
        /// <param name="selectorsString"></param>
        /// <exception cref="ArgumentException"></exception>
        public StyleSelectorList(string selectorsString)
        {
            // remove whitespaces to simplify parsing
            MatchCollection splitStrings = s_selectorParser.Matches(selectorsString + ",");

            //string[] splitStrings = selectorsString.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (splitStrings.Count == 0)
                throw new ArgumentException("Invalid argument passed to StyleSelectorList constructor - not a style string", selectorsString);

            List<StyleSelector> selectors = new List<StyleSelector>();
            List<StyleSelectorCombinator> combinators = new List<StyleSelectorCombinator>();

            StyleSelectorCombinator lastCombinator = StyleSelectorCombinator.None;

            foreach (Match match in splitStrings)
            {
                StyleSelectorCombinator combinator = StyleSelectorCombinator.Descendant; // space by default

                string selector = match.Groups[1].Value;
                string splitOperator = match.Groups[2].Value.Trim();

                if (splitOperator.Length > 0)
                {
                    switch (splitOperator[0])
                    {
                        case ',':
                            combinator = StyleSelectorCombinator.None;
                            break;
                        case '+':
                            combinator = StyleSelectorCombinator.AdjacentSibling;
                            break;
                        case '>':
                            combinator = StyleSelectorCombinator.Child;
                            break;
                        case '~':
                            combinator = StyleSelectorCombinator.Sibling;
                            break;
                        default: // there was a space symbol but it was removed during Split call so now first character is the name of the class or element
                            combinator = StyleSelectorCombinator.Descendant;
                            break;
                    }
                }

                selectors.Add(new StyleSelector(selector));
                combinators.Add(combinator);

                lastCombinator = combinator;
            }

            Match last = splitStrings[splitStrings.Count - 1];
            if (lastCombinator != StyleSelectorCombinator.None)
                throw new ArgumentException("StyleSelectorList constructor got a string ending with operator " + last, selectorsString);

            m_selectors = selectors.ToArray();
            m_combinators = combinators.ToArray();
            m_types = new StyleNodeMatch[m_selectors.Count];

            m_specificity = Analyze(out m_chainCount, out m_complex);
        }

        /// <summary>
        /// Creates style list with single selector
        /// </summary>
        /// <param name="singleSelector"></param>
        internal StyleSelectorList(StyleSelector singleSelector, StyleNodeMatch nodeType = StyleNodeMatch.None)
        {
            m_selectors = new[] { singleSelector };
            m_combinators = new[] { StyleSelectorCombinator.None };
            m_types = new[] { nodeType };
            m_specificity = Analyze(out m_chainCount, out m_complex);
        }

        /// <summary>
        /// Creates the list with specified collection of selectors and types
        /// </summary>
        /// <param name="selectors"></param>
        /// <param name="types"></param>
        internal StyleSelectorList(IList<StyleSelector> selectors, IList<StyleNodeMatch> types)
        {
            m_selectors = selectors;
            m_types = types;
            m_combinators = new StyleSelectorCombinator[selectors.Count];

            for (int i = 0; i < m_combinators.Count - 1; i++)
                m_combinators[i] = StyleSelectorCombinator.Descendant;
            m_combinators[m_combinators.Count - 1] = StyleSelectorCombinator.None;

            m_specificity = Analyze(out m_chainCount, out m_complex);
        }

        /// <summary>
        /// Creates the list with specified collection of selectors and combinators
        /// </summary>
        /// <param name="selectors"></param>
        /// <param name="combinators"></param>
        /// <param name="types"></param>
        internal StyleSelectorList(IList<StyleSelector> selectors, IList<StyleSelectorCombinator> combinators, IList<StyleNodeMatch> types)
        {
            m_selectors = selectors;
            m_types = types;
            m_combinators = combinators;

            m_specificity = Analyze(out m_chainCount, out m_complex);
        }

        internal StyleSelectorList(StyleSelectorList other, int start, int count)
            : this(new ListRange<StyleSelector>(other.m_selectors, start, count),
                  new ListRange<StyleSelectorCombinator>(other.m_combinators, start, count),
                  new ListRange<StyleNodeMatch>(other.m_types, start, count))
        {

        }


        // <summary>
        // This method analyzes the selector and combinators and sets the flags for them
        /// </summary>
        /// <param name="chainCount"></param>
        /// <param name="complex"></param>
        /// <returns>Specificity number</returns>
        private int Analyze(out int chainCount, out bool complex)
        {
            chainCount = 0;
            complex = false;

            bool universal = false;

            int countA = 0; // ids
            int countB = 0; // classes, pseudo-classes, attributes (not supported)
            int countC = 0; // types, pseudo-elements

            // combinators array is always the length of selector array having None as a last member
            for (int i = 0; i < m_combinators.Count; i++)
            {
                switch (m_combinators[i])
                {
                    case StyleSelectorCombinator.None: // separator
                        chainCount++;
                        break;
                    case StyleSelectorCombinator.Child: // Nomad: temporary tread child as descendands, TODO
                    case StyleSelectorCombinator.Descendant: // non complex, just an inheritance chain
                        break;
                    default: // complex
                        complex = true;
                        break;
                }

                if (!string.IsNullOrEmpty(m_selectors[i].Id))
                    countA++;

                if (m_selectors[i].Classes != null)
                    countB += m_selectors[i].Classes.Length;

                if (m_selectors[i].PseudoClasses != null) // TODO: different arrays for pseudo classes and pseudo-elements
                    countB += m_selectors[i].PseudoClasses.Length;

                // TODO: count of attributes

                if (!string.IsNullOrEmpty(m_selectors[i].Element))
                {
                    countC++;
                    if (m_selectors[i].Element == "*") // universal selector
                        universal = true;
                }
            }

            return complex || universal ? 0 : countA * 100000 + countB * 100 + countC; // instead of 1,2,3 we're returning 1002003
        }

        /// <summary>
        /// Split selector name to different chains if there is a comma sign
        /// </summary>
        /// <returns></returns>
        public IList<StyleSelectorList> Split()
        {
            if (m_chainCount == 1)
                return new[] { this };

            List<StyleSelectorList> result = new List<StyleSelectorList>();

            int chainStart = 0;

            for (int i = 0; i < m_combinators.Count; i++)
            {
                if (m_combinators[i] == StyleSelectorCombinator.None)
                {
                    result.Add(new StyleSelectorList(this, chainStart, i - chainStart + 1));
                    chainStart = i + 1;
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Check two selector lists for equality
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(StyleSelectorList other)
        {
            if (Count != other.Count)
                return false;

            for (int i = 0; i < other.Count; i++)
            {
                if (m_combinators[i] != other.Operators[i])
                    return false;

                if (m_types[i] != other.Types[i])
                    return false;

                if (!m_selectors[i].Equals(other.Selectors[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Check two selector lists if one can be applied to another. TODO: right now all selectors are assumed to be inheritance " "
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool AppliesTo(StyleSelectorList other)
        {
            if (!this.IsSingleChain)
                throw new ArgumentException("StyleSelectorList.AppliesTo called for non-single chain selector!");

            if (this.IsComplex)
                throw new ArgumentException("StyleSelectorList.AppliesTo called for complex selector!");

            // Here we are enumeration two collections from the tail.
            // for each entry from this.Selectors we need to find at least one entry in other.Selectors
            // if there is no corresponding entry - we fail
            // if other.Selectors is already enumerated but we have something in this.Selectors - we fail
            // otherwise we have a complete match

            int position = other.Selectors.Count;

            for (int i = m_selectors.Count - 1; i >= 0; i--)
            {
                bool found = false;

                while (--position >= 0)
                {
                    if (m_selectors[i].IsSubset(other.Selectors[position]))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return false;
            }

            // TODO: deep search using operators and everything.
            // right now below part is not working properly
            // it should take
            // "this = ul li b" and successfuly compare to "other = html ul li ul li b#b"

            return true;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < m_combinators.Count - 1; i++)
            {
                builder.Append(m_selectors[i].ToString());

                switch (m_combinators[i])
                {
                    case StyleSelectorCombinator.None:
                        builder.Append(", ");
                        break;
                    case StyleSelectorCombinator.Child:
                        builder.Append(" > ");
                        break;
                    case StyleSelectorCombinator.AdjacentSibling:
                        builder.Append(" + ");
                        break;
                    case StyleSelectorCombinator.Sibling:
                        builder.Append(" ~ ");
                        break;
                    default:
                    case StyleSelectorCombinator.Descendant:
                        builder.Append(' ');
                        break;
                }
            }
            builder.Append(m_selectors[m_selectors.Count - 1].ToString());

            return builder.ToString();
        }
    }
}
