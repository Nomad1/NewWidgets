using System;
using System.Numerics;
using NewWidgets.UI;
using NewWidgets.UI.Styles;
using NewWidgets.Utility;

#if RUNMOBILE
using RunMobile.Utility;
#else
using System.Drawing;
#endif

namespace NewWidgets.Widgets
{
    /// <summary>
    /// Base class for widgets that has some kind of repeatable background, i.e. Panel
    /// It also could be used by itself to draw something
    /// </summary>
    public class WidgetBackground : Widget
    {
        public new const string ElementType = "background";
        //

        protected readonly WindowObjectArray<WindowObject> m_background;

        private bool m_backgroundInited;

        public string BackgroundTexture
        {
            get { return GetProperty(WidgetParameterIndex.BackImage, ""); }
            set { SetProperty(WidgetParameterIndex.BackImage, value); InvalidateBackground(); }
        }

        public WidgetBackgroundStyle BackgroundStyle
        {
            get { return GetProperty(WidgetParameterIndex.BackStyle, WidgetBackgroundStyle.None); }
            set { SetProperty(WidgetParameterIndex.BackStyle, value); InvalidateBackground(); }
        }

        public WidgetBackgroundDepth BackgroundDepth
        {
            get { return GetProperty(WidgetParameterIndex.BackDepth, WidgetBackgroundDepth.Back); }
            set { SetProperty(WidgetParameterIndex.BackDepth, value); } // no need to redraw background there
        }

        public float BackgroundScale
        {
            get { return GetProperty(WidgetParameterIndex.BackScale, 1.0f); }
            set { SetProperty(WidgetParameterIndex.BackScale, value); InvalidateBackground(); }
        }

        public float BackgroundRotation
        {
            get { return GetProperty(WidgetParameterIndex.BackAngle, 0.0f); }
            set { SetProperty(WidgetParameterIndex.BackAngle, value); InvalidateBackground(); }
        }

        public float BackgroundAlpha
        {
            get { return GetProperty(WidgetParameterIndex.BackOpacity, 1.0f); }
            set { SetProperty(WidgetParameterIndex.BackOpacity, value); } // no need to redraw background there
        }

        public Vector2 BackgroundPivot
        {
            get { return GetProperty(WidgetParameterIndex.BackPivot, new Vector2(0.5f, 0.5f)); }
            set { SetProperty(WidgetParameterIndex.BackPivot, value); InvalidateBackground(); }
        }

        public Margin BackgroundPadding
        {
            get { return GetProperty(WidgetParameterIndex.BackPadding, new Margin(0.0f)); }
            set { SetProperty(WidgetParameterIndex.BackPadding, value); InvalidateBackground(); }
        }

