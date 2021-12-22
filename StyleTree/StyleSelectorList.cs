using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace StyleTree
{
    /// <summary>
    /// Helper class processed from CSS string. Can contain one or main selector chains
    /// </summary>
    public class StyleSelectorList
    {
        private static readonly Regex s_selectorParser = new Regex(@"([\w#:\-\[\]()\.\='\^\/]+)([\s,+>~]+)", RegexOptions.Compiled);

        private readonly IList<StyleSelector> m_selectors;
        private readonly IList<StyleSelectorOperator> m_operators;
        private readonly int m_chainCount;

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

        public int Count
        {
            get { return m_selectors.Count; }
        }

        public IList<StyleSelector> Selectors
        {
            get { return m_selectors; }
        }

        public IList<StyleSelectorOperator> Operators
        {
            get { return m_operators; }
        }

        public StyleSelectorList(string selectorsString)
        {
            // remove whitespaces to simplify parsing
            MatchCollection splitStrings = s_selectorParser.Matches(selectorsString + ",");

            //string[] splitStrings = selectorsString.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (splitStrings.Count == 0)
                throw new ArgumentException("Invalid argument passed to StyleSelectorList constructor - not a style string", selectorsString);

            List<StyleSelector> selectors = new List<StyleSelector>();
            List<StyleSelectorOperator> operators = new List<StyleSelectorOperator>();

            StyleSelectorOperator lastOperator = StyleSelectorOperator.None;

            foreach (Match match in splitStrings)
            {
                StyleSelectorOperator @operator = StyleSelectorOperator.Inherit; // space by default

                string selector = match.Groups[1].Value;
                string splitOperator = match.Groups[2].Value.Trim();

                if (splitOperator.Length > 0)
                {
                    switch (splitOperator[0])
                    {
                        case ',':
                            @operator = StyleSelectorOperator.None;
                            break;
                        case '+':
                            @operator = StyleSelectorOperator.DirectSibling;
                            break;
                        case '>':
                            @operator = StyleSelectorOperator.Child;
                            break;
                        case '~':
                            @operator = StyleSelectorOperator.Sibling;
                            break;
                        default: // there was a space symbol but it was removed during Split call so now first character is the name of the class or element
                            @operator = StyleSelectorOperator.Inherit;
                            break;
                    }
                }

                selectors.Add(new StyleSelector(selector));
                operators.Add(@operator);

                lastOperator = @operator;

                //if (@operator != StyleSelectorOperator.Inherit)
                //i++;
            }

            Match last = splitStrings[splitStrings.Count - 1];
            if (lastOperator != StyleSelectorOperator.None)
                    throw new ArgumentException("StyleSelectorList constructor got a string ending with operator " + last, selectorsString);

            m_selectors = selectors.ToArray();
            m_operators = operators.ToArray();

            m_chainCount = CountChains();
        }

        internal StyleSelectorList(IList<StyleSelector> selectors, IList<StyleSelectorOperator> operators)
        {
            m_selectors = selectors;
            m_operators = operators;
            m_chainCount = CountChains();
        }

        internal StyleSelectorList(StyleSelectorList other, int start, int count)
            : this(new ListRange<StyleSelector>(other.m_selectors, start, count),
                  new ListRange<StyleSelectorOperator>(other.m_operators, start, count))
        {

        }


        /// <summary>
        /// Returns amount of separate chains
        /// </summary>
        private int CountChains()
        {
            int count = 0;

            // operators array is always the length of selector array having None as a last member

            for (int i = 0; i < m_operators.Count; i++)
                if (m_operators[i] == StyleSelectorOperator.None)
                    count++;

            return count;
        }

        public IList<StyleSelectorList> Split()
        {
            if (m_chainCount == 1)
                return new[] { this };

            List<StyleSelectorList> result = new List<StyleSelectorList>();

            int chainStart = 0;

            for (int i = 0; i < m_operators.Count; i++)
            {
                if (m_operators[i] == StyleSelectorOperator.None)
                {
                    result.Add(new StyleSelectorList(this, chainStart, i - chainStart + 1));
                    chainStart = i + 1;
                }
            }

            return result.ToArray();
        }

        public StyleSelectorList Range(int from, int length)
        {
            return new StyleSelectorList(this, from, length);
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
                if (m_operators[i] != other.m_operators[i])
                    return false;

                if (!m_selectors[i].Equals(other.m_selectors[i], true))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Check two selector lists if one can be applied to another
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool AppliesTo(StyleSelectorList other)
        {
            if (IsSimple)
                return m_selectors[0].Equals(other.m_selectors[other.Count - 1], false);

            // TODO: deep search using operators and everything.
            // right now below part is not working properly
            // it should take
            // "this = ul li b" and successfuly compare to "other = html ul li ul li b#b"

            for (int i = 0; i < other.Count; i++)
            {
                if (m_operators[i] != other.m_operators[i])
                    return false;

                if (!m_selectors[i].Equals(other.m_selectors[i], false))
                    return false;
            }

            return true;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < m_operators.Count - 1; i++)
            {
                builder.Append(m_selectors[i].ToString());

                switch (m_operators[i])
                {
                    case StyleSelectorOperator.None:
                        builder.Append(", ");
                        break;
                    case StyleSelectorOperator.Child:
                        builder.Append(" > ");
                        break;
                    case StyleSelectorOperator.DirectSibling:
                        builder.Append(" + ");
                        break;
                    case StyleSelectorOperator.Sibling:
                        builder.Append(" ~ ");
                        break;
                    default:
                    case StyleSelectorOperator.Inherit:
                        builder.Append(' ');
                        break;
                }
            }
            builder.Append(m_selectors[m_selectors.Count - 1].ToString());

            return builder.ToString();
        }
    }
}
