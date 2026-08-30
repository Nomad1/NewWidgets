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
    /// Everything one markup element said that no widget property holds, kept so that saving
    /// gives the author's file back rather than a reduction of it. A widget built in code has
    /// none of this and the reference is null, so the cost lands only on widgets a document
    /// built: one object of five references, plus a list per widget that really carries
    /// unmodelled attributes or comments. A fifteen-control dialog is under a kilobyte.
    ///
    /// The alternative was to drop what the engine does not model, which is what the saver did
    /// before. That makes the file unsafe to edit: an HTML editor's own bookkeeping --
    /// <c>data-</c> attributes, <c>lang</c>, <c>tabindex</c>, the comments it writes around a
    /// region -- would be stripped on every save, so the owner would lose work by opening the
    /// document in the engine.
    ///
    /// ponytail: attributes are kept as text and never re-read after loading, so a property
    /// the engine later learns to model keeps a stale copy here until the loader stops bagging
    /// it. The upgrade path is to remove the name from <c>IsMarkupAttributeModelled</c> in the
    /// same change that adds the property.
    /// </summary>
    public class WidgetMarkup
    {
        private readonly string m_source;

        private string m_text;
        private List<KeyValuePair<string, string>> m_attributes;
        private Dictionary<string, string> m_styleAttributes;
        private List<string> m_comments;
        private List<string> m_trailingComments;

        /// <summary>
        /// The registration selector the loader matched -- <c>span</c>, <c>input[type=checkbox]</c>
        /// -- or, for an element no registration matched, the tag name on its own. This is what
        /// lets several tags map to one widget class and still round-trip: a
        /// <see cref="WidgetLabel"/> loaded from <c>&lt;label&gt;</c> saves as <c>&lt;label&gt;</c>
        /// and one loaded from <c>&lt;span&gt;</c> saves as <c>&lt;span&gt;</c>
        /// </summary>
        public string Source
        {
            get { return m_source; }
        }

        /// <summary>
        /// Text content of an element no widget models, which therefore has no Text property to
        /// keep it in. Null for every element that has one
        /// </summary>
        public string Text
        {
            get { return m_text; }
            set { m_text = value; }
        }

        /// <summary>
        /// Attributes the loader did not turn into a widget property, in document order. Null
        /// until the first one is added
        /// </summary>
        public IList<KeyValuePair<string, string>> Attributes
        {
            get { return m_attributes; }
        }

        /// <summary>
        /// Every attribute the source tag carried, exactly as written, whether or not a widget
        /// property already models it. This is a separate copy from <see cref="Attributes"/>
        /// on purpose: that list exists so <c>SaveXHTML</c> can round-trip what no property
        /// holds, and dropping an attribute from it the moment a property models it is correct
        /// for saving. Style matching needs the opposite -- <c>&lt;input type="checkbox"&gt;</c>
        /// answers <c>input[type="checkbox"]</c> even though <c>type</c> already picked
        /// <see cref="WidgetCheckBox"/> out of the markup table and a property never stored it
        /// verbatim. Null until the first one is added
        /// </summary>
        public IDictionary<string, string> StyleAttributes
        {
            get { return m_styleAttributes; }
        }

        /// <summary>
        /// Comments that stood immediately before this element. Null until the first one
        /// </summary>
        public IList<string> Comments
        {
            get { return m_comments; }
        }

        /// <summary>
        /// Comments that stood after this element's last child, which no following element can
        /// carry. Null until the first one
        /// </summary>
        public IList<string> TrailingComments
        {
            get { return m_trailingComments; }
        }

        public WidgetMarkup(string source)
        {
            m_source = source;
        }

        public void AddAttribute(string name, string value)
        {
            if (m_attributes == null)
                m_attributes = new List<KeyValuePair<string, string>>();

            m_attributes.Add(new KeyValuePair<string, string>(name, value));
        }

        public void SetStyleAttribute(string name, string value)
        {
            if (m_styleAttributes == null)
                m_styleAttributes = new Dictionary<string, string>();

            m_styleAttributes[name] = value;
        }

        public void AddComment(string text)
        {
            if (m_comments == null)
                m_comments = new List<string>();

            m_comments.Add(text);
        }

        public void AddTrailingComment(string text)
        {
            if (m_trailingComments == null)
                m_trailingComments = new List<string>();

            m_trailingComments.Add(text);
        }
    }

    /// <summary>
    /// XHTML markup support: build a widget tree from a document, and write one back out.
    ///
    /// Element names are real HTML tags wherever a sensible one exists, so the same file opens
    /// in an HTML editor and in a browser. A widget a document built <b>reports the document's
    /// own tag</b> as its <see cref="Widget.StyleElementType"/>: <c>&lt;div&gt;</c> builds a
    /// <see cref="WidgetPanel"/> that answers <c>div</c>, <c>&lt;h1&gt;</c> builds a
    /// <see cref="WidgetLabel"/> that answers <c>h1</c>. The raw tag, never the registration
    /// selector -- <c>&lt;input type="checkbox"&gt;</c> answers <c>input</c>, because an author
    /// writing <c>input { }</c> means every input and <c>checkbox</c> is not an HTML element.
    ///
    /// A widget built in code is untouched and still answers the <c>ElementType</c> const of
    /// its class, so every stylesheet written against <c>panel</c> or <c>label</c> keeps
    /// matching exactly what it matched before. The two vocabularies do not mix: a
    /// <c>label { }</c> rule does not reach a <c>&lt;span&gt;</c>, and a user interface is
    /// designed in HTML or in code rather than in both.
    ///
    /// The <see cref="WidgetType"/> recorded next to each registration is the element type the
    /// widget class is expected to declare for that tag. It is checked against what a freshly
    /// built widget reports -- before the tag replaces it -- so the table cannot drift away
    /// from the classes it names; a mismatch is logged rather than thrown.
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

        // What <input type="password"> means to a WidgetTextEdit. The widget takes a whole string
        // and the attribute says only that the text is hidden, so one character has to be picked
        // here; a mask of any other string still saves as type="password" and loads back as this
        private const string PasswordMaskChar = "*";

        private static readonly char[] s_classSeparators = new char[] { ' ', '\t', '\r', '\n' };

        // the six heading elements, all one widget. Registered from a loop rather than six
        // lines, because nothing distinguishes them here: a heading is a line of text and the
        // stylesheet says how big
        private static readonly string[] s_headingElements = new string[] { "h1", "h2", "h3", "h4", "h5", "h6" };

        // one entry per element met while loading that carried a for="...", resolved once the
        // whole tree exists because either end of the association may stand after the other.
        // The key is the element that carried the attribute -- a checkbox naming its label,
        // which is this engine's own spelling, or a <label> naming its control, which is HTML's
        private static readonly IList<KeyValuePair<Widget, string>> s_markupLabelLinks = new List<KeyValuePair<Widget, string>>();

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
        /// The built-in element table. The rule it follows is that the engine adapts to HTML
        /// and never the other way round: the owner opens the document in an HTML editor,
        /// deletes a control and inserts a new one from the toolbar, and that toolbar emits
        /// standard elements. It cannot be taught a convention, so every element it produces
        /// has to load, and each one has to save back as the tag it came from.
        ///
        /// Where two tags mean one widget both are registered, and the widget remembers which
        /// one built it in <see cref="Widget.Markup"/>. <b>The last registration for a widget
        /// class is the tag the saver gives a widget that no document built</b>, which is the
        /// only thing registration order decides.
        ///
        /// An element that is in no table at all is not skipped. It becomes a
        /// <see cref="WidgetMarkupElement"/>, a panel named by its own tag, and its children
        /// load into it -- so <c>&lt;section&gt;</c>, <c>&lt;form&gt;</c>,
        /// <c>&lt;fieldset&gt;</c> and every other wrapper an editor writes keeps the controls
        /// inside it. See that class for what such a wrapper does about geometry.
        ///
        /// Three of the earlier table's refusals are reversed here.
        /// <c>&lt;textarea&gt;</c> was refused because XHTML 1.0 Strict declares <c>rows</c>
        /// and <c>cols</c> <c>#REQUIRED</c> and the saver emitted neither; the saver now
        /// derives both from the field's own size and font, and keeps the authored pair when
        /// the document had one. <c>&lt;img&gt;</c> was refused because the saver had no
        /// <c>src</c> to give it, so a browser drew a broken-image placeholder over the CSS
        /// background holding the real picture; <c>src</c> now maps to
        /// <see cref="WidgetImage.Image"/> in both directions. <c>&lt;label&gt;</c> was refused
        /// because the saver mapped one tag per widget class and <c>span</c> had to win; the
        /// saver now reads the tag off the widget, so both load and both save as themselves.
        ///
        /// <c>&lt;div class="image"&gt;</c> stays registered and stays the form the saver
        /// invents, because <c>src</c> names a sprite: when the picture comes from an atlas
        /// through the CSS <c>background-image</c> of D133's spritesheet idiom there is no URL
        /// to put in a <c>src</c>, and a browser given one it cannot fetch draws a placeholder
        /// over the picture. A document that writes <c>&lt;img src="..."&gt;</c> has said the
        /// picture has a URL, and gets that element back unchanged.
        ///
        /// A window is <c>&lt;dialog&gt;</c> now, a real tag rather than a qualifier class on
        /// <c>&lt;div&gt;</c>. HTML5's <c>dialog:not([open]) { display: none }</c> and its other
        /// user-agent defaults are neutralised on the browser side, in
        /// <c>runmobile_design.js</c> under the row-9 browser-quirks workarounds; the engine
        /// carries none of that, so this table treats it as a plain tag registration like
        /// <c>&lt;label&gt;</c> or <c>&lt;textarea&gt;</c>.
        ///
        /// ponytail: the qualifier-class approach below (<c>div[class=image]</c>,
        /// <c>div[class=textfield]</c>) uses an ordinary class name, so a panel a game gave one
        /// of those classes to would save as <c>&lt;div class="..."&gt;</c> and load back as
        /// that widget. The ceiling is one reserved name per <c>div</c>-backed widget. Upgrade
        /// path: refuse the qualifier names in <c>Widget.AddStyleClass</c>, or move the marker
        /// to an attribute of its own once the document stops having to be XHTML 1.0 Strict.
        ///
        /// Not registered on purpose: <c>WidgetToolbar</c> and <c>WidgetScrollView</c>, because
        /// both arrange their children themselves and this loader is absolute-position only;
        /// <c>WidgetTooltip</c>, which is runtime chrome rather than document content; and
        /// <c>WidgetSelect</c> and <c>WidgetList</c>, which are the two the brief asked
        /// about by name. Both live in <c>Controls/Experimental/</c> and
        /// <c>WidgetSelect</c> reaches outside the library for
        /// <c>RunMobile.Utility.SimpleListDictionary</c>, which the headless test project
        /// cannot compile -- wiring it would put the element table beyond the reach of the
        /// suite that governs it. <c>WidgetList</c> is a <c>WidgetTable</c>, which is a
        /// <c>WidgetScrollView</c> that lays its rows out itself, so it is excluded for the
        /// same reason the toolbar is. <c>&lt;select&gt;</c> therefore takes the unknown-element
        /// path and keeps its <c>&lt;option&gt;</c> children as boxes of their own, which loses
        /// no part of the document.
        /// </summary>
        static WidgetManager()
        {
            RegisterElement<WidgetPanel>("div", WidgetType.Panel, delegate (WidgetStyle style) { return new WidgetPanel(style); });
            RegisterElement<WidgetWindow>("dialog", WidgetType.Window, delegate (WidgetStyle style) { return new WidgetWindow(style); });

            // a heading is a short line of text, which is what a WidgetLabel is. <span> is
            // registered last, so it is the tag a label built in code is written as
            RegisterElement<WidgetLabel>("label", WidgetType.Label, delegate (WidgetStyle style) { return new WidgetLabel(style); });

            foreach (string heading in s_headingElements)
                RegisterElement<WidgetLabel>(heading, WidgetType.Label, delegate (WidgetStyle style) { return new WidgetLabel(style); });

            RegisterElement<WidgetLabel>("span", WidgetType.Label, delegate (WidgetStyle style) { return new WidgetLabel(style); });

            RegisterElement<WidgetText>("p", WidgetType.Text, delegate (WidgetStyle style) { return new WidgetText(style); });

            // <a> is clickable text and WidgetButton is the only widget here that is. A browser
            // paints a link from its own stylesheet and this engine has no underline, so the
            // profile's rule holds: the stylesheet says what a control looks like. Giving it the
            // button element type is deliberate -- a link and a button are one control here
            RegisterElement<WidgetButton>("input[type=submit]", WidgetType.Button, delegate (WidgetStyle style) { return new WidgetButton(style); });
            RegisterElement<WidgetButton>("input[type=reset]", WidgetType.Button, delegate (WidgetStyle style) { return new WidgetButton(style); });
            RegisterElement<WidgetButton>("input[type=button]", WidgetType.Button, delegate (WidgetStyle style) { return new WidgetButton(style); });
            RegisterElement<WidgetButton>("a", WidgetType.Button, delegate (WidgetStyle style) { return new WidgetButton(style); });
            RegisterElement<WidgetButton>("button", WidgetType.Button, delegate (WidgetStyle style) { return new WidgetButton(style); });

            RegisterElement<WidgetImage>("img", WidgetType.Image, delegate (WidgetStyle style) { return new WidgetImage(style); });
            RegisterElement<WidgetImage>("div[class=image]", WidgetType.Image, delegate (WidgetStyle style) { return new WidgetImage(style); });

            RegisterElement<WidgetLine>("hr", WidgetType.Line, delegate (WidgetStyle style) { return new WidgetLine(style); });

            // every other input type -- text, password, email, search, tel, url, number, and an
            // input with no type at all -- falls through to this one, which is what a browser
            // does with a type it does not recognise
            RegisterElement<WidgetTextEdit>("input", WidgetType.TextEdit, delegate (WidgetStyle style) { return new WidgetTextEdit(style); });

            RegisterElement<WidgetTextField>("div[class=textfield]", WidgetType.TextField, delegate (WidgetStyle style) { return new WidgetTextField(style); });
            RegisterElement<WidgetTextField>("textarea", WidgetType.TextField, delegate (WidgetStyle style) { return new WidgetTextField(style); });

            // a radio button is a checkbox that a group is supposed to keep exclusive. Nothing
            // here models the group, so the two are one widget and the tag round-trips
            RegisterElement<WidgetCheckBox>("input[type=radio]", WidgetType.CheckBox, delegate (WidgetStyle style) { return new WidgetCheckBox(style); });
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

            // ponytail: the pending for="..." list is a static, so two loads cannot be in flight
            // at once. Nothing in this engine loads a document from inside another one; the
            // upgrade path is to thread the list through LoadMarkupChildren as a parameter.
            s_markupLabelLinks.Clear();

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

            ResolveMarkupLabelLinks(parent);
        }

        /// <summary>
        /// Ties every element that carried a <c>for="..."</c> to the element it named, now that
        /// the whole tree exists. An association in markup may point forwards, and the sample's
        /// own document writes the checkbox before its label.
        ///
        /// Both directions are accepted. HTML puts the attribute on the <c>&lt;label&gt;</c>,
        /// naming the control, and that is what an editor emits; this engine has always put it
        /// on the checkbox, naming the label, and documents written that way keep working. A
        /// name nothing answers to is logged and skipped, the same tolerance the loader shows
        /// an unknown element
        /// </summary>
        private static void ResolveMarkupLabelLinks(IWindowContainer parent)
        {
            foreach (KeyValuePair<Widget, string> link in s_markupLabelLinks)
            {
                WidgetCheckBox check = link.Key as WidgetCheckBox;

                if (check != null)
                {
                    WidgetLabel label;

                    if (WidgetPanel.TryFind(parent, link.Value, out label))
                        check.LinkedLabel = label;
                    else
                        WindowController.Instance.LogMessage("Checkbox #{0} is for=\"{1}\" but no label with that id was loaded", check.StyleId, link.Value);

                    continue;
                }

                WidgetCheckBox named;

                if (WidgetPanel.TryFind(parent, link.Value, out named))
                {
                    named.LinkedLabel = (WidgetLabel)link.Key;
                    continue;
                }

                // HTML puts for="..." on every label that names a control, and an editor writes
                // it for a text field as readily as for a checkbox. Only a checkbox does
                // anything with it here, so a label naming anything else is ordinary markup with
                // no behaviour behind it and is left alone. A name nothing answers to at all is
                // a mistake in the document and is still reported; the second walk is on this
                // failure path only, the way Find<T> tells its two failures apart
                Widget target;

                if (!WidgetPanel.TryFind(parent, link.Value, out target))
                    WindowController.Instance.LogMessage("Label #{0} is for=\"{1}\" but nothing with that id was loaded", link.Key.StyleId, link.Value);
            }

            s_markupLabelLinks.Clear();
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
            // comments met since the last element. Allocated only when a document has any,
            // which most do not
            List<string> comments = null;

            foreach (HtmlNode node in parentNode.Children)
            {
                if (node.IsComment)
                {
                    if (comments == null)
                        comments = new List<string>();

                    comments.Add(node.Text);
                    continue;
                }

                Widget widget = CreateMarkupWidget(node);

                if (widget == null)
                    continue; // either a game's own factory answered with nothing (already
                              // logged), or the element is <script>, which is skipped by design
                              // and silently (see CreateMarkupWidget)

                // a comment belongs to the element it stands in front of, so that inserting or
                // deleting an element in an editor moves its comment with it
                if (comments != null)
                {
                    foreach (string comment in comments)
                        widget.Markup.AddComment(comment);

                    comments = null;
                }

                // added before the children are walked, so a child resolving its own style sees
                // the whole ancestor chain the document declared
                parent.AddChild(widget);

                if (node.Children.Count == 0)
                    continue;

                IWindowContainer container = widget as IWindowContainer;

                if (container == null)
                    WindowController.Instance.LogMessage("Element <{0}> cannot have children, {1} node(s) inside it skipped", node.Element, node.Children.Count);
                else
                    LoadMarkupChildren(node, container);
            }

            // a comment after the last element has no element to lead, so it belongs to the
            // container itself and is written back after that container's children
            if (comments == null)
                return;

            Widget owner = parent as Widget;

            if (owner == null)
            {
                WindowController.Instance.LogMessage("{0} trailing comment(s) stood inside something that is not a widget and were not kept", comments.Count);
                return;
            }

            if (owner.Markup == null)
                owner.Markup = new WidgetMarkup(null);

            foreach (string comment in comments)
                owner.Markup.AddTrailingComment(comment);
        }

        /// <summary>
        /// Builds the widget for one element. An element in no table becomes a
        /// <see cref="WidgetMarkupElement"/> rather than being skipped: skipping used to take
        /// every control nested inside it as well, so wrapping two controls in a
        /// <c>&lt;section&gt;</c> made both disappear, and a document is what the owner edits
        /// </summary>
        private static Widget CreateMarkupWidget(HtmlNode node)
        {
            // <script> is a named exception to that rule, not a change to it. D157 is about an
            // unknown *container* -- a tag the author used to group real UI, which must keep
            // laying out its children. <script> groups no UI at all; it is instrumentation for
            // the browser preview (see NewWidgets.RunMobileSample/assets/login.xhtml) that must
            // by definition be invisible to this engine. Returning null here, before any of the
            // widget/style machinery below runs, means: no widget, no log, and -- because
            // LoadMarkupChildren only recurses into a node's children when CreateMarkupWidget
            // returned one -- an inline script body (ordinary text content on this node, per
            // HtmlNode) is never touched either.
            if (node.Element == "script")
                return null;

            string[] classes = null;
            string classAttribute = node.Class;

            if (!string.IsNullOrEmpty(classAttribute))
                classes = classAttribute.Split(s_classSeparators, StringSplitOptions.RemoveEmptyEntries);

            string id = node.Id;
            WidgetStyle style = new WidgetStyle(classes, id == null ? string.Empty : id);

            MarkupElement element;
            Widget widget;
            string source;

            if (TryGetMarkupElement(node, out element))
            {
                widget = element.Factory(style);
                source = element.Selector;

                if (widget == null)
                {
                    WindowController.Instance.LogMessage("The factory registered for <{0}> built nothing, element skipped", node.Element);
                    return null;
                }

                // read before the tag replaces it below: this checks the table against the
                // widget classes it names, which is a registration mistake and not a styling one
                if (widget.StyleElementType != element.ElementType)
                    WindowController.Instance.LogMessage("Element <{0}> is registered as element type '{1}' but the widget it builds declares '{2}'; one of the two is wrong",
                        node.Element, element.ElementType, widget.StyleElementType);
            }
            else
            {
                // the same tolerance the CSS parser shows an unknown property: a document a
                // browser renders must stay loadable here, and must stay whole
                WindowController.Instance.LogMessage("Got unknown element <{0}> in XHTML document, kept as a container of its own", node.Element);

                widget = new WidgetMarkupElement(node.Element, style);
                source = node.Element;
                element = new MarkupElement(source, node.Element, null, null, null, null);
            }

            widget.Markup = new WidgetMarkup(source);

            // the document's own tag is the element type from here on, so the author's
            // div/span/h1/input rules match and the class's own name stops being visible. Set
            // before anything resolves a style: nothing has, the widget is not in a tree yet
            widget.StyleElementType = node.Element;

            ApplyMarkupStyle(widget, node.GetAttribute("style"));
            ApplyMarkupText(widget, GetMarkupNodeText(node));
            ApplyMarkupAttributes(widget, node, element.AttributeName);
            widget.SetCodePositionFlag(false);

            return widget;
        }

        /// <summary>
        /// Everything an element says that is not its id, its classes or its text. An attribute
        /// the engine has a property for is read into that property; every other one is kept
        /// verbatim on <see cref="Widget.Markup"/> so that saving hands the author's own file
        /// back. Without that, a save would strip an editor's bookkeeping -- <c>lang</c>,
        /// <c>tabindex</c>, <c>data-</c> attributes, <c>alt</c>, <c>href</c>, a
        /// <c>&lt;textarea&gt;</c>'s <c>rows</c> and <c>cols</c> -- every time the engine
        /// touched the document.
        ///
        /// Two attributes are read <b>and</b> kept: <c>style</c>, because a widget's own style
        /// has a reader for one property at a time and no way to enumerate what was set, so
        /// the text is the only thing that can be written back; and <c>for</c>, because HTML
        /// puts it on the <c>&lt;label&gt;</c> and this engine has always put it on the
        /// checkbox, and it has to come back out where the author wrote it.
        ///
        /// ponytail: a kept attribute is text and is never re-read, so changing
        /// <see cref="WidgetCheckBox.LinkedLabel"/> or the widget's own style in code does not
        /// change what the next save writes for <c>for</c> or <c>style</c>. The ceiling is
        /// those two attributes on documents that code also edits; the upgrade path is to make
        /// the own style enumerable and drop both from the kept set.
        /// </summary>
        private static void ApplyMarkupAttributes(Widget widget, HtmlNode node, string qualifierName)
        {
            string title = node.GetAttribute("title");

            if (title != null)
                widget.Tooltip = title;

            // presence is what counts, not the value: XHTML forbids the minimized form, so a
            // hand-written document says disabled="disabled", but an editor may write disabled=""
            if (node.GetAttribute("disabled") != null)
                widget.Enabled = false;

            string source = node.GetAttribute("src");
            WidgetImage image = widget as WidgetImage;

            if (image != null && !string.IsNullOrEmpty(source))
                image.Image = source;

            WidgetTextEdit edit = widget as WidgetTextEdit;

            if (edit != null && node.GetAttribute("type") == "password")
                edit.MaskChar = PasswordMaskChar;

            WidgetCheckBox check = widget as WidgetCheckBox;

            if (check != null && node.GetAttribute("checked") != null)
                check.Checked = true;

            string linked = node.GetAttribute("for");

            // either end may carry the association: a checkbox naming its label, or -- the form
            // HTML defines and an editor emits -- a label naming its control
            if (!string.IsNullOrEmpty(linked) && (check != null || (widget is WidgetLabel && node.Element == "label")))
                s_markupLabelLinks.Add(new KeyValuePair<Widget, string>(widget, linked));

            foreach (KeyValuePair<string, string> attribute in node.Attributes)
            {
                // kept for style matching regardless of whether a property already models it --
                // see WidgetMarkup.StyleAttributes for why this is a second copy and not a
                // filtered view of the one below
                widget.Markup.SetStyleAttribute(attribute.Key, attribute.Value);

                if (!IsMarkupAttributeModelled(widget, node, qualifierName, attribute.Key, attribute.Value))
                    widget.Markup.AddAttribute(attribute.Key, attribute.Value);
            }
        }

        /// <summary>
        /// True when the attribute has a widget property behind it, so the saver writes it back
        /// from the widget and it must not also be kept verbatim. Everything else is kept
        /// </summary>
        private static bool IsMarkupAttributeModelled(Widget widget, HtmlNode node, string qualifierName, string name, string value)
        {
            switch (name)
            {
                case "id":
                case "class":
                case "title":
                case "disabled":
                    return true;
                case "value":
                    return node.Element == "input"; // where a void element keeps its text
                case "src":
                    return widget is WidgetImage;
                case "checked":
                    return widget is WidgetCheckBox;
                case "type":
                    // the attribute that picked this widget out of the table, or the one thing
                    // a text edit reads from a type it was not registered under
                    return name == qualifierName || value == "password";
                default:
                    return false;
            }
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

        /// <summary>
        /// The element's text goes into the widget's own Text property where it has one, and is
        /// kept verbatim where it has none -- which is every unknown element, so an
        /// <c>&lt;option&gt;</c> inside a <c>&lt;select&gt;</c> keeps its wording through a save
        /// even though nothing here draws it
        /// </summary>
        private static void ApplyMarkupText(Widget widget, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            PropertyInfo property = GetMarkupTextProperty(widget.GetType());

            if (property == null)
                widget.Markup.Text = text;
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
        /// One thing a document can say does not come back out, because a widget tree does not
        /// hold it: the stylesheet links, whose hrefs live in the document head and not on any
        /// widget. A <c>style="..."</c> attribute does come back, but as the text the document
        /// carried rather than as the widget's resolved own style, which
        /// <c>Widget.m_ownStyle</c> has a reader for one property at a time and no way to
        /// enumerate.
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

                WidgetMarkup markup = widget.Markup;
                MarkupElement element;

                if (markup == null || markup.Source == null)
                {
                    // built in code, so the tag is the last one registered for its class
                    string selector = GetMarkupSelector(widget.GetType());

                    if (selector == null)
                    {
                        WindowController.Instance.LogMessage("Widget class {0} has no registered element, skipped while saving", widget.GetType().Name);
                        continue;
                    }

                    element = s_markupElements[selector];
                }
                else if (!s_markupElements.TryGetValue(markup.Source, out element))
                {
                    // an element in no table, or one whose registration a game has since
                    // replaced: the tag the document used is written back as it stood
                    element = new MarkupElement(markup.Source, markup.Source, null, null, null, null);
                }

                if (markup != null && markup.Comments != null)
                    foreach (string comment in markup.Comments)
                        new HtmlNode(parentNode, HtmlNode.CommentElement, comment);

                string text = GetMarkupText(widget);

                if (text == null && markup != null)
                    text = markup.Text;

                bool isVoid = IsVoidMarkupElement(element.TagName);

                // a void element gets no text, whatever the widget holds: a browser reads what
                // stands between its tags as a sibling node and the document says the wrong
                // thing, even though it still parses back in here
                HtmlNode node = new HtmlNode(parentNode, element.TagName, isVoid ? null : text);

                // attribute order: the tag qualifier first, because it is what picks the widget
                // class, then id, then class, then the properties the widget models, then the
                // text of a void element, then everything the document said that no property
                // here holds. A qualifier that is itself the class attribute joins the class
                // list instead of being written twice, in front of the widget's own classes
                bool isClassQualifier = element.AttributeName == "class";

                WidgetTextEdit edit = widget as WidgetTextEdit;

                if (element.AttributeName != null && !isClassQualifier)
                    node.SetAttribute(element.AttributeName, element.AttributeValue);
                else if (edit != null && !string.IsNullOrEmpty(edit.MaskChar))
                    node.SetAttribute("type", "password"); // no registration of its own: an input is a WidgetTextEdit either way

                if (!string.IsNullOrEmpty(widget.StyleId))
                    node.SetAttribute("id", widget.StyleId);

                string classes = JoinMarkupClasses(isClassQualifier ? element.AttributeValue : null, widget.StyleClasses);

                if (!string.IsNullOrEmpty(classes))
                    node.SetAttribute("class", classes);

                WidgetImage image = widget as WidgetImage;

                if (image != null && !string.IsNullOrEmpty(image.Image))
                    node.SetAttribute("src", image.Image);

                WidgetCheckBox check = widget as WidgetCheckBox;

                // an association is written by id, so a linked label the document never named
                // cannot be saved -- there is nothing to point at. A widget a document built
                // carries the attribute verbatim instead, and writes it back where the author
                // put it, which may be on the <label> rather than here
                if (markup == null && check != null && check.LinkedLabel != null && !string.IsNullOrEmpty(check.LinkedLabel.StyleId))
                    node.SetAttribute("for", check.LinkedLabel.StyleId);

                if (!string.IsNullOrEmpty(widget.Tooltip))
                    node.SetAttribute("title", widget.Tooltip);

                // XHTML has no attribute minimization, so a boolean attribute repeats its name
                if (check != null && check.Checked)
                    node.SetAttribute("checked", "checked");

                if (!widget.Enabled)
                    node.SetAttribute("disabled", "disabled");

                // the two attributes XHTML 1.0 Strict requires on elements this table now uses.
                // Both are written before the kept ones, so a document that carried its own
                // value replaces this one in place rather than adding a second copy
                if (element.TagName == "textarea")
                    SetMarkupTextAreaGrid(node, widget);

                if (element.TagName == "img")
                    node.SetAttribute("alt", string.Empty);

                if (isVoid && !string.IsNullOrEmpty(text))
                {
                    if (element.TagName == "input")
                        node.SetAttribute("value", text);
                    else
                        WindowController.Instance.LogMessage("Element <{0}> is a void element with no attribute to keep text in, so \"{1}\" is not saved", element.TagName, text);
                }

                if (markup != null && markup.Attributes != null)
                    foreach (KeyValuePair<string, string> attribute in markup.Attributes)
                        node.SetAttribute(attribute.Key, attribute.Value);

                IWindowContainer childContainer = widget as IWindowContainer;

                if (childContainer != null)
                    SaveMarkupChildren(childContainer, node);
            }

            // whatever stood after this container's last element, which no element can lead
            Widget owner = container as Widget;

            if (owner == null || owner.Markup == null || owner.Markup.TrailingComments == null)
                return;

            foreach (string comment in owner.Markup.TrailingComments)
                new HtmlNode(parentNode, HtmlNode.CommentElement, comment);
        }

        /// <summary>
        /// XHTML 1.0 Strict declares <c>rows</c> and <c>cols</c> <c>#REQUIRED</c> on
        /// <c>&lt;textarea&gt;</c>, and a missing required attribute is an error no stylesheet
        /// can answer -- a validating editor rejects the whole document over it. So both are
        /// written, and both are derived from the field rather than invented: a row is one line
        /// of the field's own font at its own font size, and a column is one space of it, which
        /// is what the attributes mean. A document that carried its own pair keeps it, because
        /// the kept attributes are written after these and replace them in place.
        ///
        /// ponytail: a proportional font has no column width, so the space advance stands in
        /// for one. The ceiling is a wrong <c>cols</c> in a browser rendering the document with
        /// no stylesheet at all; every document in the D132 profile carries one.
        /// </summary>
        private static void SetMarkupTextAreaGrid(HtmlNode node, Widget widget)
        {
            int rows = 1;
            int columns = 1;

            WidgetTextField field = widget as WidgetTextField;

            if (field != null)
            {
                Font font = field.Font;
                float fontSize = field.FontSize;

                if (font != null && fontSize > 0)
                {
                    float lineHeight = font.Height * fontSize;
                    float columnWidth = font.SpaceWidth * fontSize;

                    if (lineHeight > 0)
                        rows = (int)(field.Size.Y / lineHeight);

                    if (columnWidth > 0)
                        columns = (int)(field.Size.X / columnWidth);
                }
            }

            node.SetAttribute("rows", (rows < 1 ? 1 : rows).ToString());
            node.SetAttribute("cols", (columns < 1 ? 1 : columns).ToString());
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
