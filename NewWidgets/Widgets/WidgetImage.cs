using System.Numerics;
using NewWidgets.UI;

namespace NewWidgets.Widgets
{
    public class WidgetImage : Widget
    {
        public string Image
        {
            get { return BackgroundTexture; }
            set { BackgroundTexture = value; }
        }

        public float ImageRotation
        {
            get { return BackgroundRotation; }
            set { BackgroundRotation = value; }
        }

        public Vector2 ImagePivot
        {
            get { return BackgroundPivot; }
            set { BackgroundPivot = value; }
        }

        public WidgetBackgroundStyle ImageStyle
        {
            get { return BackgroundStyle; }
            set { BackgroundStyle = value; }
        }

        public Vector2 ImageSize
        {
            get { return ImageObject.Sprite.Size; }
        }

        public ImageObject ImageObject
        {
            get
            {
                if (m_background.Count == 0)
                    PrepareBackground();

                return ((ImageObject)m_background[0]);
            }
        }

        public WidgetImage(string image)
            : this(WidgetBackgroundStyle.ImageFit, image)
        {
        }

        public WidgetImage(WidgetBackgroundStyle style, string image)
            : base(WidgetManager.DefaultWidgetStyle, true)
        {
            BackgroundTexture = image;
            BackgroundStyle = style;
        }

        public WidgetImage(WidgetStyleSheet style, string image)
            : base(style, true)
        {
            BackgroundTexture = image;
        }

        public void Relayout()
        {
            PrepareBackground();
            if (Size.X <= 0 && Size.Y <= 0 && BackgroundStyle == WidgetBackgroundStyle.Image)
                Size = ImageSize;
        }
    }
}

