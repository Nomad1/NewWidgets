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
    /// The only tricky thing is Transform field - transforms are required for heirarchy but this can change sooner or later
    /// </summary>
    public interface ISprite
    {
        /// <summary>
        /// Size in pixels of the underlying image
        /// </summary>
        /// <value>The size.</value>
        Vector2 Size { get; }

        /// <summary>
        /// Center of rotation/drawing origin in percent 
        /// </summary>
        /// <value>The pivot shift.</value>
        Vector2 PivotShift { get; set; }

        /// <summary>
        /// Current animation frame
        /// </summary>
        /// <value>The frame.</value>
        int Frame { get; set; }

        /// <summary>
        /// Number of frames
        /// </summary>
        int Frames { get; }

        /// <summary>
        /// Gets the tag of current frame
        /// </summary>
        /// <value>The frame tag.</value>
        int FrameTag { get; }

        /// <summary>
        /// Gets the size of current frame
        /// </summary>
        /// <value>The size of the frame.</value>
        Vector2 FrameSize { get; }

        /// <summary>
        /// Image transparency, from 0 to 255, only lower 8 bits are used
        /// </summary>
        /// <value>The alpha.</value>
        byte Alpha { get; set; }

        /// <summary>
        /// Color tint value, only lower 24-bits are used
        /// </summary>
        /// <value>The color.</value>
        int Color { get; set; }


        #region Transformation

        /// <summary>
        /// Set of Position/Rotation/Scale components
        /// </summary>
        /// <value>The transform.</value>
        Transform Transform { get; }

        /// <summary>
        /// Position relative to parent
        /// </summary>
        /// <value>The position.</value>
        Vector2 Position { get; set; }

        /// <summary>
        /// Rotation in degrees
        /// </summary>
        /// <value>The rotation.</value>
        float Rotation { get; set; }

        /// <summary>
        /// Uniform scale
        /// </summary>
        /// <value>The scale.</value>
        float Scale { get; set; }

        #endregion
              
        #region Methods

        /// <summary>Checks if point is inside sprite</summary>
        /// <param name="x">- point X</param>
        /// <param name="y">- point Y</param>
        bool HitTest(float x, float y);

        void Draw(object canvas);

        void Update();

        #endregion
    }
}
