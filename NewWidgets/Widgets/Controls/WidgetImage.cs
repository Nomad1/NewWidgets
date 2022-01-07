using System.Numerics;
using NewWidgets.UI;
using NewWidgets.UI.Styles;
using NewWidgets.Utility;

#if RUNMOBILE
using RunMobile.Utility;
#endif

namespace NewWidgets.Widgets
{
    public class WidgetImage : Widget
    {
        public new const string ElementType = "image";

        // internal component for image
        private ImageObject m_imageObject;

        // cached last texture name
        private string m_lastTexture;

        public string Image
        {
            get { return GetProperty(WidgetParameterIndex.Image, ""); }
            set { SetProperty(WidgetParameterIndex.Image, value); InvalidateLayout(); }
        }

        public float ImageRotation
        {
            get { return GetProperty(WidgetParameterIndex.ImageAngle, 0.0f); }
            set { SetProperty(WidgetParameterIndex.ImageAngle, value); InvalidateLayout(); }
        }

        public Vector2 ImagePivot
        {
            get { return GetProperty(WidgetParameterIndex.ImagePivot, new Vector2(0.5f, 0.5f)); }
            set { SetProperty(WidgetParameterIndex.ImagePivot, value); InvalidateLayout(); }
        }

        public Margin ImagePadding
        {
            get { return GetProperty(WidgetParameterIndex.ImagePadding, new Margin(0)); }
            set { SetProperty(WidgetParameterIndex.ImagePadding, value); InvalidateLayout(); }
        }

        public WidgetBackgroundStyle ImageStyle
        {
            get { return GetProperty(WidgetParameterIndex.ImageStyle, WidgetBackgroundStyle.ImageFit); }
            set { SetProperty(WidgetParameterIndex.ImageStyle, value); InvalidateLayout(); }
        }

        //public float ImageAlpha
        //{
        //    get { return GetProperty(WidgetParameterIndex.ImageOpacity, 1.0f); }
        //    set
        //    {
        //        if (ImageAlpha != value)
        //        {
        //            SetProperty(WidgetParameterIndex.ImageOpacity, value);
        //            UpdateColor();
        //        }
        //    }
        //}

        public uint Color
        {
            get { return GetProperty(WidgetParameterIndex.ImageColor, (uint)0xffffff); }
            set { SetProperty(WidgetParameterIndex.ImageColor, value); }
        }

        public Vector2 ImageSize
        {
            get { return ImageObject.Sprite.Size; }
        }

