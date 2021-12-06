using NewWidgets.Sample;
using NewWidgets.Widgets;
using RunMobile;
using RunMobile.Graphics;
using RunMobile.Utility;

namespace NewWidgets.RunMobileSample
{
    class SampleWindow : RunMobile.OpenTK.OpenTKDesktopWindow
    {
        private TestWindow m_window;

        public SampleWindow()
            : base(1024, 768, "Sample")
        {
        }

        protected override void Init()
        {
            AssetManager.Init("assets", ".");

            LoadShader("ui",
                        0,
                        "simple_sprite.vert.shader",
                        "simple_sprite.frag.shader",
                        DefaultSpriteShaderUniforms
                        );

            LoadShader("font",
                        0,
                        "simple_sprite.vert.shader",
                        "font.frag.shader",
                        DefaultSpriteShaderUniforms
                        );

            if (m_gameController == null)
            {
                m_gameController = new BaseGame(m_data);
                m_gameController.OnInit += HandleGameInit;
            }
        }

        private void HandleGameInit()
        {
            SpriteManager.Instance.LoadSpriteAtlas("ui");
            SpriteManager.Instance.LoadSpriteAtlas("font5");
            WidgetManager.Init(0.5f);
            WidgetManager.LoadUI(AssetManager.GetAssetTextFile("ui.xml"));

            m_window = new TestWindow();
            m_gameController.AddWindow(m_window);
        }

        protected override void OnUpdateFrame(OpenTK.FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (m_window != null)
                //m_window.SetFpsValue((float)UpdateFrequency, (float)RenderFrequency);
                m_window.SetFpsValue(UpdateFPS, DrawFPS);
        }

        public static void Main(string[] args)
        {
            SampleWindow window = new SampleWindow();
            //window.Run(60,60);
            window.Run(0.0f);
        }
    }
}
