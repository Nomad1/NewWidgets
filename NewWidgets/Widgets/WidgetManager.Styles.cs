using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Xml;
using NewWidgets.UI;
using NewWidgets.Utility;
using NewWidgets.Widgets.Styles;

namespace NewWidgets.Widgets
{
    public class WidgetStyleReference
    {
        private WidgetStyleSheet m_style;
        private readonly string m_name;

        public WidgetStyleSheet Style
        {
            get
            {
                if (m_style == null)
                    m_style = WidgetManager.GetStyle(m_name);

                return m_style;
            }
        }

        public WidgetStyleReference(string name)
        {
            m_name = name;
        }
    }

    public class WidgetStyleReference<T> : WidgetStyleReference where T : WidgetStyleSheet
    {
        public WidgetStyleReference(string name)
            : base(name)
        {

        }

        public static implicit operator T(WidgetStyleReference<T> reference)
        {
            return reference.Style as T;
        }
    }

    public static partial class WidgetManager
    {
        // styles
        private static readonly IDictionary<string, WidgetStyleSheet> s_styles = new Dictionary<string, WidgetStyleSheet>();
        private static readonly IDictionary<Type, IDictionary<string, FieldInfo>> s_styleAttributes = new Dictionary<Type, IDictionary<string, FieldInfo>>();


        // Those guys are obsolete and should be replaced by direct Widget.DefaultStyle like calls

        [Obsolete]
        public static WidgetStyleSheet DefaultWidgetStyle { get { return Widget.DefaultStyle; } }
        [Obsolete]
        public static WidgetButtonStyleSheet DefaultCheckBoxStyle { get { return WidgetCheckBox.DefaultStyle; } }
        [Obsolete]
        public static WidgetBackgroundStyleSheet DefaultButtonStyle { get { return WidgetBackground.DefaultStyle; } }
        [Obsolete]
        public static WidgetButtonStyleSheet DefaultBackgroundStyle { get { return WidgetButton.DefaultStyle; } }
        [Obsolete]
        public static WidgetBackgroundStyleSheet DefaultPanelStyle { get { return WidgetPanel.DefaultStyle; } }
        [Obsolete]
        public static WidgetTextEditStyleSheet DefaultTextEditStyle { get { return WidgetTextEdit.DefaultStyle; } }
        [Obsolete]
        public static WidgetTextStyleSheet DefaultLabelStyle { get { return WidgetLabel.DefaultStyle; } }
        [Obsolete]
        public static WidgetBackgroundStyleSheet DefaultWindowStyle { get { return WidgetWindow.DefaultStyle; } }
        [Obsolete]
        public static WidgetImageStyleSheet DefaultImageStyle { get { return WidgetImage.DefaultStyle; } }

        /// <summary>
        /// Gets the style by name
        /// </summary>
        /// <returns>The style.</returns>
        /// <param name="name">Name.</param>
        public static WidgetStyleSheet GetStyle(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                WidgetStyleSheet result;
                if (s_styles.TryGetValue(name, out result))
                    return result;
            }

            WindowController.Instance.LogError("WidgetManager got GetStyle request for not existing style {0}", name);

            return null;  // TODO: return default style to avoid crash?
        }

        /// <summary>
        /// Constructs the style of specified type and with specified parent. NOTE: Uses Reflection
        /// </summary>
        /// <returns>The style.</returns>
        /// <param name="name">Name.</param>
        /// <param name="type">Type.</param>
        /// <param name="parent">Parent.</param>
        internal static WidgetStyleSheet CreateStyle(string name, Type type, WidgetStyleSheet parent)
        {
            ConstructorInfo info = type.GetConstructor(new Type[0]);

            if (info == null)
                throw new ApplicationException(string.Format("Style class {0} doesn't have appropriate constructpr for CreateStyle", type.Name));

            //style = (WidgetStyleSheet)Activator.CreateInstance(type, new object[] { name }); // TODO: it will crash if there is no such constructor. Use ConstructorInfo

            WidgetStyleSheet result = (WidgetStyleSheet)info.Invoke(new object[0]);
            result.Name = name;
            DeepCopy(parent, result);

            return result;
        }

