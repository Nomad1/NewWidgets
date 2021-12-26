using System;
using System.Collections.Generic;
using System.Text;
using NewWidgets.UI.Styles;

namespace StyleTree
{
    /// <summary>
    /// This class takes .css file text and splits it to styles, parameters and comments
    /// It is definitelly incomplete but still could be of use
    /// </summary>
    public static class CSSParser
    {
        private enum CSSParserState
        {
            None = 0,
            Style = 0x1, // normal state, processing style selector
            Comment = 0x2, // we're inside the comment /* ... */
            ParameterBlock = 0x4, // we're inside the parameters block { ... }
            Parameter = 0x08, // we're inside the parameter definition
        }

        public static void ParseCSS(string cssText, StyleCollection targetCollection)
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
                                Console.WriteLine("WARNING: New style block started while {0} is in process", currentStyle);
                            }

                            if (parameters.Count != 0)
                            {
                                Console.WriteLine("WARNING: New style block started while parameters collection has {0} entries", parameters.Count);
                            }

                            currentStyle = text.ToString();
                            parameters = new Dictionary<string, string>();

                            text.Clear();
                            continue;
                        }
                        else
                            Console.WriteLine("ERROR: Starting parameter block without style name");
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
                                Console.WriteLine("ERROR: Parameter block finished without style name");
                                continue;
                            }

                            //LogTrace("{0}: {1} params", currentStyle.Trim(), parameters.Count);

                            targetCollection.AddStyle(currentStyle.Trim(), parameters);

                            currentStyle = null;
                            parameters = new Dictionary<string, string>();

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
                Console.WriteLine("ERROR: Empty parameter string provided");
                return false;
            }

            string[] split = text.Split(new[] { ':' }, 2);

            if (split.Length != 2)
            {
                Console.WriteLine("ERROR: Invalid parameter string provided {0}", text);
                return false;
            }

            string key = split[0].Trim();

            if (parameters.ContainsKey(key))
            {
                // TODO: it's needed only for debug purposes. CSS standard does not forbid duplicate declarations

                Console.WriteLine("WARNING: Overriding data for parameter {0}", key);
            }

            parameters[key] = split[1].Trim();

            return true;
        }
    }
}
