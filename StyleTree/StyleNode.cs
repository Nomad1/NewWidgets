using System.Collections.Generic;

namespace StyleTree
{
    /// <summary>
    /// Style node container
    /// </summary>
    public class StyleNode
    {
        private readonly StyleSelectorList m_selectorList;

        //// reference to actual data. Different nodes (i.e. "tr, .someclass, #number { }") are using the same data object
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
    }
}
