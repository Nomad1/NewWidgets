using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace NewWidgets.Utility
{
    /// <summary>
    /// This class helps to convert string values to corresponding objects
    /// The main difference from Convert.ChangeType that it supports Vector formats, color parsing, etc.
    /// </summary>
    public static class ConversionHelper
    {
        // theoretically we need custom enum for type number, to make sure that percent is parsed differently than float, uint separated from color, etc.

        private static readonly IDictionary<Type, Func<Type, string, object>> s_parsers = new Dictionary<Type, Func<Type, string, object>>()
        {
            { typeof(string), (type, str) => str },
            { typeof(uint), (type, str) => ColorParse(str) },
            { typeof(float), (type, str) => FloatParse(str) },
            { typeof(Margin), (type, str) => MarginParse(str) },
            { typeof(Vector2), (type, str) => Vector2Parse(str) },
            { typeof(Vector3), (type, str) => Vector3Parse(str) },
            { typeof(Vector4), (type, str) => Vector4Parse(str) },
        };

        private static readonly IDictionary<Type, Func<object, string>> s_formatters = new Dictionary<Type, Func<object, string>>()
        {
            { typeof(Margin), (value) => ToString((Margin)value) },
            { typeof(Vector2), (value) => ToString((Vector2)value) },
            { typeof(Vector3), (value) => ToString((Vector3)value) },
            { typeof(Vector4), (value) => ToString((Vector4)value) },
            { typeof(string), (value) => (string)value },
            { typeof(float), (value) => ToString((float)value) },
            { typeof(uint), (value) => string.Format(((uint)value >> 24) != 0 ? "#{0:x8}" : "#{0:x6}", value) }, // TODO: separate class for color
        };

        /// <summary>
        /// Registration of custom string-to-object parser
        /// </summary>
        /// <param name="type"></param>
        /// <param name="converter"></param>
        public static void RegisterParser(Type type, Func<Type, string, object> converter)
        {
            s_parsers[type] = converter;
        }

        /// <summary>
        /// Tries to parse string to specified type
        /// </summary>
        /// <param name="targetType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object ParseValue(Type targetType, string value)
        {
            Func<Type, string, object> converter;

            if (s_parsers.TryGetValue(targetType, out converter))
                return converter(targetType, value);
           
            if (targetType.IsEnum)
                return EnumParse(targetType, value);
            
            return Convert.ChangeType(value, targetType); // fallback, may crash
        }

        /// <summary>
        /// Tries to convert object to a string for CSS output
        /// </summary>
        /// <param name="sourceType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string FormatValue(Type sourceType, object value)
        {
            Func<object, string> formatter;

            if (s_formatters.TryGetValue(sourceType, out formatter))
                return formatter(value);

            return value.ToString().ToLower();
        }

        /// <summary>
        /// Converts float to text with dot as decimal separator
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToString(float value)
        {
            return value.ToString(CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        /// Converts Margin to set of floats separated by spaces
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToString(Margin value)
        {
            return string.Format("{0} {1} {2} {3}", ToString(value.Left), ToString(value.Top), ToString(value.Right), ToString(value.Bottom)); 
        }

        /// <summary>
        /// Converts Vector2 to set of floats separated by spaces
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToString(Vector2 value)
        {
            return string.Format("{0} {1}", ToString(value.X), ToString(value.Y));
        }

        /// <summary>
        /// Converts Vector3 to set of floats separated by spaces
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToString(Vector3 value)
        {
            return string.Format("{0} {1} {2}", ToString(value.X), ToString(value.Y), ToString(value.Z));
        }

        /// <summary>
        /// Converts Vector4 to set of floats separated by spaces
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToString(Vector4 value)
        {
            return string.Format("{0} {1} {2} {3}", ToString(value.X), ToString(value.Y), ToString(value.Z), ToString(value.W));
        }


        /// <summary>
        /// Culture invariant float parsing.
        /// Nomad: this was a minor bottleneck so I decided to sacrifice readability and optimize it. I take all the blame for unreadable code
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="value">Value.</param>
        public static float FloatParse(string stringValue)
        {
            char[] chars = stringValue.ToCharArray();

            int start = 0;
            int length = chars.Length;

            // TrimStart
            for (int i = start; i < length; i++)
            {
                if (chars[i] == ' ' || chars[i] == '\t' || chars[i] == '\n' || chars[i] == '\r')
                {
                    start++;
                    length--;
                }
                else
                    break;
            }

            // TrimEnd
            for (int i = length - 1; i >= start; i--)
            {
                if (chars[i] == ' ' || chars[i] == '\t' || chars[i] == '\n' || chars[i] == '\r')
                {
                    length--;
                }
                else
                    break;
            }

            if (length <= 0)
                throw new FormatException("Invalid float value");

            float multiplier = 1.0f;

            // EndsWith("%")
            if (chars[length - 1] == '%')
            {
                multiplier = 0.01f;
                length--;
            }

            // EndsWith("px") || EndsWith("pt")
            if (length >= 2 && chars[length - 2] == 'p' && (chars[length - 1] == 'x' || chars[length - 1] == 't')) // ends with px or pt
                length -= 2;

            if (length <= 0)
                throw new FormatException("Invalid float value");

            // Replace(",",".")
            for (int i = start; i < length; i++)
                if (chars[i] == ',')
                    chars[i] = '.';

            return multiplier * float.Parse(new string(chars, start, length), NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parse string value to color. Accepts 0x and # as a preffix for 24-bit hex colors
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="value">Value.</param>
        public static uint ColorParse(string value)
        {
            value = value.Trim();

            if (string.IsNullOrEmpty(value))
                throw new FormatException("Invalid Color value");

            if (value.Length >= 7 && value[0] == '#') // StartsWith("#")
                return uint.Parse(value.Substring(1), NumberStyles.HexNumber);
            if (value.Length >= 8 && value[0] == '0' && value[1] == 'x')  // StartsWith("0x")
                return uint.Parse(value.Substring(2), NumberStyles.HexNumber);

            return uint.Parse(value);
        }

        /// <summary>
        /// Parse a string to a 2-component vector
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="value">Value.</param>
        public static Vector2 Vector2Parse(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new FormatException("Invalid Vector2 value");

            string[] values = value.Split(new[] { ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (values.Length != 2)
                throw new ArgumentException("Invalid string value for Vector2 type!");

            float x = FloatParse(values[0]);
            float y = FloatParse(values[1]);
            return new Vector2(x, y);
        }

        /// <summary>
        /// Parse a string to a 3-component vector
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="value">Value.</param>
        public static Vector3 Vector3Parse(string value)
        {
            value = value.Trim();

            if (string.IsNullOrEmpty(value))
                throw new FormatException("Invalid Vector3 value");

            string[] values = value.Split(new[] { ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (values.Length != 3)
                throw new ArgumentException("Invalid string value for Vector3 type!");

            float x = FloatParse(values[0]);
            float y = FloatParse(values[1]);
            float z = FloatParse(values[2]);
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Parse a string to a 4-component vector
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="value">Value.</param>
        public static Vector4 Vector4Parse(string value)
        {
            value = value.Trim();

            if (string.IsNullOrEmpty(value))
                throw new FormatException("Invalid Vector4 value");

            string[] values = value.Split(new[] { ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (values.Length != 4)
                throw new ArgumentException("Invalid string value for Vector4 type!");

            float x = FloatParse(values[0]);
            float y = FloatParse(values[1]);
            float z = FloatParse(values[2]);
            float w = FloatParse(values[3]);
            return new Vector4(x, y, z, w);
        }


        /// <summary>
        /// Pastse a string to 4-component or 1-component margin
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="value">Value.</param>
        public static Margin MarginParse(string value)
        {
            string[] values = value.Split(new[] { ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (values.Length != 1 && values.Length != 4)
                throw new ArgumentException("Invalid string value for Margin type!");

            if (values.Length == 1)
            {
                float a = FloatParse(values[0]);
                return new Margin(a, a, a, a);
            }

            float l = FloatParse(values[0]);
            float t = FloatParse(values[1]);
            float r = FloatParse(values[2]);
            float b = FloatParse(values[3]);

            return new Margin(l, t, r, b);
        }

        /// <summary>
        /// Parse a string to specific enum tpye
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="value">Value.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static T EnumParse<T>(string value)
        {
            int result = 0; // TODO: GetUnderlyingType()?

            string[] strings = value.Split(new[] { '|', ',', '+' });

            foreach (string str in strings)
                result |= (int)Enum.Parse(typeof(T), str, true);

            return (T)Enum.ToObject(typeof(T), result);
        }

        /// <summary>
        /// Parse a string to specific enum type
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="value">Value.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static Enum EnumParse(Type enumType, string value)
        {
            int result = 0; // TODO: GetUnderlyingType()?

            string[] strings = value.Split(new[] { '|', ',', '+' });

            foreach (string str in strings)
                result |= (int)Enum.Parse(enumType, str, true);

            return (Enum)Enum.ToObject(enumType, result);
        }

    }
}
