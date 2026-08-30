using System;
using System.Collections.Generic;
using System.Numerics;
using NewWidgets.UI;
using NewWidgets.UI.Styles;
using NewWidgets.Utility;

#if RUNMOBILE
using RunMobile.Utility;
#endif

namespace NewWidgets.Widgets
{
    /// <summary>
    /// Simple tuple struct to hold id and class name of the Widget
    /// </summary>
    public struct WidgetStyle
    {
        public readonly string Id;
        public readonly string[] Classes;
        public readonly bool IsEmpty;

        /// <summary>
        /// Creates WidgetStyle with classes and id
        /// </summary>
        /// <param name="classes"></param>
        /// <param name="id"></param>
        public WidgetStyle(string[] classes, string id)
        {
            IsEmpty = false;
            Id = id;
            Classes = classes;
        }

        /// <summary>
        /// Creates WidgetStyle with id and no classes
        /// </summary>
        /// <param name="class"></param>
        public WidgetStyle(string id)
        {
            IsEmpty = false;
            Id = id;
            Classes = null;
        }
    }

    /// <summary>
    /// Base class for abstract widgets, i.e. Image or Label
    /// </summary>
    public abstract class Widget : WindowObject
    {
        public const string ElementType = "*";

        /// <summary>
        /// This is pretty much obsolete but was widely used in previous versions to specify a style
        /// without any decorations. Now we have to drag along .none class to maintain at least a partial compat
        /// </summary>
        public static readonly WidgetStyle DefaultStyle = new WidgetStyle(new string[] { "none" }, null);
        //

        // what StyleState answers with for a widget in the default state. Shared, and never
        // written to: a StyleSelector keeps the array by reference and only reads it
        private static readonly string[] s_noPseudoClasses = new string[0];

        public delegate bool TooltipDelegate(Widget sender, string text, Vector2 position);

        private WidgetStyleSheet m_style;
        private readonly StyleSheetData m_ownStyle;

        // not readonly: the markup loader replaces it with the tag the document used, so this
        // is assigned in more than one place. See StyleElementType
        private string m_elementType;
        private string m_id;
        private string[] m_styleClasses;
        private WidgetState m_currentState;

        private string m_tooltip;

        private WidgetMarkup m_markup;

        #region Style-related stuff

        private bool m_needUpdateStyle;
        private bool m_needsLayout; // flag to indicate that inner label size/opacity/formatting has changed
        private bool m_isResolvingBox; // true only while ResolveBox applies its own result, see Resize

        // true once C# code has set Position directly (outside ResolveBox's own write). A
        // Static widget positioned this way behaves as position: absolute -- see PositionType.
        private bool m_codePositioned;


        /// <summary>
        /// Pseudo-class flag
        /// </summary>
        public WidgetState CurrentState
        {
            get { return m_currentState; }
            protected set
            {
                if (m_currentState != value)
                {
                    m_currentState = value;
                    InvalidateStyle();
                }
            }
        }

        /// <summary>
        /// Element type, i.e. button, label, checkbox -- the name a type selector matches.
        ///
        /// A widget built in code reports the <c>ElementType</c> const of its own class, which
        /// is what every stylesheet written against this engine already says. A widget a
        /// document built reports <b>the tag the document used</b>: <c>div</c>, <c>span</c>,
        /// <c>h1</c>, <c>input</c>, <c>textarea</c>. The raw tag, not the registration selector
        /// that matched it, because an author writing <c>input { }</c> means every input and
        /// <c>checkbox</c> is not an element any editor emits.
        ///
        /// That is one name per widget, not two. Nothing in <see cref="StyleCollection"/> or
        /// <see cref="StyleSelector"/> knows this happened -- the selector chain is built from
        /// whatever this property answers -- so the cascade gains no second lookup and no extra
        /// comparison, which is what D144 requires of it.
        ///
        /// The consequence is intended: a <c>label { }</c> rule does not reach a widget the
        /// document wrote as <c>&lt;span&gt;</c>. There are two vocabularies, one per authoring
        /// mode, and a user interface is designed in HTML or in code, not in both.
        ///
        /// The setter is internal because the markup loader is the only thing that has a tag to
        /// offer today. It is an ordinary instance field either way, so a constructor that takes
        /// an element name -- <c>new WidgetLabel("h2", style)</c> -- is a public overload away.
        /// </summary>
        public string StyleElementType
        {
            get { return m_elementType; }
            internal set { m_elementType = value; InvalidateStyle(); }
        }

