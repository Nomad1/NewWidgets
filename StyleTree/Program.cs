using System;
using System.Collections.Generic;
using System.Text;

namespace StyleTree
{
    class MainClass
    {
        private static readonly string s_cssSamples= @"
            th, td {
              padding: 6px 15px;
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
            tr:last-child td:last-child {
              border-bottom-right-radius: 6px;
            }


.alpha:first-letter,
.bravo:first-line {
  color: #ff7b29;
  font-size: 18px;
}

body,html{height:100%}body{padding:0;margin:0}body .pull-right{float:right!important}body .pull-left{float:left!important}
/*body d[data-view],body header{display:none;opacity:0;transition:opacity .7s ease-in}body a[data-view].active{display:block;opacity:1}body s[data-nav=playground] header{display:block;opacity:1}[data-view=home]{height:100%}[data-view=home] button{opacity:0;pointer-events:none;transition:opacity 1.5s ease-in-out}[data-view=home] button.live{opacity:1;pointer-events:all}[data-view=home] .mdc-layout-grid__cell--span-4.mdc-elevation--z4{padding:1em;background:#fff}*/
            ";


        private enum CSSParserState
        {
            None = 0,
            Style = 0x1, // normal state, processing style selector
            Comment = 0x2, // we're inside the comment /* ... */
            ParameterBlock = 0x4, // we're inside the parameters block { ... }
            Parameter = 0x08, // we're inside the parameter definition
        }

        private static void LogError(string text, params object[] parameters)
        {
            Console.WriteLine("ERROR: {0}", string.Format(text, parameters));
        }

        private static void LogWarning(string text, params object[] parameters)
        {
            Console.WriteLine("WARNING: {0}", string.Format(text, parameters));
        }

        private static void LogTrace(string text, params object[] parameters)
        {
            Console.WriteLine(string.Format(text, parameters));
        }

        private static void ParseCSS(string cssText, StyleCollection targetCollection)
        {
            CSSParserState state = CSSParserState.None;
            StringBuilder text = new StringBuilder();

            string currentStyle = null;
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            for (int i = 0; i < cssText.Length; i++)
            {
                if ((state & CSSParserState.Comment) != 0) // if we're inside of the comment, ignore everything except */
                {
                    if (i != cssText.Length - 1 && cssText[i] == '*' && cssText[i + 1] == '/') // that's an end of the comment
                    {
                        state &= ~CSSParserState.Comment;
                        i++;
                    }

                    continue;
                }

                switch (cssText[i])
                {
                    case '{':
                        if ((state & CSSParserState.Style) != 0) // we're entering new param block, ending the style
                        {
                            state &= ~CSSParserState.Style;
                            state |= CSSParserState.ParameterBlock;

                            if (!string.IsNullOrEmpty(currentStyle))
                            {
                                LogWarning("New style block started while {0} is in process", currentStyle);
                            }

                            if (parameters.Count != 0)
                            {
                                LogWarning("New style block started while parameters collection has {0} entries", parameters.Count);
                            }

                            currentStyle = text.ToString();
                            parameters.Clear();

                            text.Clear();
                            continue;
                        }
                        else
                            LogError("Starting parameter block without style name");
                        break;
                    case '}': // TODO: ignore inside of the parameter text string
                        if ((state & CSSParserState.Parameter) != 0) // parameter is ending without trailing ;. Not an issue
                        {
                            state &= ~CSSParserState.Parameter;
                            ParseParameter(text.ToString(), parameters);

                            text.Clear();
                        }

                        if ((state & CSSParserState.ParameterBlock) != 0) // we're ending the param block
                        {
                            state &= ~CSSParserState.ParameterBlock;

                            if (string.IsNullOrEmpty(currentStyle))
                            {
                                LogError("Parameter block finished without style name");
                                continue;
                            }

                            //LogTrace("{0}: {1} params", currentStyle.Trim(), parameters.Count);

                            targetCollection.AddStyle(currentStyle.Trim(), parameters);

                            currentStyle = null;
                            parameters.Clear();

                            continue;
                        }
                        break;
                    case '/': // possible start of the comment
                        if ((state & CSSParserState.Comment) != 0)
                            break;

                        if (i != cssText.Length - 1)
                        {
                            if (cssText[i + 1] == '*')
                            {
                                state |= CSSParserState.Comment;
                                i++;
                                continue;
                            }
                        }
                        break;
                    case ';': // end of parameter
                        if ((state & CSSParserState.Parameter) != 0) // TODO: ignore inside of the parameter text string
                        {
                            state &= ~CSSParserState.Parameter;
                            ParseParameter(text.ToString(), parameters);

                            text.Clear();
                            continue;
                        }
                        break;
                    case ' ':
                    case '\t':
                        // whitespace
                        if ((state & (CSSParserState.Style | CSSParserState.Parameter)) == 0)
                            continue; // ignore it if we're not inside the meaningful block
                        // otherwise do nothing
                        break;
                    case '\n':
                    case '\r':
                        // line feed, ignoring it
                        continue;
                }

                if ((state & (CSSParserState.ParameterBlock | CSSParserState.Parameter)) == CSSParserState.ParameterBlock) // we're inside parameter block but parameter hasn't started yet
                {
                    state |= CSSParserState.Parameter;
                }
                else if ((state & (CSSParserState.Style | CSSParserState.ParameterBlock)) == 0) // we're outside of parameter block but style name hasn't started yet
                {
                    state |= CSSParserState.Style;
                }

                text.Append(cssText[i]);
            }
        }

        private static bool ParseParameter(string text, Dictionary<string, string> parameters)
        {
            if (string.IsNullOrEmpty(text))
            {
                LogError("Empty parameter string provided");
                return false;
            }

            string[] split = text.Split(new[] { ':' }, 2);

            if (split.Length != 2)
            {
                LogError("Invalid parameter string provided {0}", text);
                return false;
            }

            string key = split[0].Trim();

            if (parameters.ContainsKey(key))
            {
                LogWarning("Overriding data for parameter {0}", key);
            }

            parameters[key] = split[1].Trim();

            return true;
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

            ParseCSS(s_cssSamples, collection);

            collection.Dump();
        }
    }
}
