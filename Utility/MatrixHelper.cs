using System;
using System.Numerics;

namespace NewWidgets.Utility
{
    /// <summary>
    /// This class performs few matrix operations that could be different in different Matrix4x4 implementations
    /// </summary>
    public class MatrixHelper
    {
        public static void GetMatrix3d(Vector3 position, Vector3 rotation, Vector3 scale, ref Matrix4x4 resultMatrix)
        {
            float siny = (float)Math.Sin(rotation.Z);
            float cosy = (float)Math.Cos(rotation.Z);
            float sina = (float)Math.Sin(rotation.X);
            float cosa = (float)Math.Cos(rotation.X);
            float sinb = (float)Math.Sin(rotation.Y);
            float cosb = (float)Math.Cos(rotation.Y);

            resultMatrix.M11 = (cosb * cosy - sina * sinb * siny) * scale.X;
            resultMatrix.M12 = (cosy * sina * sinb + cosb * siny) * scale.X;
            resultMatrix.M13 = (-cosa * sinb) * scale.X;
            resultMatrix.M14 = 0;
            resultMatrix.M21 = (-cosa * siny) * scale.Y;
            resultMatrix.M22 = (cosa * cosy) * scale.Y;
            resultMatrix.M23 = (sina) * scale.Y;
            resultMatrix.M24 = 0;
            resultMatrix.M31 = (cosy * sinb + cosb * sina * siny) * scale.Z;
            resultMatrix.M32 = (sinb * siny - cosb * cosy * sina) * scale.Z;
            resultMatrix.M33 = (cosa * cosb) * scale.Z;
            resultMatrix.M34 = 0;
            resultMatrix.M41 = position.X;
            resultMatrix.M42 = position.Y;
            resultMatrix.M43 = position.Z;
            resultMatrix.M44 = 1.0f;
        }

        public static void Init(ref Matrix4x4 matrix)
        {
#if RUNMOBILE
            // if Matrix is array-based we need to initialize it first, otherwise it will have default(Matrix4x4) value with null array
            if (matrix.IsEmpty)
                matrix = new Matrix4x4(new float[16]);
#endif
        }

        public static void Mul(Matrix4x4 m1, ref Matrix4x4 m2)
        {
#if RUNMOBILE
            Matrix4x4.Mul(ref m2, m1);
#else
            m2 = m2 * m1;
#endif
        }

        public static void Invert(Matrix4x4 m1, ref Matrix4x4 m2)
        {
#if RUNMOBILE
            Matrix4x4.Invert(m1, ref m2);
#else
            Matrix4x4.Invert(m1, out m2);
#endif
        }

    }
}
