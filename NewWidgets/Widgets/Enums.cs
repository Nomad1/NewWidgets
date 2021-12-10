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

        // 1st grade, can be only subset of Normal
        [WidgetPseudoClass("checked")] // :checked is used for checkboxes
        [WidgetPseudoClass("selected")] // :selected is for Panorama UI compatibility
        Selected = 0x01, // :checked , :selected

        //[WidgetPseudoClass("active")] // :active represents the state when the element is currently being activated by the user
        //[WidgetPseudoClass("focus")] // :focus represents the state when the element is currently selected to receive input
        //// in our case it's all the same
        //Focused = 0x02, // :active, :focus

        // 2nd grade, can be only subset of Normal or Selected
        [WidgetPseudoClass("disabled")]
        Disabled = 0x04, // :disabled
        SelectedDisabled = Selected | Disabled, // :selected:disabled

        // 3rd grade, can be only subset of Normal, Disabled, Selected or SelectedDisabled
        [WidgetPseudoClass("hover")]
        Hovered = 0x08, // :hover
        SelectedHovered = Selected | Hovered, // :hover:selected
        SelectedDisabledHovered = Selected | Disabled | Hovered, // :hover:selected:disabled
        DisabledHovered = Normal | Disabled | Hovered, // hover:disabled

        Max = 0x0F
    }

    // List of default parameters. We need to use it as Enum to get fast access instead of dictionary search
    internal enum WidgetParameterIndex
    {
        // Invalid
        None,
        // Common
        [WidgetParameter("size", typeof(Vector2))]
        Size,
        [WidgetParameter("clip", typeof(bool))]
        Clip,
        [WidgetParameter("clip_margin", typeof(Margin))]
        ClipMargin,
        [WidgetParameter("hovered_style", typeof(WidgetStyleSheet))]
        HoveredStyle,
        [WidgetParameter("disabled_style", typeof(WidgetStyleSheet))]
        DisabledStyle,
        [WidgetParameter("selected_style", typeof(WidgetStyleSheet))]
        SelectedStyle,
        [WidgetParameter("selected_hovered_style", typeof(WidgetStyleSheet))]
        SelectedHoveredStyle,

        // Background

        [WidgetParameter("back_style", typeof(WidgetBackgroundStyle))]
        BackStyle,
        [WidgetParameter("back_depth", typeof(WidgetBackgroundDepth))]
        BackDepth,
        [WidgetParameter("back_image")]
        BackImage,
        [WidgetParameter("back_scale", typeof(float))]
        BackScale,
        [WidgetParameter("back_angle", typeof(float))]
        BackAngle,
        [WidgetParameter("back_pivot", typeof(Vector2))]
        BackPivot,
        [WidgetParameter("back_padding", typeof(Margin))]
        BackPadding,
        [WidgetParameter("back_opacity", typeof(float))]
        BackOpacity,
        [WidgetParameter("back_color", typeof(uint))]
        BackColor,

        // Text

        [WidgetParameter("font", typeof(Font))]
        Font,
        [WidgetParameter("font_size", typeof(float))]
        FontSize,
        [WidgetParameter("text_color", typeof(uint))]
        TextColor,
        [WidgetParameter("line_spacing", typeof(float))]
        LineSpacing,
        [WidgetParameter("text_align", typeof(WidgetAlign))]
        TextAlign,
        [WidgetParameter("text_padding", typeof(Margin))]
        [WidgetParameter("padding", typeof(Margin))]
        TextPadding,
        [WidgetParameter("richtext", typeof(bool))]
        RichText,

        // Image

        [WidgetParameter("image")]
        Image,
        [WidgetParameter("image_style", typeof(WidgetBackgroundStyle))]
        ImageStyle,
        [WidgetParameter("image_angle", typeof(float))]
        ImageAngle,
        [WidgetParameter("image_pivot", typeof(Vector2))]
        ImagePivot,
        [WidgetParameter("image_padding", typeof(Margin))]
        ImagePadding,
        [WidgetParameter("image_color", typeof(uint))]
        ImageColor,
        [WidgetParameter("image_opacity", typeof(float))]
        ImageOpacity,

        // Text edit

        [WidgetParameter("cursor_color", typeof(uint))]
        CursorColor,
        [WidgetParameter("cursor_char")]
        CursorChar,
        [WidgetParameter("mask_char")]
        MaskChar,


        // Button

        [WidgetParameter("button_layout", typeof(WidgetButtonLayout))]
        ButtonLayout,
        [WidgetParameter("button_text_style", typeof(WidgetStyleSheet))]
        ButtonTextStyle,
        [WidgetParameter("button_image_style", typeof(WidgetStyleSheet))]
        ButtonImageStyle,
        [WidgetParameter("button_image_padding", typeof(Margin))]
        ButtonImagePadding,
        [WidgetParameter("button_text_padding", typeof(Margin))]
        ButtonTextPadding,
        [WidgetParameter("button_animate_scale", typeof(float))]
        ButtonAnimateScale,
        [WidgetParameter("button_animate_pivot", typeof(Vector2))]
        ButtonAnimatePivot,
        [WidgetParameter("button_animate_time", typeof(int))]
        ButtonAnimateTime,

        // Scroll view

        [WidgetParameter("horizontal_scroll", typeof(WidgetStyleSheet))]
        HorizontalScrollStyle,
        [WidgetParameter("vertical_scroll", typeof(WidgetStyleSheet))]
        VerticalcrollStyle,
        [WidgetParameter("horizontal_indicator", typeof(WidgetStyleSheet))]
        HorizontalIndicatorStyle,
        [WidgetParameter("vertical_indicator", typeof(WidgetStyleSheet))]
        VerticalIndicatorStyle,

        // Text field
        [WidgetParameter("scroll_style", typeof(WidgetStyleSheet))]
        TextFieldScrollStyle,

        Max = TextFieldScrollStyle + 1
    }

}

