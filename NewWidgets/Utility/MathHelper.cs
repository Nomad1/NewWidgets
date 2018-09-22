using System;

namespace NewWidgets.Utility
{
    /// <summary>
    /// This class is only needed if we don't have some native Math functions 
    /// </summary>
    public static class MathHelper
    {
        public static readonly double Deg2Rad = Math.PI / 180.0;
        public static readonly double Rad2Deg = 180.0 / Math.PI;

        public static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }

        public static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }

        public static T Clamp<T>(T value, T min, T max) where T:IComparable<T>
        {
            if (value.CompareTo(min) == -1)
                return min;
            if (value.CompareTo(max) == 1)
                return max;

            return value;
        }
    }
}