        public ImageObject ImageObject
        {
            get
            {
                if (m_imageObject == null)
                    UpdateLayout(); // this call creates the image ahead of time. Try not to abuse it
                /*PrepareImage();*/
                return m_imageObject;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetImage"/> class.
        /// Unlike <see cref="T:NewWidgets.Widgets.WidgetBackground"/> it does not allows tiling
        /// and always contain only one image
        /// </summary>
        /// <param name="image">Image.</param>
        public WidgetImage(string image)
            : this(ElementType, default(WidgetStyle), string.IsNullOrEmpty(image) ? 0 : WidgetBackgroundStyle.ImageFit, image)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetImage"/> class.
        /// Unlike <see cref="T:NewWidgets.Widgets.WidgetBackground"/> it does not allows tiling
        /// </summary>
        /// <param name="imageStyle">Image style.</param>
        /// <param name="image">Image.</param>
        public WidgetImage(WidgetBackgroundStyle imageStyle, string image)
            : this(ElementType, default(WidgetStyle), string.IsNullOrEmpty(image)? 0 : imageStyle, image)
        {
           
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetImage"/> class.
        /// Unlike <see cref="T:NewWidgets.Widgets.WidgetBackground"/> it does not allows tiling
        /// </summary>
        /// <param name="imageStyle">Image style.</param>
        /// <param name="image">Image.</param>
        public WidgetImage(WidgetStyle style = default(WidgetStyle), WidgetBackgroundStyle imageStyle = 0, string image = "")
            : this(ElementType, style, string.IsNullOrEmpty(image) ? 0 : imageStyle, image)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetImage"/> class.
        /// Unlike <see cref="T:NewWidgets.Widgets.WidgetBackground"/> it does not allows tiling
        /// </summary>
        /// <param name="style">Style.</param>
        /// <param name="imageStyle">Image style.</param>
        /// <param name="image">Image.</param>
        protected WidgetImage(string elementType, WidgetStyle style, WidgetBackgroundStyle imageStyle, string image)
            : base(elementType, style)
        {
            if (imageStyle != 0)
                ImageStyle = imageStyle;

            if (!string.IsNullOrEmpty(image))
                Image = image;
        }

        public override void UpdateLayout()
        {
            if (m_imageObject != null && m_lastTexture != Image) // TODO: check if image was not changed meaning no need to remove it
            {
                m_imageObject.Remove();
                m_imageObject = null;
            }

            m_lastTexture = Image;

            if (string.IsNullOrEmpty(Image))
            {
                if (ImageStyle != WidgetBackgroundStyle.None)
                    WindowController.Instance.LogMessage("Initing WidgetImage {0} without texture", this);
                return;
            }

            if (m_imageObject == null)
            {
                ISprite textureSprite = WindowController.Instance.CreateSprite(Image);
                if (textureSprite == null)
                {
                    WindowController.Instance.LogError("WidgetImage texture not found for sprite {0}", textureSprite);
                    return;
                }

                m_imageObject = new ImageObject(this, textureSprite);
            }

            Vector2 size = Size - ImagePadding.Size;
            Vector2 spriteSize = m_imageObject.Sprite.Size;
            Vector2 start = ImagePadding.TopLeft;
            Vector2 center = start + size / 2;

            Vector2 position;
            float scale;
            bool nonUniformScale = false;
            float scaleY = 1.0f;

            WidgetBackgroundStyle style = ImageStyle;

            switch (style)
            {
                case WidgetBackgroundStyle.ImageFit:
                case WidgetBackgroundStyle.ImageTopLeft:
                    {
                        if (style == WidgetBackgroundStyle.ImageTopLeft)
                            position = Vector2.Zero;
                        else
                            position = center;

                        // Center and aspect fit. Good only for fixed size windows
                        scale = size.X / spriteSize.X;

                        if (scale * spriteSize.Y > size.Y)
                            scale = size.Y / spriteSize.Y;

                        break;
                    }
                case WidgetBackgroundStyle.ImageFill:
                case WidgetBackgroundStyle.ImageTopLeftFill:
                    {
                        if (style == WidgetBackgroundStyle.ImageTopLeftFill)
                            position = Vector2.Zero;
                        else
                            position = center;

                        // Center and aspect fill
                        scale = size.X / spriteSize.X;

                        if (scale * spriteSize.Y < size.Y)
                            scale = size.Y / spriteSize.Y;

                        break;
                    }
                case WidgetBackgroundStyle.ImageStretch:
                    {
                        position = center;

                        // Center and stretch
                        scale = size.X / m_imageObject.Sprite.Size.X;
                        scaleY = size.Y / m_imageObject.Sprite.Size.Y;
                        nonUniformScale = true;
                        break;
                    }
                case WidgetBackgroundStyle.Image:
                    {
                        position = start;
                        scale = 1.0f;
                        // Center and no stretch
                        break;
                    }
                default:
                    WindowController.Instance.LogError("Invalid background style {0} specified for WidgetImage", style);
                    m_imageObject.Remove();
                    m_imageObject = null;
                    return;
            }

            m_imageObject.Sprite.PivotShift = ImagePivot;
            m_imageObject.Transform.FlatScale = new Vector2(scale, nonUniformScale ? scaleY : scale);
            m_imageObject.Position = position;
            m_imageObject.Rotation = ImageRotation;


            // TODO: here we're autosizing the widget to fit the image, but there whould be an option to choose between sizing and overflow modes
            // also rotation is not counted for new size
            if (Size.X <= 0 && Size.Y <= 0)
                Size = size;

            base.UpdateLayout();
        }

        //private void UpdateColor()
        //{
        //    if (m_imageObject != null)
        //    {
        //        m_imageObject.Sprite.Color = Color;
        //        m_imageObject.Sprite.Alpha = (byte)MathHelper.Clamp((int)(Opacity * ImageAlpha * 255 + float.Epsilon), 0, 255);
        //    }
        //}

        public override bool Update()
        {
            if (!base.Update())
                return false;

            if (m_imageObject != null)
            {
                m_imageObject.Sprite.Color = Color;
                m_imageObject.Sprite.Alpha = (byte)MathHelper.Clamp((int)(OpacityValue * /*ImageAlpha **/ 255 + float.Epsilon), 0, 255);

                m_imageObject.Update();
            }

            return true;
        }

        protected override void DrawContents()
        {
            if (m_imageObject != null)
                m_imageObject.Draw();

            base.DrawContents();
        }
    }
}

