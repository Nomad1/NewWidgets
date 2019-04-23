using System.Numerics;
using NewWidgets.UI;
using NewWidgets.Utility;
using NewWidgets.Widgets.Styles;

namespace NewWidgets.Widgets
{
    public class WidgetImage : Widget
    {
        public static readonly new WidgetStyleReference DefaultStyle = WidgetManager.RegisterDefaultStyle<WidgetImageStyleSheet>("default_image");

        private ImageObject m_imageObject;

        private bool m_imageInited;

        private WidgetImageStyleSheet Style
        {
            get { return m_style.Get<WidgetImageStyleSheet>(); }
        }

        private WidgetImageStyleSheet WritableStyle
        {
            get { return m_style.Get<WidgetImageStyleSheet>(this); }
        }
        public string Image
        {
            get { return Style.Image; }
            set { WritableStyle.Image = value; InvalidateImage(); }
        }

        public float ImageRotation
        {
            get { return Style.ImageRotation; }
            set { WritableStyle.ImageRotation = value; InvalidateImage(); }
        }

        public Vector2 ImagePivot
        {
            get { return Style.ImagePivot; }
            set { WritableStyle.ImagePivot = value; InvalidateImage(); }
        }

        public Margin ImagePadding
        {
            get { return Style.ImagePadding; }
            set { WritableStyle.ImagePadding = value; InvalidateImage(); }
        }

        public WidgetBackgroundStyle ImageStyle
        {
            get { return Style.ImageStyle; }
            set { WritableStyle.ImageStyle = value; InvalidateImage(); }
        }

        public float ImageAlpha
        {
            get { return Style.ImageOpacity; }
            set { WritableStyle.ImageOpacity = value; UpdateColor(); }
        }

        public int Color
        {
            get { return Style.ImageColor; }
            set { WritableStyle.ImageColor = value; UpdateColor(); }
        }

        public override float Alpha
        {
            get { return base.Alpha; }
            set { base.Alpha = value; UpdateColor(); }
        }

        public Vector2 ImageSize
        {
            get { return ImageObject.Sprite.Size; }
        }

        public ImageObject ImageObject
        {
            get { PrepareImage(); return m_imageObject; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetImage"/> class.
        /// Unlike <see cref="T:NewWidgets.Widgets.WidgetBackground"/> it does not allows tiling
        /// and always contain only one image
        /// </summary>
        /// <param name="image">Image.</param>
        public WidgetImage(string image = "")
            : this(default(WidgetStyleReference), WidgetBackgroundStyle.Image, image)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetImage"/> class.
        /// Unlike <see cref="T:NewWidgets.Widgets.WidgetBackground"/> it does not allows tiling
        /// </summary>
        /// <param name="imageStyle">Image style.</param>
        /// <param name="image">Image.</param>
        public WidgetImage(WidgetBackgroundStyle imageStyle = 0, string image = "")
            : this(default(WidgetStyleReference), imageStyle, image)
        {
           
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetImage"/> class.
        /// Unlike <see cref="T:NewWidgets.Widgets.WidgetBackground"/> it does not allows tiling
        /// </summary>
        /// <param name="style">Style.</param>
        /// <param name="imageStyle">Image style.</param>
        /// <param name="image">Image.</param>
        public WidgetImage(WidgetStyleReference style = default(WidgetStyleReference), WidgetBackgroundStyle imageStyle = 0, string image = "")
            : base(style.IsEmpty ? DefaultStyle : style)
        {
            ImageStyle = imageStyle == 0 ? Style.ImageStyle : imageStyle;
            Image = string.IsNullOrEmpty(image) ? Style.Image : image;
        }

        private void InvalidateImage()
        {
            m_imageInited = false;
        }

        protected void PrepareImage()
        {
            InitImage(ImageStyle, Image, ImageRotation, ImagePivot, ImagePadding);
        }

        protected void InitImage(WidgetBackgroundStyle style, string texture, float rotation, Vector2 pivot, Margin padding)
        {
            m_imageInited = true;

            if (m_imageObject != null) // TODO: check if image was not changed meaning no need to remove it
                m_imageObject.Remove();

            if (string.IsNullOrEmpty(texture))
            {
                if (style != WidgetBackgroundStyle.None)
                    WindowController.Instance.LogMessage("Initing WidgetImage {0} without texture", this);
                return;
            }

            ISprite textureSprite = WindowController.Instance.CreateSprite(texture, Vector2.Zero);
            if (textureSprite == null)
            {
                WindowController.Instance.LogError("WidgetImage texture not found for sprite {0}", textureSprite);
                return;
            }

            Vector2 size = new Vector2(Size.X - padding.Left - padding.Right, Size.Y - padding.Top - padding.Bottom);
            Vector2 start = new Vector2(padding.Left, padding.Top);
            Vector2 center = start + size / 2;

            switch (style)
            {
                case WidgetBackgroundStyle.ImageFit:
                case WidgetBackgroundStyle.ImageTopLeft:
                    {
                        ImageObject image = new ImageObject(this, textureSprite);

                        if (style == WidgetBackgroundStyle.ImageTopLeft)
                            image.Position = Vector2.Zero;
                        else
                            image.Position = center;

                        // Center and aspect fit. Good only for fixed size windows
                        image.Sprite.PivotShift = pivot;
                        image.Scale = size.X / image.Sprite.Size.X;
                        image.Rotation = rotation;

                        if (image.Scale * image.Sprite.Size.Y > size.Y)
                            image.Scale = size.Y / image.Sprite.Size.Y;

                        m_imageObject = image;
                        break;
                    }
                case WidgetBackgroundStyle.ImageStretch:
                    {
                        ImageObject image = new ImageObject(this, textureSprite);

                        image.Position = center;

                        // Center and stretch
                        image.Sprite.PivotShift = pivot;
                        image.Transform.FlatScale = size / image.Sprite.Size;
                        image.Rotation = rotation;

                        m_imageObject = image;
                        break;
                    }
                case WidgetBackgroundStyle.Image:
                    {
                        ImageObject image = new ImageObject(this, textureSprite);
                        image.Position = start;

                        // Center and no stretch
                        image.Sprite.PivotShift = pivot;
                        image.Rotation = rotation;

                        m_imageObject = image;
                        break;
                    }
                default:
                    WindowController.Instance.LogError("Invalid background style {0} specified for WidgetImage", style);
                    return;
            }

            if (Size.X <= 0 && Size.Y <= 0)
                Size = size;

            UpdateColor();
        }

        protected override void Resize(Vector2 size)
        {
            if (Vector2.DistanceSquared(Size, size) > float.Epsilon)
            {
                base.Resize(size);

                InvalidateImage();
            }
        }

        public void Relayout()
        {
            InvalidateImage();
            if (Size.X <= 0 && Size.Y <= 0 && ImageStyle == WidgetBackgroundStyle.Image)
                Size = ImageSize;
        }

        private void UpdateColor()
        {
            if (m_imageObject != null)
            {
                m_imageObject.Sprite.Color = Color;
                m_imageObject.Sprite.Alpha = MathHelper.Clamp((int)(Alpha * ImageAlpha * 255 + float.Epsilon), 0, 255);
            }
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;

            if (!m_imageInited)
                PrepareImage();

            if (m_imageObject != null)
                m_imageObject.Update();

            return true;
        }

        protected override void DrawContents(object canvas)
        {
            if (m_imageObject != null)
                m_imageObject.Draw(canvas);
            base.DrawContents(canvas);
        }
    }
}

