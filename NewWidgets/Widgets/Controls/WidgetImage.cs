using System.Numerics;
using NewWidgets.UI;
using NewWidgets.Utility;

#if RUNMOBILE
using RunMobile.Utility;
#endif

namespace NewWidgets.Widgets
{
    public class WidgetImage : Widget
    {
        public new const string ElementType = "img";

        // internal component for image
        private ImageObject m_imageObject;

        // cached last texture name
        private string m_lastTexture;

        public override string ToString()
        {
            return string.Format("<{0}> #{1} {2}x{3} image={4} fit={5}", StyleElementType, StyleId, (int)Size.X, (int)Size.Y, Image, ImageStyle);
        }

        public string Image
        {
            get { return GetProperty(WidgetParameterIndex.BackImage, ""); }
            set { SetProperty(WidgetParameterIndex.BackImage, value); InvalidateLayout(); }
        }

        public float ImageRotation
        {
            get { return GetProperty(WidgetParameterIndex.BackAngle, 0.0f); }
            set { SetProperty(WidgetParameterIndex.BackAngle, value); InvalidateLayout(); }
        }

        public Vector2 ImagePivot
        {
            get { return GetProperty(WidgetParameterIndex.BackPivot, new Vector2(0.5f, 0.5f)); }
            set { SetProperty(WidgetParameterIndex.BackPivot, value); InvalidateLayout(); }
        }

        public Margin ImagePadding
        {
            get { return GetProperty(WidgetParameterIndex.BackPadding, Margin.Empty); }
            set { SetProperty(WidgetParameterIndex.BackPadding, value); InvalidateLayout(); }
        }

        public WidgetImageStyle ImageStyle
        {
            get { return GetProperty(WidgetParameterIndex.ObjectFit, WidgetImageStyle.ImageFit); }
            set { SetProperty(WidgetParameterIndex.ObjectFit, value); InvalidateLayout(); }
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
            get { return GetProperty(WidgetParameterIndex.BackColor, (uint)0xffffff); }
            set { SetProperty(WidgetParameterIndex.BackColor, value); }
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
            : this(ElementType, default(WidgetStyle), string.IsNullOrEmpty(image) ? 0 : WidgetImageStyle.ImageFit, image)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetImage"/> class.
        /// Unlike <see cref="T:NewWidgets.Widgets.WidgetBackground"/> it does not allows tiling
        /// </summary>
        /// <param name="imageStyle">Image style.</param>
        /// <param name="image">Image.</param>
        public WidgetImage(WidgetImageStyle imageStyle, string image = "")
            : this(ElementType, default(WidgetStyle), string.IsNullOrEmpty(image)? 0 : imageStyle, image)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetImage"/> class.
        /// Unlike <see cref="T:NewWidgets.Widgets.WidgetBackground"/> it does not allows tiling
        /// </summary>
        /// <param name="imageStyle">Image style.</param>
        /// <param name="image">Image.</param>
        public WidgetImage(WidgetStyle style = default(WidgetStyle), WidgetImageStyle imageStyle = 0, string image = "")
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
        internal WidgetImage(string elementType, WidgetStyle style, WidgetImageStyle imageStyle, string image)
            : base(elementType, style)
        {
            if (imageStyle != 0)
                ImageStyle = imageStyle;

            if (!string.IsNullOrEmpty(image))
                Image = image;
        }

        protected override void UpdateLayout()
        {
            if (m_imageObject != null && m_lastTexture != Image) // TODO: check if image was not changed meaning no need to remove it
            {
                m_imageObject.Remove();
                m_imageObject = null;
            }

            m_lastTexture = Image;

            if (string.IsNullOrEmpty(Image))
                return;

            if (m_imageObject == null)
            {
                ISprite textureSprite = WindowController.Instance.CreateSprite(ConversionHelper.UrlToSpriteName(Image));
                if (textureSprite == null)
                {
                    WindowController.Instance.LogError("WidgetImage texture not found for sprite {0}", textureSprite);
                    return;
                }

                m_imageObject = new ImageObject(this, textureSprite);
            }

            Vector2 spriteSize = m_imageObject.Sprite.Size;
            Vector2 size = Size.X <= 0 || Size.Y <= 0 ? spriteSize : (Size - ImagePadding.Size);
            Vector2 start = ImagePadding.TopLeft;
            Vector2 center = start + size / 2;

            // Pre-initialized rather than left for the switch to assign: WidgetImageStyle now
            // holds only cases this switch covers, so there is no default branch left to prove
            // that to the compiler.
            Vector2 position = Vector2.Zero;
            float scale = 1.0f;
            bool nonUniformScale = false;
            float scaleY = 1.0f;

            WidgetImageStyle style = ImageStyle;

            switch (style)
            {
                case WidgetImageStyle.ImageFit:
                case WidgetImageStyle.ImageTopLeft:
                    {
                        if (style == WidgetImageStyle.ImageTopLeft)
                            position = Vector2.Zero;
                        else
                            position = center;

                        // Center and aspect fit. Good only for fixed size windows
                        scale = size.X / spriteSize.X;

                        if (scale * spriteSize.Y > size.Y)
                            scale = size.Y / spriteSize.Y;

                        break;
                    }
                case WidgetImageStyle.ImageFill:
                case WidgetImageStyle.ImageTopLeftFill:
                    {
                        if (style == WidgetImageStyle.ImageTopLeftFill)
                            position = Vector2.Zero;
                        else
                            position = center;

                        // Center and aspect fill
                        scale = size.X / spriteSize.X;

                        if (scale * spriteSize.Y < size.Y)
                            scale = size.Y / spriteSize.Y;

                        break;
                    }
                case WidgetImageStyle.ImageStretch:
                    {
                        position = center;

                        // Center and stretch
                        scale = size.X / m_imageObject.Sprite.Size.X;
                        scaleY = size.Y / m_imageObject.Sprite.Size.Y;
                        nonUniformScale = true;
                        break;
                    }
                case WidgetImageStyle.Image:
                    {
                        position = start;
                        scale = 1.0f;
                        // Center and no stretch
                        break;
                    }
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

