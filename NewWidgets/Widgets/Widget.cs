using System;
using System.Numerics;
using NewWidgets.UI;
using NewWidgets.Utility;

namespace NewWidgets.Widgets
{
    public class Widget : WindowObject
    {
        public delegate bool TooltipDelegate(Widget sender, string text, Vector2 position);

        protected readonly WindowObjectArray m_background;

        private readonly WidgetStyleSheet m_style;

        private WidgetBackgroundStyle m_backgroundStyle;
        private WidgetBackgroundDepth m_backgroundDepth;

        private string m_backgroundTexture;
        private float m_backgroundScale;
        private float m_backgroundRotation;
        private Vector2 m_backgroundPivot;
        private Margin m_backgroundPadding;

        private bool m_backgroundInited;
        private bool m_clipContents;
        private Margin m_clipMargin;
        
        private int m_colorTint;
        private float m_alpha;
        private string m_tooltip;

        public WidgetStyleSheet Style
        {
            get { return m_style; }
        }
        
        protected string BackgroundTexture
        {
            get { return m_backgroundTexture; }
            set { m_backgroundTexture = value; m_backgroundInited = false; }
        }

        protected WidgetBackgroundStyle BackgroundStyle
        {
            get { return m_backgroundStyle; }
            set { m_backgroundStyle = value; m_backgroundInited = false; }
        }

        public WidgetBackgroundDepth BackgroundDepth
        {
            get { return m_backgroundDepth; }
            set { m_backgroundDepth = value; }
        }

        protected float BackgroundScale
        {
            get { return m_backgroundScale; }
            set { m_backgroundScale = value; m_backgroundInited = false; }
        }

        protected float BackgroundRotation
        {
            get { return m_backgroundRotation; }
            set { m_backgroundRotation = value; m_backgroundInited = false; }
        }

        protected Vector2 BackgroundPivot
        {
            get { return m_backgroundPivot; }
            set { m_backgroundPivot = value; m_backgroundInited = false; }
        }

        public Margin BackgroundPadding
        {
            get { return m_backgroundPadding; }
            set { m_backgroundPadding = value; m_backgroundInited = false; }
        }

        public bool ClipContents
        {
            get { return m_clipContents; }
            set { m_clipContents = value; }
        }

        public Margin ClipMargin
        {
            get { return m_clipMargin; }
            set { m_clipMargin = value; }
        }

        public string Tooltip
        {
            get { return m_tooltip; }
            set { m_tooltip = value; }
        }

        public virtual int Color
        {
            get { return m_colorTint; }
            set { m_colorTint = value; }
        }
        
        public virtual float Alpha
        {
            get { return m_alpha; }
            set { m_alpha = value; }
        }

        public event TooltipDelegate OnTooltip;

        public Widget(WidgetStyleSheet style)
            : base(null)
        {
            m_background = new WindowObjectArray();

            m_backgroundStyle = style.BackgroundStyle;
            m_backgroundTexture = style.BackgroundTexture;
            m_backgroundScale = style.BackgroundScale;
            m_backgroundPivot = style.BackgroundPivot;
            m_backgroundInited = false;
            m_backgroundDepth = style.BackgroundDepth;
            m_backgroundPadding = style.BackgroundPadding;
            m_colorTint = style.Color;
            m_alpha = style.Opacity;
            
            m_clipContents = style.Clip;
            m_clipMargin = style.ClipMargin;

            m_style = style;
        }

        protected virtual void ApplyStyle(WidgetStyleSheet style)
        {
            m_backgroundInited = false;
            
            m_backgroundStyle = style.BackgroundStyle;
            m_backgroundTexture = style.BackgroundTexture;
            m_backgroundScale = style.BackgroundScale;
            m_backgroundPadding = style.BackgroundPadding;
            m_colorTint = style.Color;
            m_clipContents = style.Clip;
            m_clipMargin = style.ClipMargin;
        }
        
