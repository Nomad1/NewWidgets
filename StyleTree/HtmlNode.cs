using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace StyleTree
{
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
            m_id = string.IsNullOrEmpty(id) ? "" : id[0] == '#' ? id : "#" + id;
            m_class = string.IsNullOrEmpty(@class) ? "" : @class[0] == '.' ? @class : "." + @class;
            m_text = text;
            m_children = new List<HtmlNode>();

            if (m_parent != null)
                m_parent.m_children.Add(this);
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
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
                    builder.Append(child);

                builder.Append("</");
                builder.Append(m_element);
                builder.Append('>');
            }

            return builder.ToString();
        }

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
                    text = child.Value;
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

        public static HtmlNode ParseXHmlt(string text)
        {
            XmlDocument document = new XmlDocument();
            document.LoadXml(text);

            return RecursiveParse(null, document.FirstChild);
        }
    }
}
