using System;
using System.Numerics;

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
        }

        protected WindowController()
        {
            s_instance = this;
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

