using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace StyleTree
{
    /// <summary>
    /// Simplest possible HTML node wrapper for storing provided attributes
    /// Static method ParseXHmlt allows loading HTML markup from XHTML file with XmlDocument
    /// </summary>
    public class HtmlNode
    {
        private readonly string m_element;
        private readonly string m_id;
        private readonly string m_class;
        private readonly string m_text;

        private readonly HtmlNode m_parent;

        private readonly ICollection<HtmlNode> m_children;

        public string Element
        {
            get { return m_element; }
        }

        public string Id
        {
            get { return m_id; }
        }

        public string Class
        {
            get { return m_class; }
        }

        public HtmlNode Parent
        {
            get { return m_parent; }
        }

        public HtmlNode(HtmlNode parent, string element, string id = "", string @class = "", string text = "")
        {
            m_parent = parent;
            m_element = element;
            m_id = id;
            m_class = @class;
            m_text = text;
            m_children = new List<HtmlNode>();

            if (m_parent != null)
                m_parent.m_children.Add(this);
        }

        /// <summary>
        /// This method is needed to serialize the node to HTML compatible string
        /// Note that it follows IJW idea, so it's definitelly not optimal and should NOT be used in production 
        /// </summary>
        private void Serialize(StringBuilder builder, int level = 0)
        {
            if (level != 0)
                builder.AppendLine(); // no need for empty line before the <html> tag

            for (int i = 0; i < level; i++)
                builder.Append("    "); // 4 spaces for tabs ;)

            builder.Append('<');
            builder.Append(m_element);

            if (!string.IsNullOrEmpty(m_id))
                builder.AppendFormat(" id=\"{0}\"", m_id);

            if (!string.IsNullOrEmpty(m_class))
                builder.AppendFormat(" class=\"{0}\"", m_class);

            if (string.IsNullOrEmpty(m_text) && m_children.Count == 0)
                builder.Append("/>");
            else
            {
                builder.Append('>');

                if (!string.IsNullOrEmpty(m_text))
                    builder.Append(m_text);

                foreach (HtmlNode child in m_children)
                    child.Serialize(builder, level + 1);

                if (m_children.Count > 0)
                {
                    builder.AppendLine();

                    for (int i = 0; i < level; i++)
                        builder.Append("    "); // 4 spaces for tabs ;)
                }

                builder.Append("</");
                builder.Append(m_element);
                builder.Append('>');
            }
        }

        /// <summary>
        /// Helper method to find the element with specified id in the hierarchy
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public HtmlNode GetElementById(string id)
        {
            if (!string.IsNullOrEmpty(m_id) && m_id == id)
                return this;

            foreach (HtmlNode child in m_children)
            {
                HtmlNode result = child.GetElementById(id);

                if (result != null)
                    return result;
            }

            return null;
        }

        private static HtmlNode RecursiveParse(HtmlNode parent, XmlNode node)
        {
            string text = "";
            foreach (XmlNode child in node.ChildNodes)
                if (child.NodeType == XmlNodeType.Text)
                {
                    text = child.Value.Trim();
                    break;
                }

                HtmlNode htmlNode = new HtmlNode(parent, node.Name,
                node.Attributes != null && node.Attributes["id"] != null ? node.Attributes["id"].Value : "",
                node.Attributes != null && node.Attributes["class"] != null ? node.Attributes["class"].Value : "",
                text);

            foreach (XmlNode child in node.ChildNodes)
                if (child.NodeType != XmlNodeType.Text)
                    RecursiveParse(htmlNode, child);

            return htmlNode;
        }

        /// <summary>
        /// This method tries to parse XHTML string with XmlDocument
        /// It will fail if file name is provided or the document is HTML, not a XHTML
        /// </summary>
        /// <param name="xhtmlString"></param>
        /// <returns></returns>
        public static HtmlNode ParseXHmlt(string xhtmlString)
        {
            XmlDocument document = new XmlDocument();
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
        public static string SaveXHmlt(HtmlNode node)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");

            node.Serialize(builder);

            return builder.ToString();
        }
    }
}
