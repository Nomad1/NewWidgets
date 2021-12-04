using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using CoreGraphics;
using NewWidgets.UI;

namespace NewWidgets.Mac
{
    /// <summary>
    /// Controller class for mac Core Graphics test implementation
    /// </summary>
    public class MacController : WindowController
    {
        /// <summary>
        /// Helper struct to store sprite data
        /// </summary>
        private struct SpriteData
        {
            public readonly string Image;
            public readonly string Name;
            public MacSprite.FrameData[] Frames;

            public SpriteData(string image, string name, MacSprite.FrameData[] frames)
            {
                Name = name;
                Image = image;
                Frames = frames;
            }
        }

        private readonly LinkedList<Tuple<Action, DateTime>> m_delayedActions = new LinkedList<Tuple<Action, DateTime>>();
        private readonly Dictionary<string, CGImage> m_images = new Dictionary<string, CGImage>();
        private readonly Dictionary<string, SpriteData> m_sprites = new Dictionary<string, SpriteData>();
        private readonly Stack<CGRect> m_clipRect = new Stack<CGRect>();

        private readonly int m_screenWidth;
        private readonly int m_screenHeight;
        private readonly float m_uiScale;
        private readonly float m_fontScale;
        private readonly bool m_isSmallScreen;
        private readonly WindowObjectArray<Window> m_windows;
        private readonly string m_imagePath;

        private CGContext m_currentContext;

        public override int ScreenWidth
        {
            get { return m_screenWidth; }
        }

        public override int ScreenHeight
        {
            get { return m_screenHeight; }
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
            get { return m_isSmallScreen; }
        }

        public override IList<Window> Windows
        {
            get { return m_windows.List; }
        }

        public string ImagePath
        {
            get { return m_imagePath; }
        }

        
        public override Vector2 PointerPosition
        {
            get
            {
                return Vector2.Zero;
            }
        }

        public override Vector3 SensorValue
        {
            get { return Vector3.Zero; }
        }

        public override Vector4 ThumbStickValue
        {
            get { return Vector4.Zero; }
        }

        internal CGRect ClipRect
        {
            get
            {
                if (m_clipRect.Count == 0)
                    return new CGRect(0, 0, ScreenWidth, ScreenHeight);

                return m_clipRect.Peek();
            }
        }

        internal CGContext CurrentContext
        {
            get { return m_currentContext; }
        }

        public event Action OnInit;
        public sealed override event TouchDelegate OnTouch;


        public MacController(int width, int height, float scale, float fontScale, bool isSmallScreen, string imagePath)
        {
            m_screenWidth = width;
            m_screenHeight = height;
            m_uiScale = scale;
            m_fontScale = fontScale;
            m_isSmallScreen = isSmallScreen;
            m_imagePath = imagePath;

            m_windows = new WindowObjectArray<Window>();

            Widgets.WidgetManager.Init(fontScale);

            foreach (string file in Directory.GetFiles(m_imagePath, "*.png"))
                RegisterSprite(Path.GetFileNameWithoutExtension(file), file, null);
        }

        private void RegisterSprite(string id, string file, MacSprite.FrameData[] frames, int subdivideX = 1, int subdivideY = 1)
        {
            CGImage image;

            if (!m_images.TryGetValue(file, out image))
                m_images[file] = image = CGImage.FromPNG(CGDataProvider.FromFile(file), null, false, CGColorRenderingIntent.Default);

            if (image == null)
            {
                LogError("Failed to load sprite {0}", id);
                return;
            }

            if (frames == null)
            {
                int count = subdivideX * subdivideY;
                frames = new MacSprite.FrameData[count];

                int frameWidth = (int)(image.Width / subdivideX);
                int frameHeight = (int)(image.Height / subdivideY);

                for (int y = 0; y < subdivideY; y++)
                    for (int x = 0; x < subdivideX; x++)
                    {
                        int index = x + y * subdivideX;
                        frames[index] = new MacSprite.FrameData(x*frameWidth, y*frameHeight, frameWidth, frameHeight, 0, 0, index);
                    }
            }

            m_sprites[id] = new SpriteData(file, id, frames);
        }

