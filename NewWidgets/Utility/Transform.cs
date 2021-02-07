using System.Numerics;

using Matrix = System.Numerics.Matrix4x4;

namespace NewWidgets.Utility
{
    /// <summary>
    /// Helper class that is capable of baking 2d transform into 4x4 matrix with support for inheritance
    /// </summary>
    public class Transform
    {
        private static readonly float AngleEpsilon = float.Epsilon;

        internal Matrix m_matrix;
        internal Matrix4x4 m_imatrix;

        private Vector3 m_rotation;
        private Vector3 m_position;
        private Vector3 m_scale;

        private bool m_hasRotation;
        private bool m_changed;
        private bool m_iMatrixChanged;

        private int m_version;
        private int m_parentVersion;

        private Transform m_parent;

        internal bool IsChanged
        {
            get
            {
                return m_changed || (m_parent != null && (m_parent.m_version != m_parentVersion || m_parent.IsChanged));
            }
        }

        internal int Version
        {
            get { return m_version; }
        }

        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        /// <value>The position vector.</value>
        public Vector3 Position
        {
            get { return m_position; }
            set
            {
                m_changed = true;
                m_position = value;
            }
        }

        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        /// <value>The position vector.</value>
        public Vector2 FlatPosition
        {
            get { return new Vector2(m_position.X, m_position.Y); }
            set
            {
                Position = new Vector3(value.X, value.Y, m_position.Z);
            }
        }

        /// <summary>
        /// Gets or sets the rotation in radians.
        /// </summary>
        /// <value>The rotation vector.</value>
        public Vector3 Rotation
        {
            get { return m_rotation; }
            set
            {
                m_hasRotation = CompareVectors(value, Vector3.Zero, AngleEpsilon);
                m_changed = true;
                m_rotation = value;
            }
        }

        /// <summary>
        /// Gets or sets the Z rotation in radians (!!)
        /// </summary>
        /// <value>The rotation.</value>
        public float RotationZ
        {
            get { return m_rotation.Z; }
            set
            {
                Rotation = new Vector3(m_rotation.X, m_rotation.Y, value);
            }
        }

        /// <summary>
        /// Gets or sets the uniform scale
        /// </summary>
        /// <value>The scale.</value>
        public float UniformScale
        {
            get { return m_scale.X; }
            set
            {
                Scale = new Vector3(value, value, value);
            }
        }

        /// <summary>
        /// Gets or sets the 2-component scale
        /// </summary>
        /// <value>The scale.</value>
        public Vector2 FlatScale
        {
            get { return new Vector2(m_scale.X, m_scale.Y); }
            set
            {
                Scale = new Vector3(value.X, value.Y, value.X);
            }
        }

        /// <summary>
        /// Non-uniform scale value
        /// </summary>
        /// <value>The scale vector.</value>
        public Vector3 Scale
        {
            get { return m_scale; }
            set
            {
                m_changed = true;
                m_scale = value;
            }
        }

        /// <summary>
        /// Gets or sets the parent transform.
        /// </summary>
        /// <value>The parent reference.</value>
        public Transform Parent
        {
            get { return m_parent; }
            set
            {
                System.Diagnostics.Debug.Assert(m_parent != this);

                m_parentVersion = 0;
                m_changed = true;
                m_parent = value;
            }
        }

        /// <summary>
        /// Result transformation 4x4 matrix
        /// </summary>
        /// <value>The matrix.</value>
        public Matrix Matrix
        {
            get
            {
                PrepareMatrix();

                return m_matrix;
            }
        }

        /// <summary>
        /// Gets the inverted transformation matrix.
        /// </summary>
        /// <value>The inverted transformation matrix.</value>
        public Matrix4x4 IMatrix
        {
            get
            {
                PrepareIMatrix();

                return m_imatrix;
            }
        }
     
        public Transform()
            : this(Vector3.Zero, Vector3.Zero, Vector3.One)
        {

        }

        /// <summary>
        /// Creates transform with Z-rotation
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="uniformScale"></param>
        public Transform(Vector2 position, float rotation, float uniformScale)
            : this(new Vector3(position.X, position.Y, 0), new Vector3(0, 0, rotation), new Vector3(uniformScale, uniformScale, uniformScale))
        {
        }

        /// <summary>
        /// Creates transform with 3-component rotation in radians
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="scale"></param>
        public Transform(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            m_changed = true;
            m_iMatrixChanged = true;
            m_position = position;
            m_rotation = rotation;
            m_scale = scale;
            m_version = 1;
            m_parentVersion = 0;
        }


        /// <summary>
        /// Unproject point
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public Vector2 GetScreenPoint(Vector2 source)
        {
            return Vector2.Transform(source, Matrix);
        }

        /// <summary>
        /// Unproject 3d point
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public Vector3 GetScreenPoint(Vector3 source)
        {
            return Vector3.Transform(source, Matrix);
        }

        /// <summary>
        /// Project point
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public Vector2 GetClientPoint(Vector2 source)
        {
            return Vector2.Transform(source, IMatrix);
        }

        /// <summary>
        /// Project point
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public Vector3 GetClientPoint(Vector3 source)
        {
            return Vector3.Transform(source, IMatrix);
        }

        /// <summary>
        /// This method bakes transform values to 4x4 matrix
        /// </summary>
        private void UpdateMatrix()
        {
            if (m_hasRotation)
                MathHelper.GetMatrix3d(m_position, m_rotation, m_scale, ref m_matrix);
            else // calculation is much simpler without rotation, it saves 3 pairs of sin+cos calculations
                MathHelper.GetMatrix3d(m_position, m_scale, ref m_matrix);

            if (m_parent != null) // if there is parent transform, baked value contains also parent transforms
            {
                m_parent.PrepareMatrix();

                // this one is the most expensive thing in whole engine
#if USE_NUMERICS
                m_matrix = Matrix.Multiply(m_matrix, m_parent.m_matrix);
#else
                if (m_parent.m_parent == null && !m_parent.m_hasRotation) // things are little bit easier if parent doesn't have a rotation matrix. We can save 20+ multiplications on this
                    MathHelper.TransformMatrix3d(m_parent.m_position, m_parent.m_scale, ref m_matrix);
                else
                    MathHelper.Mul(ref m_parent.m_matrix, ref m_matrix, ref m_matrix);
#endif
                m_parentVersion = m_parent.m_version;
            }

            m_iMatrixChanged = true;
            m_changed = false;
            m_version++;
        }

        internal void PrepareMatrix()
        {
            if (IsChanged)
                UpdateMatrix();
        }

        private void PrepareIMatrix()
        {
            PrepareMatrix();

            if (m_iMatrixChanged)
            {
                MathHelper.Invert(ref m_matrix, ref m_imatrix);

                m_iMatrixChanged = false;
            }
        }

        /// <summary>
        /// Compares coordinates of two vectors. Returns true if vectors are different
        /// </summary>
        /// <param name="one"></param>
        /// <param name="another"></param>
        /// <param name="diff"></param>
        /// <returns></returns>
        private static bool CompareVectors(Vector3 one, Vector3 another, float diff)
        {
            if (diff < another.Z - one.Z || one.Z - another.Z > diff)
                return true;
            if (diff < another.Y - one.Y || one.Y - another.Y > diff)
                return true;
            if (diff < another.X - one.X || one.X - another.X > diff)
                return true;
            return false;
        }
    }
}

