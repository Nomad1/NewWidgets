using System;
using System.Collections.Generic;
using System.Text;
using NewWidgets.UI.Styles;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
   

    /// <summary>
    /// This class is a simple wrapper for IDictionary with LoadData working as AddRange
    /// </summary>
    internal class StyleSheetData : IStyleData, ISelfNamedStyleData
    {
        private readonly IDictionary<WidgetParameterIndex, object> m_parameters;

        /// <summary>
        /// The font family this block declares. It is what tells one <c>@font-face</c> node
        /// from another, since every face spells the same header; <c>StyleCollection</c> asks
        /// only for at-rules, so an ordinary rule declaring <c>font-family</c> is unaffected.
        /// </summary>
        public string StyleDataName
        {
            get
            {
                object font;

                if (!m_parameters.TryGetValue(WidgetParameterIndex.Font, out font))
                    return string.Empty;

                return font.ToString();
            }
        }

        public StyleSheetData()
        {
            m_parameters = new Dictionary<WidgetParameterIndex, object>();
        }

        public StyleSheetData(IDictionary<WidgetParameterIndex, object> parameters)
        {
            m_parameters = parameters;
        }

        public bool TryGetParameter(WidgetParameterIndex index, out object result)
        {
            return m_parameters.TryGetValue(index, out result);
        }

        public T GetParameter<T>(WidgetParameterIndex index, T defaultValue)
        {
            object result;
            if (!m_parameters.TryGetValue(index, out result) || result.GetType() != typeof(T))
                return defaultValue;

            return (T)result;
        }

        public void SetParameter(WidgetParameterIndex index, object value)
        {
            m_parameters[index] = value;
        }

        void IStyleData.LoadData(IStyleData data)
        {
            StyleSheetData styleData = data as StyleSheetData;

            if (styleData == null)
                throw new ArgumentException("Invalid data providede to LoadData");

            foreach (var pair in styleData.m_parameters)
                m_parameters[pair.Key] = pair.Value;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            foreach (var pair in m_parameters)
            {
                var attr = WidgetParameterMap.GetAttributeByIndex(pair.Key);

                // The inverse of TextAlignProcessor's special case (Widgets/WidgetParameterIndex.cs):
                // that parser turns the CSS keyword "center" into WidgetAlign.HorizontalCenter alone,
                // since "center" means only the inline axis. Left to the generic formatter below,
                // HorizontalCenter would come back out as "horizontalcenter", which is not a CSS
                // keyword -- a browser ignores it and an external editor would likely flag or drop
                // it, defeating the one point of a shared, browser-readable stylesheet.
                //
                // Same shape, second case -- D233. WidgetBackgroundStyle.NoRepeat is declared as a
                // synonym of Image (same underlying value 1) so CSS's "no-repeat" round-trips.
                // The generic formatter below is a bare value.ToString().ToLower(), which for a
                // value two members share returns whichever was declared first in the enum -- Image,
                // since it precedes the synonym -- so it would write back the invalid keyword
                // "image" a browser drops, undoing the fix at every save. "no-repeat" is the only
                // CSS spelling for this value; write it explicitly rather than let reflection guess.
                //
                // Third case, same shape again -- D233. BackImage's "no image" value is an empty
                // string (BackgroundImageProcessor stores string.Empty for `none`). The generic
                // formatter below wraps a UnitType.Url string in url("...") unconditionally, so an
                // empty BackImage would round-trip as url("") -- a browser resolves that against
                // the stylesheet's own address and tries to load the CSS file as an image. `none`
                // is the standard's own spelling for "no background at all"; write it explicitly.
                string formatted = pair.Key == WidgetParameterIndex.TextAlign && WidgetAlign.HorizontalCenter.Equals(pair.Value)
                    ? "center"
                    : pair.Key == WidgetParameterIndex.BackStyle && WidgetBackgroundStyle.Image.Equals(pair.Value)
                    ? "no-repeat"
                    : pair.Key == WidgetParameterIndex.BackImage && string.Empty.Equals(pair.Value)
                    ? "none"
                    : ConversionHelper.FormatValue(pair.Value.GetType(), attr.UnitType, pair.Value);

                builder.AppendFormat("    {0}: {1};\n", attr.Name, formatted);
            }

            return builder.ToString();
        }
    }

    /// <summary>
    /// This one is a simple wrapper to a collection of StyleSheetData structures for particular widget.
    /// In case of local modifications it also adds a new Data object to the collection to store local changes
    /// </summary>
    internal struct WidgetStyleSheet
    {
        private bool m_hasOwnStyle; // This flag indicates that personal style has been created for the object

        private readonly LinkedList<StyleNodeMatchPair> m_data;
        private readonly string m_name;

        // Internal properties

        public bool IsEmpty
        {
            get { return m_data == null; }
        }

        public string Name
        {
            get { return m_name; }
        }

        internal WidgetStyleSheet(string name, ICollection<StyleNodeMatchPair> data)
        {
            m_name = name;

            m_hasOwnStyle = false;

            m_data = new LinkedList<StyleNodeMatchPair>();

            if (data != null)
                foreach (StyleNodeMatchPair sheetData in data)
                    m_data.AddFirst(sheetData);
        }

        internal void SetOwnStyle(StyleSheetData ownStyle)
        {
            if (m_hasOwnStyle)
                throw new WidgetException("Trying to set own style when it is already set!");

            m_data.AddFirst(new StyleNodeMatchPair(new StyleNode(new StyleSelectorList(new StyleSelector("", null, "")), ownStyle), StyleNodeMatch.OwnStyle)); // local style, the same as HTML tag style="..."

            m_hasOwnStyle = true;
        }

        /// <summary>
        /// Whether the property was DECLARED anywhere this widget's cascade reaches, and its
        /// value if so. A defaulted read cannot answer that: `position: static` written by an
        /// author and nothing written at all both come back as Static, and only one of them may
        /// be treated as "the game code decides".
        /// </summary>
        internal bool TryGetValue<T>(WidgetParameterIndex index, out T tresult)
        {
            WidgetParameterAttribute attr = WidgetParameterMap.GetAttributeByIndex(index);

            bool inherited = attr != null && attr.Inheritance == WidgetParameterInheritance.Inherit; // should be read fromParent and GrandParent nodes

            object result = null;

            LinkedListNode<StyleNodeMatchPair> node = m_data.First;

            // Let's iterate all the styles and select properties based on inheritance

            while (node != null)
            {
                StyleNode data = node.Value.Node;

                if (((StyleSheetData)data.Data).TryGetParameter(index, out result))
                {
                    break;
                }

                if (node.Next == null)
                    break;

                // if next style is the same as this one less on pseudo-class, we can think of it as a parent and do a one-time exception for data lookup
                // otherwise we need to check if the property inheritance is Initial and then break

                StyleNodeMatch nextNodeType = node.Next.Value.Match;

                // Pseudo classes should inherit data from their parent classes
                if (!inherited && (nextNodeType & (StyleNodeMatch.Parent | StyleNodeMatch.GrandParent)) != 0)
                    break;

                node = node.Next;
            }

            if (result == null)
            {
                tresult = default(T);
                return false;
            }

            if (result.GetType() != typeof(T))
                throw new WidgetException(string.Format("Trying to retrieve parameter {0} with cast to incompatible type {1} from type {2}", index, typeof(T), result.GetType()));

            tresult = (T)result;
            return true;
        }

        internal T Get<T>(WidgetParameterIndex index, T defaultValue)
        {
            T result;

            return TryGetValue(index, out result) ? result : defaultValue;
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
            WidgetParameterIndex index = WidgetParameterMap.GetIndexByName(name);

            // The box properties store a StyleLength now, so they can remember whether the
            // author wrote px, % or auto. Reading one as a float is public API that predates
            // that, so a length is unwrapped here into exactly the bare number FloatParse used
            // to return: pixels for a length, a 0..1 fraction for a percentage. `auto` has no
            // number of its own, so the caller's default stands.
            //
            // Deliberately only on this by-name overload, which already pays for a name lookup
            // and is nobody's hot path. The indexed Get below, which every property getter in
            // the library runs through, is left exactly as cheap as it was.
            if (typeof(T) == typeof(float))
            {
                WidgetParameterAttribute attribute = WidgetParameterMap.GetAttributeByIndex(index);

                if (attribute != null && attribute.Type == typeof(StyleLength))
                {
                    StyleLength length = Get(index, StyleLength.Unset);

                    if (length.IsAuto || length.IsUnset)
                        return defaultValue;

                    return (T)(object)length.Value;
                }
            }

            return Get(index, defaultValue);
        }

        internal void Set(WidgetParameterIndex index, object value)
        {
            if (!m_hasOwnStyle)
                throw new WidgetException("Trying to set data for read only style!");

            WidgetParameterAttribute attribute = WidgetParameterMap.GetAttributeByIndex(index);

            if (attribute != null && attribute.Type != null && value != null)
                if (value.GetType() != attribute.Type)
                    throw new WidgetException(string.Format("Setting attribute {0} to value {1} type {2} while expecting type {3}", index, value, value.GetType(), attribute.Type));

            ((StyleSheetData)m_data.First.Value.Node.Data).SetParameter(index, value);
        }
      
        /// <summary>
        /// Set the specified parameter by name
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="value">Value.</param>
        public void Set(string name, object value)
        {
            WidgetParameterIndex index = WidgetParameterMap.GetIndexByName(name);

            // Mirror image of the unwrap in the by-name Get above: a caller that reads a box
            // property as a bare float has to be able to write one back, which is what
            // Set("width", 100f) meant before these properties started storing a StyleLength.
            // A bare number carries no unit, so it means pixels -- exactly what FloatParse
            // handed the layout engine then. Reading a percentage out as a float and writing it
            // straight back therefore lands as pixels, which is the same information the float
            // API always lost; a caller that means a percentage sets a StyleLength.
            //
            // Deliberately only on this by-name overload, for the same reason as the read: it
            // already pays for a name lookup and nothing hot goes through it. The indexed Set
            // is left exactly as it was.
            if (value is float)
            {
                WidgetParameterAttribute attribute = WidgetParameterMap.GetAttributeByIndex(index);

                if (attribute != null && attribute.Type == typeof(StyleLength))
                {
                    Set(index, StyleLength.Pixels((float)value));
                    return;
                }
            }

            Set(index, value);
        }

        public override string ToString()
        {
            IStyleData temp = new StyleSheetData();

            for (var node = m_data.Last; node != null; node = node.Previous)
                temp.LoadData(node.Value.Node.Data);

            return temp.ToString();
        }
    }
}