        /// <summary>
        /// Class name
        /// </summary>
        public string [] StyleClasses
        {
            get { return m_styleClasses; }
            set { m_styleClasses = value; InvalidateStyle(); }
        }

        /// <summary>
        /// Element #id
        /// </summary>
        public string StyleId
        {
            get { return m_id; }
            set { m_id = value; InvalidateStyle(); }
        }

        /// <summary>
        /// The pseudo-class <see cref="WidgetState.Selected"/> reports in <see cref="StyleState"/>.
        /// One state bit backs several CSS pseudo-classes in name, but what it means depends on
        /// the widget: a checked checkbox is not a focused text edit, so the bit cannot honestly
        /// stand in for all of them at once. <c>:focus</c> is correct for every widget except a
        /// checkbox, which overrides this to report <c>:checked</c> instead -- see
        /// <see cref="WidgetCheckBox.SelectedPseudoClass"/>.
        /// </summary>
        protected virtual string SelectedPseudoClass { get { return ":focus"; } }

        /// <summary>
        /// Pseudo-class name. TODO: get rid of strings
        ///
        /// <see cref="WidgetState.Selected"/> reports whatever <see cref="SelectedPseudoClass"/>
        /// says for this widget -- <c>:focus</c> for most widgets, <c>:checked</c> for a
        /// checkbox. One state bit cannot honestly stand in for every pseudo-class name at once:
        /// reporting all of them made a <c>:checked</c> rule match a text field that merely has
        /// focus, and an <c>:active</c> rule match a checked checkbox.
        ///
        /// <c>:enabled</c> is deliberately not reported. It is the default state of every
        /// widget in this engine, so reporting it would put a pseudo-class on every widget in
        /// every tree, which changes <see cref="StyleNodeMatch.PseudoClass"/> for all of them.
        /// </summary>
        public string [] StyleState
        {
            get
            {
                // the common case by a wide margin, and it used to allocate a list and an array
                // per call, twice per style resolve, to say nothing
                if (m_currentState == WidgetState.Normal)
                    return s_noPseudoClasses;

                List<string> pseudoClasses = new List<string>(6);

                if ((m_currentState & WidgetState.Hovered) != 0)
                    pseudoClasses.Add(":hover");
                if ((m_currentState & WidgetState.Selected) != 0)
                    pseudoClasses.Add(SelectedPseudoClass);
                if ((m_currentState & WidgetState.Disabled) != 0)
                    pseudoClasses.Add(":disabled");

                return pseudoClasses.ToArray();
            }
        }

        #endregion

        /// <summary>
        /// What the markup element this widget was built from said that no property here holds:
        /// the tag it came from, and the attributes and comments the engine does not model.
        /// Null for a widget built in code, which came from no element.
        ///
        /// The saver reads it to write the element back the way the document had it. Nothing
        /// else in the library reads it, and nothing in a frame does.
        /// </summary>
        public WidgetMarkup Markup
        {
            get { return m_markup; }
            set { m_markup = value; }
        }

        /// <summary>
        /// Combines the code-level hidden flag (<see cref="WindowObject.Visible"/>) with the
        /// resolved <see cref="Display"/> value, the same way a browser combines an element's own
        /// hidden state with its computed <c>display</c>. The two are kept in separate slots --
        /// <see cref="DisplayProcessor"/> never writes here directly -- and combined only on this
        /// read, so neither one can be permanently overwritten by the other depending on call
        /// order (a value written once at resolve time into someone else's field can never be
        /// un-written by a later resolve, which is exactly the earlier ClipMargin bug in this
        /// codebase).
        /// </summary>
        public override bool Visible
        {
            get { return base.Visible && Display != WidgetDisplay.None; }
            set { base.Visible = value; }
        }

        public override bool Enabled
        {
            get { return base.Enabled; }
            set
            {
                if (Enabled != value)
                {
                    if (!value)
                        CurrentState |= WidgetState.Disabled;
                    else
                        CurrentState &= ~WidgetState.Disabled;

                    base.Enabled = value;
                }
            }
        }

