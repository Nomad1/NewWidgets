using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using RunServer.Common;
using RunServer.Math;
using System.Collections.Generic;

namespace StationHelper
{
    public enum ImageAction
    {
        None = 0,
        Move = 1,
        Scale = 2,
        Rotate = 3
    }

    [Flags]
    public enum ImageStateFlags
    {
        None = 0,
        Selected = 0x01,
        Movable = 0x02,
        Scalable = 0x04,
        Rotatable = 0x08,
        Outdated = 0x10
    }

    public class ImageShape : Shape
    {
        public delegate void ImageActionDelegate(ImageShape owner, ImageAction action, Point point, bool end);

        const int HANDLE_SIZE = 6;
        const int MOVE_HANDLE_SIZE = 14;
        const int ROTATE_HANDLE_SIZE = 10;
        const int ROTATE_HANDLE_OFFSET = 10;

        private static uint s_shapeId = 0;

        private readonly Vector[] m_points;
        private readonly PointF[] m_fpoints;

        //private readonly Brush m_brush;
        private readonly float m_width;
        private readonly float m_height;
        private readonly float m_pivotX;
        private readonly float m_pivotY;
        private readonly Image m_image;
        private readonly uint m_id;

        private float m_alpha;
        private Image m_resizedImage;
        private object m_tag;
        private Rectangle m_realBounds;
        private int m_color;

        private ImageStateFlags m_flags;

        private PointF m_tempOffset;
        private float m_tempScale;
        private bool m_forceBehind;

        private ImageAction m_dragAction;

        public event ImageActionDelegate OnAction;

        public uint ID
        {
            get { return m_forceBehind  && !Selected ? (uint)Math.Max((int)(m_id - 1000000), 0) : m_id; }
        }

        public float Alpha
        {
            get { return m_alpha; }
            set { m_alpha = value; }
        }

        public int Color
        {
            get { return m_color; }
            set { m_color = value; }
        }

        public object Tag
        {
            get { return m_tag; }
            set { m_tag = value; }
        }

        public bool Selected
        {
            get { return (m_flags & ImageStateFlags.Selected) != 0; }
            set { m_flags = value ? m_flags | ImageStateFlags.Selected : m_flags & ~ImageStateFlags.Selected; }
        }

        public bool Movable
        {
            get { return (m_flags & ImageStateFlags.Movable) != 0; }
            set { m_flags = value ? m_flags | ImageStateFlags.Movable : m_flags & ~ImageStateFlags.Movable; }
        }

        public bool Scalable
        {
            get { return (m_flags & ImageStateFlags.Scalable) != 0; }
            set { m_flags = value ? m_flags | ImageStateFlags.Scalable : m_flags & ~ImageStateFlags.Scalable; }
        }

        public bool Rotatable
        {
            get { return (m_flags & ImageStateFlags.Rotatable) != 0; }
            set { m_flags = value ? m_flags | ImageStateFlags.Rotatable : m_flags & ~ImageStateFlags.Rotatable; }
        }

        public float Width
        {
            get { return m_width; }
        }

        public float Height
        {
            get { return m_height; }
        }

        public PointF Center
        {
            get { return new PointF(m_fpoints[0].X + m_resizedImage.Width / 2, m_fpoints[0].Y + m_resizedImage.Height / 2); }
        }

        public PointF Pivot
        {
            get { return new PointF(m_pivotX, m_pivotY); }
        }

        public PointF TempOffset
        {
            get { return m_tempOffset; }
            set { m_tempOffset = value; }
        }

        public float TempScale
        {
            get { return m_tempScale; }
            set { m_tempScale = value; }
        }

        public bool ForceBehind
        {
            get { return m_forceBehind; }
            set { m_forceBehind = value; }
        }

        private static Dictionary<string, TypedWeakReference<Image>> s_imageCache = new Dictionary<string, TypedWeakReference<Image>>();

        private static Image GetImage(string name)
        {
            Image result = null;
            TypedWeakReference<Image> reference;
            if (s_imageCache.TryGetValue(name, out reference) && reference.IsAlive)
                result = reference.Target;

            if (result == null)
            {
                result = Image.FromFile(name, true);
                s_imageCache[name] = new TypedWeakReference<Image>(result);
            }
            return result;
        }

