using System;
using System.Collections.Generic;
using System.Reflection;

using NewWidgets.UI;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    /// <summary>
    /// Builds one widget for a markup element. The style carries the element's id and classes,
    /// which is all a widget constructor needs, so a game registers its own types with a one
    /// line <c>delegate</c>
    /// </summary>
    public delegate Widget WidgetFactoryDelegate(WidgetStyle style);

    /// <summary>
    /// Turns the href of a <c>&lt;link rel="stylesheet"&gt;</c> into CSS text. The library has
    /// no idea where a game keeps its resources, so resolving one is the caller's job. Returning
    /// null means "not found" and is logged, not thrown
    /// </summary>
    public delegate string StyleSheetLoaderDelegate(string href);

    /// <summary>
    /// XHTML markup support: build a widget tree from a document, and write one back out.
    ///
    /// Element names are real HTML tags wherever a sensible one exists, so the same file opens
    /// in an HTML editor and in a browser. The NewWidgets element type is NOT taken from the tag
    /// -- it comes from the widget class itself, whose constructor passes its own
    /// <c>ElementType</c> down to <see cref="Widget"/>. So <c>&lt;div&gt;</c> builds a
    /// <see cref="WidgetPanel"/>, which reports <c>StyleElementType == "panel"</c>, and every
    /// existing stylesheet rule written against <c>panel</c> keeps matching untouched. The
    /// <see cref="WidgetType"/> recorded next to each registration is the declared element type
    /// for that tag, checked against what the widget actually reports the first time one is
    /// built; a mismatch is logged rather than thrown.
    ///
    /// Layout is absolute only. Nothing here arranges anything: a child is added to its parent
    /// and its box comes from the cascade, exactly as for a widget built in code.
    /// </summary>
    public static partial class WidgetManager
    {
        /// <summary>
        /// One registered markup element. <see cref="Selector"/> is the registration key and
        /// the single place the tag-to-widget mapping lives, in both directions:
        /// <c>s_markupElements</c> reads it left to right for loading, <c>s_markupSelectors</c>
        /// right to left for saving.
        /// </summary>
        private struct MarkupElement
        {
            public readonly string Selector; // "input[type=checkbox]"
            public readonly string TagName; // "input"
            public readonly string AttributeName; // "type", or null for a plain tag
            public readonly string AttributeValue; // "checkbox", or null for a plain tag
            public readonly string ElementType; // "checkbox", the NewWidgets element type
            public readonly WidgetFactoryDelegate Factory;

            public MarkupElement(string selector, string tagName, string attributeName, string attributeValue, string elementType, WidgetFactoryDelegate factory)
            {
                Selector = selector;
                TagName = tagName;
                AttributeName = attributeName;
                AttributeValue = attributeValue;
                ElementType = elementType;
                Factory = factory;
            }
        }

        private const string XhtmlNamespace = "http://www.w3.org/1999/xhtml";

        // XHTML 1.0 Strict requires a title inside the head, and an empty <title/> makes an
        // HTML parser read the rest of the document as title text. A widget tree has no name
        // to offer, so every saved document gets the same one
        private const string DocumentTitle = "NewWidgets user interface";

        private static readonly char[] s_classSeparators = new char[] { ' ', '\t', '\r', '\n' };

        // the HTML void elements: no content, no closing tag. A browser parses anything written
        // between the tags of one as a sibling node, not as its content, so the saver has to
        // keep text out of them. Only input and hr are in the table below; the rest are listed
        // because a game registering one of them must not produce a document a browser misreads
        private static readonly string[] s_voidElements = new string[] { "area", "base", "br", "col", "hr", "img", "input", "link", "meta", "param" };

        // selector -> how to build it. Used by the loader
        private static readonly IDictionary<string, MarkupElement> s_markupElements = new Dictionary<string, MarkupElement>();

        // widget class -> selector. Used by the saver. A game's own subclass that registered
        // nothing falls back to the nearest registered base class
        private static readonly IDictionary<Type, string> s_markupSelectors = new Dictionary<Type, string>();

        // widget class -> its string Text property, or null when it has none. Reflection is
        // cheap here because it happens once per class, at load or save time, never in a frame
        private static readonly IDictionary<Type, PropertyInfo> s_markupTextProperties = new Dictionary<Type, PropertyInfo>();

        /// <summary>
        /// The built-in element table. Real HTML tags throughout, and only tags that XHTML 1.0
        /// Strict has and that a browser renders as a plain box: a browser lays every one of
        /// these out as an absolutely positioned box given the same stylesheet, which is the
        /// whole point of using markup rather than a bespoke format.
        ///
        /// A tag is kept only where the document stays valid with the attributes a widget can
        /// actually supply, and where what a browser draws of its own accord is something a
        /// stylesheet can take back. The form controls pass both: all a browser puts on an
        /// <c>&lt;input&gt;</c> or a <c>&lt;button&gt;</c> is a background, a border and a
        /// font, and the stylesheet overrides all three. Three widgets fail one test or the
        /// other and are a <c>div</c> qualified by a class instead, which is the form
        /// <c>Conformance/login.xhtml</c> is written in.
        ///
        /// A window is not <c>&lt;dialog&gt;</c>: that is HTML5, so XHTML 1.0 Strict has no
        /// such element, and a browser's <c>dialog:not([open]) { display: none }</c> would hide
        /// the window and its whole subtree. An image is not <c>&lt;img&gt;</c>: the saver has
        /// no <c>src</c> to give one, XHTML Strict requires <c>alt</c> as well, and the picture
        /// arrives through the CSS <c>background-image</c> that a broken-image placeholder
        /// would sit in front of. A text field is not <c>&lt;textarea&gt;</c>: XHTML 1.0 Strict
        /// declares <c>rows</c> and <c>cols</c> <c>#REQUIRED</c> on that element, and a
        /// <see cref="WidgetTextField"/> is sized in pixels by the cascade and holds no
        /// character grid to fill them from, so any pair of numbers written there would be
        /// invented -- and a browser sizing a stylesheet-less document from invented numbers is
        /// the smaller half of it, because a missing required attribute is an error no
        /// stylesheet can answer and a validating editor rejects the whole document over it.
        /// Its qualifier is the widget name <c>textfield</c> rather than an element type,
        /// because <c>WidgetTextField</c> reports the same element type <c>textedit</c> that
        /// <c>&lt;input&gt;</c> does and so has none of its own to use.
        ///
        /// The qualifier class stays a style class after loading, so a browser's
        /// <c>.window</c> rule and the NewWidgets <c>window</c> rule can live in one stylesheet
        /// and match the same element.
        ///
        /// ponytail: a qualifier class is an ordinary class name, so a panel a game gave the
        /// class <c>window</c> to would save as <c>&lt;div class="window"&gt;</c> and load back
        /// as a window. The ceiling is one reserved name per <c>div</c>-backed widget. Upgrade
        /// path: refuse the qualifier names in <c>Widget.AddStyleClass</c>, or move the marker
        /// to an attribute of its own once the document stops having to be XHTML 1.0 Strict.
        ///
        /// Not registered on purpose: <c>WidgetToolbar</c> and <c>WidgetScrollView</c>, because
        /// both arrange their children themselves and this loader is absolute-position only;
        /// <c>WidgetTooltip</c>, which is runtime chrome rather than document content.
        /// </summary>
        static WidgetManager()
        {
            RegisterElement<WidgetPanel>("div", WidgetType.Panel, delegate (WidgetStyle style) { return new WidgetPanel(style); });
            RegisterElement<WidgetWindow>("div[class=window]", WidgetType.Window, delegate (WidgetStyle style) { return new WidgetWindow(style); });
            RegisterElement<WidgetLabel>("span", WidgetType.Label, delegate (WidgetStyle style) { return new WidgetLabel(style); });
            RegisterElement<WidgetText>("p", WidgetType.Text, delegate (WidgetStyle style) { return new WidgetText(style); });
            RegisterElement<WidgetButton>("button", WidgetType.Button, delegate (WidgetStyle style) { return new WidgetButton(style); });
            RegisterElement<WidgetImage>("div[class=image]", WidgetType.Image, delegate (WidgetStyle style) { return new WidgetImage(style); });
            RegisterElement<WidgetLine>("hr", WidgetType.Line, delegate (WidgetStyle style) { return new WidgetLine(style); });
            RegisterElement<WidgetTextEdit>("input", WidgetType.TextEdit, delegate (WidgetStyle style) { return new WidgetTextEdit(style); });
            RegisterElement<WidgetTextField>("div[class=textfield]", WidgetType.TextField, delegate (WidgetStyle style) { return new WidgetTextField(style); });
            RegisterElement<WidgetCheckBox>("input[type=checkbox]", WidgetType.CheckBox, delegate (WidgetStyle style) { return new WidgetCheckBox(style); });
        }

        #region Registration

        /// <summary>
        /// Registers a markup element. The selector is either a plain tag name, or a tag with
        /// one attribute test in CSS form -- <c>input[type=checkbox]</c> -- which is how one tag
        /// serves more than one widget. The attribute-qualified form wins over the plain one.
        /// Registering an existing selector replaces it, so a game can override a built-in.
        /// </summary>
        /// <typeparam name="T">Widget class the factory produces. Recorded so the saver can find
        /// this element again from a widget instance</typeparam>
        /// <param name="selector">Element name, optionally with one attribute test</param>
        /// <param name="widgetType">Declared NewWidgets element type for this tag</param>
        /// <param name="factory">Builds the widget</param>
        public static void RegisterElement<T>(string selector, WidgetType widgetType, WidgetFactoryDelegate factory) where T : Widget
        {
            if (string.IsNullOrEmpty(selector))
                throw new ArgumentNullException("selector");

            if (factory == null)
                throw new ArgumentNullException("factory");

            string tagName = selector;
            string attributeName = null;
            string attributeValue = null;

            int bracket = selector.IndexOf('[');

            if (bracket > 0 && selector.EndsWith("]", StringComparison.Ordinal))
            {
                tagName = selector.Substring(0, bracket);

                string test = selector.Substring(bracket + 1, selector.Length - bracket - 2);
                int equals = test.IndexOf('=');

                if (equals <= 0)
                    throw new ArgumentException("Markup selector " + selector + " needs an attribute test of the form [name=value]");

                attributeName = test.Substring(0, equals);
                attributeValue = test.Substring(equals + 1).Trim('"', '\'');
            }

            s_markupElements[selector] = new MarkupElement(selector, tagName, attributeName, attributeValue, GetElementTypeName(widgetType), factory);
            s_markupSelectors[typeof(T)] = selector;
        }

        /// <summary>
        /// Reads the <see cref="NameAttribute"/> already carried by every <see cref="WidgetType"/>
        /// member. That attribute table is the element-name list; this method is what finally
        /// reads it, so there is no second list to keep in step
        /// </summary>
        private static string GetElementTypeName(WidgetType widgetType)
        {
            FieldInfo field = typeof(WidgetType).GetField(widgetType.ToString(), BindingFlags.Public | BindingFlags.Static);

            if (field != null)
                foreach (NameAttribute attribute in field.GetCustomAttributes(typeof(NameAttribute), false))
                    return attribute.Name;

            return widgetType.ToString().ToLowerInvariant();
        }

        #endregion

        #region Loading

        /// <summary>
        /// Builds a widget tree from an XHTML document and adds it to <paramref name="parent"/>,
        /// which stands in for the document's <c>&lt;body&gt;</c>. Stylesheets linked from the
        /// head are loaded first, so the tree resolves against them straight away.
        /// </summary>
        /// <param name="xhtmlText">The document. XHTML, not HTML: it is parsed with XmlDocument</param>
        /// <param name="styleSheetLoader">Resolves a stylesheet href to CSS text. May be null,
        /// in which case links are logged and skipped</param>
        /// <param name="parent">Container the top-level elements are added to</param>
        public static void LoadXHTML(string xhtmlText, StyleSheetLoaderDelegate styleSheetLoader, IWindowContainer parent)
        {
            if (parent == null)
                throw new ArgumentNullException("parent");

            HtmlNode document = HtmlNode.ParseXHtml(xhtmlText);

            if (document.Element != "html")
            {
                WindowController.Instance.LogMessage("XHTML document root is <{0}> and not <html>, nothing loaded", document.Element);
                return;
            }

            foreach (HtmlNode node in document.Children)
            {
                switch (node.Element)
                {
                    case "head":
                        LoadMarkupHead(node, styleSheetLoader);
                        break;
                    case "body":
                        LoadMarkupChildren(node, parent);
                        break;
                    default:
                        WindowController.Instance.LogMessage("Got unknown element <{0}> under <html> in XHTML document", node.Element);
                        break;
                }
            }
        }

        private static void LoadMarkupHead(HtmlNode head, StyleSheetLoaderDelegate styleSheetLoader)
        {
            // everything else a head can hold -- title, meta, base -- has no meaning for a
            // widget tree and is skipped without comment, the way a browser skips what it does
            // not implement
            foreach (HtmlNode node in head.Children)
            {
                if (node.Element != "link" || node.GetAttribute("rel") != "stylesheet")
                    continue;

                string href = node.GetAttribute("href");

                if (string.IsNullOrEmpty(href))
                {
                    WindowController.Instance.LogMessage("Got a stylesheet <link> with no href");
                    continue;
                }

                if (styleSheetLoader == null)
                {
                    WindowController.Instance.LogMessage("Got stylesheet <link href=\"{0}\"> but no stylesheet loader was provided", href);
                    continue;
                }

                string css = styleSheetLoader(href);

                if (css == null)
                {
                    WindowController.Instance.LogMessage("Stylesheet {0} could not be loaded", href);
                    continue;
                }

                LoadCSS(css);
            }
        }

        private static void LoadMarkupChildren(HtmlNode parentNode, IWindowContainer parent)
        {
            foreach (HtmlNode node in parentNode.Children)
            {
                Widget widget = CreateMarkupWidget(node);

                if (widget == null)
                {
                    // the element itself is already logged, but everything nested inside it goes
                    // down with it and would otherwise disappear without a word
                    if (node.Children.Count > 0)
                        WindowController.Instance.LogMessage("Skipped element <{0}> took {1} child element(s) with it, none of them loaded", node.Element, node.Children.Count);

                    continue;
                }

                // added before the children are walked, so a child resolving its own style sees
                // the whole ancestor chain the document declared
                parent.AddChild(widget);

                if (node.Children.Count == 0)
                    continue;

                IWindowContainer container = widget as IWindowContainer;

                if (container == null)
                    WindowController.Instance.LogMessage("Element <{0}> cannot have children, {1} of them skipped", node.Element, node.Children.Count);
                else
                    LoadMarkupChildren(node, container);
            }
        }

        private static Widget CreateMarkupWidget(HtmlNode node)
        {
            MarkupElement element;

            if (!TryGetMarkupElement(node, out element))
            {
                // the same tolerance the CSS parser shows an unknown property: a document a
                // browser renders must stay loadable here
                WindowController.Instance.LogMessage("Got unknown element <{0}> in XHTML document, skipped", node.Element);
                return null;
            }

            string[] classes = null;
            string classAttribute = node.Class;

            if (!string.IsNullOrEmpty(classAttribute))
                classes = classAttribute.Split(s_classSeparators, StringSplitOptions.RemoveEmptyEntries);

            string id = node.Id;

            Widget widget = element.Factory(new WidgetStyle(classes, id == null ? string.Empty : id));

            if (widget.StyleElementType != element.ElementType)
                WindowController.Instance.LogMessage("Element <{0}> is registered as element type '{1}' but its widget reports '{2}', style rules written for '{1}' will not match it",
                    node.Element, element.ElementType, widget.StyleElementType);

            ApplyMarkupStyle(widget, node.GetAttribute("style"));
            ApplyMarkupText(widget, GetMarkupNodeText(node));

            return widget;
        }

        private static bool TryGetMarkupElement(HtmlNode node, out MarkupElement element)
        {
            // an attribute-qualified registration is the more specific one and wins, the way
            // input[type=checkbox] beats input in a stylesheet. An element carries a handful of
            // attributes, so trying each one is cheaper than parsing the table per lookup
            foreach (KeyValuePair<string, string> attribute in node.Attributes)
            {
                // class holds a list of names rather than one value, so each name is tried on
                // its own: <div class="window mkwindow"> has to find div[class=window]
                if (attribute.Key == "class")
                {
                    foreach (string name in attribute.Value.Split(s_classSeparators, StringSplitOptions.RemoveEmptyEntries))
                        if (s_markupElements.TryGetValue(string.Format("{0}[class={1}]", node.Element, name), out element))
                            return true;
                }
                else if (s_markupElements.TryGetValue(string.Format("{0}[{1}={2}]", node.Element, attribute.Key, attribute.Value), out element))
                    return true;
            }

            return s_markupElements.TryGetValue(node.Element, out element);
        }

        /// <summary>
        /// Where an element keeps its text. A void element has none to keep -- a browser parses
        /// what stands between its tags as a sibling node -- so an <c>input</c> is read from the
        /// <c>value</c> attribute, which is the attribute HTML has for exactly this
        /// </summary>
        private static string GetMarkupNodeText(HtmlNode node)
        {
            if (node.Element == "input")
                return node.GetAttribute("value");

            return node.Text;
        }

        private static bool IsVoidMarkupElement(string tagName)
        {
            return Array.IndexOf(s_voidElements, tagName) >= 0;
        }

        /// <summary>
        /// Applies a style="..." attribute as the element's own inline style, which is exactly
        /// what <see cref="Widget"/> already models: its own style sheet sits at the head of the
        /// cascade and outranks everything the selectors matched
        /// </summary>
        private static void ApplyMarkupStyle(Widget widget, string style)
        {
            if (string.IsNullOrEmpty(style))
                return;

            Dictionary<string, string> declarations = new Dictionary<string, string>();

            // ponytail: a plain split, not a CSS tokenizer, so a ';' or ':' inside a quoted
            // value or inside url() would break it. CSSParser.ParseParameter handles that but is
            // private to the parser and a style attribute is not worth opening it up for.
            // Upgrade path: make that method internal and call it from here.
            foreach (string declaration in style.Split(';'))
            {
                int separator = declaration.IndexOf(':');

                if (separator <= 0)
                {
                    if (declaration.Trim().Length > 0)
                        WindowController.Instance.LogMessage("Got malformed declaration '{0}' in a style attribute", declaration.Trim());

                    continue;
                }

                declarations[declaration.Substring(0, separator).Trim()] = declaration.Substring(separator + 1).Trim();
            }

            // the same call the CSS parser makes, so an unknown property is logged and skipped
            // here for the same reason and with the same message
            IDictionary<WidgetParameterIndex, object> parameters = InitCssParameters(declarations);

            foreach (KeyValuePair<WidgetParameterIndex, object> pair in parameters)
                widget.SetProperty(pair.Key, pair.Value);
        }

        private static void ApplyMarkupText(Widget widget, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            PropertyInfo property = GetMarkupTextProperty(widget.GetType());

            if (property == null)
                WindowController.Instance.LogMessage("Element type '{0}' has no text, content \"{1}\" skipped", widget.StyleElementType, text);
            else
                property.SetValue(widget, text, null);
        }

        /// <summary>
        /// The widget's own read/write string Text property, or null when it has none. Found by
        /// reflection rather than by a type switch so that a game's own widget gets its text
        /// filled in without registering anything beyond its element
        /// </summary>
        private static PropertyInfo GetMarkupTextProperty(Type type)
        {
            PropertyInfo result;

            if (s_markupTextProperties.TryGetValue(type, out result))
                return result;

            result = type.GetProperty("Text", typeof(string));

            if (result != null && (!result.CanRead || !result.CanWrite))
                result = null;

            s_markupTextProperties[type] = result;

            return result;
        }

        #endregion

        #region Saving

        /// <summary>
        /// Writes a widget tree back out as an XHTML document. <paramref name="root"/> plays the
        /// part of <c>&lt;body&gt;</c>, mirroring <see cref="LoadXHTML"/>.
        ///
        /// Two things a document can say do not come back out, because a widget tree does not
        /// hold them: the stylesheet links, whose hrefs live in the document and not on any
        /// widget, and a style="..." attribute, which is indistinguishable from a cascaded value
        /// once applied -- <c>Widget.m_ownStyle</c> has a reader for one property at a time and
        /// no way to enumerate what was set.
        /// </summary>
        public static string SaveXHTML(IWindowContainer root)
        {
            if (root == null)
                throw new ArgumentNullException("root");

            HtmlNode html = new HtmlNode(null, "html", null);
            html.SetAttribute("xmlns", XhtmlNamespace);

            HtmlNode head = new HtmlNode(html, "head", null);
            new HtmlNode(head, "title", DocumentTitle);

            HtmlNode body = new HtmlNode(html, "body", null);

            SaveMarkupChildren(root, body);

            return HtmlNode.SaveXHtml(html);
        }

        private static void SaveMarkupChildren(IWindowContainer container, HtmlNode parentNode)
        {
            foreach (WindowObject child in container.Children)
            {
                Widget widget = child as Widget;

                if (widget == null)
                    continue; // a plain WindowObject is not a widget and has no element

                string selector = GetMarkupSelector(widget.GetType());

                if (selector == null)
                {
                    WindowController.Instance.LogMessage("Widget class {0} has no registered element, skipped while saving", widget.GetType().Name);
                    continue;
                }

                MarkupElement element = s_markupElements[selector];

                string text = GetMarkupText(widget);
                bool isVoid = IsVoidMarkupElement(element.TagName);

                // a void element gets no text, whatever the widget holds: a browser reads what
                // stands between its tags as a sibling node and the document says the wrong
                // thing, even though it still parses back in here
                HtmlNode node = new HtmlNode(parentNode, element.TagName, isVoid ? null : text);

                // attribute order: the tag qualifier first, because it is what picks the widget
                // class, then id, then class, then the text of a void element. A qualifier that
                // is itself the class attribute joins the class list instead of being written
                // twice, in front of the widget's own classes
                bool isClassQualifier = element.AttributeName == "class";

                if (element.AttributeName != null && !isClassQualifier)
                    node.SetAttribute(element.AttributeName, element.AttributeValue);

                if (!string.IsNullOrEmpty(widget.StyleId))
                    node.SetAttribute("id", widget.StyleId);

                string classes = JoinMarkupClasses(isClassQualifier ? element.AttributeValue : null, widget.StyleClasses);

                if (!string.IsNullOrEmpty(classes))
                    node.SetAttribute("class", classes);

                if (isVoid && !string.IsNullOrEmpty(text))
                {
                    if (element.TagName == "input")
                        node.SetAttribute("value", text);
                    else
                        WindowController.Instance.LogMessage("Element <{0}> is a void element with no attribute to keep text in, so \"{1}\" is not saved", element.TagName, text);
                }

                IWindowContainer childContainer = widget as IWindowContainer;

                if (childContainer != null)
                    SaveMarkupChildren(childContainer, node);
            }
        }

        /// <summary>
        /// Walks up the class hierarchy, so a game's own subclass of a registered widget still
        /// saves as the base class's element until it registers one of its own
        /// </summary>
        private static string GetMarkupSelector(Type type)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                string selector;

                if (s_markupSelectors.TryGetValue(current, out selector))
                    return selector;
            }

            return null;
        }

        private static string GetMarkupText(Widget widget)
        {
            PropertyInfo property = GetMarkupTextProperty(widget.GetType());

            if (property == null)
                return null;

            return (string)property.GetValue(widget, null);
        }

        /// <summary>
        /// The class attribute: <paramref name="first"/> is the qualifier of a
        /// <c>div[class=window]</c> style registration and leads the list, or null for a tag
        /// that needs no qualifier
        /// </summary>
        private static string JoinMarkupClasses(string first, string[] classes)
        {
            List<string> used = new List<string>((classes == null ? 0 : classes.Length) + 1);

            if (!string.IsNullOrEmpty(first))
                used.Add(first);

            // RemoveStyleClass blanks an entry rather than shrinking the array, so empty ones
            // have to be dropped here or the document grows double spaces. The qualifier is
            // dropped the second time too: a widget the loader built already carries it as a
            // style class, and writing it twice would grow the list on every save
            if (classes != null)
                for (int i = 0; i < classes.Length; i++)
                    if (!string.IsNullOrEmpty(classes[i]) && classes[i] != first)
                        used.Add(classes[i]);

            if (used.Count == 0)
                return null;

            return string.Join(" ", used.ToArray());
        }

        #endregion
    }
}
