using System;
using System.Numerics;
using NewWidgets.UI;
using RunMobile.Graphics;
using RunMobile.Utility;

namespace RunMobile
{
    /// <summary>
    /// Abstract Main game class. Implements WindowController to work with UI
    /// Game should inherit this class and provide platform-specific things alike touchscreen mode
    /// </summary>
    public class BaseGameController : WindowController
    {
        private readonly WindowObjectArray<Window> m_windows = new WindowObjectArray<Window>();

        private readonly object m_syncRoot = new object();
        private readonly SortedLinkedList<long, Action> m_delayedActions = new SortedLinkedList<long, Action>();
        protected readonly UserData m_userData;

        private string m_uiMaterial;

        private Vector3 m_lastSensorValue;
        private Vector4 m_lastThumbStickValue;
        private Vector2 m_lastPointerPosition;

        // Weird
        protected float m_fontScale;
        protected float m_uiScale;
        protected bool m_touchScreen;

        public override Vector3 SensorValue
        {
            get { return m_lastSensorValue; }
        }

        public override Vector4 ThumbStickValue
        {
            get { return m_lastThumbStickValue; }
        }

        public override Vector2 PointerPosition
        {
            get { return m_lastPointerPosition; }
        }

        public override Window [] Windows
        {
            get { return m_windows.List; }
        }

        public override int ScreenWidth
        {
            get { return GraphicsManager.Instance.ScreenWidth; }
        }

        public override int ScreenHeight
        {
            get { return GraphicsManager.Instance.ScreenHeight; }
        }

        public override float UIScale
        {
            get { return m_uiScale; }
        }

        public override float FontScale
        {
            get { return m_fontScale; }
        }

        public override bool IsTouchScreen
        {
            get { return m_touchScreen; }
        }

        public string UIMaterial
        {
            get { return m_uiMaterial; }
            set { m_uiMaterial = value; }
        }

        public UserData Data
        {
            get { return m_userData; }
        }

        public override event TouchDelegate OnTouch;

        public event Action OnInit;

        public BaseGameController(UserData data)
        {
            m_userData = data;
            m_uiScale = 1.0f;
            m_fontScale = 1.0f;
            m_uiMaterial = "ui";
        }

        /// <summary>
        /// Main update call
        /// </summary>
        public virtual void Update()
        {
            lock (m_syncRoot)
            {
                var node = m_delayedActions.First;
                while (node != null)
                {
                    var next = node.Next;
                    if (node.Value.Key < GameTime.Now)
                    {
                        try
                        {
                            node.Value.Value();
                        }
                        catch (Exception ex)
                        {
                            LogError("Error executing action: {0}", ex);
                        }

                        m_delayedActions.Remove(node);
                    }
                    else
                        break;

                    node = next;
                }
            }

            if (m_windows.Count == 0)
                Init();

            m_windows.Update();
        }

        /// <summary>
        /// Init is called on update when windows collection is empty
        /// </summary>
        protected virtual void Init()
        {
            if (OnInit != null)
                OnInit();
        }

        /// <summary>
        /// Main draw call. Called after Update
        /// </summary>
        /// <param name="context">Context.</param>
        public virtual void Draw()
        {
            m_windows.Draw();
        }

        public bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            m_lastPointerPosition = new Vector2(x, y);

            if (OnTouch != null)
            {
                Delegate[] list = OnTouch.GetInvocationList();

                for (int i = list.Length - 1; i >= 0; i--)
                {
                    if (((TouchDelegate)list[i])(x, y, press, unpress, pointer))
                        return true;
                }
            }

            for (int i = Windows.Length - 1; i >= 0; i--)
            {
                if (!Windows[i].Visible)
                    continue;

                if (Windows[i].Touch(x, y, press, unpress, pointer))
                    return true;

                bool hit = Windows[i].HitTest(x, y);

                if (hit)
                    break;

                if (Windows[i].Modal)
                {
                    if (press || unpress)
                        Windows[i].Key(SpecialKey.Back, true, "");
                    break;
                }
            }

            return false;
        }

        public void Sensor(float sensorX, float sensorY, float sensorZ)
        {
            m_lastSensorValue = new Vector3(sensorX, sensorY, sensorZ);
        }

