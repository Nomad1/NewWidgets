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

    [AttributeUsage(AttributeTargets.Field)]
    public class WidgetStyleValueAttribute : Attribute
    {
        private readonly string m_name;

        public string Name
        {
            get { return m_name; }
        }

        public WidgetStyleValueAttribute(string name)
        {
            m_name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class WidgetNestedStyleAttribute : Attribute
    {
        private readonly string m_preffix = "";

        public string Preffix
        {
            get { return m_preffix; }
        }
    }

    internal class WidgetMemberInfo
    {
        private readonly WidgetMemberInfo m_parent;
        private readonly FieldInfo m_field;

        public Type Type
        {
            get { return m_field.FieldType; }
        }

        public WidgetMemberInfo(WidgetMemberInfo parent, FieldInfo field)
        {
            m_parent = parent;
            m_field = field;
        }

        public WidgetMemberInfo(FieldInfo field)
        {
            m_parent = null;
            m_field = field;
        }

        public object GetValue(object obj)
        {
            if (m_parent != null)
                obj = m_parent.GetValue(obj);

            return m_field.GetValue(obj);
        }

        public void SetValue(object obj, object value)
        {
            if (m_parent != null)
                obj = m_parent.GetValue(obj);

            m_field.SetValue(obj, value);
        }
    }


    /// <summary>
    /// Reference class that allows lazy loading of styles.
    /// </summary>
    public struct WidgetStyleReference
    {
        private WidgetStyleSheet m_style;
        private readonly string m_name;

        private WidgetStyleSheet Style
        {
            get
            {
                if (m_style == null)
                    m_style = WidgetManager.InternalGetStyle(m_name);

                return m_style;
            }
        }

        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(m_name); }
        }

        public bool IsValid
        {
            get { return !IsEmpty && Style != null; }
        }

        public bool IsWritable
        {
            get { return IsValid && Style.InstancedFor != 0; }
        }

        public Type Type
        {
            get { return IsValid ? Style.GetType() : null; }
        }

        public WidgetStyleReference(string name)
        {
            m_name = name;
            m_style = null;
        }

        internal WidgetStyleReference(string name, WidgetStyleSheet style)
        {
            m_name = name;
            m_style = style;
        }

        public override string ToString()
        {
            return string.Format("Style reference: {0}, style: {1}", m_name, m_style == null ? "(not resolved)" : m_style.ToString());
        }

        public T Get<T>(object instancedFor = null) where T : WidgetStyleSheet
        {
            WidgetStyleSheet result = Style;

            if (result == null)
                throw new WidgetException("Broken style!");

            if (instancedFor != null)
            {
                int instanceHash = instancedFor.GetHashCode();

                if (result.InstancedFor != instanceHash)
                {
                    result = WidgetManager.CreateStyle(m_name + "_" + instanceHash, result.GetType(), result);
                    result.InstancedFor = instanceHash;

                    m_style = result; // replacing cached style with clone
                }
            }

            if (!(result is T))
                throw new WidgetException("Asked to convert style to incompatible type!");

            return (T)result;
        }
    }

    #endregion

    public static partial class WidgetManager
    {
        public static WidgetStyleReference RegisterDefaultStyle<T>(string name) where T : WidgetStyleSheet, new()
        {
            WidgetStyleReference result = new WidgetStyleReference(name);

            if (!s_styles.ContainsKey(name))
            {
                WidgetStyleSheet defaultStyle = new T();
                defaultStyle.Name = name;
                s_styles[name] = defaultStyle;
            }

            return result;
        }

        // styles
        private static readonly IDictionary<string, WidgetStyleSheet> s_styles = new Dictionary<string, WidgetStyleSheet>();
        private static readonly IDictionary<Type, IDictionary<string, WidgetMemberInfo>> s_styleAttributes = new Dictionary<Type, IDictionary<string, WidgetMemberInfo>>();


        // Those guys are obsolete and should be replaced by direct Widget.DefaultStyle like calls

        [Obsolete("Use Widget.DefaultStyle instead")]
        public static WidgetStyleReference DefaultWidgetStyle { get { return Widget.DefaultStyle; } }
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
        public static WidgetStyleReference GetStyle(string name)
        {
            WidgetStyleSheet style = InternalGetStyle(name);
            if (style != null)
                return new WidgetStyleReference(name, style);

            throw new WidgetException(string.Format("Style {0} not found for GetStyle!", name));
        }

        internal static WidgetStyleSheet InternalGetStyle(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                WidgetStyleSheet result;
                if (s_styles.TryGetValue(name, out result))
                    return result;
            }

            WindowController.Instance.LogError("WidgetManager got GetStyle request for not existing style {0}", name);

            return null;
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
                throw new ApplicationException(string.Format("Style class {0} doesn't have appropriate constructor for CreateStyle", type.Name));

            //style = (WidgetStyleSheet)Activator.CreateInstance(type, new object[] { name }); // TODO: it will crash if there is no such constructor. Use ConstructorInfo

            WidgetStyleSheet result = (WidgetStyleSheet)info.Invoke(new object[0]);
            result.Name = name;
            DeepCopy(parent, result);

            return result;
        }

        private static void RegisterStyle(XmlNode node)
        {
            string name = GetAttribute(node, "name");

            string @class = GetAttribute(node, "class");

            string parent = GetAttribute(node, "parent");

            // NOTE: if there is no @class and no parent, style will have default WidgetStyleSheet
            // that wouldn't work for most widgets!

            WidgetStyleSheet parentStyle = parent == null ? null : InternalGetStyle(parent);

            if (parentStyle == null)
                parentStyle = new WidgetStyleSheet();

            WidgetStyleSheet style = null;

            if (!string.IsNullOrEmpty(@class))
            {
                switch (@class.ToLower())
                {
                    case "image":
                        style = CreateStyle(name, typeof(WidgetImageStyleSheet), parentStyle);
                        break;
                    case "background":
                    case "panel":
                    case "window":
                        style = CreateStyle(name, typeof(WidgetBackgroundStyleSheet), parentStyle);
                        break;
                    case "label":
                    case "text":
                        style = CreateStyle(name, typeof(WidgetTextStyleSheet), parentStyle);
                        break;
                    case "textedit":
                        style = CreateStyle(name, typeof(WidgetTextEditStyleSheet), parentStyle);
                        break;
                    case "button":
                        style = CreateStyle(name, typeof(WidgetButtonStyleSheet), parentStyle);
                        break;
                    case "checkbox":
                        style = CreateStyle(name, typeof(WidgetCheckBoxStyleSheet), parentStyle);
                        break;
                    default:
                        Type type = Type.GetType(@class);

                        if (type == null)
                            type = Type.GetType(typeof(WidgetStyleSheet).Namespace + "." + @class);

                        if (type == null)
                            type = Type.GetType(typeof(WidgetStyleSheet).Namespace + ".Widget" + @class + "StyleSheet");

                        if (type == null)
                            WindowController.Instance.LogError("Class {0} not found for style {1}", @class, name);

                        style = CreateStyle(name, type, parentStyle);
                        break;

                }
            }

            if (style == null)
                style = CreateStyle(name, parentStyle.GetType(), parentStyle);

            InitStyle(style, node);

            s_styles[name] = style;

            WindowController.Instance.LogMessage("Registered style {0}", name);
        }

        private static IDictionary<string, WidgetMemberInfo> InitStyleMap(WidgetStyleSheet style)
        {
            Type type = style.GetType();

            IDictionary<string, WidgetMemberInfo> styleMap;

            if (!s_styleAttributes.TryGetValue(type, out styleMap))
            {
                styleMap = new Dictionary<string, WidgetMemberInfo>();

                s_styleAttributes[type] = styleMap;

                InitStyleMapType(type, styleMap, null);
            }

            return styleMap;
        }

        private static void InitStyleMapType(Type type, IDictionary<string, WidgetMemberInfo> styleMap, WidgetMemberInfo parent)
        {
            while (type != null)
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (FieldInfo field in fields)
                {
                    foreach (WidgetStyleValueAttribute attribute in field.GetCustomAttributes(typeof(WidgetStyleValueAttribute), true))
                        {
                            styleMap[attribute.Name] = new WidgetMemberInfo(parent, field);
                        }

                    foreach (WidgetNestedStyleAttribute attribute in field.GetCustomAttributes(typeof(WidgetNestedStyleAttribute), true))
                    {
                        InitStyleMapType(field.FieldType, styleMap, new WidgetMemberInfo(parent, field)); // recursion for nested types!
                    }
                }

                // process base type
                type = type.BaseType;
            }
        }

        private static void InitStyle(WidgetStyleSheet style, XmlNode node)
        {
            IDictionary<string, WidgetMemberInfo> styleMap = InitStyleMap(style);

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

                    WidgetMemberInfo field;
                    if (styleMap.TryGetValue(element.Name, out field))
                        InitField(style, field, value);
                    else
                        WindowController.Instance.LogMessage("Invalid field {0} in style for {1}", element.Name, style.Name);
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

        private static void InitField(WidgetStyleSheet style, WidgetMemberInfo member, string value)
        {
            Type memberType = member.Type;

            object memberValue;

            if (memberType == typeof(Font))
                memberValue = GetFont(value); // font should be registered first to avoid confusion
            else if (memberType == typeof(WidgetStyleReference))
                memberValue = new WidgetStyleReference(value); // saves only reference
            else if (memberType == typeof(string))
                memberValue = (object)value;
            else if (memberType == typeof(float))
                memberValue = (object)FloatParse(value); // bo-oxing (
            else if (memberType == typeof(int))
                memberValue = (object)ColorParse(value); // assume int field is color by default
            else if (memberType == typeof(Margin))
                memberValue = (object)MarginParse(value);
            else if (memberType == typeof(Vector2))
                memberValue = (object)Vector2Parse(value);
            else if (memberType.IsEnum)
                memberValue = (object)EnumParse(memberType, value);
            else
                memberValue = Convert.ChangeType(value, memberType); // fallback, may crash

            member.SetValue(style, memberValue);
        }

        private static void DeepCopy(WidgetStyleSheet styleFrom, WidgetStyleSheet styleTo)
        {
            IDictionary<string, WidgetMemberInfo> styleMapFrom = InitStyleMap(styleFrom);
            IDictionary<string, WidgetMemberInfo> styleMapTo = InitStyleMap(styleTo);

            foreach (var pair in styleMapTo)
            {
                WidgetMemberInfo memberFrom;

                if (!styleMapFrom.TryGetValue(pair.Key, out memberFrom))
                    continue;

                DeepCopyMember(styleFrom, memberFrom, styleTo, pair.Value);
            }
        }

        private static void DeepCopyMember(object from, WidgetMemberInfo memberFrom, object to, WidgetMemberInfo memberTo)
        {
            object memberValue = memberFrom.GetValue(from);

            memberTo.SetValue(to, memberValue);
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

        public static string GetAttribute(XmlNode node, string name)
        {
            var attribute = node.Attributes.GetNamedItem(name);

            return attribute == null ? null : attribute.Value;
        }

#endregion
    }
}
