using System;
using System.IO;
using System.Numerics;

using NewWidgets.UI;
using NewWidgets.Widgets;

namespace NewWidgets.Test
{
    /// <summary>
    /// Coverage for the XHTML markup loader and saver (<see cref="WidgetManager.LoadXHTML"/>
    /// and <see cref="WidgetManager.SaveXHTML"/>). Every class and id used here starts with
    /// "mk" so nothing collides with the scratch styles of the other groups sharing
    /// WidgetManager's process-wide style collection.
    /// </summary>
    internal static class MarkupTests
    {
        private const float Tolerance = 0.01f;

        // Relative, like the corpus baselines: the suite is run from the project directory
        private const string ConformanceLoginPath = "Conformance/login.xhtml";

        // The round-trip fixture, written in exactly the form SaveXHTML emits: same doctype
        // line, four-space indent per level, attributes in the order tag-qualifier, id, class,
        // value, and an empty element written self-closing. Writing it this way is what lets
        // the test compare whole documents as text instead of comparing a hand-rolled tree dump.
        //
        // Every element here is one a browser and an XHTML 1.0 Strict editor both accept, and
        // a widget that has no HTML element of its own is a <div> whose first class names it --
        // the convention Conformance/login.xhtml is written in.
        private static readonly string[] s_roundTripLines = new string[]
        {
            "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">",
            "<html xmlns=\"http://www.w3.org/1999/xhtml\">",
            "    <head>",
            "        <title>NewWidgets user interface</title>",
            "    </head>",
            "    <body>",
            "        <div id=\"mk_window\" class=\"window mkwindow\">",
            "            <div id=\"mk_back\" class=\"mkpanel mkpattern\"/>",
            "            <span id=\"mk_title\" class=\"mklabel\">Log in</span>",
            "            <input type=\"checkbox\" id=\"mk_check\" class=\"mkcheckbox\"/>",
            "            <input id=\"mk_login\" class=\"mkedit\" value=\"user\"/>",
            "            <button id=\"mk_connect\" class=\"mkbutton\">Connect</button>",
            "            <div id=\"mk_logo\" class=\"image mkimage\"/>",
            "            <div id=\"mk_field\" class=\"textfield mkfield\">first line</div>",
            "            <hr id=\"mk_line\" class=\"mkline\"/>",
            "            <p id=\"mk_text\" class=\"mklabel\">rich &amp; plain</p>",
            "        </div>",
            "    </body>",
            "</html>"
        };

        public static void Register()
        {
            TestRunner.Add("Test 47: XHTML document builds a widget tree", Test47_MarkupBuildsWidgetTree);
            TestRunner.Add("Test 48: XHTML round-trip", Test48_MarkupRoundTrip);
            TestRunner.Add("Test 49: XHTML stylesheet link and inline style", Test49_MarkupStylesAndLinks);
            TestRunner.Add("Test 50: saved XHTML is a document a browser renders", Test50_MarkupOutputIsRenderableHtml);
            TestRunner.Add("Test 51: the login.xhtml reference and the element table agree", Test51_ConformanceLoginDocument);
        }

        private static string GetRoundTripDocument()
        {
            return string.Join(Environment.NewLine, s_roundTripLines);
        }

        // The loader takes a stylesheet-resolver delegate rather than touching the filesystem
        // itself, because the library has no idea where a game keeps its resources. This one
        // answers the single href the tests use and nothing else.
        private static string LoadTestStyleSheet(string href)
        {
            if (href == "mk.css")
                return ".mklinked { width: 321px; height: 21px; }";

            return null;
        }

        private static Widget GetChild(IWindowContainer container, int index)
        {
            int position = 0;

            foreach (WindowObject child in container.Children)
            {
                if (position == index)
                    return child as Widget;

                position++;
            }

            return null;
        }

        private static Widget GetChildById(IWindowContainer container, string id)
        {
            foreach (WindowObject child in container.Children)
            {
                Widget widget = child as Widget;

                if (widget != null && widget.StyleId == id)
                    return widget;
            }

            return null;
        }

