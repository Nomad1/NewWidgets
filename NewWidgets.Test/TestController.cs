using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

using NewWidgets.UI;
using NewWidgets.Utility;

namespace NewWidgets.Test
{
    // Constraint: WidgetManager (NewWidgets.Widgets.WidgetManager) keeps its CSS style
    // collection in a static field that accumulates styles for the lifetime of the process.
    // WidgetManager.ResetStyles() now exists to clear it, but the test groups in this project
    // (other than SeamTests, which exercises ResetStyles deliberately) still do not call it --
    // they instead use distinct class and id names from every other group so they stay
    // independent of each other. CorpusTests in particular relies on running last so its
    // ~3300 lines of real game CSS cannot collide with anything above it; do not add a
    // ResetStyles call there.

    /// <summary>
    /// Headless <see cref="WindowController"/> for running NewWidgets widget code in a
    /// console test process, with no graphics device and no RunMobile dependency.
    /// </summary>
    internal class TestController : WindowController
    {
        private struct SpriteSize
        {
            public readonly int Width;
            public readonly int Height;

            public SpriteSize(int width, int height)
            {
                Width = width;
                Height = height;
            }
        }

        private struct Subdivision
        {
            public readonly int X;
            public readonly int Y;

            public Subdivision(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        private struct SpriteBuild
        {
            public readonly Vector2 Size;
            public readonly TestSprite.FrameInfo[] Frames;

            public SpriteBuild(Vector2 size, TestSprite.FrameInfo[] frames)
            {
                Size = size;
                Frames = frames;
            }
        }

        // One ScheduleAction(action, delay) call, queued instead of run inline. Sequence
        // breaks ties between two actions with the same DueTime, so draining stays in
        // scheduling order for same-frame actions instead of whatever order a re-sort left them in.
        private struct ScheduledAction
        {
            public readonly Action Action;
            public readonly long DueTime;
            public readonly long Sequence;

            public ScheduledAction(Action action, long dueTime, long sequence)
            {
                Action = action;
                DueTime = dueTime;
                Sequence = sequence;
            }
        }

        private const int DefaultSpriteWidth = 64;
        private const int DefaultSpriteHeight = 64;
        private const int FirstAsciiCode = 32;
        private const int AsciiGlyphCount = 96; // ASCII 32..127 inclusive

        private readonly List<Window> m_windows = new List<Window>();
        private readonly Dictionary<string, Subdivision> m_subdivisions = new Dictionary<string, Subdivision>();
        private readonly Dictionary<string, SpriteSize> m_spriteSizes = new Dictionary<string, SpriteSize>();
        private readonly Dictionary<string, SpriteBuild> m_fontSprites = new Dictionary<string, SpriteBuild>();
        private readonly List<string> m_messages = new List<string>();
        private readonly List<string> m_errors = new List<string>();
        private readonly List<ScheduledAction> m_scheduledActions = new List<ScheduledAction>();

        private int m_screenWidth;
        private int m_screenHeight;
        private float m_uiScale;
        private float m_fontScale;
        private long m_time;
        private long m_scheduledActionSequence;

        private int m_lastClipX;
        private int m_lastClipY;
        private int m_lastClipWidth;
        private int m_lastClipHeight;
        private int m_clipRectCount;

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
            get { return false; }
        }

        public override Vector2 PointerPosition
        {
            get { return Vector2.Zero; }
        }

        public override Vector3 SensorValue
        {
            get { return Vector3.Zero; }
        }

        public override Vector4 ThumbStickValue
        {
            get { return Vector4.Zero; }
        }

        public override Window[] Windows
        {
            get { return m_windows.ToArray(); }
        }

        public int LastClipX
        {
            get { return m_lastClipX; }
        }

        public int LastClipY
        {
            get { return m_lastClipY; }
        }

        public int LastClipWidth
        {
            get { return m_lastClipWidth; }
        }

        public int LastClipHeight
        {
            get { return m_lastClipHeight; }
        }

        public int ClipRectCount
        {
            get { return m_clipRectCount; }
        }

        // Lets a test assert the scheduled-action queue drains rather than growing forever.
        public int PendingActionCount
        {
            get { return m_scheduledActions.Count; }
        }

        public IList<string> Messages
        {
            get { return m_messages; }
        }

        public IList<string> Errors
        {
            get { return m_errors; }
        }

        public override event TouchDelegate OnTouch;

        public TestController(int screenWidth, int screenHeight)
        {
            m_screenWidth = screenWidth;
            m_screenHeight = screenHeight;
            m_uiScale = 1.0f;
            m_fontScale = 1.0f;
        }

        public TestController()
            : this(1920, 1080)
        {
        }

        public override void ShowKeyboard(bool show)
        {
        }

        public override void AddWindow(Window window)
        {
            m_windows.Add(window);
        }

        public override void SetSpriteSubdivision(string id, int subdivideX, int subdivideY)
        {
            m_subdivisions[id] = new Subdivision(subdivideX, subdivideY);
        }

