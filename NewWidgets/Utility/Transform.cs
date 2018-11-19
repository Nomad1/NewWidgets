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
        internal Matrix4x4 m_localMatrix;
        internal Matrix4x4 m_imatrix;

		private Vector3 m_rotation;
        private Vector3 m_position;
        private Vector3 m_scale;
        
		private bool m_changed;
        private bool m_iMatrixChanged;
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
        /// Non-uniform scale value
        /// </summary>
        /// <value>The scale vector.</value>
        public Vector3 Scale
        {
            get { return m_scale; }
            set
            {
                if (!m_changed && (m_scale - value).LengthSquared() >= ScaleEpsilon)
                    m_changed = true;

                m_scale = value;
            }
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
                if (!m_changed && (value - m_position).LengthSquared() > PositionEpsilon)
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
				Vector3 newVector = new Vector3(value.X, value.Y, 0);

				if (!m_changed && (newVector - m_position).LengthSquared() > PositionEpsilon)
                    m_changed = true;

				m_position = newVector;
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
				if (!m_changed && (value - m_rotation).LengthSquared() > AngleEpsilon)
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
                if (!m_changed && (Math.Abs(m_rotation.Z - value) >= AngleEpsilon))
                    m_changed = true;

                m_rotation.Z = value;
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
				if (!m_changed && (Math.Abs(m_scale.X - value) >= ScaleEpsilon))
                    m_changed = true;

				m_scale = new Vector3(value, value, value);
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
				Vector3 newScale = new Vector3(value.X, value.Y, value.X);

				if (!m_changed && (newScale - m_scale).LengthSquared() >= ScaleEpsilon)
                    m_changed = true;

				m_scale = newScale;
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
                if (IsChanged)
                {
                    UpdateMatrix();
                }
                
                return m_parent == null ? m_localMatrix : m_matrix;
            }
        }

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
                Vector3 position = m_parent == null ? m_localMatrix.Translation : m_matrix.Translation;
                
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
            Vector3 result;

            if (m_parent == null)
                result = MathHelper.Transform(new Vector3(source, 0), ref m_localMatrix);
            else
                result = MathHelper.Transform(new Vector3(source, 0), ref m_matrix);
            return new Vector2(result.X, result.Y);
        }

        /// <summary>
        /// Project point
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public Vector2 GetClientPoint(Vector2 source)
        {
            PrepareIMatrix();
            Vector3 result = MathHelper.Transform(new Vector3(source, 0), ref m_imatrix);
            return new Vector2(result.X, result.Y);
        }

        /// <summary>
        /// This method bakes transform values to 4x4 matrix
        /// </summary>
        private void UpdateMatrix()
        {
            MathHelper.Init(ref m_matrix);
            MathHelper.Init(ref m_localMatrix);

            if (m_changed)
                MathHelper.GetMatrix3d(m_position, m_rotation, m_scale, ref m_localMatrix);

            if (m_parent != null) // if there is parent transform, baked value contains also parent transforms
                MathHelper.Mul(m_parent.Matrix, m_localMatrix, ref m_matrix); // this one is most expensive thing in whole engine

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
                MathHelper.Invert(Matrix, ref m_imatrix);
                m_iMatrixChanged = false;
            }
        }
    }
}

