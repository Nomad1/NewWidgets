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
        /// Regular expression to parse selectors to basic parts
        /// </summary>
        private static readonly Regex s_selectorParser = new Regex(@"^(?<element>@{0,1}[\*|\w|\-_]+)?(?<id>#[\w|\-_]+)?(?<class>\.[\w|\-|\._]+)?(?<attributes>\[.+\])?(?<pseudostyle>:.+)?$", RegexOptions.Compiled);

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

        public StyleSelector(string selectorString)
        {
            MatchCollection matches = s_selectorParser.Matches(selectorString);

            if (matches.Count != 1)
                throw new ArgumentException("Invalid selector string", selectorString);

            foreach (Match match in matches)
            {
                m_id = match.Groups["id"].Value.TrimStart('#'); // element ID should not have leading #
                m_element = match.Groups["element"].Value; // element type goes as is
                m_classes = match.Groups["class"].Value.Split(new[] { ' ', '.' }, StringSplitOptions.RemoveEmptyEntries); // classes should be split. May be we need to use Regex as well, but right now simple split would work

                if (match.Groups["pseudostyle"].Success) // Pseudo-classes are tricky and can be in form ::first-child, :disabled or even :not(enabled)
                {
                    MatchCollection psMatches = s_pseudoClassParser.Matches(match.Groups["pseudostyle"].Value);
                    m_pseudoClasses = new string[psMatches.Count];

                    for (int i = 0; i < m_pseudoClasses.Length; i++)
                        m_pseudoClasses[i] = psMatches[i].Groups[0].Value;
                }

                string attributesText = match.Groups["attributes"].Value;

                if (!string.IsNullOrEmpty(attributesText))
                {
                    // strip the enclosing [ ] and split on the first '=' -- the same parsing
                    // WidgetManager.RegisterElement already does for its own selector table
                    string test = attributesText.Substring(1, attributesText.Length - 2);
                    int equals = test.IndexOf('=');

                    if (equals > 0)
                    {
                        m_attributes = new Dictionary<string, string>(1);
                        m_attributes[test.Substring(0, equals).Trim()] = test.Substring(equals + 1).Trim().Trim('"', '\'');
                    }
                }

                break;
            }
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
        /// whatever <c>Widget.Markup.StyleAttributes</c> the document gave it</param>
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
