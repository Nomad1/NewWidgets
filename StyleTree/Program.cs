using System;
using System.Collections.Generic;
using System.Text;

namespace StyleTree
{
    class MainClass
    {
        private static readonly string s_cssSample2 = @".main {
    color: rebeccapurple;
    border: 2px solid #ccc;
    padding: 1em;
}

.special {
    color: black;
    font-weight: bold;
}
#b { background-color: blue; color: yellow }

li ul li ul li b { font-color: red; }
b { color: green; }

/*html ul.main li ul.special li ul li b#b { color: red; }*/
";

        private static readonly string s_cssSample1= @"body,html{height:100%}
            th, td {
              padding: 6px 15px;
            }
a,b,c,d,e,f {
color: #red;
}
b
{
              background: #42444e
}
            th { /* somthing */
              background: #42444e;
              color: #fff;
              text-align: left;
            }
    #foo > .bar + div.k1.k2 [id='baz']:hello(2):not(:where(#yolo))::before {}
table {
border-collapse: separate;
              border-spacing: 0;
              width: 100%;
            }
    ul.phone_numbers li:last-child > span:after {}
    a[href^=/] {}
    /*some*/ .container:has(nav) {}
    #id {}
    .barstyle {}
    div.kk1.kk2 {
    }
    [id='baz']:hello(2):not(:where(#yolo))::before {}
            tr:first-child th:first-child {
              border-top-left-radius: 6px;
            }
            tr:first-child th:last-child {
              border-top-right-radius: 6px;
            }
            td {
              border-right: 1px solid #c6c9cc;
              border-bottom: 1px solid #c6c9cc;
            }
            td:first-child {
              border-left: 1px solid #c6c9cc;
            }
            tr:nth-child(even) td {
              background: #eaeaed;
            }
            tr:last-child td:first-child {
              border-bottom-left-radius: 6px;
            }
            test.table td {
              border-bottom-right-radius: 6px;
            }


.alpha:first-letter,
.bravo:first-line {
  color: #ff7b29;
  font-size: 18px;
}

body{padding:0;margin:0}body .pull-right{float:right!important}body .pull-left{float:left!important}
/*body d[data-view],body header{display:none;opacity:0;transition:opacity .7s ease-in}body a[data-view].active{display:block;opacity:1}body s[data-nav=playground] header{display:block;opacity:1}[data-view=home]{height:100%}[data-view=home] button{opacity:0;pointer-events:none;transition:opacity 1.5s ease-in-out}[data-view=home] button.live{opacity:1;pointer-events:all}[data-view=home] .mdc-layout-grid__cell--span-4.mdc-elevation--z4{padding:1em;background:#fff}*/
            ";

        private static readonly string s_htmlSample = "<html><ul class=\".main\"><li>Item One</li><li>Item Two<ul><li>2.1</li><li>2.2</li></ul></li><li>Item Three<ul id=\"#sdf\" class=\".special\"><li>3.1<ul><li id=\"#some\"><b id=\"#b\">3.1.1</b></li><li>3.1.2</li></ul></li><li>3.2</li></ul></li></ul></html>";


        private static void PrintStyle(StyleCollection collection, string element, string @class, string id, string pseudoClass)
        {
            StyleSelector styleSelector = new StyleSelector(element, @class, id, pseudoClass);

            StyleData data = collection.GetStyleData(styleSelector.ToString());

            if (data == null)
                Console.WriteLine("Style for {0} not found!", styleSelector);
            else
                Console.WriteLine("Style search result for {0}:\n{1}", styleSelector, data);

        }

        private static void PrintStyle(StyleCollection collection, string value)
        {
            StyleData data = collection.GetStyleData(value);

            if (data == null)
                Console.WriteLine("Style for \"{0}\" not found!", value);
            else
                Console.WriteLine("Style search result for \"{0}\":\n{1}", value, data);

        }

        private static StyleSelector GetHtmlStyle(HtmlNode node)
        {
            return new StyleSelector(node.Element, node.Class, node.Id, "");
        }

        private static void PrintStyle(StyleCollection collection, HtmlNode htmlNode)
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

        }

        public static void Main(string[] args)
        {
            StyleCollection collection = new StyleCollection();

            /*collection.AddStyle("a[href^=/]", new Dictionary<string, string>());
            collection.AddStyle(".container:has(nav)", new Dictionary<string, string>());
            collection.AddStyle("a[href]:lt($var)", new Dictionary<string, string>());
            collection.AddStyle("#id", new Dictionary<string, string>());
            collection.AddStyle(".barstyle", new Dictionary<string, string>());
            collection.AddStyle("div.kk1.kk2", new Dictionary<string, string>());
            collection.AddStyle("[id='baz']:hello(2):not(:where(#yolo))::before", new Dictionary<string, string>());
            collection.AddStyle("#foo > .bar + div.k1.k2 [id='baz']:hello(2):not(:where(#yolo))::before", new Dictionary<string, string>());
            */

            CSSParser.ParseCSS(s_cssSample2, collection);

            collection.Dump();

            /*
            HtmlNode html = new HtmlNode(null, "html");
                // TODO: Parse from file

                HtmlNode ul1 = new HtmlNode(html, "ul", "", "main");
                HtmlNode li1 = new HtmlNode(ul1, "li", "", "", "Item One");
                HtmlNode li2 = new HtmlNode(ul1, "li", "", "", "Item Two");
                HtmlNode ul2 = new HtmlNode(li2, "ul");
                HtmlNode li21 = new HtmlNode(ul2, "li", "", "", "2.1");
                HtmlNode li22 = new HtmlNode(ul2, "li", "", "", "2.2");
                HtmlNode li3 = new HtmlNode(ul1, "li", "", "", "Item Three");
                HtmlNode ul3 = new HtmlNode(li3, "ul", "sdf", "special");
                HtmlNode li31 = new HtmlNode(ul3, "li", "", "", "3.1");
                HtmlNode ul4 = new HtmlNode(li31, "ul");
                HtmlNode li41 = new HtmlNode(ul4, "li", "some");
                HtmlNode b = new HtmlNode(li41, "b", "b", "", "3.1.1");
                HtmlNode li42 = new HtmlNode(ul4, "li", "", "", "3.1.2");
                HtmlNode li32 = new HtmlNode(ul3, "li", "", "", "3.2");
            */

            HtmlNode html = HtmlNode.ParseXHmlt(s_htmlSample);

            HtmlNode b = html.GetElementById("#b");
            //PrintStyle(collection, "td", null, null, null);
            //PrintStyle(collection, ".test td");

            PrintStyle(collection, b);

            Console.WriteLine(html);
        }
    }
}
