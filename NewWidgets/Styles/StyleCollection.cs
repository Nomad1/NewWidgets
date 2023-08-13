using System;
using System.Collections.Generic;

namespace NewWidgets.UI.Styles
{
    /// <summary>
    /// Manager for cascading styles
    /// </summary>
    public class StyleCollection
    {
        /// All possible nodes
        private readonly IDictionary<string, StyleNode> m_allNodes = new Dictionary<string, StyleNode>();

        private readonly IDictionary<string, ICollection<StyleNode>> m_elementCollection = new Dictionary<string, ICollection<StyleNode>>();
        private readonly IDictionary<string, ICollection<StyleNode>> m_idCollection = new Dictionary<string, ICollection<StyleNode>>();
        private readonly IDictionary<string, ICollection<StyleNode>> m_classCollection = new Dictionary<string, ICollection<StyleNode>>();

        /// <summary>
        /// Creates an empty style collection 
        /// </summary>
        public StyleCollection()
        {
        }

        /// <summary>
        /// This method tries to add the style to each of three dictionaries
        /// </summary>
        /// <param name="node"></param>
        private void AddStyle(StyleNode node)
        {
            if (node == null || !node.SelectorList.IsSingleChain)
                throw new ArgumentException("Invalid StyleNode for AddStyle call");

            StyleSelector selector = node.SelectorList.Selectors[node.SelectorList.Count - 1]; // last selector in the list

            // All nodes
            m_allNodes[selector.ToString()] = node;

            if (!string.IsNullOrEmpty(selector.Id)) // we have an explicit id, i.e. tr#myid
                AddToCollection(m_idCollection, selector.Id, node);

            if (selector.Classes != null && selector.Classes.Length > 0) // we have one or more classes: .mytable.something, only .something will be used
                AddToCollection(m_classCollection, selector.Classes[selector.Classes.Length -1], node); // last class should be stored

            if (!string.IsNullOrEmpty(selector.Element))
                AddToCollection(m_elementCollection, selector.Element, node); // there is an element name, i.e. button
        }

        /// <summary>
        /// Helper method to add to list inside a dictionary
        /// </summary>
        private static void AddToCollection(IDictionary<string, ICollection<StyleNode>> collection, string key, StyleNode node)
        {
            if (string.IsNullOrEmpty(key))
                return;

            ICollection<StyleNode> list;

            if (!collection.TryGetValue(key, out list))
                collection[key] = list = new List<StyleNode>();

            list.Add(node);
        }

        /// <summary>
        /// This is a main method for adding new styles to the collection
        /// </summary>
        /// <param name="selectorsString"></param>
        /// <param name="properties"></param>
        public void AddStyle(string selectorsString, IStyleData data)
        {
            AddStyle(new StyleSelectorList(selectorsString), data);
        }

        /// <summary>
        /// This is a main method for adding new styles to the collection
        /// </summary>
        /// <param name="selectorList"></param>
        /// <param name="properties"></param>
        public void AddStyle(StyleSelectorList selectorList, IStyleData data)
        {
            foreach (StyleSelectorList selector in selectorList.Split())
            {
                StyleNode node = FindExactStyle(selector);

                if (node != null) // we have a node that is 100% matching the new one, so we need to append new data to it
                {
                    node.Data.LoadData(data);
                }
                else
                {
                    node = new StyleNode(selector, data);
                    AddStyle(node);
                }
            }
        }

        private StyleNode FindExactStyle(StyleSelectorList selectorList)
        {
            if (selectorList == null || selectorList.IsEmpty || !selectorList.IsSingleChain)
                throw new ArgumentException("Invalid StyleNode for FindExactStyle call");

            // Nomad: fast search was a mistake, excat search requires exact results

            StyleNode result;

            if (m_allNodes.TryGetValue(selectorList.ToString(), out result))
                return result;

#if OLD
            // then we look in our dictionaries for nodes having some of the target parts
            ICollection<StyleNode> collection = null;

            if (!string.IsNullOrEmpty(selector.Id)) // if it has an id, check id collection
                m_idCollection.TryGetValue(selector.Id, out collection);
            else if (selector.Classes.Length > 0) // if it has a class, check class collection
                m_classCollection.TryGetValue(selector.Classes[selector.Classes.Length - 1], out collection);
            else
                if (!string.IsNullOrEmpty(selector.Element)) // otherwise check element collection
                m_elementCollection.TryGetValue(selector.Element, out collection);

            if (collection != null)
            {
                foreach (StyleNode node in collection)
                    if (node.SelectorList.Equals(selectorList)) // here we check only for exact 100% match
                        return node;
            }
#endif

            return null;
        }

        /// <summary>
        /// Returns all styles for selected element type
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public ICollection<StyleNode> GetElementNodes(StyleSelector selector)
        {
            ICollection<StyleNode> collection;

            if (!string.IsNullOrEmpty(selector.Element)) // if it has an element name, check element collection
                if (m_elementCollection.TryGetValue(selector.Element, out collection))
                    return collection;

            return null;
        }

