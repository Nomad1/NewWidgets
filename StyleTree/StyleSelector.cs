using System.Text;

namespace StyleTree
{
    /// <summary>
    /// Basic CSS selector description parsed from form E.class#id:hover
    /// </summary>
    internal class StyleSelector
    {
        private readonly string m_element;
        private readonly string m_class;
        private readonly string m_id;
        private readonly string m_pseudoClass;

        public string Element
        {
            get { return m_element; }
        }

        public string Class
        {
            get { return m_class; }
        }

        public string Id
        {
            get { return m_id; }
        }

        public string PseudoClass
        {
            get { return m_pseudoClass; }
        }

        public StyleSelector(string element, string @class, string id, string pseudoClass)
        {
            m_element = element;
            m_class = @class;
            m_id = id;
            m_pseudoClass = pseudoClass;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            if (m_element != null)
                builder.Append(m_element);

            if (m_class != null)
                builder.Append(m_class);

            if (m_id != null)
                builder.Append(m_id);

            if (m_pseudoClass != null)
                builder.Append(m_pseudoClass);

            return builder.ToString();
        }

        public bool ExactMatch(StyleSelector other)
        {
            if (other == null)
                return false;

            return m_element == other.Element && m_class == other.Class && m_id == other.Id && m_pseudoClass == other.PseudoClass;
        }
    }
}
