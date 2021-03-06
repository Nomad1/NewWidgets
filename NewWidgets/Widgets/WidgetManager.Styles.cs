﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Xml;
using NewWidgets.UI;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    #region Helpers

    public class WidgetException : ApplicationException
    {
        public WidgetException()
            : base()
        {
        }

        public WidgetException(string message)
            : base(message)
        {
        }

        public WidgetException(string message, Exception ex)
            : base(message, ex)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    internal class WidgetParameterAttribute : Attribute
    {
        private readonly string m_name;
        private readonly Type m_type;

        public string Name
        {
            get { return m_name; }
        }

        public Type Type
        {
            get { return m_type; }
        }

        public WidgetParameterAttribute(string name, Type type = null)
        {
            m_name = name;
            m_type = type ?? typeof(string);
        }
    }


    #endregion

    public static partial class WidgetManager
    {
        // styles
        private static readonly IDictionary<string, WidgetStyleSheet> s_styles = new Dictionary<string, WidgetStyleSheet>();
        private static readonly IDictionary<string, Tuple<WidgetParameterIndex, Type>> s_styleAttributes = InitStyleMap();


        // Those guys are obsolete and should be replaced by direct Widget.DefaultStyle like calls

        [Obsolete("Use Widget.DefaultStyle instead")]
        public static WidgetStyleSheet DefaultWidgetStyle { get { return Widget.DefaultStyle; } }
      /*  [Obsolete("Use WidgetTextEdit.DefaultStyle instead")]
        public static WidgetStyleReference DefaultTextEditStyle { get { return WidgetTextEdit.DefaultStyle; } }
        [Obsolete("Use WidgetWindow.DefaultStyle instead")]
        public static WidgetStyleReference DefaultWindowStyle { get { return WidgetWindow.DefaultStyle; } }
        [Obsolete("Use WidgetWindow.DefaultStyle instead")]
        public static WidgetStyleReference DefaultPanelStyle { get { return WidgetPanel.DefaultStyle; } }*/

        //
        //[Obsolete("Use WidgetLabel.DefaultStyle instead")]
        //public static WidgetTextStyleSheet DefaultLabelStyle { get { return WidgetLabel.DefaultStyle.Get<WidgetTextStyleSheet>(); } }  // needed only for font size


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

        private static IDictionary<string, Tuple<WidgetParameterIndex, Type>> InitStyleMap()
        {
            Dictionary<string, Tuple<WidgetParameterIndex, Type>> result = new Dictionary<string, Tuple<WidgetParameterIndex, Type>>();

            FieldInfo[] fields = typeof(WidgetParameterIndex).GetFields(BindingFlags.Public | BindingFlags.Static);

            foreach (FieldInfo field in fields)
            {
                foreach (WidgetParameterAttribute attribute in field.GetCustomAttributes(typeof(WidgetParameterAttribute), true))
                    result[attribute.Name] = new Tuple<WidgetParameterIndex, Type>((WidgetParameterIndex)field.GetValue(null), attribute.Type);
            }

            return result;
        }

        private static void InitStyle(ref WidgetStyleSheet style, XmlNode node)
        {
            foreach (XmlNode element in node.ChildNodes)
            {
#if !DEBUG
                try
#endif
                {
                    string value = element.InnerText;
                    if (string.IsNullOrEmpty(value) && element.Attributes.GetNamedItem("value") != null)
                        value = element.Attributes.GetNamedItem("value").Value;

                    if (value == null)
                        value = string.Empty;
                    else
                        value = value.Trim('\r', '\n', '\t', ' ');

                    Tuple<WidgetParameterIndex, Type> field;

                    if (s_styleAttributes.TryGetValue(element.Name, out field))
                        style.Set(null, field.Item1, ParseValue(field.Item2, value));

                    //style.Set(null, element.Name, value);

                }
#if !DEBUG
                catch (Exception ex)
                {
                    WindowController.Instance.LogError("Error parsing style {0}, element {1}: {2}", style.Name, element.Name, ex);
                    throw;
                }
#endif
            }
        }

        internal static WidgetParameterIndex GetParameterIndexByName(string name)
        {
            Tuple<WidgetParameterIndex, Type> field;

            if (s_styleAttributes.TryGetValue(name, out field))
                return field.Item1;

            return 0;
        }

        internal static object ParseValue(Type targetType, string value)
        {
            object memberValue;

            if (targetType == typeof(Font))
                memberValue = GetFont(value); // font should be registered first to avoid confusion
            else if (targetType == typeof(WidgetStyleSheet))
                memberValue = GetStyle(value, true); // saves only reference
            else if (targetType == typeof(string))
                memberValue = (object)value;
            else if (targetType == typeof(float))
                memberValue = (object)FloatParse(value); // bo-oxing (
            else if (targetType == typeof(int))
                memberValue = (object)ColorParse(value); // assume int field is color by default
            else if (targetType == typeof(Margin))
                memberValue = (object)MarginParse(value);
            else if (targetType == typeof(Vector2))
                memberValue = (object)Vector2Parse(value);
            else if (targetType.IsEnum)
                memberValue = (object)EnumParse(targetType, value);
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
        public static int ColorParse(string value)
        {
            if (value.Length >= 7 && value[0] == '#')
                return int.Parse(value.Substring(1), System.Globalization.NumberStyles.HexNumber);
            if (value.Length >= 8 && value[0] == '0' && value[1] == 'x')
                return int.Parse(value.Substring(2), System.Globalization.NumberStyles.HexNumber);

            return int.Parse(value);
        }

        /// <summary>
        /// Parse a string to 2-component vector
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
        /// Parse a string to specific enum tpye
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
