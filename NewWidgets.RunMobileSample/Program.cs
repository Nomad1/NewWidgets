using System;
using System.IO;
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

        public SampleWindow()
            : base(800, 600, OpenTK.Graphics.GraphicsMode.Default, "Sample", GameWindowFlags.Default, DisplayDevice.Default, 4, 1, OpenTK.Graphics.GraphicsContextFlags.ForwardCompatible)
        {
            //Keyboard.KeyDown += Keyboard_KeyDown;

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
            SpriteManager.Instance.LoadSpriteAtlas("ui", "ui");
            SpriteManager.Instance.LoadSpriteAtlas("font5", "font5");
            WidgetManager.LoadUI(AssetManager.GetAssetTextFile("ui.xml"));


            m_gameController.AddWindow(new TestWindow());
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
                            }//,
                            //"COMPAT_QUALITY"
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

            if (m_gameController != null)
            {
                m_gameController.Update();
            }
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
        }

        public static void Main(string[] args)
        {
            SampleWindow window = new SampleWindow();
            window.Run();
        }
    }
}
