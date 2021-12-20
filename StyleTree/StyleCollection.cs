using System;
using System.Collections.Generic;

namespace StyleTree
{
    /// <summary>
    /// Manager for cascading styles
    /// </summary>
    public class StyleCollection
    {
        private readonly IDictionary<string, ICollection<StyleNode>> m_elementCollection = new Dictionary<string, ICollection<StyleNode>>();
        private readonly IDictionary<string, ICollection<StyleNode>> m_idCollection = new Dictionary<string, ICollection<StyleNode>>();
        private readonly IDictionary<string, ICollection<StyleNode>> m_classCollection = new Dictionary<string, ICollection<StyleNode>>();

        public StyleCollection()
        {
            // html selector is always a root. There is also a special case with :root pseudo-class but it's not supported for now
            StyleNode root = new StyleNode(
                new StyleSelectorList(
                    new[] { new StyleSelector("html", null, null, null) },
                    new[] { StyleSelectorOperator.None }),
                new StyleData());

            AddStyle(root);
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
            AddToCollection(m_classCollection, selector.Class, node); // if it startis with . it is a class
            AddToCollection(m_elementCollection, selector.Element, node); // it's an element name
        }

        /// <summary>
        /// Helper method to add to list inside a dictionary
        /// </summary>
        private static void AddToCollection(IDictionary<string, ICollection<StyleNode>> collection, string key, StyleNode node)
        {
            if (string.IsNullOrEmpty(key))
                return;

            ICollection<StyleNode> list = null;

            if (!collection.TryGetValue(key, out list))
                collection[key] = list = new List<StyleNode>();

            list.Add(node);
        }

        /// <summary>
        /// This is a main method for adding new styles to the collection
        /// </summary>
        /// <param name="selectorsString"></param>
        /// <param name="properties"></param>
        public void AddStyle(string selectorsString, IDictionary<string, string> properties)
        {
            // TODO: process properties to remove scripts, shorthand values, unit conversion, tc.
            // properties = ProcessProperties(properties)

            StyleSelectorList selectorList = new StyleSelectorList(selectorsString);

            StyleData data = null;

            foreach (StyleSelectorList selector in selectorList.Split())
            {
                StyleNode node = FindExactStyle(selector);

                if (node != null) // we have a node that is 100% matching the new one
                {
                    node.Data.LoadData(properties);
                    continue;
                }

                if (data == null)
                    data = new StyleData(properties);

                AddStyle(new StyleNode(selector, data));
            }
        }

        private StyleNode FindExactStyle(StyleSelectorList selectorList)
        {
            if (selectorList == null || selectorList.IsEmpty || !selectorList.IsSingleChain)
                throw new ArgumentException("Invalid StyleNode for FindExactStyle call");

            // To use fast search in the collections we have to extract last selector from the list

            StyleSelector selector = selectorList.Selectors[selectorList.Count - 1];

            // then we look in our dictionaries for nodes having
            ICollection<StyleNode> result = null;

            if (!string.IsNullOrEmpty(selector.Id)) // if it starts with # it should be an ID
                m_idCollection.TryGetValue(selector.Id, out result);
            else if (!string.IsNullOrEmpty(selector.Class)) // if it startis with . it is a class
                m_classCollection.TryGetValue(selector.Class, out result);
            else // it's an element name
                if (!string.IsNullOrEmpty(selector.Element))
                m_elementCollection.TryGetValue(selector.Element, out result);

            if (result != null)
            {
                foreach (StyleNode node in result)
                    if (node.SelectorList.Equals(selectorList))
                        return node;
            }

            return null;
        }

        public StyleNode FindStyle(string selectorsString)
        {
            StyleSelectorList selectorList = new StyleSelectorList(selectorsString);

            StyleNode result = FindExactStyle(selectorList);

            if (result != null)
                return result;

            throw new NotImplementedException();
        }

        public void Dump()
        {
            Console.WriteLine("Elements ({0}):", m_elementCollection.Count);
            foreach (var pair in m_elementCollection)
            {
                foreach(StyleNode node in pair.Value)
                    Console.WriteLine("{0}: {1}", pair.Key, node.StyleSelector);
            }

            Console.WriteLine("Classes ({0}):", m_classCollection.Count);
            foreach (var pair in m_classCollection)
            {
                foreach (StyleNode node in pair.Value)
                    Console.WriteLine("{0}: {1}", pair.Key, node.StyleSelector);
            }

            Console.WriteLine("IDs ({0}):", m_idCollection.Count);
            foreach (var pair in m_idCollection)
            {
                foreach (StyleNode node in pair.Value)
                    Console.WriteLine("{0}: {1}", pair.Key, node.StyleSelector);
            }
        }


        //private StyleNode RecursiveFindExact(StyleNode node, string selectorString)
        //{
        //    foreach (StyleNode child in node.Children)
        //    {
        //        if (IsExactNode(child.Selector, selectorString))
        //            return child;

        //        StyleNode result = RecursiveFindExact(child, selectorString);
        //        if (result != null)
        //            return result;
        //    }
        //    return null;
        //}

    }
}