        public override bool Selected
        {
            get { return base.Selected; }
            set
            {
                if (Selected != value)
                {
                    if (value)
                        CurrentState |= WidgetState.Selected;
                    else
                        CurrentState &= ~WidgetState.Selected;

                    base.Selected = value;
                }
            }
        }

        public override bool Hovered
        {
            get { return base.Hovered; }
            set
            {
                if (Hovered != value)
                {
                    if (value)
                        CurrentState |= WidgetState.Hovered;
                    else
                        CurrentState &= ~WidgetState.Hovered;

                    base.Hovered = value;
                }
            }
        }

        /// <summary>
        /// Indicates if the contents should be clipped. Almost the same as overflow:hidden and overflow:visible in HTML
        /// </summary>
        public WidgetOverflow Overflow
        {
            get { return GetProperty(WidgetParameterIndex.Overflow, WidgetOverflow.Visible); }
            set { SetProperty(WidgetParameterIndex.Overflow, value); } // clipping is applied on each redraw so we don't need to call Invalidate of any kind
        }

        /// <summary>
        /// The CSS <c>display</c> value resolved for this widget, i.e. the el.style.display slot:
        /// style resolution (<see cref="DisplayProcessor"/>) and code both write through the same
        /// underlying style property, so either can set it and either can win depending on which
        /// ran last -- exactly like the DOM. See <see cref="Visible"/> for where this is combined
        /// with the code-level hidden flag.
        /// </summary>
        public WidgetDisplay Display
        {
            get { return GetProperty(WidgetParameterIndex.Display, WidgetDisplay.Block); }
            set { SetProperty(WidgetParameterIndex.Display, value); }
        }

        /// <summary>
        /// The effective CSS <c>position</c> for this widget -- the same value <see cref="ResolveBox"/>
        /// resolves against, promoting a declared Static to Absolute when the widget was placed
        /// by C# code (see <see cref="m_codePositioned"/> and the <see cref="Position"/> setter).
        /// Setting Static here clears that promotion, opting the widget back into normal flow.
        /// </summary>
        public WidgetPosition PositionType
        {
            get
            {

                WidgetPosition positionMode = GetProperty(WidgetParameterIndex.Position, WidgetPosition.Static);
                if (positionMode == WidgetPosition.Static && m_codePositioned)
                    return WidgetPosition.Absolute;

                return positionMode;
            }
            set
            {
                SetProperty(WidgetParameterIndex.Position, value);

                m_codePositioned = false;
            }
        }

        /// <summary>
        /// Wrapper for Overflow
        /// </summary>
        [Obsolete]
        public bool ClipContent
        {
            get { return Overflow == WidgetOverflow.Hidden; }
            set { Overflow = value ? WidgetOverflow.Hidden : WidgetOverflow.Visible; } // clipping is applied on each redraw so we don't need to call Invalidate of any kind
        }

        /// <summary>
        /// Wrapper for Overflow
        /// </summary>
        [Obsolete]
        public bool ClipContents
        {
            get { return Overflow == WidgetOverflow.Hidden; }
            set { Overflow = value ? WidgetOverflow.Hidden : WidgetOverflow.Visible; } // clipping is applied on each redraw so we don't need to call Invalidate of any kind
        }

        /// <summary>
        /// Margin for border clipping if ClipContents is on
        /// </summary>
        public Margin ClipMargin
        {
            get { return GetProperty(WidgetParameterIndex.ClipMargin, Margin.Empty); }
            set { SetProperty(WidgetParameterIndex.ClipMargin, value); } // clipping is applied on each redraw so we don't need to call Invalidate of any kind
        }

        /// <summary>
        /// Overall opacity of this Widget. TODO: think of the difference between content and background opacity
        /// </summary>
        public float Opacity
        {
            get { return GetProperty(WidgetParameterIndex.Opacity, 1.0f); }
            set { SetProperty(WidgetParameterIndex.Opacity, value); } // Opacity and color should be applied on each redraw - it's cheap and it is the best way to handle colors and transparency
        }

        /// <summary>
        /// Gets actual opacity value as multiplication of current and all parent values
        /// </summary>
        public float OpacityValue
        {
            get { return Parent == null ? Opacity : Opacity * Parent.Opacity; }
        }

        /// <summary>
        /// Gets tooltip string for this control
        /// </summary>
        public string Tooltip
        {
            get { return m_tooltip; }
            set { m_tooltip = value; }
        }