        public void ThumbSticks(float leftX, float leftY, float rightX, float rightY)
        {
            m_lastThumbStickValue = new Vector4(leftX, leftY, rightX, rightY);
        }

        public bool Key(SpecialKey key, bool up, string keyString)
        {
            for (int i = Windows.Length - 1; i >= 0; i--)
            {
                if (!Windows[i].Visible)
                    continue;

                if (Windows[i].Key(key, up, keyString))
                    return true;

                break;
            }

            return false;
        }

        public bool Zoom(float x, float y, float value)
        {
            for (int i = Windows.Length - 1; i >= 0; i--)
            {
                if (!Windows[i].Visible)
                    continue;

                if (Windows[i].Zoom(x, y, value))
                    return true;

                bool hit = Windows[i].HitTest(x, y);

                if (hit)
                    break;
            }

            return false;
        }

        public override void ShowKeyboard(bool show)
        {
            
        }

        #region WindowController implementation

        public override long GetTime()
        {
            return GameTime.Now;
        }

        public override void LogMessage(string message, params object[] parameters)
        {
            LogConsole.WriteLine(LogLevel.PRINT, message, parameters);
        }

        public override void LogError(string error, params object[] parameters)
        {
            LogConsole.WriteLine(LogLevel.ERROR, error, parameters);
        }

        public override void ScheduleAction(Action action, int delay)
        {
            lock (m_syncRoot)
                m_delayedActions.Add(GameTime.Now + delay, action);
        }

        public override ISprite CreateSprite(string id)
        {
#if MINI
            // CSS resource strings (e.g. "--font-resource: url(\"font|font5\")") use the
            // "material|id" convention from the non-MINI path below. MINI mode has no
            // named materials -- every sprite lives in one shared registry -- so only the
            // id after the '|' is meaningful here.
            int miniIndex = id.IndexOf('|');
            string spriteId = miniIndex != -1 ? id.Substring(miniIndex + 1) : id;

            SpriteData [] data = SpriteManager.Instance.GetTexture(spriteId);

            if (data == null || data.Length == 0)
            {
                LogError("Asked to create unknown sprite " + id);
                return null;
            }

            return new Sprite(TextureManager.Instance.GetTexture(data[0].TextureId), data);

#else
            int index = id.IndexOf('|');

            if (index != -1)
            {
                return new Sprite(id.Substring(0, index), id.Substring(index + 1));
            }

            return new Sprite(m_uiMaterial, id);
#endif
        }

        public override ISprite CloneSprite(ISprite sprite)
        {
            Sprite oldSprite = sprite as Sprite;
            if (oldSprite == null)
            {
                LogError("Asked to clone sprite for unknown or empty sprite!");
                return null;
            }
            return new Sprite(oldSprite);
        }

        public void RegisterFramedAnimation(string id, params string[] frameNames)
        {
            SpriteManager.Instance.RegisterFramedAnimation(id, frameNames);
        }

        public void LoadSpriteAtlas(string binResourceId, string textureId)
        {
            SpriteManager.Instance.LoadSpriteAtlas(binResourceId, textureId);
        }

        public override void SetSpriteSubdivision(string id, int width, int height)
        {
            SpriteManager.Instance.RegisterSpriteSubdivision(id, 0, id, width, height);
        }

        public void RegisterSpriteSubdivision(string sourceId, int sourceFrame, string targetId, int width, int height)
        {
            SpriteManager.Instance.RegisterSpriteSubdivision(sourceId, sourceFrame, targetId, width, height);
        }

        public void RegisterSpriteSubdivision(string sourceId, int sourceFrame, string targetId, RectangleF[] parts)
        {
            SpriteManager.Instance.RegisterSpriteSubdivision(sourceId, sourceFrame, targetId, parts);
        }

        public override void PlaySound(string id)
        {
            GameSound.PlaySound(id);
        }

        public override void StopSound(string id)
        {
            GameSound.StopSound(id);
        }

        public override void AddWindow(Window window)
        {
            m_windows.Add(window);
        }

        public override void SetClipRect(int x, int y, int width, int height)
        {
            GraphicsManager.Instance.SetClipRect(x, y, width, height);
        }

        public override void CancelClipRect()
        {
            GraphicsManager.Instance.CancelClipRect();
        }
#endregion
    }
}

