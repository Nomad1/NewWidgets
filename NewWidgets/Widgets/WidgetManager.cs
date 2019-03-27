using System;
using System.Numerics;

using System.Collections.Generic;
using System.Xml;
using NewWidgets.UI;

namespace NewWidgets.Widgets
{
    public static class WidgetManager
    {
        // 
        private static float s_fontScale;
        private static Font s_mainFont;

        private static Widget s_currentTooltip;

        // styles
        private static WidgetStyleSheet s_defaultWidgetStyle;
        private static WidgetStyleSheet s_defaultWindowStyle;
        private static WidgetStyleSheet s_defaultPanelStyle;
        private static WidgetStyleSheet s_defaultLabelStyle;
        private static WidgetStyleSheet s_defaultButtonStyle;
        private static WidgetStyleSheet s_defaultCheckBoxStyle;
        private static WidgetStyleSheet s_defaultTextEditStyle;

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

        public static WidgetStyleSheet DefaultWidgetStyle { get { return s_defaultWidgetStyle; } }
        public static WidgetStyleSheet DefaultCheckBoxStyle { get { return s_defaultCheckBoxStyle ?? s_defaultWidgetStyle; } }
        public static WidgetStyleSheet DefaultButtonStyle { get { return s_defaultButtonStyle ?? s_defaultWidgetStyle; } }
        public static WidgetStyleSheet DefaultPanelStyle { get { return s_defaultPanelStyle ?? s_defaultWidgetStyle; } }
        public static WidgetStyleSheet DefaultTextEditStyle { get { return s_defaultTextEditStyle ?? s_defaultWidgetStyle; } }
        public static WidgetStyleSheet DefaultLabelStyle { get { return s_defaultLabelStyle ?? s_defaultWidgetStyle; } }
        public static WidgetStyleSheet DefaultWindowStyle { get { return s_defaultWindowStyle ?? s_defaultWidgetStyle; } }

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

            s_defaultWidgetStyle = new WidgetStyleSheet();
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

            WidgetStyleSheet parent = node.Attributes.GetNamedItem("parent") == null ? null : GetStyle(node.Attributes.GetNamedItem("parent").Value);
            if (parent == null)
                parent = DefaultWidgetStyle;
            
            WidgetStyleSheet style = new WidgetStyleSheet(name, parent, node);

            switch (name)
            {
            case "default":
                s_defaultWidgetStyle = style;
                break;
            case "default_panel":
                s_defaultPanelStyle = style;
                break;
            case "default_label":
                s_defaultLabelStyle = style;
                break;
            case "default_button":
                s_defaultButtonStyle = style;
                break;
            case "default_checkbox":
                s_defaultCheckBoxStyle = style;
                break;
            case "default_textedit":
                s_defaultTextEditStyle = style;
                break;
            case "default_window":
                s_defaultWindowStyle = style;
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

            IFocusableWidget focusedWidget = null;
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

            IFocusableWidget focusedWidget = null;
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