        /// <summary>
        /// Widget parent up in the control tree
        /// </summary>
        public new Widget Parent
        {
            get { return base.Parent as Widget; }
            set { base.Parent = value; InvalidateStyle(); }
        }

        /// <summary>
        /// Windows Object that is parent to this control. Nomad: I believe this is kind of shit, but we still need
        /// window based parents :(
        /// </summary>
        public WindowObject ParentObject
        {
            get { return base.Parent; }
        }

        public bool NeedsLayout
        {
            get { return m_needsLayout; }
        }

        /// <summary>
        /// Size of the box that percentages and anchors resolve against: the parent widget, or
        /// the screen when there is no widget parent. Note that Parent is base.Parent as Widget,
        /// so a widget sitting directly under a plain Window resolves against the screen.
        ///
        /// ponytail: this is the parent's whole box. CSS 2.1 10.1 uses the ancestor's padding
        /// edge for an absolutely positioned element; the two differ only once a parent that has
        /// a padding gets a percentage-sized or anchored child. Subtract Padding here and offset
        /// the resolved position by padding-left/top when that turns up -- nothing else in this
        /// engine gives children a content box to sit in, so introducing one here alone would
        /// only be inconsistent.
        /// </summary>
        private Vector2 ContainingBlockSize
        {
            get
            {
                Widget parent = Parent;

                if (parent != null)
                    return parent.Size;

                if (WindowController.Instance == null)
                    return Vector2.Zero;

                return new Vector2(WindowController.Instance.ScreenWidth, WindowController.Instance.ScreenHeight);
            }
        }

#if DEBUG_SIZE
        public new Vector2 Size
        {
            get
            {
                if (ParentObject == null)
                    LogConsole.WriteLine(LogLevel.WARNING, "Asked for {0}.Size before adding to parent element - style cound be invalid at the moment!", this);
                if (m_needUpdateStyle)
                    LogConsole.WriteLine(LogLevel.WARNING, "Asked for {0}.Size before calling UpdateStyle!", this);

                return base.Size;
            }
            set
            {
                base.Size = value;
            }
        }
#endif

        public event TooltipDelegate OnTooltip;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.Widget"/> class.
        /// </summary>
        /// <param name="style">Style.</param>
        protected Widget(string elementType, WidgetStyle style = default(WidgetStyle))
            : base(null)
        {
            m_elementType = elementType;
            m_id = string.IsNullOrEmpty(style.Id) ? string.Empty : style.Id;
            m_styleClasses = style.Classes;

            m_currentState = WidgetState.Normal;

            // creating own style sheet
            m_ownStyle = new StyleSheetData();

            // and complex object containing only that sheet
            m_style = new WidgetStyleSheet(elementType + "_" + GetHashCode(), null);
            m_style.SetOwnStyle(m_ownStyle);

            m_needsLayout = true;
            m_needUpdateStyle = true;

            //Size = m_style.Get(WidgetParameterIndex.Size, new Vector2(0, 0)); // obsolete, needed in some very rare cases

            m_codePositioned = true; // this flag means that default position is set from code, not markup. loading from markup resets it
        }

        #region Styles

        internal T GetProperty<T>(WidgetParameterIndex index, T defaultValue)
        {
            return m_style.Get(index, defaultValue);
        }

        /// <summary>
        /// Reads a property from the widget's own style alone, ignoring the whole cascade behind
        /// it. This is the el.style.width read: the missing read half of <see cref="SetProperty"/>,
        /// which every widget property setter in the library writes through and which lands in the
        /// own style and nowhere else.
        ///
        /// Kept although its only caller today is one assertion in Test 35, because nothing else
        /// can ask the question that assertion asks. GetProperty walks the cascade, so on a widget
        /// whose class legitimately declares a width it cannot tell a value frozen into the own
        /// style from the class rule the widget is supposed to be re-resolving -- which is exactly
        /// the defect Test 35 guards. It costs one dictionary probe, walks no cascade and is
        /// strictly cheaper than GetProperty, so it adds nothing to the path D144 protects
        /// </summary>
        internal T GetOwnProperty<T>(WidgetParameterIndex index, T defaultValue)
        {
            return m_ownStyle.GetParameter(index, defaultValue);
        }

