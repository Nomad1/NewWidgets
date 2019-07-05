using System;
using System.Numerics;

namespace NewWidgets.Utility
{
    /// <summary>
    /// This class is only needed if we don't have some native Math functions 
    /// </summary>
    public static class MathHelper
    {
        public static readonly double Deg2Rad = Math.PI / 180.0;
        public static readonly double Rad2Deg = 180.0 / Math.PI;

        private static readonly Random s_random = new Random();

        public static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }

        public static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }

        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) == -1)
                return min;
            if (value.CompareTo(max) == 1)
                return max;

            return value;
        }

        // few matrix operations that could be different in different Matrix4x4 implementations

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
            // if Matrix is array-based we need to initialize it first, otherwise it will have default(Matrix4x4) value with null array
        }

        public static void Mul(Matrix4x4 m1, ref Matrix4x4 m2)
        {
            m2 = m2 * m1;
        }

        public static void Mul(Matrix4x4 m1, Matrix4x4 m2, ref Matrix4x4 nresult)
        {
            nresult = Matrix4x4.Multiply(m2, m1);
        }

        public static Vector3 Transform(Vector3 vector, ref Matrix4x4 matrix)
        {
            Vector3 result;
            result.X = matrix.M11 * vector.X + matrix.M21 * vector.Y + matrix.M31 * vector.Z + matrix.M41;
            result.Y = matrix.M12 * vector.X + matrix.M22 * vector.Y + matrix.M32 * vector.Z + matrix.M42;
            result.Z = matrix.M13 * vector.X + matrix.M23 * vector.Y + matrix.M33 * vector.Z + matrix.M43;

            return result;
        }

        public static void Invert(Matrix4x4 m1, ref Matrix4x4 m2)
        {
            Matrix4x4.Invert(m1, out m2);
        }

        public static float LinearInterpolation(float x, float from, float to)
        {
            x = Clamp(x, 0.0f, 1.0f);
            return from + (to - from) * x;
        }

        public static Vector2 LinearInterpolation(float x, Vector2 from, Vector2 to)
        {
            x = Clamp(x, 0.0f, 1.0f);
            return from + (to - from) * x;
        }

        public static Vector3 LinearInterpolation(float x, Vector3 from, Vector3 to)
        {
            x = Clamp(x, 0.0f, 1.0f);
            return from + (to - from) * x;
        }

        public static int LinearInterpolationInt(float x, int from, int to)
        {
            return Clamp((int)(from + (to - from) * x + 0.5f), Math.Min(from, to), Math.Max(from, to));
        }

        public static int GetRandomInt(int from, int to)
        {
            return s_random.Next(to - from) + from;
        }
    }
}