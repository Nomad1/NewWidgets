using System.Numerics;
using System.Runtime.InteropServices;

namespace NewWidgets.Utility
{
    /// <summary>
    /// Helper struct to store margins and padding. Similar to RectangleF with few helper methods
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [System.ComponentModel.TypeConverter(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public struct Margin
    {
        public static readonly Margin Empty = new Margin(0, 0, 0, 0);

        public readonly float Left;
        public readonly float Top;
        public readonly float Right;
        public readonly float Bottom;

        public Vector2 TopLeft
        {
            get { return new Vector2(Left, Top); }
        }

        public Vector2 BottomRight
        {
            get { return new Vector2(Right, Bottom); }
        }
        
        public Vector2 Size
        {
            get { return new Vector2(Width, Height); }
        }

        public float Width
        {
            get { return Right + Left; }
        }

        public float Height
        {
            get { return Bottom + Top; }
        }

        public Margin(float value)
        {
            Left = value;
            Top = value;
            Right = value;
            Bottom = value;
        }

        public Margin(float left, float top, float right, float bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public Margin(Vector2 leftTop, Vector2 rightBottom)
        {
            Left = leftTop.X;
            Top = leftTop.Y;
            Right = rightBottom.X;
            Bottom = rightBottom.Y;
        }
        
        public override string ToString()
        {
            return string.Format("[Top:{0} Left:{1} Bottom:{2} Right:{3}]", Top, Left, Bottom, Right);
        }

        public static bool IsEmpty(Margin margin)
        {
            return margin.Left == 0 && margin.Top == 0 && margin.Bottom == 0 && margin.Right == 0;
        }
    }

}

