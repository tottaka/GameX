using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace GameX.Editor
{
    public class Sprite
    {
        public Texture2D Texture;
        public Vector2 Position;
        public Vector2 Size;
        public Vector2 Min => Position;
        public Vector2 Max => Position + Size;

        public Sprite(Texture2D target, Vector2 pos, Vector2 size)
        {
            Texture = target;
            Position = pos;
            Size = size;
        }
    }

    internal static partial class ImGuiEx
    {
        public static void Image(Sprite sprite, Vector2 size)
        {
            ImGui.Image((IntPtr)sprite.Texture.GLTexture, size, sprite.Min, sprite.Max);
        }
    }

}
