using System;
using System.Drawing;
using RunServer.Common;
using RunServer.Math;

namespace StationHelper
{
    public interface Shape : IComparable<Shape>
    {
        void Draw(Graphics g, int order);

        bool Init(Matrix transform, RectangleF bounds);

        object Tag { get; }

        bool HitTest(Point point);
    }

    public class PolygonShape : Shape
    {
        private readonly Vector[] m_points;
        private readonly Vector m_normal;
        private readonly PointF[] m_fpoints;
        private readonly Brush m_brush;

        private Vector m_lowerBound;
        private Vector m_upperBound;
        private float m_z;

        public Vector Normal
        {
            get { return m_normal; }
        }

        public Vector UpperBound
        {
            get { return m_upperBound; }
        }

        public Vector LowerBound
        {
            get { return m_lowerBound; }
        }

        public Vector[] Points
        {
            get { return m_points; }
        }

        public float Z
        {
            get { return m_z; }
        }

        public object Tag
        {
            get { return null; }
        }

        public PolygonShape(Vector[] points, Vector normal, Brush brush)
        {
            m_points = points;
            m_normal = normal;
            m_fpoints = new PointF[points.Length];
            m_brush = brush;
        }

        public bool Init(Matrix transform, RectangleF bounds)
        {
            m_lowerBound = new Vector(float.MaxValue, float.MaxValue, float.MaxValue);
            m_upperBound = new Vector(float.MinValue, float.MinValue, float.MinValue);

            for (int i = 0; i < m_points.Length; i++)
            {
                Vector point = transform * m_points[i];
                m_fpoints[i] = new PointF(point.X, point.Y);
                if (point.X < m_lowerBound.X)
                    m_lowerBound.X = point.X;
                if (point.Y < m_lowerBound.Y)
                    m_lowerBound.Y = point.Y;
                if (point.Z < m_lowerBound.Z)
                    m_lowerBound.Z = point.Z;
                if (point.X > m_upperBound.X)
                    m_upperBound.X = point.X;
                if (point.Y > m_upperBound.Y)
                    m_upperBound.Y = point.Y;
                if (point.Z > m_upperBound.Z)
                    m_upperBound.Z = point.Z;
            }

            Vector normal = Vector.Normalize(transform * m_normal - transform * Vector.Zero);

            bool empty = m_points.Length == 0 || Vector.Dot(normal, new Vector(0, 0, -1)) <= 0 || LowerBound.DistanceFlat(UpperBound) < 1 || !MathHelper.BoundIntersection(new Vector(m_lowerBound.X, m_lowerBound.Y, 0), new Vector(m_upperBound.X, m_upperBound.Y, 0), bounds.Left, bounds.Top, 0, bounds.Right, bounds.Bottom, 0);
            m_z = m_lowerBound.Z;

            return !empty;
        }

        public void Draw(Graphics g, int order)
        {
            g.FillPolygon(m_brush, m_fpoints);
            g.DrawPolygon(Pens.Black, m_fpoints);
            //g.DrawString(order.ToString(), SystemFonts.DefaultFont, Brushes.Black, Points[0].X, Points[0].Y);
        }

