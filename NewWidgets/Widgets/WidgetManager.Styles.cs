using System;
using System.Collections.Generic;
using System.Numerics;
using System.Xml;
using NewWidgets.UI;
using NewWidgets.UI.Styles;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    public static partial class WidgetManager
    {
        // this is primary CSS style collection for now
        private static readonly StyleCollection m_styleCollection = new StyleCollection();

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
            ICollection<IStyleData> result = m_styleCollection.GetStyleData(list);

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


        private static void RegisterFont(XmlNode node)
        {
            string name = node.Attributes.GetNamedItem("name").Value;
            string resource = node.Attributes.GetNamedItem("resource").Value;
            float spacing = FloatParse(node.Attributes.GetNamedItem("spacing").Value);
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

            name = name.StartsWith("default_") ? name.Substring(8) : string.IsNullOrEmpty(parent) ? ("." + name) : ("." + parent + "." + name);

            m_styleCollection.AddStyle(name, new StyleSheetData(parameters));

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

                    // Old XML stylesheets should be converted to CSS, but for compatibility reasons
                    // I'm adding fake CSS elements starting with "-nw-" solely for parsing purposes

                    WidgetParameterIndex index = IndexNameMap<WidgetParameterIndex>.GetIndexByName("-nw-" + element.Name);

                    if (index == 0)
                    {
                        WindowController.Instance.LogMessage("Got unknown attribute -nw-{0} in xml style sheet for {1}", element.Name, node.Name);
                        continue;
                    }

                    // Here we're retrieving WidgetXMLParameterAttribute that helps with conversion of
                    // XML string to one or several CSS indexed values. 

                    WidgetXMLParameterAttribute attribute = IndexNameMap<WidgetParameterIndex>.GetAttributeByIndex<WidgetXMLParameterAttribute>(index);

                    // TODO: replace ParseValue with smarter conversion taking Dictionary and Type as an input and able to split short-hand properties

                    if (attribute != null && attribute.Type != null)
                        style[index] = ParseValue(attribute.Type, value);
                    else
                        style[index] = value;

                }
                catch (Exception ex)
                {
                    WindowController.Instance.LogError("Error parsing style {0}, element -nw-{1}: {2}", node.Name, element.Name, ex);
                    throw new WidgetException("Error parsing style!", ex);
                }
            }

            return style;
        }

        /// <summary>
        /// Tries to parse string to specified type
        /// </summary>
        /// <param name="targetType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static object ParseValue(Type targetType, string value)
        {
            object memberValue;

            if (targetType == typeof(Font))
                memberValue = GetFont(value); // font should be registered first to avoid confusion
            else if (targetType == typeof(string))
                memberValue = value;
            else if (targetType == typeof(float))
                memberValue = FloatParse(value); // bo-oxing (
            else if (targetType == typeof(uint))
                memberValue = ColorParse(value); // assume uint field is color by default
            else if (targetType == typeof(Margin))
                memberValue = MarginParse(value);
            else if (targetType == typeof(Vector2))
                memberValue = Vector2Parse(value);
            else if (targetType == typeof(Vector3))
                memberValue = Vector3Parse(value);
            else if (targetType == typeof(Vector4))
                memberValue = Vector4Parse(value);
            else if (targetType.IsEnum)
                memberValue = EnumParse(targetType, value);
            else
                memberValue = Convert.ChangeType(value, targetType); // fallback, may crash

            return memberValue;
        }

        #region String parsers

        /// <summary>
        /// Culture invariant float parsing
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="value">Value.</param>
        public static float FloatParse(string value)
        {
            return float.Parse(value.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parse string value to color. Accepts 0x and # as a preffix for 24-bit hex colors
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="value">Value.</param>
        public static uint ColorParse(string value)
        {
            if (value.Length >= 7 && value[0] == '#')
                return uint.Parse(value.Substring(1), System.Globalization.NumberStyles.HexNumber);
            if (value.Length >= 8 && value[0] == '0' && value[1] == 'x')
                return uint.Parse(value.Substring(2), System.Globalization.NumberStyles.HexNumber);

            return uint.Parse(value);
        }

        /// <summary>
        /// Parse a string to a 2-component vector
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="value">Value.</param>
        public static Vector2 Vector2Parse(string value)
        {
            string[] values = value.Split(';');

            if (values.Length != 2)
                throw new ArgumentException("Invalid string value for Vector2 type!");

            float x = FloatParse(values[0]);
            float y = FloatParse(values[1]);
            return new Vector2(x, y);
        }

        /// <summary>
        /// Parse a string to a 3-component vector
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="value">Value.</param>
        public static Vector3 Vector3Parse(string value)
        {
            string[] values = value.Split(';');

            if (values.Length != 3)
                throw new ArgumentException("Invalid string value for Vector3 type!");

            float x = FloatParse(values[0]);
            float y = FloatParse(values[1]);
            float z = FloatParse(values[2]);
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Parse a string to a 4-component vector
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="value">Value.</param>
        public static Vector4 Vector4Parse(string value)
        {
            string[] values = value.Split(';');

            if (values.Length != 4)
                throw new ArgumentException("Invalid string value for Vector4 type!");

            float x = FloatParse(values[0]);
            float y = FloatParse(values[1]);
            float z = FloatParse(values[2]);
            float w = FloatParse(values[3]);
            return new Vector4(x, y, z, w);
        }


        /// <summary>
        /// Pastse a string to 4-component or 1-component margin
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="value">Value.</param>
        public static Margin MarginParse(string value)
        {
            string[] values = value.Split(';');

            if (values.Length != 1 && values.Length != 4)
                throw new ArgumentException("Invalid string value for Margin type!");

            if (values.Length == 1)
            {
                float a = FloatParse(values[0]);
                return new Margin(a, a, a, a);
            }

            float l = FloatParse(values[0]);
            float t = FloatParse(values[1]);
            float r = FloatParse(values[2]);
            float b = FloatParse(values[3]);

            return new Margin(l, t, r, b);
        }

        /// <summary>
        /// Parse a string to specific enum tpye
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="value">Value.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static T EnumParse<T>(string value)
        {
            int result = 0; // TODO: GetUnderlyingType()?

            string[] strings = value.Split('|');

            foreach (string str in strings)
                result |= (int)Enum.Parse(typeof(T), str, true);

            return (T)Enum.ToObject(typeof(T), result);
        }

        /// <summary>
        /// Parse a string to specific enum type
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="value">Value.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static Enum EnumParse(Type enumType, string value)
        {
            int result = 0; // TODO: GetUnderlyingType()?

            string[] strings = value.Split('|');

            foreach (string str in strings)
                result |= (int)Enum.Parse(enumType, str, true);

            return (Enum)Enum.ToObject(enumType, result);
        }

        public static string GetAttribute(XmlNode node, string name)
        {
            var attribute = node.Attributes.GetNamedItem(name);

            return attribute == null ? null : attribute.Value;
        }

        #endregion
    }
}
