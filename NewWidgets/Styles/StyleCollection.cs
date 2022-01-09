﻿using System;
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

            StyleSelector selector = node.SelectorList.Selectors[node.SelectorList.Count - 1]; // last selector in the list

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

            // Styles are now sorted by specificity
            SortedSet<StyleNode> styles = new SortedSet<StyleNode>(StyleNode.Comparer.Instance);

            for (int i = 0; i < selectorList.Count; i++)
            {
                StyleSelectorList selectorPart = new StyleSelectorList(selectorList, 0, i + 1);

                StyleSelector selector = selectorPart.Selectors[selectorPart.Count - 1];

                // we look in our dictionaries for nodes having some of the target parts
                ICollection<StyleNode> collection;

                if (!string.IsNullOrEmpty(selector.Element)) // if it has an element name, check element collection
                    if (m_elementCollection.TryGetValue(selector.Element, out collection))
                        foreach (StyleNode node in collection)
                            if (!styles.Contains(node) && node.SelectorList.AppliesTo(selectorPart))
                            {
                                styles.Add(node);

                                //Console.WriteLine("Found match for element {0} to style {1}", selector.Element, node);
                            }

                if (!string.IsNullOrEmpty(selector.Id)) // if it has an id, check id collection
                    if (m_idCollection.TryGetValue(selector.Id, out collection))
                        foreach (StyleNode node in collection)
                            if (!styles.Contains(node) && node.SelectorList.AppliesTo(selectorPart))
                            {
                                styles.Add(node);

                                //Console.WriteLine("Found match for id #{0} to style {1}", selector.Id, node);
                            }

                if (selector.Classes != null)
                    foreach (string @class in selector.Classes) // if it has a class, check class collection
                        if (m_classCollection.TryGetValue(@class, out collection))
                            foreach (StyleNode node in collection)
                                if (!styles.Contains(node) && node.SelectorList.AppliesTo(selectorPart))
                                {
                                    styles.Add(node);

                                    //Console.WriteLine("Found match for class {0} to style {1}", @class, node);
                                }

            }

            // rule out some very simple cases

            if (styles.Count == 0)
                return null;

            if (styles.Count == 1)
                foreach (StyleNode node in styles)
                    return new[] { node.Data };

            IStyleData[] result = new IStyleData[styles.Count];

            // here we're storing data as an array. The only problem is that we don't store selector hierarchy so we don't know if there is `button:hover` that
            // should inherit everything from `button` however `button label` should not.

            int j = 0;

            foreach (StyleNode node in styles)
                result[j++] = node.Data;

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

        /// <summary>
        /// Saves the collection to output stream
        /// </summary>
        /// <param name="outputStream"></param>
        public void Dump(System.IO.TextWriter outputStream)
        {
            SortedSet<StyleNode> styles = new SortedSet<StyleNode>(StyleNode.Comparer.Instance);

            foreach (var pair in m_elementCollection)
                foreach (var node in pair.Value)
                    if (!styles.Contains(node))
                        styles.Add(node);

            foreach (var pair in m_idCollection)
                foreach (var node in pair.Value)
                    if (!styles.Contains(node))
                        styles.Add(node);

            foreach (var pair in m_classCollection)
                foreach (var node in pair.Value)
                    if (!styles.Contains(node))
                        styles.Add(node);

            foreach (StyleNode node in styles)
                outputStream.WriteLine(node);
        }
    }
}
