using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK.Mathematics;
using Vector4 = System.Numerics.Vector4;

namespace GameX
{

    public enum ClearFlags { Skybox, Color, Depth, None };

    public struct ViewportRect
    {
        public static ViewportRect Default = new ViewportRect(new Vector4(0.0f, 0.0f, 1.0f, 1.0f));


        public float Top;
        public float Left;
        public float Width;
        public float Height;

        public ViewportRect(Vector4 viewportBounds)
        {
            Top = viewportBounds.X;
            Left = viewportBounds.Y;
            Width = viewportBounds.Z;
            Height = viewportBounds.W;
        }

        internal Rectangle GetScaledViewport(Rectangle windowRect)
        {
            int left = (int)(windowRect.Width * Left);
            int top = (int)(windowRect.Height * Top);
            int width = (int)(windowRect.Width * Width);
            int height = (int)(windowRect.Height * Height);

            return new Rectangle(left, top, width - left, height - top);
        }

        internal float GetAspectRatio(Rectangle windowRect) => (windowRect.Width * Width) / (windowRect.Height * Height);

    }


    public sealed class Camera : GameObject
    {
        public ClearFlags ClearMode = ClearFlags.Skybox;
        public Color4 ClearColor = Color4.AliceBlue;

        public float zFar = 100.0f;
        public float zNear = 0.1f;
        public float FOV = 60.0f;

        public int Depth = 0;

        public ViewportRect Viewport;

        internal Matrix4 ProjectionMatrix { get; private set; }

        public Camera(ViewportRect viewport, Rectangle windowBounds)
        {
            Viewport = viewport;
            ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FOV), Viewport.GetAspectRatio(windowBounds), zNear, zFar);
        }

        internal Matrix4 GetViewMatrix()
        {
            return transform.LookAt(transform.Forward);
        }

        internal void SetScreenBounds(Rectangle bounds)
        {
            ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FOV), Viewport.GetAspectRatio(bounds), zNear, zFar);
        }

        /*
        public Vector3 ScreenToWorld(float x, float y)
        {
            Matrix4 viewMatrix = GetViewMatrix();
            Matrix4 projViewMatrix = viewMatrix * ProjectionMatrix;
            projViewMatrix.Invert();

            return Vector3.Unproject(new Vector3(x, Viewport.v_Height - y, zNear), Viewport.v_Left, Viewport.v_Top, Viewport.v_Width, Viewport.v_Height, zNear, zFar, projViewMatrix);
        }

        public Vector2 WorldToScreen(Vector3 worldPos)
        {
            Matrix4 viewMatrix = GetViewMatrix();
            Matrix4 projViewMatrix = viewMatrix * ProjectionMatrix;

            Vector3 projection = Vector3.Project(worldPos, Viewport.v_Left, Viewport.v_Top, Viewport.v_Width, Viewport.v_Height, zNear, zFar, projViewMatrix);
            return new Vector2(projection.X, Viewport.v_Height - projection.Y);
        }
        */

    }
}
