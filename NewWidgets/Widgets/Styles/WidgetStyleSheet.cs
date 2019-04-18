using System;

using System.Xml;
using System.Numerics;

using NewWidgets.UI;
using NewWidgets.Utility;
using System.Diagnostics;

namespace NewWidgets.Widgets.Styles
{
    /// <summary>
    /// Very basic style sheet for dummy widgets
    /// </summary>
    public class WidgetStyleSheet
    {
        private readonly string m_name;

        private Vector2 m_size = new Vector2(0);
        private bool m_clipContents = false;
        private Margin m_clipMargin = new Margin(0);

        private string m_hoveredStyle = "";
        private string m_disabledStyle = "";
        private string m_selectedStyle = "";

        private int m_instancedFor; // This variable contains hash code of specific object for which this instance of style sheet was created
                                    // raises assertion if zero and any setter is called
        
        public bool ClipContents
        {
            get { return m_clipContents; }
            internal set { m_clipContents = value; CheckReadonly(); }
        }

        public Margin ClipMargin
        {
            get { return m_clipMargin; }
            internal set { m_clipMargin = value; CheckReadonly(); }
        }

        public string Name
        {
            get { return m_name; }
        }

        public Vector2 Size
        {
            get { return m_size; }
        }

        public string HoveredStyle
        {
            get { return m_hoveredStyle; }
        }

        public string DisabledStyle
        {
            get { return m_disabledStyle; }
        }

        public string SelectedStyle
        {
            get { return m_selectedStyle; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetStyleSheet"/> class.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="parent">Parent.</param>
        internal WidgetStyleSheet(string name, WidgetStyleSheet parent)
        {
            m_name = string.IsNullOrEmpty(name) ? (parent.Name + "_" + GetHashCode()) : name;

            m_clipContents = parent.m_clipContents;
            m_size = parent.m_size;
            m_clipMargin = parent.m_clipMargin;

            m_disabledStyle = parent.m_disabledStyle;
            m_hoveredStyle = parent.m_hoveredStyle;
            m_selectedStyle = parent.m_selectedStyle;
        }

        internal WidgetStyleSheet Clone(object instancedFor)
        {
            int instanceHash = instancedFor.GetHashCode();

            if (instanceHash == m_instancedFor)
                return null;

            WidgetStyleSheet result = Clone();
            result.m_instancedFor = instanceHash;
            return result;
        }

        protected virtual WidgetStyleSheet Clone()
        {
            return new WidgetStyleSheet(null, this); 
        }

        protected void CheckReadonly()
        {
            Debug.Assert(m_instancedFor != 0, "Read only class");
        }

        internal void Init(XmlNode node)
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

                    ParseParameter(element.Name, value);
                }
                catch (Exception ex)
                {
                    WindowController.Instance.LogError("Error parsing style {0}, element {1}: {2}", m_name, element.Name, ex);
                    throw;
                }
            }
        }

        protected virtual bool ParseParameter(string name, string value)
        {
            switch (name)
            {
                case "size":
                    m_size = Vector2Parse(value);
                    break;
                case "clip_margin":
                    m_clipMargin = MarginParse(value);
                    break;
                case "clip":
                    m_clipContents = bool.Parse(value);
                    break;
                case "hovered_style":
                    m_hoveredStyle = value;
                    break;
                case "disabled_style":
                    m_disabledStyle = value;
                    break;
                case "selected_style":
                    m_selectedStyle = value;
                    break;
                default:
                    return false;
            }

            return true;
        }


        #region Static readers

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

        protected static T EnumParse<T>(string value) 
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        #endregion

        /* public string GetParameter(string name)
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
         }*/
    }
}
