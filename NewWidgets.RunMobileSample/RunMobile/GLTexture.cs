using System;
using RunMobile.Utility;
using RunMobile.Graphics;

#if MINI
using OpenTK.Graphics.OpenGL;

using SubTextureTarget = OpenTK.Graphics.OpenGL.TextureTarget;
using TextureComponentCount = OpenTK.Graphics.OpenGL.PixelInternalFormat;


#elif OPENGLES
using OpenTK.Graphics.ES30;

using SubTextureTarget = OpenTK.Graphics.ES30.TextureTarget2d;
#else
using OpenTK.Graphics.OpenGL4;

using SubTextureTarget = OpenTK.Graphics.OpenGL4.TextureTarget;
using TextureComponentCount = OpenTK.Graphics.OpenGL4.PixelInternalFormat;
#endif

namespace RunMobile.OpenTK.OpenGL
{
    internal class GLTexture : IGraphicsTexture
    {
        private struct MipLevel
        {
            public readonly int Width;
            public readonly int Height;
            public readonly int Depth;
            public readonly int DataOffset;
            public readonly int DataSize;

            public MipLevel(int width, int height, int depth, int offset, int size)
            {
                Width = width;
                Height = height;
                Depth = depth;
                DataOffset = offset;
                DataSize = size;
            }
        }

        private readonly TextureData m_texture;
        private readonly int m_targetMipMask;
        private readonly MipLevel[] m_mipMaps;
        private readonly int m_unpack;
        private readonly TextureTarget m_target;


        private readonly SizedInternalFormat m_internalFormat;
        private readonly PixelFormat m_pixelFormat;
        private readonly PixelType m_pixelType;
        private readonly bool m_compressed;

        private uint m_id;
        private int m_mipMask;
        private int m_buffer;
        private IntPtr m_bufferSync;
        private IntPtr m_data;

        private bool m_disposed;

        public uint TextureId
        {
            get { return m_id; }
        }

        public bool IsDisposed
        {
            get { return m_disposed; }
        }

        public TextureData Data
        {
            get { return m_texture; }
        }

        public bool NeedToContinueLoading
        {
            get { return m_texture.DataHandle != null && GLHelper.Version >= GLHelper.VAOVersion && ((m_mipMask != m_targetMipMask && m_buffer != 0) || (m_bufferSync != IntPtr.Zero)); }
        }

