using System.Numerics;
using RunMobile.Utility;

namespace RunMobile.Graphics
{
    /// <summary>
    /// Sprite class. Used for drawing textured rectangles such as UI elements
    /// In very rare cases should be addresed directly. Method Draw() submits the data to low-level library
    /// </summary>
    public class Sprite : NewWidgets.UI.ISprite
    {
        private static readonly int AlphaDrawThreshold = 0x02;

        private readonly IGraphicsTexture m_texture;
        private readonly SpriteData[] m_frames;
        private uint m_color;

        private Vector2 m_pivotShift;

        private int m_frame;

        //private uint m_color;
        private RectangleF m_screenRect;


        private TextureFlags m_flags;

        // transformation
        private readonly Transform m_transform;
        private int m_transformVersion = -1;


        private SpriteData[] Frames
        {
            get { return m_frames; }
        }

        public Vector2 Size
        {
            get { return new Vector2(Frames[m_frame].OriginalWidth, Frames[m_frame].OriginalHeight); }
        }

        public Vector2 FrameSize
        {
            get { return new Vector2(Frames[m_frame].Width, Frames[m_frame].Height); }
        }

        public RectangleF ScreenRect
        {
            get
            {
                EnsureScreenRect();

                return m_screenRect;
            }
        }

        public Transform Transform
        {
            get { return m_transform; }
        }

        public Vector2 PivotShift
        {
            get { return m_pivotShift; }
            set { m_pivotShift = value; }
        }

        public IGraphicsTexture Texture
        {
            get { return m_texture; }
        }

        /// <summary>
        /// Number of current frame
        /// </summary>
        /// <value>The frame.</value>
        public int Frame
        {
            get
            {
                return m_frame;
            }
            set
            {
                m_frame = value % Frames.Length;
                m_transformVersion = 0; // FrameRect could change so we need to invalidate screen rectangle
            }
        }

        /// <summary>
        /// Number of frames for current animation
        /// </summary>
        /// <value>The frames.</value>
        public int FrameCount
        {
            get { return Frames.Length; }
        }

        /// <summary>
        /// Get current rame tag
        /// </summary>
        /// <value>The frame tag.</value>
        public int FrameTag
        {
            get { return Frames[m_frame].Tag; }
        }

        /// <summary>
        /// Sprite transparency from 0 to 255
        /// </summary>
        /// <value>The alpha.</value>
        public byte Alpha
        {
            get { return (byte)((m_color & 0xff000000) >> 24); }
            set { m_color = (m_color & 0x00ffffff) | ((uint)value << 24); }
        }

        /// <summary>
        /// Sprite color, up to 0xffffff
        /// </summary>
        /// <value>The color.</value>
        public uint Color
        {
            get { return m_color & 0x00ffffff; }
            set { m_color = (value & 0x00ffffff) | (m_color & 0xff000000); }
        }

        /// <summary>
        /// Gets or sets texture flags. Passed to shader
        /// </summary>
        /// <value>The flags.</value>
        public TextureFlags Flags
        {
            get { return m_flags; }
            set { m_flags = value; }
        }

        /// <summary>
        /// Sprite that takes its parameters from first texture of the provided material
        /// </summary>
        /// <param name="material"></param>
        public Sprite(IGraphicsTexture texture, SpriteData[] frames, TextureFlags flags = 0)
        {
            m_texture = texture;
            m_frames = frames;
            m_pivotShift = new Vector2(0, 0);
            m_transform = new Transform();
            m_frame = 0;
            m_transformVersion = 0;
            m_flags = flags;
            m_color = 0xffffffff;
        }

        /// <summary>
        /// Clone the sprite
        /// </summary>
        /// <param name="other"></param>
        public Sprite(Sprite other)
        {
            m_pivotShift = other.m_pivotShift;
            m_transform = new Transform();
            m_frame = other.m_frame;
            m_transformVersion = 0;
            m_flags = other.m_flags;
            m_texture = other.m_texture;
            m_frames = other.m_frames;
            m_color = other.m_color;
        }

        ~Sprite()
        {
            Release();
        }

        public void Release()
        {
            //m_currentAnimation = null;
            //m_color = 0;
        }

        public void Update()
        {

            //m_transform.PrepareMatrix();
        }

        public void Draw()
        {
            if (Alpha <= AlphaDrawThreshold)
                return;

            m_transform.EnsureMatrix();

            GraphicsManager.Instance.DrawTexture(m_texture, ref m_frames[m_frame], ref m_transform.MatrixReference, m_color, m_flags, m_pivotShift);
        }

        public bool HitTest(float px, float py)
        {
            if (!ScreenRect.Contains(px, py)) // AABB test
                return false;

            // OOBB test

            m_transform.EnsureMatrix();

            Vector2 coord = m_transform.GetClientPoint(new Vector2(px, py)) + m_pivotShift * FrameSize;

            return coord.X >= 0 && coord.Y >= 0 && coord.X < FrameSize.X && coord.Y < FrameSize.Y;
        }

        private void EnsureScreenRect()
        {
            m_transform.EnsureMatrix();

            if (m_transformVersion == m_transform.Version)
                return;

            Vector2 from = m_pivotShift * FrameSize;

            Vector3[] arr = new Vector3[4];

            var transform = m_transform.MatrixReference;

            Vector3 zero = transform.Translation;
#if USE_NUMERICS || NETSTANDARD || NET47 || NET47_OR_GREATER || NET472 || NETCOREAPP
            Vector3 oneVectorX = new Vector3(transform.M11, transform.M12, transform.M13);
            Vector3 oneVectorY = new Vector3(transform.M21, transform.M22, transform.M23);
#else
            Vector3 oneVectorX = transform.Row1;
            Vector3 oneVectorY = transform.Row2;
#endif
            Vector3 width = oneVectorX * FrameSize.X;
            Vector3 height = oneVectorY * FrameSize.Y;

            arr[0] = zero - oneVectorX * from.X - oneVectorY * from.Y;
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

        public override string ToString()
        {
            return string.Format("Sprite: {0} frames, first frame texture {1}", Frames.Length, m_texture);
        }
    }
}