        private static int GetChildCount(IWindowContainer container)
        {
            int count = 0;

            foreach (WindowObject child in container.Children)
                count++;

            return count;
        }

        /// <summary>
        /// Test 47 asserts on the widget objects, not on any text. It is the half of the
        /// round-trip that proves the document was really turned into widgets: the right
        /// concrete classes, the NewWidgets element type each one reports (which is what a
        /// stylesheet selector matches on), the id and classes, the text, and the nesting.
        /// Without it, a save that echoed the source string would pass Test 48.
        /// </summary>
        private static void Test47_MarkupBuildsWidgetTree(TestContext context)
        {
            TestEnvironment.Setup();

            WidgetPanel root = new WidgetPanel();

            WidgetManager.LoadXHTML(GetRoundTripDocument(), LoadTestStyleSheet, root);

            context.AreEqual(1, GetChildCount(root), "<body> should contribute exactly one top-level widget");

            WidgetWindow window = GetChild(root, 0) as WidgetWindow;

            context.IsNotNull(window, "<div class=\"window\"> should build a WidgetWindow rather than the panel a bare <div> builds");

            if (window == null)
                return;

            context.AreEqual("window", window.StyleElementType, "<div class=\"window\"> should keep the NewWidgets element type 'window' so a 'window' rule still matches");
            context.AreEqual("mk_window", window.StyleId, "the id attribute should become StyleId");
            context.AreEqual(2, window.StyleClasses.Length, "both class attribute values should become style classes");
            context.AreEqual("window", window.StyleClasses[0], "the element-type class should stay a style class, so a browser's .window rule and NewWidgets match the same element");
            context.AreEqual("mkwindow", window.StyleClasses[1], "the class attribute should become StyleClasses");

            context.AreEqual(9, GetChildCount(window), "every element nested inside the window should become a child widget");

            WidgetPanel back = GetChild(window, 0) as WidgetPanel;
            context.IsNotNull(back, "<div> should build a WidgetPanel");
            if (back != null)
            {
                context.AreEqual("panel", back.StyleElementType, "<div> should keep the element type 'panel'");
                context.AreEqual(2, back.StyleClasses.Length, "a two-value class attribute should become two style classes");
                context.AreEqual("mkpattern", back.StyleClasses[1], "the second class should survive in order");
            }

            WidgetLabel title = GetChild(window, 1) as WidgetLabel;
            context.IsNotNull(title, "<span> should build a WidgetLabel");
            if (title != null)
            {
                context.AreEqual("label", title.StyleElementType, "<span> should keep the element type 'label'");
                context.AreEqual("Log in", title.Text, "element text content should become the widget text");
            }

            WidgetCheckBox check = GetChild(window, 2) as WidgetCheckBox;
            context.IsNotNull(check, "<input type=\"checkbox\"> should build a WidgetCheckBox rather than a text edit");
            if (check != null)
                context.AreEqual("checkbox", check.StyleElementType, "the checkbox should keep the element type 'checkbox'");

            WidgetTextEdit login = GetChild(window, 3) as WidgetTextEdit;
            context.IsNotNull(login, "a bare <input> should build a WidgetTextEdit");
            if (login != null)
            {
                context.AreEqual("textedit", login.StyleElementType, "<input> should keep the element type 'textedit'");
                context.AreEqual("user", login.Text, "an input is a void element, so its text comes from the value attribute and not from content between the tags");
            }

            WidgetButton connect = GetChild(window, 4) as WidgetButton;
            context.IsNotNull(connect, "<button> should build a WidgetButton");
            if (connect != null)
            {
                context.AreEqual("button", connect.StyleElementType, "<button> should keep the element type 'button'");
                context.AreEqual("Connect", connect.Text, "button text content should reach the button label");
            }

            WidgetImage logo = GetChild(window, 5) as WidgetImage;
            context.IsNotNull(logo, "<div class=\"image\"> should build a WidgetImage");
            if (logo != null)
                context.AreEqual("image", logo.StyleElementType, "the image should keep the element type 'image'");

            WidgetTextField field = GetChild(window, 6) as WidgetTextField;
            context.IsNotNull(field, "<div class=\"textfield\"> should build a WidgetTextField rather than the panel a bare <div> builds");
            if (field != null)
            {
                context.AreEqual("first line", field.Text, "text field content should become the field text");
                context.AreEqual("textfield", field.StyleClasses[0], "the qualifier class should stay a style class, so a browser's .textfield rule and NewWidgets match the same element");
            }

            context.IsNotNull(GetChild(window, 7) as WidgetLine, "<hr> should build a WidgetLine");

            WidgetText text = GetChild(window, 8) as WidgetText;
            context.IsNotNull(text, "<p> should build a WidgetText");
            if (text != null)
                context.AreEqual("rich & plain", text.Text, "an XML entity in the text content should be unescaped exactly once");

            context.AreEqual(window, back == null ? null : back.Parent, "a nested element should be parented to the widget of the element that contains it");
        }

