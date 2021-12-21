using System;
using System.Collections.Generic;
using System.Text;

namespace StyleTree
{
    class MainClass
    {
        private static readonly string s_cssSamples= @"body,html{height:100%}
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


        private static void PrintStyle(StyleCollection collection, string element, string @class, string id, string pseudoClass)
        {
            StyleSelector styleSelector = new StyleSelector(element, @class, id, pseudoClass);

            StyleNode node = collection.FindStyle(styleSelector.ToString());

            if (node == null)
                Console.WriteLine("Style for {0} not found!", styleSelector.ToString());
            else
                Console.WriteLine("Style search result for {0}:\n{1}", styleSelector.ToString(), node.ToString());

        }

        private static void PrintStyle(StyleCollection collection, string value)
        {
            StyleNode node = collection.FindStyle(value);

            if (node == null)
                Console.WriteLine("Style for \"{0}\" not found!", value);
            else
                Console.WriteLine("Style search result for \"{0}\":\n{1}", value, node.ToString());

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

            CSSParser.ParseCSS(s_cssSamples, collection);

            collection.Dump();

            //PrintStyle(collection, "td", null, null, null);
            PrintStyle(collection, ".test td");
        }
    }
}
