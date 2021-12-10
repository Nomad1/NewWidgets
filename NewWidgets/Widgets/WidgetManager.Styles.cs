using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Xml;
using NewWidgets.UI;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    public static partial class WidgetManager
    {
        // styles
        private static readonly IDictionary<string, WidgetStyleSheet> s_styles = new Dictionary<string, WidgetStyleSheet>();
        private static readonly IDictionary<string, Tuple<WidgetParameterIndex, Type>> s_styleParameters = InitStyleParameterMap();

        /// <summary>
        /// Gets the style by name
        /// </summary>
        /// <returns>The style.</returns>
        /// <param name="name">Name.</param>
        public static WidgetStyleSheet GetStyle(string name, bool autoCreate = false)
        {
            if (!string.IsNullOrEmpty(name))
            {
                WidgetStyleSheet result;

                if (s_styles.TryGetValue(name, out result))
                    return result;

                if (autoCreate)
                {
                    return s_styles[name] = new WidgetStyleSheet(name, Widget.DefaultStyle);
                }
            }

            return default(WidgetStyleSheet);
        }

        #region XML CSS loading

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

            string parent = GetAttribute(node, "parent");

            WidgetStyleSheet parentStyle = GetStyle(parent, true); // creates the parent!

            WidgetStyleSheet style = GetStyle(name, false); // don't create the child yet!

            if (style.IsEmpty)
            {
                style = new WidgetStyleSheet(name, parentStyle); // creates the child!
                s_styles[name] = style;
            }
            else
                style.SetParent(parentStyle);

            InitStyle(ref style, node);

            WindowController.Instance.LogMessage("Registered style {0}", name);
        }

        #endregion

        /// <summary>
        /// This method enumerates all items of WidgetParameterIndex looking for WidgetParameter attribute
        /// and storing data in a dictionary
        /// </summary>
        /// <returns></returns>
        private static IDictionary<string, Tuple<WidgetParameterIndex, Type>> InitStyleParameterMap()
        {
            Dictionary<string, Tuple<WidgetParameterIndex, Type>> result = new Dictionary<string, Tuple<WidgetParameterIndex, Type>>();

            FieldInfo[] fields = typeof(WidgetParameterIndex).GetFields(BindingFlags.Public | BindingFlags.Static);

            foreach (FieldInfo field in fields)
            {
                WidgetParameterIndex index = (WidgetParameterIndex)field.GetValue(null);

                foreach (WidgetParameterAttribute attribute in field.GetCustomAttributes(typeof(WidgetParameterAttribute), false))
                    result[attribute.Name] = new Tuple<WidgetParameterIndex, Type>(index, attribute.Type);
            }

            return result;
        }

        private static void InitStyle(ref WidgetStyleSheet style, XmlNode node)
        {
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

                    Tuple<WidgetParameterIndex, Type> field;

                    if (s_styleParameters.TryGetValue(element.Name, out field))
                        style.Set(null, field.Item1, ParseValue(field.Item2, value));

                    //style.Set(null, element.Name, value);

                }
                catch (Exception ex)
                {
                    WindowController.Instance.LogError("Error parsing style {0}, element {1}: {2}", style.Name, element.Name, ex);
                    throw new WidgetException("Error parsing style!", ex);
                }
            }
        }

        internal static Tuple<WidgetParameterIndex, Type> GetParameterIndexByName(string name)
        {
            Tuple<WidgetParameterIndex, Type> field;

            if (s_styleParameters.TryGetValue(name, out field))
                return field;

            return null;
        }

        internal static object ParseValue(Type targetType, string value)
        {
            object memberValue;

            if (targetType == typeof(Font))
                memberValue = GetFont(value); // font should be registered first to avoid confusion
            else if (targetType == typeof(WidgetStyleSheet))
                memberValue = GetStyle(value, true); // saves only reference
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
