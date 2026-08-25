using System;
using System.Numerics;
using Vector2 = System.Numerics.Vector2;
using NewWidgets.Sample;
using NewWidgets.UI;
using NewWidgets.Utility;
using NewWidgets.Widgets;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using RunMobile;
using RunMobile.Graphics;
using RunMobile.OpenTK.OpenGL;
using RunMobile.Utility;

namespace NewWidgets.RunMobileSample
{
    /// <summary>
    /// Top level OpenTK/SDL2 window. Owns the GL context, drives the BaseGameController
    /// and forwards mouse/keyboard input to it. Modeled on HIH2.W7's Code/Program.cs,
    /// the proven-working RunMobile.OpenTK.OpenTKDesktopWindow replacement for this host.
    /// </summary>
    class SampleWindow : GameWindow
    {
        private BaseGameController m_gameController;
        private TestWindow m_window;
        private bool m_pressed;

        public SampleWindow(int width, int height, bool fullScreen)
            : base(width, height, GraphicsMode.Default, "NewWidgets RunMobile Sample", fullScreen ? GameWindowFlags.Fullscreen : GameWindowFlags.Default)
        {
            VSync = VSyncMode.Adaptive;

            AssetManager.Init("assets", ".");

            Mouse.ButtonDown += HandleButtonDown;
            Mouse.ButtonUp += HandleButtonUp;
            Mouse.Move += HandleMove;
        }

        private void HandleMove(object sender, MouseMoveEventArgs e)
        {
            if (m_pressed)
                m_gameController.Touch(e.X, e.Y, true, false, (int)MouseButton.Left);
        }

        private void HandleButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Left)
                m_pressed = false;
            m_gameController.Touch(e.X, e.Y, false, true, (int)e.Button);
        }

        private void HandleButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Left)
                m_pressed = true;
            m_gameController.Touch(e.X, e.Y, true, false, (int)e.Button);
        }

        protected override void OnResize(EventArgs e)
        {
            MakeCurrent();
            base.OnResize(e);

            if (m_gameController == null)
            {
                GLHelper.Init(Width, Height);

                m_gameController = new BaseGameController(null);
                m_gameController.OnInit += HandleGameInit;
            }
        }

        /// <summary>
        /// This method is called only once to create game windows and load textures
        /// </summary>
        private void HandleGameInit()
        {
            SpriteManager.Instance.LoadSpriteAtlas("ui"); // loads ui.bin with default ui atlas
            SpriteManager.Instance.LoadSpriteAtlas("font5"); // loads font5.bin with font MSDF atlas
            WidgetManager.Init(0.5f); // 0.5 is font scale
            WidgetManager.OnTooltip += TextTooltip;

            // The skin: what a control looks like, and nothing about any particular dialog.
            // The dialog's own geometry is login.css, which assets/login.xhtml links and
            // TestWindow loads with the document -- see the header of login.css.
            WidgetManager.LoadCSS(AssetManager.GetAssetTextFile("ui.css"));

            // Here we're creating main window and it's added to the Game Controller
            m_window = new TestWindow();
            m_gameController.AddWindow(m_window);
        }

        public static bool TextTooltip(Widget sender, string text, Vector2 position)
        {
            if ((sender != null && (!sender.Visible || sender.Opacity <= 0)) || string.IsNullOrWhiteSpace(text))
            {
                WidgetTooltip.Hide();
                return false;
            }

            RectangleF region;

            if (sender == null)
                region = new RectangleF(position.X, position.Y, 32, 32);
            else
                region = sender.ScreenRect;


            if (WidgetTooltip.CurrentTooltip != null && text != null && WidgetTooltip.CurrentTooltip.Tag == (object)text)
            {
                WidgetTooltip.CurrentTooltip.UpdatePosition(position);
                WidgetTooltip.CurrentTooltip.Region = region;
                return true;
            }

            WidgetTooltip.Hide();

            WidgetTooltip tooltip = new WidgetTooltip();
            tooltip.Tag = text;

            Margin padding = tooltip.GetProperty("padding", new Margin(0));
            WidgetLabel body = new WidgetLabel(text);
            body.Relayout();
            tooltip.AddChild(body);

            body.Position = padding.TopLeft;
            body.Update();

            tooltip.Size = body.Size + new Vector2(padding.Width, padding.Height);
            tooltip.Shift = new Vector2(20, 20); // cursor size?

            WidgetTooltip.Show(tooltip, position, region);

            return true;
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (Keyboard[Key.Escape])
                Close();

            if (m_gameController == null)
                return;

            TextureManager.Instance.Update();
            m_gameController.Update();

            // Pass FPS values to text label in the TestWindow class
            if (m_window != null)
                m_window.SetFpsValue((float)UpdateFrequency, (float)RenderFrequency);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            if (m_gameController == null)
                return;

            GLHelper.Instance.ClearColor();
            m_gameController.Draw();
            GLHelper.Instance.Flush();

            SwapBuffers();
        }

        public static void Main(string[] args)
        {
            int width = 1024;
            int height = 768;
            bool fullScreen = false;

            if (args.Length >= 2)
            {
                try
                {
                    width = int.Parse(args[0]);
                    height = int.Parse(args[1]);

                    if (args.Length >= 3)
                        fullScreen = bool.Parse(args[2]);
                }
                catch
                {
                    width = 1024;
                    height = 768;
                    fullScreen = false;
                }
            }

            using (SampleWindow window = new SampleWindow(width, height, fullScreen))
                window.Run(60.0); // 60 FPS for update and rendering should be ok
        }
    }
}