        internal void SetProperty<T>(WidgetParameterIndex index, T value)
        {
            m_style.Set(index, value);
        }

        /// <summary>
        /// Retrieve stylesheet property value by name
        /// </summary>
        /// <typeparam name="T">Desired type</typeparam>
        /// <param name="name">property name</param>
        /// <param name="defaultValue">default value</param>
        /// <returns></returns>
        public T GetProperty<T>(string name, T defaultValue)
        {
            return m_style.Get(name, defaultValue);
        }

        /// <summary>
        /// Sets a named property for all assigned stylesheets
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetProperty(string name, string value)
        {
            m_style.Set(name, value);
        }

        /// <summary>
        /// This method should be called if the hierarchy or parent state are changed
        /// </summary>
        protected void InvalidateStyle()
        {
            m_needUpdateStyle = true;
        }

        /// <summary>
        /// This method should be called when widget layout is changed (size, padding, etc.)
        /// </summary>
        public void InvalidateLayout()
        {
            m_needsLayout = true;
        }

        /// <summary>
        /// Adds one class name to a style
        /// </summary>
        /// <param name="className"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void AddStyleClass(string className)
        {
            if (string.IsNullOrEmpty(className))
                throw new ArgumentNullException("@className should not be empty!");

            string[] array = new string[m_styleClasses.Length + 1];
            m_styleClasses.CopyTo(array, 0);
            array[m_styleClasses.Length] = className;
            m_styleClasses = array;
            InvalidateLayout();
        }

        /// <summary>
        /// Removes one class from the style
        /// </summary>
        /// <param name="className"></param>
        /// <returns>true if the class was removed</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool RemoveStyleClass(string className)
        {
            if (string.IsNullOrEmpty(className))
                throw new ArgumentNullException("@className should not be empty!");

            bool changed = false;

            for (int i = 0; i < m_styleClasses.Length; i++)
            {
                if (m_styleClasses[i] == className)
                {
                    m_styleClasses[i] = string.Empty;
                    changed = true;
                }
            }

            if (changed)
                InvalidateLayout();

            return changed;
        }


        /// <summary>
        /// This method should be called whenever you need to update widget
        /// size, style and layout immediatelly
        /// </summary>
        public void Relayout()
        {
            if (!m_needsLayout && !m_needUpdateStyle) // style was already loaded, no need to reload it
                return;

            UpdateStyle();
            UpdateLayout();
        }

        /// <summary>
        /// This method is to be called when:
        /// 1. Widget size has changed (Resize was called)
        /// 2. Widget style has changed
        /// 3. Widget content has changed and widget should be resized
        /// </summary>
        protected virtual void UpdateLayout()
        {
            m_needsLayout = false;
            // nothing to do in base
        }

        [Obsolete("Don't use it unless you know what to do!")]
        public void ForceUpdateLayout()
        {
            UpdateLayout();
        }

        [Obsolete("Don't use it unless you know what to do!")]
        public void ForceUpdateStyle()
        {
            UpdateStyle();
        }

        protected override void Resize(Vector2 size)
        {
            base.Resize(size);

            // Only a resize that came from outside the box resolver is recorded in the own
            // style: an explicit widget.Size = ... from game code, or a widget measuring its own
            // content in UpdateLayout. That is the el.style.width equivalent and it is correct.
            // The resolver's own result must never go there, because the own style sits at the
            // head of the cascade and answers every later lookup before the cascade is reached,
            // so writing it would freeze the widget's size after the first layout pass and no
            // added class and no :hover rule could ever resize it again.
            if (!m_isResolvingBox)
            {
                SetProperty(WidgetParameterIndex.Width, StyleLength.Pixels(size.X));
                SetProperty(WidgetParameterIndex.Height, StyleLength.Pixels(size.Y));
            }

            InvalidateLayout();
        }

        /// <summary>
        /// Applies a computed size the same way <see cref="ResolveBox"/> applies its own result --
        /// through the ordinary <see cref="Size"/> setter, but with <see cref="m_isResolvingBox"/>
        /// raised so <see cref="Resize"/> does not record it into the own style. For a subclass
        /// that computes its size from its content or its children, e.g. auto height in
        /// <see cref="Controls.WidgetPanel.UpdateLayout"/>: going through the plain <see cref="Size"/>
        /// setter instead would freeze that size into the own style, which sits at the head of the
        /// cascade and would answer every later layout pass before auto could run again -- see the
        /// comment on <see cref="Resize"/>.
        /// </summary>
        protected void SetResolvedSize(Vector2 size)
        {
            m_isResolvingBox = true;
            Size = size;
            m_isResolvingBox = false;
        }