        public override ISprite CloneSprite(ISprite sprite)
        {
            TestSprite source = (TestSprite)sprite;
            ISprite clone = CreateSprite(source.Id);
            clone.Frame = source.Frame;
            clone.PivotShift = source.PivotShift;
            clone.Alpha = source.Alpha;
            clone.Color = source.Color;
            return clone;
        }

        public override ISprite CreateSprite(string id)
        {
            SpriteBuild fontBuild;
            if (m_fontSprites.TryGetValue(id, out fontBuild))
                return new TestSprite(id, fontBuild.Size, fontBuild.Frames);

            SpriteSize size = GetSpriteSize(id);
            TestSprite.FrameInfo[] frames = BuildFrames(id, size);
            return new TestSprite(id, new Vector2(size.Width, size.Height), frames);
        }

        public override void SetClipRect(int x, int y, int width, int height)
        {
            m_lastClipX = x;
            m_lastClipY = y;
            m_lastClipWidth = width;
            m_lastClipHeight = height;
            m_clipRectCount++;
        }

        public override void CancelClipRect()
        {
            // no-op; the last recorded clip rect stays readable for assertions
        }

        public override void LogMessage(string message, params object[] parameters)
        {
            m_messages.Add(FormatLog(message, parameters));
        }

        public override void LogError(string error, params object[] parameters)
        {
            m_errors.Add(FormatLog(error, parameters));
        }

        // Queues the action instead of running it inline. AnimationManager re-schedules
        // itself through this call every "frame" (NewWidgets/UI/AnimationManager.cs); running
        // it synchronously used to recurse through the call stack until it overflowed. Queuing
        // means a nested ScheduleAction call -- one made from inside an action this class is
        // currently running -- just appends to the list rather than calling back in, so there
        // is no recursion regardless of how deep the chain of re-scheduling goes. Call
        // AdvanceTime to actually run what is due.
        public override void ScheduleAction(Action action, int delay)
        {
            m_scheduledActions.Add(new ScheduledAction(action, m_time + delay, m_scheduledActionSequence));
            m_scheduledActionSequence++;
        }

        public override long GetTime()
        {
            return m_time;
        }

        public override void PlaySound(string id)
        {
        }

        public override void StopSound(string id)
        {
        }

        public void SetScreenSize(int width, int height)
        {
            m_screenWidth = width;
            m_screenHeight = height;
        }

        public void SetUIScale(float value)
        {
            m_uiScale = value;
        }

        public void SetFontScale(float value)
        {
            m_fontScale = value;
        }

        // Advances the deterministic clock GetTime() reports, then runs every action now due
        // (DueTime <= the new GetTime()), earliest DueTime first, ties broken by scheduling
        // order. An action that schedules another action lands back in m_scheduledActions
        // instead of running immediately, and this loop -- not the call stack -- is what picks
        // it up if it is already due, so nested scheduling never recurses. Tests must drive
        // time this way rather than relying on wall-clock time, so runs stay reproducible.
        public void AdvanceTime(long milliseconds)
        {
            m_time += milliseconds;

            int dueIndex = FindNextDueIndex();
            while (dueIndex >= 0)
            {
                ScheduledAction due = m_scheduledActions[dueIndex];
                m_scheduledActions.RemoveAt(dueIndex);
                due.Action();

                dueIndex = FindNextDueIndex();
            }
        }

        public void ClearLog()
        {
            m_messages.Clear();
            m_errors.Clear();
        }

        // Registers the pixel size CreateSprite(id) should report for a plain (non-font)
        // sprite. Unregistered ids default to 64x64.
        public void RegisterSprite(string id, int width, int height)
        {
            m_spriteSizes[id] = new SpriteSize(width, height);
        }

        // Registers spriteId as a monospace test font sheet: 96 frames covering ASCII
        // 32..127 inclusive, frame i has FrameTag == 32 + i and FrameSize == (glyphWidth,
        // glyphHeight). Every glyph, including space, reports the identical FrameSize, so
        // with spacing 0 and leading 0, Font.MeasureString of an N-character string comes
        // out to exactly N * glyphWidth wide and glyphHeight tall -- text measurement
        // becomes fully predictable for tests.
        public void RegisterTestFont(string spriteId, int glyphWidth, int glyphHeight)
        {
            TestSprite.FrameInfo[] frames = new TestSprite.FrameInfo[AsciiGlyphCount];
            for (int i = 0; i < AsciiGlyphCount; i++)
                frames[i] = new TestSprite.FrameInfo(glyphWidth, glyphHeight, FirstAsciiCode + i);

            m_fontSprites[spriteId] = new SpriteBuild(new Vector2(glyphWidth * AsciiGlyphCount, glyphHeight), frames);
        }