        /// <summary>
        /// Test 48 is the round trip. It loads the fixture, saves the resulting widget tree and
        /// compares the saved text with the source text, character for character.
        ///
        /// What that comparison covers: element names, the attribute set and its order, the
        /// nesting depth and sibling order, the text content and its escaping, and the
        /// self-closing form of empty elements. It is meaningful because nothing but the widget
        /// tree crosses between load and save -- the loader keeps no copy of the document -- so
        /// every character that comes back out had to be recoverable from a real widget object.
        /// Test 47 asserts on those objects directly, which rules out the degenerate way of
        /// passing this one.
        ///
        /// The second half saves a second time from a tree reloaded out of the first save. That
        /// catches a loader or saver that is merely self-consistent on the first pass and drifts
        /// afterwards, which a single comparison against a hand-written source cannot see.
        /// </summary>
        private static void Test48_MarkupRoundTrip(TestContext context)
        {
            TestEnvironment.Setup();

            string source = GetRoundTripDocument();

            WidgetPanel firstRoot = new WidgetPanel();
            WidgetManager.LoadXHTML(source, LoadTestStyleSheet, firstRoot);

            string firstSave = WidgetManager.SaveXHTML(firstRoot);

            context.AreEqual(source, firstSave, "saving the tree loaded from the fixture should reproduce the fixture exactly");

            WidgetPanel secondRoot = new WidgetPanel();
            WidgetManager.LoadXHTML(firstSave, LoadTestStyleSheet, secondRoot);

            string secondSave = WidgetManager.SaveXHTML(secondRoot);

            context.AreEqual(firstSave, secondSave, "a second load/save pass should be a fixed point");

            // The other direction the saver exists for: a tree built in code, the way
            // Sample/Sample/TestWindow.cs builds one. This pins what such a tree can and cannot
            // say as markup today, which is the question of whether the login form can move out
            // of code.
            WidgetWindow codeWindow = new WidgetWindow(WidgetManager.GetStyle("mkcode"));
            codeWindow.StyleId = "mk_code_window";

            WidgetLabel codeLabel = new WidgetLabel("Connect to server");
            codeLabel.StyleId = "mk_code_title";
            codeLabel.Size = new Vector2(600, 60);
            codeWindow.AddChild(codeLabel);

            WidgetTextEdit codeEdit = new WidgetTextEdit();
            codeEdit.StyleId = "mk_code_edit";
            codeEdit.Text = "hello";
            codeWindow.AddChild(codeEdit);

            WidgetPanel codeRoot = new WidgetPanel();
            codeRoot.AddChild(codeWindow);

            string codeSave = WidgetManager.SaveXHTML(codeRoot);

            context.IsTrue(codeSave.IndexOf("<div id=\"mk_code_window\" class=\"window mkcode\">", StringComparison.Ordinal) >= 0,
                "a WidgetWindow built in code should save as a <div> whose first class is its element type, plus its id and its own class");
            context.IsTrue(codeSave.IndexOf("<span id=\"mk_code_title\">Connect to server</span>", StringComparison.Ordinal) >= 0,
                "a WidgetLabel built in code should save as <span> carrying its text");
            context.IsTrue(codeSave.IndexOf("<input id=\"mk_code_edit\" value=\"hello\"/>", StringComparison.Ordinal) >= 0,
                "a WidgetTextEdit built in code should carry its text in the value attribute of a self-closed <input>");
            context.IsFalse(codeSave.IndexOf(">hello</input>", StringComparison.Ordinal) >= 0,
                "an input is a void element: a browser parses text written between its tags as a sibling node, not as its content");
            context.IsFalse(codeSave.IndexOf("style=", StringComparison.Ordinal) >= 0,
                "known gap: a size set in code lands in the widget's own style, which the saver cannot enumerate, so no style attribute comes out");
        }