        protected virtual void UpdateStyle()
        {
            m_needUpdateStyle = false;
            m_needsLayout = true; // make sure that any style changes result in layout updates as well

            List<StyleSelector> styles = new List<StyleSelector>();
            List<StyleNodeMatch> types = new List<StyleNodeMatch>();

            Widget current = this;

            do
            {
                styles.Add(new StyleSelector(current.StyleElementType, current.StyleClasses, current.StyleId, current.StyleState,
                    current.Markup == null ? null : current.Markup.StyleAttributes));

                // Reason why this style was added

                if (current == this)
                {
                    StyleNodeMatch type = StyleNodeMatch.Element;

                    if (!string.IsNullOrEmpty(StyleId))
                        type |= StyleNodeMatch.Id;

                    if (StyleClasses != null && StyleClasses.Length > 0)
                        type |= StyleNodeMatch.Class;

                    if (StyleState != null && StyleState.Length > 0)
                        type |= StyleNodeMatch.PseudoClass;
                    
                    // type for current node
                    types.Add(type);
                }
                else
                {
                    types.Add(this.Parent == current ? StyleNodeMatch.Parent : StyleNodeMatch.GrandParent);
                }

                current = current.Parent;
            }
            while (current != null);

            styles.Reverse();
            types.Reverse();

            StyleSelectorList list = new StyleSelectorList(styles, types);

            m_style = WidgetManager.GetStyle(list);

            m_style.SetOwnStyle(m_ownStyle);

            //Console.WriteLine("Resolved style: {0} {{\n{1}\n}}", list, m_style);

            ResolveBox();
        }

        /// <summary>
        /// Turns the declared box properties into a real position and size, following CSS 2.1
        /// 10.3.7 for the horizontal axis, 10.6.4 for the vertical one and 10.4 for the min/max
        /// clamp -- the rules for an absolutely positioned box, which is what every NewWidgets
        /// widget was before <c>position</c> carried any meaning. Static and relative widgets
        /// share the same size resolution but never let left/top/right/bottom anchor the box:
        /// see <see cref="WidgetPosition"/>.
        ///
        /// This runs from UpdateStyle, which is where the engine has always applied CSS
        /// geometry. Keeping it there means a widget that measures its own content in
        /// UpdateLayout still has the last word, exactly as before, and WidgetPanel.Update
        /// resolves the panel through base.Update() before it updates m_children, so a parent is
        /// always settled by the time a child asks for its containing block.
        ///
        /// Allocation free: every lookup unboxes into a struct and both axes live on the stack.
        /// Note that nothing here invalidates the style, so a resize never costs a cascade
        /// re-walk -- Resize only raises the layout flag, as it always did.
        /// </summary>
        private void ResolveBox()
        {
            Vector2 containingBlock = ContainingBlockSize;

            Vector2 size = Size;
            Vector2 position = Position;

            WidgetPosition positionMode = PositionType;

            StyleLength left = m_style.Get(WidgetParameterIndex.Left, StyleLength.Unset);
            StyleLength right = m_style.Get(WidgetParameterIndex.Right, StyleLength.Unset);
            StyleLength top = m_style.Get(WidgetParameterIndex.Top, StyleLength.Unset);
            StyleLength bottom = m_style.Get(WidgetParameterIndex.Bottom, StyleLength.Unset);

            // Only an absolutely positioned box is anchored by left/top/right/bottom -- CSS 2.1
            // 9.3.1. Static and relative both resolve as if those four were never declared, so
            // the box keeps the position it already had; relative then offsets that below.
            bool anchored = positionMode == WidgetPosition.Absolute;

            StyleAxis horizontal = new StyleAxis(
                anchored ? left : StyleLength.Unset,
                anchored ? right : StyleLength.Unset,
                m_style.Get(WidgetParameterIndex.Width, StyleLength.Unset),
                m_style.Get(WidgetParameterIndex.MarginLeft, StyleLength.Unset),
                m_style.Get(WidgetParameterIndex.MarginRight, StyleLength.Unset),
                m_style.Get(WidgetParameterIndex.MinWidth, StyleLength.Unset),
                m_style.Get(WidgetParameterIndex.MaxWidth, StyleLength.Unset));

            horizontal.Resolve(containingBlock.X, ref position.X, ref size.X);

            StyleAxis vertical = new StyleAxis(
                anchored ? top : StyleLength.Unset,
                anchored ? bottom : StyleLength.Unset,
                m_style.Get(WidgetParameterIndex.Height, StyleLength.Unset),
                m_style.Get(WidgetParameterIndex.MarginTop, StyleLength.Unset),
                m_style.Get(WidgetParameterIndex.MarginBottom, StyleLength.Unset),
                m_style.Get(WidgetParameterIndex.MinHeight, StyleLength.Unset),
                m_style.Get(WidgetParameterIndex.MaxHeight, StyleLength.Unset));

            vertical.Resolve(containingBlock.Y, ref position.Y, ref size.Y);

            if (positionMode == WidgetPosition.Relative)
            {
                // CSS 2.1 9.4.3: offset from the position the box would have had, left winning
                // over right and top winning over bottom when both sides of an axis are declared
                position.X += left.IsDefinite ? left.Resolve(containingBlock.X) : (right.IsDefinite ? -right.Resolve(containingBlock.X) : 0.0f);
                position.Y += top.IsDefinite ? top.Resolve(containingBlock.Y) : (bottom.IsDefinite ? -bottom.Resolve(containingBlock.Y) : 0.0f);
            }

            // Kept true through the Position write below too: that write is ResolveBox applying
            // its own result, same as Size, and must not itself mark the widget code-positioned.
            m_isResolvingBox = true;
            Size = size;

            if (Vector2.DistanceSquared(position, Position) > float.Epsilon)
                Position = position;

            m_isResolvingBox = false;

            // a ZIndex of 0 already means "nothing explicit" in this engine, so it doubles as
            // the not-declared case and no sentinel is needed
            int zIndex = m_style.Get(WidgetParameterIndex.ZIndex, 0);

            if (zIndex != 0)
                ZIndex = zIndex;
        }

