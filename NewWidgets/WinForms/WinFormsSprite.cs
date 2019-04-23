using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;

using NewWidgets.UI;
using NewWidgets.Utility;

namespace NewWidgets.WinForms
{
    public class WinFormsSprite : ISprite
    {
        public struct FrameData
        {
            public readonly int X;
            public readonly int Y;
            public readonly int Width;
            public readonly int Height;
            public readonly int OffsetX;
            public readonly int OffsetY;

            public readonly int Tag;

            public FrameData(int x, int y, int width, int height, int offsetX, int offsetY, int tag)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
                OffsetX = offsetX;
                OffsetY = offsetY;
                Tag = tag;
            }
        }

        private static uint ColorMask = 0x00ffffff;
        private static uint AlphaMask = 0xff000000;
        private static uint AlphaDrawThreshold = 0x01000000; // alpha <= 1 means invisible

        private readonly string m_id;
        private readonly Vector2 m_size;
        private readonly FrameData[] m_frames;
        private readonly Image m_image;

        private Transform m_transform;

        private Vector2 m_pivotShift;
        private uint m_color;
        private int m_frame;

        private uint m_cacheHash;
        private Bitmap m_cache;

        public string Id
        {
            get { return m_id; }
        }

        public Vector2 Size
        {
            get { return m_size; }
        }

        public Vector2 PivotShift
        {
            get { return m_pivotShift; }
            set { m_pivotShift = value; }
        }

        public int Frame
        {
            get { return m_frame; }
            set
            {
                Debug.Assert(value >= 0 && value < m_frames.Length, "Invalid frame number");
                m_frame = value;
            }
        }

        public int Frames
        {
            get { return m_frames.Length; }
        }

        public int FrameTag
        {
            get { return m_frames[m_frame].Tag; }
        }

        public Vector2 FrameSize
        {
            get { return new Vector2(m_frames[m_frame].Width, m_frames[m_frame].Height); }
        }

        public int Alpha
        {
            get { return (int)((m_color & AlphaMask) >> 24); }
            set
            {
                value = MathHelper.Clamp(value, 0, 255);
                m_color = (m_color & ColorMask) | ((uint)value << 24);
            }
        }

        public int Color
        {
            get { return (int)(m_color & ColorMask); }
            set { m_color = ((uint)value & ColorMask) | (m_color & AlphaMask); }
        }

        public Transform Transform
        {
            get { return m_transform; }
        }

        public float Scale
        {
            get { return m_transform.UniformScale; }
            set { m_transform.UniformScale = value; }
        }

        public Vector2 Position
        {
            get { return m_transform.FlatPosition; }
            set { m_transform.FlatPosition = value; }
        }

        public float Rotation
        {
            get { return m_transform.RotationZ; }
            set { m_transform.RotationZ = value; }
        }

        internal WinFormsSprite(Image image, string id, Vector2 size, FrameData[] frames)
        {
            m_image = image;
            m_id = id;
            m_size = size;
            m_frames = frames;


            m_pivotShift = Vector2.Zero;
            m_transform = new Transform(Vector2.Zero, 0, 1.0f);
            m_frame = 0;
            m_color = 0xffffffff;
        }

        public bool HitTest(float x, float y)
        {
            if (!GetScreenRect().Contains(x, y)) // AABB test
                return false;

            // OOBB test

            Vector2 coord = m_transform.GetClientPoint(new Vector2(x, y)) + m_pivotShift * FrameSize;

            return coord.X >= 0 && coord.Y >= 0 && coord.X < Size.X && coord.Y < Size.Y;
        }

        public void Draw(object canvas)
        {
            if ((m_color & AlphaMask) <= AlphaDrawThreshold)
                return;

            Graphics graphics = canvas as Graphics;

            if (graphics == null)
                return;

            // Clipping!
            graphics.SetClip(((WinFormsController)WindowController.Instance).GetClipRect);

            // Here goes sprite drawing

            Vector2 from = -m_pivotShift * FrameSize + new Vector2(m_frames[m_frame].OffsetX, m_frames[m_frame].OffsetY);

            Vector2[] arr = new Vector2[3];
            arr[0] = m_transform.GetScreenPoint(from);
            arr[1] = m_transform.GetScreenPoint(from + new Vector2(FrameSize.X, 0));
            arr[2] = m_transform.GetScreenPoint(from + new Vector2(0, FrameSize.Y));

            ImageAttributes ia = new ImageAttributes();
            ColorMatrix matrix = new ColorMatrix();
            matrix.Matrix00 = ((m_color >> 16) & 0xff) / 255.0f;
            matrix.Matrix11 = ((m_color >> 8) & 0xff) / 255.0f;
            matrix.Matrix22 = ((m_color >> 0) & 0xff) / 255.0f;
            matrix.Matrix33 = ((m_color >> 24) & 0xff) / 255.0f;
            ia.SetColorMatrix(matrix);

            UpdateCache(); // make sure that cache is valid
           
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;

                if (m_cache != null)
                {
                    // round everything to ints to avoid blur and increase speed

                    graphics.DrawImage(m_cache,
                       new Point[] { new Point((int)(arr[0].X + 0.5f), (int)(arr[0].Y + 0.5f)), new Point((int)(arr[1].X + 0.5f), (int)(arr[1].Y + 0.5f)), new Point((int)(arr[2].X + 0.5f), (int)(arr[2].Y + 0.5f)) },
                       new Rectangle(0, 0, m_cache.Width, m_cache.Height),
                     GraphicsUnit.Pixel,
                     ia
                     );
                }
            }
        }

        private void UpdateCache()
        {
            int nwidth = (int)System.Math.Ceiling(m_transform.ActualScale.X * FrameSize.X);
            int nheight = (int)System.Math.Ceiling(m_transform.ActualScale.Y * FrameSize.Y);
            uint cacheHash = ((uint)(m_frame & 0xff) << 24) | ((uint)nwidth << 12) | (uint)nheight;

            if (cacheHash != m_cacheHash)
            {
                if (m_cache != null)
                {
                    m_cache.Dispose();
                    m_cache = null;
                }

                if (nwidth > 0 && nheight > 0)
                {
                    m_cache = new Bitmap(nwidth, nheight);
                    using (Graphics g = Graphics.FromImage(m_cache))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        g.DrawImage(m_image,
                        new RectangleF(0, 0, m_cache.Width, m_cache.Height),
                        new Rectangle(m_frames[m_frame].X, m_frames[m_frame].Y, m_frames[m_frame].Width, m_frames[m_frame].Height),
                        GraphicsUnit.Pixel);
                    }
                }
                m_cacheHash = cacheHash;
            }
        }

        public void Update()
        {
        }

        private RectangleF GetScreenRect()
        {
            Vector2 from = -m_pivotShift * FrameSize + new Vector2(m_frames[m_frame].OffsetX, m_frames[m_frame].OffsetY);

            Vector2[] arr = new Vector2[4];
            arr[0] = m_transform.GetScreenPoint(from);
            arr[1] = m_transform.GetScreenPoint(from + new Vector2(FrameSize.X, 0));
            arr[2] = m_transform.GetScreenPoint(from + new Vector2(0, FrameSize.Y));
            arr[3] = m_transform.GetScreenPoint(from + FrameSize);

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

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }
    }
}