        /// <summary>
        /// Test 49 covers the two document features that are not part of the element tree: the
        /// stylesheet link in the head, and the inline style attribute. Both are asserted
        /// through resolved geometry rather than through the style sheet, because a resolved
        /// size is the only observable proof that the declaration reached the cascade.
        /// It also pins the tolerance rule: an element nobody registered is logged and skipped,
        /// never thrown, the same way an unknown CSS property is.
        /// </summary>
        private static void Test49_MarkupStylesAndLinks(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();

            string[] lines = new string[]
            {
                "<html xmlns=\"http://www.w3.org/1999/xhtml\">",
                "<head>",
                "<title>ignored</title>",
                "<link rel=\"stylesheet\" href=\"mk.css\" />",
                "</head>",
                "<body>",
                "<div id=\"mk_linked\" class=\"mklinked\"/>",
                "<div id=\"mk_inline\" style=\"width: 123px; height: 45px; left: 7px\"/>",
                "<div id=\"mk_bogus\" style=\"no-such-property: 5px\"/>",
                "<marquee id=\"mk_unknown\"><div id=\"mk_orphan\"/><div id=\"mk_orphan2\"/></marquee>",
                "<span id=\"mk_mixed\">a<span id=\"mk_inner\"/>c</span>",
                "</body>",
                "</html>"
            };

            string document = string.Join(Environment.NewLine, lines);

            WidgetPanel root = new WidgetPanel();
            root.Size = new Vector2(1000, 1000);

            controller.ClearLog();

            context.DoesNotThrow(delegate { WidgetManager.LoadXHTML(document, LoadTestStyleSheet, root); },
                "an unknown element must not stop the document from loading");

            context.AreEqual(4, GetChildCount(root), "the unknown <marquee> and its subtree should be skipped, the three known <div> elements and the <span> kept");

            bool loggedUnknownElement = false;
            bool loggedDroppedChildren = false;

            foreach (string message in controller.Messages)
                if (message.IndexOf("marquee", StringComparison.Ordinal) >= 0)
                {
                    loggedUnknownElement = true;

                    if (message.IndexOf("2", StringComparison.Ordinal) >= 0 && message.IndexOf("child", StringComparison.Ordinal) >= 0)
                        loggedDroppedChildren = true;
                }

            context.IsTrue(loggedUnknownElement, "the skipped element should be reported through LogMessage");
            context.IsTrue(loggedDroppedChildren, "the children that go down with a skipped element should be reported too, with their count, instead of vanishing without a word");

            WidgetLabel mixed = GetChild(root, 3) as WidgetLabel;
            context.IsNotNull(mixed, "an element with an element in the middle of its text should still be built");

            if (mixed != null)
                context.AreEqual("ac", mixed.Text, "both halves of a text split by a child element should survive: keeping the first one alone loses the second without saying so");

            Widget linked = GetChild(root, 0);
            context.IsNotNull(linked, "the element carrying the linked stylesheet class should exist");

            if (linked != null)
            {
                linked.Relayout();
                context.AreEqualFloat(321, linked.Size.X, Tolerance, "the width from the linked stylesheet should apply");
                context.AreEqualFloat(21, linked.Size.Y, Tolerance, "the height from the linked stylesheet should apply");
            }

            Widget inline = GetChild(root, 1);
            context.IsNotNull(inline, "the element carrying an inline style should exist");

            if (inline != null)
            {
                inline.Relayout();
                context.AreEqualFloat(123, inline.Size.X, Tolerance, "the width from the style attribute should apply");
                context.AreEqualFloat(45, inline.Size.Y, Tolerance, "the height from the style attribute should apply");
                context.AreEqualFloat(7, inline.Position.X, Tolerance, "the left from the style attribute should apply");
            }

            Widget bogus = GetChild(root, 2);
            context.IsNotNull(bogus, "an element whose inline style names an unknown property should still be built");
        }

