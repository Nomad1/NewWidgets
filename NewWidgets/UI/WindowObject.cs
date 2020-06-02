using System;
using System.Numerics;
using NewWidgets.Utility;

#if RUNMOBILE
using RunMobile.Utility;
#endif

namespace NewWidgets.UI
{
    public enum SpecialKey
    {
        None = 0,
        Menu = 1,
        Back = 2,
        Left = 3,
        Right = 4,
        Up = 5,
        Down = 6,
        Select = 7,
        Enter = 8,
        Tab = 9,

        Home = 10,
        End = 11,

        Slash,
        BackSlash,
        Semicolon,
        Quote,
        Comma,
        Period,
        Minus,
        Plus,
        BracketLeft,
        BracketRight,
        Tilde,
        Grave,
        Backspace,
        Delete,
        EraseLine, // combination of Ctrl+Backspace or Cmd+Backspace

        Letter,

        A,
        B,
        C,
        D,
        E,
        F,
        G,
        H,
        I,
        J,
        K,
        L,
        M,
        N,
        O,
        P,
        Q,
        R,
        S,
        T,
        U,
        V,
        W,
        X,
        Y,
        Z,

        One,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Zero,

        Shift,
        Paste,
        Control,

        Joystick_Up,
        Joystick_Down,
        Joystick_Left,
        Joystick_Right,
        Joystick_A,
        Joystick_B,
        Joystick_X,
        Joystick_Y,
        Joystick_LBumper,
        Joystick_RBumper,
        Joystick_Start,
        Joystick_Back,
        Joystick_RTrigger,
        Joystick_LTrigger,

        Max
    }

    [Flags]
    public enum WindowObjectFlags
    {
        None = 0x00,
        Removing = 0x01,
        Visible = 0x02,
        Enabled = 0x04,
        Changed = 0x08,
        Selected = 0x10,
        Hovered = 0x20,

        Default = Visible | Enabled | Changed
    }

    public class WindowObject
    {
        private readonly Transform m_transform;

        private Vector2 m_size;
        private object m_tag;
        private int m_zIndex;
        private int m_tempZIndex;

        private WindowObject m_parent;

        private WindowObjectFlags m_flags;

        private object m_lastList;

        public event TouchDelegate OnTouch;
        public event Action<WindowObject, bool> OnVisibilityChange;

        internal object LastList
        {
            get { return m_lastList; }
            set { m_lastList = value; }
        }

        public object Tag
        {
            get { return m_tag; }
            set { m_tag = value; }
        }

        public int ZIndex
        {
            get { return m_zIndex == 0 ? m_tempZIndex : m_zIndex; }
            set { m_zIndex = value; }
        }

        internal int TempZIndex
        {
            get { return m_tempZIndex; }
            set { m_tempZIndex = value; }
        }

        public WindowObject Parent
        {
            get { return m_parent; }
            set { m_parent = value; m_transform.Parent = value == null ? null : value.Transform; }
        }

        public Transform Transform
        {
            get { return m_transform; }
        }

        public Vector2 Position
        {
            get { return m_transform.FlatPosition; }
            set { m_transform.FlatPosition = value; }
        }

        /// <summary>
        /// Gets or sets the uniform scale. Setting this value resets non-uniform scale
        /// </summary>
        /// <value>The scale.</value>
        public float Scale
        {
            get { return m_transform.UniformScale; }
            set { m_transform.UniformScale = value; }
        }

        /// <summary>
        /// Z-rotation in degrees
        /// </summary>
        public float Rotation
        {
            get { return m_transform.RotationZ * (float)MathHelper.Rad2Deg; }
            set { m_transform.RotationZ = value * (float)MathHelper.Deg2Rad; }
        }

        public virtual bool Visible
        {
            get { return (m_flags & WindowObjectFlags.Visible) == WindowObjectFlags.Visible; }
            set
            {
                if (value)
                    m_flags |= WindowObjectFlags.Visible;
                else
                    m_flags &= ~WindowObjectFlags.Visible;
                if (OnVisibilityChange != null)
                    OnVisibilityChange(this, value);
            }
        }

        public virtual bool Enabled
        {
            get { return (m_flags & WindowObjectFlags.Enabled) == WindowObjectFlags.Enabled; }
            set
            {
                if (value)
                    m_flags |= WindowObjectFlags.Enabled;
                else
                    m_flags &= ~WindowObjectFlags.Enabled;
            }
        }

        public virtual bool Hovered
        {
            get { return (m_flags & WindowObjectFlags.Hovered) == WindowObjectFlags.Hovered; }
            set
            {
                if (value)
                    m_flags |= WindowObjectFlags.Hovered;
                else
                    m_flags &= ~WindowObjectFlags.Hovered;
            }
        }

        public virtual bool Selected
        {
            get { return (m_flags & WindowObjectFlags.Selected) == WindowObjectFlags.Selected; }
            set
            {
                if (value)
                    m_flags |= WindowObjectFlags.Selected;
                else
                    m_flags &= ~WindowObjectFlags.Selected;
            }
        }

