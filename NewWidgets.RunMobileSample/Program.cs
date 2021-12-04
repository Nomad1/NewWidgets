using System;
using NewWidgets.Sample;
using NewWidgets.Widgets;
using OpenTK;
using RunMobile;
using RunMobile.Graphics;
using RunMobile.OpenTK.OpenGL;
using RunMobile.Utility;

namespace NewWidgets.RunMobileSample
{
    class SampleWindow : GameWindow
    {
        private BaseGame m_gameController;
        private UserData m_data;

        private long m_lastFrameUpdate;
        private long m_lastFrameDraw;

        private float m_averageFrameUpdateTime;
        private float m_averageFrameTime;

        private TestWindow m_window;

        public SampleWindow()
            : base(800, 600, OpenTK.Graphics.GraphicsMode.Default, "Sample", GameWindowFlags.Default, DisplayDevice.Default, 4, 1, OpenTK.Graphics.GraphicsContextFlags.ForwardCompatible)
        {
            //Keyboard.KeyDown += Keyboard_KeyDown;

            const int targetFps = 60;
            m_averageFrameTime = m_averageFrameUpdateTime = 1000.0f / targetFps;
            m_lastFrameDraw = m_lastFrameUpdate = Environment.TickCount; // unprecise timer

            VSync = VSyncMode.Adaptive;

            AssetManager.Init("assets", ".");
        }

        protected override void Dispose(bool manual)
        {
            base.Dispose(manual);

            TextureManager.Instance.Unbind();
            TextureManager.Instance.Clear();
            ModelManager.Instance.Clear();

            GameSound.Deinit();
        }

        private void HandleInit()
        {
            SpriteManager.Instance.LoadSpriteAtlas("ui");
            SpriteManager.Instance.LoadSpriteAtlas("font5");
            WidgetManager.Init(0.5f);
            WidgetManager.LoadUI(AssetManager.GetAssetTextFile("ui.xml"));

            m_window = new TestWindow();
            m_gameController.AddWindow(m_window);
        }

        protected override void OnResize(EventArgs e)
        {
            if (Width == 0 || Height == 0)
            {
                TextureManager.Instance.Unbind();
                return;
            }

            MakeCurrent();

            GLGraphicsManager.Init(Width, Height, 410, true, Context.GraphicsMode.Samples);

            GLGraphicsManager.Instance.LoadShader("ui",
                            ShaderFlags.None,
                            AssetManager.GetAssetTextFile("simple_sprite.vert.shader"),
                            AssetManager.GetAssetTextFile("simple_sprite.frag.shader"),
                            new[]
                            {
                                new ShaderUniform("s_pmvMatrix", ShaderDataType.Matrix, ShaderBuiltinUniform.ProjectionViewMatrix),
                                new ShaderUniform("s_texture_0", ShaderDataType.None, ShaderBuiltinUniform.Texture0),
                            }
                            );

            GLGraphicsManager.Instance.LoadShader("font",
                            ShaderFlags.None,
                            AssetManager.GetAssetTextFile("simple_sprite.vert.shader"),
                            AssetManager.GetAssetTextFile("font.frag.shader"),
                            new[]
                            {
                                new ShaderUniform("s_pmvMatrix", ShaderDataType.Matrix, ShaderBuiltinUniform.ProjectionViewMatrix),
                                new ShaderUniform("s_texture_0", ShaderDataType.None, ShaderBuiltinUniform.Texture0)
                            },
                            "COMPAT_QUALITY"
                           );


            if (m_gameController == null)
            {
                m_gameController = new BaseGame(m_data);
                m_gameController.OnInit += HandleInit;
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            GameTime.UpdateNow();

            if (m_gameController != null)
            {
                m_gameController.Update();

                m_window.SetFpsValue(1000.0f / m_averageFrameUpdateTime, 1000.0f / m_averageFrameTime);
            }

            UpdateUpdateFps();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GLGraphicsManager.Instance.BeginFrame();

            TextureManager.Instance.Draw(); // we need to continue loading textures in some cases

            if (m_gameController != null)
            {
                m_gameController.Draw();
            }

            GLGraphicsManager.Instance.EndFrame();

            SwapBuffers();

            UpdateDrawFps();
        }

        private void UpdateDrawFps()
        {
            long time = Environment.TickCount;

            int frameTime = (int)(time - m_lastFrameDraw);

            // calculate average frame time for last N frames
            int diminishing = 120 - 1;

            m_averageFrameTime = (m_averageFrameTime * diminishing + frameTime) / (float)(diminishing + 1);
            m_lastFrameDraw = time;
        }

        private void UpdateUpdateFps()
        {
            long time = Environment.TickCount;

            int frameTime = (int)(time - m_lastFrameUpdate);

            // calculate average frame time for last N frames
            int diminishing = 120 - 1;

            m_averageFrameUpdateTime = (m_averageFrameUpdateTime * diminishing + frameTime) / (float)(diminishing + 1);
            m_lastFrameUpdate = time;
        }

        public static void Main(string[] args)
        {
            SampleWindow window = new SampleWindow();
            window.Run();
        }
    }
}