        public GLTexture(TextureData texture)
        {
            m_texture = texture;
            m_id = 0;

            m_internalFormat = (SizedInternalFormat)m_texture.Format;
            m_pixelFormat = (PixelFormat)m_texture.Format;
            m_pixelType = PixelType.UnsignedByte;
            m_compressed = false;

            switch (m_texture.Format)
            {
                case TextureFormat.GL_UNSIGNED_BYTE:
                case TextureFormat.GL_RGBA:
                    m_internalFormat = SizedInternalFormat.Rgba8;
                    m_pixelFormat = PixelFormat.Rgba;
                    break;
                case TextureFormat.GL_BGRA:
                    m_internalFormat = SizedInternalFormat.Rgba8;
                    break;
                case TextureFormat.GL_RGB:
                    m_internalFormat = SizedInternalFormat.Rgba8;
                    m_pixelFormat = PixelFormat.Rgb;
                    break;
                case TextureFormat.GL_UNSIGNED_SHORT_4_4_4_4:
                    m_internalFormat = SizedInternalFormat.Rgba8;
                    m_pixelFormat = PixelFormat.Rgba;
                    m_pixelType = PixelType.UnsignedShort4444;
                    break;
                case TextureFormat.GL_UNSIGNED_SHORT_5_5_5_1:
                    m_internalFormat = SizedInternalFormat.Rgba8;
                    m_pixelFormat = PixelFormat.Rgba;
                    m_pixelType = PixelType.UnsignedShort5551;
                    break;
                case TextureFormat.GL_UNSIGNED_SHORT_5_6_5:
                    m_internalFormat = SizedInternalFormat.Rgba8; // previously was (SizedInternalFormat)All.Rgb
                    m_pixelFormat = PixelFormat.Rgb;
                    m_pixelType = PixelType.UnsignedShort565;
                    break;
                case TextureFormat.GL_DEPTH_COMPONENT:
                    break;
                case TextureFormat.GL_DEPTH_COMPONENT16:
                    m_pixelFormat = PixelFormat.DepthComponent;
                    m_pixelType = PixelType.UnsignedShort;
                    break;
                case TextureFormat.GL_RED:
                case TextureFormat.GL_ALPHA:
                case TextureFormat.GL_LUMINANCE:
                    m_internalFormat = SizedInternalFormat.R8;
                    m_pixelFormat = GLHelper.RealVersion >= GLHelper.OneComponentTextureVersion || m_target == SubTextureTarget.Texture3D ? PixelFormat.Red : PixelFormat.Luminance;
                    break;
                case TextureFormat.GL_R16:
                    m_pixelFormat = PixelFormat.Red;
                    m_pixelType = PixelType.UnsignedShort;
                    break;
                case TextureFormat.GL_R16_SNORM:
                    m_pixelFormat = PixelFormat.Red;
                    m_pixelType = PixelType.Short;
                    break;
                case TextureFormat.GL_R16F:
                    m_pixelFormat = PixelFormat.Red;
                    m_pixelType = PixelType.HalfFloat;
                    break;
                case TextureFormat.GL_RGBA16F:
                    m_pixelFormat = PixelFormat.Rgba;
                    m_pixelType = PixelType.HalfFloat;
                    break;
                case TextureFormat.GL_RG:
                case TextureFormat.GL_LUMINANCE_ALPHA:
                    m_internalFormat = SizedInternalFormat.Rg8;
                    m_pixelFormat = GLHelper.RealVersion >= GLHelper.OneComponentTextureVersion || m_target == SubTextureTarget.Texture3D ? PixelFormat.Rg : PixelFormat.LuminanceAlpha;
                    break;
                default:
                    m_pixelFormat = (PixelFormat)m_texture.Format;
                    m_internalFormat = (SizedInternalFormat)m_texture.Format;
                    m_compressed = true;
                    break;
            }

            m_mipMaps = GetMipLevels(texture, out m_unpack, out m_targetMipMask);
            m_target = texture.Depth <= 1 ? TextureTarget.Texture2D : TextureTarget.Texture3D;
        }

        ~GLTexture()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                m_disposed = true;

                if (disposing)
                {
                    // dispose managed state (managed objects).
                }

