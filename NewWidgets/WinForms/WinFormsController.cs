using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;
using NewWidgets.UI;

namespace NewWidgets.WinForms
{
    /// <summary>
    /// Controller class for Windows Forms test implementation
    /// </summary>
    public class WinFormsController : WindowControllerBase
    {
        /// <summary>
        /// Helper struct to store sprite data
        /// </summary>
        private struct SpriteData
        {
            public readonly string Image;
            public readonly string Name;
            public WinFormsSprite.FrameData[] Frames;

            public SpriteData(string image, string name, WinFormsSprite.FrameData[] frames)
            {
                Name = name;
                Image = image;
                Frames = frames;
            }
        }

        private readonly LinkedList<Tuple<Action, DateTime>> m_delayedActions = new LinkedList<Tuple<Action, DateTime>>();
        private readonly Dictionary<string, Image> m_images = new Dictionary<string, Image>();
        private readonly Dictionary<string, SpriteData> m_sprites = new Dictionary<string, SpriteData>();
        private readonly Stack<Rectangle> m_clipRect = new Stack<Rectangle>();

        private readonly int m_screenWidth;
        private readonly int m_screenHeight;
        private readonly float m_screenScale;
        private readonly float m_fontScale;
        private readonly bool m_isSmallScreen;
        private readonly WindowObjectArray m_windows;
        private readonly string m_imagePath;

        public override int ScreenWidth
        {
            get { return m_screenWidth; }
        }

        public override int ScreenHeight
        {
            get { return m_screenHeight; }
        }

        public override float ScreenScale
        {
            get { return m_screenScale; }
        }

        public override float FontScale
        {
            get { return m_fontScale; }
        }

        public override bool IsSmallScreen
        {
            get { return m_isSmallScreen; }
        }

        public override WindowObjectArray Windows
        {
            get { return m_windows; }
        }

        public string ImagePath
        {
            get { return m_imagePath; }
        }

        public Rectangle GetClipRect
        {
            get
            {
                if (m_clipRect.Count == 0)
                    return new Rectangle(0, 0, ScreenWidth, ScreenHeight);
                return m_clipRect.Peek();
            }
        }

        public event Action OnInit;
        public override event TouchDelegate OnTouch;


        public WinFormsController(int width, int height, float scale, float fontScale, bool isSmallScreen, string imagePath)
        {
            Instance = this;

            m_screenWidth = width;
            m_screenHeight = height;
            m_screenScale = scale;
            m_fontScale = fontScale;
            m_isSmallScreen = isSmallScreen;
            m_imagePath = imagePath;

            m_windows = new WindowObjectArray();

            Widgets.WidgetManager.Init(fontScale);

            foreach (string file in Directory.GetFiles(m_imagePath, "*.png"))
                RegisterSprite(Path.GetFileNameWithoutExtension(file), file, null, 1, 1);
        }

        private void RegisterSprite(string id, string file, WinFormsSprite.FrameData[] frames, int subdivideX = 1, int subdivideY = 1)
        {
            Image image = null;

            if (!m_images.TryGetValue(file, out image))
                m_images[file] = image = Image.FromFile(file);

            if (image == null)
                LogError("Failed to load sprite {0}", id);

            if (frames == null)
            {
                int count = subdivideX * subdivideY;
                frames = new WinFormsSprite.FrameData[count];

                int frameWidth = image.Size.Width / subdivideX;
                int frameHeight = image.Size.Height / subdivideY;

                for (int y = 0; y < subdivideY; y++)
                    for (int x = 0; x < subdivideX; x++)
                    {
                        int index = x + y * subdivideX;
                        frames[index] = new WinFormsSprite.FrameData(x*frameWidth, y*frameHeight, frameWidth, frameHeight, 0, 0, index);
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

            Dictionary<string, List<WinFormsSprite.FrameData>> atlas = new Dictionary<string, List<WinFormsSprite.FrameData>>();
            try
            {
                using (Stream stream = File.OpenRead(atlasFile))
                {
                    BinaryReader reader = new BinaryReader(stream);
                    uint magic = reader.ReadUInt32();

                    int origWidth = magic == 0xfcdeabcc ? 0 : reader.ReadInt16();
                    int origHeight = magic == 0xfcdeabcc ? 0 : reader.ReadInt16();

                    byte count = reader.ReadByte();

                    for (int i = 0; i < count; i++)
                    {
                        int x = reader.ReadInt16();
                        int y = reader.ReadInt16();
                        int width = reader.ReadInt16();
                        int height = reader.ReadInt16();
                        if (magic == 0xfcdeabcc)
                        {
                            origWidth = reader.ReadInt16();
                            origHeight = reader.ReadInt16();
                        }
                        int offsetX = reader.ReadInt16();
                        int offsetY = reader.ReadInt16();
                        string texture = reader.ReadString();
                        int tag = magic == 0xfadeabcc ? (short)-1 : reader.ReadInt16();

                        List<WinFormsSprite.FrameData> frames;

                        if (string.IsNullOrEmpty(texture))
                            texture = spriteName;

                        if (!atlas.TryGetValue(texture, out frames))
                            atlas[texture] = frames = new List<WinFormsSprite.FrameData>();

                        frames.Add(new WinFormsSprite.FrameData(x, y, width, height, offsetX, offsetY, tag));
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

        public override SpriteBase CloneSprite(SpriteBase sprite, Vector2 position)
        {
            SpriteBase result = CreateSprite(((WinFormsSprite)sprite).Id, position);
            result.Frame = sprite.Frame;
            result.PivotShift = sprite.PivotShift;
            result.Alpha = sprite.Alpha;
            result.Color = sprite.Color;
            result.Position = position;
            return result;
        }

        public override SpriteBase CreateSprite(string id, Vector2 position)
        {
            SpriteData spriteData;
            if (!m_sprites.TryGetValue(id, out spriteData))
            {
                LogError("Controller asked to create non-existing sprite {0}", id);
                return null;
            }

            Image image;
            if (!m_images.TryGetValue(spriteData.Image, out image))
            {
                LogError("Controller asked to retrieve invalid image {0}", spriteData.Image);
                return null;
            }

            WinFormsSprite result = new WinFormsSprite(image, id, new Vector2(image.Size.Width, image.Size.Height), spriteData.Frames);
            result.Position = position;
            return result;
        }

        public override long GetTime()
        {
            return Environment.TickCount;
        }

        public override void LogError(string error, params object[] parameters)
        {
            Console.Error.WriteLine(error, parameters);
            Console.WriteLine(error, parameters);
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
            m_clipRect.Push(new Rectangle(x, y, width, height));
            // no clip rects in winforms
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
                        Windows[i].Key(SpecialKey.Back, true, '\0');
                    break;
                }
            }

            return false;
        }

        public bool Key(int code, bool up, char character)
        {
            SpecialKey key = (SpecialKey)code;

            for (int i = Windows.Count - 1; i >= 0; /*i--*/)
            {
                if (Windows[i].Key(key, up, character))
                    return true;

                break;
            }

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

        public void Draw(Graphics canvas)
        {
            Windows.Draw(canvas);
        }

        public void Update()
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

            Windows.Update();
        }

        #endregion
    }
}
