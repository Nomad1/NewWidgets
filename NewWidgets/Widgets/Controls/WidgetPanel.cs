using System;
using System.Collections.Generic;
using System.Numerics;
using NewWidgets.UI;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    public class WidgetPanel : WidgetBackground, IWindowContainer
    {
        public new const string ElementType = "div";
        //

        private readonly WindowObjectArray<Widget> m_children = new WindowObjectArray<Widget>();

        public override string ToString()
        {
            return string.Format("<{0}> #{1} {2}x{3} at {4},{5} children={6}", StyleElementType, StyleId, (int)Size.X, (int)Size.Y, (int)Position.X, (int)Position.Y, Children.Count);
        }

        public IList<Widget> Children
        {
            get { return m_children.List; }
        }

        ICollection<WindowObject> IWindowContainer.Children
        {
            get { return m_children.List; }
        }

        public int MaximumZIndex
        {
            get { return m_children.MaximumZIndex; }
        }

        public WidgetPanel(WidgetStyle style = default(WidgetStyle))
           : this(ElementType, style)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetPanel"/> class.
        /// </summary>
        /// <param name="style">Style.</param>
        internal WidgetPanel(string elementType, WidgetStyle style)
            : base(elementType, style)
        {
        }

        /// <summary>
        /// Normal flow, CSS 2.1 9.4.1: an in-flow child stacks under the previous one instead of
        /// keeping whatever position it already had, the way a block container stacks its
        /// block-level children. Vertical only -- no line boxes, no wrapping, no horizontal flow.
        ///
        /// Runs after <see cref="Widget.UpdateLayout"/>, by which point <see cref="Widget.UpdateStyle"/>
        /// has already resolved this panel's own box (see <see cref="Update"/>), so <see cref="Widget.Size"/>
        /// here is this panel's settled size and can stand in as the containing block for a
        /// child's percentage margin.
        /// </summary>
        protected override void UpdateLayout()
        {
            base.UpdateLayout();

            float top = GetProperty(WidgetParameterIndex.Padding, Margin.Empty).Top;
            float left = GetProperty(WidgetParameterIndex.Padding, Margin.Empty).Left;
            WidgetDisplay display = GetProperty(WidgetParameterIndex.Display, WidgetDisplay.Block);

            // justify-content hands out whatever room is left over on the main axis, so it has to
            // know the total extent BEFORE anything is placed: one measuring pass here, then the
            // placement loop below shifts the start by `justifyOffset` and pads each gap by
            // `justifyGap`. flex-start needs neither, and is left on the original single-pass
            // path so nothing that does not ask for this pays for it.
            //
            // Only a flex row is justified. A block child takes the full width, so there is no
            // spare room to hand out -- which is why CSS does not define justify-content for a
            // block container either.
            float justifyOffset = 0.0f;
            float justifyGap = 0.0f;

            if (display == WidgetDisplay.Flex)
            {
                WidgetJustifyContent justify = GetProperty(WidgetParameterIndex.JustifyContent, WidgetJustifyContent.FlexStart);

                if (justify != WidgetJustifyContent.FlexStart)
                {
                    float content = 0.0f;
                    int count = 0;

                    foreach (Widget child in m_children.List)
                    {
                        if (!child.Visible)
                            continue;

                        child.Relayout();

                        if (child.PositionType == WidgetPosition.Absolute)
                            continue; // out of flow, so it takes none of the room

                        StyleLength childBefore = child.GetProperty(WidgetParameterIndex.MarginLeft, StyleLength.Unset);
                        StyleLength childAfter = child.GetProperty(WidgetParameterIndex.MarginRight, StyleLength.Unset);

                        content += (childBefore.IsDefinite ? childBefore.Resolve(Size.X) : 0.0f)
                            + child.Size.X
                            + (childAfter.IsDefinite ? childAfter.Resolve(Size.X) : 0.0f);

                        count++;
                    }

                    Margin padding = GetProperty(WidgetParameterIndex.Padding, Margin.Empty);
                    float free = Size.X - padding.Width - content;

                    // A row sized to its own content has nothing spare, and a row that overflows
                    // has less than nothing: CSS packs both at the start rather than pulling items
                    // backwards, so anything not positive is left alone.
                    if (free > 0.0f && count > 0)
                        switch (justify)
                        {
                            case WidgetJustifyContent.FlexEnd:
                                justifyOffset = free;
                                break;
                            case WidgetJustifyContent.Center:
                                justifyOffset = free / 2.0f;
                                break;
                            case WidgetJustifyContent.SpaceBetween:
                                justifyGap = count > 1 ? free / (count - 1) : 0.0f;
                                break;
                            case WidgetJustifyContent.SpaceAround:
                                justifyGap = free / count;
                                justifyOffset = justifyGap / 2.0f;
                                break;
                            case WidgetJustifyContent.SpaceEvenly:
                                justifyGap = free / (count + 1);
                                justifyOffset = justifyGap;
                                break;
                        }
                }

                left += justifyOffset;
            }

            foreach (Widget child in m_children.List)
            {
                if (!child.Visible)
                    continue; // takes no space, like display: none

                child.Relayout();

                WidgetPosition positionType = child.PositionType;

                // ponytail: CSS z-index does not change flow order, but m_children is sorted by
                // z-index (see WindowObjectArray), so a child with an explicit z-index stacks out
                // of document order here.
                if (positionType == WidgetPosition.Absolute)
                    continue;

                // A Relative child stays in flow and is placed exactly as if it were Static; its
                // own offset from left/top/right/bottom is applied by its own ResolveBox and must
                // not be applied here too.

                StyleLength marginLeft = child.GetProperty(WidgetParameterIndex.MarginLeft, StyleLength.Unset);
                StyleLength marginRight = child.GetProperty(WidgetParameterIndex.MarginRight, StyleLength.Unset);
                StyleLength marginTop = child.GetProperty(WidgetParameterIndex.MarginTop, StyleLength.Unset);
                StyleLength marginBottom = child.GetProperty(WidgetParameterIndex.MarginBottom, StyleLength.Unset);

                float before = marginLeft.IsDefinite ? marginLeft.Resolve(Size.X) : 0.0f;
                float after = marginRight.IsDefinite ? marginRight.Resolve(Size.X) : 0.0f;
                float above = marginTop.IsDefinite ? marginTop.Resolve(Size.Y) : 0.0f;
                float below = marginBottom.IsDefinite ? marginBottom.Resolve(Size.Y) : 0.0f;

                child.Position = new Vector2(left + before, top + above);

                // Widget.Position's setter marks the widget code-positioned, which PositionType
                // would then read back as Absolute (see Widget.PositionType) and pull it out of
                // flow on the very next pass. Writing PositionType back to what it already was
                // clears that promotion -- the same reset Test 133 exercises -- without reaching
                // into Widget's private m_isResolvingBox from outside its class.
                //child.PositionType = positionType;

                if (positionType == WidgetPosition.Relative)
                {
                    // The offset itself has to come from a real ResolveBox, per the comment
                    // above -- forced by marking layout dirty again, since child.Relayout() just
                    // above already cleared both dirty flags and would otherwise no-op. This
                    // ResolveBox call sees the flow position just written as Position's current
                    // value and adds left/top/right/bottom on top of it, CSS 2.1 9.4.3.
                    child.InvalidateLayout();
                    child.Relayout();
                }

                // ponytail: adjacent margins do not collapse here, unlike CSS 2.1 8.3.1.
                if (display == WidgetDisplay.Block)
                    top += above + child.Size.Y + below;
                else
                    left += before + child.Size.X + after + justifyGap;
            }

            // CSS 2.1 10.6.3: a normal-flow block box whose height is auto -- declared that way,
            // or never declared at all -- is as tall as its content, here the stacked children's
            // flow plus the padding below them. A declared height is definite and is left alone,
            // unlike width, which this engine leaves alone either way -- see StyleUnit.Unset.
            //
            // CSS 2.1 10.6.4 says the same of an absolutely positioned box, so being out of flow is
            // NOT what disqualifies a panel here. One case is: when top and bottom are both pinned
            // the height is stretched between them, and Widget.ResolveBox has already computed it,
            // so a content height would clobber a correct answer.
            StyleLength height = GetProperty(WidgetParameterIndex.Height, StyleLength.Unset);

            bool stretched = PositionType == WidgetPosition.Absolute
                && GetProperty(WidgetParameterIndex.Top, StyleLength.Unset).IsDefinite
                && GetProperty(WidgetParameterIndex.Bottom, StyleLength.Unset).IsDefinite;

            if (!height.IsDefinite && !stretched)
            {
                float bottom = GetProperty(WidgetParameterIndex.Padding, Margin.Empty).Bottom;
                float right = GetProperty(WidgetParameterIndex.Padding, Margin.Empty).Right;

                if (display == WidgetDisplay.Block)
                    SetResolvedSize(new Vector2(Size.X, top + bottom));
                else
                    SetResolvedSize(new Vector2(left + right, Size.Y));
            }
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;

            m_children.Update();

            return true;
        }

        protected override void DrawContents()
        {
            base.DrawContents();

            m_children.Draw();
        }

        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            bool processed = base.Touch(x, y, press, unpress, pointer);

            if (processed)
                return true;

            if (!Enabled)
                return true;

            if (m_children.Touch(x, y, press, unpress, pointer))
                return true;

            if (m_background.Touch(x, y, press, unpress, pointer)) // make sure that click inside panel is not transparent
                return true;

            return false;
        }

        public override bool Zoom(float x, float y, float value)
        {
            //While it's not required for Widget descendants, all WidgetPanel descendants should use
            //the following lines:

            //bool processed = base.Zoom(x, y, value);

            //if (processed)
            //    return true;

            if (!Enabled)
                return true;

            if (m_children.Zoom(x, y, value))
                return true;

            if (m_background.Zoom(x, y, value))
                return true;

            return false;
        }

        public override bool Key(SpecialKey key, bool up, string keyString)
        {
            if (!Enabled)
                return true;

            if (m_children.Key(key, up, keyString))
                return true;

            if (m_background.Key(key, up, keyString))
                return true;

            return false;
        }

        public void AddChild(Widget child)
        {
            IWindowContainer parentContainer = child.Parent as IWindowContainer;
            if (parentContainer != null && parentContainer != this)
                parentContainer.RemoveChild(child);

            m_children.Add(child);
            child.Parent = this;
        }

        public bool RemoveChild(WindowObject child)
        {
            Widget childWidget = child as Widget;
            if (child.Parent != this)
                return false;

            if (childWidget == null)
                throw new ArgumentException(nameof(child));

            m_children.Remove(childWidget);

            return true;
        }

        void IWindowContainer.AddChild(WindowObject child)
        {
            if (child is Widget)
                AddChild((Widget)child);
            else
                throw new ArgumentException(nameof(child));
        }

        public virtual void Clear()
        {
            foreach (Widget obj in m_children.List)
                obj.Remove();

            m_children.Clear();
        }

        public override void Remove()
        {
            Clear();

            base.Remove();
        }

        /// <summary>
        /// Finds a widget by the <c>#id</c> it was built with, anywhere below this one. This is
        /// how a tree loaded from XHTML is bound to code: markup owns the structure and the
        /// stylesheet owns the appearance, so the id a rule already names a control by is the
        /// only handle code has on it.
        ///
        /// Generic because a caller wants a specific class -- there is nothing to do with a
        /// <see cref="Widget"/> that has no <c>OnPress</c> -- and throwing rather than returning
        /// null because both ways of getting the call wrong, a typo in the id and the wrong class
        /// in the angle brackets, are mistakes in the line just written. Handing back a null
        /// would move the report to whichever event handler dereferences it first. Use
        /// <see cref="TryFind{T}(string, out T)"/> where absence is a legitimate answer.
        /// </summary>
        /// <typeparam name="T">Class the widget is expected to be</typeparam>
        /// <param name="id">The id, without the '#'</param>
        /// <exception cref="ArgumentException">No widget carries the id, or the one that does is
        /// of another class</exception>
        public T Find<T>(string id) where T : Widget
        {
            T result;

            if (TryFind(id, out result))
                return result;

            Widget other;

            // second walk, on the failure path only, to tell the two mistakes apart
            if (TryFind(id, out other))
                throw new ArgumentException(string.Format("Widget #{0} is a {1} and not a {2}", id, other.GetType().Name, typeof(T).Name));

            throw new ArgumentException(string.Format("No widget with id #{0} below this {1}", id, GetType().Name));
        }

        /// <summary>
        /// The <see cref="Find{T}"/> that answers instead of throwing, in the shape of
        /// <c>TryGetValue</c>
        /// </summary>
        public bool TryFind<T>(string id, out T widget) where T : Widget
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException("id"); // every unnamed widget has StyleId == string.Empty and would match

            return TryFind(this, id, out widget);
        }

        /// <summary>
        /// The walk itself, over <see cref="IWindowContainer"/> so that the markup loader can
        /// search from whatever it was given as the document body.
        ///
        /// Not <see cref="Window.FindChildren"/>, which is otherwise the same walk: it skips a
        /// child whose <c>Visible</c> is false, and half the controls of a real dialog start
        /// hidden -- the sample's own <c>#local_edit</c> does -- so binding one would fail.
        ///
        /// ponytail: a walk, not an index. An index would have to be invalidated by every
        /// AddChild, RemoveChild and StyleId assignment, and a static one would collide across
        /// documents, since WidgetManager's state is process-wide and the same document may be
        /// loaded more than once. The ceiling is O(n) per lookup over a dialog's worth of widgets
        /// -- the sample login dialog is 14 -- paid while the dialog is being built and never in
        /// a frame, so D144 does not reach it. Upgrade path: a Dictionary built by the loader and
        /// owned by the root panel, once a document is large enough for the walk to profile.
        ///
        /// Public rather than internal because <see cref="WidgetManager.LoadXHTML"/> takes an
        /// <see cref="IWindowContainer"/> as the document body, and that container is often a
        /// <see cref="Window"/> rather than a panel -- the sample's own dialog loads into one.
        /// Binding a loaded document to code has to start somewhere, and the panel that would
        /// answer the instance overload is itself the first thing a caller has to find.
        /// </summary>
        public static bool TryFind<T>(IWindowContainer container, string id, out T widget) where T : Widget
        {
            foreach (WindowObject child in container.Children)
            {
                Widget childWidget = child as Widget;

                if (childWidget == null)
                    continue; // a plain WindowObject carries no id

                if (childWidget.StyleId == id)
                {
                    // an id is unique, so the element carrying it either is the class the caller
                    // asked for or the lookup has failed -- searching on could only find a
                    // duplicate the document should not have
                    widget = childWidget as T;
                    return widget != null;
                }

                IWindowContainer childContainer = childWidget as IWindowContainer;

                if (childContainer != null && TryFind(childContainer, id, out widget))
                    return true;
            }

            widget = null;
            return false;
        }
    }

    /// <summary>
    /// The widget an element this library has never heard of becomes. A mainstream HTML editor
    /// emits <c>&lt;section&gt;</c>, <c>&lt;form&gt;</c>, <c>&lt;fieldset&gt;</c> and
    /// <c>&lt;select&gt;</c> from its toolbar, and skipping one used to take every control
    /// nested inside it down as well, so wrapping two controls in a <c>&lt;section&gt;</c> made
    /// both disappear. This is an ordinary panel that carries the tag as its element type
    /// instead of <c>panel</c>, so that a <c>section { }</c> rule matches it -- which is what an
    /// HTML author expects -- and a <c>panel</c> rule written for real panels does not.
    ///
    /// It draws nothing of its own and no rule positions it unless the author writes one, so it
    /// is a zero-sized box at the origin and its children keep the coordinates they would have
    /// had without the wrapper. That is the whole of its geometry: D134's profile is absolute
    /// positioning, and nothing here arranges anything.
    ///
    /// ponytail: a percentage-sized child resolves against this box, so a child of an unstyled
    /// wrapper resolves a percentage against zero, where a browser would skip a wrapper that is
    /// not itself positioned and resolve against the nearest positioned ancestor. The ceiling is
    /// one nesting level per unstyled wrapper; the upgrade path is for the wrapper to report its
    /// own containing block as its size, which needs a containing-block rule of its own.
    /// </summary>
    public class WidgetMarkupElement : WidgetPanel
    {
        public WidgetMarkupElement(string elementType, WidgetStyle style)
            : base(elementType, style)
        {
        }
    }
}

