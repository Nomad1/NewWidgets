using System;

namespace NewWidgets.UI.Styles
{
    /// <summary>
    /// Operator that shows how two CSS selectors are combined
    /// </summary>
    public enum StyleSelectorCombinator
    {
        None = 1, // comma
        Descendant = 2, // E F an F element descendant of an E element
        Child = 3, // E > F an F element child of an E element
        AdjacentSibling = 4, // E + F  an F element immediately preceded by an E element
        Sibling = 5, // E ~ F   an F element preceded by an E element
    }

    public enum StyleNodeType
    {
        None = 0, // invalid
        OwnStyle = 1, // local style set in element properties, i.e. <a style="color:red">
        Id = 2, // style found by precise element id, i.e. <a id="id">
        Class = 3, // style set in class attribute, i.e. <a class="class">
        Element = 4, // style by element type, i.e. a { color: red; }
        PseudoClass = 5, // style by matching element pseudo-class, i.e. a:hover { color: red}
        Parent = 6, // direct parent of current element:  <div class="s"><a>tmp</a></div>
        GrandParent = 7, // indirect parent of current element: <div class="s"><p><a>tmp</a></p></div>

        // TODO: Sibling?
    }

    /// <summary>
    /// This is an interface for property storage container
    /// Simplest version of this container is just a Dictionary with all the properties
    /// </summary>
    public interface IStyleData
    {
        /// <summary>
        /// This method is used when we need to append new data to existing data collection
        /// </summary>
        /// <param name="data"></param>
        void LoadData(IStyleData data);
    }

    /// <summary>
    /// Style node container
    /// </summary>
    public class StyleNode : IComparable<StyleNode>
    {
        private readonly StyleSelectorList m_selectorList;

        // reference to actual data. Different nodes (i.e. "tr, .someclass, #number { }") are using the same data object
        private readonly IStyleData m_data;

        public IStyleData Data
        {
            get { return m_data; }
        }

        internal StyleSelectorList SelectorList
        {
            get { return m_selectorList; }
        }

        public string StyleSelector
        {
            get { return m_selectorList.ToString(); }
        }

        public int Specificity
        {
            get { return m_selectorList.Specificity; }
        }

        internal StyleNode(StyleSelectorList selector, IStyleData data)
        {
            m_selectorList = selector;
            m_data = data;
        }

        public override string ToString()
        {
            return string.Format("{0} {{\n{1}}}\n", m_selectorList, m_data);
        }

        public int CompareTo(StyleNode other)
        {
            int result = Specificity.CompareTo(other.Specificity);
            if (result <= 0) // we need to avoid result of 0
                return -1;
            return 1;
        }

        public bool IsPseudoClassParent(StyleNode child)
        {
            if (child == null)
                return false;

            if (child.SelectorList.Selectors[child.SelectorList.Selectors.Count - 1].PseudoClasses != null)
                return true;

            return false;
        }

        /// <summary>
        /// Checks if the current selector is parent of the element group (button, etc.)
        /// </summary>
        /// <returns></returns>
        public bool IsElementParent(string elementType)
        {
            return SelectorList.IsSimple &&
                (elementType == SelectorList.Selectors[0].Element || SelectorList.Selectors[0].Element == "*")
                &&
                (SelectorList.Selectors[0].Classes == null || SelectorList.Selectors[0].Classes.Length == 0);
        }
    }
}
