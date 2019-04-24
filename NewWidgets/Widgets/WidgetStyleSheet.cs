#define MEMORY_PRIORITY // undefine for speed priority


using System.Numerics;

using NewWidgets.Utility;
using NewWidgets.UI;
using System.Collections.Generic;

namespace NewWidgets.Widgets
{
    internal enum WidgetParameterIndex
    {
        // Common
        None,
        [WidgetParameter("size", typeof(Vector2))]
        Size,
        [WidgetParameter("clip", typeof(bool))]
        Clip,
        [WidgetParameter("clip_margin", typeof(Margin))]
        ClipMargin,
        [WidgetParameter("hovered_style", typeof(WidgetStyleSheet))]
        HoveredStyle,
        [WidgetParameter("disabled_style", typeof(WidgetStyleSheet))]
        DisabledStyle,
        [WidgetParameter("selected_style", typeof(WidgetStyleSheet))]
        SelectedStyle,

        // Background

        [WidgetParameter("back_style", typeof(WidgetBackgroundStyle))]
        BackStyle,
        [WidgetParameter("back_depth", typeof(WidgetBackgroundDepth))]
        BackDepth,
        [WidgetParameter("back_image")]
        BackImage,
        [WidgetParameter("back_scale", typeof(float))]
        BackScale,
        [WidgetParameter("back_angle", typeof(float))]
        BackAngle,
        [WidgetParameter("back_pivot", typeof(Vector2))]
        BackPivot,
        [WidgetParameter("back_padding", typeof(Margin))]
        BackPadding,
        [WidgetParameter("back_opacity", typeof(float))]
        BackOpacity,
        [WidgetParameter("back_color", typeof(int))]
        BackColor,

        // Text

        [WidgetParameter("font", typeof(Font))]
        Font,
        [WidgetParameter("font_size", typeof(float))]
        FontSize,
        [WidgetParameter("text_color", typeof(int))]
        TextColor,
        [WidgetParameter("line_spacing", typeof(float))]
        LineSpacing,
        [WidgetParameter("text_align", typeof(WidgetAlign))]
        TextAlign,
        [WidgetParameter("text_padding", typeof(Margin))]
        [WidgetParameter("padding", typeof(Margin))]
        TextPadding,
        [WidgetParameter("richtext", typeof(bool))]
        RichText,

        // Image

        [WidgetParameter("image")]
        Image,
        [WidgetParameter("image_style", typeof(WidgetBackgroundStyle))]
        ImageStyle,
        [WidgetParameter("image_angle", typeof(float))]
        ImageAngle,
        [WidgetParameter("image_pivot", typeof(Vector2))]
        ImagePivot,
        [WidgetParameter("image_padding", typeof(Margin))]
        ImagePadding,
        [WidgetParameter("image_color", typeof(int))]
        ImageColor,
        [WidgetParameter("image_opacity", typeof(float))]
        ImageOpacity,

        // Text edit

        [WidgetParameter("cursor_color", typeof(int))]
        CursorColor,
        [WidgetParameter("cursor_char")]
        CursorChar,
        [WidgetParameter("mask_char")]
        MaskChar,


        // Button

        [WidgetParameter("button_layout", typeof(WidgetButtonLayout))]
        ButtonLayout,
        [WidgetParameter("button_text_style", typeof(WidgetStyleSheet))]
        ButtonTextStyle,
        [WidgetParameter("button_image_style", typeof(WidgetStyleSheet))]
        ButtonImageStyle,

        // Scroll view

        [WidgetParameter("horizontal_scroll", typeof(WidgetStyleSheet))]
        HorizontalScrollStyle,
        [WidgetParameter("vertical_scroll", typeof(WidgetStyleSheet))]
        VerticalcrollStyle,
        [WidgetParameter("horizontal_indicator", typeof(WidgetStyleSheet))]
        HorizontalIndicatorStyle,
        [WidgetParameter("vertical_indicator", typeof(WidgetStyleSheet))]
        VerticalIndicatorStyle,