        /// <summary>
        /// Load sprite atlas resulting in bunch of registered sprites or single sprite with 
        /// many frames (i.e. fonts)
        /// </summary>
        /// <param name="atlasFile"></param>
        public void RegisterSpriteAtlas(string atlasFile)
        {
            string spriteName = Path.GetFileNameWithoutExtension(atlasFile);

            Dictionary<string, List<MacSprite.FrameData>> atlas = new Dictionary<string, List<MacSprite.FrameData>>();
            try
            {
                using (Stream stream = File.OpenRead(atlasFile))
                {
                    BinaryReader reader = new BinaryReader(stream);
                    uint magic = reader.ReadUInt32();

                    int origWidth = magic == 0xfcdeabcc || magic == 0xfddeabcc ? 0 : reader.ReadInt16();
                    int origHeight = magic == 0xfcdeabcc || magic == 0xfddeabcc ? 0 : reader.ReadInt16();

                    int frames = magic == 0xfddeabcc ? reader.ReadInt16() : reader.ReadByte();

                    SpriteData[] sprites = new SpriteData[frames];

                    for (int i = 0; i < frames; i++)
                    {
                        int x = reader.ReadInt16();
                        int y = reader.ReadInt16();
                        int width = reader.ReadInt16();
                        int height = reader.ReadInt16();
                        if (magic == 0xfcdeabcc || magic == 0xfddeabcc)
                        {
                            origWidth = reader.ReadInt16();
                            origHeight = reader.ReadInt16();
                        }
                        int offsetX = reader.ReadInt16();
                        int offsetY = reader.ReadInt16();
                        string texture = reader.ReadString();

                        int tag = magic == 0xfadeabcc ? -1 : reader.ReadInt16();

                        List<MacSprite.FrameData> frameData;

                        if (string.IsNullOrEmpty(texture))
                            texture = spriteName;

                        if (!atlas.TryGetValue(texture, out frameData))
                            atlas[texture] = frameData = new List<MacSprite.FrameData>();

                        frameData.Add(new MacSprite.FrameData(x, y, width, height, offsetX, offsetY, tag));
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Asked to register invalid sprite atlas {0}. Error: {1}", atlasFile, ex);
                return;
            }

            foreach(var pair in atlas)
                RegisterSprite(pair.Key, Path.Combine(m_imagePath, pair.Key + ".png"), pair.Value.ToArray());
        }

        #region Abstract WindowControllerBase implementation

        public override void SetSpriteSubdivision(string id, int subdivideX, int subdivideY)
        {
            RegisterSprite(id, Path.Combine(m_imagePath, id + ".png"), null, subdivideX, subdivideY);
        }

        public override ISprite CloneSprite(ISprite sprite)
        {
            ISprite result = CreateSprite(((MacSprite)sprite).Id);
            result.Frame = sprite.Frame;
            result.PivotShift = sprite.PivotShift;
            result.Alpha = sprite.Alpha;
            result.Color = sprite.Color;
            return result;
        }

        public override ISprite CreateSprite(string id)
        {
            SpriteData spriteData;
            if (!m_sprites.TryGetValue(id, out spriteData))
            {
                LogError("Controller asked to create non-existing sprite {0}", id);
                return null;
            }

            CGImage image;
            if (!m_images.TryGetValue(spriteData.Image, out image))
            {
                LogError("Controller asked to retrieve invalid image {0}", spriteData.Image);
                return null;
            }

            MacSprite result = new MacSprite(image, id, new Vector2((int)image.Width, (int)image.Height), spriteData.Frames);
            return result;
        }

        public override long GetTime()
        {
            return Environment.TickCount;
        }

        public override void LogError(string error, params object[] parameters)
        {
            Console.Error.WriteLine(error, parameters);
        }

        public override void LogMessage(string message, params object[] parameters)
        {
            Console.WriteLine(message, parameters);
        }

        public override void PlaySound(string id)
        {
            Console.WriteLine("Playing sound {0}", id);
        }

        public override void ScheduleAction(Action action, int delay)
        {
            m_delayedActions.AddLast(new Tuple<Action, DateTime>(action, DateTime.Now.AddMilliseconds(delay)));
        }

        public override void SetClipRect(int x, int y, int width, int height)
        {
            m_clipRect.Push(new CGRect(x, ScreenHeight - y, width, -height));
        }

        public override void CancelClipRect()
        {
            m_clipRect.Pop();
            // no clip rects in winforms
        }

        #endregion

        #region External events

        public bool Touch(float x, float y, bool press, bool unpress, int pointer)
        {
            y = ScreenHeight - y;

            if (OnTouch != null)
            {
                foreach (Delegate ndelegate in OnTouch.GetInvocationList())
                {
                    if (((TouchDelegate)ndelegate)(x, y, press, unpress, pointer))
                        return true;
                }
            }

            for (int i = Windows.Count - 1; i >= 0; i--)
            {
                if (Windows[i].Touch(x, y, press, unpress, pointer))
                    return true;

                bool hit = Windows[i].HitTest(x, y);

                if (hit)
                    break;

                if (((Window)Windows[i]).Modal)
                {
                    if (press || unpress)
                        Windows[i].Key(SpecialKey.Back, true, "");
                    break;
                }
            }

            return false;
        }

        public bool Key(SpecialKey key, bool up, string keyString)
        {
            if (Windows.Count > 0 && Windows[Windows.Count - 1].Key(key, up, keyString))
                return true;

            return false;
        }

        public bool Zoom(float x, float y, float value)
        {
            for (int i = Windows.Count - 1; i >= 0; i--)
            {
                if (Windows[i].Zoom(x, y, value))
                    return true;

                bool hit = Windows[i].HitTest(x, y);

                if (hit)
                    break;
            }

            return false;
        }

        #endregion

        #region Draw/Update/Init

        public void Draw(CGContext context)
        {
            m_currentContext = context;

            m_windows.Draw();

            m_currentContext = null;
        }

        public bool Update()
        {
            LinkedListNode<Tuple<Action, DateTime>> node = m_delayedActions.First;
            while (node != null)
            {
                LinkedListNode<Tuple<Action, DateTime>> next = node.Next;
                if (node.Value.Item2 < DateTime.Now)
                {
                    try
                    {
                        node.Value.Item1();
                    }
                    catch(Exception ex)
                    {
                        LogError("Error executing action: {0}", ex);
                    }

                    m_delayedActions.Remove(node);
                }
                node = next;
            }

            if (Windows.Count == 0 && OnInit != null)
                OnInit();

            return m_windows.Update();
        }

        public override void AddWindow(Window window)
        {
            m_windows.Add(window);
        }

        public override void ShowKeyboard(bool show)
        {

        }

        #endregion
    }
}