        public ImageShape(uint id, Vector point, string imageName, float scale, float alpha = 1.0f, float pivotX = 0.5f, float pivotY = 0.5f)
        {
            m_id = id == 0 ? ++s_shapeId : id;
            m_image = GetImage(imageName);
            //((Bitmap)m_image).MakeTransparent(Color.White);
            m_width = m_image.Width * scale;
            m_height = m_image.Height * scale;

            m_tempScale = 1.0f;
            m_tempOffset = new PointF(0,0);
            m_alpha = alpha;
            m_color = 0xffffff;

            m_pivotX = pivotX;
            m_pivotY = pivotY;

            m_points = new Vector[] {
                point + new Vector(-m_width * pivotX, -m_height * pivotY, 0),
                point + new Vector(m_width * (1.0f - pivotX), -m_height * pivotY, 0),
                point + new Vector(-m_width * pivotX, m_height * (1.0f - pivotY), 0),
                //point + new Vector(-m_width / 2, m_height / 2, 0),
            };

            m_fpoints = new PointF[3];

            //m_brush = new TextureBrush(m_image, System.Drawing.Drawing2D.WrapMode.Clamp);
        }

        public void InitMoving(EventHandler moveCallback)
        {

        }

        void Shape.Draw(Graphics g, int order)
        {
            if (m_resizedImage == null)
                return;

            if (m_tempScale != 1.0f)
            {
                float newWidth = m_resizedImage.Width * m_tempScale;
                float newHeight = m_resizedImage.Height * m_tempScale;

                g.DrawImage(m_resizedImage,
                            new RectangleF(
                               (int)(m_fpoints[0].X + m_tempOffset.X - (newWidth - m_resizedImage.Width) * m_pivotX),
                                (int)(m_fpoints[0].Y + m_tempOffset.Y - (newHeight - m_resizedImage.Height) * m_pivotY),
                            (int)newWidth, (int)newHeight));
            }
            else
               // g.DrawImage(m_resizedImage, (int)(m_fpoints[0].X + m_tempOffset.X + 0.5f), (int)(m_fpoints[0].Y + m_tempOffset.Y + 0.5f));
            g.DrawImage(m_resizedImage, (m_fpoints[0].X + m_tempOffset.X), (m_fpoints[0].Y + m_tempOffset.Y));

            if (Selected)
            {
                Rectangle border = new Rectangle((int)(m_fpoints[0].X + m_realBounds.X + m_tempOffset.X), (int)(m_fpoints[0].Y + m_realBounds.Y + m_tempOffset.Y), m_realBounds.Width, m_realBounds.Height);
                ControlPaint.DrawBorder(g, border, System.Drawing.Color.Black, ButtonBorderStyle.Dashed);

                if (Scalable)
                {
                    Rectangle NW = new Rectangle(border.X - HANDLE_SIZE / 2, border.Y - HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE);
                    Rectangle NE = new Rectangle(border.X + border.Width - HANDLE_SIZE - 1 + HANDLE_SIZE / 2, border.Y - HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE);
                    Rectangle SW = new Rectangle(border.X - HANDLE_SIZE / 2, border.Y + border.Height - HANDLE_SIZE - 1 + HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE);
                    Rectangle SE = new Rectangle(border.X + border.Width - HANDLE_SIZE - 1 + HANDLE_SIZE / 2, border.Y + border.Height - HANDLE_SIZE - 1 + HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE);

                    ControlPaint.DrawGrabHandle(g, NW, true, true);
                    ControlPaint.DrawGrabHandle(g, NE, true, true);
                    ControlPaint.DrawGrabHandle(g, SW, true, true);
                    ControlPaint.DrawGrabHandle(g, SE, true, true);
                }

                if (Movable)
                {
                    Rectangle center = new Rectangle(border.X + border.Width / 2 - MOVE_HANDLE_SIZE / 2, border.Y + border.Height / 2 - MOVE_HANDLE_SIZE / 2, MOVE_HANDLE_SIZE, MOVE_HANDLE_SIZE);

                    ControlPaint.DrawContainerGrabHandle(g, center);
                }

                if (Rotatable)
                {
                    Rectangle pivot = new Rectangle((int)(m_fpoints[0].X + m_resizedImage.Width / 2 - ROTATE_HANDLE_SIZE / 2 + m_tempOffset.X - ROTATE_HANDLE_OFFSET), (int)(m_fpoints[0].Y + m_resizedImage.Height / 2 - ROTATE_HANDLE_SIZE / 2 + m_tempOffset.Y - ROTATE_HANDLE_OFFSET), ROTATE_HANDLE_SIZE, ROTATE_HANDLE_SIZE);

                    ControlPaint.DrawGrabHandle(g, pivot, false, true);
                }
            }
        }

        public bool HitTest(Point point)
        {
            return m_realBounds.Contains(new Point((int)(point.X - m_fpoints[0].X), (int)(point.Y - m_fpoints[0].Y)));
        }

