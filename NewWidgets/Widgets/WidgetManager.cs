using System;
using System.Numerics;

using System.Collections.Generic;
using System.Xml;
using NewWidgets.UI;

namespace NewWidgets.Widgets
{
    public static partial class WidgetManager
    {
        // 
        private static float s_fontScale;
        private static Font s_mainFont;

        private static Widget s_currentTooltip;

        // this window is used for tooltips and other top-level controls. If not set, last one of Windows collection is to be used instead
        private static Window s_topLevelWindow;

        private static readonly Dictionary<string, Font> s_fonts = new Dictionary<string, Font>();

        // focus
        private static readonly Dictionary<int, IFocusableWidget> s_focusedWidgets = new Dictionary<int, IFocusableWidget>();
        private static readonly LinkedList<Widget> s_exclusiveWidgets = new LinkedList<Widget>();

        // Events

        public static event Widget.TooltipDelegate OnTooltip;

        // Properties
        public static Font MainFont { get { return s_mainFont; } }
        public static float FontScale { get { return s_fontScale; } }

        public static Font GetFont(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                Font result;
                if (s_fonts.TryGetValue(name, out result))
                    return result;
            }

            WindowController.Instance.LogError("WidgetManager got GetFont for not existing font {0}", name);

            return null; // TODO: return default font to avoid crash?
        }

        /// <summary>
        /// Initialization method. It should be called before any widget activity is performed
        /// </summary>
        /// <param name="fontScale">Font scale.</param>
        public static void Init(float fontScale)
        {
            s_fontScale = fontScale; 
            s_styles.Clear();

            WindowController.Instance.OnTouch += HandleTouch;
        }

        public static void RegisterTopLevelWindow(Window window)
        {
            s_topLevelWindow = window;
        }

        public static void LoadUI(string uiData)
        {
#if !DEBUG
            try
#endif
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
#if !DEBUG
            catch (Exception ex)
            {
                WindowController.Instance.LogError("Error loading ui data: " + ex);
                throw;
            }
#endif
        }

        private static bool HandleTouch(float x, float y, bool press, bool unpress, int pointer)
        {
            if (s_currentTooltip != null && !s_currentTooltip.HitTest(x, y))
                HandleTooltip(null, null, Vector2.Zero, null);

            if (s_exclusiveWidgets.Count > 0)
            {
                return s_exclusiveWidgets.Last.Value.Touch(x, y, press, unpress, pointer);
            }
            
            return false;
        }

        private static void RegisterFont(XmlNode node)
        {
            string name = node.Attributes.GetNamedItem("name").Value;
            string resource = node.Attributes.GetNamedItem("resource").Value;
            float spacing = FloatParse(node.Attributes.GetNamedItem("spacing").Value);
            int baseline = int.Parse(node.Attributes.GetNamedItem("baseline").Value);

            int shift = 0;

            if (node.Attributes.GetNamedItem("shift") != null)
                shift = int.Parse(node.Attributes.GetNamedItem("shift").Value);

            int leading = 0;

            if (node.Attributes.GetNamedItem("leading") != null)
                leading = int.Parse(node.Attributes.GetNamedItem("leading").Value);

            //IWindowController.Instance.RegisterSpriteTemplate(resource, 1.0f, 0.0f, 0.0f, 0);
            //IWindowController.Instance.RegisterSpriteAnimation(resource, 0, resource, resource, 0, 0, 0, 0);

            Font font = new Font(resource, spacing, leading, baseline, shift);

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

        public static void SetFocus(IFocusableWidget widget, bool value = true)
        {
            WindowController.Instance.ScheduleAction(delegate
            {
                widget.SetFocused(value);
            }, 1); // schedule for the next frame
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

            // Nomad: PVS analyzer indicated some amount of fuzzy logic here.
            // I think that whole focusing and unfocusing logic should be refactored

            if (widget == focusedWidget)
            {
                if (!focus && focusedWidget != null)
                {
                    focusedWidget.SetFocused(false);
                    s_focusedWidgets.Remove(windowHash);
                }

                return;
            }

            if (focusedWidget != null)
            {
                IFocusableWidget oldWidget = focusedWidget;
                s_focusedWidgets.Remove(windowHash);
                oldWidget.SetFocused(false);
            }

            focusedWidget = widget;

            if (focusedWidget == null)
                s_focusedWidgets.Remove(windowHash);
            else
            {
                if (focus)
                    focusedWidget.SetFocused(true);
                s_focusedWidgets[windowHash] = focusedWidget;
            }
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
            return s_topLevelWindow ?? WindowController.Instance.Windows[WindowController.Instance.Windows.Count - 1];
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
