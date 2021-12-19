using System;
using System.Collections.Generic;
using System.Text;

namespace StyleTree
{
    /// <summary>
    /// Helper class processed from CSS string. Can contain one or main selector chains
    /// </summary>
    internal class StyleSelectorList
    {
        private readonly IList<StyleSelector> m_selectors;
        private readonly IList<StyleSelectorOperand> m_operands;
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
        /// Returns true if there is only one selector chain without extra SelectorOperand.None
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

        public IList<StyleSelectorOperand> Operands
        {
            get { return m_operands; }
        }

        public StyleSelectorList(string selectorsString)
        {
            // parse string to chains
            string[] selectors = selectorsString.Trim().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);


            // if there is no inheritance we can try to resolve it immediately
            //bool simple = selectorString.IndexOfAny(new char[] { ' ', '+', '>', '~' }) == -1;

            // string[] pseudoClassSeparation = selectorString.Split(new char[] { ':' }, 2);

            //string simpleName = pseudoClassSeparation[0];

            //if (simpleName.Length == 0) // malformed selector, pseudo-class not allowed without a name
            //  return null;

            //string pseudoClass = pseudoClassSeparation.Length > 1 ? pseudoClassSeparation[1] : null;

            m_chainCount = selectors.Length;
        }

        internal StyleSelectorList(IList<StyleSelector> selectors, IList<StyleSelectorOperand> operands)
        {
            m_selectors = selectors;
            m_operands = operands;
            m_chainCount = CountChains();
        }

        private StyleSelectorList(StyleSelectorList other, int start, int count)
            : this(new ListRange<StyleSelector>(other.m_selectors, start, count),
                  new ListRange<StyleSelectorOperand>(other.m_operands, start, count))
        {

        }


        /// <summary>
        /// Returns amount of separate chains
        /// </summary>
        private int CountChains()
        {
            int count = 0;

            // operands array is always the length of selector array having None as a last member

            for (int i = 0; i < m_operands.Count; i++)
                if (m_operands[i] == StyleSelectorOperand.None)
                    count++;

            return count;
        }

        public IList<StyleSelectorList> Split()
        {
            if (m_chainCount == 1)
                return new[] { this };

            List<StyleSelectorList> result = new List<StyleSelectorList>();

            int chainStart = 0;
            int chainLength = 1;

            for (int i = 0; i < m_operands.Count; i++)
            {
                if (m_operands[i] == StyleSelectorOperand.None)
                {
                    result.Add(new StyleSelectorList(this, chainStart, chainLength));
                    chainStart = i;
                    chainLength = 1;
                }
            }

            return result.ToArray();
        }

        public bool Equals(StyleSelectorList other)
        {
            if (Count != other.Count)
                return false;

            for (int i = 0; i < other.Count; i++)
            {
                if (m_operands[i] != other.m_operands[i])
                    return false;

                if (!m_selectors[i].Equals(other.m_selectors[i]))
                    return false;
            }

            return true;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < m_operands.Count; i++)
            {
                builder.Append(m_selectors[i].ToString());

                switch (m_operands[i])
                {
                    case StyleSelectorOperand.None:
                        builder.Append(", ");
                        break;
                    case StyleSelectorOperand.Child:
                        builder.Append(" > ");
                        break;
                    case StyleSelectorOperand.DirectSibling:
                        builder.Append(" + ");
                        break;
                    case StyleSelectorOperand.Sibling:
                        builder.Append(" ~ ");
                        break;
                    default:
                    case StyleSelectorOperand.Inherit:
                        builder.Append(' ');
                        break;
                }
            }
            builder.Append(m_selectors[m_selectors.Count - 1].ToString());

            return builder.ToString();
        }
    }
}
