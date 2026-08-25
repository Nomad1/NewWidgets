#if MINI
using System;
using System.Numerics;
using OpenTK.Graphics.OpenGL;
using RunMobile.Graphics;
using RunMobile.Utility;

#if USE_NUMERICS || NETSTANDARD || NET47
using Matrix = System.Numerics.Matrix4x4;
#else
using Matrix = System.Numerics.Matrix4x3;
#endif

namespace RunMobile.OpenTK.OpenGL
{
    /// <summary>
    /// This is a very simple and clutchy class implementing GraphicsManager
    /// to work with 2D sprites and OpenTK version of GL
    /// it could be used only for old 2D games and compatibility with MINI interfaces
    /// otherwise use non-MINI version of RunMobile.OpenTK
    /// </summary>
    public class GLHelper : GraphicsManager
    {
#if OPENGLES
        public const int VAOVersion = 200; // VAO appears in OpenGL ES 2.0
        public const int ModelsVersion = 300; // Allow models starting with OpenGL ES 3.0
        public const int CompatShaders = 200; // for OpenGL ES 2.0 we restrict use of models, however they are supported
        public const int OneComponentTextureVersion = 200;
        public const int BaseInstanceVersion = 310;
#else
        public const int ModelsVersion = 310; // Model support is possible with any version but to conserve performance and depth buffer I'm restricting models to version 3.1
        public const int VAOVersion = 300; // VAO appears in OpenGL 3.0
        public const int CompatShaders = 210; // for OpenGL 2.1 we need to use simplier shaders
        public const int OneComponentTextureVersion = 300;
        public const int BufferStorageVersion = 430;
        public const int BaseInstanceVersion = 420;
#endif

        private static int s_version;
        private static int s_realVersion;
        private static int s_maxTextureSize;

        public static new GLHelper Instance
        {
            get { return GraphicsManager.Instance as GLHelper; }
        }

        public static int Version
        {
            get { return s_version; }
        }

        public static int RealVersion
        {
            get { return s_realVersion; }
        }

        public static int MaxTextureSize
        {
            get { return s_maxTextureSize; }
        }

        public static void Init(int width, int height)
        {
            GraphicsManager.Instance = new GLHelper(width, height);
        }
        
        private readonly int m_width;
        private readonly int m_height;
        private readonly int[] m_background = { 0, 0, 0, 0 };

        private readonly bool m_supportPackedTextures;

        public override int ScreenWidth
        {
            get { return m_width; }
        }

        public override int ScreenHeight
        {
            get { return m_height; }
        }

        public bool SupportPackedTextures
        {
            get { return m_supportPackedTextures; }
        }

        public override bool SupportsModels
        {
            get { return false; }
        }

        public override bool SupportsPoints
        {
            get { return false; }
        }

        public override bool SupportsEngineComposer
        {
            get { return false; }
        }

        public override float FrameTime { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override IGraphicsCamera Camera { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override IGraphicsCamera DefaultCamera => throw new NotImplementedException();

        public GLHelper(int width, int heigth, bool topDown = false)
        {
            m_width = width;//width > heigth ? width : heigth;
            m_height = heigth;//width > heigth ? heigth : width;

           

            //if (compatVersion != 0 && s_version > compatVersion)
            //{
            //    s_version = compatVersion;
            //    LogConsole.WriteLine(LogLevel.SYSTEM, "Force compatibility mode, OpenGL version set to {0}, real version {1}", s_version, s_realVersion);
            //}
            //else
            //    LogConsole.WriteLine(LogLevel.SYSTEM, "OpenGL version {0}", s_version);


            GL.Viewport(0, 0, m_width, m_height);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();

            if (topDown)
                GL.Ortho(0.0f, width, 0, heigth, -1, 1);
            else
                GL.Ortho(0.0f, width, heigth, 0.0f, -100f, 100f);

            GL.Enable(EnableCap.Dither);
            GL.Disable(EnableCap.Lighting);
            //GL11.Disable(EnableCap.Multisample);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.AlphaTest);

            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.ScissorTest);

            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.TextureCoordArray);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            GL.Scissor(0, 0, m_width, m_height);
            
            LogConsole.WriteLine(LogLevel.SYSTEM, "OpenGL init width viewport {0}x{1}", m_width, m_height);

            ErrorCode error = GL.GetError();
            if (error != ErrorCode.NoError)
            {
                LogConsole.WriteLine(LogLevel.ERROR, "OpenGL initialization error: " + error);
                System.Diagnostics.Debug.Assert(false, "Error initializing OpenGL!");
            }

            int[] maxTextureSize = new int[1];
            GL.GetInteger(GetPName.MaxTextureSize, maxTextureSize);

            LogConsole.WriteLine(LogLevel.PRINT, "Maximum texture size: " + maxTextureSize[0]);
            if (maxTextureSize[0] < 2048)
                System.Diagnostics.Debug.Assert(false, "Your system does not support game textures. Please install latest graphics drivers or upgrade your video card!");

            s_maxTextureSize = maxTextureSize[0];

            string version = "";
            try
            {
                version = GL.GetString(StringName.Version);
                int minorVersion = 0;
                int majorVersion = 0;

                string[] split = version.Split(new char[] { '.' }, 2);
                if (split.Length == 2) // have dot
                {
                    string[] majorSplit = split[0].Split(' ', '-', ',', '.');
                    string[] minorSplit = split[1].Split(' ', '-', ',', '.');

                    minorVersion = int.Parse(minorSplit[0]);
                    majorVersion = int.Parse(majorSplit[majorSplit.Length - 1]);
                }

                s_version = (int)(float.Parse(majorVersion + "." + minorVersion, System.Globalization.CultureInfo.InvariantCulture.NumberFormat) * 100 + 0.5f);

                LogConsole.WriteLine(LogLevel.SYSTEM, "Working with OpenGL {0}.{1} ({2})", majorVersion, minorVersion, version);
            }
            catch (Exception ex)
            {
                LogConsole.WriteLine(LogLevel.ERROR, "Failed to get OpenGL version, error {0} Returned string: {1}", ex.Message, version);
                s_version = 200;
            }

            s_realVersion = s_version;

            int [] param = new int[1];
            GL.GetInteger(GetPName.NumCompressedTextureFormats, param);
            int [] formats = new int[param[0]];

            GL.GetInteger(GetPName.CompressedTextureFormats, formats);

            for(int i=0; i < formats.Length; i++)
                LogConsole.WriteLine(LogLevel.PRINT, "Supported format: 0x{0:X}: {1}", formats[i], (All)formats[i]);

            LogConsole.WriteLine(LogLevel.PRINT, "Extensions: " + GL.GetString(StringName.Extensions));
        }

