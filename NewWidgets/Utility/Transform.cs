using System;
using System.Numerics;

#if RUNMOBILE
using RunMobile.Utility;
#endif

namespace NewWidgets.Utility
{
    /// <summary>
    /// Helper class that is capable of baking 2d transform into 4x4 matrix with support for inheritance
    /// </summary>
    public class Transform
    {
        [Flags]
        private enum TransformType
        {
            None = 0,
            Translation = 0x01,
            Rotation = 0x02,
            Scale = 0x04,
            Parent = 0x08,
        }


#if STEP_UPDATES
        private static readonly float PositionEpsilon = 0.001f;
        private static readonly float AngleEpsilon = 0.001f;
        private static readonly float ScaleEpsilon = 0.001f;
#else
        private static readonly float PositionEpsilon = float.Epsilon; // Position vector is three-component, so that should be eps^3
        private static readonly float AngleEpsilon = float.Epsilon;
        private static readonly float ScaleEpsilon = float.Epsilon; // Scale vector is three-component, so that should be eps^3
#endif

        internal Matrix4x4 m_matrix;
        internal Matrix4x4 m_imatrix;

        private Vector3 m_rotation;
        private Vector3 m_position;
        private Vector3 m_scale;

        private bool m_changed;
        private bool m_iMatrixChanged;
        private TransformType m_transformType;

        private int m_version;
        private int m_parentVersion;

        private Transform m_parent;

        internal bool IsChanged
        {
            get
            {
                return m_changed || (m_parent != null && (m_parent.IsChanged || m_parent.m_version != m_parentVersion));
            }
        }

        public int Version
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
                if (!m_changed && m_position.BoxDistance(value) >= PositionEpsilon)
                    m_changed = true;

                m_position = value;

                if (m_position.BoxDistance(Vector3.Zero) >= PositionEpsilon)
                    m_transformType |= TransformType.Translation;
                else
                    m_transformType &= ~TransformType.Translation;
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
                if (!m_changed && m_rotation.BoxDistance(value) >= AngleEpsilon)
                    m_changed = true;

                m_rotation = value;

                if (m_rotation.BoxDistance(Vector3.Zero) >= AngleEpsilon)
                    m_transformType |= TransformType.Rotation;
                else
                    m_transformType &= ~TransformType.Rotation;
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
                if (!m_changed && m_scale.BoxDistance(value) >= ScaleEpsilon)
                    m_changed = true;

                m_scale = value;
                if (m_scale.BoxDistance(Vector3.One) >= ScaleEpsilon)
                    m_transformType |= TransformType.Scale;
                else
                    m_transformType &= ~TransformType.Scale;
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

                if (!m_changed && m_parent != value)
                    m_changed = true;

                m_parent = value;

