using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace NewWidgets.UI.Styles
{
    /// <summary>
    /// Basic CSS selector description parsed from form E.class#id:hover
    /// </summary>
    public class StyleSelector
    {
        /// <summary>
        /// Regular expression to parse pseudo classes separated by :
        /// </summary>
        private static readonly Regex s_pseudoClassParser = new Regex(@"(:{1,2}[^:]+)", RegexOptions.Compiled);

        private readonly string m_element;
        private readonly string [] m_classes;
        private readonly string m_id;
        private readonly string [] m_pseudoClasses;
        private readonly IDictionary<string, string> m_attributes;

        public string Element
        {
            get { return m_element; }
        }

        public string [] Classes
        {
            get { return m_classes; }
        }

        public string Id
        {
            get { return m_id; }
        }

        public string [] PseudoClasses
        {
            get { return m_pseudoClasses; }
        }

        /// <summary>
        /// Attribute tests carried by this selector segment, i.e. <c>type=checkbox</c> for
        /// <c>input[type=checkbox]</c>. Null when the selector has none.
        ///
        /// ponytail: only <c>[name=value]</c> equality is supported -- not <c>^=</c>, <c>$=</c>,
        /// <c>*=</c>, <c>~=</c>, and not more than one attribute test chained on one segment.
        /// That is the same limit <c>WidgetManager.RegisterElement</c> already lives with for
        /// the markup table, and nothing the stylesheets in this repository write needs more.
        /// Upgrade path: widen the parsing in the constructor and the comparison in
        /// <see cref="CompareAttributes"/>.
        /// </summary>
        public IDictionary<string, string> Attributes
        {
            get { return m_attributes; }
        }

        /// <summary>
        /// Reads one compound selector -- `input.cls[type="text"]:hover` and every other ordering
        /// of the same parts. CSS puts an optional type FIRST and then allows the id, classes,
        /// attribute tests and pseudo-classes in ANY order, so this scans tokens rather than
        /// matching a fixed sequence.
        ///
        /// It used to be one regex with the groups in a set order, which meant
        /// `input[type="text"].cls` -- the spelling anyone would write -- threw "Invalid selector
        /// string" while `input.cls[type="text"]` parsed. Same selector, and a browser accepts
        /// both.
        /// </summary>
        public StyleSelector(string selectorString)
        {
            string element = string.Empty;
            string id = string.Empty;
            List<string> classes = new List<string>();
            List<string> pseudoClasses = null;
            Dictionary<string, string> attributes = null;

            int at = 0;
            int length = selectorString == null ? 0 : selectorString.Length;

            // the type, if there is one, is whatever precedes the first token marker
            while (at < length && !IsTokenStart(selectorString[at]))
                at++;

            if (at > 0)
                element = selectorString.Substring(0, at);

            while (at < length)
            {
                char marker = selectorString[at];

                if (marker == '[')
                {
                    int close = FindAttributeEnd(selectorString, at);

                    if (close < 0)
                        throw new ArgumentException("Invalid selector string", selectorString);

                    string test = selectorString.Substring(at + 1, close - at - 1);
                    int equals = test.IndexOf('=');

                    if (equals > 0)
                    {
                        if (attributes == null)
                            attributes = new Dictionary<string, string>(1);

                        attributes[test.Substring(0, equals).Trim()] = test.Substring(equals + 1).Trim().Trim('"', '\'');
                    }

                    at = close + 1;
                    continue;
                }

                if (marker == ':')
                {
                    // one pseudo-class, `::` and a `:not(...)` argument included, ending where the
                    // next token starts -- a ':' does not end it, since `:focus:hover` is two
                    int from = at;

                    at++;

                    if (at < length && selectorString[at] == ':')
                        at++;

                    while (at < length && !IsTokenStart(selectorString[at]))
                        at++;

                    if (at < length && selectorString[at] == '(')
                    {
                        int depth = 0;

                        while (at < length)
                        {
                            if (selectorString[at] == '(')
                                depth++;
                            else if (selectorString[at] == ')' && --depth == 0)
                            {
                                at++;
                                break;
                            }

                            at++;
                        }
                    }

                    if (at == from + 1)
                        throw new ArgumentException("Invalid selector string", selectorString);

                    if (pseudoClasses == null)
                        pseudoClasses = new List<string>();

                    pseudoClasses.Add(selectorString.Substring(from, at - from));
                    continue;
                }

                // '#' or '.', both reading a plain name
                int nameStart = ++at;

                while (at < length && !IsTokenStart(selectorString[at]))
                    at++;

                if (at == nameStart)
                    throw new ArgumentException("Invalid selector string", selectorString);

                string name = selectorString.Substring(nameStart, at - nameStart);

                if (marker == '#')
                    id = name;
                else
                    classes.Add(name);
            }

            m_element = element;
            m_id = id;
            m_classes = classes.ToArray();

            if (pseudoClasses != null)
                m_pseudoClasses = pseudoClasses.ToArray();

            m_attributes = attributes;
        }

        /// <summary>
        /// True where a simple selector begins, which is also where the one before it ends.
        /// </summary>
        private static bool IsTokenStart(char c)
        {
            return c == '#' || c == '.' || c == '[' || c == ':';
        }

        /// <summary>
        /// The ']' closing the attribute test that opens at <paramref name="from"/>, skipping any
        /// bracket inside a quoted value. Negative when the test is never closed.
        /// </summary>
        private static int FindAttributeEnd(string selectorString, int from)
        {
            char quote = '\0';

            for (int i = from + 1; i < selectorString.Length; i++)
            {
                char c = selectorString[i];

                if (quote != '\0')
                {
                    if (c == quote)
                        quote = '\0';
                }
                else if (c == '"' || c == '\'')
                    quote = c;
                else if (c == ']')
                    return i;
            }

            return -1;
        }

        public StyleSelector(string element, string classes, string id, string pseudoClasses)
        {
            m_element = string.IsNullOrEmpty(element) ? "" : element; // element type goes as is
            m_classes = string.IsNullOrEmpty(classes) ? null : classes.Split(new[] { ' ', '.' }, StringSplitOptions.RemoveEmptyEntries); // classes should be split. May be we need to use Regex as well, but right now simple split would work
            m_id = string.IsNullOrEmpty(id) ? "" : id.TrimStart('#'); // element ID should not have leading #

            if (!string.IsNullOrEmpty(pseudoClasses)) // Pseudo-classes are tricky and can be in form ::first-child, :disabled or even :not(enabled)
            {
                MatchCollection psMatches = s_pseudoClassParser.Matches(pseudoClasses);
                m_pseudoClasses = new string[psMatches.Count];

                for (int i = 0; i < m_pseudoClasses.Length; i++)
                    m_pseudoClasses[i] = psMatches[i].Groups[0].Value;
            }
        }

        /// <param name="attributes">Attribute tests to carry -- see <see cref="Attributes"/>.
        /// Passed by a widget building its own live selector to match against the cascade, from
        /// whatever <c>Widget.StyleAttributes</c> the document gave it</param>
        public StyleSelector(string element, string [] classes, string id, string [] pseudoClasses = null, IDictionary<string, string> attributes = null)
        {
            m_element = string.IsNullOrEmpty(element) ? "" : element; // element type goes as is
            m_classes = classes;
            m_id = string.IsNullOrEmpty(id) ? "" : id;
            m_pseudoClasses = pseudoClasses;
            m_attributes = attributes;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            if (!string.IsNullOrEmpty(m_element))
                builder.Append(m_element); // element type goes as is

            if (!string.IsNullOrEmpty(m_id))
            {
                builder.Append('#'); // id is to be preceded by #
                builder.Append(m_id);
            }

            if (m_classes != null)
            {
                foreach (string @class in m_classes)
                {
                    builder.Append('.'); // class is to be prepended by .
                    builder.Append(@class);
                }
            }

            if (m_attributes != null)
            {
                foreach (KeyValuePair<string, string> attribute in m_attributes)
                    builder.AppendFormat("[{0}=\"{1}\"]", attribute.Key, attribute.Value);
            }

            if (m_pseudoClasses != null)
            {
                foreach (string pseudoClass in m_pseudoClasses) // pseudo classes have their own separators
                    builder.Append(pseudoClass);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Returns true if styles are equal
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(StyleSelector other)
        {
            if (other == null)
                return false;

            return
                m_element == other.Element &&
                m_id == other.Id &&
                CompareClasses(m_classes, other.Classes, true) &&
                CompareClasses(m_pseudoClasses, other.PseudoClasses, true) &&
                CompareAttributes(m_attributes, other.Attributes, true);

        }

        /// <summary>
        /// Returns true if other is a subset of this selector
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsSubset(StyleSelector other)
        {
            if (other == null)
                return false;

            // returns true if this style can be applied to target string, i.t.
            // this = button and other = button.foo:hover
            // but fails if this class has specifications, i.e.
            // this = button#id and other = button.foo:hover
            // clearly other don't have id = #id so it can't be used there

            return
                (string.IsNullOrEmpty(m_element) || m_element == other.Element) &&
                (string.IsNullOrEmpty(m_id) || m_id == other.Id) &&
                (m_classes == null || m_classes.Length == 0 || CompareClasses(m_classes, other.Classes, false)) &&
                (m_pseudoClasses == null || m_pseudoClasses.Length == 0 || CompareClasses(m_pseudoClasses, other.PseudoClasses, false)) &&
                (m_attributes == null || m_attributes.Count == 0 || CompareAttributes(m_attributes, other.Attributes, false));
        }

        /// <summary>
        /// Returns true if other is mostly equals to this one except for pseudo classes
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsChild(StyleSelector other)
        {
            if (other == null)
                return false;

            return
                (m_element == other.Element) &&
                (m_id == other.Id) &&
                CompareClasses(m_classes, other.Classes, true) &&
                CompareAttributes(m_attributes, other.Attributes, true) &&
                (m_pseudoClasses == null || m_pseudoClasses.Length == 0 || CompareClasses(m_pseudoClasses, other.PseudoClasses, false));
        }

        /// <summary>
        /// This method compares two arrays and returns true if they are equal if exactMatch == true.
        /// If (exactMatch == false) it returns true if at least one of another classes is in one collection
        /// </summary>
        /// <param name="one"></param>
        /// <param name="another"></param>
        /// <param name="exactMatch"></param>
        /// <returns></returns>
        private static bool CompareClasses(string[] one, string[] another, bool exactMatch)
        {
            if (one == null && another == null)
                return true;

            if (one == null || another == null)
                return false;

            if (exactMatch) // both arrays should be equal
            {
                if (one.Length != another.Length)
                    return false;
            }

            // all elements of One array should be in Another

            foreach (string oneClass in one)
                if (Array.IndexOf(another, oneClass) == -1)
                    return false;

            return true;
        }

        /// <summary>
        /// Same contract as <see cref="CompareClasses"/>, for attribute tests: if
        /// <paramref name="exactMatch"/>, both dictionaries must hold exactly the same
        /// name/value pairs; otherwise every pair in <paramref name="one"/> must be present with
        /// an equal value in <paramref name="another"/>, which is what a pattern selector needs
        /// of the target it is tested against
        /// </summary>
        private static bool CompareAttributes(IDictionary<string, string> one, IDictionary<string, string> another, bool exactMatch)
        {
            if (one == null && another == null)
                return true;

            if (one == null || another == null)
                return false;

            if (exactMatch && one.Count != another.Count)
                return false;

            foreach (KeyValuePair<string, string> pair in one)
            {
                string value;

                if (!another.TryGetValue(pair.Key, out value) || value != pair.Value)
                    return false;
            }

            return true;
        }
    }
}
