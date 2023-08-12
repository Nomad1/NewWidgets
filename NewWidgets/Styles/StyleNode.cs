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

    [Flags]
    public enum StyleNodeMatch
    {
        None = 0, // invalid
        OwnStyle = 0x01, // local style set in element properties, i.e. <a style="color:red">
        Id = 0x02, // style found by precise element id, i.e. <a id="id">
        Class = 0x04, // style set in class attribute, i.e. <a class="class">
        Element = 0x08, // style by element type, i.e. a { color: red; }
        PseudoClass = 0x10, // style by matching element pseudo-class, i.e. a:hover { color: red}
        Parent = 0x20, // direct parent of current element:  <div class="s"><a>tmp</a></div>
        GrandParent = 0x40, // indirect parent of current element: <div class="s"><p><a>tmp</a></p></div>

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

        public StyleSelector Last
        {
            get { return m_selectorList.Selectors[m_selectorList.Count - 1]; }
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

            if (child.Last.PseudoClasses != null)
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

        ///// <summary>
        ///// Tries to guess node type relative to this element type. It could not guess Parent/GrandParent relations
        ///// </summary>
        ///// <param name="elementType"></param>
        ///// <returns></returns>
        //public StyleNodeType GetNodeType(string elementType, string id, string [] classes, string [] pseudoClasses)
        //{
        //    if (!string.IsNullOrEmpty(id) && this.Last.Id == id)
        //        return StyleNodeType.Id; // this style matches our personal Id


        //    return SelectorList.IsSimple &&
        //        (elementType == SelectorList.Selectors[0].Element || SelectorList.Selectors[0].Element == "*")
        //        &&
        //        (SelectorList.Selectors[0].Classes == null || SelectorList.Selectors[0].Classes.Length == 0);
        //}
    }
}
