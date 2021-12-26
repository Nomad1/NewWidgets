using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NewWidgets.UI.Styles;
using NUnit.Framework;

namespace StyleTree
{
    [TestFixture]
    public static class HtmlTest
    {
        private static StyleSelector GetHtmlStyle(HtmlNode node)
        {
            return new StyleSelector(node.Element, node.Class, node.Id, "");
        }

        private static IDictionary<string, string> PrintStyle(StyleCollection collection, HtmlNode htmlNode)
        {
            List<StyleSelector> styleList = new List<StyleSelector>();

            HtmlNode current = htmlNode;
            while (current != null)
            {
                styleList.Add(GetHtmlStyle(current));
                current = current.Parent;
            }

            StyleSelector[] styles = new StyleSelector[styleList.Count];
            StyleSelectorOperator[] operators = new StyleSelectorOperator[styleList.Count];

            for (int i = 0; i < styleList.Count; i++)
            {
                styles[styleList.Count - i - 1] = styleList[i];
                operators[i] = StyleSelectorOperator.Inherit;
            }

            operators[operators.Length - 1] = StyleSelectorOperator.None; // trailing None operator

            StyleSelectorList list = new StyleSelectorList(styles, operators);
            StyleData data = collection.GetStyleData(list);

            if (data == null)
                Console.WriteLine("Style for \"{0}\" not found!", list);
            else
                Console.WriteLine("Style search result for \"{0}\":\n{1}", list, data);

            return data.Properties;

        }

        private static IDictionary<string, string> PerformTest(string htmlName, string cssName, string elementId)
        {
            if (!File.Exists(htmlName))
                throw new ArgumentException("File " + htmlName + " not found for CSS test");

            if (!File.Exists(cssName))
                throw new ArgumentException("File " + cssName + " not found for CSS test");

            StyleCollection collection = new StyleCollection();

            HtmlNode html = HtmlNode.ParseXHmlt(File.ReadAllText(htmlName));
            CSSParser.ParseCSS(File.ReadAllText(cssName), collection);

            HtmlNode element = html.GetElementById(elementId);

            if (element == null)
                throw new ArgumentException("Element " + elementId + " not found for CSS test");

            return PrintStyle(collection, element);

            //Console.WriteLine(html);

        }

        [Test]
        public static void Test1()
        {
            Dictionary<string, string> compare = new Dictionary<string, string>()
            {
                { "border" , "2px solid #ccc"},
                { "color" , "green"},
                { "padding" , "1em"},
                { "font-weight" , "bold"},
                { "background-color" , "green"}
            };

            IDictionary<string, string> result = PerformTest("Test1/index.html", "Test1/style.css", "b");

            Assert.IsTrue(result.OrderBy(kvp => kvp.Key).SequenceEqual(compare.OrderBy(kvp => kvp.Key)));
        }
    }
}
