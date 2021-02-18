using System;
using System.Numerics;

#if RUNMOBILE
using RunMobile.Utility;
#else
using System.Drawing;
using NewWidgets.Utility;
#endif

namespace NewWidgets.UI
{
    /// <summary>
    /// Basic class for UI elements
    /// </summary>
    public abstract class WindowObject
    {
        private readonly Transform m_transform;

        private Vector2 m_size;
        private object m_tag;
        private int m_zIndex;
        private int m_tempZIndex;

        private int m_transformVersion;
        private RectangleF m_screenRect;

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

        public RectangleF ScreenRect
        {
            get
            {
                if (m_transformVersion != m_transform.Version)
                {
                    Vector2 from = Vector2.Zero;

                    Vector3[] arr = new Vector3[4];

                    var transform = m_transform.Matrix;

                    Vector3 zero = transform.Translation;
                    Vector3 oneVectorX = new Vector3(transform.M11, transform.M12, transform.M13);
                    Vector3 oneVectorY = new Vector3(transform.M21, transform.M22, transform.M23);

                    Vector3 width = oneVectorX * Size.X;
                    Vector3 height = oneVectorY * Size.Y;

                    arr[0] = zero + oneVectorX * from.X + oneVectorY * from.Y;
                    arr[1] = arr[0] + width;
                    arr[2] = arr[0] + height;
                    arr[3] = arr[1] + height;

                    /*// I leave these for double-checking: varr elements should be equal to arr
                     
                    Vector2[] varr = new Vector2[4];
                    varr[0] = m_transform.GetScreenPoint(from);
                    varr[1] = m_transform.GetScreenPoint(from + new Vector2(Size.X, 0));
                    varr[2] = m_transform.GetScreenPoint(from + new Vector2(0, Size.Y));
                    varr[3] = m_transform.GetScreenPoint(from + Size);
                    */

                    float minX = arr[0].X;
                    float minY = arr[0].Y;
                    float maxX = arr[0].X;
                    float maxY = arr[0].Y;

                    for (int i = 1; i < arr.Length; i++)
                    {
                        if (arr[i].X < minX)
                            minX = arr[i].X;

                        if (arr[i].X > maxX)
                            maxX = arr[i].X;

                        if (arr[i].Y < minY)
                            minY = arr[i].Y;

                        if (arr[i].Y > maxY)
                            maxY = arr[i].Y;
                    }

                    m_screenRect = new RectangleF(minX, minY, maxX - minX, maxY - minY);

                    m_transformVersion = m_transform.Version;
                }

                return m_screenRect;
            }
        }

        protected WindowObject(WindowObject parent, Transform transform = null)
        {
            m_parent = parent;
            m_transform = transform ?? new Transform();
            if (parent != null)
                m_transform.Parent = parent.Transform;

            m_flags = WindowObjectFlags.Default;
        }

        public virtual void Reset()
        {
            m_flags = WindowObjectFlags.Default;

            AnimationManager.Instance.RemoveAnimation(this);
        }

        protected virtual void Resize(Vector2 size)
        {
            m_size = size;
        }

        public virtual bool HitTest(float x, float y)
        {
            if (!ScreenRect.Contains(x, y)) // AABB test
                return false;

            // OOBB test
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

        public virtual void Draw()
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
            AnimationManager.Instance.RemoveAnimation(this, AnimationKind.Position);

            Vector2 current = Position;

            if ((point - current).LengthSquared() <= float.Epsilon)
            {
                if (callback != null)
                    callback();
                return;
            }

            AnimationManager.Instance.StartAnimation(this, AnimationKind.Position, current, point, time, (float x, Vector2 from, Vector2 to) => Position = MathHelper.LinearInterpolation(x, from, to), callback);
        }

        public virtual void Rotate(float angle, int time, Action callback, bool normalize)
        {
            AnimationManager.Instance.RemoveAnimation(this, AnimationKind.Rotation);

            float current = Rotation;

            // if we need to normalize the angle it could be done like this:
            /* float delta = (angle - current) % 360;

             if (normalize)
             {
                 if (delta > 180)
                     delta -= 360;
                 else if (delta < -180)
                     delta += 360;
             }*/

            AnimationManager.Instance.StartAnimation(this, AnimationKind.Rotation, current, angle, time, (float x, float from, float to) => Rotation = MathHelper.LinearInterpolation(x, from, to), callback);
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
            AnimationManager.Instance.RemoveAnimation(this, AnimationKind.Scale);

            Vector2 current = Transform.FlatScale;

            if ((target - current).LengthSquared() <= float.Epsilon)
            {
                if (callback != null)
                    callback();
                return;
            }

            AnimationManager.Instance.StartAnimation(this, AnimationKind.Scale, current, target, time,
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