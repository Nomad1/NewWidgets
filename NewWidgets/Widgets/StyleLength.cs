using System;
using System.Globalization;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    /// <summary>
    /// Unit a <see cref="StyleLength"/> was written in.
    /// </summary>
    public enum StyleUnit
    {
        /// <summary>
        /// The property was never declared. Distinct from Auto: the CSS initial value of
        /// width is auto, but this engine keeps whatever size the widget already has
        /// instead, so "never declared" and "declared auto" cannot be folded together
        /// </summary>
        Unset = 0,
        /// <summary>
        /// px, pt and bare numbers, all stored in pixels
        /// </summary>
        Pixels = 1,
        /// <summary>
        /// % of the containing block, stored as a 0..1 fraction
        /// </summary>
        Percent = 2,
        /// <summary>
        /// em, font units
        /// </summary>
        Em = 3,
        /// <summary>
        /// The CSS auto keyword
        /// </summary>
        Auto = 4,
    }

    /// <summary>
    /// A CSS length that remembers the unit it was written in.
    ///
    /// <see cref="ConversionHelper.FloatParse"/> destroys the unit at parse time: it sniffs the
    /// suffix, multiplies a percentage by 0.01 and hands back a bare float, so by the time the
    /// layout engine sees the number it can no longer tell 50% from 0.5px. Every property that
    /// takes part in box layout stores this struct instead, so the containing block can be
    /// applied when the box is resolved rather than when the stylesheet is read.
    ///
    /// This is deliberately per-property. Every other percentage in the library --
    /// background-size, background-position, --background-opacity, opacity -- keeps the
    /// old bare-fraction behaviour, which is what the shipped stylesheets mean by it.
    /// </summary>
    public struct StyleLength
    {
        /// <summary>
        /// The property was never declared
        /// </summary>
        public static readonly StyleLength Unset = new StyleLength(StyleUnit.Unset, 0.0f);

        /// <summary>
        /// The property was declared as the auto keyword
        /// </summary>
        public static readonly StyleLength Auto = new StyleLength(StyleUnit.Auto, 0.0f);

        private readonly StyleUnit m_unit;
        private readonly float m_value;

        public StyleUnit Unit
        {
            get { return m_unit; }
        }

        /// <summary>
        /// The bare number, in pixels for a length and as a 0..1 fraction for a percentage.
        /// This is exactly what FloatParse used to return, and is what the legacy
        /// GetProperty("width", 0f) reads hand back
        /// </summary>
        public float Value
        {
            get { return m_value; }
        }

        public bool IsUnset
        {
            get { return m_unit == StyleUnit.Unset; }
        }

        public bool IsAuto
        {
            get { return m_unit == StyleUnit.Auto; }
        }

        /// <summary>
        /// True when the value turns into a number once the containing block is known
        /// </summary>
        public bool IsDefinite
        {
            get { return m_unit == StyleUnit.Pixels || m_unit == StyleUnit.Percent || m_unit == StyleUnit.Em; }
        }

        public StyleLength(StyleUnit unit, float value)
        {
            m_unit = unit;
            m_value = value;
        }

        /// <summary>
        /// Wraps an already resolved pixel value
        /// </summary>
        public static StyleLength Pixels(float value)
        {
            return new StyleLength(StyleUnit.Pixels, value);
        }

        /// <summary>
        /// Turns the value into pixels against the containing block size on this axis.
        /// Auto and Unset have no number of their own and resolve to zero; the caller is
        /// expected to test <see cref="IsDefinite"/> before asking
        /// </summary>
        public float Resolve(float containingBlock)
        {
            if (m_unit == StyleUnit.Percent)
                return m_value * containingBlock;

            // ponytail: em on a box property is treated as pixels, which is what FloatParse
            // did before this struct existed. font-size in this engine is a scale factor and
            // not a length, so there is no pixel font size to multiply by; resolve against one
            // once font-size carries real pixels.
            return m_value;
        }

        /// <summary>
        /// Parses one CSS length value: auto, a percentage, or a length in px, pt or em.
        /// Anything else throws, exactly as the bare float parse used to
        /// </summary>
        public static StyleLength Parse(string value)
        {
            string trimmed = value.Trim();

            if (string.Equals(trimmed, "auto", StringComparison.OrdinalIgnoreCase))
                return Auto;

            // FloatParse already handles px, pt, em, % and the decimal separator, and it is the
            // documented hot path for this; it just cannot say which suffix it saw, so the
            // suffix is sniffed a second time here
            float number = ConversionHelper.FloatParse(trimmed);

            if (trimmed.EndsWith("%", StringComparison.Ordinal))
                return new StyleLength(StyleUnit.Percent, number);

            if (trimmed.EndsWith("em", StringComparison.OrdinalIgnoreCase))
                return new StyleLength(StyleUnit.Em, number);

            return new StyleLength(StyleUnit.Pixels, number);
        }

        /// <summary>
        /// Adapter matching <see cref="ConversionHelper.DataParserDelegate"/> so this type can be
        /// registered with <see cref="ConversionHelper.RegisterParser"/>
        /// </summary>
        internal static object ParseData(string value, UnitType unitType)
        {
            return Parse(value);
        }

        /// <summary>
        /// Writes the value back in the unit it was authored in, so a stylesheet round-trips.
        /// ConversionHelper has no formatter for this type and falls back to ToString().ToLower()
        /// </summary>
        public override string ToString()
        {
            switch (m_unit)
            {
                case StyleUnit.Auto:
                    return "auto";
                case StyleUnit.Unset:
                    return "unset";
                case StyleUnit.Percent:
                    return m_value.ToString("0%", CultureInfo.InvariantCulture.NumberFormat);
                case StyleUnit.Em:
                    return string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0}em", m_value);
                default:
                    return string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0}px", m_value);
            }
        }
    }

    /// <summary>
    /// One axis -- horizontal or vertical -- of an absolutely positioned CSS box, and the rules
    /// that turn its seven declared lengths into a position and a size. Every NewWidgets widget
    /// is absolutely positioned, so this is CSS 2.1 10.3.7 for the horizontal axis and the
    /// identical 10.6.4 for the vertical one, with 10.4's min/max clamp on top.
    ///
    /// Tightly coupled to <see cref="StyleLength"/>: it is seven of them plus the rule that
    /// resolves them, and it is of no use to anything that does not hold them.
    /// </summary>
    internal struct StyleAxis
    {
        private readonly StyleLength m_start;       // left or top
        private readonly StyleLength m_end;         // right or bottom
        private readonly StyleLength m_size;        // width or height
        private readonly StyleLength m_marginStart; // margin-left or margin-top
        private readonly StyleLength m_marginEnd;   // margin-right or margin-bottom
        private readonly StyleLength m_minSize;
        private readonly StyleLength m_maxSize;

        public StyleAxis(StyleLength start, StyleLength end, StyleLength size,
            StyleLength marginStart, StyleLength marginEnd,
            StyleLength minSize, StyleLength maxSize)
        {
            m_start = start;
            m_end = end;
            m_size = size;
            m_marginStart = marginStart;
            m_marginEnd = marginEnd;
            m_minSize = minSize;
            m_maxSize = maxSize;
        }

        /// <summary>
        /// Resolves the axis. Both position and length come in carrying the values the widget
        /// already has and are left untouched where nothing was declared
        /// </summary>
        public void Resolve(float containingBlock, ref float position, ref float length)
        {
            // the size has to settle first: an anchored box is placed around the size it
            // actually ends up with, which is CSS 2.1 10.4's "recompute" wording in practice
            length = Clamp(ResolveLength(containingBlock, length), containingBlock);

            ResolvePosition(containingBlock, length, ref position);
        }

        private float ResolveLength(float containingBlock, float currentLength)
        {
            if (m_size.IsDefinite)
                return m_size.Resolve(containingBlock);

            // width is auto, or was never declared at all

            if (m_start.IsDefinite && m_end.IsDefinite)
            {
                // 10.3.7: both edges are pinned and the size is auto, so the box stretches
                // between them and auto margins count as zero
                float marginStart = m_marginStart.IsDefinite ? m_marginStart.Resolve(containingBlock) : 0.0f;
                float marginEnd = m_marginEnd.IsDefinite ? m_marginEnd.Resolve(containingBlock) : 0.0f;

                return containingBlock - m_start.Resolve(containingBlock) - m_end.Resolve(containingBlock) - marginStart - marginEnd;
            }

            // ponytail: auto with fewer than two pinned edges is CSS shrink-to-fit, which needs
            // the content's preferred size. The widget keeps the size it already has, which is
            // also what the engine did before any of this existed. WidgetLabel, WidgetText and
            // WidgetButton already measure a content size in their UpdateLayout; hoist that to a
            // virtual on Widget and call it here when a real shrink-to-fit is wanted.
            return currentLength;
        }

        private float Clamp(float length, float containingBlock)
        {
            if (m_maxSize.IsDefinite)
            {
                float max = m_maxSize.Resolve(containingBlock);

                if (length > max)
                    length = max;
            }

            // 10.4: applied after max-width, so a min that contradicts a max wins
            if (m_minSize.IsDefinite)
            {
                float min = m_minSize.Resolve(containingBlock);

                if (length < min)
                    length = min;
            }

            return length;
        }

        private void ResolvePosition(float containingBlock, float length, ref float position)
        {
            bool hasStart = m_start.IsDefinite;
            bool hasEnd = m_end.IsDefinite;

            if (!hasStart && !hasEnd)
                return; // nothing anchors the box, so it stays where the game code put it

            float start = hasStart ? m_start.Resolve(containingBlock) : 0.0f;
            float end = hasEnd ? m_end.Resolve(containingBlock) : 0.0f;

            float marginStart = m_marginStart.IsDefinite ? m_marginStart.Resolve(containingBlock) : 0.0f;
            float marginEnd = m_marginEnd.IsDefinite ? m_marginEnd.Resolve(containingBlock) : 0.0f;

            if (hasStart && hasEnd && (m_marginStart.IsAuto || m_marginEnd.IsAuto))
            {
                // 10.3.7: whatever is left over between the two pinned edges goes to the auto
                // margins, split evenly when both of them are auto -- this is how a box centres
                float free = containingBlock - start - end - length;

                if (m_marginStart.IsAuto && m_marginEnd.IsAuto)
                {
                    if (free < 0.0f)
                    {
                        // an over-large box in a left-to-right block overflows to the right
                        marginStart = 0.0f;
                        marginEnd = free;
                    }
                    else
                    {
                        marginStart = free * 0.5f;
                        marginEnd = free * 0.5f;
                    }
                }
                else if (m_marginStart.IsAuto)
                    marginStart = free - marginEnd;
                else
                    marginEnd = free - marginStart;
            }

            // With both edges pinned and no auto margin the box is over-constrained; 10.3.7
            // says a left-to-right containing block ignores the end edge, so start wins here
            if (hasStart)
                position = start + marginStart;
            else
                position = containingBlock - end - marginEnd - length;
        }
    }
}