        #endregion

        internal void SetCodePositionFlag(bool value)
        {
            m_codePositioned = value;
        }

        public override bool Update()
        {
            if (m_needUpdateStyle)
                UpdateStyle();

            if (m_needsLayout)
                UpdateLayout();

            return base.Update();
        }

        public override void Draw()
        {
            base.Draw(); // does nothing 

            if (!Visible)
                return;

            if (Overflow == WidgetOverflow.Hidden)
            {
                Vector2 clipTopLeft = this.Transform.GetScreenPoint(new Vector2(ClipMargin.Left, ClipMargin.Top));
                Vector2 clipBottomRight = this.Transform.GetScreenPoint(new Vector2(this.Size.X - ClipMargin.Right, this.Size.Y - ClipMargin.Bottom));

                WindowController.Instance.SetClipRect(
                    (int)Math.Floor(clipTopLeft.X),
                    (int)Math.Floor(clipTopLeft.Y),
                    (int)Math.Ceiling(clipBottomRight.X - clipTopLeft.X),
                    (int)Math.Ceiling(clipBottomRight.Y - clipTopLeft.Y));
            }

            DrawContents();
            
            if (Overflow == WidgetOverflow.Hidden)
                WindowController.Instance.CancelClipRect();
        }

        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            if ((!string.IsNullOrEmpty(m_tooltip) || OnTooltip != null) && ((pointer == 0 && !unpress && !press) || (press && WindowController.Instance.IsTouchScreen)))
                return WidgetManager.HandleTooltip(this, m_tooltip, new Vector2(x, y), OnTooltip);

            return base.Touch(x, y, press, unpress, pointer);
        }

        /// <summary>
        /// This method draws widget contents with clipping
        /// </summary>
        protected virtual void DrawContents()
        {
        }
        
        public void FadeTo(float alpha, int time, Action callback)
        {
            AnimationManager.Instance.StartAnimation(this, AnimationKind.Alpha, Opacity, alpha, time, (float x, float from, float to) => Opacity = MathHelper.LinearInterpolation(x, from, to), callback);
        }
    }
}

