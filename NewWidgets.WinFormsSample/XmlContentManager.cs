using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace SpaceAdventure.Content
{
    /*
    public class XmlContentManager : ContentManager
    {
        private readonly IDictionary<Type, IDictionary<uint, ContentBase>> m_data = new Dictionary<Type, IDictionary<uint, ContentBase>>();

        public void RegisterData(ContentBase data)
        {
            Type type = data.GetType();

            IDictionary<uint, ContentBase> dictionary;
            if (!m_data.TryGetValue(type, out dictionary))
            {
                dictionary = new Dictionary<uint, ContentBase>();
                m_data[type] = dictionary;
            }

            dictionary[data.ID] = data;
        }

        public T GetData<T>(uint id) where T : ContentBase
        {
            IDictionary<uint, ContentBase> dictionary;
            if (!m_data.TryGetValue(typeof(T), out dictionary))
                throw new KeyNotFoundException("Data type not found: " + typeof(T));

            ContentBase result;

            if (dictionary.TryGetValue(id, out result))
                return (T)result;

            return default(T);
        }

        public void RemoveData<T>(uint id) where T : ContentBase
        {
            IDictionary<uint, ContentBase> dictionary;
            if (!m_data.TryGetValue(typeof(T), out dictionary))
                throw new KeyNotFoundException("Data type not found: " + typeof(T));

            dictionary.Remove(id);
        }


        public ICollection<T> GetAllData<T>() where T : ContentBase
        {
            IDictionary<uint, ContentBase> dictionary;
            if (!m_data.TryGetValue(typeof(T), out dictionary))
                throw new KeyNotFoundException("Data type not found: " + typeof(T));

            List<T> result = new List<T>(dictionary.Count);

            foreach (ContentBase content in dictionary.Values)
                result.Add((T)content);

            result.Sort((x, y) => x.ID.CompareTo(y.ID));

            return result;
        }

        public string GetAttribute(XmlNode node, string attribute, string def)
        {
            if (node.Attributes != null && node.Attributes.GetNamedItem(attribute) != null)
            {
                return node.Attributes.GetNamedItem(attribute).Value;
            }
            return def;
        }

        public int GetIntAttribute(XmlNode node, string attribute, int def)
        {
            if (node.Attributes != null && node.Attributes.GetNamedItem(attribute) != null)
            {
                int result;
                string value = node.Attributes.GetNamedItem(attribute).Value;
                if (int.TryParse(value, out result))
                {
                    return result;
                }
                else
                {
                    Console.WriteLine("Invalid int attribute " + attribute + ", value " + value);
                }
            }
            return def;
        }

        public float GetFloatAttribute(XmlNode node, string attribute, float def)
        {
            if (node.Attributes != null && node.Attributes.GetNamedItem(attribute) != null)
            {
                return ParseFloat(node.Attributes.GetNamedItem(attribute).Value, def);
            }
            return def;
        }

        public static float ParseFloat(string value, float def)
        {
            float result;
            if (float.TryParse(value.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out result))
                return result;

            return def;
        }

        public void SetAttribute<T>(XmlNode node, string name, T value)
        {
            XmlAttribute attribute = node.OwnerDocument.CreateAttribute(name);
            attribute.Value = value.ToString();
            node.Attributes.Append(attribute);
        }

        public XmlNode CreateNode(XmlNode rootNode, string name)
        {
            XmlElement node = rootNode.OwnerDocument.CreateElement(name);
            rootNode.AppendChild(node);
            return node;
        }

        internal class ContentHandler
        {
            private readonly string m_nodeName;
            private readonly Action<ContentManager, XmlNode, object> m_handler;

            public string NodeName
            {
                get { return m_nodeName; }
            }

            public Action<ContentManager, XmlNode, object> Handler
            {
                get { return m_handler; }
            }

            public ContentHandler(string node, Action<ContentManager, XmlNode, object> handler)
            {
                m_nodeName = node;
                m_handler = handler;
            }
        }

    }*/

    public interface IContentBase
    {
        uint Id { get; }
    }

    public interface IContentNode
    {
        string Name { get; }
        IEnumerable<IContentNode> Children { get; }
    }

    public delegate void HandleContentDelegate(IContentManager manager, IContentNode node, object param);

    public interface IContentManager
    {
        float GetFloatAttribute(IContentNode node, string attribute, float def);
        int GetIntAttribute(IContentNode node, string attribute, int def);
        int GetColorAttribute(IContentNode node, string attribute, int def);
        string GetAttribute(IContentNode node, string attribute, string def);

        string GetStringValue(IContentNode node, string def);
        float GetFloatValue(IContentNode node, float def);

        void SetAttribute<T>(IContentNode node, string attribute, T value);

        //void RegisterData(IContentBase data);
        void RegisterData<T>(uint id, T data) where T : IContentBase;
        void RegisterContentType(string type, HandleContentDelegate handler);

        T GetData<T>(uint id) where T : IContentBase;
        bool TryGetData<T>(uint id, out T result) where T : IContentBase;
        ICollection<T> GetAllData<T>() where T : IContentBase;
        T[] GetAllDataSorted<T>() where T : IContentBase;
        void RemoveData<T>(uint id) where T : IContentBase;

        IContentNode CreateNode(IContentNode parentNode, string name);
    }

    public class ContentException : ApplicationException
    {
        public ContentException(string message)
            : base(message)
        {
        }
    }

    public class XmlContentManager : IContentManager
    {
        #region Helper class

        private class XmlContentNode : IContentNode
        {
            private readonly XmlNode m_node;

            public string Name
            {
                get { return m_node.Name; }
            }

            public XmlNode Node
            {
                get { return m_node; }
            }

            public IEnumerable<IContentNode> Children
            {
                get
                {
                    List<IContentNode> children = new List<IContentNode>();
                    foreach (XmlNode child in m_node.ChildNodes)
                        children.Add(new XmlContentNode(child));
                    return children;
                }
            }

            public XmlContentNode(XmlNode node)
            {
                m_node = node;
            }
        }


        #endregion

        private readonly Dictionary<string, HandleContentDelegate> m_contentTypes = new Dictionary<string, HandleContentDelegate>();

//        private readonly IDictionary<Type, IDictionary<uint, IContentBase>> m_data = new Dictionary<Type, IDictionary<uint, IContentBase>>();
        private readonly IDictionary<Type, System.Collections.IDictionary> m_data = new Dictionary<Type, System.Collections.IDictionary>();

        public readonly string LanguageSuffix;

        public void RegisterContentType(string type, HandleContentDelegate handler)
        {
            m_contentTypes[type] = handler;
        }

        public XmlContentManager(string languageSuffix = "EN")
        {
            LanguageSuffix = languageSuffix;
        }

        /* public void RegisterData(IContentBase data)
         {
             Type type = data.GetType();

             IDictionary<uint, IContentBase> dictionary;
             if (!m_data.TryGetValue(type, out dictionary))
             {
                 dictionary = new Dictionary<uint, IContentBase>();
                 m_data[type] = dictionary;
             }

             dictionary[data.ID] = data;
         }*/

        public void RegisterData<T>(uint id, T data) where T : IContentBase
        {
            Type type = typeof(T);

            System.Collections.IDictionary dictionary;
            if (!m_data.TryGetValue(type, out dictionary))
            {
                dictionary = new Dictionary<uint, T>();
                m_data[type] = dictionary;
            }

            ((IDictionary<uint, T>)dictionary)[id] = data;
        }

        public T GetData<T>(uint id) where T : IContentBase
        {
            System.Collections.IDictionary dictionary;
            if (!m_data.TryGetValue(typeof(T), out dictionary))
                throw new KeyNotFoundException("Data type not found: " + typeof(T));

            T result;

            if (((IDictionary<uint, T>)dictionary).TryGetValue(id, out result))
                return result;

            return default(T);
        }

        public void RemoveData<T>(uint id) where T : IContentBase
        {
            System.Collections.IDictionary dictionary;
            if (!m_data.TryGetValue(typeof(T), out dictionary))
                throw new KeyNotFoundException("Data type not found: " + typeof(T));

            dictionary.Remove(id);
        }

        public T[] GetAllDataSorted<T>() where T : IContentBase
        {
            System.Collections.IDictionary dictionary;
            if (!m_data.TryGetValue(typeof(T), out dictionary))
                throw new KeyNotFoundException("Data type not found: " + typeof(T));

            T[] result = new T[dictionary.Count];

            ((IDictionary<uint, T>)dictionary).Values.CopyTo(result, 0);

            Array.Sort(result, (x, y) => Comparer<uint>.Default.Compare(x.Id, y.Id));

            return result;
        }

        public ICollection<T> GetAllData<T>() where T : IContentBase
        {
            System.Collections.IDictionary dictionary;
            if (!m_data.TryGetValue(typeof(T), out dictionary))
                throw new KeyNotFoundException("Data type not found: " + typeof(T));

            return (ICollection<T>)dictionary.Values;
        }

        public bool TryGetData<T>(uint id, out T result) where T : IContentBase
        {
            System.Collections.IDictionary dictionary;
            if (!m_data.TryGetValue(typeof(T), out dictionary))
                throw new KeyNotFoundException("Data type not found: " + typeof(T));

            T nresult;
            if (((IDictionary<uint, T>)dictionary).TryGetValue(id, out nresult))
            {
                result = nresult;
                return true;
            }

            result = default(T);
            return false;
        }

        private void ProcessXmlNode(XmlNode node, object context)
        {
            HandleContentDelegate handler;
            if (m_contentTypes.TryGetValue(node.Name, out handler))
            {
                handler(this, new XmlContentNode(node), context);
                return;
            }

            foreach (XmlNode child in node.ChildNodes)
                ProcessXmlNode(child, context);
        }

        public bool ParseXml(string xmlContent, object context)
        {
            try
            {
#if JAVASCRIPT
                XmlDocumentParser parser = new XmlDocumentParser();
                XmlDocument document = parser.ParseFromString(xmlContent, "text/xml");
#else
                XmlDocument document = new XmlDocument();
                document.LoadXml(xmlContent);
#endif
                foreach (XmlNode root in document.ChildNodes)
                {
                    if (StringEquals(root.Name, "root"))
                    {
                        foreach (XmlNode node in root.ChildNodes)
                            ProcessXmlNode(node, context);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception loading xml content: " + ex);
                return false;
            }
        }

        public string SaveXml(Action<IContentNode> processRootNode)
        {
            XmlDocument document = new XmlDocument();

            XmlDeclaration xmldecl;
            xmldecl = document.CreateXmlDeclaration("1.0", "utf-8", null);

            XmlNode rootNode = document.CreateElement("root");
            document.AppendChild(rootNode);
            document.InsertBefore(xmldecl, rootNode);

            processRootNode(new XmlContentNode(rootNode));

            XmlWriterSettings writerSettings = new XmlWriterSettings();
            writerSettings.Encoding = Encoding.UTF8;
            writerSettings.OmitXmlDeclaration = false;
            writerSettings.Indent = true;
            writerSettings.NewLineChars = "\n";
            writerSettings.IndentChars = "\t";

            using (var stringWriter = new StringWriter())
            using (var xmlWriter = XmlWriter.Create(stringWriter, writerSettings))
            {
                document.Save(xmlWriter);
                return stringWriter.ToString();
            }
        }

        #region static helpers

        public static bool StringEquals(string one, string another)
        {
#if JAVASCRIPT
            return one == another;
#else
            return one.Equals(another);
#endif
        }

        public string GetAttribute(IContentNode inode, string attribute, string def)
        {
            XmlContentNode node = inode as XmlContentNode;

            if (node == null || node.Node == null)
                throw new ContentException("Invalid content node");

            if (node.Node.Attributes != null && node.Node.Attributes.GetNamedItem(attribute) != null)
            {
                return node.Node.Attributes.GetNamedItem(attribute).Value;
            }
            return def;
        }

        public int GetIntAttribute(IContentNode inode, string attribute, int def)
        {
            XmlContentNode node = inode as XmlContentNode;

            if (node == null || node.Node == null)
                throw new ContentException("Invalid content node");

            if (node.Node.Attributes != null && node.Node.Attributes.GetNamedItem(attribute) != null)
            {
                int result;
                string value = node.Node.Attributes.GetNamedItem(attribute).Value;
                if (int.TryParse(value, out result))
                {
                    return result;
                }
                else
                {
                    Console.WriteLine("Invalid int attribute " + attribute + ", value " + value);
                }
            }
            return def;
        }

        public string GetStringValue(IContentNode inode, string def)
        {
            XmlContentNode node = inode as XmlContentNode;

            if (node == null || node.Node == null)
                throw new ContentException("Invalid content node");

            string text = node.Node.InnerText;

            string[] lines = text.Split('\n', '\r');

            for (int i = 0; i < lines.Length; i++)
                lines[i] = lines[i].Trim(' ', '\t', '\n', '\r');

            bool skipFirst = string.IsNullOrEmpty(lines[0]);
            bool skipLast = string.IsNullOrEmpty(lines[lines.Length - 1]);

            return string.Join("\n", lines, skipFirst ? 1 : 0, lines.Length - (skipFirst ? 1 : 0) - (skipLast ? 1 : 0));
        }

        public float GetFloatValue(IContentNode inode, float def)
        {
            XmlContentNode node = inode as XmlContentNode;

            if (node == null || node.Node == null)
                throw new ContentException("Invalid content node");

            return ParseFloat(node.Node.InnerText, def);
        }

        public int GetColorAttribute(IContentNode inode, string attribute, int def)
        {
#if JAVASCRIPT
            return def;
#else

            string colorString = GetAttribute(inode, attribute, string.Empty);
            if (!string.IsNullOrEmpty(colorString))
            {
                bool hex = false;
                if (colorString.StartsWith("#"))
                {
                    colorString = colorString.Substring(1);
                    hex = true;
                }
                else
                if (colorString.StartsWith("0x"))
                {
                    hex = true;
                    colorString = colorString.Substring(2);
                }

                return int.Parse(colorString, hex ? System.Globalization.NumberStyles.HexNumber : 0);
            }
            return def;
#endif
        }

        public float GetFloatAttribute(IContentNode inode, string attribute, float def)
        {
            XmlContentNode node = inode as XmlContentNode;

            if (node == null || node.Node == null)
                throw new ContentException("Invalid content node");

            if (node.Node.Attributes != null && node.Node.Attributes.GetNamedItem(attribute) != null)
            {
                return ParseFloat(node.Node.Attributes.GetNamedItem(attribute).Value, def);
            }
            return def;
        }

        public void SetAttribute<T>(IContentNode inode, string name, T value)
        {
            XmlContentNode node = inode as XmlContentNode;

            if (node == null || node.Node == null)
                throw new ContentException("Invalid content node");

            XmlAttribute attribute = node.Node.OwnerDocument.CreateAttribute(name);
            attribute.Value = value.ToString();
            node.Node.Attributes.Append(attribute);
        }

        public IContentNode CreateNode(IContentNode inode, string name)
        {
            XmlContentNode rootNode = inode as XmlContentNode;

            if (rootNode == null || rootNode.Node == null)
                throw new ContentException("Invalid content node");

            XmlElement node = rootNode.Node.OwnerDocument.CreateElement(name);
            rootNode.Node.AppendChild(node);
            return new XmlContentNode(node);
        }

        private static float ParseFloat(string value, float def)
        {
#if JAVASCRIPT
            try
            {
            return float.Parse(value.Replace(",", "."));
            }
            catch
            {
            return def;
            }
#else
            float result;
            if (float.TryParse(value.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out result))
                return result;

            return def;
#endif
        }
        #endregion
    }

}
