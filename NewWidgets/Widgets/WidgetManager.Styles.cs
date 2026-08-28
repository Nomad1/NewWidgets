using System;
using System.Collections.Generic;
using System.Xml;
using NewWidgets.UI;
using NewWidgets.UI.Styles;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    public static partial class WidgetManager
    {
        /// <summary>
        /// The uniform grid one sprite has already been cut into, so a second request for the
        /// same sprite can be told apart from a conflicting one.
        /// </summary>
        private struct SpriteSubdivision
        {
            public readonly int TileX;
            public readonly int TileY;

            public SpriteSubdivision(int tileX, int tileY)
            {
                TileX = tileX;
                TileY = tileY;
            }
        }

        // this is primary CSS style collection for now
        private static readonly StyleCollection s_styleCollection = new StyleCollection();

        // Every sprite already handed to WindowController.SetSpriteSubdivision, by sprite name.
        // A subdivision reads frame 0 of the sprite and overwrites the sprite with the pieces,
        // so cutting the same sprite twice cuts the top-left ninth into nine and silently
        // destroys the nine-patch. Since border-image-slice is declared per rule, and dozens of
        // rules name the same sprite, the second request has to be recognised rather than
        // obeyed. Deliberately never cleared -- ResetStyles empties the stylesheet, but the
        // cuts live on WindowController and it has no way to undo them.
        private static readonly IDictionary<string, SpriteSubdivision> s_spriteSubdivisions = new Dictionary<string, SpriteSubdivision>();

        /// <summary>
        /// How far a stored <c>border-image-slice</c> value may sit from one third and still be
        /// the thirds this engine cuts: 33.3333%, 33.33% and 33.4% are all the same patch.
        /// </summary>
        private static readonly float s_thirdSliceTolerance = 0.005f;

        /// <summary>
        /// Gets the style by name. This method is here only for compatibility purposes and it would be removed in later versions
        /// </summary>
        /// <returns>The style.</returns>
        /// <param name="class">Name.</param>
        public static WidgetStyle GetStyle(string @class, bool notUsed = false)
        {
            if (string.IsNullOrEmpty(@class))
                return default(WidgetStyle);

            string[] classes = @class.Split(new[] { ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (classes.Length == 0)
                return default(WidgetStyle);

            return new WidgetStyle(classes, string.Empty);
        }

        /// <summary>
        /// Gets the style by selector list. It works with hierarchy, specificity and all the stuff
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        internal static WidgetStyleSheet GetStyle(StyleSelectorList list)
        {
            ICollection<StyleNodeMatchPair> result = s_styleCollection.GetStyleData(list);

            return new WidgetStyleSheet(list.ToString(), result);
        }

        /// <summary>
        /// Gets the style by single style selector
        /// </summary>
        /// <param name="singleSelector"></param>
        /// <returns></returns>
        internal static WidgetStyleSheet GetStyle(StyleSelector singleSelector)
        {
            return GetStyle(new StyleSelectorList(singleSelector, StyleNodeMatch.Class));
        }

        public static void LoadCSS(string uiData)
        {
            CSSParser.ParseCSS(uiData, s_styleCollection, InitCssData);
        }

        private static IStyleData InitCssData(string name, Dictionary<string, string> parameters)
        {
            // @font-face names the font it declares with a font-family declaration rather than
            // with its selector, so it is read before the declarations are parsed
            if (name == "@font-face")
                return InitFontFace(parameters);

            IDictionary<WidgetParameterIndex, object> style = InitCssParameters(parameters);
            StyleSheetData data = new StyleSheetData(style);

            if (name.StartsWith("@font."))
                RegisterFont(name.Split('.')[1], data);
            else
                ScanBorderImageSubdivision(style);

            return data;
        }

        /// <summary>
        /// The subdivision scan, and the reason <c>@sprite</c> is gone: a stylesheet says how a
        /// sprite is cut in the rule that uses it, with the standard's own
        /// <c>border-image-slice</c>, and this reads that out of every rule as the stylesheet
        /// loads. A rule naming a sprite it does not slice contributes nothing, so the rule that
        /// does slice it is the one that decides -- and every rule in the shipped skins carries
        /// both, so no rule depends on the cascade to complete it.
        ///
        /// Load time, once per rule. Nothing here runs on a read or a draw.
        /// </summary>
        private static void ScanBorderImageSubdivision(IDictionary<WidgetParameterIndex, object> style)
        {
            object source;
            object slice;

            if (!style.TryGetValue(WidgetParameterIndex.BorderImageSource, out source))
                return;

            if (!style.TryGetValue(WidgetParameterIndex.BorderImageSlice, out slice))
                return;

            // the url is stored as authored so SaveCSS can write it back whole (D188); the cut
            // is registered against the sprite the fragment names (D187)
            string sprite = ConversionHelper.UrlToSpriteName((string)source);

            if (string.IsNullOrEmpty(sprite) || string.Equals(sprite, "none", StringComparison.OrdinalIgnoreCase))
                return;

            int tileX;
            int tileY;

            if (!TryGetBorderImageGrid((Margin)slice, out tileX, out tileY))
            {
                WindowController.Instance.LogError("border-image-slice {0} of sprite {1} is neither the 3x3 nor the horizontal 3x1 cut this engine draws, so {1} is left whole. A vertical three-patch does not exist here", slice, sprite);
                return;
            }

            RegisterSpriteSubdivision(sprite, tileX, tileY);
        }

        /// <summary>
        /// Reads a CSS <c>border-image-slice</c> as one of the two uniform grids this engine
        /// cuts. <c>border-image-slice</c> is written top right bottom left, so a slice at
        /// thirds on both axes is the 3x3 nine-patch and one at thirds on the horizontal axis
        /// alone -- <c>0 33.3333%</c> -- is the 3x1 three-patch.
        ///
        /// <c>33.3333% 0</c>, the vertical form, is not a grid this engine has: the three-patch
        /// renderer walks three frames along x and scales by height (D193). It returns false
        /// here along with every other slice, and an arbitrary slice is drawn by
        /// <c>WidgetBackground.InitBorderImageBackground</c> instead.
        /// </summary>
        internal static bool TryGetBorderImageGrid(Margin slice, out int tileX, out int tileY)
        {
            tileX = 0;
            tileY = 0;

            bool cutsHorizontally = IsThirdSlice(slice.Left) && IsThirdSlice(slice.Right);
            bool cutsVertically = IsThirdSlice(slice.Top) && IsThirdSlice(slice.Bottom);

            if (cutsHorizontally && cutsVertically)
            {
                tileX = 3;
                tileY = 3;
                return true;
            }

            if (cutsHorizontally && slice.Top == 0.0f && slice.Bottom == 0.0f)
            {
                tileX = 3;
                tileY = 1;
                return true;
            }

            return false;
        }

        private static bool IsThirdSlice(float value)
        {
            return Math.Abs(value - 1.0f / 3.0f) < s_thirdSliceTolerance;
        }

        /// <summary>
        /// Cuts one sprite into a uniform grid, at most once. Every request for a subdivision
        /// goes through here -- the CSS scan above and the two legacy XML patch elements alike
        /// -- because <c>SetSpriteSubdivision</c> registers the pieces under the source's own
        /// name, so a second call reads the already-cut sprite and quietly replaces the whole
        /// nine-patch with nine slivers of its top-left corner.
        ///
        /// A repeat of the same grid is the normal case and is silent. A different grid for a
        /// sprite already cut is a stylesheet defect that cannot be honoured either way round,
        /// so the first cut stands and the conflict is reported.
        /// </summary>
        private static void RegisterSpriteSubdivision(string sprite, int tileX, int tileY)
        {
            SpriteSubdivision existing;

            if (s_spriteSubdivisions.TryGetValue(sprite, out existing))
            {
                if (existing.TileX != tileX || existing.TileY != tileY)
                    WindowController.Instance.LogError("Sprite {0} is asked for a {1}x{2} subdivision but is already cut {3}x{4}; the first cut stands, because cutting a sprite twice destroys it. Two rules disagree about the same sprite", sprite, tileX, tileY, existing.TileX, existing.TileY);

                return;
            }

            s_spriteSubdivisions[sprite] = new SpriteSubdivision(tileX, tileY);

            WindowController.Instance.SetSpriteSubdivision(sprite, tileX, tileY);
        }

        /// <summary>
        /// Reads a CSS <c>@font-face</c> rule. This engine's font registry is keyed by the
        /// selector of an <c>@font.&lt;name&gt;</c> rule, where the standard names the font with a
        /// <c>font-family</c> declaration inside the block, so the name is taken from the raw
        /// declaration text: <see cref="FontFamilyProcessor"/> resolves a family to an already
        /// registered <see cref="Font"/>, and the font being declared here is not one yet.
        ///
        /// The family is then put back into the block, after the font exists, for two reasons:
        /// <c>SaveCSS</c> writes the parameters and nothing else, so a block without it saves
        /// as a nameless face that no reparse can recover; and it is the only thing that tells
        /// one <c>@font-face</c> node from another, which is what
        /// <see cref="StyleSheetData.StyleDataName"/> hands to the collection.
        /// </summary>
        private static IStyleData InitFontFace(IDictionary<string, string> parameters)
        {
            string family;

            if (!parameters.TryGetValue("font-family", out family))
            {
                WindowController.Instance.LogError("Got a @font-face rule with no font-family name");
                return new StyleSheetData(InitCssParameters(parameters));
            }

            parameters.Remove("font-family");

            StyleSheetData data = new StyleSheetData(InitCssParameters(parameters));

            string name = UnquoteFontFamily(family);

            RegisterFont(name, data);

            Font font;

            // set directly rather than through FontFamilyProcessor: the value a font-family
            // declaration resolves to is the Font object, and this is where it first exists
            if (TryGetFont(name, out font))
                data.SetParameter(WidgetParameterIndex.Font, font);

            return data;
        }

        /// <summary>
        /// Builds a <see cref="Font"/> from the font metrics of a parsed <c>@font-face</c> or
        /// <c>@font.&lt;name&gt;</c> block and puts it in the registry <see cref="GetFont"/> reads.
        /// </summary>
        private static void RegisterFont(string fontName, StyleSheetData data)
        {
            string resource = data.GetParameter(WidgetParameterIndex.FontResource, "");
            string material = data.GetParameter(WidgetParameterIndex.FontMaterial, "");

            // ponytail: a material is just prepended to the resource with a pipe, the same
            // shape the XML skin loader has always accepted as resource="material|sprite" --
            // there is no shader-selection concept here beyond string concatenation. Ceiling:
            // a real material system replaces this once one exists.
            //
            // Only the quotes come off. UnquoteFontFamily is font-family logic and does not
            // belong on a material name, and UrlToSpriteName is worse: it returns everything
            // after a '#', so composing before it would silently discard the material the
            // moment a resource carried a fragment. A font is not a sprite and is never
            // addressed as one, so neither helper has a job here.
            // Extract first, compose second. UrlToSpriteName returns everything after a '#',
            // so composing first would silently discard the material whenever a resource
            // carried a fragment -- and Test 92 pins that a font may be written as
            // url("ui.svg#glyphs"). Only quotes come off the material: UnquoteFontFamily is
            // font-family logic and has no business on a shader name.
            resource = ConversionHelper.UrlToSpriteName(resource);

            if (!string.IsNullOrEmpty(material))
                resource = material.Trim('"', '\'') + "|" + resource;

            if (string.IsNullOrEmpty(fontName) || string.IsNullOrEmpty(resource))
            {
                WindowController.Instance.LogError("Got a font rule for {0} with no resource to load it from", fontName);
                return;
            }

            Font font = new Font(
                fontName,
                resource,
                data.GetParameter(WidgetParameterIndex.FontSpacing, 0.0f),
                data.GetParameter(WidgetParameterIndex.FontLeading, 0),
                data.GetParameter(WidgetParameterIndex.FontBaseline, 10),
                data.GetParameter(WidgetParameterIndex.FontShift, 0));

            s_fonts[fontName] = font;

            if (fontName == DefaultFontName)
                s_mainFont = font;
        }

        /// <summary>
        /// Strips the whitespace and the optional quotes CSS allows around one family name.
        /// </summary>
        internal static string UnquoteFontFamily(string family)
        {
            return family.Trim().Trim('"', '\'');
        }

        /// <summary>
        /// Font lookup that does not complain when the font is absent, which is what walking a
        /// <c>font-family</c> stack needs -- <see cref="GetFont"/> logs an error and is the
        /// right call for a name that is already known to be the only candidate.
        /// </summary>
        internal static bool TryGetFont(string name, out Font font)
        {
            font = null;

            if (string.IsNullOrEmpty(name))
                return false;

            if (name == DefaultFontName)
            {
                font = s_mainFont;
                return font != null;
            }

            return s_fonts.TryGetValue(name, out font);
        }

        private static IDictionary<WidgetParameterIndex, object> InitCssParameters(IDictionary<string, string> parameters)
        {
            Dictionary<WidgetParameterIndex, object> style = new Dictionary<WidgetParameterIndex, object>();

            foreach (KeyValuePair<string,string> pair in parameters)
            {
                string key = pair.Key;
                string value = pair.Value;

                try
                {
                    if (value == null)
                        value = string.Empty;
                    else
                        value = value.Trim('\r', '\n', '\t', ' ');

                    IParameterProcessor processor = WidgetParameterMap.GetProcessorByCssName(key);

                    if (processor == null)
                        WindowController.Instance.LogMessage("Got unknown attribute {0} in CSS style sheet", key);
                    else
                        processor.Process(style, value);
                }
                catch (Exception ex)
                {
                    WindowController.Instance.LogError("Error parsing style, element {0}: {1}", key, ex);
                    throw new WidgetException("Error parsing style!", ex);
                }
            }

            return style;
        }

        public static void SaveCSS(System.IO.TextWriter outputStream)
        {
            s_styleCollection.Dump(outputStream);
        }

        /// <summary>
        /// Clears everything <see cref="LoadCSS"/> populates: the CSS style collection itself,
        /// and the font registry (<c>s_fonts</c>/<c>s_mainFont</c> in WidgetManager.cs) that
        /// <see cref="InitCssData"/> fills in from <c>@font</c> rules -- both are stylesheet
        /// content, so a caller that resets one but not the other would still see stale fonts
        /// resolve after the styles referencing them are gone. Intended for tests that need to
        /// load a stylesheet in isolation from whatever ran before.
        ///
        /// Does NOT touch: widget/window runtime state (focus, tooltip, exclusive widgets,
        /// top-level window, font scale) -- none of it is stylesheet-derived; the legacy XML
        /// style loader's lookforward/default-style tables (<c>s_lookForwardStyles</c>,
        /// <c>s_defaultStyles</c>) -- that path is obsolete and unused by CSS-based callers;
        /// or sprite subdivisions registered on <c>WindowController.Instance</c> from
        /// <c>border-image-slice</c> -- those live on a different class entirely, which has no
        /// way to undo a cut, so the record of what is already cut is not cleared either.
        /// </summary>
        public static void ResetStyles()
        {
            s_styleCollection.Clear();
            s_fonts.Clear();
            s_mainFont = null;
        }


        #region XML style for backwards compatibility

        private static readonly IDictionary<string, string> s_lookForwardStyles = new Dictionary<string, string>();
        private static readonly IDictionary<string, string> s_defaultStyles = new Dictionary<string, string>();

        [Obsolete]
        public static void LoadUI(string uiData)
        {
            LoadXML(uiData);
        }

        /// <summary>
        /// Loads ui data from a XML string
        /// </summary>
        /// <param name="uiData"></param>
        /// <exception cref="WidgetException"></exception>
        public static void LoadXML(string uiData)
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
                                    RegisterXmlFont(node);
                                    break;
                                case "nine":
                                    RegisterXmlNinePatch(node);
                                    break;
                                case "three":
                                    RegisterXmlThreePatch(node);
                                    break;
                                case "style":
                                    RegisterXmlStyle(node);
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

        private static void RegisterXmlFont(XmlNode node)
        {
            string name = node.Attributes.GetNamedItem("name").Value;
            string resource = node.Attributes.GetNamedItem("resource").Value;
            float spacing = ConversionHelper.FloatParse(node.Attributes.GetNamedItem("spacing").Value);
            int baseline = int.Parse(node.Attributes.GetNamedItem("baseline").Value);

            int shift = 0;

            if (node.Attributes.GetNamedItem("shift") != null)
                shift = int.Parse(node.Attributes.GetNamedItem("shift").Value);

            int leading = 0;

            if (node.Attributes.GetNamedItem("leading") != null)
                leading = int.Parse(node.Attributes.GetNamedItem("leading").Value);

            Font font = new Font(name, resource, spacing, leading, baseline, shift);

            s_fonts[name] = font;

            if (name == "default")
                s_mainFont = font;

            Dictionary<WidgetParameterIndex, object> fontStyle = new Dictionary<WidgetParameterIndex, object>();
            fontStyle[WidgetParameterIndex.FontResource] = resource;
            fontStyle[WidgetParameterIndex.FontSpacing] = spacing;
            fontStyle[WidgetParameterIndex.FontShift] = shift;
            fontStyle[WidgetParameterIndex.FontLeading] = leading;
            fontStyle[WidgetParameterIndex.FontBaseline] = baseline;

            s_styleCollection.AddStyle("@font." + name, new StyleSheetData(fontStyle));

            WindowController.Instance.LogMessage("Registered font {0}, resource {1}, spacing {2}", name, resource, spacing);
        }

        private static void RegisterXmlNinePatch(XmlNode node)
        {
            string name = node.Attributes.GetNamedItem("name").Value;

            RegisterSpriteSubdivision(name, 3, 3);

            WindowController.Instance.LogMessage("Registered nine patch {0}", name);
        }

        private static void RegisterXmlThreePatch(XmlNode node)
        {
            string name = node.Attributes.GetNamedItem("name").Value;

            RegisterSpriteSubdivision(name, 3, 1);

            WindowController.Instance.LogMessage("Registered three patch {0}", name);
        }

        private static void RegisterXmlStyle(XmlNode node)
        {
            string name = GetAttribute(node, "name");

            if (string.IsNullOrEmpty(name))
                throw new WidgetException("Got style without a name!");

            string parent = GetAttribute(node, "parent");

            if (s_lookForwardStyles.ContainsKey(name))
                name = s_lookForwardStyles[name];
            else
            {
                if (name.StartsWith("default_"))
                {
                    string subName = name.Substring(8);
                    s_defaultStyles[name] = subName;
                    name = subName;
                }
                else
                {
                    switch (name)
                    {
                       /* case "tooltip":
                        case "checkbox":
                        case "button":
                        case "image":
                        case "label":
                        //case WidgetText.ElementType:
                        case WidgetLine.ElementType:
                        case WidgetPanel.ElementType:

                        case "context_menu":
                        case "scrollview":

                        case WidgetTextEdit.ElementType:
                        case WidgetWindow.ElementType:
                        case WidgetList.ElementType:

                        case WidgetProgressLine.ElementType:
                        case WidgetProgressLine.LabelId:
                        case WidgetProgressLine.LineId:

                        case WidgetSelect.ElementType:
                        case WidgetSelect.LeftButtonId:
                        case WidgetSelect.RightButtonId:
                        case WidgetSelect.LabelId:

                        case WidgetTable.ElementType:
                        //case "tablerow": // tr?
                        //case "row_header": // th?
                        //case "row_odd":
                        //case "row_even":

                        case WidgetSlider.ElementType:
                        case WidgetSlider.TrackerId:
                        case WidgetSlider.LabelId:
                        case WidgetSlider.LineId:
                            s_defaultStyles[name] = name;
                            break;*/
                        case "scroll_horizontal":
                            name = "#scrollview_hscroll";
                            break;
                        case "scroll_indicator_horizontal":
                            name = "#scrollview_htrack";
                            break;
                        case "scroll_vertical":
                            name = "#scrollview_vscroll";
                            break;
                        case "scroll_indicator_vertical":
                            name = "#scrollview_vtrack";
                            break;
                        default: // class name, start with .
                            name = string.IsNullOrEmpty(parent) || s_defaultStyles.ContainsKey(parent) ? char.IsLetter(name[0]) ? ("." + name) : name : ("." + parent + "." + name);
                            break;
                    }
                }
            }

            IDictionary<WidgetParameterIndex, object> parameters = InitXmlStyle(node, name);

            s_styleCollection.AddStyle(name, new StyleSheetData(parameters));

            WindowController.Instance.LogMessage("Registered style {0}", name);
        }

        private static IDictionary<WidgetParameterIndex, object> InitXmlStyle(XmlNode node, string elementName)
        {
            Dictionary<WidgetParameterIndex, object> style = new Dictionary<WidgetParameterIndex, object>();

            foreach (XmlNode element in node.ChildNodes)
            {
                switch (element.Name)
                {
                    case "selected_style":
                        s_lookForwardStyles[element.InnerText] = elementName + ":focus";
                        continue;
                    case "hovered_style":
                        s_lookForwardStyles[element.InnerText] = elementName + ":hover";
                        continue;
                    case "disabled_style":
                        s_lookForwardStyles[element.InnerText] = elementName + ":disabled";
                        continue;
                    case "button_image_style":
                        s_lookForwardStyles[element.InnerText] = elementName + " #checkbox_image";
                        continue;
                }

                try
                {
                    string value = element.InnerText;

                    if (string.IsNullOrEmpty(value))
                        value = GetAttribute(element, "value");

                    if (value == null)
                        value = string.Empty;
                    else
                        value = value.Trim('\r', '\n', '\t', ' ');

                    IParameterProcessor processor = WidgetParameterMap.GetProcessorByXmlName(element.Name);

                    if (processor == null)
                        WindowController.Instance.LogMessage("Got unknown attribute {0} in xml style sheet for {1}", element.Name, node.Name);
                    else
                        processor.Process(style, value);
                }
                catch (Exception ex)
                {
                    WindowController.Instance.LogError("Error parsing style {0}, element -nw-{1}: {2}", node.Name, element.Name, ex);
                    throw new WidgetException("Error parsing style!", ex);
                }
            }

            return style;
        }

        private static string GetAttribute(XmlNode node, string name)
        {
            var attribute = node.Attributes.GetNamedItem(name);

            return attribute == null ? null : attribute.Value;
        }

        #endregion
    }
}
