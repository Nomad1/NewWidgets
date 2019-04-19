using System;

using System.Numerics;

using NewWidgets.Utility;
using System.Diagnostics;

namespace NewWidgets.Widgets.Styles
{
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

    /// <summary>
    /// Very basic style sheet for dummy widgets
    /// </summary>
    public class WidgetStyleSheet
    {
        private string m_name;
        private int m_instancedFor; // This variable contains hash code of specific object for which this instance of style sheet was created
                                    // raises assertion if zero and any setter is called

        // style fields

        [WidgetStyleValue("size")]
        private Vector2 m_size = new Vector2(0);
        [WidgetStyleValue("clip")]
        private bool m_clipContents = false;
        [WidgetStyleValue("clip_margin")]
        private Margin m_clipMargin = new Margin(0);

        [WidgetStyleValue("hovered_style")]
        private string m_hoveredStyle = "default";
        [WidgetStyleValue("disabled_style")]
        private string m_disabledStyle = "default";
        [WidgetStyleValue("selected_style")]
        private string m_selectedStyle = "default";

        // Internal properties

        public string Name
        {
            get { return m_name; }
            internal set { m_name = value; }
        }

        // Style properties

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
        /// Clone the specified style for use in <paramref name="instancedFor"/>
        /// </summary>
        /// <returns>The clone.</returns>
        /// <param name="instancedFor">Instanced for.</param>
        public WidgetStyleSheet Clone(object instancedFor)
        {
            int instanceHash = instancedFor.GetHashCode();

            if (instanceHash == m_instancedFor)
                return null;

            WidgetStyleSheet result = WidgetManager.CreateStyle(this.Name + "_" + instanceHash, this.GetType(), this);
            result.m_instancedFor = instanceHash;

            return result;
        }

        protected void CheckReadonly()
        {
            Debug.Assert(m_instancedFor != 0, "Read only class");
        }

    }
}
