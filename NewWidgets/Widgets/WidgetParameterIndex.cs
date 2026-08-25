using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using NewWidgets.UI;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    // List of default parameters. We need to use it as Enum to get fast access instead of dictionary search
    internal enum WidgetParameterIndex
    {
        // Invalid
        None,

        // Size and position

        // Every property below that takes part in box layout is a StyleLength rather than a
        // float, because a float cannot tell 50% from 0.5px and the containing block is not
        // known yet when the stylesheet is read. Every other percentage in this table stays a
        // bare 0..1 fraction, which is what the shipped stylesheets mean by it.

        [WidgetParameter("width", typeof(StyleLength), UnitType.Length)]
        Width, // part of size, width and height are not inherited

        [WidgetParameter("height", typeof(StyleLength), UnitType.Length)]
        Height, // part of size,width and height are not inherited

        [WidgetParameter("min-width", typeof(StyleLength), UnitType.Length)]
        MinWidth,
        [WidgetParameter("max-width", typeof(StyleLength), UnitType.Length)]
        MaxWidth,
        [WidgetParameter("min-height", typeof(StyleLength), UnitType.Length)]
        MinHeight,
        [WidgetParameter("max-height", typeof(StyleLength), UnitType.Length)]
        MaxHeight,

        [Obsolete] // no longer used, only for compatibility
        [WidgetParameter("size", "size", typeof(Vector2), UnitType.Length, WidgetParameterInheritance.Initial,
                                         typeof(Vector2SplitProcessor), "width", "height")] // non CSS
        Size,

        [WidgetParameter("x", "x", typeof(StyleLength), UnitType.Length, WidgetParameterInheritance.Initial, typeof(DefaultProcessor), "left")] // Panorama UI, writes to left instead
        [WidgetParameter("left", typeof(StyleLength), UnitType.Length)] // CSS
        Left, // part of position

        [WidgetParameter("y", "y", typeof(StyleLength), UnitType.Length, WidgetParameterInheritance.Initial, typeof(DefaultProcessor), "top")] // Panorama UI, writes to top instead
        [WidgetParameter("top", typeof(StyleLength), UnitType.Length)] // CSS
        Top, // part of position

        [WidgetParameter("right", typeof(StyleLength), UnitType.Length)] // CSS
        Right, // anchors the far edge of the box instead of the near one
        [WidgetParameter("bottom", typeof(StyleLength), UnitType.Length)] // CSS
        Bottom,

        [WidgetParameter("z", "z", typeof(int), UnitType.None, WidgetParameterInheritance.Initial, typeof(DefaultProcessor), "z-index")] // Panorama UI, writes to z-index instead
        [WidgetParameter("z-index", typeof(int), UnitType.None)] // CSS
        ZIndex,

        // `position` used to be an obsolete Vector2 shorthand for `left top`. It is retired
        // rather than kept alongside the CSS meaning: no stylesheet in either shipped game or
        // in the test corpus ever used it, it was already marked [Obsolete], and one name that
        // means either a pair of numbers or a positioning scheme is a trap for the D132
        // validator. D134's profile is absolute positioning only, so `absolute` is the value
        // this engine already is and the rest are outside the profile.
        [WidgetParameter("position", "position", typeof(string), UnitType.None, WidgetParameterInheritance.Initial,
                                                 typeof(IgnoredProcessor), "absolute")]
        Position,

        // Every box this engine draws is a block in an absolutely positioned parent, so the
        // only part of `display` that carries meaning here is whether the box is drawn at all
        [WidgetParameter("display", "display", typeof(WidgetDisplay), UnitType.None, WidgetParameterInheritance.Initial,
                                               typeof(DisplayProcessor))]
        Display,

        // Margins. Only the four longhands are ever stored, the shorthand splits into them.
        // Note that `padding`, `--clip-margin` and `--background-padding` deliberately stay a
        // Margin of plain floats: none of them accepts auto and none of them is percentage-sized

        [WidgetParameter("margin", "margin", typeof(StyleLength), UnitType.Length, WidgetParameterInheritance.Initial,
                                             typeof(StyleLengthBoxProcessor), "margin-top", "margin-right", "margin-bottom", "margin-left")]
        MarginShorthand,

        [WidgetParameter("margin-left", typeof(StyleLength), UnitType.Length)]
        MarginLeft,
        [WidgetParameter("margin-top", typeof(StyleLength), UnitType.Length)]
        MarginTop,
        [WidgetParameter("margin-right", typeof(StyleLength), UnitType.Length)]
        MarginRight,
        [WidgetParameter("margin-bottom", typeof(StyleLength), UnitType.Length)]
        MarginBottom,

        // Common
        [WidgetParameter("opacity", typeof(float), UnitType.Percent)]
        Opacity, // opacity is a special value that should not be inherited but multiplied with parent


        [Obsolete]
        [WidgetParameter("clip", "--clip", typeof(bool), UnitType.None, WidgetParameterInheritance.Initial, typeof(OverflowProcessor), "overflow")] // instead of `visible` and `hidden` we use `true` and `false`. It doesn't cover `scroll` option although
        Clip,
        [WidgetParameter("overflow", "overflow", typeof(WidgetOverflow))] // instead of `visible` and `hidden` we use `true` and `false`. It doesn't cover `scroll` option although
        Overflow,
        [WidgetParameter("clip-path", "clip-path", typeof(Margin), UnitType.Length, WidgetParameterInheritance.Initial,
                                       typeof(ClipPathProcessor), "--clip-margin")] // CSS name. inset() is the one clip-path shape a Margin can express
        [WidgetParameter("clip_margin", "--clip-margin", typeof(Margin), UnitType.Length)] // clip margin is a Margin type, while CSS clip is a rect. We can't convert one to another. Last, so SaveCSS still writes it
        ClipMargin,

        // Properties the standard defines and this engine has no concept of. The values listed
        // after the processor are the ones the engine already behaves as, and are accepted in
        // silence; any other value is reported so a stylesheet author is not left believing it
        // took effect. See the doc comment on IgnoredProcessor.
        [WidgetParameter("box-sizing", "box-sizing", typeof(string), UnitType.None, WidgetParameterInheritance.Initial,
                                       typeof(IgnoredProcessor), "border-box")] // a widget has no border and draws its padding inside its size, so its width is already the border box
        BoxSizing,
        // WidgetTextField splits its text on the line breaks it is given and never collapses
        // whitespace, which is the `pre` family. It does not wrap a long line either, so no
        // value is honoured exactly; the two that preserve breaks are the closest and pass
        [WidgetParameter("white-space", "white-space", typeof(string), UnitType.None, WidgetParameterInheritance.Inherit,
                                        typeof(IgnoredProcessor), "pre", "pre-wrap")]
        WhiteSpace,
        [WidgetParameter("border", "border", typeof(string), UnitType.None, WidgetParameterInheritance.Initial,
                                   typeof(IgnoredProcessor), "none")] // nothing in this engine draws a border; a frame is a background nine-patch
        Border,
        [WidgetParameter("padding", typeof(Margin), UnitType.Length)] // padding is of type Margin
        Padding,

        // Background

        [WidgetParameter("back_color", "background-color", typeof(uint), UnitType.Color)] // unlike HTML it doesn't supports transparency yet
        BackColor,
        [WidgetParameter("back_image", "background-image", typeof(string), UnitType.Url)]
        BackImage,
        [WidgetParameter("back_style", "background-repeat", typeof(WidgetBackgroundStyle), UnitType.None, WidgetParameterInheritance.Initial,
                                       typeof(BackgroundRepeatProcessor))] // the CSS keywords on top of this engine's own repeat modes
        BackStyle,


        [WidgetParameter("back_depth", "--background-depth", typeof(WidgetBackgroundDepth))] // nothing like that in HTML
        BackDepth,
        [WidgetParameter("back_scale", "background-size", typeof(float), UnitType.Percent, WidgetParameterInheritance.Initial,
                                       typeof(BackgroundSizeProcessor), "background-repeat")] // a single percentage is a scale factor; contain/cover/100% 100% choose a background style instead
        BackScale,
        [WidgetParameter("back_angle", "--background-rotation", typeof(float))]
        BackAngle,
        [WidgetParameter("back_pivot", "background-position", typeof(Vector2), UnitType.Percent, WidgetParameterInheritance.Initial,
                                       typeof(BackgroundPositionProcessor), "--background-offset")] // a percentage or keyword pair is the pivot; a length pair is a sprite-sheet offset and goes elsewhere
        BackPivot,

        // D133's sprite-sheet idiom, `background-position: -804px -225px`, in pixels. It has to
        // be a separate property because the pivot above is a 0..1 fraction and FloatParse
        // hands back a bare number either way, so a length and a percentage are otherwise
        // indistinguishable -- which is how minus 804 *fractions* used to be stored in silence
        [WidgetParameter("back_offset", "--background-offset", typeof(Vector2), UnitType.Length)]
        BackOffset,

        // Border image (D130): the standard's own nine-patch vocabulary. Both longhands write
        // this engine's own background properties as well as their own, the way
        // `background-size: contain` above already writes `background-repeat`: there is one
        // background sprite here, and a rule that names it in standard words must reach the
        // same renderer that `background-image` plus `background-repeat: nineimage` reaches
        [WidgetParameter("border-image-source", "border-image-source", typeof(string), UnitType.Url, WidgetParameterInheritance.Initial,
                                                typeof(BorderImageSourceProcessor), "background-image")]
        BorderImageSource,
        [WidgetParameter("border-image-slice", "border-image-slice", typeof(Margin), UnitType.Percent, WidgetParameterInheritance.Initial,
                                               typeof(BorderImageSliceProcessor), "--border-image-fill", "background-repeat")]
        BorderImageSlice,
        [WidgetParameter("--border-image-fill", typeof(bool))] // the `fill` keyword of border-image-slice, which is a flag rather than a number
        BorderImageFill,
        [WidgetParameter("border-image-width", typeof(Margin), UnitType.Percent)]
        BorderImageWidth,
        [WidgetParameter("border-image-repeat", typeof(WidgetBorderImageRepeat))]
        BorderImageRepeat,
        [WidgetParameter("back_padding", "--background-padding", typeof(Margin), UnitType.Length)]
        BackPadding,
        [WidgetParameter("back_opacity", "background-color-opacity",  typeof(float), UnitType.Percent)] // Panorama UI compat, invalid in CSS
        BackOpacity,

        // Text

        [WidgetParameter("font", "font-family", typeof(Font), UnitType.None, WidgetParameterInheritance.Inherit,
                                 typeof(FontFamilyProcessor))]
        Font,
        [WidgetParameter("font_size", "font-size", typeof(float), UnitType.FontUnits, WidgetParameterInheritance.Inherit)]
        FontSize,
        [WidgetParameter("text_color", "color", typeof(uint), UnitType.Color, WidgetParameterInheritance.Inherit)]
        TextColor,
        [WidgetParameter("line_spacing", "line-height", typeof(float), UnitType.Percent, WidgetParameterInheritance.Inherit)]
        LineSpacing,
        [WidgetParameter("text_align", "text-align", typeof(WidgetAlign), UnitType.None, WidgetParameterInheritance.Inherit)] // TODO: more alignment options
        TextAlign,
        //[WidgetParameter("text_padding", "--text-padding", typeof(Margin), UnitType.Length)] // changed to "padding"
        //TextPadding,
        [WidgetParameter("richtext", "--richtext", typeof(bool))]
        RichText,

        // Image // migrated to background styles

        //[WidgetParameter("image", typeof(string), UnitType.Url)] // image name
        //Image,
        //[WidgetParameter("image_style", "--image-style", typeof(WidgetBackgroundStyle))]
        //ImageStyle,
        //[WidgetParameter("image_angle", "--image-rotation", typeof(float))]
        //ImageAngle,
        //[WidgetParameter("image_pivot", "--image-position", typeof(Vector2), UnitType.Percent)]
        //ImagePivot,
        //[WidgetParameter("image_padding", "--image-padding", typeof(Margin), UnitType.Length)] // changed to "padding"
        //ImagePadding,
        //[WidgetParameter("image_color", "--image-color", typeof(uint), UnitType.Color)]
        //ImageColor,
        //[WidgetParameter("image_opacity", "--image-opacity", typeof(float), UnitType.Percent)]
        //ImageOpacity,

        // Text edit

        [WidgetParameter("caret-color", typeof(uint), UnitType.Color)] // CSS name, accepted
        [WidgetParameter("cursor_color", "--cursor-color", typeof(uint), UnitType.Color)] // legacy. Last, so SaveCSS still writes it
        CursorColor,
        [WidgetParameter("cursor_char", "--cursor_char")]
        CursorChar,
        [WidgetParameter("mask_char", "--mask_char")]
        MaskChar,


        // Button

        [WidgetParameter("button_layout", "--button-layout", typeof(WidgetButtonLayout))]
        ButtonLayout,
        //[WidgetParameter("button_image_padding", "--button-image-padding", typeof(Margin), UnitType.Length)]
        //ButtonImagePadding,
        //[WidgetParameter("button_text_padding", "--button-text-padding",  typeof(Margin), UnitType.Length)]
        //ButtonTextPadding,
        [WidgetParameter("button_animate_scale", "--button-animate-scale", typeof(float), UnitType.Percent)]
        ButtonAnimateScale,
        [WidgetParameter("button_animate_pivot", "--button-animate-pivot", typeof(Vector2), UnitType.Percent)]
        ButtonAnimatePivot,
        [WidgetParameter("button_animate_time", "--button-animate-time", typeof(int))]
        ButtonAnimateTime,

        // Font
        [WidgetParameter("src", typeof(string), UnitType.Url)] // CSS @font-face name, accepted
        [WidgetParameter("font_resource", "--font-resource", typeof(string), UnitType.Url)] // legacy. Last, so SaveCSS still writes it
        FontResource,
        [WidgetParameter("letter-spacing", typeof(float))] // CSS name, accepted
        [WidgetParameter("font_spacing", "--font-spacing", typeof(float))] // legacy. Last, so SaveCSS still writes it
        FontSpacing,
        [WidgetParameter("font_shift", "--font-shift", typeof(int))]
        FontShift,
        [WidgetParameter("font_leading", "--font-leading", typeof(int))]
        FontLeading,
        [WidgetParameter("font_baseline", "--font-baseline", typeof(int))]
        FontBaseline,

        Max
    }


    internal class DefaultProcessor : IParameterProcessor
    {
        private WidgetParameterIndex m_target;

        private Type m_type;
        private string m_targetName;
        private UnitType m_unitType;

        public void Init(string target, Type type, UnitType unitType, string[] parameters)
        {
            m_targetName = (parameters != null && parameters.Length >= 1) ? parameters[0] : target;
            m_type = type;
            m_unitType = unitType;
        }

        public void Process(IDictionary<WidgetParameterIndex, object> data, string stringValue)
        {
            if (m_target == 0)
            {
                m_target = WidgetParameterMap.GetIndexByName(m_targetName);
                Debug.Assert(m_target != 0);
            }

            object value = ConversionHelper.ParseValue(m_type, m_unitType, stringValue);

            data[m_target] = value;
        }
    }

    internal class Vector2SplitProcessor : IParameterProcessor
    {
        private string m_targetNameX;
        private string m_targetNameY;
        private WidgetParameterIndex m_targetX;
        private WidgetParameterIndex m_targetY;
        private UnitType m_unitType;

        public void Init(string target, Type type, UnitType unitType, string[] parameters)
        {
            Debug.Assert(parameters != null && parameters.Length == 2);

            m_unitType = unitType;

            m_targetNameX = parameters[0];
            m_targetNameY = parameters[1];
        }

        public void Process(IDictionary<WidgetParameterIndex, object> data, string stringValue)
        {
            if (m_targetX == 0)
            {
                m_targetX = WidgetParameterMap.GetIndexByName(m_targetNameX);
                Debug.Assert(m_targetX != 0);

                m_targetY = WidgetParameterMap.GetIndexByName(m_targetNameY);
                Debug.Assert(m_targetY != 0);
            }

            Vector2 value = (Vector2)ConversionHelper.ParseValue(typeof(Vector2), m_unitType, stringValue);

            // both users of this processor -- the obsolete `size` and `position` shorthands --
            // target box properties, and those store StyleLength rather than a bare float
            data[m_targetX] = StyleLength.Pixels(value.X);
            data[m_targetY] = StyleLength.Pixels(value.Y);
        }
    }

    internal class Vector3SplitProcessor : IParameterProcessor
    {
        private string m_targetNameX;
        private string m_targetNameY;
        private string m_targetNameZ;
        private WidgetParameterIndex m_targetX;
        private WidgetParameterIndex m_targetY;
        private WidgetParameterIndex m_targetZ;
        private UnitType m_unitType;

        public void Init(string target, Type type, UnitType unitType, string[] parameters)
        {
            Debug.Assert(parameters != null && parameters.Length == 3);

            m_unitType = unitType;

            m_targetNameX = parameters[0];
            m_targetNameY = parameters[1];
            m_targetNameZ = parameters[2];
        }

        public void Process(IDictionary<WidgetParameterIndex, object> data, string stringValue)
        {
            if (m_targetX == 0)
            {
                m_targetX = WidgetParameterMap.GetIndexByName(m_targetNameX);
                Debug.Assert(m_targetX != 0);

                m_targetY = WidgetParameterMap.GetIndexByName(m_targetNameY);
                Debug.Assert(m_targetY != 0);

                m_targetZ = WidgetParameterMap.GetIndexByName(m_targetNameZ);
                Debug.Assert(m_targetZ != 0);
            }

            Vector3 value = (Vector3)ConversionHelper.ParseValue(typeof(Vector3), m_unitType, stringValue);

            data[m_targetX] = value.X;
            data[m_targetY] = value.Y;
            data[m_targetZ] = value.Z;
        }
    }

    /// <summary>
    /// Splits the `margin` shorthand into its four longhands. CSS orders a box shorthand
    /// clockwise from the top: one value sets every side, two are vertical then horizontal,
    /// three are top, horizontal, bottom, and four are the sides in that order.
    /// ConversionHelper.MarginParse already does this ordering, but it produces four floats
    /// and cannot carry `auto`, which is the whole point of writing `margin: 0 auto`
    /// </summary>
    internal class StyleLengthBoxProcessor : IParameterProcessor
    {
        private readonly WidgetParameterIndex[] m_targets = new WidgetParameterIndex[4];

        private string[] m_targetNames;

        public void Init(string target, Type type, UnitType unitType, string[] parameters)
        {
            Debug.Assert(parameters != null && parameters.Length == 4);

            m_targetNames = parameters;
        }

        public void Process(IDictionary<WidgetParameterIndex, object> data, string stringValue)
        {
            if (m_targets[0] == 0)
                for (int i = 0; i < m_targets.Length; i++)
                {
                    m_targets[i] = WidgetParameterMap.GetIndexByName(m_targetNames[i]);
                    Debug.Assert(m_targets[i] != 0);
                }

            string[] values = stringValue.Split(new[] { ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (values.Length < 1 || values.Length > 4)
                throw new ArgumentException("Invalid string value for a box shorthand!");

            StyleLength top = StyleLength.Parse(values[0]);
            StyleLength right = top;
            StyleLength bottom = top;
            StyleLength left = top;

            if (values.Length >= 2)
            {
                right = StyleLength.Parse(values[1]);
                left = right;
            }

            if (values.Length >= 3)
                bottom = StyleLength.Parse(values[2]);

            if (values.Length == 4)
                left = StyleLength.Parse(values[3]);

            data[m_targets[0]] = top;
            data[m_targets[1]] = right;
            data[m_targets[2]] = bottom;
            data[m_targets[3]] = left;
        }
    }

    /// <summary>
    /// Base for the processors below, each of which reads one CSS property and writes at most
    /// two entries: the property's own index, and optionally one companion named by the first
    /// processor parameter. The name-to-index map is built by <see cref="WidgetParameterMap"/>'s
    /// static constructor, which is also what constructs these processors, so a lookup cannot
    /// happen in <see cref="Init"/> and is deferred to the first declaration instead -- the same
    /// shape <see cref="DefaultProcessor"/> already uses.
    /// </summary>
    internal abstract class CssPropertyProcessor : IParameterProcessor
    {
        private string m_propertyName;
        private string m_companionName;
        private WidgetParameterIndex m_propertyIndex;
        private WidgetParameterIndex m_companionIndex;

        /// <summary>
        /// Index of the property itself
        /// </summary>
        protected WidgetParameterIndex PropertyIndex
        {
            get
            {
                ResolveIndices();
                return m_propertyIndex;
            }
        }

        /// <summary>
        /// Index of the property named by the first processor parameter, where there is one
        /// </summary>
        protected WidgetParameterIndex CompanionIndex
        {
            get
            {
                ResolveIndices();
                return m_companionIndex;
            }
        }

        public virtual void Init(string target, Type type, UnitType unitType, string[] parameters)
        {
            m_propertyName = target;
            m_companionName = (parameters != null && parameters.Length >= 1) ? parameters[0] : null;
        }

        public abstract void Process(IDictionary<WidgetParameterIndex, object> data, string stringValue);

        protected void Report(string reason, string value)
        {
            WindowController.Instance.LogMessage("Property {0}: {1}, value {2} is ignored", m_propertyName, reason, value);
        }

        private void ResolveIndices()
        {
            if (m_propertyIndex != 0)
                return;

            m_propertyIndex = WidgetParameterMap.GetIndexByName(m_propertyName);
            Debug.Assert(m_propertyIndex != 0);

            if (m_companionName != null)
            {
                m_companionIndex = WidgetParameterMap.GetIndexByName(m_companionName);
                Debug.Assert(m_companionIndex != 0);
            }
        }
    }

    /// <summary>
    /// A property the standard defines and this engine has no concept of at all.
    ///
    /// The processor parameters list the values the engine already behaves as: those are
    /// accepted in silence, because honouring them and ignoring them are the same thing here.
    /// Any other value is reported, so a stylesheet author is not left believing a declaration
    /// took effect. Nothing is ever stored, so the property costs nothing to read.
    /// </summary>
    internal class IgnoredProcessor : IParameterProcessor
    {
        private string m_propertyName;
        private string[] m_acceptedValues;

        public void Init(string target, Type type, UnitType unitType, string[] parameters)
        {
            m_propertyName = target;
            m_acceptedValues = parameters;
        }

        public void Process(IDictionary<WidgetParameterIndex, object> data, string stringValue)
        {
            if (m_acceptedValues != null)
                for (int i = 0; i < m_acceptedValues.Length; i++)
                    if (string.Equals(m_acceptedValues[i], stringValue, StringComparison.OrdinalIgnoreCase))
                        return;

            WindowController.Instance.LogMessage("Property {0} is accepted and ignored by this engine, and the value {1} cannot be honoured", m_propertyName, stringValue);
        }
    }

    /// <summary>
    /// CSS <c>display</c>, reduced to the two values D134's profile leaves with a meaning.
    /// Anything else is reported and treated as a block, because the profile is absolute
    /// positioning only and flow, flex and grid are outside it by design.
    /// </summary>
    internal class DisplayProcessor : CssPropertyProcessor
    {
        public override void Process(IDictionary<WidgetParameterIndex, object> data, string stringValue)
        {
            switch (stringValue.ToLowerInvariant())
            {
                case "none":
                    data[PropertyIndex] = WidgetDisplay.None;
                    return;
                case "block":
                case "inline":
                case "inline-block":
                    data[PropertyIndex] = WidgetDisplay.Block;
                    return;
                default:
                    Report("only none and block are inside the absolute-positioning profile", stringValue);
                    data[PropertyIndex] = WidgetDisplay.Block;
                    return;
            }
        }
    }

    /// <summary>
    /// CSS <c>clip-path</c>, of which <c>inset()</c> is the one shape a
    /// <see cref="Margin"/> can express -- and it is exactly this engine's own
    /// <c>--clip-margin</c>, which is what the conformance stylesheet was written with before
    /// it was migrated to standard names.
    /// </summary>
    internal class ClipPathProcessor : CssPropertyProcessor
    {
        public override void Process(IDictionary<WidgetParameterIndex, object> data, string stringValue)
        {
            if (string.Equals(stringValue, "none", StringComparison.OrdinalIgnoreCase))
            {
                data[CompanionIndex] = Margin.Empty;
                return;
            }

            if (!stringValue.StartsWith("inset(", StringComparison.OrdinalIgnoreCase) || !stringValue.EndsWith(")", StringComparison.Ordinal))
            {
                // ponytail: circle(), ellipse(), polygon() and path() have no Margin equivalent
                // and would need a real clip shape on the renderer to mean anything
                Report("only the inset() shape has an equivalent here", stringValue);
                return;
            }

            string inset = stringValue.Substring(6, stringValue.Length - 7);

            // inset() takes an optional `round <radius>` tail that a rectangle cannot carry
            int round = inset.IndexOf("round", StringComparison.OrdinalIgnoreCase);

            if (round >= 0)
            {
                Report("the rounded corners of inset() are dropped", stringValue);
                inset = inset.Substring(0, round);
            }

            data[CompanionIndex] = ConversionHelper.MarginParse(inset, UnitType.Length);
        }
    }

    /// <summary>
    /// CSS <c>background-repeat</c>, whose keywords sit on top of this engine's own repeat
    /// modes in one enum. <c>repeat</c> and its axis variants all tile, because there is no
    /// single-axis tiling here; the legacy names (<c>nineimage</c>, <c>threeimage</c>,
    /// <c>imagefit</c> and the rest) keep working, and the golden master guards them.
    /// </summary>
    internal class BackgroundRepeatProcessor : CssPropertyProcessor
    {
        public override void Process(IDictionary<WidgetParameterIndex, object> data, string stringValue)
        {
            switch (stringValue.ToLowerInvariant())
            {
                case "repeat":
                case "repeat-x":
                case "repeat-y":
                case "round":
                case "space":
                    // ponytail: CSS tiles one axis at a time and rounds or spaces the tiles;
                    // WidgetBackgroundStyle.ImageTiled tiles both axes and does neither. Split
                    // the style enum into a repeat mode and a fit mode to tell them apart.
                    data[PropertyIndex] = WidgetBackgroundStyle.ImageTiled;
                    return;
                default:
                    data[PropertyIndex] = ConversionHelper.ParseValue(typeof(WidgetBackgroundStyle), UnitType.None, stringValue);
                    return;
            }
        }
    }

    /// <summary>
    /// CSS <c>background-size</c>. A single percentage stays what both shipped games mean by
    /// it -- a scale factor applied to the sprite, 92 declarations between them and guarded by
    /// the golden master -- while the keyword forms choose one of this engine's fit modes.
    ///
    /// ponytail: the engine folds CSS's two properties, background-repeat and background-size,
    /// into one WidgetBackgroundStyle, so within a rule the later of the two wins. Splitting
    /// that enum into a repeat mode and a fit mode is the upgrade path.
    /// </summary>
    internal class BackgroundSizeProcessor : CssPropertyProcessor
    {
        public override void Process(IDictionary<WidgetParameterIndex, object> data, string stringValue)
        {
            string[] values = stringValue.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (values.Length == 1)
            {
                switch (values[0].ToLowerInvariant())
                {
                    case "contain":
                        data[CompanionIndex] = WidgetBackgroundStyle.ImageFit;
                        return;
                    case "cover":
                        data[CompanionIndex] = WidgetBackgroundStyle.ImageFill;
                        return;
                    case "auto":
                        return; // the initial value: the sprite keeps its own size
                    default:
                        data[PropertyIndex] = ConversionHelper.FloatParse(values[0], UnitType.Percent);
                        return;
                }
            }

            if (values.Length == 2 && IsFullSize(values[0]) && IsFullSize(values[1]))
            {
                data[CompanionIndex] = WidgetBackgroundStyle.ImageStretch;
                return;
            }

            // ponytail: an arbitrary two-value background-size sizes the two axes separately,
            // which needs a Vector2 scale on the renderer rather than the single float here
            Report("only contain, cover, 100% 100% and a single scale are supported", stringValue);
        }

        private static bool IsFullSize(string value)
        {
            return value == "100%";
        }
    }

    /// <summary>
    /// CSS <c>background-position</c>, which this engine reads two different ways depending on
    /// the unit -- and which is why the unit has to survive parsing at all.
    ///
    /// A percentage or keyword pair is the sprite pivot, a 0..1 fraction, which is what both
    /// shipped games write. A length pair is D133's sprite-sheet offset in pixels and goes to
    /// its own property, because storing minus 804 pixels as minus 804 *fractions* is the
    /// silent nonsense this split exists to stop. Per-property by design, D129:
    /// <see cref="ConversionHelper.FloatParse"/> still ignores its unit argument everywhere
    /// else, and 96 corpus declarations depend on that.
    /// </summary>
    internal class BackgroundPositionProcessor : CssPropertyProcessor
    {
        private const int EitherAxis = 0;
        private const int HorizontalAxis = 1;
        private const int VerticalAxis = 2;

        public override void Process(IDictionary<WidgetParameterIndex, object> data, string stringValue)
        {
            string[] values = stringValue.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (values.Length < 1 || values.Length > 2)
            {
                // ponytail: the three and four value forms name an edge and an offset from it,
                // as in `left 10px top 20px`, which needs an origin edge per axis to store
                Report("only the one and two value forms are supported", stringValue);
                return;
            }

            float x;
            float y;
            bool xIsPercent;
            bool yIsPercent;
            int xAxis;
            int yAxis;

            if (!TryParseComponent(values[0], out x, out xIsPercent, out xAxis))
            {
                Report("the first value is not a length, a percentage or an edge keyword", stringValue);
                return;
            }

            // one value sets the horizontal position and centres the other axis
            if (values.Length == 1)
            {
                y = 0.5f;
                yIsPercent = true;
                yAxis = EitherAxis;
            }
            else if (!TryParseComponent(values[1], out y, out yIsPercent, out yAxis))
            {
                Report("the second value is not a length, a percentage or an edge keyword", stringValue);
                return;
            }

            // `top left` names the axes in the other order, so the keywords decide which is which
            if (xAxis == VerticalAxis || yAxis == HorizontalAxis)
            {
                float swapValue = x;
                bool swapPercent = xIsPercent;

                x = y;
                xIsPercent = yIsPercent;
                y = swapValue;
                yIsPercent = swapPercent;
            }

            if (xIsPercent && yIsPercent)
            {
                data[PropertyIndex] = new Vector2(x, y);
                return;
            }

            if (!xIsPercent && !yIsPercent)
            {
                data[CompanionIndex] = new Vector2(x, y);
                return;
            }

            // ponytail: a mixed pair, `-804px 50%`, would need one axis resolved against the
            // sprite and the other against the box, which no single property here can hold
            Report("a percentage and a length cannot be mixed on the two axes", stringValue);
        }

        private static bool TryParseComponent(string value, out float number, out bool isPercent, out int axis)
        {
            isPercent = true;

            switch (value.ToLowerInvariant())
            {
                case "left":
                    number = 0.0f;
                    axis = HorizontalAxis;
                    return true;
                case "right":
                    number = 1.0f;
                    axis = HorizontalAxis;
                    return true;
                case "top":
                    number = 0.0f;
                    axis = VerticalAxis;
                    return true;
                case "bottom":
                    number = 1.0f;
                    axis = VerticalAxis;
                    return true;
                case "center":
                    number = 0.5f;
                    axis = EitherAxis;
                    return true;
            }

            axis = EitherAxis;
            isPercent = value.EndsWith("%", StringComparison.Ordinal);

            try
            {
                number = ConversionHelper.FloatParse(value);
            }
            catch (FormatException)
            {
                number = 0.0f;
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// CSS <c>border-image-source</c>. Stores the url as it was authored, so <c>SaveCSS</c>
    /// writes a D186 reference back whole (D188), and mirrors it into <c>background-image</c>,
    /// which is the property the renderer reads. <c>none</c> is the initial value and clears
    /// the background sprite rather than naming one called "none".
    /// </summary>
    internal class BorderImageSourceProcessor : CssPropertyProcessor
    {
        public override void Process(IDictionary<WidgetParameterIndex, object> data, string stringValue)
        {
            string value = ConversionHelper.StringParse(stringValue, UnitType.Url);

            data[PropertyIndex] = value;

            if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase))
                data[CompanionIndex] = string.Empty;
            else
                data[CompanionIndex] = value;
        }
    }

    /// <summary>
    /// CSS <c>border-image-slice</c>: one to four numbers or percentages, plus the optional
    /// <c>fill</c> keyword, which is a flag and is stored on its own.
    ///
    /// A slice at thirds also picks the background style, because thirds is exactly what this
    /// engine's two patch renderers cut: <c>33.3333% fill</c> is a nine-patch and
    /// <c>0 33.3333% fill</c> is the horizontal three-patch (D193). Any other slice leaves the
    /// style alone and is drawn by the arbitrary-slice path in <c>WidgetBackground</c>.
    ///
    /// ponytail: the four values are stored as a Margin of bare floats, so a percentage
    /// arrives as a 0..1 fraction and a number as itself -- distinguishable by inspection but
    /// not by type. The arbitrary-slice renderer tells them apart by magnitude; give this a
    /// StyleLength box to do it by unit.
    /// </summary>
    internal class BorderImageSliceProcessor : CssPropertyProcessor
    {
        private string m_backgroundStyleName;
        private WidgetParameterIndex m_backgroundStyle;

        public override void Init(string target, Type type, UnitType unitType, string[] parameters)
        {
            base.Init(target, type, unitType, parameters);

            Debug.Assert(parameters != null && parameters.Length >= 2);

            m_backgroundStyleName = parameters[1];
        }

        public override void Process(IDictionary<WidgetParameterIndex, object> data, string stringValue)
        {
            string[] values = stringValue.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            bool fill = false;
            StringBuilder numbers = new StringBuilder();

            // the fill keyword may sit at either end of the number list
            for (int i = 0; i < values.Length; i++)
            {
                if (string.Equals(values[i], "fill", StringComparison.OrdinalIgnoreCase))
                {
                    fill = true;
                    continue;
                }

                if (numbers.Length != 0)
                    numbers.Append(' ');

                numbers.Append(values[i]);
            }

            if (numbers.Length == 0)
            {
                Report("the slice needs at least one number", stringValue);
                return;
            }

            Margin slice = ConversionHelper.MarginParse(numbers.ToString(), UnitType.Percent);

            data[PropertyIndex] = slice;
            data[CompanionIndex] = fill;

            if (m_backgroundStyle == 0)
            {
                m_backgroundStyle = WidgetParameterMap.GetIndexByName(m_backgroundStyleName);
                Debug.Assert(m_backgroundStyle != 0);
            }

            int tileX;
            int tileY;

            // the `fill` keyword is not read here: both patch renderers always draw the middle
            // cell, so a hollow frame is not a style this engine has
            if (WidgetManager.TryGetBorderImageGrid(slice, out tileX, out tileY))
                data[m_backgroundStyle] = tileY == 1 ? WidgetBackgroundStyle.ThreeImage : WidgetBackgroundStyle.NineImage;
        }
    }

    /// <summary>
    /// CSS <c>font-family</c>, which is a comma separated stack of names rather than one name.
    /// The raw string used to go straight to <c>GetFont</c>, so a quoted name failed, a stack
    /// failed, and <c>inherit</c> logged an error about a font nobody declared.
    /// </summary>
    internal class FontFamilyProcessor : CssPropertyProcessor
    {
        public override void Process(IDictionary<WidgetParameterIndex, object> data, string stringValue)
        {
            // font-family already inherits, so the CSS-wide keywords are served by storing
            // nothing at all and letting the cascade do what it does
            switch (stringValue.ToLowerInvariant())
            {
                case "inherit":
                case "initial":
                case "unset":
                case "revert":
                    return;
            }

            string[] families = stringValue.Split(',');

            // the first family this engine actually has a font for wins, which is what a
            // browser does with a stack ending in a generic name it cannot supply either
            for (int i = 0; i < families.Length; i++)
            {
                Font font;

                if (WidgetManager.TryGetFont(WidgetManager.UnquoteFontFamily(families[i]), out font))
                {
                    data[PropertyIndex] = font;
                    return;
                }
            }

            WindowController.Instance.LogError("WidgetManager got font-family {0} naming no registered font", stringValue);
        }
    }

    internal class OverflowProcessor : IParameterProcessor
    {
        private WidgetParameterIndex m_target;
        private string m_targetName;

        public void Init(string target, Type type, UnitType unitType, string[] parameters)
        {
            m_targetName = (parameters != null && parameters.Length >= 1) ? parameters[0] : target;
        }

        public void Process(IDictionary<WidgetParameterIndex, object> data, string stringValue)
        {
            if (m_target == 0)
            {
                m_target = WidgetParameterMap.GetIndexByName(m_targetName);
                Debug.Assert(m_target != 0);
            }

            bool value = (bool)ConversionHelper.ParseValue(typeof(bool), UnitType.None, stringValue);

            data[m_target] = value ? WidgetOverflow.Hidden : WidgetOverflow.Visible;
        }
    }


}