        public void Flush()
        {
            GL.Scissor(0, 0, m_width, m_height);
            //GLSprite.FlushBatch();
            GL.Flush ();
        }

        public void SetBackgroundColor(int red, int green, int blue, int alpha)
        {
            m_background[0] = red;
            m_background[1] = green;
            m_background[2] = blue;
            m_background[3] = alpha;
        }

        public void ClearColor()
        {
            GL.ClearColor(0, 0, 0, 0);
            //if (m_globalAdd)
            //    GL.ClearColor(m_globalTint[0] / 255.0f, m_globalTint[1] / 255.0f, m_globalTint[2] / 255.0f, m_globalTint[3] / 255.0f);
            //else
            //    GL.ClearColor(m_background[0] / 255.0f, m_background[1] / 255.0f, m_background[2] / 255.0f, m_background[3] / 255.0f);
        }
        
        public override void SetClipRect(int x, int y, int width, int height)
        {
            GL.Scissor(x, ScreenHeight - y - height - 1, width, height);
        }

        public override void CancelClipRect()
        {
            SetClipRect(0, 0, m_width, m_height);
        }

        public static ErrorCode GetError()
        {
            return GL.GetError();
        }

        public override IGraphicsTexture CreateTexture(TextureData texture)
        {
            return new GLTexture(texture);
        }

#if OLD
        public static readonly int GL_TEXTURE_CROP_RECT_OES = 4098;
        private static readonly int[] s_textureCropOesTiv = new int[4];

        private static readonly short[] s_indexValues = new short[] { 0, 1, 2, 1, 2, 3 };
        private static readonly float[] s_vertexValues = new float[] { -0.5f, 0.5f, 0.5f, 0.5f, -0.5f, -0.5f, 0.5f, -0.5f };
        
        public void DrawTexfOES(float x, float y, float z, float w, float h)
        {
            GL.PushMatrix();
            GL.LoadIdentity();

            GL.Translate(w / 2.0f + x, h / 2.0f + y, 0.0f);

            GL.Scale(w, -h, 1.0f);

            int [] textureId = new int[1];
            GL.GetInteger(GetPName.TextureBinding2D, textureId); //GL_TEXTURE_BINDING_2D

            int[] tiv = s_textureCropOesTiv;

            int texScale = 1;// s_textureScale.ContainsKey(textureId[0]) ? s_textureScale[textureId[0]] : 1;
            int[] texW = new int[1];
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureWidth, texW);
            texW[0] *= texScale;
            int[] texH = new int[1];
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureHeight, texH);
            texH[0] *= texScale;

            float[] texCoordValues = new float[8];

            float left = 1.0f - (tiv[0] + tiv[2]) / (float)texW[0];
            float bottom = 1.0f - tiv[1] / (float)texH[0];
            float right = 1.0f - tiv[0] / (float)texW[0];
            float top = 1.0f - (tiv[1] + tiv[3]) / (float)texH[0];

            texCoordValues[0] = right;
            texCoordValues[2] = left;
            texCoordValues[4] = right;
            texCoordValues[6] = left;

