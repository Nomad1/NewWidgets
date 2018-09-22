using NewWidgets.Utility;
using System.Numerics;

namespace NewWidgets.UI
{
    public abstract class SpriteBase
    {
        /// <summary>
        /// Size in pixels of the underlying image
        /// </summary>
        /// <value>The size.</value>
        public abstract Vector2 Size { get; }

        /// <summary>
        /// Center of rotation/drawing origin in percent 
        /// </summary>
        /// <value>The pivot shift.</value>
        public abstract Vector2 PivotShift { get; set; }

        /// <summary>
        /// Current animation frame
        /// </summary>
        /// <value>The frame.</value>
        public abstract int Frame { get; set; }

        /// <summary>
        /// Gets the tag of current frame
        /// </summary>
        /// <value>The frame tag.</value>
        public abstract int FrameTag { get; }

        /// <summary>
        /// Gets the size of current frame
        /// </summary>
        /// <value>The size of the frame.</value>
        public abstract Vector2 FrameSize { get; }

        /// <summary>
        /// Image transparency, from 0 to 255, only lower 8 bits are used
        /// </summary>
        /// <value>The alpha.</value>
        public abstract int Alpha { get; set; }

        /// <summary>
        /// Color tint value, only lower 24-bits are used
        /// </summary>
        /// <value>The color.</value>
        public abstract int Color { get; set; }


        #region Transformation

        /// <summary>
        /// Set of Position/Rotation/Scale components
        /// </summary>
        /// <value>The transform.</value>
        public abstract Transform Transform { get; }

        /// <summary>
        /// Position relative to parent
        /// </summary>
        /// <value>The position.</value>
        public abstract Vector2 Position { get; set; }

        /// <summary>
        /// Rotation in degrees
        /// </summary>
        /// <value>The rotation.</value>
        public abstract float Rotation { get; set; }

        /// <summary>
        /// Uniform scale
        /// </summary>
        /// <value>The scale.</value>
        public abstract float Scale { get; set; }

        #endregion
              
        #region Methods

        /// <summary>Checks if point is inside sprite</summary>
        /// <param name="x">- point X</param>
        /// <param name="y">- point Y</param>
        public abstract bool HitTest(float x, float y);

        public abstract void Draw(object canvas);

        public abstract void Update();

        #endregion
    }
}
