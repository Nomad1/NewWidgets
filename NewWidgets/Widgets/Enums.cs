using System;
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
        NotTopLeft = Right | Bottom,
        Center = Left | Right | Top | Bottom
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

    public enum WidgetStyleClassIndex
    {
        None = 0,
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
        Tooltip,
    }

    /// <summary>
    /// Type of visibility for overflown content: overflow:hidden and overflow:visible
    /// </summary>
    public enum WidgetOverflow
    {
        Hidden = 1, // Clip = true means overflow:hidden
        Visible = 2, // Clip = false means overflow:visible
    }

    // Nomad: one day we'll migrate to these
    public enum StyleParameterType
    {
        None = 0,
        Integer = 1, //  An <integer> consists of one or more digits "0" to "9"
        Number = 2, // A <number> can either be an <integer>, or it can be zero or more digits followed by a dot (.) followed by one or more digits.
        Length = 3, // a <number> (with or without a decimal point) immediately followed by a unit identifier (e.g., px, em, etc.).
        Percentage = 4, // a <number> immediately followed by '%'.
        Uri = 5, // The format of a URI value is 'url(' followed by optional white space followed by an optional single quote (') or double quote (") character followed by the URI itself, followed by an optional single quote (') or double quote (") character followed by optional white space followed by ')'. The two quote characters must be the same.
        Counter = 6, // NYU
        Color = 7, // A <color> is either a keyword or a numerical RGB specification.
        String = 8, // Strings can either be written with double quotes or with single quotes
        Enum = 9, // special type for enumerations
    }

}