        private void InitBackground(WidgetBackgroundStyle style, string texture, float scale, float rotation, Vector2 backgroundPivot)
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

            Vector2 backSize = new Vector2(Size.X - m_backgroundPadding.Left - m_backgroundPadding.Right, Size.Y - m_backgroundPadding.Top - m_backgroundPadding.Bottom);
            Vector2 backStart = new Vector2(m_backgroundPadding.Left, m_backgroundPadding.Top);
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
                                image = new ImageObject(this, WindowController.Instance.CreateSprite(texture, size * new Vector2(x,y)));

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
                                scale = Size.Y/background.Sprite.Size.Y;

                                backSize = background.Sprite.Size * scale;


                                // TODO: right now all sizes are measured as 1/3 of image width or heigth. We need to think of using different sizes to make it flexible

                                float width = (backSize.X / 3);
                                float height = backSize.Y;

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
            if ((Size - size).LengthSquared() > float.Epsilon)
            {
                base.Resize(size);

                m_backgroundInited = false;
            }
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;

            if (!m_backgroundInited)
            {
                InitBackground(m_backgroundStyle, m_backgroundTexture, m_backgroundScale, m_backgroundRotation, m_backgroundPivot);
            }

            if (m_background.Count > 0)
            {
                int ialpha = Math.Min(Math.Max(0, (int)(m_alpha * 255)), 255);

                WindowObject[] array = m_background.List;

                for (int i = array.Length - 1; i >= 0; i--)
                    if (array[i] != null && array[i].Visible && array[i] is ImageObject)
                {
                    ((ImageObject)array[i]).Sprite.Alpha = ialpha;
                    ((ImageObject)array[i]).Sprite.Color = m_colorTint;
                }
                
                m_background.Update();
            }

            return true;
        }

        public override void Draw(object canvas)
        {
            base.Draw(canvas);

            if (!Visible)
                return;

            if (m_backgroundDepth == WidgetBackgroundDepth.Back)
                m_background.Draw(canvas);

            if (m_clipContents)
            {
                Vector2 actualScale = Transform.ActualScale;

                WindowController.Instance.SetClipRect(
                    (int)(this.Transform.ActualPosition.X + m_clipMargin.Left * actualScale.X),
                    (int)(this.Transform.ActualPosition.Y + m_clipMargin.Top * actualScale.Y),
                    (int)((this.Size.X - m_clipMargin.Left - m_clipMargin.Right) * actualScale.X + 0.5f),
                    (int)((this.Size.Y - m_clipMargin.Top - m_clipMargin.Bottom) * actualScale.Y + 0.5f));
            }

            if (m_backgroundDepth == WidgetBackgroundDepth.BackClipped)
                m_background.Draw(canvas);
                        
            DrawContents(canvas);
            
            if (m_backgroundDepth == WidgetBackgroundDepth.TopClipped)
                m_background.Draw(canvas);
            
            if (m_clipContents)
                WindowController.Instance.CancelClipRect();

            if (m_backgroundDepth == WidgetBackgroundDepth.Top)
                m_background.Draw(canvas);
        }

        public override bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            if ((!string.IsNullOrEmpty(m_tooltip) || OnTooltip != null) && ((pointer == 0 && !unpress && !press) || (pointer != 0 && press)))
                return WidgetManager.HandleTooltip(this, m_tooltip, new Vector2(x, y), OnTooltip);

            return base.Touch(x, y, press, unpress, pointer);
        }

        protected virtual void DrawContents(object canvas)
        {
        }
        
        public override void Remove()
        {
            foreach (WindowObject obj in m_background.List)
                obj.Remove();

            m_background.Clear();

            base.Remove();
        }

        public void FadeTo(float alpha, int time, Action callback)
        {
            Animator.StartAnimation(AnimationKind.Alpha, alpha - Alpha, time, (float value) => Alpha += value, callback);
        }
    }
}

