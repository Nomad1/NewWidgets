using System;

namespace NewWidgets.BrowserPreview
{
    /// <summary>
    /// Decodes the base (mip 0) level of a RunMobile RLE texture (".rle") into raw interleaved
    /// pixel bytes.
    ///
    /// This is a copy, not a link, of the decode algorithm in the read-only original at
    /// RunMobile/Graphics/Textures/Texture.RLE.cs (methods LoadRLEData / FromFlaggedInt /
    /// UncompressRLE). It is a copy rather than a compiled-in link -- unlike Png.cs and
    /// SpriteData.cs, which this project links directly -- because the original file's public
    /// entry point pulls in TextureManager/TextureData/IDataHandle/ArrayDataHandle from the
    /// RunMobile engine assembly, none of which this standalone console tool has any reason to
    /// reference. Only the three dependency-free static methods are copied, unchanged in logic.
    ///
    /// One behavioural difference from the original: LoadRLEData decodes each colour channel's
    /// RLE run-stream on RunMobile.Utility.Parallel.For (one thread per channel, up to 4).
    /// This runs the same per-channel loop sequentially instead. Each channel writes into a
    /// disjoint set of array slots (stride = channel count), so thread count changes only decode
    /// wall-clock time, never the result -- and wall-clock time does not matter for a one-shot
    /// preview build.
    ///
    /// ponytail: only mip level 0 (the base image) is decoded. A .rle file with mipmaps > 1
    /// stores every level back-to-back in the same stream, but this reads only the header plus
    /// the first level's four channel blocks and stops -- a browser preview has no use for
    /// mipmaps. Upgrade path, if ever needed: keep looping the same way Texture.RLE.cs does,
    /// halving width/height each pass.
    /// </summary>
    internal static class RleDecoder
    {
        /// <summary>
        /// Decodes mip level 0 of an RLE-compressed texture into RGBA/RGB/etc. interleaved bytes
        /// (channel count reported via <paramref name="channels"/>; the caller expands to RGBA).
        /// </summary>
        public static byte[] Decode(byte[] compressedData, out int width, out int height, out int channels)
        {
            int pos = 0;
            width = BitConverter.ToInt32(compressedData, pos);
            pos += 4;
            height = BitConverter.ToInt32(compressedData, pos);
            pos += 4;

            byte channelByte = compressedData[pos];
            pos++;

            channels = channelByte & 0xF;

            int baseLevelSize = width * height * channels;
            byte[] data = new byte[baseLevelSize];

            // Each channel's compressed block is prefixed, for mip level 0, by one 4-byte
            // little-endian length; the blocks themselves follow immediately after all the
            // length prefixes for this level.
            Tuple<int, int>[] blocks = new Tuple<int, int>[channels];
            int blockPos = pos + 4 * channels;
            for (int i = 0; i < channels; i++)
            {
                int blockLen = BitConverter.ToInt32(compressedData, pos);
                pos += 4;

                blocks[i] = new Tuple<int, int>(blockPos, blockLen);
                blockPos += blockLen;
            }

            // decodes each channel's RLE run-stream directly into its interleaved position
            // (stride = channel count), one channel at a time
            for (int i = 0; i < channels; i++)
                UncompressRLE(compressedData, blocks[i].Item1, blocks[i].Item2, data, i, channels);

            // Alpha-0 pixels carry undefined RGB in this format; zero them so anything that
            // reads RGB regardless of alpha (a naive PNG viewer, a compositor) never shows a
            // fringe of garbage colour. Mirrors the original decoder's own pass.
            if (channels == 4)
            {
                for (int k = 0; k < data.Length; k += 4)
                {
                    if (data[k + 3] == 0)
                    {
                        data[k + 0] = 0;
                        data[k + 1] = 0;
                        data[k + 2] = 0;
                    }
                }
            }

            return data;
        }

        private static int FromFlaggedInt(byte[] data, int offset, out int len)
        {
            int result = 0;
            for (len = 0; len < 3; len++)
            {
                byte b = data[offset + len];
                result += (b & 0x7F) << (len * 7);
                if ((b & 0x80) == 0)
                    return result;
            }

            return result;
        }

        private static void UncompressRLE(byte[] compressed, int offset, int compressedLength, byte[] data, int dataOffset, int stride)
        {
            byte splitChar = compressed[offset + 4];

            unchecked
            {
                compressedLength += offset;
                offset += 5;
                while (offset < compressedLength)
                {
                    byte current = compressed[offset];
                    if (current != splitChar)
                    {
                        data[dataOffset] = current;
                        dataOffset += stride;
                        offset++;
                        continue;
                    }

                    int shift;
                    int len = FromFlaggedInt(compressed, offset + 1, out shift);

                    current = compressed[offset + shift + 2];

                    for (int j = 0; j < len; j++)
                    {
                        data[dataOffset] = current;
                        dataOffset += stride;
                    }
                    offset += shift + 3;
                }
            }
        }
    }
}
