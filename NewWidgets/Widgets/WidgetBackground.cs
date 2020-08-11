using System;
using System.Numerics;
using NewWidgets.UI;
using NewWidgets.Utility;

#if RUNMOBILE
using RunMobile.Utility;
#endif

namespace NewWidgets.Widgets
{
    public class WidgetBackground : Widget
    {
        public static readonly new WidgetStyleSheet DefaultStyle = WidgetManager.GetStyle("default_background", true);

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
            get { return GetProperty(WidgetParameterIndex.BackPadding, new Margin(0)); }
            set { SetProperty(WidgetParameterIndex.BackPadding, value); InvalidateBackground(); }
        }

        public int BackgroundColor
        {
            get { return GetProperty(WidgetParameterIndex.BackColor, 0xffffff); }
            set { SetProperty(WidgetParameterIndex.BackColor, value); } // no need to redraw background there
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NewWidgets.Widgets.WidgetBackground"/> class.
        /// </summary>
        /// <param name="style">Style.</param>
        public WidgetBackground(WidgetStyleSheet style = default(WidgetStyleSheet))
            : base(style.IsEmpty ? DefaultStyle : style)
        {
            m_background = new WindowObjectArray<WindowObject>();
        }

        public override bool SwitchStyle(WidgetStyleType styleType)
        {
            if (base.SwitchStyle(styleType))
            {
                InvalidateBackground();
                return true;
            }
            return false;
        }

        protected virtual void PrepareBackground()
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

            foreach (WindowObject obj in m_background.List)
                obj.Remove();

            m_background.Clear();

            if (string.IsNullOrEmpty(texture))
            {
                if (style != WidgetBackgroundStyle.None)
                    WindowController.Instance.LogMessage("Initing Widget {0} without texture", this);
                return;
            }

            ISprite textureSprite = WindowController.Instance.CreateSprite(texture, Vector2.Zero);
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
                                    image = new ImageObject(this, WindowController.Instance.CreateSprite(texture, size * new Vector2(x, y)));

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

                            ISprite sprite = WindowController.Instance.CreateSprite(texture, new Vector2(x, y));
                            sprite.Frame = i;

                            if (!inited)
                            {
                                inited = true;
                                backSize = sprite.Size * scale;

                                // TODO: right now all sizes are measured as 1/3 of image width or heigth. We need to think of using different sizes to make it flexible

                                float width = (backSize.X / 3);
                                float height = (backSize.Y / 3);

                                scales = new Vector2[9];
                                scales[0] = scales[2] = scales[6] = scales[8] = new Vector2(1.0f, 1.0f);
                                scales[1] = scales[7] = new Vector2((Size.X - width * 2) / width, 1.0f);
                                scales[3] = scales[5] = new Vector2(1.0f, (Size.Y - height * 2) / height);
                                scales[4] = new Vector2(scales[1].X, scales[3].Y);

                                positions = new Vector2[9];
                                positions[0] = new Vector2(0, 0);
                                positions[1] = new Vector2(width, 0);
                                positions[2] = new Vector2(Size.X - width, 0);
                                positions[3] = new Vector2(0, height);
                                positions[4] = new Vector2(width, height);
                                positions[5] = new Vector2(Size.X - width, height);
                                positions[6] = new Vector2(0, Size.Y - height);
                                positions[7] = new Vector2(width, Size.Y - height);
                                positions[8] = new Vector2(Size.X - width, Size.Y - height);
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
                            ImageObject background = new ImageObject(this, WindowController.Instance.CreateSprite(texture, new Vector2(x, y)));
                            background.Sprite.Frame = i;

                            if (scales == null)
                            {
                                scale = Size.Y / background.Sprite.Size.Y;

                                backSize = background.Sprite.Size * scale;

                                // TODO: right now all sizes are measured as 1/3 of image width or heigth. We need to think of using different sizes to make it flexible

                                float width = (backSize.X / 3);

                                scales = new Vector2[3];
                                scales[0] = scales[2] = new Vector2(1.0f, 1.0f);
                                scales[1] = new Vector2((Size.X - width * 2) / width, 1.0f);

                                positions = new Vector2[9];
                                positions[0] = new Vector2(0, 0);
                                positions[1] = new Vector2(width, 0);
                                positions[2] = new Vector2(Size.X - width, 0);
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
                PrepareBackground();

            if (m_background.Count > 0)
            {
                int ialpha = MathHelper.Clamp((int)(Alpha * BackgroundAlpha * 255 + float.Epsilon), 0, 255); // I'm adding epsilon there to avoid 0.999999 rounding to 254

                WindowObject[] array = m_background.List;

                for (int i = array.Length - 1; i >= 0; i--)
                    if (array[i] != null && array[i].Visible && array[i] is ImageObject)
                    {
                        ((ImageObject)array[i]).Sprite.Alpha = ialpha;
                        ((ImageObject)array[i]).Sprite.Color = BackgroundColor;
                    }

                m_background.Update();
            }

            return true;
        }

        public override void Draw(object canvas)
        {
            if (!Visible)
                return;

            if (BackgroundStyle == WidgetBackgroundStyle.ImageStretch)
            {
                // do nothing
                LogConsole.WriteLine(LogLevel.CHATTER, "Back");
            }


            if (BackgroundDepth == WidgetBackgroundDepth.Back)
                m_background.Draw(canvas);

          
            if (ClipContents)
            {
                Vector2 actualScale = Transform.ActualScale;

                WindowController.Instance.SetClipRect(
                    (int)(this.Transform.ActualPosition.X + ClipMargin.Left * actualScale.X),
                    (int)(this.Transform.ActualPosition.Y + ClipMargin.Top * actualScale.Y),
                    (int)((this.Size.X - ClipMargin.Left - ClipMargin.Right) * actualScale.X + 0.5f),
                    (int)((this.Size.Y - ClipMargin.Top - ClipMargin.Bottom) * actualScale.Y + 0.5f));
            }

            if (BackgroundDepth == WidgetBackgroundDepth.BackClipped)
                m_background.Draw(canvas);

            DrawContents(canvas);

            if (BackgroundDepth == WidgetBackgroundDepth.TopClipped)
                m_background.Draw(canvas);

            if (ClipContents)
                WindowController.Instance.CancelClipRect();

            if (BackgroundDepth == WidgetBackgroundDepth.Top)
                m_background.Draw(canvas);
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