        Max = VerticalIndicatorStyle + 1
    }

    /// <summary>
    /// Style sheet for various widget parameters
    /// </summary>
    public struct WidgetStyleSheet
    {
        private class ParameterData
        {
            public ParameterData Parent;
#if MEMORY_PRIORITY
            public readonly IDictionary<int, object> ObjectParameters;
#else
            public readonly object[] ObjectParameters; // internal storage for known indexed parameters
#endif
            public readonly IDictionary<string, string> DictionaryParameters; // external storage for custom parameters

            public ParameterData(ParameterData parent)
            {
                Parent = parent;
#if MEMORY_PRIORITY
                ObjectParameters = new Dictionary<int, object>();
#else
                ObjectParameters = new object[(int)WidgetParameterIndex.Max];
#endif
                DictionaryParameters = new Dictionary<string, string>();
            }

            public object GetParameter(WidgetParameterIndex index)
            {
                object result;
#if MEMORY_PRIORITY
                ObjectParameters.TryGetValue((int)index, out result);
#else
                result = ObjectParameters[(int)index];
#endif

                if (result == null && Parent != null)
                    result = Parent.GetParameter(index);

                return result;
            }

            public string GetParameter(string name)
            {
                string result;

                if (!DictionaryParameters.TryGetValue(name, out result))
                {
                    if (Parent != null)
                        result = Parent.GetParameter(name);
                }

                return result;
            }
        }

        private string m_name;
        private int m_instancedFor; // This variable contains hash code of specific object for which this instance of style sheet was created
                                    // raises assertion if zero and any setter is called

        private ParameterData m_data;
        // Internal properties

        public string Name
        {
            get { return m_name; }
            internal set { m_name = value; }
        }

        public bool IsEmpty
        {
            get { return m_data == null; }
        }

        internal int InstancedFor
        {
            get { return m_instancedFor; }
            set { m_instancedFor = value; }
        }

        internal WidgetStyleSheet(string name, WidgetStyleSheet parent)
        {
            m_name = name;
            m_data = new ParameterData(parent.m_data);
            m_instancedFor = 0;
        }

        /// <summary>
        /// This method is needed to repair hierarchy that could be broken because of premature load
        /// </summary>
        /// <param name="parent">Parent.</param>
        internal void SetParent(WidgetStyleSheet parent)
        {
            m_data.Parent = parent.m_data;
        }

        private bool MakeWritable(object instance)
        {
            if (instance == null)
                return false;

            int instanceHash = instance.GetHashCode();

            if (instanceHash == m_instancedFor)
                return true;

            m_instancedFor = instanceHash;
            m_name = m_name + "_" + m_instancedFor;
            m_data = new ParameterData(m_data);

            return true;
        }

        public override string ToString()
        {
            return string.Format("Style: {0}, instanced {1}", m_name, m_instancedFor != 0);
        }

        internal T Get<T>(WidgetParameterIndex index, T defaultValue)
        {
            object result = m_data.GetParameter(index);

            if (result == null)
                return defaultValue;

            return (T)result;
        }

        internal void Set(object instance, WidgetParameterIndex index, object value)
        {
            if (instance != null)
                MakeWritable(instance);

            m_data.ObjectParameters[(int)index] = value;
        }

        /// <summary>
        /// Retrieve parameter by name
        /// </summary>
        /// <returns>The parameter.</returns>
        /// <param name="name">Name.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public T Get<T>(string name, T defaultValue)
        {
            object result = m_data.GetParameter(name);

            if (result == null)
                return defaultValue;

            return (T)result;
        }

        /// <summary>
        /// Set the specified parameter by name
        /// </summary>
        /// <param name="instance">Instance.</param>
        /// <param name="name">Name.</param>
        /// <param name="value">Value.</param>
        public void Set(object instance, string name, string value)
        {
            if (instance != null)
                MakeWritable(instance);

            m_data.DictionaryParameters[name] = value;
        }
    }
}
