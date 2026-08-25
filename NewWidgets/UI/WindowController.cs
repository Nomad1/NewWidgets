using System;
using System.Numerics;

#if RUNMOBILE
using RunMobile.Utility;
#else
using System.Drawing;
#endif

namespace NewWidgets.UI
{
    public delegate bool TouchDelegate(float x, float y, bool press, bool unpress, int pointer);

    /// <summary>
    /// Abstract class for UI controller that provides scaling, sprites and other data needed for UI
    /// You should implement this class to use NewWidgets
    /// </summary>
    public abstract class WindowController
    {
        private static WindowController s_instance;

        public static WindowController Instance
        {
            get
            {
                System.Diagnostics.Debug.Assert(s_instance != null, "WindowController.Instance requested before WindowController was inited!");
                return s_instance;
            }
            private set
            {
                System.Diagnostics.Debug.Assert(s_instance == null, "WindowController.Instance set requested more than once!");
                s_instance = value;
            }
        }

        protected WindowController()
        {
            Instance = this;
        }

        /// <summary>
        /// Top level touch event
        /// </summary>
        public abstract event TouchDelegate OnTouch;

        /// <summary>
        /// Gets the width of the screen in pixels
        /// </summary>
        /// <value>The width of the screen.</value>
        public abstract int ScreenWidth { get; }

        /// <summary>
        /// Gets the height of the screen in pixels
        /// </summary>
        /// <value>The height of the screen.</value>
        public abstract int ScreenHeight { get; }

        /// <summary>
        /// Gets the screen scale for UI auto-scaling
        /// </summary>
        /// <value>The screen scale.</value>
        public abstract float UIScale { get; }

        /// <summary>
        /// Gets the button scale to avoid huge buttons on tablets and small on phones
        /// </summary>
        /// <value>The button scale.</value>
        public abstract float FontScale { get; }

        /// <summary>
        /// Gets a value indicating whether device is a mobile phone (less than 6" or something else)
        /// </summary>
        /// <value><c>true</c> if is small screen; otherwise, <c>false</c>.</value>
        public abstract bool IsTouchScreen { get; }

        /// <summary>
        /// Gets last mouse or touch position
        /// </summary>
        public abstract Vector2 PointerPosition { get; }

        /// <summary>
        /// Gets last sensor value
        /// </summary>
        public abstract Vector3 SensorValue { get; }

        /// <summary>
        /// Gets last thumb sticks value
        /// </summary>
        public abstract Vector4 ThumbStickValue { get; }

        /// <summary>
        /// Shows or hides keyboard if text edit field is focused
        /// </summary>
        /// <param name="show">Commands to show or hide the keyboard</param>
        public abstract void ShowKeyboard(bool show);

        /// <summary>
        /// List of currently displayed windows
        /// </summary>
        /// <value>The windows.</value>
        public abstract Window [] Windows { get; }

        /// <summary>
        /// Adds new window to Windows collection
        /// </summary>
        public abstract void AddWindow(Window window);

        /// <summary>
        /// Indicates that sprite should be divided to MxN equal frames
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="subdivideX">Number of horizontal frames</param>
        /// <param name="subdivideY">Number of horizontal frames</param>
        public abstract void SetSpriteSubdivision(string id, int subdivideX, int subdivideY);

        /// <summary>
        /// Registers <paramref name="targetId"/> as a copy of frame 0 of <paramref name="sourceId"/>
        /// cut into arbitrary parts, so a nine-patch can have corners that are not exactly a third
        /// of the source. Each part is a rectangle in normalized 0..1 source coordinates,
        /// X/Y/Width/Height, and part <c>i</c> becomes frame <c>i</c> of the target sprite.
        /// </summary>
        /// <returns><c>true</c> if <paramref name="targetId"/> was registered and can be created
        /// with <see cref="CreateSprite"/>. <c>false</c> if this controller cannot cut a sprite
        /// into arbitrary parts, in which case nothing was registered under
        /// <paramref name="targetId"/> and the caller must fall back to the uniform grid.</returns>
        /// <param name="sourceId">Sprite to cut.</param>
        /// <param name="targetId">Name to register the cut sprite under.</param>
        /// <param name="parts">Parts in normalized source coordinates.</param>
        /// <remarks>
        /// Dormant: no host implements this overload except the test controller, and a host
        /// author can safely leave it alone. Its only caller is the arbitrary-slice nine-patch
        /// in <c>WidgetBackground.InitBorderImageBackground</c>, which is itself dormant because
        /// no shipping stylesheet declares <c>border-image-slice</c>; every such widget takes
        /// the false return below and falls back to thirds. The owner decided the feature is not
        /// needed yet.
        ///
        /// Anyone implementing it should read the notes on
        /// <c>WidgetBackground.InitBorderImageBackground</c> first. In particular RunMobile's
        /// <c>SpriteManager.RegisterSpriteSubdivision(..., RectangleF[] parts)</c> does not add
        /// the source frame's X and Y offset to each part, so it cuts correctly only from a
        /// sprite at the atlas origin, and that has to be fixed there before this seam is
        /// useful.
        /// </remarks>
        public virtual bool SetSpriteSubdivision(string sourceId, string targetId, RectangleF[] parts)
        {
            // Virtual rather than abstract: hosts outside this repository must keep compiling
            // untouched, and a host that never implements this keeps the uniform subdivision
            // it has always had.
            LogMessage("SetSpriteSubdivision of {0} into {1} arbitrary parts is not implemented by {2}, falling back to a uniform 3x3 grid",
                sourceId, parts.Length, GetType().Name);

            SetSpriteSubdivision(sourceId, 3, 3);

            return false;
        }

        /// <summary>
        /// Clones the sprite.
        /// </summary>
        /// <returns>The sprite.</returns>
        /// <param name="sprite">Sprite.</param>
        /// <param name="position">Position.</param>
        public abstract ISprite CloneSprite(ISprite sprite);

        /// <summary>
        /// Construct sprite by hashed id string and with default UI material
        /// </summary>
        /// <returns>The sprite.</returns>
        /// <param name="id">Identifier.</param>
        public abstract ISprite CreateSprite(string id);


        /// <summary>
        /// Construct sprite by hashed id string and with default UI material and specified position
        /// </summary>
        /// <param name="id"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public ISprite CreateSprite(string id, Vector2 position)
        {
            ISprite result = CreateSprite(id);
            result.Transform.FlatPosition = position;
            return result;
        }

        /// <summary>
        /// Sets the screen clip rectangle.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        public abstract void SetClipRect(int x, int y, int width, int height);

        /// <summary>
        /// Cancels the current screen clip rectangle
        /// </summary>
        public abstract void CancelClipRect();

        /// <summary>
        /// Logs the message.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="parameters">Parameters.</param>
        public abstract void LogMessage(string message, params object[] parameters);

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="error">Error.</param>
        /// <param name="parameters">Parameters.</param>
        public abstract void LogError(string error, params object[] parameters);

        /// <summary>
        /// Schedule action
        /// </summary>
        /// <param name="action">Action.</param>
        /// <param name="delay">Start delay in milliseconds</param>
        public abstract void ScheduleAction(Action action, int delay);

        /// <summary>
        /// Gets current engine time in milliseconds
        /// </summary>
        /// <returns>The time in milliseconds</returns>
        public abstract long GetTime();

        /// <summary>
        /// Plays the sound by it's name
        /// </summary>
        /// <param name="id">Sound id</param>
        public abstract void PlaySound(string id);

        /// <summary>
        /// Stops the sound by it's name
        /// </summary>
        /// <param name="id">Sound id</param>
        public abstract void StopSound(string id);
    }
}