        /// <summary>
        /// This method retrieves an ordered list (cascade) of style properties
        /// for given selector list
        /// </summary>
        /// <param name="selectorList"></param>
        /// <returns></returns>
        internal ICollection<StyleNodeMatchPair> GetStyleData(StyleSelectorList selectorList)
        {
            if (selectorList == null || selectorList.IsEmpty)
                throw new ArgumentException("Invalid StyleNode for FindStyle call");

            List<StyleNodeMatchPair> styles = new List<StyleNodeMatchPair>();

            for (int i = 0; i < selectorList.Count; i++)
            {
                // Styles are sorted by specificity
                SortedDictionary<StyleNode, StyleNodeMatch> partStyles = new SortedDictionary<StyleNode, StyleNodeMatch>();

                StyleSelectorList selectorPart = new StyleSelectorList(selectorList, 0, i + 1);

                StyleSelector selector = selectorPart.Selectors[selectorPart.Count - 1];
                StyleNodeMatch nodeType = selectorPart.Types[selectorPart.Count - 1];

                // we look in our dictionaries for nodes having some of the target parts
                ICollection<StyleNode> collection;

                if (!string.IsNullOrEmpty(selector.Element)) // if it has an element name, check element collection
                    if (m_elementCollection.TryGetValue(selector.Element, out collection))
                        foreach (StyleNode node in collection)
                            if (!partStyles.ContainsKey(node) && node.SelectorList.AppliesTo(selectorPart))
                            {
                                partStyles[node] = nodeType | StyleNodeMatch.Element;

                                //Console.WriteLine("Found match for element {0} to style {1}", selector.Element, node);
                            }

                if (!string.IsNullOrEmpty(selector.Id)) // if it has an id, check id collection
                    if (m_idCollection.TryGetValue(selector.Id, out collection))
                        foreach (StyleNode node in collection)
                            if (!partStyles.ContainsKey(node) && node.SelectorList.AppliesTo(selectorPart))
                            {
                                partStyles[node] = nodeType | StyleNodeMatch.Id;

                                //Console.WriteLine("Found match for id #{0} to style {1}", selector.Id, node);
                            }

                if (selector.Classes != null)
                {
                    // Classes should be processed in reverse order meaning that the last class has more priority
                    for (int c = selector.Classes.Length - 1; c >= 0; c--) // if it has a class, check class collection
                    {
                        string @class = selector.Classes[c];
                        if (m_classCollection.TryGetValue(@class, out collection))
                            foreach (StyleNode node in collection)
                                if (!partStyles.ContainsKey(node) && node.SelectorList.AppliesTo(selectorPart))
                                {
                                    partStyles[node] = nodeType | StyleNodeMatch.Class;

                                    //Console.WriteLine("Found match for class {0} to style {1}", @class, node);
                                }
                    }
                }

                foreach(var pair in partStyles)
                    styles.Add(new StyleNodeMatchPair(pair.Key, pair.Value));
            }

            // rule out some very simple cases

            if (styles.Count == 0)
                return null;

#if !DEBUG
            if (styles.Count == 1)
                foreach (var pair in styles)
                    return new[] { pair };
#endif

            // here we're storing data as an array. The only problem is that we don't store selector hierarchy so we don't know if there is `button:hover` that
            // should inherit everything from `button` however `button label` should not.

            int j = 0;

#if DEBUG
            string resultString = "";
            foreach (var pair in styles)
            {
                if (j != 0)
                    resultString += " ";
                resultString += string.Format("[{0} = {1}]", pair.Node.StyleSelector, pair.Node.Specificity);
            }

            Console.WriteLine("Resolved {0} for {1}", resultString, selectorList);
#endif
            // 1. find hierarchy match (considering operators) for each list entry. It's different from exact match because it should consider inheritance and incapsulation
            // 2. compose a specificity list and sort the results
            // 3. mix all the properties to one list and return new style. Note: we'll need to backtrack parent stylesheet to make sure new style is invalidated if parents are modified

            return styles;
        }

        /*
        /// <summary>
        /// This method retrieves an ordered list (cascade) of style properties
        /// for given selector string
        /// </summary>
        /// <param name="selectorsString"></param>
        /// <returns></returns>
        public ICollection<StyleNode> GetStyleData(string selectorsString)
        {
            return GetStyleData(new StyleSelectorList(selectorsString));
        }*/

        /// <summary>
        /// Saves the collection to output stream
        /// </summary>
        /// <param name="outputStream"></param>
        public void Dump(System.IO.TextWriter outputStream)
        {
            foreach (StyleNode node in m_allNodes.Values)
                outputStream.WriteLine(node);
        }
    }
}
