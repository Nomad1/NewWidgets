using System;
using System.Numerics;

using System.Collections.Generic;
using System.Xml;
using NewWidgets.UI;
using System.Reflection;
using NewWidgets.Widgets.Styles;

namespace NewWidgets.Widgets
{
    public static class WidgetManager
    {
        private const string DefaultStyleName = "default";
        private const string DefaultPanelStyleName = "default_panel";
        private const string DefaultButtonStyleName = "default_button";
        private const string DefaultImageStyleName = "default_image";
        private const string DefaultLabelStyleName = "default_label";
        private const string DefaultTextEditStyleName = "default_textedit";
        private const string DefaultCheckBoxStyleName = "default_checkbox";
        private const string DefaultWindowStyleName = "default_window";

        // 
        private static float s_fontScale;
        private static Font s_mainFont;

        private static Widget s_currentTooltip;

        // styles
        private static WidgetStyleSheet s_defaultWidgetStyle;
        private static WidgetBackgroundStyleSheet s_defaultWindowStyle;
        private static WidgetBackgroundStyleSheet s_defaultPanelStyle;
        private static WidgetTextStyleSheet s_defaultLabelStyle;
        private static WidgetButtonStyleSheet s_defaultButtonStyle;
        private static WidgetButtonStyleSheet s_defaultCheckBoxStyle;
        private static WidgetTextEditStyleSheet s_defaultTextEditStyle;
        private static WidgetImageStyleSheet s_defaultImageStyle;

        private static readonly Dictionary<string, WidgetStyleSheet> s_styles = new Dictionary<string, WidgetStyleSheet>();
        private static readonly Dictionary<string, Font> s_fonts = new Dictionary<string, Font>();

        // focus
        private static readonly Dictionary<int, IFocusableWidget> s_focusedWidgets = new Dictionary<int, IFocusableWidget>();
        private static readonly LinkedList<Widget> s_exclusiveWidgets = new LinkedList<Widget>();

        // Events

        public static event Widget.TooltipDelegate OnTooltip;

        // Properties
        public static Font MainFont { get { return s_mainFont; } }
        public static float FontScale { get { return s_fontScale; } }

        public static WidgetStyleSheet DefaultWidgetStyle { get { return s_defaultWidgetStyle ?? (s_defaultWidgetStyle = new WidgetStyleSheet(DefaultStyleName, null)); } }
        public static WidgetButtonStyleSheet DefaultCheckBoxStyle { get { return s_defaultCheckBoxStyle ?? (s_defaultButtonStyle = new WidgetButtonStyleSheet(DefaultCheckBoxStyleName, DefaultWidgetStyle)); } }
        public static WidgetButtonStyleSheet DefaultButtonStyle { get { return s_defaultButtonStyle ?? (s_defaultButtonStyle = new WidgetButtonStyleSheet(DefaultButtonStyleName, DefaultWidgetStyle)); } }
        public static WidgetBackgroundStyleSheet DefaultPanelStyle { get { return s_defaultPanelStyle ?? (s_defaultPanelStyle = new WidgetBackgroundStyleSheet(DefaultPanelStyleName, DefaultWidgetStyle)); } }
        public static WidgetTextEditStyleSheet DefaultTextEditStyle { get { return s_defaultTextEditStyle ?? (s_defaultTextEditStyle = new WidgetTextEditStyleSheet(DefaultTextEditStyleName, DefaultWidgetStyle)); } }
        public static WidgetTextStyleSheet DefaultLabelStyle { get { return s_defaultLabelStyle ?? (s_defaultLabelStyle = new WidgetTextStyleSheet(DefaultLabelStyleName, DefaultWidgetStyle)); } }
        public static WidgetBackgroundStyleSheet DefaultWindowStyle { get { return s_defaultWindowStyle ?? (s_defaultWindowStyle = new WidgetBackgroundStyleSheet(DefaultWindowStyleName, DefaultWidgetStyle)); } }
        public static WidgetImageStyleSheet DefaultImageStyle { get { return s_defaultImageStyle ?? (s_defaultImageStyle = new WidgetImageStyleSheet(DefaultImageStyleName, DefaultWidgetStyle)); } }

        //

        public static WidgetStyleSheet GetStyle(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                WidgetStyleSheet result;
                if (s_styles.TryGetValue(name, out result))
                    return result;
            }

            WindowController.Instance.LogError("WidgetManager got GetStyle request for not existing style {0}", name);

            return null;  // TODO: return default style to avoid crash?
        }

        public static Font GetFont(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                Font result;
                if (s_fonts.TryGetValue(name, out result))
                    return result;
            }

