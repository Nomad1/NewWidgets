using System;

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
		/// Divide image to 33/33/33 percents both vertically and horizontally and scale central parts on corresponding axises
		/// Note that BackgroundPivot defaults to 0.5;0.5 meaning that tiling starts at the center, but actually
		/// first tiling image is at the top-left corner so BackgroundPivot position is shifted by 0.5;0.5
		/// </summary>
        NineImage = 6,
		/// <summary>
		/// Image is divided to 33/33/33 percents horizontally
		/// </summary>
		ThreeImage = 7
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
        None = 0x0,
        Normal = 0x01,
        Visible = 0x02,
        Inertial = 0x04,
        AutoHide = 0x08
    }

}

