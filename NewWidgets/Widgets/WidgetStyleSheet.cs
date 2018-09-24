using System;

using System.Collections.Generic;
using System.Xml;
using System.Numerics;

using NewWidgets.UI;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    public class WidgetStyleSheet
    {
        private readonly WidgetBackgroundStyle m_backgroundStyle;
        private readonly WidgetBackgroundDepth m_backgroundDepth;
        private readonly string m_backgroundTexture;
        private readonly float m_backgroundScale;
        private readonly Vector2 m_backgroundPivot;
        private readonly Margin m_backgroundPadding;

        private readonly Font m_font;
        private readonly float m_fontSize;
        
        private readonly int m_color;
        
        private readonly float m_opacity;

        private readonly Vector2 m_size;

        private readonly Dictionary<string, string> m_parameters;

        private readonly string m_name;

        private readonly bool m_clip;

        private readonly Margin m_clipMargin;

        private readonly Margin m_padding;
        
        public string Name
        {
            get { return m_name; }
        }

        public WidgetBackgroundStyle BackgroundStyle
        {
            get { return m_backgroundStyle; }
        }
        
        public WidgetBackgroundDepth BackgroundDepth
        {
            get { return m_backgroundDepth; }
        }

        public Margin BackgroundPadding
        {
            get { return m_backgroundPadding; }
        }

        public string BackgroundTexture
        {
            get { return m_backgroundTexture; }
        }

        public Vector2 BackgroundPivot
        {
            get { return m_backgroundPivot; }
        }
        
        public float BackgroundScale
        {
            get { return /*WidgetManager.UIScale * */m_backgroundScale; }
        }
        
        public float FontSize
        {
            get { return WidgetManager.FontScale * m_fontSize; }
        }

        public float Opacity
        {
            get { return m_opacity; }
        }

        public Font Font
        {
            get { return m_font; }
        }

        public int Color
        {
            get { return m_color; }
        }
        
        public Vector2 Size
        {
            get { return m_size/* * WidgetManager.UIScale*/; }
        }
        
        public bool Clip
        {
            get { return m_clip; }
        }
        
        public Margin ClipMargin
        {
            get { return m_clipMargin; }
        }

        public Margin Padding
        {
            get { return m_padding; }
        }

        internal WidgetStyleSheet()
        {
            m_clip = false;
            m_size = new Vector2(0, 0);
            m_name = "Default";
            m_parameters = new Dictionary<string, string>();
            m_color = 0xffffff;
            m_backgroundPivot = new Vector2(0.5f, 0.5f);
            m_opacity = 1.0f;
            m_clipMargin = new Margin(2);
            m_backgroundPadding = new Margin(0);
            m_font = WidgetManager.MainFont;
            m_fontSize = 1.0f;
            m_backgroundScale = 1.0f;
            m_backgroundStyle = WidgetBackgroundStyle.None;
            m_backgroundTexture = "";
        }

        internal WidgetStyleSheet(WidgetStyleSheet parent)
        {
            m_clip = parent.Clip;
            m_size = parent.Size;
            m_name = parent.Name + "_" + GetHashCode();
            m_parameters = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> pair in parent.m_parameters)
                m_parameters.Add(pair.Key, pair.Value);
                
            m_color = parent.Color;
            m_opacity = parent.Opacity;
            m_clipMargin = parent.ClipMargin;
            m_font = parent.Font;
            m_fontSize = parent.FontSize;
            m_backgroundScale = parent.BackgroundScale;
            m_backgroundDepth = parent.BackgroundDepth;
            m_backgroundStyle = parent.BackgroundStyle;
            m_backgroundTexture = parent.BackgroundTexture;
            m_backgroundPivot = parent.BackgroundPivot;
            m_backgroundPadding = parent.m_backgroundPadding;
            m_padding = parent.Padding;
        }

        internal WidgetStyleSheet(string name, WidgetStyleSheet parent, XmlNode node)
            : this(parent)
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
                    
                    switch (element.Name)
                    {
                    case "back_style":
                        m_backgroundStyle = (WidgetBackgroundStyle)Enum.Parse(typeof(WidgetBackgroundStyle), value);
                        break;
                    case "back_depth":
                        m_backgroundDepth = (WidgetBackgroundDepth)Enum.Parse(typeof(WidgetBackgroundDepth), value);
                        break;
                    case "back_image":
                        m_backgroundTexture = value;
                        break;
                    case "back_scale":
                        m_backgroundScale = FloatParse(value);
                        break;
                    case "back_pivot":
                        {
                            string[] values = value.Split(';');
                            float x = FloatParse(values[0]);
                            float y = FloatParse(values[1]);
                            m_backgroundPivot = new Vector2(x, y);
                            break;
                        }
                        case "font":
                        m_font = WidgetManager.GetFont(value);
                        break;
                    case "font_size":
                        m_fontSize = FloatParse(value);
                        break;
                    case "clip":
                        m_clip = bool.Parse(value);
                        break;
                    case "color":
                        m_color = ColorParse(value);
                        break;
                    case "opacity":
                        m_opacity = FloatParse(value);
                        break;
                    case "size":
                        m_size = Vector2Parse(value);
                        break;
                    case "clip_margin":
                        m_clipMargin = MarginParse(value);
                        break;
                    case "padding":
                        m_padding = MarginParse(value);
                        break;
                    case "back_padding":
                        m_backgroundPadding = MarginParse(value);
                        break;
                    default:
                        m_parameters[element.Name] = value;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    WindowController.Instance.LogError("Error parsing style {0}, element {1}: {2}", name, element.Name, ex);
                    throw;
                }
            }
        }

        protected static float FloatParse(string value)
        {
            return float.Parse(value.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
        }

        protected static int ColorParse(string value)
        {
            if (value.Length >= 7 && value[0] == '#')
                return int.Parse(value.Substring(1), System.Globalization.NumberStyles.HexNumber);
            if (value.Length >= 8 && value[0] == '0' && value[1] == 'x')
                return int.Parse(value.Substring(2), System.Globalization.NumberStyles.HexNumber);

            return int.Parse(value);
        }

        protected static Vector2 Vector2Parse(string value)
        {
            string[] values = value.Split(';');
            float x = FloatParse(values[0]);
            float y = FloatParse(values[1]);
            return new Vector2(x, y);
        }

        protected static Margin MarginParse(string value)
        {
            string[] values = value.Split(';');
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

        public string GetParameter(string name)
        {
            if (m_parameters != null)
            {
                string result;
                if (m_parameters.TryGetValue(name, out result))
                    return result;
            }
            return null;
        }

        public T GetParameter<T>(string name)
        {
            if (m_parameters != null)
            {
                string result;
                if (m_parameters.TryGetValue(name, out result))
                {
                    if (typeof(T) == typeof(Single))
                        return (T)(object)FloatParse(result); // bo-oxing (
                    else if (typeof(T) == typeof(Margin))
                        return (T)(object)MarginParse(result);
                    else if (typeof(T) == typeof(Vector2))
                        return (T)(object)Vector2Parse(result);
                    
                    return (T)Convert.ChangeType(result, typeof(T));
                }
            }
            return default(T);
        }
        
        public int GetParameterColor(string name, int def)
        {
            if (m_parameters != null)
            {
                string result;
                if (m_parameters.TryGetValue(name, out result))
                {
                    return ColorParse(result);
                }
            }
            return def;
        }
    }
    
}
