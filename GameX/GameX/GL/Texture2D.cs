using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace GameX
{
    public enum TextureCoordinate
    {
        S = TextureParameterName.TextureWrapS,
        T = TextureParameterName.TextureWrapT,
        R = TextureParameterName.TextureWrapR
    }

    public sealed class Texture2D : IDisposable
    {
        public const SizedInternalFormat Srgb8Alpha8 = (SizedInternalFormat)All.Srgb8Alpha8;
        public const SizedInternalFormat RGB32F = (SizedInternalFormat)All.Rgb32f;

        public const GetPName MAX_TEXTURE_MAX_ANISOTROPY = (GetPName)0x84FF;

        public static readonly float MaxAniso;

        static Texture2D()
        {
            MaxAniso = GL.GetFloat(MAX_TEXTURE_MAX_ANISOTROPY);
        }

        public int GLTexture;
        public TextureUnit TextureSlot;
        public int Width, Height;
        public int MipmapLevels;
        public SizedInternalFormat InternalFormat;

        internal string RelativeAssetPath;

        internal Texture2D(string relativePath, bool generateMipmaps, TextureUnit slot)
        {
            RelativeAssetPath = relativePath;
            TextureSlot = slot;
            InternalFormat = SizedInternalFormat.Rgba8;
            if (generateMipmaps)
            {
                // Calculate how many levels to generate for this texture
                MipmapLevels = (int)Math.Floor(Math.Log(Math.Max(Width, Height), 2));
            }
            else
            {
                // There is only one level
                MipmapLevels = 1;
            }
        }

        public Texture2D(Bitmap image, bool generateMipmaps, TextureUnit slot, bool srgb)
        {
            Width = image.Width;
            Height = image.Height;
            TextureSlot = slot;
            InternalFormat = srgb ? Srgb8Alpha8 : SizedInternalFormat.Rgba8;

            if (generateMipmaps)
            {
                // Calculate how many levels to generate for this texture
                MipmapLevels = (int)Math.Floor(Math.Log(Math.Max(Width, Height), 2));
            }
            else
            {
                // There is only one level
                MipmapLevels = 1;
            }

            GLException.CheckError("Clear");

            GL.CreateTextures(TextureTarget.Texture2D, 1, out GLTexture);
            GL.TextureStorage2D(GLTexture, MipmapLevels, InternalFormat, Width, Height);
            GLException.CheckError("Storage2d");

            BitmapData data = image.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, global::System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TextureSubImage2D(GLTexture, 0, 0, 0, Width, Height, PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            GLException.CheckError("SubImage");

            image.UnlockBits(data);

            if (generateMipmaps) GL.GenerateTextureMipmap(GLTexture);

            GL.TextureParameter(GLTexture, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GLException.CheckError("WrapS");
            GL.TextureParameter(GLTexture, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GLException.CheckError("WrapT");

            GL.TextureParameter(GLTexture, TextureParameterName.TextureMinFilter, (int)(generateMipmaps ? TextureMinFilter.Linear : TextureMinFilter.LinearMipmapLinear));
            GL.TextureParameter(GLTexture, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GLException.CheckError("Filtering");

            GL.TextureParameter(GLTexture, TextureParameterName.TextureMaxLevel, MipmapLevels - 1);

            // This is a bit weird to do here
            image.Dispose();
        }

        public Texture2D(int width, int height, IntPtr data, TextureUnit slot, bool generateMipmaps = false, bool srgb = false)
        {
            Width = width;
            Height = height;
            TextureSlot = slot;
            InternalFormat = srgb ? Srgb8Alpha8 : SizedInternalFormat.Rgba8;
            MipmapLevels = generateMipmaps == false ? 1 : (int)Math.Floor(Math.Log(Math.Max(Width, Height), 2));

            GL.CreateTextures(TextureTarget.Texture2D, 1, out GLTexture);
            GL.TextureStorage2D(GLTexture, MipmapLevels, InternalFormat, Width, Height);

            GL.TextureSubImage2D(GLTexture, 0, 0, 0, Width, Height, PixelFormat.Bgra, PixelType.UnsignedByte, data);

            if (generateMipmaps) 
                GL.GenerateTextureMipmap(GLTexture);

            SetWrap(TextureCoordinate.S, TextureWrapMode.Repeat);
            SetWrap(TextureCoordinate.T, TextureWrapMode.Repeat);

            GL.TextureParameter(GLTexture, TextureParameterName.TextureMaxLevel, MipmapLevels - 1);
        }

        internal void Load(string basePath)
        {
            using (Bitmap image = (Bitmap)Image.FromFile(Path.Combine(basePath, RelativeAssetPath)))
            {
                Width = image.Width;
                Height = image.Height;

                GLException.CheckError("Clear");

                GL.CreateTextures(TextureTarget.Texture2D, 1, out GLTexture);
                GL.TextureStorage2D(GLTexture, MipmapLevels, InternalFormat, Width, Height);
                GLException.CheckError("Storage2d");

                BitmapData data = image.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, global::System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.TextureSubImage2D(GLTexture, 0, 0, 0, Width, Height, PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                GLException.CheckError("SubImage");

                image.UnlockBits(data);
            }

            if (MipmapLevels > 1)
                GL.GenerateTextureMipmap(GLTexture);

            GL.TextureParameter(GLTexture, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GLException.CheckError("WrapS");
            GL.TextureParameter(GLTexture, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GLException.CheckError("WrapT");

            GL.TextureParameter(GLTexture, TextureParameterName.TextureMinFilter, (int)(MipmapLevels > 1 ? TextureMinFilter.Linear : TextureMinFilter.LinearMipmapLinear));
            GL.TextureParameter(GLTexture, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GLException.CheckError("Filtering");

            GL.TextureParameter(GLTexture, TextureParameterName.TextureMaxLevel, MipmapLevels - 1);

        }

        public void SetMinFilter(TextureMinFilter filter)
        {
            BindTexture();
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)filter);
            UnbindTexture();
        }

        public void SetMagFilter(TextureMagFilter filter)
        {
            BindTexture();
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)filter);
            UnbindTexture();
        }

        public void SetLod(int @base, int min, int max)
        {
            BindTexture();
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureLodBias, @base);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinLod, min);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLod, max);
            UnbindTexture();
        }

        public void SetWrap(TextureCoordinate coord, TextureWrapMode mode)
        {
            BindTexture();
            GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)coord, (int)mode);
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
            GL.BindTexture(TextureTarget.Texture2D, GLTexture);
        }

        public void UnbindTexture()
        {
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void Dispose()
        {
            GL.DeleteTexture(GLTexture);
        }

        public static Texture2D FromFile(string filePath, TextureUnit slot, bool Mipmaps = false, bool srgb = false)
        {
            return new Texture2D((Bitmap)Image.FromFile(filePath), Mipmaps, slot, srgb);
        }

        public static Texture2D GetThumbnailImage(Image image, Vector2 size)
        {
            Image thumb = image.GetThumbnailImage((int)size.X, (int)size.Y, () => false, IntPtr.Zero);
            Texture2D tex = new Texture2D((Bitmap)thumb, false, TextureUnit.Texture0, false);
            thumb.Dispose();
            return tex;
        }

    }
}
