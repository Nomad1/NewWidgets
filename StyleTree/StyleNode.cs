using System;
using System.Collections.Generic;
using System.Text;

namespace StyleTree
{
    /// <summary>
    /// Simple data storage for properties. Theoretically short-hand properties should be expanded before going there
    /// Also there is no support for expressions ATM
    /// </summary>
    public class StyleData
    {
        private IDictionary<string, string> m_properties;

        public bool IsEmpty
        {
            get { return m_properties.Count == 0; }
        }

        public StyleData()
        {
            m_properties = new Dictionary<string, string>();
        }

        public StyleData(IDictionary<string, string> data)
        {
            m_properties = data;
        }

        public void LoadData(IDictionary<string, string> data)
        {
            foreach (KeyValuePair<string, string> pair in data)
            {
                string oldValue;

                if (m_properties.TryGetValue(pair.Key, out oldValue))
                {
                    Console.WriteLine("Style Warning: property {0}:{1} overrides old property {0}:{2}", pair.Key, pair.Value, oldValue);
                }

                m_properties[pair.Key] = pair.Value;
            }
        }
    }

    /// <summary>
    /// Basic CSS selector description in form E.class#id:hover
    /// </summary>
    public class StyleSelector
    {
        private readonly string m_elementType;
        private readonly string m_class;
        private readonly string m_id;
        private readonly string m_pseudoClass;

        // reference to actual data. I.e. tr, .someclass, #number { } are using the same data object
        private readonly StyleData m_data;

        public StyleData Data
        {
            get { return m_data; }
        }

        public StyleSelector(string type, string @class, string id, string pseudoClass, StyleData data)
        {
            m_elementType = type;
            m_class = @class;
            m_id = id;
            m_pseudoClass = pseudoClass;
            m_data = data;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            if (m_elementType != null)
                builder.Append(m_elementType);

            if (m_class != null)
                builder.Append(m_class);

            if (m_id != null)
                builder.Append(m_id);

            if (m_pseudoClass != null)
                builder.Append(m_pseudoClass);

            return builder.ToString();
        }
    }

    public class SelectorList
    {
    }

    /// <summary>
    /// Tree node in style hierarchy. 
    /// </summary>
    public class StyleNode
    {
        private readonly StyleNode m_parent;

        private readonly ICollection<StyleNode> m_children;

        private readonly StyleSelector m_selector;


        public StyleSelector Selector
        {
            get { return m_selector; }
        }

        public ICollection<StyleNode> Children
        {
            get { return m_children; }
        }

        public StyleNode(StyleNode parent, StyleSelector selector)
        {
            m_parent = parent;
            m_selector = selector;

            m_children = new List<StyleNode>();
        }
    }

    public class StyleTree
    {
        private readonly StyleNode m_root;

        private readonly StyleNode m_any;

        private readonly IDictionary<string, ICollection<StyleNode>> m_elementCollection;
        private readonly IDictionary<string, ICollection<StyleNode>> m_idCollection;
        private readonly IDictionary<string, ICollection<StyleNode>> m_classCollection;

        public StyleTree()
        {
            m_root = new StyleNode(null, new StyleSelector("html", null, null, null, new StyleData()));
            m_any = null;
        }

        public void AddStyle(string selectorsString, IDictionary<string, string> properties)
        {
            // TODO: process properties to remove scripts, shorthand values, unit conversion, tc.
            // properties = ProcessProperties(properties)

            string[] selectors = selectorsString.Split(',');

            StyleData data = null;

            foreach (string selectorString in selectors)
            {
                string trimmedString = selectorString.Trim();

                StyleNode targetNode = FindExactNode(trimmedString);

                if (targetNode != null)
                {
                    targetNode.Selector.Data.LoadData(properties);
                    continue;
                }

                StyleNode parentNode = FindParentNode(trimmedString);

                if (data == null)
                    data = new StyleData(properties);

                AddNode(parentNode, trimmedString, data);
            }
        }

        private void AddNode(StyleNode parent, string selectorString, StyleData data)
        {
            // TODO: split selector string to elements and create new StyleNode
        }

        public StyleNode FindExactNode(string selectorString)
        {
            if (string.IsNullOrEmpty(selectorString))
                return null;

            // html selector is always a root. There is also a special case with :root pseudo-class but it's not supported for now
            if (selectorString == "html")
                return m_root;

            // * is a special symbol for any element type
            if (selectorString == "*")
                return m_any;

            // if there is no inheritance we can try to resolve it immediately
            bool simple = selectorString.IndexOfAny(new char[] { ' ', '+', '>', '~' }) == -1;

            if (simple)
            {
                string[] pseudoClassSeparation = selectorString.Split(new char[] { ':' }, 2);

                string simpleName = pseudoClassSeparation[0];

                if (simpleName.Length == 0) // malformed selector, pseudo-class not allowed without a name
                    return null;

                string pseudoClass = pseudoClassSeparation.Length > 1 ? pseudoClassSeparation[1] : null;

                ICollection<StyleNode> result;

                if (simpleName[0] == '#') // if it starts with # it should be an ID
                    m_idCollection.TryGetValue(simpleName, out result);
                else if (simpleName[0] == '.') // if it startis with . it is a class
                    m_classCollection.TryGetValue(simpleName, out result);
                else if (simpleName == "*")
                    result = m_any == null ? null : new[] { m_any };
                else // it's an element name
                    m_elementCollection.TryGetValue(simpleName, out result);

                if (result != null)
                {
                    foreach (StyleNode node in result)
                        if (IsExactNode(node.Selector, selectorString))
                            return node;
                }
            }

            return RecursiveFindExact(m_root, selectorString);
        }

        private StyleNode RecursiveFindExact(StyleNode node, string selectorString)
        {
            foreach (StyleNode child in node.Children)
            {
                if (IsExactNode(child.Selector, selectorString))
                    return child;

                StyleNode result = RecursiveFindExact(child, selectorString);
                if (result != null)
                    return result;
            }
            return null;
        }

        private bool IsExactNode(StyleSelector selector, string selectorString)
        {
            // TODO: smarter check

            return selector.ToString() == selectorString;
        }
    }
}
