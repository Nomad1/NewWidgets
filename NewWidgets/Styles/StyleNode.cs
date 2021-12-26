using System.Collections.Generic;

namespace NewWidgets.UI.Styles
{
    /// <summary>
    /// Operator that shows how two CSS selectors are combined
    /// </summary>
    public enum StyleSelectorOperator
    {
        None = 1, // comma
        Inherit = 2, // E F an F element descendant of an E element
        Child = 3, // E > F an F element child of an E element
        DirectSibling = 4, // E + F  an F element immediately preceded by an E element
        Sibling = 5, // E ~ F   an F element preceded by an E element
    }

    /// <summary>
    /// Style node container
    /// </summary>
    public class StyleNode
    {
        private readonly StyleSelectorList m_selectorList;

        // reference to actual data. Different nodes (i.e. "tr, .someclass, #number { }") are using the same data object
        private readonly StyleData m_data;

        internal StyleData Data
        {
            get { return m_data; }
        }

        internal StyleSelectorList SelectorList
        {
            get { return m_selectorList; }
        }

        internal StyleSelector MainSelector
        {
            get { return m_selectorList.Selectors[m_selectorList.Count - 1]; }
        }

        public string StyleSelector
        {
            get { return m_selectorList.ToString(); }
        }

        public IDictionary<string, string> Properties
        {
            get { return m_data.Properties; }
        }

        internal StyleNode(StyleSelectorList selector, StyleData data)
        {
            m_selectorList = selector;
            m_data = data;
        }

        public override string ToString()
        {
            return string.Format("{0} {{\n{1}}}\n", m_selectorList, m_data);
        }
    }
}
