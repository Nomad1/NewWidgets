#if RUNMOBILE
using RunMobile.Utility;
#else
using NewWidgets.Utility;
#endif
using System.Numerics;

namespace NewWidgets.UI
{
    /// <summary>
    /// Interface for 2d sprite. Sprite instances are created in WindowController, so it could be literally anything, i.e. System.Drawing.Bitmap wrapper
    /// The only tricky thing is Transform field - transforms are required for heirarchy and should reference their parent transforms
    /// </summary>
    public interface ISprite
    {
        /// <summary>
        /// Size in pixels of the underlying image
        /// </summary>
        /// <value>The size.</value>
        Vector2 Size { get; }

        /// <summary>
        /// Gets the size of current frame
        /// </summary>
        /// <value>The size of the frame in pixels.</value>
        Vector2 FrameSize { get; }

        /// <summary>
        /// Center of rotation and drawing origin in float coords 0 to 1.0
        /// </summary>
        /// <value>The pivot shift.</value>
        Vector2 PivotShift { get; set; }

        /// <summary>
        /// Current animation frame
        /// </summary>
        /// <value>The frame.</value>
        int Frame { get; set; }

        /// <summary>
        /// Number of frames in this sprite
        /// </summary>
        int FrameCount { get; }

        /// <summary>
        /// Gets the tag of current frame
        /// </summary>
        /// <value>The frame tag.</value>
        int FrameTag { get; }

      
        /// <summary>
        /// Image transparency, from 0 to 255
        /// </summary>
        /// <value>The alpha.</value>
        byte Alpha { get; set; }

        /// <summary>
        /// Color tint value, only lower 24-bits are used
        /// </summary>
        /// <value>The color.</value>
        int Color { get; set; }

        /// <summary>
        /// Set of Position/Rotation/Scale components
        /// </summary>
        /// <value>The transform.</value>
        Transform Transform { get; }

        #region Methods

        /// <summary>Checks if point is inside sprite in global coords</summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        bool HitTest(float x, float y);

        void Draw();

        void Update();

        #endregion
    }
}