        public bool ExtendedHitTest(object sender, Point point, bool click)
        {
            if (m_resizedImage == null)
                return false;

            if (m_dragAction != ImageAction.None)
            {
                if (OnAction != null)
                    OnAction(this, m_dragAction, point, click);

                if (click)
                    m_dragAction = ImageAction.None;
                
                return true;
            }

            ImageAction action = ImageAction.None;

            Cursor cursor = Cursors.Default;

            if (Selected)
            {
                Rectangle border = new Rectangle((int)(m_fpoints[0].X + m_realBounds.X + m_tempOffset.X), (int)(m_fpoints[0].Y + m_realBounds.Y + m_tempOffset.Y), m_realBounds.Width, m_realBounds.Height);

                if (Scalable)
                {
                    Rectangle NW = new Rectangle(border.X - HANDLE_SIZE / 2, border.Y - HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE);

                    if (NW.Contains(point))
                    {
                        cursor = Cursors.SizeNWSE;
                        action = ImageAction.Scale;
                    }

                    Rectangle NE = new Rectangle(border.X + border.Width - HANDLE_SIZE - 1 + HANDLE_SIZE / 2, border.Y - HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE);

                    if (NE.Contains(point))
                    {
                        cursor = Cursors.SizeNESW;
                        action = ImageAction.Scale;
                    }

                    Rectangle SW = new Rectangle(border.X - HANDLE_SIZE / 2, border.Y + border.Height - HANDLE_SIZE - 1 + HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE);
                    if (SW.Contains(point))
                    {
                        cursor = Cursors.SizeNESW;
                        action = ImageAction.Scale;
                    }

                    Rectangle SE = new Rectangle(border.X + border.Width - HANDLE_SIZE - 1 + HANDLE_SIZE / 2, border.Y + border.Height - HANDLE_SIZE - 1 + HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE);

                    if (SE.Contains(point))
                    {
                        cursor = Cursors.SizeNWSE;
                        action = ImageAction.Scale;
                    }
                }

                if (Movable)
                {
                    Rectangle center = new Rectangle(border.X + border.Width / 2 - MOVE_HANDLE_SIZE / 2, border.Y + border.Height / 2 - MOVE_HANDLE_SIZE / 2, MOVE_HANDLE_SIZE, MOVE_HANDLE_SIZE);

                    if (center.Contains(point))
                    {
                        cursor = Cursors.SizeAll;
                        action = ImageAction.Move;
                    }
                }

                if (Rotatable)
                {
                    Rectangle pivot = new Rectangle((int)(m_fpoints[0].X + m_resizedImage.Width / 2 - ROTATE_HANDLE_SIZE / 2 + m_tempOffset.X - ROTATE_HANDLE_OFFSET), (int)(m_fpoints[0].Y + m_resizedImage.Height / 2 - ROTATE_HANDLE_SIZE / 2 + m_tempOffset.Y - ROTATE_HANDLE_OFFSET), ROTATE_HANDLE_SIZE, ROTATE_HANDLE_SIZE);

                    if (pivot.Contains(point))
                    {
                        cursor = Cursors.PanEast;
                        action = ImageAction.Rotate;
                    }
                }
            }

            //if(cursor != Cursors.Default)
                //((Control)sender).Cursor = cursor;

            if (action == ImageAction.None)
                return false;

            if (click)
            {
                if (OnAction != null)
                    OnAction(this, action, point, false);
                m_dragAction = action;
            }

            return true;
        }

        public int CompareTo(Shape other)
        {
            if (other is ImageShape)
                return ID.CompareTo(((ImageShape)other).ID);
            return 1;
        }

        public bool Init(Matrix transform, RectangleF bounds)
        {
            for (int i = 0; i < m_points.Length; i++)
            {
                Vector point = transform * m_points[i];
                m_fpoints[i] = new PointF(point.X, point.Y);
            }

            int width = (int)Math.Round(Math.Abs(m_fpoints[1].X - m_fpoints[2].X));
            int height = (int)Math.Round(Math.Abs(m_fpoints[2].Y - m_fpoints[0].Y));

            //m_bounds = new Rectangle((int)Math.Min(m_fpoints[1].X, m_fpoints[2].X), (int)Math.Min(m_fpoints[0].Y, m_fpoints[2].Y), width, height);

            if (m_resizedImage != null)
            {
                if (m_resizedImage.Width != width || m_resizedImage.Height != height)
                {
                    m_resizedImage.Dispose();
                    m_resizedImage = null;
                }
                else
                    return true;
            }

            //if (!bounds.IntersectsWith(new RectangleF(m_fpoints[0].X, m_fpoints[0].Y, width, height)))
                //return false;

            if (width <= 0 || height <= 0)
                return false;

            m_resizedImage = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(m_resizedImage))
            {
                ImageAttributes ia = new ImageAttributes();
                ColorMatrix matrix = new ColorMatrix();
                matrix.Matrix33 = m_alpha;
                matrix.Matrix00 = (m_color >> 16) / 255.0f;
                matrix.Matrix11 = ((m_color >> 8) & 0xff) / 255.0f;
                matrix.Matrix22 = ((m_color & 0xff)) / 255.0f;
                ia.SetColorMatrix(matrix);

                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(m_image, new Rectangle(0, 0, width, height), 0, 0, m_image.Width, m_image.Height, GraphicsUnit.Pixel, ia);
            }
            ((Bitmap)m_resizedImage).MakeTransparent();

