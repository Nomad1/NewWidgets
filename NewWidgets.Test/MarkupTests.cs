using System;
using System.Collections.Generic;
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
        // src, title, checked, disabled, value, then everything no widget property models --
        // for="..." among them -- and an empty element written self-closing.
        // Writing it this way is what lets the test compare whole documents as text instead of
        // comparing a hand-rolled tree dump.
        //
        // Every element here is one a browser and an XHTML 1.0 Strict editor both accept, and
        // a widget that has no HTML element of its own is a <div> whose first class names it --
        // the convention Conformance/login.xhtml is written in.
        //
        // Every attribute the element table understands appears exactly once, so the whole-text
        // comparison in Test 48 is what polices them: an attribute the loader drops, or one the
        // saver spells differently, changes a character and fails there. #mk_check names a label
        // that stands after it in the document on purpose -- an association that points forwards
        // is the case a straight-through loader cannot resolve.
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
            "            <input type=\"checkbox\" id=\"mk_check\" class=\"mkcheckbox\" title=\"Use a local server\" checked=\"checked\" for=\"mk_label\"/>",
            "            <input id=\"mk_login\" class=\"mkedit\" value=\"user\"/>",
            "            <button id=\"mk_connect\" class=\"mkbutton\" title=\"Start connection\" disabled=\"disabled\">Connect</button>",
            "            <div id=\"mk_logo\" class=\"image mkimage\" src=\"settings_icon\"/>",
            "            <div id=\"mk_field\" class=\"textfield mkfield\">first line</div>",
            "            <hr id=\"mk_line\" class=\"mkline\"/>",
            "            <p id=\"mk_text\" class=\"mklabel\">rich &amp; plain</p>",
            "            <input type=\"password\" id=\"mk_pass\" class=\"mkedit\" value=\"secret\"/>",
            "            <span id=\"mk_label\" class=\"mklabel\">Local server</span>",
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
            TestRunner.Add("Test 51b: binding a loaded tree to code by id", Test51b_FindWidgetById);
            TestRunner.Add("Test 85: a document written the way an HTML editor writes one", Test85_EditorShapedDocument);
            TestRunner.Add("Test 86: an unknown element keeps its children instead of deleting them", Test86_UnknownElementDegrades);
            TestRunner.Add("Test 87: a round trip keeps what no widget property models", Test87_UnmodelledAttributesAndComments);
            TestRunner.Add("Test 88: the four pseudo-classes drive the widgets a document builds", Test88_PseudoClassStatesDriveWidgets);
            TestRunner.Add("Test 90: a widget's element type is its source tag, and code-built widgets keep theirs", Test90_ElementTypeIsTheSourceTag);
            TestRunner.Add("Test 91: the four text widgets answer to four element names", Test91_TextWidgetsHaveElementNamesOfTheirOwn);
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

            context.AreEqual("div", window.StyleElementType, "<div class=\"window\"> should report the tag the document used, so the author's div rule matches it and the .window class carries the rest");
            context.AreEqual("mk_window", window.StyleId, "the id attribute should become StyleId");
            context.AreEqual(2, window.StyleClasses.Length, "both class attribute values should become style classes");
            context.AreEqual("window", window.StyleClasses[0], "the element-type class should stay a style class, so a browser's .window rule and NewWidgets match the same element");
            context.AreEqual("mkwindow", window.StyleClasses[1], "the class attribute should become StyleClasses");

            context.AreEqual(11, GetChildCount(window), "every element nested inside the window should become a child widget");

            WidgetPanel back = GetChild(window, 0) as WidgetPanel;
            context.IsNotNull(back, "<div> should build a WidgetPanel");
            if (back != null)
            {
                context.AreEqual("div", back.StyleElementType, "<div> should report 'div', not the 'panel' its widget class declares");
                context.AreEqual(2, back.StyleClasses.Length, "a two-value class attribute should become two style classes");
                context.AreEqual("mkpattern", back.StyleClasses[1], "the second class should survive in order");
            }

            WidgetLabel title = GetChild(window, 1) as WidgetLabel;
            context.IsNotNull(title, "<span> should build a WidgetLabel");
            if (title != null)
            {
                context.AreEqual("span", title.StyleElementType, "<span> should report 'span': a label rule belongs to labels built in code");
                context.AreEqual("Log in", title.Text, "element text content should become the widget text");
            }

            WidgetCheckBox check = GetChild(window, 2) as WidgetCheckBox;
            context.IsNotNull(check, "<input type=\"checkbox\"> should build a WidgetCheckBox rather than a text edit");
            if (check != null)
            {
                context.AreEqual("input", check.StyleElementType, "the checkbox should report the raw tag 'input' and not the 'input[type=checkbox]' registration that matched it: an author writing input { } means every input, and checkbox is not an HTML element");
                context.IsTrue(check.Checked, "a checked attribute should set WidgetCheckBox.Checked");
                context.AreEqual("Use a local server", check.Tooltip, "a title attribute should become the widget's tooltip");
            }

            WidgetTextEdit login = GetChild(window, 3) as WidgetTextEdit;
            context.IsNotNull(login, "a bare <input> should build a WidgetTextEdit");
            if (login != null)
            {
                context.AreEqual("input", login.StyleElementType, "<input> should report 'input'");
                context.AreEqual("user", login.Text, "an input is a void element, so its text comes from the value attribute and not from content between the tags");
            }

            WidgetButton connect = GetChild(window, 4) as WidgetButton;
            context.IsNotNull(connect, "<button> should build a WidgetButton");
            if (connect != null)
            {
                context.AreEqual("button", connect.StyleElementType, "<button> should report 'button', which is the one name that is spelled the same in both vocabularies");
                context.AreEqual("Connect", connect.Text, "button text content should reach the button label");
                context.AreEqual("Start connection", connect.Tooltip, "a title attribute should become the widget's tooltip");
                context.IsFalse(connect.Enabled, "a disabled attribute should clear Enabled, which is the same property read the other way round");
            }

            WidgetImage logo = GetChild(window, 5) as WidgetImage;
            context.IsNotNull(logo, "<div class=\"image\"> should build a WidgetImage");
            if (logo != null)
            {
                context.AreEqual("div", logo.StyleElementType, "<div class=\"image\"> should report the raw tag 'div', with .image left to carry what makes it an image");
                context.AreEqual("settings_icon", logo.Image, "a src attribute should name the sprite the image draws");
            }

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

            WidgetTextEdit pass = GetChild(window, 9) as WidgetTextEdit;
            context.IsNotNull(pass, "<input type=\"password\"> should build a WidgetTextEdit, the same widget a bare input builds");
            if (pass != null)
            {
                context.AreEqual("*", pass.MaskChar, "type=\"password\" should set the mask character, which is all a text edit has to say about it");
                context.AreEqual("secret", pass.Text, "masking should not touch the text the widget holds");
            }

            WidgetLabel local = GetChild(window, 10) as WidgetLabel;
            context.IsNotNull(local, "the label a checkbox names should be built like any other <span>");

            if (check != null)
                context.AreEqual(local, check.LinkedLabel, "a for attribute should link the checkbox to the label it names, even though that label stands after it in the document");

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

            context.AreEqual(5, GetChildCount(root), "the three known <div> elements, the unknown <marquee> kept as a container of its own, and the <span>");

            bool loggedUnknownElement = false;

            foreach (string message in controller.Messages)
                if (message.IndexOf("marquee", StringComparison.Ordinal) >= 0)
                    loggedUnknownElement = true;

            context.IsTrue(loggedUnknownElement, "an unknown element should be reported through LogMessage even though it is kept");

            WidgetLabel mixed = GetChild(root, 4) as WidgetLabel;
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
        /// attribute is <c>display: none</c> in every user-agent stylesheet, an
        /// <c>&lt;img&gt;</c> without a <c>src</c> draws a broken-image placeholder over the
        /// CSS background the picture may arrive in, a <c>&lt;textarea&gt;</c> without
        /// <c>rows</c> and <c>cols</c> is invalid XHTML 1.0 Strict, and text between the tags
        /// of a void element is parsed as a sibling node. Each of those survives a round trip
        /// through XmlDocument intact, so only a check on the output text can see them.
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
                "an image the saver invents must stay a <div>: src names a sprite, and a browser given a src it cannot fetch draws a broken-image placeholder over the CSS background the atlas picture comes from");

            // an <img> and a <textarea> the document itself asked for are written back, and
            // each has to carry what XHTML 1.0 Strict requires of it or a validating editor
            // rejects the whole file
            string[] required = new string[]
            {
                "<html xmlns=\"http://www.w3.org/1999/xhtml\">",
                "<body>",
                "<img id=\"mk_req_img\" src=\"picture.png\"/>",
                "<textarea id=\"mk_req_area\">text</textarea>",
                "</body>",
                "</html>"
            };

            WidgetPanel requiredRoot = new WidgetPanel();
            WidgetManager.LoadXHTML(string.Join(Environment.NewLine, required), null, requiredRoot);

            string requiredSaved = WidgetManager.SaveXHTML(requiredRoot);

            context.IsTrue(requiredSaved.IndexOf("<img id=\"mk_req_img\" src=\"picture.png\" alt=\"\"/>", StringComparison.Ordinal) >= 0,
                "an <img> must be given the alt XHTML 1.0 Strict declares #REQUIRED, empty being the spelling for a picture that says nothing");
            context.IsTrue(requiredSaved.IndexOf("rows=\"", StringComparison.Ordinal) >= 0 && requiredSaved.IndexOf("cols=\"", StringComparison.Ordinal) >= 0,
                "a <textarea> must be given the rows and cols XHTML 1.0 Strict declares #REQUIRED, derived from the field rather than left out");

            // a code-built field saves as a real <textarea>, so the derivation has to hold
            // there too -- that tree carries no document to copy a pair of numbers from
            WidgetPanel codeFieldRoot = new WidgetPanel();
            WidgetTextField codeField = new WidgetTextField();
            codeField.StyleId = "mk_code_area";
            codeFieldRoot.AddChild(codeField);

            string codeFieldSaved = WidgetManager.SaveXHTML(codeFieldRoot);

            context.IsTrue(codeFieldSaved.IndexOf("<textarea id=\"mk_code_area\"", StringComparison.Ordinal) >= 0,
                "a WidgetTextField built in code should save as the real element, now that the required attributes can be supplied");
            context.IsTrue(codeFieldSaved.IndexOf("rows=\"", StringComparison.Ordinal) >= 0 && codeFieldSaved.IndexOf("cols=\"", StringComparison.Ordinal) >= 0,
                "the required attributes have to be there whether or not a document supplied them");

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
            context.IsTrue(saved.IndexOf("<div id=\"mk_logo\" class=\"image mkimage\" src=\"settings_icon\"/>", StringComparison.Ordinal) >= 0,
                "an image should save as a <div> carrying the element type as its first class, with the sprite named by src");
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
            WidgetCheckBox local = GetChildById(window, "local_check") as WidgetCheckBox;

            context.IsNotNull(local, "class=\"checkbox\" should build a WidgetCheckBox");

            WidgetLabel localLabel = GetChildById(window, "local_label") as WidgetLabel;

            context.IsNotNull(localLabel, "<label> should build a WidgetLabel, the same widget the <span> elements around it build");

            if (local != null && localLabel != null)
                context.AreEqual(localLabel, local.LinkedLabel,
                    "the document names the association the way HTML does, on the label -- an editor writes it that way and it has to reach the checkbox");
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

        /// <summary>
        /// Test 51b is the other half of loading a document: reaching the widgets it built. Markup
        /// owns the structure and CSS the appearance, so the only handle code has on a control is
        /// the <c>#id</c> the stylesheet already names it by, and without a lookup a loaded dialog
        /// cannot be given a single event handler.
        ///
        /// The failure modes matter as much as the hit. A mistyped id and an id that names a
        /// widget of another class are both mistakes in the two lines the programmer just wrote,
        /// and both have to be reported where they are made rather than handed back as a null to
        /// dereference in an event handler later.
        /// </summary>
        private static void Test51b_FindWidgetById(TestContext context)
        {
            TestEnvironment.Setup();

            WidgetPanel root = new WidgetPanel();
            WidgetManager.LoadXHTML(GetRoundTripDocument(), LoadTestStyleSheet, root);

            WidgetWindow window = root.Find<WidgetWindow>("mk_window");
            context.IsNotNull(window, "a top-level element should be found by its id");

            WidgetButton connect = root.Find<WidgetButton>("mk_connect");
            context.IsNotNull(connect, "an element nested inside another should be found from the root, not only from its own parent");

            if (connect == null)
                return;

            context.AreEqual("Connect", connect.Text, "the widget found by id should be the real one out of the tree, carrying everything the document gave it");
            context.AreEqual(connect, window.Find<WidgetButton>("mk_connect"), "searching from the element's own parent should find the same object");

            // half the controls of a real dialog start hidden -- the sample's own #local_edit
            // does -- so a lookup that skipped them would be useless for binding
            connect.Visible = false;
            context.AreEqual(connect, root.Find<Widget>("mk_connect"), "an invisible widget must still be found: Visible is a runtime state, not a statement about whether the widget exists");
            connect.Visible = true;

            WidgetTextEdit edit;
            context.IsTrue(root.TryFind("mk_login", out edit), "TryFind should report a hit");
            context.AreEqual("user", edit == null ? null : edit.Text, "TryFind should fill its out parameter on a hit");

            context.IsFalse(root.TryFind("mk_nosuch", out edit), "TryFind should report a miss rather than throw, which is what makes it usable where absence is a legitimate answer");
            context.IsNull(edit, "TryFind should clear its out parameter on a miss");

            context.Throws(typeof(ArgumentException), delegate { root.Find<WidgetButton>("mk_conect"); },
                "a mistyped id should throw at the lookup rather than return null for a caller to dereference three screens later");
            context.Throws(typeof(ArgumentException), delegate { root.Find<WidgetButton>("mk_title"); },
                "an id that names a widget of another class should throw as well: it is the same mistake, made in the type instead of the string");
            context.Throws(typeof(ArgumentNullException), delegate { root.Find<Widget>(string.Empty); },
                "an empty id should throw: StyleId is empty on every widget the document did not name, so an empty search would match an arbitrary one of them");
        }

        // The strongest test in this file: a document written the way a mainstream HTML
        // editor's toolbar writes one. Every element here is what such a toolbar inserts --
        // a real <label>, a real <textarea>, a real <img>, a <select> with <option>s, an
        // <h2>, an <a>, an <input type="submit"> -- and the whole thing is wrapped in a
        // <section>, which is the element an editor produces for "group these controls" and
        // which this library has never heard of.
        //
        // It is written in the saver's own output form, so the assertion can be a whole-text
        // comparison. That is what makes it strict: an attribute the loader drops, a tag the
        // saver spells differently, a child the unknown wrapper swallows, all change a
        // character and fail here.
        private static readonly string[] s_editorLines = new string[]
        {
            "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">",
            "<html xmlns=\"http://www.w3.org/1999/xhtml\">",
            "    <head>",
            "        <title>NewWidgets user interface</title>",
            "    </head>",
            "    <body>",
            "        <section id=\"mked_section\" class=\"mkedsection\">",
            "            <h2 id=\"mked_head\">Account</h2>",
            "            <label id=\"mked_label\" for=\"mked_check\">Remember me</label>",
            "            <input type=\"checkbox\" id=\"mked_check\"/>",
            "            <textarea id=\"mked_notes\" rows=\"4\" cols=\"40\">notes</textarea>",
            "            <img id=\"mked_logo\" src=\"logo.png\" alt=\"Logo\"/>",
            "            <select id=\"mked_kind\">",
            "                <option value=\"a\">Alpha</option>",
            "                <option value=\"b\">Beta</option>",
            "            </select>",
            "            <p id=\"mked_text\">Hello</p>",
            "            <a id=\"mked_link\" href=\"http://example.com\">Register</a>",
            "            <input type=\"submit\" id=\"mked_go\" value=\"Go\"/>",
            "            <hr id=\"mked_rule\"/>",
            "            <div id=\"mked_div\"/>",
            "            <span id=\"mked_span\">plain</span>",
            "        </section>",
            "    </body>",
            "</html>"
        };

        private static string GetEditorDocument()
        {
            return string.Join(Environment.NewLine, s_editorLines);
        }

        /// <summary>
        /// Test 85 is the editor-shaped document. The owner will delete a control in an HTML
        /// editor and insert a new one from its toolbar; that toolbar cannot be taught this
        /// library's conventions, so whatever it emits has to load. This asserts on the widget
        /// tree first, because a saver that echoed its input would otherwise pass, and then on
        /// the saved text, character for character.
        /// </summary>
        private static void Test85_EditorShapedDocument(TestContext context)
        {
            TestEnvironment.Setup();

            string source = GetEditorDocument();

            WidgetPanel root = new WidgetPanel();
            WidgetManager.LoadXHTML(source, null, root);

            context.AreEqual(1, GetChildCount(root), "the <section> wrapper should contribute exactly one top-level widget");

            WidgetPanel section = GetChild(root, 0) as WidgetPanel;

            context.IsNotNull(section, "an element this library does not know should still become a container, not disappear");

            if (section == null)
                return;

            context.AreEqual("section", section.StyleElementType,
                "an unknown container should report its own tag as the element type, so a 'section' rule matches it and a 'panel' rule written for real panels does not");
            context.AreEqual(12, GetChildCount(section), "every control the editor put inside the wrapper should be a child widget of it");

            WidgetLabel head = GetChildById(section, "mked_head") as WidgetLabel;
            context.IsNotNull(head, "<h2> should build a WidgetLabel");
            if (head != null)
                context.AreEqual("Account", head.Text, "the heading's text should reach the widget");

            WidgetLabel label = GetChildById(section, "mked_label") as WidgetLabel;
            context.IsNotNull(label, "<label> should build a WidgetLabel, the same widget <span> builds");
            if (label != null)
                context.AreEqual("Remember me", label.Text, "the label's text should reach the widget");

            WidgetCheckBox check = GetChildById(section, "mked_check") as WidgetCheckBox;
            context.IsNotNull(check, "<input type=\"checkbox\"> should build a WidgetCheckBox");
            if (check != null && label != null)
                context.AreEqual(label, check.LinkedLabel,
                    "a <label for=\"...\"> naming the checkbox should link the two, which is the direction HTML writes the association in and the direction an editor emits");

            WidgetTextField notes = GetChildById(section, "mked_notes") as WidgetTextField;
            context.IsNotNull(notes, "<textarea> should build a WidgetTextField");
            if (notes != null)
                context.AreEqual("notes", notes.Text, "the textarea's content should reach the field");

            WidgetImage logo = GetChildById(section, "mked_logo") as WidgetImage;
            context.IsNotNull(logo, "<img> should build a WidgetImage");
            if (logo != null)
                context.AreEqual("logo.png", logo.Image, "the img's src should name the picture the image draws");

            WidgetPanel select = GetChildById(section, "mked_kind") as WidgetPanel;
            context.IsNotNull(select, "<select> should still become a container so its options are not deleted");
            if (select != null)
            {
                context.AreEqual("select", select.StyleElementType, "the select should report its own tag as the element type");
                context.AreEqual(2, GetChildCount(select), "both <option> elements should survive as children");
            }

            context.IsNotNull(GetChildById(section, "mked_text") as WidgetText, "<p> should build a WidgetText");

            WidgetButton link = GetChildById(section, "mked_link") as WidgetButton;
            context.IsNotNull(link, "<a> should build a WidgetButton, which is the only widget here that is clickable text");
            if (link != null)
                context.AreEqual("Register", link.Text, "the link's text should reach the widget");

            WidgetButton go = GetChildById(section, "mked_go") as WidgetButton;
            context.IsNotNull(go, "<input type=\"submit\"> should build a WidgetButton and not the text edit a bare input builds");
            if (go != null)
                context.AreEqual("Go", go.Text, "a submit button carries its text in the value attribute, because an input is a void element");

            context.IsNotNull(GetChildById(section, "mked_rule") as WidgetLine, "<hr> should build a WidgetLine");
            context.IsNotNull(GetChildById(section, "mked_div") as WidgetPanel, "<div> should still build a WidgetPanel");
            context.IsNotNull(GetChildById(section, "mked_span") as WidgetLabel, "<span> should still build a WidgetLabel");

            string firstSave = WidgetManager.SaveXHTML(root);

            context.AreEqual(source, firstSave, "saving the tree loaded from an editor-shaped document should reproduce it exactly, tag for tag and attribute for attribute");

            WidgetPanel secondRoot = new WidgetPanel();
            WidgetManager.LoadXHTML(firstSave, null, secondRoot);

            context.AreEqual(firstSave, WidgetManager.SaveXHTML(secondRoot), "a second load/save pass should be a fixed point");
        }

        /// <summary>
        /// Test 86 is the degradation rule. An element nobody registered used to be logged and
        /// skipped together with everything nested inside it, so wrapping two controls in a
        /// <c>&lt;section&gt;</c> made both disappear. It now becomes a container of its own,
        /// named by its tag, and its children load normally.
        /// </summary>
        private static void Test86_UnknownElementDegrades(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();

            string[] lines = new string[]
            {
                "<html xmlns=\"http://www.w3.org/1999/xhtml\">",
                "<head><title>t</title></head>",
                "<body>",
                "<fieldset id=\"mkun_group\"><span id=\"mkun_one\">one</span><span id=\"mkun_two\">two</span></fieldset>",
                "<br id=\"mkun_break\" />",
                "</body>",
                "</html>"
            };

            string document = string.Join(Environment.NewLine, lines);

            WidgetPanel root = new WidgetPanel();
            root.Size = new Vector2(1000, 1000);

            controller.ClearLog();

            context.DoesNotThrow(delegate { WidgetManager.LoadXHTML(document, null, root); },
                "an unknown element must not stop the document from loading");

            context.AreEqual(2, GetChildCount(root), "the unknown <fieldset> and the unknown <br> should both become widgets");

            WidgetPanel group = GetChildById(root, "mkun_group") as WidgetPanel;

            context.IsNotNull(group, "an unknown element with children should become a container");

            if (group == null)
                return;

            context.AreEqual("fieldset", group.StyleElementType, "the unknown container should be named by its tag");
            context.AreEqual(2, GetChildCount(group), "both children of the unknown wrapper should load: this is the case that used to delete them");
            context.IsNotNull(GetChildById(group, "mkun_one") as WidgetLabel, "a child of an unknown wrapper should be built by the ordinary rules");

            bool logged = false;

            foreach (string message in controller.Messages)
                if (message.IndexOf("fieldset", StringComparison.Ordinal) >= 0)
                    logged = true;

            context.IsTrue(logged, "an unknown element should still be reported through LogMessage, whether or not it was kept");

            // geometry: the wrapper is a box of its own at the origin, so a child keeps the
            // coordinates it would have had without the wrapper. That is what makes wrapping
            // safe under D134's absolute-position profile.
            root.Relayout();
            group.Relayout();

            context.AreEqualFloat(0, group.Position.X, Tolerance, "an unknown wrapper that no rule positions should sit at the origin, so it does not shift its children");
            context.AreEqualFloat(0, group.Position.Y, Tolerance, "an unknown wrapper that no rule positions should sit at the origin, so it does not shift its children");
            context.AreEqualFloat(0, group.Size.X, Tolerance, "and it should have no box of its own, so it draws nothing over what it wraps");

            // and it comes back out as the tag it came in as, children and all
            context.IsTrue(WidgetManager.SaveXHTML(root).IndexOf("<fieldset id=\"mkun_group\">", StringComparison.Ordinal) >= 0,
                "an unknown container should be written back under its own tag, not as the div its widget class would suggest");
        }

        /// <summary>
        /// Test 87 is the property that makes a document safe to edit. An HTML editor writes
        /// bookkeeping of its own -- attributes nothing here models, and comments -- and a save
        /// that dropped them would quietly rewrite the author's file every time the engine
        /// touched it.
        /// </summary>
        private static void Test87_UnmodelledAttributesAndComments(TestContext context)
        {
            TestEnvironment.Setup();

            string[] lines = new string[]
            {
                "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">",
                "<html xmlns=\"http://www.w3.org/1999/xhtml\">",
                "    <head>",
                "        <title>NewWidgets user interface</title>",
                "    </head>",
                "    <body>",
                "        <!-- editor bookkeeping before the dialog -->",
                "        <div id=\"mkat_panel\" class=\"mkatpanel\" style=\"width: 123px\" lang=\"en\" data-editor-id=\"7\" tabindex=\"3\">",
                "            <!-- a note about the field -->",
                "            <input id=\"mkat_edit\" value=\"text\" name=\"login\" placeholder=\"type here\"/>",
                "            <!-- and one at the end of the panel -->",
                "        </div>",
                "        <!-- and one at the end of the body -->",
                "    </body>",
                "</html>"
            };

            string source = string.Join(Environment.NewLine, lines);

            WidgetPanel root = new WidgetPanel();
            WidgetManager.LoadXHTML(source, null, root);

            WidgetPanel panel = GetChildById(root, "mkat_panel") as WidgetPanel;

            context.IsNotNull(panel, "the panel should load whatever else the element says");

            if (panel != null)
            {
                panel.Relayout();
                context.AreEqualFloat(123, panel.Size.X, Tolerance, "a style attribute must still be applied, not merely carried");
            }

            string firstSave = WidgetManager.SaveXHTML(root);

            context.AreEqual(source, firstSave, "an attribute or a comment the engine has no property for must survive a save, in place");

            WidgetPanel secondRoot = new WidgetPanel();
            WidgetManager.LoadXHTML(firstSave, null, secondRoot);

            context.AreEqual(firstSave, WidgetManager.SaveXHTML(secondRoot), "a second load/save pass should be a fixed point");
        }

        /// <summary>
        /// Test 88 is the state audit. A pseudo-class in a stylesheet is worth nothing unless
        /// putting the widget into that state really changes what the widget resolves, so each
        /// case here drives a real control through its own public API -- Hovered, Selected,
        /// Checked, Enabled -- and reads a property back.
        ///
        /// Each state gets a class of its own on purpose. This engine has one state bit behind
        /// :focus, :checked, :selected and :active, so two rules for two of those names on the
        /// same element would be an equal-specificity tie, which Test 23 records as unresolved.
        /// </summary>
        private static void Test88_PseudoClassStatesDriveWidgets(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();

            // a button and a text edit lay their text out through a real LabelObject, so the
            // group needs a font before it can put either of them into a state
            controller.RegisterTestFont("mkstsprite", 10, 16);

            TestEnvironment.LoadCss(
                "@font.mkstfont { --font-resource: url(\"mkstsprite\"); --font-spacing: 0; }" +
                ".mkst_hover { color: #111111; font-family: mkstfont; } .mkst_hover:hover { color: #ff0000; }" +
                ".mkst_focus { color: #111111; font-family: mkstfont; } .mkst_focus:focus { color: #00ff00; }" +
                ".mkst_check { color: #111111; font-family: mkstfont; } .mkst_check:checked { color: #0000ff; }" +
                ".mkst_off { color: #111111; font-family: mkstfont; } .mkst_off:disabled { color: #808080; }");

            string[] lines = new string[]
            {
                "<html xmlns=\"http://www.w3.org/1999/xhtml\">",
                "<head><title>t</title></head>",
                "<body>",
                "<button id=\"mkst_button\" class=\"mkst_hover\">hover me</button>",
                "<input id=\"mkst_edit\" class=\"mkst_focus\" value=\"edit\"/>",
                "<input type=\"checkbox\" id=\"mkst_box\" class=\"mkst_check\"/>",
                "<button id=\"mkst_dead\" class=\"mkst_off\">off</button>",
                "</body>",
                "</html>"
            };

            WidgetPanel root = new WidgetPanel();
            WidgetManager.LoadXHTML(string.Join(Environment.NewLine, lines), null, root);

            // :hover
            Widget button = GetChildById(root, "mkst_button");
            context.IsNotNull(button, "the hover subject should have loaded");

            if (button != null)
            {
                button.Relayout();
                context.AreEqual((uint)0x111111, button.GetProperty(WidgetParameterIndex.TextColor, (uint)0), "the base rule should apply before the widget is hovered");
                button.Hovered = true;
                button.Relayout();
                context.AreEqual((uint)0xff0000, button.GetProperty(WidgetParameterIndex.TextColor, (uint)0), ":hover must change what the widget resolves once it is hovered");
                button.Hovered = false;
            }

            // :focus
            WidgetTextEdit edit = GetChildById(root, "mkst_edit") as WidgetTextEdit;
            context.IsNotNull(edit, "the focus subject should have loaded");

            if (edit != null)
            {
                edit.Relayout();
                context.AreEqual((uint)0x111111, edit.GetProperty(WidgetParameterIndex.TextColor, (uint)0), "the base rule should apply before the edit is focused");
                edit.Selected = true;
                edit.Relayout();
                context.IsTrue(edit.IsFocused, "the widget's own idea of focus should follow the state bit the cascade reads");
                context.AreEqual((uint)0x00ff00, edit.GetProperty(WidgetParameterIndex.TextColor, (uint)0), ":focus must change what the widget resolves once it is focused");
                edit.Selected = false;
            }

            // :checked
            WidgetCheckBox box = GetChildById(root, "mkst_box") as WidgetCheckBox;
            context.IsNotNull(box, "the checked subject should have loaded");

            if (box != null)
            {
                box.Relayout();
                context.AreEqual((uint)0x111111, box.GetProperty(WidgetParameterIndex.TextColor, (uint)0), "the base rule should apply before the box is checked");
                box.Checked = true;
                box.Relayout();
                context.AreEqual((uint)0x0000ff, box.GetProperty(WidgetParameterIndex.TextColor, (uint)0), ":checked must change what a checked box resolves, which is the whole reason a checkbox has a state");
                box.Checked = false;
            }

            // :disabled
            Widget dead = GetChildById(root, "mkst_dead");
            context.IsNotNull(dead, "the disabled subject should have loaded");

            if (dead != null)
            {
                dead.Relayout();
                context.AreEqual((uint)0x111111, dead.GetProperty(WidgetParameterIndex.TextColor, (uint)0), "the base rule should apply while the widget is enabled");
                dead.Enabled = false;
                dead.Relayout();
                context.AreEqual((uint)0x808080, dead.GetProperty(WidgetParameterIndex.TextColor, (uint)0), ":disabled must change what a disabled widget resolves");
                dead.Enabled = true;
            }
        }
    
        /// <summary>
        /// The rule the whole markup design rests on: a widget has <b>one</b> element name.
        /// A document built it, so the name is the tag the document used; code built it, so the
        /// name is its class's own <c>ElementType</c> const, exactly as before.
        ///
        /// Asserted through what the cascade actually matches rather than through the property,
        /// because a name nothing selects on would be decoration. The two vocabularies are shown
        /// not to leak into each other in either direction: a <c>span</c> rule does not reach a
        /// label built in code, and a <c>label</c> rule does not reach one a document wrote as
        /// <c>&lt;span&gt;</c>.
        ///
        /// The last pair is the reason the raw tag is used and not the registration selector
        /// that matched it. <c>&lt;input type="checkbox"&gt;</c> is registered as
        /// <c>input[type=checkbox]</c>, but <c>checkbox</c> is not an element any HTML editor
        /// emits and an author writing <c>input { }</c> means every input, so the checkbox has
        /// to answer <c>input</c> like the rest of them.
        /// </summary>
        private static void Test90_ElementTypeIsTheSourceTag(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();

            // a label lays its text out through a real LabelObject, so the group needs a font
            controller.RegisterTestFont("mktagsprite", 10, 16);

            TestEnvironment.LoadCss(
                "@font.mktagfont { --font-resource: url(\"mktagsprite\"); --font-spacing: 0; }" +
                "span { color: #010101; font-family: mktagfont; }" +
                "label { color: #020202; font-family: mktagfont; }" +
                "div { color: #030303; font-family: mktagfont; }" +
                "input { color: #040404; font-family: mktagfont; }" +
                "h1 { color: #050505; font-family: mktagfont; }" +
                "panel { color: #060606; font-family: mktagfont; }");

            string[] lines = new string[]
            {
                "<html xmlns=\"http://www.w3.org/1999/xhtml\">",
                "<head><title>t</title></head>",
                "<body>",
                "<div id=\"mktag_div\">",
                "<span id=\"mktag_span\">span</span>",
                "<h1 id=\"mktag_h1\">heading</h1>",
                "<label id=\"mktag_label\">label</label>",
                "<input type=\"checkbox\" id=\"mktag_check\"/>",
                "</div>",
                "</body>",
                "</html>"
            };

            WidgetPanel root = new WidgetPanel();
            WidgetManager.LoadXHTML(string.Join(Environment.NewLine, lines), null, root);

            WidgetPanel container = root.Find<WidgetPanel>("mktag_div");

            context.AreEqual("div", container.StyleElementType, "<div> should report its own tag");
            context.AreEqual((uint)0x030303, ResolveColor(container), "a div rule should reach a widget the document wrote as <div>, which a panel rule could not");

            WidgetLabel span = root.Find<WidgetLabel>("mktag_span");

            context.AreEqual("span", span.StyleElementType, "<span> should report its own tag and not the 'label' its class declares");
            context.AreEqual((uint)0x010101, ResolveColor(span), "a span rule should reach it, and the label rule in the same sheet should not");

            WidgetLabel heading = root.Find<WidgetLabel>("mktag_h1");

            context.AreEqual("h1", heading.StyleElementType, "<h1> should report h1, so a heading can be sized without enlarging every label on screen");
            context.AreEqual((uint)0x050505, ResolveColor(heading), "an h1 rule should reach the heading");

            WidgetLabel label = root.Find<WidgetLabel>("mktag_label");

            context.AreEqual("label", label.StyleElementType, "<label> is a real HTML tag, so here the two vocabularies happen to agree");
            context.AreEqual((uint)0x020202, ResolveColor(label), "the label rule should reach the one element that really is a <label>");

            WidgetCheckBox check = root.Find<WidgetCheckBox>("mktag_check");

            context.AreEqual("input", check.StyleElementType, "a checkbox should report the tag, not the input[type=checkbox] registration that built it");
            context.AreEqual((uint)0x040404, ResolveColor(check), "an input rule should reach a checkbox, exactly as it does in a browser");

            // The other half, and the one the golden master also guards: nothing above changed
            // what a widget built in code answers, so every rule Amalthea and SiegeWars ever
            // wrote still matches what it matched.
            WidgetPanel codePanel = new WidgetPanel();
            WidgetLabel codeLabel = new WidgetLabel("built in code");
            WidgetCheckBox codeCheck = new WidgetCheckBox();

            root.AddChild(codePanel);
            codePanel.AddChild(codeLabel);
            codePanel.AddChild(codeCheck);

            context.AreEqual("panel", codePanel.StyleElementType, "a WidgetPanel built in code should still report 'panel'");
            context.AreEqual("label", codeLabel.StyleElementType, "a WidgetLabel built in code should still report 'label'");
            context.AreEqual("checkbox", codeCheck.StyleElementType, "a WidgetCheckBox built in code should still report 'checkbox'");

            context.AreEqual((uint)0x060606, ResolveColor(codePanel), "the panel rule should still reach a panel built in code, and the div rule should not");
            context.AreEqual((uint)0x020202, ResolveColor(codeLabel), "the label rule should still reach a label built in code, and the span rule should not");
        }

        /// <summary>
        /// <see cref="WidgetText"/> used to report <c>label</c> and <see cref="WidgetTextField"/>
        /// used to report <c>textedit</c>, so no selector could tell either pair apart. Each of
        /// the four now answers to a name of its own, and this asserts it the way Test 90 does:
        /// through what the cascade matches, not through the property, because the property
        /// would still read back what was written to it if the element name were wrong.
        /// </summary>
        private static void Test91_TextWidgetsHaveElementNamesOfTheirOwn(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();

            // all four lay their text out through a real LabelObject, so the group needs a font
            controller.RegisterTestFont("txtsplitsprite", 10, 16);

            // only color and font-family are declared, the two properties every one of these
            // rules already carries in both games: this collection is process-wide, and a
            // property the games do not declare would survive into their baselines
            TestEnvironment.LoadCss(
                "@font.txtsplitfont { --font-resource: url(\"txtsplitsprite\"); --font-spacing: 0; }" +
                "label { color: #0a0a0a; font-family: txtsplitfont; }" +
                "text { color: #0b0b0b; font-family: txtsplitfont; }" +
                "textedit { color: #0c0c0c; font-family: txtsplitfont; }" +
                "textfield { color: #0d0d0d; font-family: txtsplitfont; }");

            WidgetLabel label = new WidgetLabel("label");
            WidgetText text = new WidgetText("text");
            WidgetTextEdit textEdit = new WidgetTextEdit();
            WidgetTextField textField = new WidgetTextField();

            context.AreEqual("label", label.StyleElementType, "a WidgetLabel should keep the element name it has always had");
            context.AreEqual("text", text.StyleElementType, "a WidgetText should report an element name of its own and no longer borrow the label's");
            context.AreEqual("textedit", textEdit.StyleElementType, "a WidgetTextEdit should keep the element name it has always had");
            context.AreEqual("textfield", textField.StyleElementType, "a WidgetTextField should report an element name of its own and no longer borrow the text edit's");

            context.AreEqual((uint)0x0a0a0a, ResolveColor(label), "the label rule should reach a WidgetLabel, and the text rule should not");
            context.AreEqual((uint)0x0b0b0b, ResolveColor(text), "the text rule should reach a WidgetText -- it reached nothing at all before the split");
            context.AreEqual((uint)0x0c0c0c, ResolveColor(textEdit), "the textedit rule should reach a WidgetTextEdit, and the textfield rule should not");
            context.AreEqual((uint)0x0d0d0d, ResolveColor(textField), "the textfield rule should reach a WidgetTextField -- it reached nothing at all before the split");

            // The element name lives in two places -- the const above and the WidgetType
            // [Name] attribute the markup table reads -- and CreateMarkupWidget compares them
            // on every element it builds. A document holding the two tags whose names moved
            // proves the second place moved with the first: it would log a mismatch per
            // element otherwise, and nothing but that comparison could catch it.
            string[] lines = new string[]
            {
                "<html xmlns=\"http://www.w3.org/1999/xhtml\">",
                "<head><title>t</title></head>",
                "<body>",
                "<p id=\"txtsplit_p\">paragraph</p>",
                "<textarea id=\"txtsplit_area\" rows=\"2\" cols=\"10\">field</textarea>",
                "</body>",
                "</html>"
            };

            controller.ClearLog();

            WidgetPanel root = new WidgetPanel();
            WidgetManager.LoadXHTML(string.Join(Environment.NewLine, lines), null, root);

            context.AreEqual(0, CountElementTypeMismatches(controller.Messages),
                "the WidgetType [Name] table and the ElementType consts must agree, or every markup-built element logs a mismatch");
        }

        /// <summary>
        /// Counts the "registered as element type ... but the widget it builds declares ..."
        /// messages <see cref="WidgetManager"/> logs when the two element-name vocabularies
        /// disagree.
        /// </summary>
        private static int CountElementTypeMismatches(IList<string> messages)
        {
            int count = 0;

            for (int i = 0; i < messages.Count; i++)
                if (messages[i].IndexOf("is registered as element type", StringComparison.Ordinal) >= 0)
                    count++;

            return count;
        }

        private static uint ResolveColor(Widget widget)
        {
            widget.Relayout();

            return widget.GetProperty(WidgetParameterIndex.TextColor, (uint)0);
        }
    }
}
