using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace GameX
{
    public sealed class CubeMap : IDisposable
    {
        public const SizedInternalFormat Srgb8Alpha8 = (SizedInternalFormat)All.Srgb8Alpha8;
        public const SizedInternalFormat RGB32F = (SizedInternalFormat)All.Rgb32f;

        public const GetPName MAX_TEXTURE_MAX_ANISOTROPY = (GetPName)0x84FF;

        public static readonly float MaxAniso;

        static CubeMap()
        {
            MaxAniso = GL.GetFloat(MAX_TEXTURE_MAX_ANISOTROPY);
        }

        public readonly int GLTexture;
        public readonly TextureUnit TextureSlot;
        public readonly int Width, Height;
        public readonly int MipmapLevels;
        public readonly SizedInternalFormat InternalFormat;

        public CubeMap(Bitmap[] textures, TextureUnit slot, bool srgb = false)
        {
            TextureSlot = slot;
            InternalFormat = srgb ? Srgb8Alpha8 : SizedInternalFormat.Rgba8;

            // There is only one level
            MipmapLevels = 1;

            GLException.CheckError("Clear");

            GL.CreateTextures(TextureTarget.TextureCubeMap, 1, out GLTexture);
            GLException.CheckError("CreateTexture");

            BindTexture();
            for (int i = 0; i < textures.Length; i++)
            {
                Bitmap bmp = textures[i];
                BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, global::System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, PixelInternalFormat.Rgba8, bmp.Width, bmp.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data.Scan0);
                GLException.CheckError("SubImage");

                bmp.UnlockBits(data);
                bmp.Dispose();
            }
            UnbindTexture();

            SetMagFilter(TextureMagFilter.Linear);
            SetMinFilter(TextureMinFilter.Linear);
            SetWrap(TextureCoordinate.S, TextureWrapMode.ClampToEdge);
            SetWrap(TextureCoordinate.T, TextureWrapMode.ClampToEdge);
            SetWrap(TextureCoordinate.R, TextureWrapMode.ClampToEdge);

            GL.TextureParameter(GLTexture, TextureParameterName.TextureMaxLevel, MipmapLevels - 1);
        }

        public void SetMinFilter(TextureMinFilter filter)
        {
            BindTexture();
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)filter);
            UnbindTexture();
        }

        public void SetMagFilter(TextureMagFilter filter)
        {
            BindTexture();
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)filter);
            UnbindTexture();
        }

        public void SetLod(int @base, int min, int max)
        {
            BindTexture();
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureLodBias, @base);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinLod, min);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMaxLod, max);
            UnbindTexture();
        }

        public void SetWrap(TextureCoordinate coord, TextureWrapMode mode)
        {
            BindTexture();
            GL.TexParameter(TextureTarget.TextureCubeMap, (TextureParameterName)coord, (int)mode);
            UnbindTexture();
        }

        public void SetPixelStore(PixelStoreParameter pixelStore, int param)
        {
            BindTexture();
            GL.PixelStore(PixelStoreParameter.UnpackRowLength, param);
            UnbindTexture();
        }

        public void BindTexture()
        {
            GL.ActiveTexture(TextureSlot);
            GL.BindTexture(TextureTarget.TextureCubeMap, GLTexture);
        }

        public void UnbindTexture()
        {
            GL.BindTexture(TextureTarget.TextureCubeMap, 0);
        }

        public void Dispose()
        {
            GL.DeleteTexture(GLTexture);
        }

        /*
        public static CubeMap FromFile(string filePath, TextureUnit slot, Rectangle[] textureCoords, bool srgb = false)
        {
            return new CubeMap((Bitmap)Image.FromFile(filePath), slot, textureCoords, srgb);
        }
        */
    }
}