            WindowController.Instance.LogError("WidgetManager got GetStyle for not existing font {0}", name);

            return null; // TODO: return default font to avoid crash?
        }

        /// <summary>
        /// Initialization method. It should be called before any widget activity is performed
        /// </summary>
        /// <param name="fontScale">Font scale.</param>
        public static void Init(float fontScale)
        {
            s_fontScale = (int)(8 * fontScale + 0.5f) / 8.0f; 
            s_styles.Clear();

            WindowController.Instance.OnTouch += HandleTouch;
        }

        public static void LoadUI(string uiData)
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
                                    RegisterFont(node);
                                    break;
                                case "nine":
                                    RegisterNinePatch(node);
                                    break;
                                case "three":
                                    RegisterThreePatch(node);
                                    break;
                                case "style":
                                    RegisterStyle(node);
                                    break;
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                WindowController.Instance.LogError("Error loading ui data: " + ex);
                throw;
            }
        }

        private static bool HandleTouch(float x, float y, bool press, bool unpress, int pointer)
        {
            if (s_currentTooltip != null && !s_currentTooltip.HitTest(x, y))
                HandleTooltip(null, null, Vector2.Zero, null);

            if (s_exclusiveWidgets.Count > 0)
            {
//                s_exclusiveWidgets.Last.Value.Touch(x, y, press, unpress, pointer);
                return s_exclusiveWidgets.Last.Value.Touch(x, y, press, unpress, pointer);
            }
            
            return false;
        }

        private static void RegisterFont(XmlNode node)
        {
            string name = node.Attributes.GetNamedItem("name").Value;
            string resource = node.Attributes.GetNamedItem("resource").Value;
            int spacing = int.Parse(node.Attributes.GetNamedItem("spacing").Value);
            int baseline = int.Parse(node.Attributes.GetNamedItem("baseline").Value);

            int leading = 0;

            if (node.Attributes.GetNamedItem("leading") != null)
                leading = int.Parse(node.Attributes.GetNamedItem("leading").Value);

            //IWindowController.Instance.RegisterSpriteTemplate(resource, 1.0f, 0.0f, 0.0f, 0);
            //IWindowController.Instance.RegisterSpriteAnimation(resource, 0, resource, resource, 0, 0, 0, 0);

            Font font = new Font(resource, spacing, leading, baseline);

            s_fonts[name] = font;

            if (name == "default")
                s_mainFont = font;

            WindowController.Instance.LogMessage("Registered font {0}, resource {1}, spacing {2}", name, resource, spacing);
        }

        private static void RegisterNinePatch(XmlNode node)
        {
            string name = node.Attributes.GetNamedItem("name").Value;

            WindowController.Instance.SetSpriteSubdivision(name, 3, 3);

            WindowController.Instance.LogMessage("Registered nine patch {0}", name);
        }

        private static void RegisterThreePatch(XmlNode node)
        {
            string name = node.Attributes.GetNamedItem("name").Value;

            WindowController.Instance.SetSpriteSubdivision(name, 3, 1);

            WindowController.Instance.LogMessage("Registered three patch {0}", name);
        }

        private static void RegisterStyle(XmlNode node)
        {
            string name = node.Attributes.GetNamedItem("name").Value;

            string @class = node.Attributes.GetNamedItem("class").Value;

            WidgetStyleSheet parent = node.Attributes.GetNamedItem("parent") == null ? null : GetStyle(node.Attributes.GetNamedItem("parent").Value);

            if (parent == null)
                parent = DefaultWidgetStyle;

            WidgetStyleSheet style = null;

            if (!string.IsNullOrEmpty(@class))
            {
                switch (@class.ToLower())
                {
                    case "image":
                        style = new WidgetImageStyleSheet(name, parent);
                        break;
                    case "background":
                    case "panel":
                    case "window":
                        style = new WidgetBackgroundStyleSheet(name, parent);
                        break;
                    case "label":
                    case "text":
                        style = new WidgetTextStyleSheet(name, parent);
                        break;
                    case "textedit":
                        style = new WidgetTextEditStyleSheet(name, parent);
                        break;
                    case "button":
                    case "checkbox":
                        style = new WidgetButtonStyleSheet(name, parent);
                        break;
                    default:
                        Type type = Type.GetType(@class);

                        if (type == null)
                            type = Type.GetType(typeof(WidgetStyleSheet).Namespace + "." + @class);

                        if (type == null)
                            type = Type.GetType(typeof(WidgetStyleSheet).Namespace + ".Widget" + @class + "StyleSheet");

                        if (type == null)
                            WindowController.Instance.LogError("Class {0} not found for style {1}", @class, name);

                        ConstructorInfo info = type.GetConstructor(new Type[] { typeof(string), typeof(WidgetStyleSheet) });

                        if (info != null)
                            style = (WidgetStyleSheet)info.Invoke(new object[] { name, parent });
                        break;

                        //style = (WidgetStyleSheet)Activator.CreateInstance(type, new object[] { name, parent }); // TODO: it will crash if there is no such constructor. Use ConstructorInfo
                }
            }

            if (style == null)
                style = new WidgetStyleSheet(name, parent);

            switch (name)
            {
            case DefaultStyleName:
                s_defaultWidgetStyle = style;
                break;
            case DefaultPanelStyleName:
                s_defaultPanelStyle = style as WidgetBackgroundStyleSheet;
                break;
            case DefaultLabelStyleName:
                s_defaultLabelStyle = style as WidgetTextStyleSheet;
                break;
            case DefaultButtonStyleName:
                s_defaultButtonStyle = style as WidgetButtonStyleSheet;
                break;
            case DefaultCheckBoxStyleName:
                s_defaultCheckBoxStyle = style;
                break;
            case DefaultTextEditStyleName:
                s_defaultTextEditStyle = style as WidgetTextEditStyleSheet;
                break;
            case DefaultWindowStyleName:
                s_defaultWindowStyle = style as WidgetBackgroundStyleSheet;
                break;
            }
            s_styles[name] = style;

            WindowController.Instance.LogMessage("Registered style {0}", name);
        }

        public static bool HasFocus(IWindowContainer window)
        {
            if (window == null)
                return false; // no window, no crime

            int windowHash = window.GetHashCode();

            IFocusableWidget focusedWidget;
            if (s_focusedWidgets.TryGetValue(windowHash, out focusedWidget))
                return focusedWidget != null;
            
            return false;
        }
        
        internal static void UpdateFocus(IFocusableWidget widget, bool focus)
        {
            IWindowContainer window = ((WindowObject)widget).Window;

            if (window == null)
                return; // no window, no crime
            
            int windowHash = window.GetHashCode();

            IFocusableWidget focusedWidget;
            s_focusedWidgets.TryGetValue(windowHash, out focusedWidget);
            
            if (!focus)
            {
                if (widget == focusedWidget)
                {
                    s_focusedWidgets.Remove(windowHash);
                }
                return;
            }

            if (focus && widget == focusedWidget)
            {
                return;
            }

            if (focusedWidget != null && focus)
            {
                IFocusableWidget oldWidget = focusedWidget;
                s_focusedWidgets.Remove(windowHash);
                oldWidget.SetFocused(false);
            }

            if (focus)
            {
                focusedWidget = widget;
                focusedWidget.SetFocused(true);
            }

            if (focusedWidget == null)
                s_focusedWidgets.Remove(windowHash);
            else
                s_focusedWidgets[windowHash] = focusedWidget;
        }

        internal static bool FocusNext(IFocusableWidget widget)
        {
            WindowObject obj = widget as WindowObject;
            if (obj == null)
                throw new ArgumentException(nameof(widget) + " is not a WindowObject!");

            IWindowContainer window = obj.Window;

            if (window == null)
                return false;

            List<WindowObject> focusables = new List<WindowObject>();
            Window.FindChildren(window, (WindowObject arg) => arg is IFocusableWidget, focusables);

            WindowObject nextFocusable = null;

            for (int i = 0; i < focusables.Count; i++)
            {
                if (focusables[i] == widget)
                {
                    nextFocusable = focusables[(i + 1) % focusables.Count];
                    break;
                }
            }

            if (nextFocusable == null || nextFocusable == widget)
                return false; // do nothing

            UpdateFocus((IFocusableWidget)nextFocusable, true);

            return true;
        }

        internal static bool HandleTooltip(Widget widget, string tooltip, Vector2 position, Widget.TooltipDelegate tooltipDelegate)
        {
            Widget.TooltipDelegate callback = tooltipDelegate ?? OnTooltip;

            if (callback == null)
                return false;

            bool result = callback(widget, tooltip, position);

            if (result)
                s_currentTooltip = widget;
            else
                s_currentTooltip = null;

            return result;
        }
        
        public static Window GetTopmostWindow()
        {
            return (Window)WindowController.Instance.Windows[WindowController.Instance.Windows.Count - 1];
        }
        
        public static void SetExclusive(Widget widget)
        {
            s_exclusiveWidgets.AddLast(widget);
        }
        
        public static void RemoveExclusive(Widget widget)
        {
            s_exclusiveWidgets.Remove(widget);
        }
    }
    
}