        public bool Removing
        {
            get { return (m_flags & WindowObjectFlags.Removing) == WindowObjectFlags.Removing; }
        }

        internal bool HasChanges
        {
            get
            {
                bool value = (m_flags & WindowObjectFlags.Changed) == WindowObjectFlags.Changed;
                m_flags &= ~WindowObjectFlags.Changed;
                return value;
            }
            set
            {
                if (value)
                    m_flags |= WindowObjectFlags.Changed;
                else
                    m_flags &= ~WindowObjectFlags.Changed;
            }
        }

        public Vector2 Size
        {
            get { return m_size; }
            set
            {
                if (Vector2.DistanceSquared(value, Size) > float.Epsilon)
                    Resize(value);
            }
        }

        /// <summary>
        /// Unwinds hierarchy to find top-level window
        /// </summary>
        /// <value>The window</value>
        public IWindowContainer Window
        {
            get
            {
                if (m_parent == null)
                    return this as IWindowContainer;

                return m_parent.Window;
            }
        }

        public bool IsTopmost
        {
                get
                {
                    IWindowContainer windowParent = this.Parent as IWindowContainer;

                    if (windowParent != null)
                    {
                        int max = windowParent.MaximumZIndex;
                        if (this.ZIndex == max)
                           return true;
                    }
                    return false;
                }
        }

        protected WindowObject(WindowObject parent, Transform transform = null)
        {
            m_parent = parent;
            m_transform = transform == null ? new Transform() : transform;
            if (parent != null)
                m_transform.Parent = parent.Transform;

            m_flags = WindowObjectFlags.Default;
        }

        public virtual void Reset()
        {
            m_flags = WindowObjectFlags.Default;

            Animator.RemoveAnimation(this);
        }

        protected virtual void Resize(Vector2 size)
        {
            m_size = size;
        }

        public virtual bool HitTest(float x, float y)
        {
            Vector2 coord = m_transform.GetClientPoint(new Vector2(x, y));

            return coord.X >= 0 && coord.Y >= 0 && coord.X < Size.X && coord.Y < Size.Y;
        }

        public virtual void Remove()
        {
            m_flags |= WindowObjectFlags.Removing;
        }

        public virtual bool Update()
        {
            if ((m_flags & WindowObjectFlags.Removing) != 0)
                return false;

            return true;
        }

        public virtual void Draw(object canvas)
        {

        }

        public virtual bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            if (Enabled && OnTouch != null)
                return OnTouch(x, y, press, unpress, pointer);

            return false;
        }

        public virtual bool Zoom(float x, float y, float value)
        {
            return false;
        }

        public virtual bool Key(SpecialKey key, bool up, string keyString)
        {
            return false;
        }

        public virtual void Move(Vector2 point, int time, Action callback)
        {
            Animator.RemoveAnimation(this, AnimationKind.Position);

            Vector2 current = Position;

            if ((point - current).LengthSquared() <= float.Epsilon)
            {
                if (callback != null)
                    callback();
                return;
            }

            Animator.StartAnimation(this, AnimationKind.Position, current, point, time, (float x, Vector2 from, Vector2 to) => Position = MathHelper.LinearInterpolation(x, from, to), callback);
        }

        public virtual void Rotate(float angle, int time, Action callback, bool normalize)
        {
            Animator.RemoveAnimation(this, AnimationKind.Rotation);

            float current = Rotation;

            /* float delta = (angle - current) % 360;

             if (normalize)
             {
                 if (delta > 180)
                     delta -= 360;
                 else if (delta < -180)
                     delta += 360;
             }*/

            Animator.StartAnimation(this, AnimationKind.Rotation, current, angle, time, (float x, float from, float to) => Rotation = MathHelper.LinearInterpolation(x, from, to), callback);
        }

        public virtual void Rotate(float angle, int time, Action callback)
        {
            Rotate(angle, time, callback, true);
        }

        public virtual void ScaleTo(float target, int time, Action callback)
        {
            ScaleTo(new Vector2(target, target), time, callback);
        }

        public virtual void ScaleTo(Vector2 target, int time, Action callback)
        {
            Animator.RemoveAnimation(this, AnimationKind.Scale);

            Vector2 current = Transform.FlatScale;

            if ((target - current).LengthSquared() <= float.Epsilon)
            {
                if (callback != null)
                    callback();
                return;
            }

            Animator.StartAnimation(this, AnimationKind.Scale, current, target, time,
                (float x, Vector2 from, Vector2 to) =>
                Transform.FlatScale = MathHelper.LinearInterpolation(x, from, to), callback);
        }

        public void BringToFront()
        {
            IWindowContainer windowParent = this.Parent as IWindowContainer;

            if (windowParent != null)
            {
                int max = windowParent.MaximumZIndex;
                if (max == 0 || this.ZIndex != max)
                    this.ZIndex = max + 1;
            }
        }
    }
}