            texCoordValues[1] = bottom;
            texCoordValues[3] = bottom;
            texCoordValues[5] = top;
            texCoordValues[7] = top;

            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.TextureCoordArray);
            GL.VertexPointer(2, VertexPointerType.Float, 0, s_vertexValues);
            GL.TexCoordPointer(2, TexCoordPointerType.Float, 0, texCoordValues);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedShort, s_indexValues);

            GL.PopMatrix();
        }
#else
        private readonly static float[] m_vertexValues = new float[8];
        private readonly static float[] m_texCoordValues = new float[8];

        public override void DrawTexture(IGraphicsTexture texture, ref SpriteData frame, ref Matrix transform, uint color, TextureFlags flags, Vector2 pivot)
        {
            ((GLTexture)texture).Bind();
#if USE_NUMERICS || NETSTANDARD || NET47
            Vector3 oneVectorX = new Vector3(transform.M11, transform.M12, transform.M13);
            Vector3 oneVectorY = new Vector3(transform.M21, transform.M22, transform.M23);
#else
            Vector3 oneVectorX = transform.Row1;
            Vector3 oneVectorY = transform.Row2;
#endif

            Vector3 shift = oneVectorX * (+pivot.X * frame.OriginalWidth - frame.OffsetX) + oneVectorY * (+pivot.Y * frame.OriginalHeight - frame.OffsetY);

            Vector3 width = oneVectorX * frame.Width;
            Vector3 height = oneVectorY * frame.Height;

            float iwidth = 1.0f / ((GLTexture)texture).Data.Width;
            float iheight = 1.0f / ((GLTexture)texture).Data.Height;

            Vector4 texCoord = new Vector4(
                frame.X * iwidth, frame.Y * iheight,
                (frame.X + frame.Width) * iwidth, (frame.Y + frame.Height) * iheight);


            Vector3 v3 = transform.Translation - shift;
            Vector3 v0 = v3 + height;
            Vector3 v2 = v3 + width;
            Vector3 v1 = v2 + height;

            byte red = (byte)((color & 0x00ff0000) >> 16);
            byte green = (byte)((color & 0x0000ff00) >> 8);
            byte blue = (byte)(color & 0x000000ff);
            byte alpha = (byte)((color & 0xff000000) >> 24);

            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (float)All.Modulate);
            GL.Color4(red, green, blue, alpha);

            //GLHelper.Instance.DrawSimpleQuad(
            //   v0, v1, v2, v3,
            //   flags,
            //   ((GLTextureRect)textures[0]).TexCoords,
            //   color,
            //   textures[0].Texture.TextureId, textures.Length == 2 ? textures[1].Texture.TextureId : 0,
            //   (GLShader)shader);

            //quads[0] = new SimpleQuadVertexData(v0.X, v0.Y, v0.Z, texCoord.X, texCoord.W, (uint)color, flag, pointers.TextureSlotId, m_currentScissor);
            //quads[1] = new SimpleQuadVertexData(v1.X, v1.Y, v1.Z, texCoord.Z, texCoord.W, (uint)color, flag, pointers.TextureSlotId, m_currentScissor);
            //quads[2] = new SimpleQuadVertexData(v2.X, v2.Y, v2.Z, texCoord.Z, texCoord.Y, (uint)color, flag, pointers.TextureSlotId, m_currentScissor);
            //quads[3] = new SimpleQuadVertexData(v3.X, v3.Y, v3.Z, texCoord.X, texCoord.Y, (uint)color, flag, pointers.TextureSlotId, m_currentScissor);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.PushMatrix();
            GL.LoadIdentity();

            GL.EnableClientState(ArrayCap.VertexArray);

            m_vertexValues[0] = v0.X;
            m_vertexValues[1] = v0.Y;
            m_vertexValues[2] = v1.X;
            m_vertexValues[3] = v1.Y;
            m_vertexValues[4] = v3.X;
            m_vertexValues[5] = v3.Y;
            m_vertexValues[6] = v2.X;
            m_vertexValues[7] = v2.Y;

            GL.VertexPointer(2, VertexPointerType.Float, 0, m_vertexValues);

            GL.EnableClientState(ArrayCap.TextureCoordArray);

            m_texCoordValues[0] = texCoord.X;
            m_texCoordValues[1] = texCoord.W;
            m_texCoordValues[2] = texCoord.Z;
            m_texCoordValues[3] = texCoord.W;
            m_texCoordValues[4] = texCoord.X;
            m_texCoordValues[5] = texCoord.Y;
            m_texCoordValues[6] = texCoord.Z;
            m_texCoordValues[7] = texCoord.Y;

            GL.TexCoordPointer(2, TexCoordPointerType.Float, 0, m_texCoordValues);

            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            GL.PopMatrix();
        }

        public override IGraphicsCamera CreateCamera(Vector3 position, Vector3 target, float fov, string name)
        {
            throw new NotImplementedException();
        }

        public override string GetProfilerDump()
        {
            return string.Empty;
        }

        public override void SetProfilerValue(string name, int color, double value, bool graph)
        {
            throw new NotImplementedException();
        }
#endif
    }
}
#endif