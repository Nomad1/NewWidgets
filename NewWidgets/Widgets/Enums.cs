using System;
using System.Numerics;
using NewWidgets.UI;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    [Flags]
    public enum WidgetAlign
    {
        None = 0,
        Left = 0x01,
        Right = 0x02,
        Top = 0x10,
        Bottom = 0x20,

        HorizontalCenter = Left | Right,
        VerticalCenter = Top | Bottom,
        NotTopLeft = Right | Bottom
    }

    public enum WidgetBackgroundStyle
    {
        /// <summary>
        /// No background at all
        /// </summary>
        None = 0,
        /// <summary>
        /// Center and no stretch
        /// </summary>
        Image = 1,
        /// <summary>
        /// Center and aspect fit. Good only for fixed size windows
        /// </summary>
        ImageFit = 2,
        /// <summary>
        /// Aspect fit starting from top left
        /// </summary>
        ImageTopLeft = 3,
        /// <summary>
        /// Center and stretch
        /// </summary>
        ImageStretch = 4,
        /// <summary>
        /// Tiles image using BackgroundScale and image size to calculate number of tiles
        /// </summary>
        ImageTiled = 5,
        /// <summary>
        /// Divide image to 33/33/33 percents both vertically and horizontally and scale central parts on corresponding axes
        /// Note that BackgroundPivot defaults to 0.5;0.5 meaning that tiling starts at the center, but actually
        /// first tiling image is at the top-left corner so BackgroundPivot position is shifted by 0.5;0.5
        /// </summary>
        NineImage = 6,
        /// <summary>
        /// Image is divided to 33/33/33 percents horizontally
        /// </summary>
        ThreeImage = 7,
        /// <summary>
        /// Center and aspect fit without gaps
        /// </summary>
        ImageFill = 8,
        /// <summary>
        /// Aspect fill starting from top left
        /// </summary>
        ImageTopLeftFill = 9,
    }

    public enum WidgetBackgroundDepth
    {
        /// <summary>
        /// Far layer, not clipped, below content
        /// </summary>
        Back = 0,
        /// <summary>
        /// Topmost layer, not clipped, above content
        /// </summary>
        Top = 1,
        /// <summary>
        /// Middle layer, clipped, below conent
        /// </summary>
        BackClipped = 2,
        /// <summary>
        /// Middle layer, clipped, above content
        /// </summary>
        TopClipped = 3,
    }

    [Flags]
    public enum WidgetScrollType
    {
        /// <summary>
        /// Invisible scroll
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Normal scroll
        /// </summary>
        Normal = 0x01,
        /// <summary>
        /// Force visible
        /// </summary>
        Visible = 0x02,
        /// <summary>
        /// Inertial scrolling when released
        /// </summary>
        Inertial = 0x04,
        /// <summary>
        /// Automatically hide scroll when it's not needed
        /// </summary>
        AutoHide = 0x08
    }

    /// <summary>
    /// State is a binary state mask working almost the same as CSS pseudo-class
    /// </summary>
    [Flags]
    public enum WidgetState
    {
        // main style
        [WidgetPseudoClass("")]
        [WidgetPseudoClass("enabled")] // by design all our widgets are enabled meaning that :enabled class is default
        Normal = 0x0, // :default

        // hovered style
        [WidgetPseudoClass("hover")]
        Hovered = 0x01, // :hover
        SelectedHovered = Selected | Hovered, // :hover:selected
        SelectedDisabledHovered = Selected | Disabled | Hovered, // :hover:selected:disabled
        DisabledHovered = Normal | Disabled | Hovered, // hover:disabled

        // 1st grade, can be only subset of Normal
        [WidgetPseudoClass("checked")] // :checked is used for checkboxes
        [WidgetPseudoClass("selected")] // :selected is for Panorama UI compatibility
        [WidgetPseudoClass("active")] // :active represents the state when the element is currently being activated by the user
        [WidgetPseudoClass("focus")] // :focus represents the state when the element is currently selected to receive input
        Selected = 0x02, // :checked , :selected, :active

        // 2nd grade, can be only subset of Normal or Selected
        [WidgetPseudoClass("disabled")]
        Disabled = 0x04, // :disabled
        SelectedDisabled = Selected | Disabled, // :selected:disabled

        Max = 0x0F
    }

    internal enum WidgetParameterInheritance
    {
        Inherit, // always inherit from the parent
        Initial, // always have default value
        Unset, // goes back to inherit/initial
        Revert // NYI
    }

    [Flags]
    internal enum WidgetParameterUnits
    {
        None, // default for the data type
        Pixels, // px
        Percentage, // %
        Auto,
    }

    public enum WidgetStyleClassIndex
    {
        None = 0,
    }

    // List of default parameters. We need to use it as Enum to get fast access instead of dictionary search
    internal enum WidgetParameterIndex
    {
        // Invalid
        None,

        [WidgetCSSParameter("opacity", typeof(float))]
        [WidgetXMLParameter("opacity", typeof(float))]
        Opacity, // opacity is a special value that should not be inherited but multiplied with parent

        [WidgetCSSParameter("width", typeof(float))]
        Width, // part of size, width and height are not inherited

        [WidgetCSSParameter("height", typeof(float))]
        Height, // part of size,width and height are not inherited

        [WidgetXMLParameter("size", typeof(Vector2))] // non CSS
        Size,

        [WidgetCSSParameter("x", typeof(float))] // Panorama UI compat
        X, // part of position
        [WidgetCSSParameter("left", typeof(float))] // CSS
        Left, // part of position
        [WidgetCSSParameter("y", typeof(float))] // Panorama UI compat
        Y, // part of position
        [WidgetCSSParameter("top", typeof(float))] // CSS
        Top, // part of position
        [WidgetCSSParameter("z", typeof(float))] // Panorama UI compat
        Z, // part of position
        [WidgetCSSParameter("z", typeof(int))] // CSS
        ZIndex,
        [WidgetCSSParameter("position", typeof(Vector3))]
        Position,


        // Common
        [WidgetCSSParameter("overflow", typeof(bool))] // TODO: enum for this
        [WidgetXMLParameter("clip", typeof(bool))] // instead of `visible` and `hidden` we use `true` and `false`. It doesn't cover `scroll` option although
        Clip,
        [WidgetCSSParameter("clip", typeof(Margin))] // clip margin is a Margin type, not a rect. TODO:
        [WidgetXMLParameter("clip_margin", typeof(Margin))] // xml clip margin is a Margin type, not a rect. TODO:
        ClipMargin,
        [WidgetCSSParameter("padding", typeof(Margin))] // padding is of type Margin
        [WidgetXMLParameter("padding", typeof(Margin))] // padding is of type Margin
        Padding,

        // Background

        [WidgetCSSParameter("background-color", typeof(uint))] // unlike HTML it doesn't supports transparency yet. TODO: wrapper type for Color
        [WidgetXMLParameter("back_color", typeof(uint))] // unlike HTML it doesn't supports transparency yet
        BackColor,
        [WidgetCSSParameter("background-image", typeof(string))]
        [WidgetXMLParameter("back_image", typeof(string))]
        BackImage,
        [WidgetCSSParameter("background-repeat", typeof(WidgetBackgroundStyle))] // we have own repeat modes so this needs to be worked out
        [WidgetXMLParameter("back_style", typeof(WidgetBackgroundStyle))] // TODO: another parameter that converts to one of our styles
        BackStyle,


        [WidgetXMLParameter("back_depth", typeof(WidgetBackgroundDepth))] // nothing like that in HTML
        BackDepth,
        [WidgetCSSParameter("background-size", typeof(float))] // right now its a single percentage value. TODO: another property to support two values and exact length
        [WidgetXMLParameter("back_scale", typeof(float))] // right now its a single percentage value. TODO: another property to support two values and exact length
        BackScale,
        [WidgetXMLParameter("back_angle", typeof(float))]
        BackAngle,
        [WidgetXMLParameter("back_pivot", typeof(Vector2))] // pivot + padding are powerful but in CSS there is only background-origin, TODO: implement it
        BackPivot,
        [WidgetXMLParameter("back_padding", typeof(Margin))]
        BackPadding,
        [WidgetCSSParameter("background-color-opacity", typeof(float))] // Panorama UI compat
        [WidgetXMLParameter("back_opacity", typeof(float))]
        BackOpacity,

        // Text

        [WidgetCSSParameter("font-family", typeof(string), WidgetParameterInheritance.Inherit)]
        [WidgetXMLParameter("font", typeof(Font), WidgetParameterInheritance.Inherit)]
        Font,
        [WidgetCSSParameter("font-size", typeof(float), WidgetParameterInheritance.Inherit)]
        [WidgetXMLParameter("font_size", typeof(float), WidgetParameterInheritance.Inherit)]
        FontSize,
        [WidgetCSSParameter("color", typeof(uint), WidgetParameterInheritance.Inherit)]
        [WidgetXMLParameter("text_color", typeof(uint), WidgetParameterInheritance.Inherit)]
        TextColor,
        [WidgetCSSParameter("line-height", typeof(float), WidgetParameterInheritance.Inherit)]
        [WidgetXMLParameter("line_spacing", typeof(float), WidgetParameterInheritance.Inherit)]
        LineSpacing,
        [WidgetCSSParameter("text-align", typeof(WidgetAlign), WidgetParameterInheritance.Inherit)]
        [WidgetXMLParameter("text_align", typeof(WidgetAlign), WidgetParameterInheritance.Inherit)]
        TextAlign,
        [WidgetXMLParameter("text_padding", typeof(Margin))]
//        [WidgetParameter("padding", typeof(Margin))]
        TextPadding,
        [WidgetXMLParameter("richtext", typeof(bool))]
        RichText,

        // Image

        [WidgetXMLParameter("image")]
        Image,
        [WidgetXMLParameter("image_style", typeof(WidgetBackgroundStyle))]
        ImageStyle,
        [WidgetXMLParameter("image_angle", typeof(float))]
        ImageAngle,
        [WidgetXMLParameter("image_pivot", typeof(Vector2))]
        ImagePivot,
        [WidgetXMLParameter("image_padding", typeof(Margin))]
        ImagePadding,
        [WidgetXMLParameter("image_color", typeof(uint))]
        ImageColor,
        [WidgetXMLParameter("image_opacity", typeof(float))]
        ImageOpacity,

        // Text edit

        [WidgetXMLParameter("cursor_color", typeof(uint))]
        CursorColor,
        [WidgetXMLParameter("cursor_char")]
        CursorChar,
        [WidgetXMLParameter("mask_char")]
        MaskChar,


        // Button

        [WidgetXMLParameter("button_layout", typeof(WidgetButtonLayout))]
        ButtonLayout,
        [WidgetXMLParameter("button_text_style", typeof(string))]
        ButtonTextStyle,
        [WidgetXMLParameter("button_image_style", typeof(string))]
        ButtonImageStyle,
        [WidgetXMLParameter("button_image_padding", typeof(Margin))]
        ButtonImagePadding,
        [WidgetXMLParameter("button_text_padding", typeof(Margin))]
        ButtonTextPadding,
        [WidgetXMLParameter("button_animate_scale", typeof(float))]
        ButtonAnimateScale,
        [WidgetXMLParameter("button_animate_pivot", typeof(Vector2))]
        ButtonAnimatePivot,
        [WidgetXMLParameter("button_animate_time", typeof(int))]
        ButtonAnimateTime,

        Max = ButtonAnimateTime + 1
    }

    public enum WidgetType : int
    {
        [Name("*")]
        Widget = 0, // basic widget, does nothing
        [Name("background")]
        Background,
        [Name("button")]
        Button,
        [Name("panel")]
        Panel,
        [Name("image")]
        Image,
        [Name("checkbox")]
        CheckBox,
        [Name("label")]
        Label,
        [Name("label")]
        Text,
        [Name("line")]
        Line,
        [Name("scrollview")]
        ScrollView,
        [Name("textedit")]
        TextEdit,
        [Name("textedit")]
        TextField,
        [Name("toolbar")]
        Toolbar,
        [Name("tooltip")]
        tooltip,
    }

}