        // Finds the queued action with the smallest DueTime that is <= m_time (ties broken by
        // Sequence, i.e. scheduling order), or -1 if nothing is due yet.
        private int FindNextDueIndex()
        {
            int bestIndex = -1;
            long bestDueTime = 0;
            long bestSequence = 0;

            for (int i = 0; i < m_scheduledActions.Count; i++)
            {
                ScheduledAction candidate = m_scheduledActions[i];

                if (candidate.DueTime > m_time)
                    continue;

                if (bestIndex < 0 || candidate.DueTime < bestDueTime || (candidate.DueTime == bestDueTime && candidate.Sequence < bestSequence))
                {
                    bestIndex = i;
                    bestDueTime = candidate.DueTime;
                    bestSequence = candidate.Sequence;
                }
            }

            return bestIndex;
        }

        private SpriteSize GetSpriteSize(string id)
        {
            SpriteSize size;
            if (m_spriteSizes.TryGetValue(id, out size))
                return size;

            return new SpriteSize(DefaultSpriteWidth, DefaultSpriteHeight);
        }

        private TestSprite.FrameInfo[] BuildFrames(string id, SpriteSize size)
        {
            Subdivision subdivision;
            if (m_subdivisions.TryGetValue(id, out subdivision))
            {
                int frameCount = subdivision.X * subdivision.Y;
                int frameWidth = size.Width / subdivision.X;
                int frameHeight = size.Height / subdivision.Y;

                TestSprite.FrameInfo[] frames = new TestSprite.FrameInfo[frameCount];
                for (int i = 0; i < frameCount; i++)
                    frames[i] = new TestSprite.FrameInfo(frameWidth, frameHeight, i);

                return frames;
            }

            return new TestSprite.FrameInfo[] { new TestSprite.FrameInfo(size.Width, size.Height, 0) };
        }

        private static string FormatLog(string message, object[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
                return message;

            return string.Format(message, parameters);
        }
    }

    /// <summary>
    /// Plain-data headless <see cref="ISprite"/>. Draw() and Update() do nothing; there is
    /// no graphics device behind this class.
    /// </summary>
    internal class TestSprite : ISprite
    {
        public struct FrameInfo
        {
            public readonly int Width;
            public readonly int Height;
            public readonly int Tag;

            public FrameInfo(int width, int height, int tag)
            {
                Width = width;
                Height = height;
                Tag = tag;
            }
        }

        private readonly string m_id;
        private readonly Vector2 m_size;
        private readonly FrameInfo[] m_frames;
        private readonly Transform m_transform;

        private Vector2 m_pivotShift;
        private uint m_color;
        private int m_frame;

        public string Id
        {
            get { return m_id; }
        }

        public Vector2 Size
        {
            get { return m_size; }
        }

        public Vector2 FrameSize
        {
            get { return new Vector2(m_frames[m_frame].Width, m_frames[m_frame].Height); }
        }

        public Vector2 PivotShift
        {
            get { return m_pivotShift; }
            set { m_pivotShift = value; }
        }

        public int Frame
        {
            get { return m_frame; }
            set
            {
                Debug.Assert(value >= 0 && value < m_frames.Length, "Invalid frame number");
                m_frame = value;
            }
        }

        public int FrameCount
        {
            get { return m_frames.Length; }
        }

        public int FrameTag
        {
            get { return m_frames[m_frame].Tag; }
        }

        public byte Alpha
        {
            get { return (byte)((m_color & 0xff000000) >> 24); }
            set { m_color = (m_color & 0x00ffffff) | ((uint)value << 24); }
        }

        public uint Color
        {
            get { return m_color & 0x00ffffff; }
            set { m_color = (value & 0x00ffffff) | (m_color & 0xff000000); }
        }

        public Transform Transform
        {
            get { return m_transform; }
        }

        internal TestSprite(string id, Vector2 size, FrameInfo[] frames)
        {
            m_id = id;
            m_size = size;
            m_frames = frames;
            m_transform = new Transform();
            m_pivotShift = Vector2.Zero;
            m_color = 0xffffffff;
            m_frame = 0;
        }

        public bool HitTest(float x, float y)
        {
            Vector2 client = m_transform.GetClientPoint(new Vector2(x, y)) + m_pivotShift * FrameSize;

            return client.X >= 0 && client.Y >= 0 && client.X < Size.X && client.Y < Size.Y;
        }

        public void Draw()
        {
        }

        public void Update()
        {
        }
    }

    /// <summary>
    /// Constructs and caches the single <see cref="TestController"/> and
    /// <see cref="Widgets.WidgetManager"/> initialization every test process needs.
    /// </summary>
    internal static class TestEnvironment
    {
        private static TestController s_controller;

        public static TestController Setup()
        {
            if (s_controller == null)
            {
                // WindowController.Instance and WidgetManager.Init() both throw if invoked
                // a second time in the same process, so this must run exactly once.
                s_controller = new TestController();
                Widgets.WidgetManager.Init(1.0f);
            }

            return s_controller;
        }

        public static void LoadCss(string css)
        {
            Widgets.WidgetManager.LoadCSS(css);
        }
    }
}