        public int CompareTo(Shape sother)
        {
            PolygonShape other = sother as PolygonShape;
            if (other != null)
            {
                if (LowerBound.Z - float.Epsilon >= other.UpperBound.Z)
                    return 1;

                if (UpperBound.Z <= other.LowerBound.Z + float.Epsilon)
                    return -1;

                return Z.CompareTo(other.Z);
#if FALSE
                //if (!MathHelper.BoundOverlapFlat(LowerBound, UpperBound, other.LowerBound, other.UpperBound))
                  //  return UpperBound.Z.CompareTo(other.UpperBound.Z);

                /*foreach (Vector vector in other.Points)
                {
                    if (vector.X > LowerBound.X && vector.X < UpperBound.X && vector.Y > LowerBound.Y && vector.Y < UpperBound.Y)
                    {
                        float ownz = (Vector.Dot(Normal, Points[0]) - vector.X * Normal.X - vector.Y * Normal.Y) / Normal.Z;
                        if (vector.Z - ownz > 1)
                            return 1;
                    }
                }*/

                //float olbz = (Vector.Dot(Normal, Points[0]) - other.LowerBound.X * Normal.X - other.LowerBound.Y * Normal.Y) / Normal.Z;
                //float olbz = (Vector.Dot(Normal, Points[0]) - other.LowerBound.X * Normal.X - other.LowerBound.Y * Normal.Y) / Normal.Z;
                //float oubz = (Vector.Dot(Normal, Points[0]) - other.UpperBound.X * Normal.X - other.UpperBound.Y * Normal.Y) / Normal.Z;)

                foreach (Vector vector in Points)
                {
                    float olbz = (Vector.Dot(other.Normal, other.Points[0]) - vector.X*other.Normal.X - vector.Y * other.Normal.Y) / other.Normal.Z;
                    if (olbz < vector.Z)
                        return 1;
                }

                return -1;
                //return olbz.CompareTo(other.Z);

                return Z.CompareTo(other.Z);
                return UpperBound.Z.CompareTo(other.UpperBound.Z);
                

                int scorea = 0;
                int scoreb = 0;

                foreach (Vector vector in Points)
                {
                    if (vector.X > other.LowerBound.X && vector.X < other.UpperBound.X && vector.Y > other.LowerBound.Y && vector.Y < other.UpperBound.Y)
                    {
                        float otherz = (Vector.Dot(other.Normal, other.Points[0]) - vector.X * other.Normal.X - vector.Y * other.Normal.Y) / other.Normal.Z;
                        if (otherz < vector.Z)
                            scorea++;
                    }
                }

                foreach (Vector vector in other.Points)
                {
                    if (vector.X > LowerBound.X && vector.X < UpperBound.X && vector.Y > LowerBound.Y && vector.Y < UpperBound.Y)
                    {
                        float ownz = (Vector.Dot(Normal, Points[0]) - vector.X * Normal.X - vector.Y * Normal.Y) / Normal.Z;
                        if (ownz < vector.Z)
                            scoreb++;
                        
                            /*scorea++;
                        else*/
                            //scoreb++;
                    }
                }

                //if (score != 0)
                    //return score > 0 ? 1 : -1;
                //if (scorea + scoreb == 0 || scorea == scoreb)
                  //  return (UpperBound.Z + LowerBound.Z) > (other.UpperBound.Z + other.LowerBound.Z) ? 1 : -1;*/

                /*if (scorea == 0 && scoreb == 0)
                    return Z.CompareTo(other.Z);*/
                return scorea > 0 ? 1 : scoreb > 0 ? -1 : Z.CompareTo(other.Z);
                //return Z.CompareTo(other.Z);
#endif
            }
            return 1;
        }

        public bool HitTest(Point point)
        {
            return false;
        }
    }

    public class PointShape : Shape
    {
        private readonly Vector m_originalPoint;
        private readonly Brush m_brush;
        private readonly float m_radius;
        private readonly float m_diameter;
        private Vector m_point;

        public object Tag
        {
            get { return null; }
        }

        public PointShape(Vector point, Brush brush, float radius)
        {
            m_originalPoint = point;
            m_brush = brush;
            m_radius = radius;
            m_diameter = radius * 2;
        }

        public void Draw(Graphics g, int order)
        {
            g.FillEllipse(m_brush, m_point.X - m_radius, m_point.Y - m_radius, m_diameter, m_diameter);
            g.DrawEllipse(Pens.Black, m_point.X - m_radius, m_point.Y - m_radius, m_diameter, m_diameter);
        }

        public int CompareTo(Shape other)
        {
            return 1;
        }

        public bool Init(Matrix transform, RectangleF bounds)
        {
            m_point = transform * m_originalPoint;
            return bounds.Contains(new PointF(m_point.X, m_point.Y));
            //return true;
        }

        public bool HitTest(Point point)
        {
            return false;
        }
    }

    public class LineShape : Shape
    {
        private readonly Vector m_originalFrom;
        private readonly Vector m_originalTo;
        private readonly Pen m_pen;

        private Vector m_from;
        private Vector m_to;

        public object Tag
        {
            get { return null; }
        }

        public LineShape(Vector from, Vector to, Pen pen)
        {
            m_originalFrom = from;
            m_originalTo = to;
            m_pen = pen;
        }

        public void Draw(Graphics g, int order)
        {
            //g.SmoothingMode = SmoothingMode.AntiAlias;
            //g.DrawLines(m_pen, m_points);
            g.DrawLine(m_pen, m_from.X, m_from.Y, m_to.X, m_to.Y);
            //g.SmoothingMode = SmoothingMode.None;
        }

        public int CompareTo(Shape other)
        {
            return 1;
        }

        public bool Init(Matrix transform, RectangleF bounds)
        {
            m_from = transform * m_originalFrom;
            m_to = transform * m_originalTo;
            return m_from.DistanceFlat(m_to) > float.Epsilon;
        }

        public bool HitTest(Point point)
        {
            return false;
        }
    }
}