        /// <summary>
        /// Test 50 asserts on the saved text as a document rather than as a round trip. The
        /// round trip only proves the saver and the loader agree with each other, and they can
        /// agree on markup no browser renders: <c>&lt;dialog&gt;</c> without an <c>open</c>
        /// attribute is <c>display: none</c> in every user-agent stylesheet, <c>&lt;img&gt;</c>
        /// without a <c>src</c> draws a broken-image placeholder over the CSS background the
        /// picture actually arrives in, and text between the tags of a void element is parsed
        /// as a sibling node. Each of those survives a round trip through XmlDocument intact,
        /// so only a check on the output text can see them.
        /// </summary>
        private static void Test50_MarkupOutputIsRenderableHtml(TestContext context)
        {
            TestEnvironment.Setup();

            WidgetPanel root = new WidgetPanel();
            WidgetManager.LoadXHTML(GetRoundTripDocument(), LoadTestStyleSheet, root);

            string saved = WidgetManager.SaveXHTML(root);

            context.IsFalse(saved.IndexOf("<dialog", StringComparison.Ordinal) >= 0,
                "<dialog> must not be emitted: XHTML 1.0 Strict has no such element, and a browser hides one that carries no open attribute, taking its whole subtree with it");
            context.IsFalse(saved.IndexOf("<img", StringComparison.Ordinal) >= 0,
                "<img> must not be emitted: the saver has no src to give it, so a browser draws a broken-image placeholder in front of the CSS background the picture comes from");
            context.IsFalse(saved.IndexOf("<textarea", StringComparison.Ordinal) >= 0,
                "<textarea> must not be emitted: XHTML 1.0 Strict declares rows and cols #REQUIRED on it, and a text field sized in pixels by the cascade has no character grid to fill them from, so a validating editor rejects every document holding one");

            context.IsFalse(saved.IndexOf("</input>", StringComparison.Ordinal) >= 0,
                "a void element must never be given a closing tag");
            context.IsFalse(saved.IndexOf("</hr>", StringComparison.Ordinal) >= 0,
                "a void element must never be given a closing tag");
            context.IsTrue(saved.IndexOf("value=\"user\"", StringComparison.Ordinal) >= 0,
                "the text of an input should come back out through its value attribute");

            context.IsFalse(saved.IndexOf("<head/>", StringComparison.Ordinal) >= 0,
                "an empty <head/> is not valid XHTML 1.0 Strict, which requires a title inside it");
            context.IsTrue(saved.IndexOf("<title>NewWidgets user interface</title>", StringComparison.Ordinal) >= 0,
                "the head should carry a title with real content: a self-closed <title/> makes an HTML parser read the rest of the document as the title text");

            // the widget that has no HTML element of its own becomes a div whose first class is
            // its NewWidgets element type, which is what Conformance/login.xhtml is written in
            context.IsTrue(saved.IndexOf("<div id=\"mk_window\" class=\"window mkwindow\">", StringComparison.Ordinal) >= 0,
                "a window should save as a <div> carrying the element type as its first class");
            context.IsTrue(saved.IndexOf("<div id=\"mk_logo\" class=\"image mkimage\"/>", StringComparison.Ordinal) >= 0,
                "an image should save as a <div> carrying the element type as its first class, the picture coming from the CSS background");
            context.IsTrue(saved.IndexOf("<div id=\"mk_field\" class=\"textfield mkfield\">first line</div>", StringComparison.Ordinal) >= 0,
                "a text field should save as a <div> carrying its qualifier as its first class, which needs no attribute a widget cannot supply");
        }

