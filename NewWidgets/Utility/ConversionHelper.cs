using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace NewWidgets.Utility
{
    /// <summary>
    /// Special enumeration for unit types
    /// </summary>
    public enum UnitType
    {
        None = 0, // default
        Length = 1, // px, pt, etc.
        Percent = 2, // %
        FontUnits = 3, // em = percent / 100
        Url = 4, // url("")
        Color = 5, // #aarrggbb
    }


    /// <summary>
    /// This class helps to convert string values to corresponding objects
    /// The main difference from Convert.ChangeType that it supports Vector formats, color parsing, etc.
    /// </summary>
    public static class ConversionHelper
    {
        // theoretically we need custom enum for type number, to make sure that percent is parsed differently than float, uint separated from color, etc.

        public delegate object DataParserDelegate(string str, UnitType unitType);
        public delegate string DataFormatterDelegate(object value, UnitType unitType);

        private static readonly IDictionary<Type, DataParserDelegate> s_parsers = new Dictionary<Type, DataParserDelegate>()
        {
            { typeof(string), (str, unitType) => StringParse(str, unitType) },
            { typeof(uint), (str, unitType) => UintParse(str, unitType) },
            { typeof(float), (str, unitType) => FloatParse(str, unitType) },
            { typeof(Margin), (str, unitType) => MarginParse(str, unitType) },
            { typeof(Vector2), (str, unitType) => Vector2Parse(str, unitType) },
            { typeof(Vector3), (str, unitType) => Vector3Parse(str, unitType) },
            { typeof(Vector4), (str, unitType) => Vector4Parse(str, unitType) },
        };

        private static readonly IDictionary<Type, DataFormatterDelegate> s_formatters = new Dictionary<Type, DataFormatterDelegate>()
        {
            { typeof(Margin), (value, unitType) => ToString((Margin)value, unitType) },
            { typeof(Vector2), (value, unitType) => ToString((Vector2)value, unitType) },
            { typeof(Vector3), (value, unitType) => ToString((Vector3)value, unitType) },
            { typeof(Vector4), (value, unitType) => ToString((Vector4)value, unitType) },
            { typeof(string), (value, unitType) => ToString((string)value, unitType) },
            { typeof(float), (value, unitType) => ToString((float)value, unitType) },
            { typeof(uint), (value, unitType) => ToString((uint)value, unitType) }
        };

        /// <summary>
        /// Registration of custom string-to-object parser
        /// </summary>
        /// <param name="type"></param>
        /// <param name="parser"></param>
        public static void RegisterParser(Type type, DataParserDelegate parser)
        {
            s_parsers[type] = parser;
        }

        /// <summary>
        /// Tries to parse string to specified type
        /// </summary>
        /// <param name="targetType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object ParseValue(Type targetType, UnitType unitType, string value)
        {
            DataParserDelegate parser;

            if (s_parsers.TryGetValue(targetType, out parser))
                return parser(value, unitType);
           
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
        public static string FormatValue(Type sourceType, UnitType unitType, object value)
        {
            DataFormatterDelegate formatter;

            if (s_formatters.TryGetValue(sourceType, out formatter))
                return formatter(value, unitType);

            return value.ToString().ToLower();
        }

        /// <summary>
        /// Converts float to text with dot as decimal separator
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToString(float value, UnitType unitType = UnitType.None)
        {
            switch (unitType)
            {
                case UnitType.Length:
                    return string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0}px", value);
                case UnitType.FontUnits:
                    return string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0}em", value);
                case UnitType.Percent:
                    return value.ToString("0%", CultureInfo.InvariantCulture.NumberFormat);
                case UnitType.None:
                    return value.ToString(CultureInfo.InvariantCulture.NumberFormat);
                case UnitType.Color:
                case UnitType.Url:
                    throw new FormatException("Invalid unit type " + unitType + " for float");
            }

            return value.ToString(CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        /// Converts Margin to set of floats separated by spaces
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToString(Margin value, UnitType unitType = UnitType.None)
        {
            return string.Format("{0} {1} {2} {3}", ToString(value.Left, unitType), ToString(value.Top, unitType), ToString(value.Right, unitType), ToString(value.Bottom, unitType)); 
        }

        /// <summary>
        /// Converts Vector2 to set of floats separated by spaces
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToString(Vector2 value, UnitType unitType = UnitType.None)
        {
            return string.Format("{0} {1}", ToString(value.X, unitType), ToString(value.Y, unitType));
        }

        /// <summary>
        /// Converts Vector3 to set of floats separated by spaces
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToString(Vector3 value, UnitType unitType = UnitType.None)
        {
            return string.Format("{0} {1} {2}", ToString(value.X, unitType), ToString(value.Y, unitType), ToString(value.Z, unitType));
        }

        /// <summary>
        /// Converts Vector4 to set of floats separated by spaces
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToString(Vector4 value, UnitType unitType = UnitType.None)
        {
            return string.Format("{0} {1} {2} {3}", ToString(value.X, unitType), ToString(value.Y, unitType), ToString(value.Z, unitType), ToString(value.W, unitType));
        }


        /// <summary>
        /// Converts uint value to color or regular numeric format
        /// </summary>
        /// <param name="value"></param>
        /// <param name="unitType"></param>
        /// <returns></returns>
        public static string ToString(uint value, UnitType unitType = UnitType.None)
        {
            if (unitType == UnitType.Color)
            {
                // CSS writes alpha last, this engine stores it first, so the eight digit form is emitted as #rrggbbaa
                if ((value >> 24) != 0)
                    return string.Format("#{0:x8}", (value << 8) | (value >> 24));

                return string.Format("#{0:x6}", value);
            }

            return Convert.ToString(value);
        }

        public static string ToString(string value, UnitType unitType = UnitType.None)
        {
            switch (unitType)
            {
                case UnitType.Url:
                    return string.Format("url(\"{0}\")", value);
                case UnitType.None:
                    return Convert.ToString(value);
            }
            throw new FormatException("Invalid unit type " + unitType + " for string data");
        }

        /// <summary>
        /// Culture invariant float parsing.
        /// Nomad: this was a minor bottleneck so I decided to sacrifice readability and optimize it. I take all the blame for unreadable code
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="value">Value.</param>
        public static float FloatParse(string stringValue, UnitType unitType = UnitType.None)
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
            if (length >= 2)
            {
                if ((chars[length - 2] == 'p' && chars[length - 1] == 'x') || (chars[length - 2] == 'e' && chars[length - 1] == 'm')) // em is relative to font scale
                {
                    multiplier = 1.0f;
                    length -= 2;
                }
                else
                {
                    if ((chars[length - 2] == 'p' && chars[length - 1] == 't'))
                    {
                        multiplier = 96.0f / 72.0f; // px = 1/96 inches and pt = 1/72 inches. px/pt = (1/96) / (1/72)
                        length -= 2;
                    }
                }
            }

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
        /// <summary>
        /// The sixteen basic HTML colour keywords, plus transparent.
        /// ponytail: CSS defines 148 named colours. These sixteen cover hand-written UI
        /// stylesheets; add the rest from the CSS Color specification if a stylesheet needs them.
        /// </summary>
        private static readonly IDictionary<string, uint> s_namedColors = new Dictionary<string, uint>()
        {
            { "transparent", 0x00000000 },
            { "black", 0x000000 },
            { "silver", 0xc0c0c0 },
            { "gray", 0x808080 },
            { "grey", 0x808080 },
            { "white", 0xffffff },
            { "maroon", 0x800000 },
            { "red", 0xff0000 },
            { "purple", 0x800080 },
            { "fuchsia", 0xff00ff },
            { "green", 0x008000 },
            { "lime", 0x00ff00 },
            { "olive", 0x808000 },
            { "yellow", 0xffff00 },
            { "navy", 0x000080 },
            { "blue", 0x0000ff },
            { "teal", 0x008080 },
            { "aqua", 0x00ffff },
        };

        /// <summary>
        /// Parse string value to color. Accepts the CSS forms #rgb, #rgba, #rrggbb, #rrggbbaa,
        /// rgb(), rgba() and the basic colour keywords, plus this engine's own 0xAARRGGBB form.
        /// Colours are stored as 0xAARRGGBB, so a CSS alpha, which is written last, moves to the
        /// high byte.
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="value">Value.</param>
        public static uint UintParse(string value, UnitType unitType = UnitType.None)
        {
            value = value.Trim();

            if (string.IsNullOrEmpty(value))
                throw new FormatException("Invalid Color value");

            if (unitType == UnitType.Color)
            {
                uint named;
                if (s_namedColors.TryGetValue(value.ToLowerInvariant(), out named))
                    return named;

                if (value[0] == '#')
                    return HexColorParse(value.Substring(1));

                if (value.Length >= 8 && value[0] == '0' && value[1] == 'x')  // StartsWith("0x")
                    return uint.Parse(value.Substring(2), NumberStyles.HexNumber);

                if (value.StartsWith("rgb(") || value.StartsWith("rgba("))
                    return RgbFunctionParse(value);
            }

            return uint.Parse(value);
        }

        /// <summary>
        /// Parses the digits of a # colour, without the leading #.
        /// Three and four digit forms repeat each digit, so #f00 is #ff0000.
        /// The four and eight digit forms carry a CSS alpha, written last, which moves to the high byte.
        /// </summary>
        private static uint HexColorParse(string digits)
        {
            if (digits.Length == 3 || digits.Length == 4)
            {
                StringBuilder expanded = new StringBuilder(8);

                // CSS writes alpha last, this engine stores it first
                if (digits.Length == 4)
                    expanded.Append(digits[3]).Append(digits[3]);

                for (int i = 0; i < 3; i++)
                    expanded.Append(digits[i]).Append(digits[i]);

                return uint.Parse(expanded.ToString(), NumberStyles.HexNumber);
            }

            if (digits.Length == 6)
                return uint.Parse(digits, NumberStyles.HexNumber);

            if (digits.Length == 8)
            {
                // #rrggbbaa: CSS writes alpha last, this engine stores it first
                uint rgba = uint.Parse(digits, NumberStyles.HexNumber);
                return (rgba >> 8) | (rgba << 24);
            }

            throw new FormatException("Invalid Color value #" + digits);
        }

        /// <summary>
        /// Parses rgb(r, g, b) and rgba(r, g, b, a). Components are 0 to 255, alpha is 0 to 1.
        /// </summary>
        private static uint RgbFunctionParse(string value)
        {
            int open = value.IndexOf('(');
            int close = value.IndexOf(')');

            if (open < 0 || close < open)
                throw new FormatException("Invalid Color value " + value);

            string[] parts = value.Substring(open + 1, close - open - 1).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 3 && parts.Length != 4)
                throw new FormatException("Invalid Color value " + value);

            uint red = (uint)Math.Round(FloatParse(parts[0]));
            uint green = (uint)Math.Round(FloatParse(parts[1]));
            uint blue = (uint)Math.Round(FloatParse(parts[2]));
            uint alpha = 0;

            if (parts.Length == 4)
                alpha = (uint)Math.Round(FloatParse(parts[3]) * 255.0f);

            return (alpha << 24) | (red << 16) | (green << 8) | blue;
        }

        public static string StringParse(string value, UnitType unitType = UnitType.None)
        {
            value = value.Trim();

            switch (unitType)
            {
                case UnitType.Url:
                    if (value.StartsWith("url(") && value.EndsWith(")"))
                        return value.Substring(4, value.Length - 5).Trim(' ', '\"', '\t');
                    else
                        return value;
                        //throw new FormatException("Invalid string " + value + " for Url unit type");
                case UnitType.None:
                    return value;
            }

            throw new FormatException("Invalid unit type " + unitType + " for string type");
        }


        /// <summary>
        /// Parse a string to a 2-component vector
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="value">Value.</param>
        public static Vector2 Vector2Parse(string value, UnitType unitType = UnitType.None)
        {
            if (string.IsNullOrEmpty(value))
                throw new FormatException("Invalid Vector2 value");

            string[] values = value.Split(new[] { ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (values.Length != 2)
                throw new ArgumentException("Invalid string value for Vector2 type!");

            float x = FloatParse(values[0], unitType);
            float y = FloatParse(values[1], unitType);
            return new Vector2(x, y);
        }

        /// <summary>
        /// Parse a string to a 3-component vector
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="value">Value.</param>
        public static Vector3 Vector3Parse(string value, UnitType unitType = UnitType.None)
        {
            value = value.Trim();

            if (string.IsNullOrEmpty(value))
                throw new FormatException("Invalid Vector3 value");

            string[] values = value.Split(new[] { ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (values.Length != 3)
                throw new ArgumentException("Invalid string value for Vector3 type!");

            float x = FloatParse(values[0], unitType);
            float y = FloatParse(values[1], unitType);
            float z = FloatParse(values[2], unitType);
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Parse a string to a 4-component vector
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="value">Value.</param>
        public static Vector4 Vector4Parse(string value, UnitType unitType = UnitType.None)
        {
            value = value.Trim();

            if (string.IsNullOrEmpty(value))
                throw new FormatException("Invalid Vector4 value");

            string[] values = value.Split(new[] { ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (values.Length != 4)
                throw new ArgumentException("Invalid string value for Vector4 type!");

            float x = FloatParse(values[0], unitType);
            float y = FloatParse(values[1], unitType);
            float z = FloatParse(values[2], unitType);
            float w = FloatParse(values[3], unitType);
            return new Vector4(x, y, z, w);
        }


        /// <summary>
        /// Pastse a string to 4-component or 1-component margin
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="value">Value.</param>
        public static Margin MarginParse(string value, UnitType unitType = UnitType.None)
        {
            string[] values = value.Split(new[] { ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (values.Length < 1 || values.Length > 4)
                throw new ArgumentException("Invalid string value for Margin type!");

            // CSS orders a box shorthand clockwise from the top: top, right, bottom, left.
            // One value sets every side. Two are vertical then horizontal. Three are top,
            // horizontal, bottom. Four are the sides in order.

            float top = FloatParse(values[0], unitType);
            float right = top;
            float bottom = top;
            float left = top;

            if (values.Length >= 2)
            {
                right = FloatParse(values[1], unitType);
                left = right;
            }

            if (values.Length >= 3)
                bottom = FloatParse(values[2], unitType);

            if (values.Length == 4)
                left = FloatParse(values[3], unitType);

            return new Margin(left, top, right, bottom);
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
            {
                result |= (int)Enum.Parse(enumType, str.Replace('-', '_'), true);
            }

            return (Enum)Enum.ToObject(enumType, result);
        }

    }
}
