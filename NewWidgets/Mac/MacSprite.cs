using System.Diagnostics;
using System.Numerics;
using CoreGraphics;
using NewWidgets.UI;
using NewWidgets.Utility;

namespace NewWidgets.Mac
{
    public class MacSprite : ISprite
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

        private static readonly uint ColorMask = 0x00ffffff;
        private static readonly uint AlphaMask = 0xff000000;
        private static readonly uint AlphaDrawThreshold = 0x01000000; // alpha <= 1 means invisible

        private readonly string m_id;
        private readonly Vector2 m_size;
        private readonly FrameData[] m_frames;
        private readonly CGImage m_image;

        private Transform m_transform;

        private Vector2 m_pivotShift;
        private uint m_color;
        private int m_frame;

        private CGImage [] m_subImages;

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

        public int FrameCount
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

        public byte Alpha
        {
            get { return (byte)((m_color & AlphaMask) >> 24); }
            set { m_color = (m_color & ColorMask) | ((uint)value << 24); }
        }

        public uint Color
        {
            get { return (uint)(m_color & ColorMask); }
            set { m_color = ((uint)value & ColorMask) | (m_color & AlphaMask); }
        }

        public Transform Transform
        {
            get { return m_transform; }
        }

        internal MacSprite(CGImage image, string id, Vector2 size, FrameData[] frames)
        {
            m_image = image;
            m_id = id;
            m_size = size;
            m_frames = frames;

            m_pivotShift = Vector2.Zero;
            m_transform = new Transform(Vector2.Zero, 0, 1.0f);
            m_frame = 0;
            m_color = 0xffffffff;

            m_subImages = new CGImage[frames.Length];

            for (int i = 0; i < frames.Length; i++)
                if (frames[i].Width != image.Width || frames[i].Height != image.Height)
                    m_subImages[i] = image.WithImageInRect(new CGRect(frames[i].X, frames[i].Y, frames[i].Width, frames[i].Height));
        }

        ~MacSprite()
        {
            for (int i = 0; i < m_subImages.Length; i++)
                if (m_subImages[i] != null)
                    m_subImages[i].Dispose();
        }

        public bool HitTest(float x, float y)
        {
            if (!GetScreenRect().Contains(x, y)) // AABB test
                return false;

            // OOBB test

            Vector2 coord = m_transform.GetClientPoint(new Vector2(x, y)) + m_pivotShift * FrameSize;

            return coord.X >= 0 && coord.Y >= 0 && coord.X < Size.X && coord.Y < Size.Y;
        }


        public void Draw()
        {
            if ((m_color & AlphaMask) <= AlphaDrawThreshold)
                return;

            CGContext context = ((MacController)WindowController.Instance).CurrentContext;

            // Clipping!
            context.ClipToRect(((MacController)WindowController.Instance).ClipRect);

            // Here goes sprite drawing

            Vector2 from = -m_pivotShift * FrameSize + new Vector2(m_frames[m_frame].OffsetX, m_frames[m_frame].OffsetY);

            Vector2[] arr = new Vector2[3];
            arr[0] = m_transform.GetScreenPoint(from);
            arr[1] = m_transform.GetScreenPoint(from + new Vector2(FrameSize.X, 0));
            arr[2] = m_transform.GetScreenPoint(from + new Vector2(0, FrameSize.Y));

            // TODO: set tint color
            context.SetFillColor(CGColor.CreateSrgb(((m_color >> 16) & 0xff) / 255.0f, ((m_color >> 8) & 0xff) / 255.0f, ((m_color >> 0) & 0xff) / 255.0f, ((m_color >> 24) & 0xff) / 255.0f));

            context.SetAlpha(Alpha / 255.0f);

            context.DrawImage(new CGRect(arr[0].X, WindowController.Instance.ScreenHeight - arr[0].Y, arr[1].X - arr[0].X, -(arr[2].Y - arr[0].Y)), m_subImages[m_frame] ?? m_image);

            context.ResetClip();
        }

        public void Update()
        {
        }

        private CGRect GetScreenRect()
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

            return new CGRect(minX, minY, maxX - minX, maxY - minY);
        }
    }
}