                if (m_parent != null)
                    m_transformType |= TransformType.Parent;
                else
                    m_transformType &= ~TransformType.Parent;
            }
        }

        /// <summary>
        /// Result transformation 4x4 matrix
        /// </summary>
        /// <value>The matrix.</value>
        public Matrix4x4 Matrix
        {
            get
            {
                PrepareMatrix();

                return m_matrix;
            }
        }

        /// <summary>
        /// Gets the local matrix.
        /// </summary>
        /// <value>The local matrix.</value>
        //public Matrix4x4 LocalMatrix
        //{
        //    get
        //    {
        //        if ((m_transformType & TransformType.Rotation) == 0)
        //        {
        //            //if ((m_transformType & TransformType.Scale) == 0) // it's faster to assign than to check
        //            {
        //                m_localMatrix.M11 = m_scale.X;
        //                m_localMatrix.M22 = m_scale.Y;
        //                m_localMatrix.M33 = m_scale.Z;
        //            }

        //            m_localMatrix.Translation = m_position;

        //        }
        //        else
        //            PrepareMatrix();

        //        return m_localMatrix;
        //    }
        //}

        /// <summary>
        /// Gets the inverted atrix.
        /// </summary>
        /// <value>The inverted atrix.</value>
        public Matrix4x4 IMatrix
        {
            get
            {
                if (m_iMatrixChanged || IsChanged)
                {
                    MathHelper.Invert(Matrix, ref m_imatrix);
                    m_iMatrixChanged = false;
                }

                return m_imatrix;
            }
        }

        // 2d actual values

        public Vector2 ActualPosition
        {
            get
            {
                // Alternative is GetScreenPoint(new Vector3(0,0,0));
                PrepareMatrix();
                Vector3 position = m_parent == null ? m_position : m_matrix.Translation;

                return new Vector2(position.X, position.Y);  // or new Vector3(transform[12], transform[13], transform[14]); 
            }
        }

        public Vector2 ActualScale
        {
            get
            {
                return (m_parent == null ? Vector2.One : m_parent.ActualScale) * new Vector2(m_scale.X, m_scale.Y);
            }
        }

        public float ActualRotation
        {
            get
            {
                return (m_parent == null ? 0 : m_parent.ActualRotation) + m_rotation.Z;
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
            if (position.BoxDistance(Vector3.Zero) > PositionEpsilon)
                m_transformType |= TransformType.Translation;

            if (rotation.BoxDistance(Vector3.Zero) > AngleEpsilon)
                m_transformType |= TransformType.Rotation;

            if (scale.BoxDistance(Vector3.One) > ScaleEpsilon)
                m_transformType |= TransformType.Scale;

            m_changed = true;
            m_iMatrixChanged = true;
            m_position = position;
            m_rotation = rotation;
            m_scale = scale;
            m_version = 1;
        }


        /// <summary>
        /// Unproject point
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public Vector2 GetScreenPoint(Vector2 source)
        {
            PrepareMatrix();
            return MathHelper.Transform(new Vector3(source, 0), ref m_matrix).XY();
        }

        /// <summary>
        /// Unproject 3d point
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public Vector3 GetScreenPoint3(Vector3 source)
        {
            PrepareMatrix();
            return MathHelper.Transform(source, ref m_matrix);
        }

        /// <summary>
        /// Project point
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public Vector2 GetClientPoint(Vector2 source)
        {
            PrepareIMatrix();
            return MathHelper.Transform(new Vector3(source, 0), ref m_imatrix).XY();
        }

        /// <summary>
        /// Project point
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public Vector3 GetClientPoint3(Vector3 source)
        {
            PrepareIMatrix();
            return MathHelper.Transform(source, ref m_imatrix);
        }

        /// <summary>
        /// This method bakes transform values to 4x4 matrix
        /// </summary>
        private void UpdateMatrix()
        {
            if ((m_transformType & TransformType.Rotation) == 0)
                MathHelper.GetMatrix3d(m_position, m_scale, ref m_matrix);
            else
                MathHelper.GetMatrix3d(m_position, m_rotation, m_scale, ref m_matrix);

            if (m_parent != null) // if there is parent transform, baked value contains also parent transforms
            {
                // this one is the most expensive thing in whole engine
#if USE_NUMERICS
                m_matrix = Matrix4x4.Multiply(m_localMatrix, m_parent.Matrix);
#else
                m_parent.PrepareMatrix();

                m_matrix.Mul(ref m_parent.m_matrix);
#endif
            }

            m_iMatrixChanged = true;

            m_changed = false;
            m_version++;
            m_parentVersion = m_parent != null ? m_parent.m_version : 0;
        }

        internal void PrepareMatrix()
        {
            if (IsChanged)
                UpdateMatrix();
        }

        internal void PrepareIMatrix()
        {
            if (m_iMatrixChanged || IsChanged)
            {
                PrepareMatrix();

#if USE_NUMERICS
                Matrix4x4.Invert(m_matrix, out m_imatrix);
#else
                m_imatrix.AssignInverted(ref m_matrix);
#endif
                m_iMatrixChanged = false;
            }
        }
    }
}