            // Here goes the magic. We're scanning bitmap raw pixel data to find top-left and bottom-right non-empty pixels. (c) Nomad

            BitmapData data = ((Bitmap)m_resizedImage).LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            int minx = width;
            int miny = height;
            int maxx = 0;
            int maxy = 0;

            int size = data.Stride * data.Height;

            for (int index = 3; index < size; index += 4)
            {
                byte a = System.Runtime.InteropServices.Marshal.ReadByte(data.Scan0, index);

                if (a != 0)
                {
                    int y = index / data.Stride;
                    int x = (index % data.Stride) >> 2;

                    if (x < minx)
                        minx = x;
                    else if (x > maxx)
                        maxx = x;

                    if (y < miny)
                        miny = y;
                    else if (y > maxy)
                        maxy = y;
                }
            }

            ((Bitmap)m_resizedImage).UnlockBits(data);

            m_realBounds = new Rectangle(minx, miny, maxx - minx + 1, maxy - miny + 1);
            return true;
        }

        public byte[,] GetMask(float scale)
        {
            /*int width = (int)Math.Floor(m_width * scale);
            int height = (int)Math.Floor(m_height * scale);
            byte[,] result = new byte[width + 1, height + 1];

            using (Bitmap resizedImage = new Bitmap(width, height))
            {
                using (Graphics g = Graphics.FromImage(resizedImage))
                {
                    ImageAttributes ia = new ImageAttributes();
                    ColorMatrix matrix = new ColorMatrix();
                    matrix.Matrix33 = 1;
                    ia.SetColorMatrix(matrix);

                    //g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(m_image, new Rectangle(0, 0, width, height), 0, 0, m_image.Width, m_image.Height, GraphicsUnit.Pixel, ia);
                }
                resizedImage.MakeTransparent();

                BitmapData data = resizedImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                int size = data.Stride * data.Height;

                for (int index = 3; index < size; index += 4)
                {
                    byte a = System.Runtime.InteropServices.Marshal.ReadByte(data.Scan0, index);

                    if (a >= 0)
                    {
                        int y = index / data.Stride;
                        int x = (index % data.Stride) >> 2;

                        result[x + 1, y + 1] = a;
                    }
                }

                resizedImage.UnlockBits(data);
            }
*/

            int width = (int)Math.Round(m_image.Width * scale);
            int height = (int)Math.Round(m_image.Height * scale);
            byte[,] result = new byte[width + 1, height + 1];

            int halfPixel = (int)Math.Round(0.5 / scale);

            BitmapData data = ((Bitmap)m_image).LockBits(new Rectangle(0, 0, m_image.Width, m_image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            /*int size = data.Stride * data.Height;

            for (int index = 3; index < size; index += 4)
            {
                byte a = System.Runtime.InteropServices.Marshal.ReadByte(data.Scan0, index);

                if (a >= 0)
                {
                    int y = index / data.Stride;
                    int x = (index % data.Stride) >> 2;

                    result[x + 1, y + 1] = a;
                }
            }*/

            for (int y = 0; y < height + 1; y++)
                for (int x = 0; x < width + 1; x++)
                {
                    int nx = (int)((x) / scale) - halfPixel;
                    int ny = (int)((y) / scale) - halfPixel;

                    if (nx >= 0 && ny >= 0 && nx < m_image.Width && ny < m_image.Height)
                    {
                        int index = (nx + ny * m_image.Width) * 4 + 3;

                        byte a = System.Runtime.InteropServices.Marshal.ReadByte(data.Scan0, index);

                        result[x, y] = a;
                    }
                }

            ((Bitmap)m_image).UnlockBits(data);

            return result;
        }
    }
}
