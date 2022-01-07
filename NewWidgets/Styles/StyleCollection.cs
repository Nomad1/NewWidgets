using System;
using System.Collections.Generic;

namespace NewWidgets.UI.Styles
{
    /// <summary>
    /// Manager for cascading styles
    /// </summary>
    public class StyleCollection
    {
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

            StyleSelector selector = node.MainSelector;

            AddToCollection(m_idCollection, selector.Id, node); // if it starts with # it should be an ID

            if (selector.Classes != null && selector.Classes.Length > 0)
                AddToCollection(m_classCollection, selector.Classes[selector.Classes.Length -1], node); // last class should be stored

            AddToCollection(m_elementCollection, selector.Element, node); // it's an element name
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
            // TODO: process properties to remove scripts, shorthand values, unit conversion, tc.
            // properties = ProcessProperties(properties)

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
                    continue;
                }

                AddStyle(new StyleNode(selector, data));
            }
        }

        private StyleNode FindExactStyle(StyleSelectorList selectorList)
        {
            if (selectorList == null || selectorList.IsEmpty || !selectorList.IsSingleChain)
                throw new ArgumentException("Invalid StyleNode for FindExactStyle call");

            // To use fast search in the collections we have to extract last selector from the list

            StyleSelector selector = selectorList.Selectors[selectorList.Count - 1];

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

            return null;
        }

        /// <summary>
        /// This method retrieves an ordered list (cascade) of style properties
        /// for given selector list
        /// </summary>
        /// <param name="selectorList"></param>
        /// <returns></returns>
        public ICollection<IStyleData> GetStyleData(StyleSelectorList selectorList)
        {
            if (selectorList == null || selectorList.IsEmpty)
                throw new ArgumentException("Invalid StyleNode for FindStyle call");

            List<StyleNode> styles = new List<StyleNode>();
            for (int i = 0; i < selectorList.Count; i++)
            {
                StyleSelectorList selectorPart = new StyleSelectorList(selectorList, 0, i + 1);

                StyleSelector selector = selectorPart.Selectors[selectorPart.Count - 1];

                // we look in our dictionaries for nodes having some of the target parts
                ICollection<StyleNode> collection;

                if (!string.IsNullOrEmpty(selector.Element)) // if it has an element name, check element collection
                    if (m_elementCollection.TryGetValue(selector.Element, out collection))
                        foreach (StyleNode node in collection)
                            if (node.SelectorList.AppliesTo(selectorPart))
                            {
                                styles.Add(node);

                                //Console.WriteLine("Found match for element {0} to style {1}", selector.Element, node);
                            }

                if (!string.IsNullOrEmpty(selector.Id)) // if it has an id, check id collection
                    if (m_idCollection.TryGetValue(selector.Id, out collection))
                        foreach (StyleNode node in collection)
                            if (node.SelectorList.AppliesTo(selectorPart))
                            {
                                styles.Add(node);

                                //Console.WriteLine("Found match for id #{0} to style {1}", selector.Id, node);
                            }

                if (selector.Classes != null)
                    foreach (string @class in selector.Classes) // if it has a class, check class collection
                        if (m_classCollection.TryGetValue(@class, out collection))
                            foreach (StyleNode node in collection)
                                if (node.SelectorList.AppliesTo(selectorPart))
                                {
                                    styles.Add(node);

                                    //Console.WriteLine("Found match for class {0} to style {1}", @class, node);
                                }

            }

            // TODO: sort styles by specificity. Right now order element-class-id gives us a little bit of similarity to specificity system

            if (styles.Count == 0)
                return null;

            if (styles.Count == 1)
                return new[] { styles[0].Data };

            styles.Sort((one, another) => one.SelectorList.Specificity.CompareTo(another.SelectorList.Specificity));

            IStyleData[] result = new IStyleData[styles.Count];

            for (int i = 0; i < styles.Count; i++)
                result[i] = styles[i].Data;

            // 1. find hierarchy match (considering operators) for each list entry. It's different from exact match because it should consider inheritance and incapsulation
            // 2. compose a specificity list and sort the results
            // 3. mix all the properties to one list and return new style. Note: we'll need to backtrack parent stylesheet to make sure new style is invalidated if parents are modified

            return result;
        }

        /// <summary>
        /// This method retrieves an ordered list (cascade) of style properties
        /// for given selector string
        /// </summary>
        /// <param name="selectorsString"></param>
        /// <returns></returns>
        public ICollection<IStyleData> GetStyleData(string selectorsString)
        {
            return GetStyleData(new StyleSelectorList(selectorsString));
        }

        public void Dump()
        {
            Console.WriteLine("Elements ({0}):", m_elementCollection.Count);
            foreach (var pair in m_elementCollection)
            {
                foreach(StyleNode node in pair.Value)
                    Console.WriteLine(node);
            }

            Console.WriteLine("Classes ({0}):", m_classCollection.Count);
            foreach (var pair in m_classCollection)
            {
                foreach (StyleNode node in pair.Value)
                    Console.WriteLine(node);
            }

            Console.WriteLine("IDs ({0}):", m_idCollection.Count);
            foreach (var pair in m_idCollection)
            {
                foreach (StyleNode node in pair.Value)
                    Console.WriteLine(node);
            }
        }
    }
}
