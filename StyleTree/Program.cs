using System;
using System.Collections.Generic;

namespace StyleTree
{
    class MainClass
    {
        private static readonly string s_cssSamples= @"
            ul.phone_numbers li:last-child > span:after {}
            a[href^=/] {}
            .container:has(nav) {}
            #id {}
            .barstyle {}
            div.kk1.kk2 {
            }
            [id='baz']:hello(2):not(:where(#yolo))::before {}
            #foo > .bar + div.k1.k2 [id='baz']:hello(2):not(:where(#yolo))::before {}
            table {
              border-collapse: separate;
              border-spacing: 0;
              width: 100%;
            }
            th, td {
              padding: 6px 15px;
            }
            th {
              background: #42444e;
              color: #fff;
              text-align: left;
            }
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
            tr:last-child td:last-child {
              border-bottom-right-radius: 6px;
            }
            ";

        public static void Main(string[] args)
        {
            StyleCollection collection = new StyleCollection();

            collection.AddStyle("a[href^=/]", new Dictionary<string, string>());
            collection.AddStyle(".container:has(nav)", new Dictionary<string, string>());
            collection.AddStyle("a[href]:lt($var)", new Dictionary<string, string>());
            collection.AddStyle("#id", new Dictionary<string, string>());
            collection.AddStyle(".barstyle", new Dictionary<string, string>());
            collection.AddStyle("div.kk1.kk2", new Dictionary<string, string>());
            collection.AddStyle("[id='baz']:hello(2):not(:where(#yolo))::before", new Dictionary<string, string>());
            collection.AddStyle("#foo > .bar + div.k1.k2 [id='baz']:hello(2):not(:where(#yolo))::before", new Dictionary<string, string>());

            collection.Dump();
        }
    }
}
