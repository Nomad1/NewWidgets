using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace NewWidgets.Utility
{
    /// <summary>
    /// Simplest possible HTML node wrapper for storing provided attributes.
    /// <see cref="ParseXHtml"/> loads XHTML markup with <c>XmlDocument</c> and
    /// <see cref="SaveXHtml"/> writes it back.
    ///
    /// This is the 2021 StyleTree/HtmlNode.cs, moved here and given two things the widget
    /// markup loader needs: arbitrary attributes instead of only id and class (so link/href,
    /// input/type and style are reachable), and XML escaping on the way out. The original stays
    /// in the StyleTree project, which is a separate NUnit sandbox built against an API this
    /// library no longer has.
    ///
    /// ponytail: attributes are a flat list, scanned linearly, because an element carries a
    /// handful of them and insertion order has to survive a round trip. Swap in an ordered
    /// dictionary if something ever puts hundreds of attributes on one element.
    /// </summary>
    public class HtmlNode
    {
        private const string Indent = "    "; // 4 spaces for tabs ;)

        /// <summary>
        /// Element name of a comment node. A comment is kept as an ordinary child so that its
        /// place among the elements survives; '#comment' is the name the DOM already gives one
        /// and no real tag can collide with it, because '#' is not a name start character
        /// </summary>
        public const string CommentElement = "#comment";

        private readonly string m_element;
        private readonly string m_text;

        private readonly HtmlNode m_parent;

        private readonly List<HtmlNode> m_children;
        private readonly List<KeyValuePair<string, string>> m_attributes;

        public string Element
        {
            get { return m_element; }
        }

        public string Text
        {
            get { return m_text; }
        }

        public HtmlNode Parent
        {
            get { return m_parent; }
        }

        public IReadOnlyList<HtmlNode> Children
        {
            get { return m_children; }
        }

        public IReadOnlyList<KeyValuePair<string, string>> Attributes
        {
            get { return m_attributes; }
        }

        public string Id
        {
            get { return GetAttribute("id"); }
        }

        public string Class
        {
            get { return GetAttribute("class"); }
        }

        /// <summary>
        /// True for a comment node, whose <see cref="Text"/> is the comment body
        /// </summary>
        public bool IsComment
        {
            get { return m_element == CommentElement; }
        }

        public HtmlNode(HtmlNode parent, string element, string text)
        {
            m_parent = parent;
            m_element = element;
            m_text = text;
            m_children = new List<HtmlNode>();
            m_attributes = new List<KeyValuePair<string, string>>();

            if (m_parent != null)
                m_parent.m_children.Add(this);
        }

        /// <summary>
        /// Returns the attribute value, or null when the element does not carry it. Null rather
        /// than an empty string, because <c>checked=""</c> and no <c>checked</c> at all are
        /// different things in markup
        /// </summary>
        public string GetAttribute(string name)
        {
            for (int i = 0; i < m_attributes.Count; i++)
                if (m_attributes[i].Key == name)
                    return m_attributes[i].Value;

            return null;
        }

        /// <summary>
        /// Adds the attribute, or replaces it in place when it is already there, so that the
        /// order attributes were written in is the order they are written back out
        /// </summary>
        public void SetAttribute(string name, string value)
        {
            for (int i = 0; i < m_attributes.Count; i++)
                if (m_attributes[i].Key == name)
                {
                    m_attributes[i] = new KeyValuePair<string, string>(name, value);
                    return;
                }

            m_attributes.Add(new KeyValuePair<string, string>(name, value));
        }

        /// <summary>
        /// This method is needed to serialize the node to HTML compatible string
        /// Note that it follows IJW idea, so it's definitelly not optimal and should NOT be used in production
        /// </summary>
        private void Serialize(StringBuilder builder, int level)
        {
            if (level != 0)
                builder.AppendLine(); // no need for empty line before the <html> tag

            for (int i = 0; i < level; i++)
                builder.Append(Indent);

            if (IsComment)
            {
                // a comment carries no attributes and no children, and its body must not be
                // escaped: XML forbids '--' inside one, so nothing a parser produced can need it
                builder.Append("<!--");
                builder.Append(m_text);
                builder.Append("-->");
                return;
            }

            builder.Append('<');
            builder.Append(m_element);

            foreach (KeyValuePair<string, string> attribute in m_attributes)
                builder.AppendFormat(" {0}=\"{1}\"", attribute.Key, Escape(attribute.Value));

            if (string.IsNullOrEmpty(m_text) && m_children.Count == 0)
                builder.Append("/>");
            else
            {
                builder.Append('>');

                if (!string.IsNullOrEmpty(m_text))
                    builder.Append(Escape(m_text));

                foreach (HtmlNode child in m_children)
                    child.Serialize(builder, level + 1);

                if (m_children.Count > 0)
                {
                    builder.AppendLine();

                    for (int i = 0; i < level; i++)
                        builder.Append(Indent);
                }

                builder.Append("</");
                builder.Append(m_element);
                builder.Append('>');
            }
        }

        /// <summary>
        /// The five predefined XML entities. Text and attribute values both go through this, so
        /// a widget whose text contains an ampersand still produces a document that parses back
        /// </summary>
        private static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        private static HtmlNode RecursiveParse(HtmlNode parent, XmlNode node)
        {
            StringBuilder text = new StringBuilder();

            // every text node, not only the first one: <span>a<b/>c</span> holds two of them,
            // and taking "a" alone drops "c" without a word. A node carries one string, so the
            // pieces are joined; the element between them still becomes a child of its own
            //
            // ponytail: joining loses where the text sat relative to the children, so saving
            // such a node writes all its text before all its children. A widget has one text
            // property and cannot say more than that; the upgrade path is a child text node.
            foreach (XmlNode child in node.ChildNodes)
                if (child.NodeType == XmlNodeType.Text)
                    text.Append(child.Value);

            HtmlNode htmlNode = new HtmlNode(parent, node.Name, text.ToString().Trim());

            if (node.Attributes != null)
                foreach (XmlAttribute attribute in node.Attributes)
                    htmlNode.SetAttribute(attribute.Name, attribute.Value);

            // elements and comments in one pass, so a comment keeps its place among the
            // elements it stands between. Text nodes were taken above, out of order by design
            foreach (XmlNode child in node.ChildNodes)
                if (child.NodeType == XmlNodeType.Element)
                    RecursiveParse(htmlNode, child);
                else if (child.NodeType == XmlNodeType.Comment)
                    new HtmlNode(htmlNode, CommentElement, child.Value);

            return htmlNode;
        }

        /// <summary>
        /// This method tries to parse XHTML string with XmlDocument
        /// It will fail if file name is provided or the document is HTML, not a XHTML
        /// </summary>
        /// <param name="xhtmlString"></param>
        /// <returns></returns>
        public static HtmlNode ParseXHtml(string xhtmlString)
        {
            XmlDocument document = new XmlDocument();
            document.XmlResolver = null; // a doctype naming the XHTML DTD must not turn into a web request
            document.LoadXml(xhtmlString);

            return RecursiveParse(null, document.DocumentElement);
        }

        /// <summary>
        /// Converts HTML hierarchy to XHtml string with padding
        /// Note that it's just a simple serialization, it does not use XmlDocument and never checks
        /// the integrity of files produced
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static string SaveXHtml(HtmlNode node)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");

            node.Serialize(builder, 0);

            return builder.ToString();
        }
    }
}
