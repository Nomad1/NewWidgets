using System;
using System.Numerics;

using System.Collections.Generic;
using NewWidgets.UI;

namespace NewWidgets.Widgets
{
    /// <summary>
    /// This static class contains different static variables for widgets
    /// along with global handler, focusing handlers and style loading
    /// </summary>
    public static partial class WidgetManager
    {
        private const string DefaultFontName = "default";

        // 
        private static float s_fontScale;
        private static Font s_mainFont;

        private static Widget s_currentTooltip;

        private static bool s_isInited;

        // this window is used for tooltips and other top-level controls. If not set, last one of Windows collection is to be used instead
        private static Window s_topLevelWindow;

        private static readonly Dictionary<string, Font> s_fonts = new Dictionary<string, Font>();

        // focus
        private static readonly Dictionary<int, IFocusable> s_focusedWidgets = new Dictionary<int, IFocusable>();
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
                if (name == DefaultFontName)
                    return s_mainFont;

                Font result;
                if (s_fonts.TryGetValue(name, out result))
                    return result;
            } else
                throw new WidgetException("Empty font name passed to GetFont!");

            WindowController.Instance.LogError("WidgetManager got GetFont for not existing font {0}", name);

            return null; // TODO: return default font to avoid crash?
        }

        /// <summary>
        /// Initialization method. It should be called before any widget activity is performed
        /// </summary>
        /// <param name="fontScale">Font scale.</param>
        public static void Init(float fontScale)
        {
            if (s_isInited)
                throw new WidgetException("Widget manager is already inited!");

            s_isInited = true;
            s_fontScale = fontScale; 

            WindowController.Instance.OnTouch += HandleTouch;

            // We need to register conversion from string to Font type
            Utility.ConversionHelper.RegisterParser(typeof(Font), (str, unitType) => GetFont(str));
        }

        private static bool HandleTouch(float x, float y, bool press, bool unpress, int pointer)
        {
            if (s_currentTooltip != null && !s_currentTooltip.HitTest(x, y))
                HideTooltips();

            if (s_exclusiveWidgets.Count > 0)
            {
                return s_exclusiveWidgets.Last.Value.Touch(x, y, press, unpress, pointer);
            }
            
            return false;
        }

        /// <summary>
        /// Hides current tooltip
        /// </summary>
        public static void HideTooltips()
        {
            if (s_currentTooltip != null)
                HandleTooltip(null, null, Vector2.Zero, null);
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

            return WindowController.Instance.IsTouchScreen ? false : result;
        }

        #region Focus handling

        public static void SetFocus(IFocusable widget, bool value = true)
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

            IFocusable focusedWidget;
            if (s_focusedWidgets.TryGetValue(windowHash, out focusedWidget))
                return focusedWidget != null;
            
            return false;
        }

        internal static void UpdateFocus(IFocusable widget, bool focus)
        {
            IWindowContainer window = ((WindowObject)widget).Window;

            if (window == null)
                return; // no window, no crime

            int windowHash = window.GetHashCode();

            IFocusable focusedWidget;
            s_focusedWidgets.TryGetValue(windowHash, out focusedWidget);

            // Nomad: PVS analyzer indicated some amount of fuzzy logic here.
            // I think that whole focusing and unfocusing logic should be refactored

            if (widget == focusedWidget)
            {
                if (!focus && focusedWidget != null)
                {
                    focusedWidget.SetFocused(false);
                    s_focusedWidgets.Remove(windowHash);
                    WindowController.Instance.ShowKeyboard(false);
                }

                return;
            }

            if (focusedWidget != null)
            {
                IFocusable oldWidget = focusedWidget;
                s_focusedWidgets.Remove(windowHash);
                oldWidget.SetFocused(false);
            }

            focusedWidget = widget;

            WindowController.Instance.ShowKeyboard(focus && (focusedWidget is WidgetTextEdit || focusedWidget is WidgetTextField));

            if (focusedWidget == null)
                s_focusedWidgets.Remove(windowHash);
            else
            {
                if (focus)
                    focusedWidget.SetFocused(true);
                s_focusedWidgets[windowHash] = focusedWidget;
            }
        }

        internal static bool FocusNext(IFocusable widget)
        {
            WindowObject obj = widget as WindowObject;
            if (obj == null)
                throw new ArgumentException(nameof(widget) + " is not a WindowObject!");

            IWindowContainer window = obj.Window;

            if (window == null)
                return false;

            List<WindowObject> focusables = new List<WindowObject>();
            Window.FindChildren(window, (WindowObject arg) => arg is IFocusable, focusables);

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

            UpdateFocus((IFocusable)nextFocusable, true);

            return true;
        }

        public static void RemoveFocus(IWindowContainer window)
        {
            int windowHash = window.GetHashCode();

            List<WindowObject> focusables = new List<WindowObject>();
            Window.FindChildren(window, (WindowObject arg) => arg is IFocusable, focusables);

            for (int i = 0; i < focusables.Count; i++)
            {
                if (((IFocusable)focusables[i]).IsFocusable)
                    ((IFocusable)focusables[i]).SetFocused(false);
            }

            s_focusedWidgets.Remove(windowHash);
            WindowController.Instance.ShowKeyboard(false);
        }

        #endregion

        #region Modal windows support

        public static void RegisterTopLevelWindow(Window window)
        {
            s_topLevelWindow = window;
        }

        public static Window GetTopmostWindow()
        {
            return s_topLevelWindow ?? WindowController.Instance.Windows[WindowController.Instance.Windows.Length - 1];
        }
        
        public static void SetExclusive(Widget widget)
        {
            s_exclusiveWidgets.AddLast(widget);
        }
        
        public static void RemoveExclusive(Widget widget)
        {
            s_exclusiveWidgets.Remove(widget);
        }

        #endregion
    }
}