                if (m_id != 0)
                {
                    LogConsole.WriteLine(LogLevel.PRINT, "Disposing texture {0} id {1}", m_texture.Name, m_id);


                    TextureManager.Instance.RunDelayed(Unbind);
                    //Unbind();
                }
            }
        }

        public void Unbind()
        {
            LogConsole.WriteLine(LogLevel.PRINT, "Unbinding texture {0} id {1}", m_texture.Name, m_id);

            if (m_buffer != 0)
            {
                GL.DeleteBuffers(1, ref m_buffer);
                m_buffer = 0;
            }

            if (m_bufferSync != IntPtr.Zero)
            {
                GL.DeleteSync(m_bufferSync);
                m_bufferSync = IntPtr.Zero;
            }

            if (m_id != 0)
            {
#if !MINI
                for (int i = 0; i < GLHelper.Instance.CurrentTextures.Length; i++)
                {
                    if (GLHelper.Instance.CurrentTextures[i] == m_id)
                        GLHelper.Instance.CurrentTextures[i] = 0;
                }
#endif
                GL.DeleteTextures(1, new uint[] { m_id & 0x7fffffff });
                m_id = 0;
            }

            m_mipMask = 0;
        }

        public int PreLoad()
        {
            if (m_texture.DataHandle == null)
                return 0;

            if (m_buffer != 0)
                return 0;

            IntPtr data = m_texture.DataHandle.GetPointer();
            try
            {
                if (GLHelper.Version >= GLHelper.VAOVersion)
                {
                    GL.GenBuffers(1, out m_buffer);
                    GL.BindBuffer(BufferTarget.PixelUnpackBuffer, m_buffer);

                    GL.BufferData(BufferTarget.PixelUnpackBuffer, new IntPtr(m_texture.DataHandle.Length), data, BufferUsageHint.StaticDraw);
                }
                else
                {
                    m_data = data;
                }
                LogConsole.WriteLine(LogLevel.PRINT, "Pre loaded texture {0}, length {1}, buffer {2}, type {3}, format {4}", m_texture.Name, m_texture.DataHandle.Length, m_buffer, m_texture.DataHandle, m_texture.Format);
                return m_texture.DataHandle.Length;
            }
            finally
            {
                if (GLHelper.Version >= GLHelper.VAOVersion)
                    m_texture.DataHandle.ReleasePointer(data);
            }
        }

        private static MipLevel[] GetMipLevels(TextureData texture, out int unpack, out int mask)
        {
            int width = texture.Width;
            int height = texture.Height;
            int depth = texture.Depth;

            if (depth < 1)
                depth = 1;

            int blockSize; // size of block width*height*bpp
            int blockSizeX = 1;
            int blockSizeY = 1;
            int blockSizeZ = 1;
            int minBlocks = 1;
            int addSize = 0;

            unpack = 1;
            mask = 0;

            switch (texture.Format)
            {
                case TextureFormat.GL_UNSIGNED_SHORT_5_6_5:
                case TextureFormat.GL_UNSIGNED_SHORT_5_5_5_1:
                    blockSize = 16;
                    unpack = 2;
                    break;
                case TextureFormat.GL_UNSIGNED_SHORT_4_4_4_4:
                    blockSize = 16;
                    unpack = 4;
                    break;
                case TextureFormat.GL_LUMINANCE_ALPHA:
                case TextureFormat.GL_RG:
                case TextureFormat.GL_DEPTH_COMPONENT16:
                case TextureFormat.GL_R16:
                case TextureFormat.GL_R16F:
                case TextureFormat.GL_R16_SNORM:
                    blockSize = 16;
                    unpack = 1;
                    break;
                case TextureFormat.GL_RGBA16F:
                    blockSize = 64;
                    unpack = 1;
                    break;
                case TextureFormat.GL_ALPHA:
                case TextureFormat.GL_LUMINANCE:
                case TextureFormat.GL_RED:
                case TextureFormat.GL_DEPTH_COMPONENT:
                    blockSize = 8;
                    unpack = 1;
                    break;
                case TextureFormat.GL_RGB:
                    blockSize = 24;
                    unpack = 1;
                    break;
                case TextureFormat.COMPRESSED_RGB_PVRTC_4BPPV1_IMG:
                case TextureFormat.COMPRESSED_RGBA_PVRTC_4BPPV1_IMG:
                case TextureFormat.COMPRESSED_RGBA_PVRTC_4BPPV2_IMG:
                    blockSizeX = blockSizeY = 4;
                    blockSize = blockSizeX * blockSizeY * 4;
                    minBlocks = 2;
                    break;
                case TextureFormat.COMPRESSED_RGB_PVRTC_2BPPV1_IMG:
                case TextureFormat.COMPRESSED_RGBA_PVRTC_2BPPV1_IMG:
                case TextureFormat.COMPRESSED_RGBA_PVRTC_2BPPV2_IMG:
                    blockSizeX = 8;
                    blockSizeY = 4;
                    blockSize = blockSizeX * blockSizeY * 2;
                    minBlocks = 2;
                    break;
                case TextureFormat.GL_PALETTE8_RGBA8_OES:
                    blockSize = 8;
                    addSize = 1024; // 256 palette entries
                    break;
                case TextureFormat.ATC_RGB_AMD:
                    blockSizeX = blockSizeY = 4;
                    blockSize = blockSizeX * blockSizeY * 4;
                    break;
                case TextureFormat.ATC_RGBA_EXPLICIT_ALPHA_AMD:
                case TextureFormat.ATC_RGBA_INTERPOLATED_ALPHA_AMD:
                    blockSizeX = blockSizeY = 4;
                    blockSize = blockSizeX * blockSizeY * 8;
                    break;
                case TextureFormat.COMPRESSED_LUMINANCE_ALPHA_LATC2_EXT:
                case TextureFormat.COMPRESSED_SIGNED_RG_RGTC2:
                case TextureFormat.COMPRESSED_RED_GREEN_RGTC2_EXT:
                    blockSizeX = blockSizeY = 4;
                    blockSize = blockSizeX * blockSizeY * 8;
                    break;
                case TextureFormat.COMPRESSED_RGBA_S3TC_DXT5_EXT:
                case TextureFormat.COMPRESSED_RGBA_S3TC_DXT3_EXT:
                case TextureFormat.COMPRESSED_RGBA_S3TC_DXT1_EXT:
                    blockSizeX = blockSizeY = 4;
                    blockSize = blockSizeX * blockSizeY * 8;
                    break;
                case TextureFormat.COMPRESSED_RGB_S3TC_DXT1_EXT:
                    blockSizeX = blockSizeY = 4;
                    blockSize = blockSizeX * blockSizeY * 4;
                    break;
                case TextureFormat.COMPRESSED_RGBA8_ETC2_EAC:
                case TextureFormat.COMPRESSED_RG11_EAC:
                    blockSizeX = blockSizeY = 4;
                    blockSize = blockSizeX * blockSizeY * 8;
                    break;
                case TextureFormat.COMPRESSED_RGBA_ASTC_12x12_KHR:
                    blockSizeX = blockSizeY = 12;
                    blockSize = 128;
                    break;
                case TextureFormat.COMPRESSED_RGBA_ASTC_4x4_KHR:
                    blockSizeX = blockSizeY = 4;
                    blockSize = 128;
                    break;
                case TextureFormat.COMPRESSED_RGBA_ASTC_6x6_KHR:
                    blockSizeX = blockSizeY = 6;
                    blockSize = 128;
                    break;
                case TextureFormat.COMPRESSED_RGBA_ASTC_8x8_KHR:
                    blockSizeX = blockSizeY = 8;
                    blockSize = 128;
                    break;
                case TextureFormat.COMPRESSED_RGBA_ASTC_10x10_KHR:
                    blockSizeX = blockSizeY = 10;
                    blockSize = 128;
                    break;
                case TextureFormat.ETC1_RGB8_OES:
                case TextureFormat.COMPRESSED_RGB8_ETC2:
                case TextureFormat.COMPRESSED_RGB8_PUNCHTHROUGH_ALPHA1_ETC2:
                case TextureFormat.COMPRESSED_R11_EAC:
                    blockSizeX = blockSizeY = 4;
                    blockSize = blockSizeX * blockSizeY * 4;
                    break;
                default:
                    blockSize = 32;
                    unpack = 4;
                    break;
            }

            MipLevel[] mipMaps = new MipLevel[texture.MipMaps + 1];

            int offset = 0;
            int faces = 0;
            int arrayElements = 0;

            {
                int iwidth = (int)width;
                int iheight = (int)height;
                int idepth = Math.Max((int)depth, 1);
                int ifaces = Math.Max((int)faces, 1);
                int iarrayElements = Math.Max((int)arrayElements, 1);

                for (int i = 0; i < mipMaps.Length; i++)
                {
                    mask |= 1 << i;

                    if (texture.HaveBlockSize)
                        offset += 4;

                    int totalSize = 0;

                    for (int a = 0; a < iarrayElements; a++)
                        for (int f = 0; f < ifaces; f++)
                        {
                            totalSize += (Math.Max((iwidth + blockSizeX - 1) / blockSizeX, minBlocks)
                                        * Math.Max((iheight + blockSizeY - 1) / blockSizeY, minBlocks)
                                        * Math.Max((idepth + blockSizeZ - 1) / blockSizeZ, minBlocks)
                                        * blockSize / 8
                                        + addSize);

                            totalSize += totalSize % 4;
                        }

                    totalSize += totalSize % 4;

                    mipMaps[i] = new MipLevel(iwidth, iheight, idepth, offset, totalSize);

                    offset += totalSize;

                    iwidth = Math.Max(iwidth >> 1, 1);
                    iheight = Math.Max(iheight >> 1, 1);
                    idepth = Math.Max(idepth >> 1, 1);
                }
            }


            return mipMaps;
        }

        private void LoadTextureData()
        {
            if (m_texture.DataHandle == null)
                return;

            if (GLHelper.Version >= GLHelper.VAOVersion && m_mipMask == m_targetMipMask && m_buffer != 0 && m_bufferSync != IntPtr.Zero)
            {
                if (GL.IsSync(m_bufferSync))
                {
                    GL.DeleteSync(m_bufferSync);
                    m_bufferSync = IntPtr.Zero;

                    GL.DeleteBuffers(1, ref m_buffer);
                    m_buffer = 0;

                    if (m_data != IntPtr.Zero)
                    {
                        m_texture.DataHandle.ReleasePointer(m_data);
                        m_data = IntPtr.Zero;
                        LogConsole.WriteLine(LogLevel.PRINT, "Cleared buffers for texture {0}", m_id);
                    }

                    LogConsole.WriteLine(LogLevel.PRINT, "Cleared buffers for texture {0}", m_id);
                }
                return;
            }

            GL.BindTexture(m_target, m_id & 0x7fffffff);

            if (GLHelper.Version >= GLHelper.VAOVersion)
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, m_buffer);

            int baseLevel = 0;

            for (int i = m_mipMaps.Length - 1; i >= 0; i--)
            {
                if ((m_mipMask & (1 << i)) != 0)
                    continue;

                baseLevel = i;
                m_mipMask |= (1 << i);
                MipLevel mipMap = m_mipMaps[i];


                IntPtr tdata;
                if (GLHelper.Version >= GLHelper.VAOVersion)
                    tdata = (IntPtr)mipMap.DataOffset;
                else
                    tdata = m_data + mipMap.DataOffset;

                int width = mipMap.Width;
                int height = mipMap.Height;
                int depth = mipMap.Depth;
                int dataSize = mipMap.DataSize;

                //LogConsole.WriteLine(LogLevel.PRINT, "Going to load 2d texture {0}[{1}], size {2}, {3}, data length {4} format {5}, data handle: {6}", m_texture.Name, m_id, width, height, dataSize, m_texture.Format, m_texture.DataHandle);

                

                if (m_compressed)
                {
                    if (m_target == SubTextureTarget.Texture2D)
                        GL.CompressedTexSubImage2D((SubTextureTarget)m_target, i, 0, 0, width, height, m_pixelFormat, dataSize, tdata);
                    else
                        GL.CompressedTexSubImage3D((SubTextureTarget)m_target, i, 0, 0, 0, width, height, depth, m_pixelFormat, dataSize, tdata);
                }
                else
                {
                    if (m_target == SubTextureTarget.Texture2D)
                        GL.TexSubImage2D((SubTextureTarget)m_target, i, 0, 0, width, height, m_pixelFormat, m_pixelType, tdata);
                    else
                        GL.TexSubImage3D((SubTextureTarget)m_target, i, 0, 0, 0, width, height, depth, m_pixelFormat, m_pixelType, tdata);
                }

                ErrorCode error = GLHelper.GetError();
                if (error != ErrorCode.NoError)
                {
                    LogConsole.WriteLine(LogLevel.ERROR, "LoadTextureData GLError: {0}", error);
                    return;
                }

#if DEBUG
                LogConsole.WriteLine(LogLevel.PRINT, "Loaded mipmap for texture {0}[{1}], size {2}, {3}, data length {4} format {5}, data handle: {6}", m_texture.Name, i, width, height, dataSize, m_texture.Format, m_texture.DataHandle);
#endif
                if (NeedToContinueLoading) // checks both for GL version, capabilities and if the texture is not fully loaded yet
                    break;
            }

            if (GLHelper.Version >= GLHelper.VAOVersion)
            {
                GL.TexParameter(m_target, TextureParameterName.TextureBaseLevel, baseLevel);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
            }

            if (m_mipMask == m_targetMipMask)
            {
                LogConsole.WriteLine(LogLevel.PRINT, "Texture {0} loading complete", m_texture.Name);

                if (GLHelper.Version >= GLHelper.VAOVersion)
                {
                    // we need to create sync to make sure buffer is not removed until the textures are loaded
                    m_bufferSync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);
                }
                else
                {
                    m_texture.DataHandle.ReleasePointer(m_data);
                    m_data = IntPtr.Zero;
                }
            }
        }

        public bool Load()
        {
            GL.BindTexture(m_target, m_id & 0x7fffffff);

#if DEBUG
            //Stopwatch startTime = new Stopwatch();
            //startTime.Start();
#endif
            ErrorCode error;

            if (m_buffer == 0 && m_texture.DataHandle != null)
                PreLoad();

            
            if (m_texture.DataHandle != null)
            {
                // allocate immutable storage for static textures
#if !MINI
                if (m_texture.Depth <= 1)
                    GL.TexStorage2D((TextureTarget2d)m_target, m_mipMaps.Length, m_internalFormat, m_texture.Width, m_texture.Height);
                else
                    GL.TexStorage3D((TextureTarget3d)m_target, m_mipMaps.Length, m_internalFormat, m_texture.Width, m_texture.Height, m_texture.Depth);

                error = GLHelper.GetError();
                if (error != ErrorCode.NoError)
                {
                    LogConsole.WriteLine(LogLevel.ERROR, "TexStorage GLError: {0}", error);
                    return false;
                }
#else
                for (int i = m_mipMaps.Length - 1; i >= 0; i--)
                {
                    MipLevel mipMap = m_mipMaps[i];
                    int width = mipMap.Width;
                    int height = mipMap.Height;
                    int depth = mipMap.Depth;

                    if (m_compressed)
                    {
                        if (m_texture.Depth <= 1)
                            GL.CompressedTexImage2D((SubTextureTarget)m_target, i, (TextureComponentCount)m_internalFormat, width, height, 0, mipMap.DataSize, IntPtr.Zero);
                        else
                            GL.CompressedTexImage3D((SubTextureTarget)m_target, i, (TextureComponentCount)m_internalFormat, width, height, depth, 0, mipMap.DataSize, IntPtr.Zero);
                    }
                    else
                    {
                        if (m_texture.Depth <= 1)
                            GL.TexImage2D((SubTextureTarget)m_target, i, (TextureComponentCount)m_internalFormat, width, height, 0, m_pixelFormat,
                                m_pixelType, IntPtr.Zero);
                        else
                            GL.TexImage3D((SubTextureTarget)m_target, i, (TextureComponentCount)m_internalFormat, width, height, depth, 0, m_pixelFormat,
                                m_pixelType, IntPtr.Zero);
                    }
                }
#endif
                LoadTextureData();
            }
            else
            {
                // allocate mutable storage for dynamic textures
                {
                    if (m_texture.Depth <= 1)
                        GL.TexImage2D((SubTextureTarget)m_target, 0, (TextureComponentCount)m_internalFormat, m_texture.Width, m_texture.Height, 0, m_pixelFormat,
                            m_pixelType, IntPtr.Zero);
                    else
                        GL.TexImage3D((SubTextureTarget)m_target, 0, (TextureComponentCount)m_internalFormat, m_texture.Width, m_texture.Height, m_texture.Depth, 0, m_pixelFormat,
                            m_pixelType, IntPtr.Zero);
                }
            }

            bool smooth = !m_texture.IsNoSmooth;
            bool useMipMaps = smooth;

            if (m_mipMaps.Length <= 1)
                useMipMaps = false;

            TextureMinFilter minMode = smooth ? useMipMaps ? TextureMinFilter.LinearMipmapLinear : TextureMinFilter.Linear : useMipMaps ? TextureMinFilter.NearestMipmapNearest : TextureMinFilter.Nearest;
            TextureMagFilter magMode = smooth ? TextureMagFilter.Linear : TextureMagFilter.Nearest;

            if (useMipMaps && m_mipMask == 0)
                minMode = TextureMinFilter.Linear;

            GL.TexParameter(m_target, TextureParameterName.TextureMinFilter, (int)minMode);
            GL.TexParameter(m_target, TextureParameterName.TextureMagFilter, (int)magMode);

#if !OPENGLES && !MINI
            if (smooth && GLHelper.Instance.AnisotropyLevel > 0 && GLHelper.Version >= GLHelper.VAOVersion)
                GL.TexParameter(m_target, (TextureParameterName)All.TextureMaxAnisotropyExt, GLHelper.Instance.AnisotropyLevel);
#endif

            if (m_mipMaps.Length > 1)
                GL.TexParameter(m_target, TextureParameterName.TextureMaxLevel, m_mipMaps.Length - 1);
            else
                GL.TexParameter(m_target, TextureParameterName.TextureMaxLevel, 0);

            error = GLHelper.GetError();
            if (error != ErrorCode.NoError)
            {
                LogConsole.WriteLine(LogLevel.ERROR, "Bind Parameters GLError: {0}", error);
                return false;
            }

#if DEBUG
            //startTime.Stop();
            //LogConsole.WriteLine(LogLevel.PRINT, "Load took {0} for {1}", startTime.Elapsed, m_texture.Name);
#endif

            return true;
        }

        public uint Init()
        {
            if (m_id <= 0)
            {
                uint[] textures = new uint[1];
                GL.GenTextures(1, textures);
                m_id = textures[0];

                if (m_texture.Depth > 1)
                    m_id |= 0x80000000;

                ErrorCode error = GLHelper.GetError();
                if (error != ErrorCode.NoError)
                {
                    LogConsole.WriteLine(LogLevel.ERROR, "Load GenTexture GLError: " + error);
                    return 0;
                }
                LogConsole.WriteLine(LogLevel.PRINT, "Generated {0} texture {1}", m_texture.Depth > 1 ? "3d" :"2d", m_id & 0x7fffffff);
            }

            return m_id;
        }

        public uint Bind(int sampler = 0)
        {
#if !MINI
            GLHelper.Instance.SetActiveTextureSampler(sampler);
#endif
            ErrorCode error;

            Init();

#if !MINI
            if (GLHelper.Instance.CurrentTextures[sampler] == m_id) // nothing to do
                return m_id;
#endif
            GL.BindTexture(m_target, m_id & 0x7fffffff);

#if !MINI
            GLHelper.Instance.TextureBinds++;
#endif
            error = GLHelper.GetError();
            if (error != ErrorCode.NoError || m_id <= 0)
            {
                LogConsole.WriteLine(LogLevel.ERROR, "Load Bind GLError: {0} for texture {1}", error, m_id);
                return 0;
            }

            if (m_mipMask == 0)
                Load();

#if !MINI
            GLHelper.Instance.CurrentTextures[sampler] = m_id;
#endif
            return m_id;
        }

        public void UpdateData(IDataHandle dataHandle)
        {
            Bind();

            IntPtr data = IntPtr.Zero;

            try
            {
                data = dataHandle.GetPointer();

                switch (m_texture.Format)
                {
                    case TextureFormat.GL_RED:
                    case TextureFormat.GL_ALPHA:
                    case TextureFormat.GL_LUMINANCE:
                        GL.TexSubImage2D((SubTextureTarget)m_target, 0, 0, 0, m_texture.Width, m_texture.Height,
                            GLHelper.RealVersion >= GLHelper.OneComponentTextureVersion ? PixelFormat.Red : PixelFormat.Luminance,
                            PixelType.UnsignedByte, data);
                        break;
                    case TextureFormat.GL_LUMINANCE_ALPHA:
                    case TextureFormat.GL_RG:
                        GL.TexSubImage2D((SubTextureTarget)m_target, 0, 0, 0, m_texture.Width, m_texture.Height,
                            GLHelper.RealVersion >= GLHelper.OneComponentTextureVersion ? PixelFormat.Rg : PixelFormat.LuminanceAlpha,
                            PixelType.UnsignedByte, data);
                        break;
                    case TextureFormat.GL_RGB:
                        GL.TexSubImage2D((SubTextureTarget)m_target, 0, 0, 0, m_texture.Width, m_texture.Height,
                            PixelFormat.Rgb,
                            PixelType.UnsignedByte, data);
                        break;
                    default:
                        GL.TexSubImage2D((SubTextureTarget)m_target, 0, 0, 0, m_texture.Width, m_texture.Height,
                            (PixelFormat)m_texture.Format,
                            PixelType.UnsignedByte, data);
                        break;
                }
            }
            finally
            {
                if (data != IntPtr.Zero)
                    dataHandle.ReleasePointer(data);
            }
        }

        public bool Update()
        {
            // Continue loading for partially loaded mipmaps

            if (NeedToContinueLoading)
            {
                LoadTextureData();
            }
            //if (Load(false))
            //return true;

            return false;

        }

        public override string ToString()
        {
            return string.Format("[Texture {0}, {1}x{2}x{3}]", Data.Name, Data.Width, Data.Height, Data.Depth);
        }
    }
}
