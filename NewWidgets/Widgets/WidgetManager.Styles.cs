using System;
using System.Collections.Generic;
using System.Xml;
using NewWidgets.UI;
using NewWidgets.UI.Styles;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    public static partial class WidgetManager
    {
        // this is primary CSS style collection for now
        private static readonly StyleCollection s_styleCollection = new StyleCollection();

        /// <summary>
        /// Gets the style by name. This method is here only for compatibility purposes and it would be removed in later versions
        /// </summary>
        /// <returns>The style.</returns>
        /// <param name="class">Name.</param>
        public static WidgetStyle GetStyle(string @class, bool notUsed = false)
        {
            return new WidgetStyle(new string[] { @class }, string.Empty);
        }

        /// <summary>
        /// Gets the style by selector list. It works with hierarchy, specificity and all the stuff
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        internal static WidgetStyleSheet GetStyle(StyleSelectorList list)
        {
            ICollection<StyleNode> result = s_styleCollection.GetStyleData(list);

            return new WidgetStyleSheet(list.ToString(), result);
        }

        /// <summary>
        /// Gets the style by single style selector
        /// </summary>
        /// <param name="singleSelector"></param>
        /// <returns></returns>
        internal static WidgetStyleSheet GetStyle(StyleSelector singleSelector)
        {
            return GetStyle(new StyleSelectorList(singleSelector));
        }

        #region XML style loading

        /// <summary>
        /// Loads ui data from a XML string
        /// </summary>
        /// <param name="uiData"></param>
        /// <exception cref="WidgetException"></exception>
        public static void LoadUI(string uiData)
        {
            try
            {
                XmlDocument document = new XmlDocument();
                document.LoadXml(uiData);

                foreach (XmlNode root in document.ChildNodes)
                {
                    if (root.Name == "ui")
                    {
                        foreach (XmlNode node in root.ChildNodes)
                        {
                            switch (node.Name)
                            {
                                case "font":
                                    RegisterFont(node);
                                    break;
                                case "nine":
                                    RegisterNinePatch(node);
                                    break;
                                case "three":
                                    RegisterThreePatch(node);
                                    break;
                                case "style":
                                    RegisterStyle(node);
                                    break;
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                WindowController.Instance.LogError("Error loading ui data: " + ex);
                throw new WidgetException("Error loading ui data", ex);
            }
        }

        public static void SaveUI(System.IO.TextWriter outputStream)
        {
            s_styleCollection.Dump(outputStream);
        }

        private static void RegisterFont(XmlNode node)
        {
            string name = node.Attributes.GetNamedItem("name").Value;
            string resource = node.Attributes.GetNamedItem("resource").Value;
            float spacing = ConversionHelper.FloatParse(node.Attributes.GetNamedItem("spacing").Value);
            int baseline = int.Parse(node.Attributes.GetNamedItem("baseline").Value);

            int shift = 0;

            if (node.Attributes.GetNamedItem("shift") != null)
                shift = int.Parse(node.Attributes.GetNamedItem("shift").Value);

            int leading = 0;

            if (node.Attributes.GetNamedItem("leading") != null)
                leading = int.Parse(node.Attributes.GetNamedItem("leading").Value);

            Font font = new Font(resource, spacing, leading, baseline, shift);

            s_fonts[name] = font;

            if (name == "default")
                s_mainFont = font;

            WindowController.Instance.LogMessage("Registered font {0}, resource {1}, spacing {2}", name, resource, spacing);
        }

        private static void RegisterNinePatch(XmlNode node)
        {
            string name = node.Attributes.GetNamedItem("name").Value;

            WindowController.Instance.SetSpriteSubdivision(name, 3, 3);

            WindowController.Instance.LogMessage("Registered nine patch {0}", name);
        }

        private static void RegisterThreePatch(XmlNode node)
        {
            string name = node.Attributes.GetNamedItem("name").Value;

            WindowController.Instance.SetSpriteSubdivision(name, 3, 1);

            WindowController.Instance.LogMessage("Registered three patch {0}", name);
        }

        private static void RegisterStyle(XmlNode node)
        {
            string name = GetAttribute(node, "name");

            if (string.IsNullOrEmpty(name))
                throw new WidgetException("Got style without a name!");

            string parent = GetAttribute(node, "parent");

            IDictionary<WidgetParameterIndex, object> parameters = InitStyle(node);

            name = name.StartsWith("default_") ? name.Substring(8) : string.IsNullOrEmpty(parent) ? char.IsLetter(name[0]) ? ("." + name) : name : ("." + parent + "." + name);

            s_styleCollection.AddStyle(name, new StyleSheetData(parameters));

            WindowController.Instance.LogMessage("Registered style {0}", name);
        }

        #endregion

        private static IDictionary<WidgetParameterIndex, object> InitStyle(XmlNode node)
        {
            Dictionary<WidgetParameterIndex, object> style = new Dictionary<WidgetParameterIndex, object>();

            foreach (XmlNode element in node.ChildNodes)
            {
                try
                {
                    string value = element.InnerText;
                    if (string.IsNullOrEmpty(value) && element.Attributes.GetNamedItem("value") != null)
                        value = element.Attributes.GetNamedItem("value").Value;

                    if (value == null)
                        value = string.Empty;
                    else
                        value = value.Trim('\r', '\n', '\t', ' ');

                    IParameterProcessor processor = WidgetParameterMap.GetProcessorByXmlName(element.Name);

                    if (processor == null)
                        WindowController.Instance.LogMessage("Got unknown attribute {0} in xml style sheet for {1}", element.Name, node.Name);
                    else
                        processor.Process(style, value);
                }
                catch (Exception ex)
                {
                    WindowController.Instance.LogError("Error parsing style {0}, element -nw-{1}: {2}", node.Name, element.Name, ex);
                    throw new WidgetException("Error parsing style!", ex);
                }
            }

            return style;
        }

        public static string GetAttribute(XmlNode node, string name)
        {
            var attribute = node.Attributes.GetNamedItem(name);

            return attribute == null ? null : attribute.Value;
        }
    }
}
