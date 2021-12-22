using System.Collections.Generic;

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
    }
}
