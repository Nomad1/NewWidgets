using System;
using NewWidgets.Sample;
using NewWidgets.Widgets;
using RunMobile;
using RunMobile.Graphics;
using RunMobile.Utility;

namespace NewWidgets.RunMobileSample
{
    /// <summary>
    /// This is a simple class implementing DesktopWindow (system level OpenGL window) in order to load two shaders and display
    /// a TestWindow class
    /// </summary>
    class SampleWindow : RunMobile.OpenTK.OpenTKDesktopWindow
    {
        private TestWindow m_window;

        public SampleWindow()
            : base(1024, 768, "Sample")
        {
        }

        /// <summary>
        /// Engine initialization. We need to set resource path there and load shaders.
        /// </summary>
        protected override void Init()
        {
            AssetManager.Init("assets", ".");

            // we need to load at least default UI sprite shader

            LoadShader("ui",
                        0,
                        "simple_sprite.vert.shader",
                        "simple_sprite.frag.shader",
                        DefaultSpriteShaderUniforms
                        );

            // font shader theoretically could be combined with sprite shader
            // but it's better to have a separate one

            LoadShader("font",
                        0,
                        "simple_sprite.vert.shader",
                        "font.frag.shader",
                        DefaultSpriteShaderUniforms
                        );
        }

        /// <summary>
        /// Creation of game controller. We need to override it to hook onto OnInit method
        /// </summary>
        protected override BaseGameController CreateGameController()
        {
            BaseGameController gameController = new BaseGameController(UserData);
            gameController.OnInit += HandleGameInit;

            return gameController;
        }

        /// <summary>
        /// This method is called only once to create game windows and load textures
        /// </summary>
        private void HandleGameInit()
        {
            SpriteManager.Instance.LoadSpriteAtlas("ui"); // loads ui.bin with defulat ui atlas
            SpriteManager.Instance.LoadSpriteAtlas("font5"); // loads font5.bin with font MSDF atlas
            WidgetManager.Init(0.5f); // 0.5 is font scale
            WidgetManager.LoadUI(AssetManager.GetAssetTextFile("ui.xml"));

            WidgetManager.SaveUI(Console.Out);

            // Here we're creating main window and it's added to the Game Controller
            m_window = new TestWindow();
            GameController.AddWindow(m_window);
        }

        protected override void OnUpdateFrame(OpenTK.FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            // Pass FPS values to text label in the TestWindow class
            if (m_window != null)
                m_window.SetFpsValue(UpdateFPS, DrawFPS);
        }

        public static void Main(string[] args)
        {
            SampleWindow window = new SampleWindow();
            window.Run(60, 60); // 60 FPS for update and rendering should be ok
        }
    }
}