        public uint BackgroundColor
        {
            get { return GetProperty(WidgetParameterIndex.BackColor, (uint)0xffffff); }
            set { SetProperty(WidgetParameterIndex.BackColor, value); } // no need to redraw background there
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetBackground"/> class.
        /// </summary>
        /// <param name="style">Style.</param>
        public WidgetBackground(WidgetStyle style = default(WidgetStyle))
            : base(ElementType, style)
        {
            m_background = new WindowObjectArray<WindowObject>();
        }

        /// <summary>
        /// Protected constructor for inheritance
        /// </summary>
        /// <param name="elementType"></param>
        /// <param name="style"></param>
        internal WidgetBackground(string elementType, WidgetStyle style)
            : base(elementType, style)
        {
            m_background = new WindowObjectArray<WindowObject>();
        }

        protected override void UpdateStyle()
        {
            base.UpdateStyle();

            InvalidateBackground();
        }

        protected void UpdateBackground()
        {
            InitBackground(BackgroundStyle, BackgroundTexture, BackgroundScale, BackgroundRotation, BackgroundPivot, BackgroundPadding);
        }

        protected void InvalidateBackground()
        {
            m_backgroundInited = false;
        }

        protected void InitBackground(WidgetBackgroundStyle style, string texture, float scale, float rotation, Vector2 backgroundPivot, Margin backgroundPadding)
        {
            m_backgroundInited = true;

            // background-image is stored as it was authored, so a D186 reference still carries
            // its atlas file here; everything below wants the sprite name the fragment holds
            texture = ConversionHelper.UrlToSpriteName(texture);

            foreach (WindowObject obj in m_background.List)
                obj.Remove();

            m_background.Clear();

            if (string.IsNullOrEmpty(texture))
            {
                if (style != WidgetBackgroundStyle.None)
                    WindowController.Instance.LogMessage("Initing Widget {0} without texture", this);
                return;
            }

            ISprite textureSprite = WindowController.Instance.CreateSprite(texture);
            if (textureSprite == null)
            {
                WindowController.Instance.LogError("Widget texture not found for sprite {0}", textureSprite);
                return;
            }

            Vector2 backSize = new Vector2(Size.X - backgroundPadding.Left - backgroundPadding.Right, Size.Y - backgroundPadding.Top - backgroundPadding.Bottom);
            Vector2 backStart = new Vector2(backgroundPadding.Left, backgroundPadding.Top);
            Vector2 backCenter = backStart + backSize / 2;

            switch (style)
            {
                case WidgetBackgroundStyle.ImageFit:
                case WidgetBackgroundStyle.ImageTopLeft:
                    {
                        ImageObject background = new ImageObject(this, textureSprite);

                        if (style == WidgetBackgroundStyle.ImageTopLeft)
                            background.Position = Vector2.Zero;
                        else
                            background.Position = backCenter;

                        // Center and aspect fit. Good only for fixed size windows
                        background.Sprite.PivotShift = backgroundPivot;
                        background.Scale = backSize.X / background.Sprite.Size.X;
                        background.Rotation = rotation;

                        if (background.Scale * background.Sprite.Size.Y > backSize.Y)
                            background.Scale = backSize.Y / background.Sprite.Size.Y;

                        m_background.Add(background);
                        break;
                    }
                case WidgetBackgroundStyle.ImageFill:
                case WidgetBackgroundStyle.ImageTopLeftFill:
                    {
                        ImageObject background = new ImageObject(this, textureSprite);

                        if (style == WidgetBackgroundStyle.ImageTopLeftFill)
                            background.Position = Vector2.Zero;
                        else
                            background.Position = backCenter;

                        background.Position = backCenter;

                        // Center and aspect fill
                        background.Sprite.PivotShift = backgroundPivot;
                        background.Scale = backSize.X / background.Sprite.Size.X;
                        background.Rotation = rotation;

                        if (background.Scale * background.Sprite.Size.Y < backSize.Y)
                            background.Scale = backSize.Y / background.Sprite.Size.Y;

                        m_background.Add(background);
                        break;
                    }
                case WidgetBackgroundStyle.ImageStretch:
                    {
                        ImageObject background = new ImageObject(this, textureSprite);

                        background.Position = backCenter;

                        // Center and stretch
                        background.Sprite.PivotShift = backgroundPivot;
                        background.Transform.FlatScale = backSize / background.Sprite.Size;
                        background.Rotation = rotation;

                        m_background.Add(background);
                        break;
                    }
                case WidgetBackgroundStyle.ImageTiled:
                    {
                        ImageObject background = new ImageObject(this, textureSprite);

                        Vector2 size = background.Sprite.Size * scale;

                        // Tile unstretched
                        int countX = (int)Math.Ceiling(backSize.X / size.X);
                        int countY = (int)Math.Ceiling(backSize.Y / size.Y);

                        for (int x = 0; x < countX; x++)
                            for (int y = 0; y < countY; y++)
                            {
                                ImageObject image;
                                if (x == 0 && y == 0)
                                    image = background;
                                else
                                {
                                    image = new ImageObject(this, WindowController.Instance.CreateSprite(texture));
                                    image.Transform.FlatPosition = size * new Vector2(x, y);
                                }

                                image.Scale = scale;
                                m_background.Add(image);
                            }
                        break;
                    }
                case WidgetBackgroundStyle.Image:
                    {
                        ImageObject background = new ImageObject(this, textureSprite);
                        background.Position = backStart;

                        // Center and no stretch
                        background.Sprite.PivotShift = backgroundPivot;
                        background.Rotation = rotation;

                        m_background.Add(background);
                        break;
                    }
                case WidgetBackgroundStyle.NineImage:
                    {
                        // CSS border-image-slice, when the style declares one, says where the
                        // corners are. Without it the sprite is cut in thirds below, which is
                        // what every style that predates the property expects.
                        //
                        // A slice at thirds is what the loop below already draws, from the 3x3
                        // cut the stylesheet scan registered once for the whole sprite. Sending
                        // it here instead would cut the same sprite again, per widget, through a
                        // seam no shipping host implements. So thirds stays on the path the two
                        // games render with, and only a slice this branch cannot express goes
                        // to the arbitrary cut.
                        //
                        // That arbitrary path is dormant: no shipping stylesheet declares a
                        // slice that is not thirds, so it is reached only from the conformance
                        // fixtures. See InitBorderImageBackground for what waking it needs.
                        Margin slice = GetProperty(WidgetParameterIndex.BorderImageSlice, Margin.Empty);

                        int tileX;
                        int tileY;

                        if (!Margin.IsEmpty(slice) && !WidgetManager.TryGetBorderImageGrid(slice, out tileX, out tileY)
                            && InitBorderImageBackground(texture, textureSprite, slice, scale, backgroundPivot))
                            break;

                        Vector2 shift = (-new Vector2(0.5f, 0.5f) + backgroundPivot) * Size;

                        float x = 0;
                        float y = 0;

                        Vector2[] scales = new Vector2[9];
                        Vector2[] positions = new Vector2[9];
                        bool inited = false;

                        for (int i = 0; i < 9; i++)
                        {
                            if (i % 3 == 0)
                                x = 0;

                            ISprite sprite = WindowController.Instance.CreateSprite(texture);
                            sprite.Transform.FlatPosition = new Vector2(x, y);
                            sprite.Frame = i;

                            if (!inited)
                            {
                                inited = true;
                                backSize = sprite.Size * scale;

                                // TODO: right now all sizes are measured as 1/3 of image width or heigth. We need to think of using different sizes to make it flexible

                                float width = (backSize.X / 3);
                                float height = (backSize.Y / 3);

                                float xscale = (Size.X - width * 2) / width;
                                float yscale = (Size.Y - height * 2) / height;

                                scales = new []
                                {
                                    new Vector2(1.0f, 1.0f),
                                    new Vector2(xscale, 1.0f),
                                    new Vector2(1.0f, 1.0f),
                                    new Vector2(1.0f, yscale),
                                    new Vector2(xscale, yscale),
                                    new Vector2(1.0f, yscale),
                                    new Vector2(1.0f, 1.0f),
                                    new Vector2(xscale, 1.0f),
                                    new Vector2(1.0f, 1.0f)
                                };

                                positions = new []
                                {
                                    new Vector2(0, 0),
                                    new Vector2(width, 0),
                                    new Vector2(Size.X - width, 0),
                                    new Vector2(0, height),
                                    new Vector2(width, height),
                                    new Vector2(Size.X - width, height),
                                    new Vector2(0, Size.Y - height),
                                    new Vector2(width, Size.Y - height),
                                    new Vector2(Size.X - width, Size.Y - height)
                                };
                            }

                            ImageObject background = new ImageObject(this, sprite);
                            background.Position = shift + positions[i];
                            background.Transform.FlatScale = scales[i] * scale;

                            m_background.Add(background);
                        }
                        break;
                    }
                case WidgetBackgroundStyle.ThreeImage:
                    {
                        Vector2 shift = (-new Vector2(0.5f, 0.5f) + backgroundPivot) * Size;

                        float x = 0;
                        float y = 0;

                        Vector2[] scales = null;
                        Vector2[] positions = null;

                        for (int i = 0; i < 3; i++)
                        {
                            ImageObject background = new ImageObject(this, WindowController.Instance.CreateSprite(texture));
                            background.Transform.FlatPosition = new Vector2(x, y);
                            background.Sprite.Frame = i;

                            if (scales == null)
                            {
                                scale = Size.Y / background.Sprite.Size.Y;

                                backSize = background.Sprite.Size * scale;

                                // TODO: right now all sizes are measured as 1/3 of image width or heigth. We need to think of using different sizes to make it flexible

                                float width = (backSize.X / 3);

                                scales = new[]
                                {
                                    new Vector2(1.0f, 1.0f),
                                    new Vector2((Size.X - width * 2) / width, 1.0f),
                                    new Vector2(1.0f, 1.0f),
                                };

                                positions = new[]
                                {
                                    new Vector2(0, 0),
                                    new Vector2(width, 0),
                                    new Vector2(Size.X - width, 0)
                                };
                            }

                            background.Position = shift + positions[i];
                            background.Transform.FlatScale = scales[i] * scale;

                            m_background.Add(background);
                            background.Parent = this;
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Nine-patch cut at an arbitrary <c>border-image-slice</c> rather than at thirds.
        /// The nine source cells are registered with the host as one sprite of nine frames,
        /// then laid out so the corners keep their source size, the edges stretch on one axis
        /// and the centre on both. Per CSS the centre is drawn only with the <c>fill</c>
        /// keyword. Edges stretch and never repeat, which is D139.
        ///
        /// Dormant, and deliberately so. What it does is the TODO the original author left in
        /// the thirds path a few lines above: cut the nine-patch where the style says the
        /// corners are instead of always at one third of the source. Every shipping stylesheet
        /// now declares <c>border-image-slice</c>, but always at thirds, which the caller sends
        /// to the uniform path -- so the fallback still runs everywhere and only the
        /// conformance fixtures reach this method. The owner decided the arbitrary slice is not
        /// needed yet and asked for it to be left in place, finished, rather than reverted.
        ///
        /// Three things stand between it and real use:
        ///
        /// 1. No host implements the rectangle-based overload of
        /// <c>WindowController.SetSpriteSubdivision(sourceId, targetId, RectangleF[] parts)</c>.
        /// The base class declines it and returns false, so outside the test controller this
        /// method always fails at that call and the caller falls back to thirds.
        ///
        /// 2. RunMobile's <c>SpriteManager.RegisterSpriteSubdivision(sourceId, sourceFrame,
        /// targetId, RectangleF[] parts)</c> computes each part against the image's dimensions
        /// but never adds the frame's own X and Y offset, unlike its uniform-grid sibling which
        /// does add them. It therefore cuts correctly only when the source sits at the atlas
        /// origin, and a nine-patch packed anywhere else would sample the wrong texels. That is
        /// one line each for X and Y, in RunMobile, and it must be fixed before this path can
        /// work from a real atlas.
        ///
        /// 3. The number-versus-percentage distinction in <c>ResolveSlice</c> below is a
        /// magnitude heuristic, marked there as such. The real fix is boxing the property in a
        /// <c>StyleLength</c> the way <c>width</c> and <c>left</c> already are.
        /// </summary>
        /// <returns><c>false</c> if the host cannot cut a sprite at arbitrary offsets, in
        /// which case nothing was added and the caller falls back to thirds.</returns>
        private bool InitBorderImageBackground(string texture, ISprite textureSprite, Margin slice, float scale, Vector2 backgroundPivot)
        {
            Vector2 sourceSize = textureSprite.Size;

            if (sourceSize.X <= 0 || sourceSize.Y <= 0)
                return false;

            float sliceLeft = ResolveSlice(slice.Left, sourceSize.X);
            float sliceRight = ResolveSlice(slice.Right, sourceSize.X);
            float sliceTop = ResolveSlice(slice.Top, sourceSize.Y);
            float sliceBottom = ResolveSlice(slice.Bottom, sourceSize.Y);

            // three source columns and three source rows in source pixels: the two slices and
            // whatever is left between them
            float[] sourceColumnStart = new float[] { 0, sliceLeft, sourceSize.X - sliceRight };
            float[] sourceColumnWidth = new float[] { sliceLeft, sourceSize.X - sliceLeft - sliceRight, sliceRight };
            float[] sourceRowStart = new float[] { 0, sliceTop, sourceSize.Y - sliceBottom };
            float[] sourceRowHeight = new float[] { sliceTop, sourceSize.Y - sliceTop - sliceBottom, sliceBottom };

            // the host wants normalized rectangles, so every cell is divided by the source size
            RectangleF[] parts = new RectangleF[9];

            for (int row = 0; row < 3; row++)
                for (int column = 0; column < 3; column++)
                    parts[row * 3 + column] = new RectangleF(
                        sourceColumnStart[column] / sourceSize.X,
                        sourceRowStart[row] / sourceSize.Y,
                        sourceColumnWidth[column] / sourceSize.X,
                        sourceRowHeight[row] / sourceSize.Y);

            // the cut sprite is named after the source and the slice, so two widgets sharing
            // both share one registration and a third slice of the same texture gets its own
            string targetId = string.Format("{0}|border-image|{1}_{2}_{3}_{4}", texture, sliceLeft, sliceTop, sliceRight, sliceBottom);

            if (!WindowController.Instance.SetSpriteSubdivision(texture, targetId, parts))
                return false;

            bool fill = GetProperty(WidgetParameterIndex.BorderImageFill, false);

            Vector2 shift = (-new Vector2(0.5f, 0.5f) + backgroundPivot) * Size;

            // the same three columns and rows in destination pixels: corners keep their source
            // size times the background scale, and the middle takes everything left over
            float[] destinationColumnStart = new float[] { 0, sliceLeft * scale, Size.X - sliceRight * scale };
            float[] destinationColumnWidth = new float[] { sliceLeft * scale, Size.X - (sliceLeft + sliceRight) * scale, sliceRight * scale };
            float[] destinationRowStart = new float[] { 0, sliceTop * scale, Size.Y - sliceBottom * scale };
            float[] destinationRowHeight = new float[] { sliceTop * scale, Size.Y - (sliceTop + sliceBottom) * scale, sliceBottom * scale };

            // one image per cell, skipping the ones a degenerate slice leaves empty: a slice of
            // "0 20" has no top and bottom row at all and draws as a three-patch
            for (int row = 0; row < 3; row++)
                for (int column = 0; column < 3; column++)
                {
                    if (sourceColumnWidth[column] <= 0 || sourceRowHeight[row] <= 0)
                        continue;

                    bool isCenter = row == 1 && column == 1;

                    if (isCenter && !fill)
                        continue;

                    ISprite sprite = WindowController.Instance.CreateSprite(targetId);
                    sprite.Frame = row * 3 + column;

                    ImageObject background = new ImageObject(this, sprite);
                    background.Position = shift + new Vector2(destinationColumnStart[column], destinationRowStart[row]);
                    background.Transform.FlatScale = new Vector2(
                        destinationColumnWidth[column] / sourceColumnWidth[column],
                        destinationRowHeight[row] / sourceRowHeight[row]);

                    m_background.Add(background);
                }

            return true;
        }

        /// <summary>
        /// Turns one stored <c>border-image-slice</c> value into source pixels.
        ///
        /// ponytail: the four values live in a Margin of bare floats, and the parser has
        /// already turned a percentage into a 0..1 fraction, so "20" (twenty source pixels)
        /// and "20%" (a fifth of the source) arrive as the same type and are told apart here
        /// by size: 1 or less is a fraction, more than 1 is pixels. The ceiling is the two
        /// inputs that collide -- "100%" and a one pixel slice both arrive as 1 and are read
        /// as a fraction, and "150%", which CSS clamps to 100%, is read as 150 pixels. The
        /// upgrade path is to box border-image-slice in a StyleLength, as width and left
        /// already are, and read the real unit here instead of guessing at it.
        /// </summary>
        private static float ResolveSlice(float value, float sourceSize)
        {
            if (value <= 1.0f)
                return value * sourceSize;

            return value;
        }

        protected override void Resize(Vector2 size)
        {
            //if (Vector2.DistanceSquared(Size, size) > float.Epsilon)
            {
                base.Resize(size);

                InvalidateBackground();
            }
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;

            if (!m_backgroundInited)
                UpdateBackground();

            if (m_background.Count > 0)
            {
                int ialpha = MathHelper.Clamp((int)(OpacityValue * BackgroundAlpha * 255 + float.Epsilon), 0, 255); // I'm adding epsilon there to avoid 0.999999 rounding to 254

                WindowObject[] array = m_background.List;

                for (int i = 0; i < array.Length; i++)
                    if (array[i] != null && array[i].Visible && array[i] is ImageObject)
                    {
                        ((ImageObject)array[i]).Sprite.Alpha = (byte)ialpha;
                        ((ImageObject)array[i]).Sprite.Color = BackgroundColor;
                    }

                m_background.Update();
            }

            return true;
        }

        public sealed override void Draw()
        {
            if (!Visible)
                return;

            if (BackgroundDepth == WidgetBackgroundDepth.Back)
                m_background.Draw();

            WidgetOverflow clip = Overflow; // I'm caching this variable to make sure we apply clipping and then cancel it accordingly

            if (clip == WidgetOverflow.Hidden)
            {
                Vector2 clipTopLeft = this.Transform.GetScreenPoint(new Vector2(ClipMargin.Left, ClipMargin.Top));
                Vector2 clipBottomRight = this.Transform.GetScreenPoint(new Vector2(this.Size.X - ClipMargin.Right, this.Size.Y - ClipMargin.Bottom));

                WindowController.Instance.SetClipRect(
                    (int)Math.Floor(clipTopLeft.X),
                    (int)Math.Floor(clipTopLeft.Y),
                    (int)Math.Ceiling(clipBottomRight.X - clipTopLeft.X),
                    (int)Math.Ceiling(clipBottomRight.Y - clipTopLeft.Y));
            }

            if (BackgroundDepth == WidgetBackgroundDepth.BackClipped)
                m_background.Draw();

            DrawContents();

            if (BackgroundDepth == WidgetBackgroundDepth.TopClipped)
                m_background.Draw();

            if (clip == WidgetOverflow.Hidden)
            {
                WindowController.Instance.CancelClipRect();
            }

            if (BackgroundDepth == WidgetBackgroundDepth.Top)
                m_background.Draw();
        }

        public override void Remove()
        {
            foreach (WindowObject obj in m_background.List)
                obj.Remove();

            m_background.Clear();

            base.Remove();
        }
    }
}

