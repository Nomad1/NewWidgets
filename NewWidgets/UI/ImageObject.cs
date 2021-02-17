using System.Numerics;

namespace NewWidgets.UI
{
    /// <summary>
    /// Very simple object containing an sprite
    /// </summary>
    public class ImageObject : WindowObject
    {
        private readonly ISprite m_sprite;

        public ISprite Sprite
        {
            get { return m_sprite; }
        }

        public Vector2 PivotShift
        {
            get { return m_sprite == null ? Vector2.Zero : m_sprite.PivotShift; }
            set { if (m_sprite != null) m_sprite.PivotShift = value; }
        }

        public ImageObject(WindowObject parent, ISprite sprite)
            : base(parent, sprite == null ? null : sprite.Transform)
        {
            m_sprite = sprite;

            if (sprite != null)
                Size = sprite.Size;
        }

        public override void Draw(object canvas)
        {
            if (Removing)
                return;

            if (Visible && m_sprite != null)
                m_sprite.Draw(canvas);

            base.Draw(canvas);
        }

        public override bool Update()
        {
            if (Removing)
                return false;

            if (m_sprite != null)
                m_sprite.Update();

            return base.Update();
        }

        public override bool HitTest(float x, float y)
        {
            if (m_sprite != null)
                return m_sprite.HitTest(x, y);

            return base.HitTest(x, y);
        }
    }
}