        /// <summary>
        /// Test 51 runs the hand-written reference document, <c>Conformance/login.xhtml</c>,
        /// through the real loader. The document was written before the loader existed and the
        /// element table's own doc comment names it as the convention the table follows, so a
        /// disagreement between the two is a fault in both of them at once and nothing else in
        /// the suite can see it: every other group builds its markup from a string literal that
        /// was written to match the table.
        ///
        /// Every element in that file carries its widget's name as a class, so each assertion
        /// below asks the same question -- does the class the author wrote name the widget the
        /// loader builds? A <c>&lt;span class="button"&gt;</c> answers no, because a span is a
        /// label, and that is the fault this test exists to keep out.
        ///
        /// The stylesheet is deliberately left unresolved. WidgetManager's style collection is
        /// process-wide and is not cleared between groups, and login.css declares ordinary names
        /// like <c>.window</c> and <c>.label</c> that the other groups would then see. Nothing
        /// asserted here needs the cascade.
        /// </summary>
        private static void Test51_ConformanceLoginDocument(TestContext context)
        {
            TestEnvironment.Setup();

            context.IsTrue(File.Exists(ConformanceLoginPath), "the reference document {0} should be readable from the test working directory", ConformanceLoginPath);

            if (!File.Exists(ConformanceLoginPath))
                return;

            WidgetPanel root = new WidgetPanel();

            WidgetManager.LoadXHTML(File.ReadAllText(ConformanceLoginPath), null, root);

            context.AreEqual(1, GetChildCount(root), "<body> should contribute exactly one top-level widget");

            WidgetWindow window = GetChild(root, 0) as WidgetWindow;

            context.IsNotNull(window, "<div class=\"window\"> should build a WidgetWindow");

            if (window == null)
                return;

            context.AreEqual(14, GetChildCount(window), "every element inside the window should become a child widget, none of them skipped as unknown");

            context.IsNotNull(GetChildById(window, "login_back") as WidgetPanel, "class=\"panel\" should build a WidgetPanel");
            context.IsNotNull(GetChildById(window, "login_title") as WidgetLabel, "class=\"label\" should build a WidgetLabel");
            context.IsNotNull(GetChildById(window, "login_edit") as WidgetTextEdit, "class=\"textedit\" should build a WidgetTextEdit");
            context.IsNotNull(GetChildById(window, "pass_edit") as WidgetTextEdit, "an input type=\"password\" carrying class=\"textedit\" should build a WidgetTextEdit as well");
            context.IsNotNull(GetChildById(window, "local_check") as WidgetCheckBox, "class=\"checkbox\" should build a WidgetCheckBox");
            context.IsNotNull(GetChildById(window, "logo_image") as WidgetImage, "class=\"image\" should build a WidgetImage");

            WidgetButton connect = GetChildById(window, "login_button") as WidgetButton;

            context.IsNotNull(connect, "class=\"button\" should build a WidgetButton, not the WidgetLabel a <span> builds");

            if (connect != null)
                context.AreEqual("Connect", connect.Text, "the button's text should reach the button");

            context.IsNotNull(GetChildById(window, "website_button") as WidgetButton, "a second class on the element should not stop class=\"button\" building a WidgetButton");

            WidgetTextField field = GetChildById(window, "text_field") as WidgetTextField;

            context.IsNotNull(field, "class=\"textfield\" should build a WidgetTextField, not the WidgetPanel a bare <div> builds");

            if (field != null)
                context.IsTrue(field.Text != null && field.Text.StartsWith("WidgetTextField", StringComparison.Ordinal),
                    "the field's content should reach the widget: a WidgetPanel has no text property, so the whole block would be dropped with only a log line to say so");
        }
    }
}
