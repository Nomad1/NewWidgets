using System.Numerics;

using NewWidgets.Utility;
using System.Diagnostics;

namespace NewWidgets.Widgets.Styles
{
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
        private Vector2 m_size;
        [WidgetStyleValue("clip")]
        private bool m_clipContents;
        [WidgetStyleValue("clip_margin")]
        private Margin m_clipMargin;

        [WidgetStyleValue("hovered_style")]
        private WidgetStyleReference m_hoveredStyle;
        [WidgetStyleValue("disabled_style")]
        private WidgetStyleReference m_disabledStyle;
        [WidgetStyleValue("selected_style")]
        private WidgetStyleReference m_selectedStyle;

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
            internal set { m_size = value; } // never used
        }

        public WidgetStyleReference HoveredStyle
        {
            get { return m_hoveredStyle; }
            internal set { m_hoveredStyle = value; CheckReadonly(); }
        }

        public WidgetStyleReference DisabledStyle
        {
            get { return m_disabledStyle; }
            internal set { m_disabledStyle = value; CheckReadonly(); }
        }

        public WidgetStyleReference SelectedStyle
        {
            get { return m_selectedStyle; }
            internal set { m_selectedStyle = value; CheckReadonly(); }
        }

        internal int InstancedFor
        {
            get { return m_instancedFor; }
            set { m_instancedFor = value; }
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

        public override string ToString()
        {
            return string.Format("Style: {0}, name {1}", GetType().Name, m_name);
        }

    }
}