        private static void RegisterStyle(XmlNode node)
        {
            string name = node.Attributes.GetNamedItem("name").Value;

            string @class = node.Attributes.GetNamedItem("class").Value;

            WidgetStyleSheet parent = node.Attributes.GetNamedItem("parent") == null ? null : GetStyle(node.Attributes.GetNamedItem("parent").Value);

            if (parent == null)
                parent = new WidgetStyleSheet();

            WidgetStyleSheet style = null;

            if (!string.IsNullOrEmpty(@class))
            {
                switch (@class.ToLower())
                {
                    case "image":
                        style = CreateStyle(name, typeof(WidgetImageStyleSheet), parent);
                        break;
                    case "background":
                    case "panel":
                    case "window":
                        style = CreateStyle(name, typeof(WidgetBackgroundStyleSheet), parent);
                        break;
                    case "label":
                    case "text":
                        style = CreateStyle(name, typeof(WidgetTextStyleSheet), parent);
                        break;
                    case "textedit":
                        style = CreateStyle(name, typeof(WidgetTextEditStyleSheet), parent);
                        break;
                    case "button":
                    case "checkbox":
                        style = CreateStyle(name, typeof(WidgetButtonStyleSheet), parent);
                        break;
                    default:
                        Type type = Type.GetType(@class);

                        if (type == null)
                            type = Type.GetType(typeof(WidgetStyleSheet).Namespace + "." + @class);

                        if (type == null)
                            type = Type.GetType(typeof(WidgetStyleSheet).Namespace + ".Widget" + @class + "StyleSheet");

                        if (type == null)
                            WindowController.Instance.LogError("Class {0} not found for style {1}", @class, name);

                        style = CreateStyle(name, type, parent);
                        break;

                }
            }

            if (style == null)
                style = CreateStyle(name, typeof(WidgetStyleSheet), parent);

            s_styles[name] = style;

            WindowController.Instance.LogMessage("Registered style {0}", name);
        }

        private static IDictionary<string, FieldInfo> InitStyleMap(WidgetStyleSheet style)
        {
            Type type = style.GetType();

            IDictionary<string, FieldInfo> styleMap;

            if (!s_styleAttributes.TryGetValue(type, out styleMap))
            {
                styleMap = new Dictionary<string, FieldInfo>();

                FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

                foreach (FieldInfo field in fields)
                    foreach (WidgetStyleValueAttribute attribute in field.GetCustomAttributes<WidgetStyleValueAttribute>())
                    {
                        styleMap[attribute.Name] = field;
                    }

                s_styleAttributes[type] = styleMap;
            }

            return styleMap;
        }

        private static void InitStyle(WidgetStyleSheet style, XmlNode node)
        {
            IDictionary<string, FieldInfo> styleMap = InitStyleMap(style);

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

                    FieldInfo field;
                    if (styleMap.TryGetValue(value, out field))
                        InitField(style, field, value);
                }
                catch (Exception ex)
                {
                    WindowController.Instance.LogError("Error parsing style {0}, element {1}: {2}", style.Name, element.Name, ex);
                    throw;
                }
            }
        }

        private static void InitField(WidgetStyleSheet style, FieldInfo field, string value)
        {
            Type fieldType = field.FieldType;

            object fieldValue;

            if (fieldType == typeof(Font))
                fieldValue = GetFont(value); // font should be registered first to avoid confusion
            else if (fieldType == typeof(WidgetStyleReference))
                fieldValue = new WidgetStyleReference(value); // saves only reference
            else if (fieldType == typeof(string))
                fieldValue = (object)value;
            else if (fieldType == typeof(float))
                fieldValue = (object)FloatParse(value); // bo-oxing (
            else if (fieldType == typeof(int))
                fieldValue = (object)ColorParse(value); // assume int field is color by default
            else if (fieldType == typeof(Margin))
                fieldValue = (object)MarginParse(value);
            else if (fieldType == typeof(Vector2))
                fieldValue = (object)Vector2Parse(value);
            else if (fieldType.IsEnum)
                fieldValue = (object)EnumParse(fieldType, value);
            else
                fieldValue = Convert.ChangeType(value, fieldType); // fallback, may crash

            field.SetValue(style, fieldValue);
        }

        private static void DeepCopy(WidgetStyleSheet styleFrom, WidgetStyleSheet styleTo)
        {
            IDictionary<string, FieldInfo> styleMapFrom = InitStyleMap(styleFrom);
            IDictionary<string, FieldInfo> styleMapTo = InitStyleMap(styleTo);

            foreach (var pair in styleMapTo)
            {
                FieldInfo fieldFrom;

                if (!styleMapFrom.TryGetValue(pair.Key, out fieldFrom))
                    continue;

                pair.Value.SetValue(styleTo, fieldFrom.GetValue(styleFrom));
            }
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

            if (value.Length != 2)
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

            if (value.Length != 1 && value.Length != 4)
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
            return (T)Enum.Parse(typeof(T), value, true);
        }

        /// <summary>
        /// Parse a string to specific enum tpye
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="value">Value.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static Enum EnumParse(Type enumType, string value)
        {
            return (Enum)Enum.Parse(enumType, value, true);
        }

        #endregion
    }
}
