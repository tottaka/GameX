using GameX.Properties;
using System;
using System.Collections.Generic;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;

namespace GameX
{
    public class Skybox : IDisposable
    {
        public Shader DefaultShader;
        public CubeMap Texture;

        /// <summary>
        /// The Vertex Buffer Object Handle
        /// </summary>
        public int BufferHandle { get; private set; }

        /// <summary>
        /// The Vertex Array Object Handle
        /// </summary>
        public int ArrayHandle { get; private set; }

        public bool IsDisposed { get; private set; }

        public Skybox()
        {
            Texture = new CubeMap(new System.Drawing.Bitmap[] {
                Resources.skybox_right,
                Resources.skybox_left,
                Resources.skybox_top,
                Resources.skybox_bottom,
                Resources.skybox_front,
                Resources.skybox_back
            }, TextureUnit.Texture0);
            DefaultShader = new Shader(Resources.skybox_vert_shader, Resources.skybox_frag_shader, true);

            Vector3[] CubeVertices = new Vector3[] {

                new Vector3(-1.0f,  1.0f, -1.0f),
                new Vector3(-1.0f, -1.0f, -1.0f),
                new Vector3(1.0f, -1.0f, -1.0f),
                new Vector3(1.0f, -1.0f, -1.0f),
                new Vector3(1.0f,  1.0f, -1.0f),
                new Vector3(-1.0f,  1.0f, -1.0f),

                new Vector3(-1.0f, -1.0f,  1.0f),
                new Vector3(-1.0f, -1.0f, -1.0f),
                new Vector3(-1.0f,  1.0f, -1.0f),
                new Vector3(-1.0f,  1.0f, -1.0f),
                new Vector3(-1.0f,  1.0f,  1.0f),
                new Vector3(-1.0f, -1.0f,  1.0f),

                new Vector3(1.0f, -1.0f, -1.0f),
                new Vector3(1.0f, -1.0f,  1.0f),
                new Vector3(1.0f,  1.0f,  1.0f),
                new Vector3(1.0f,  1.0f,  1.0f),
                new Vector3(1.0f,  1.0f, -1.0f),
                new Vector3(1.0f, -1.0f, -1.0f),

                new Vector3(-1.0f, -1.0f,  1.0f),
                new Vector3(-1.0f,  1.0f,  1.0f),
                new Vector3(1.0f,  1.0f,  1.0f),
                new Vector3(1.0f,  1.0f,  1.0f),
                new Vector3(1.0f, -1.0f,  1.0f),
                new Vector3(-1.0f, -1.0f,  1.0f),

                new Vector3(-1.0f,  1.0f, -1.0f),
                new Vector3(1.0f,  1.0f, -1.0f),
                new Vector3(1.0f,  1.0f,  1.0f),
                new Vector3(1.0f,  1.0f,  1.0f),
                new Vector3(-1.0f,  1.0f,  1.0f),
                new Vector3(-1.0f,  1.0f, -1.0f),

                new Vector3(-1.0f, -1.0f, -1.0f),
                new Vector3(-1.0f, -1.0f,  1.0f),
                new Vector3(1.0f, -1.0f, -1.0f),
                new Vector3(1.0f, -1.0f, -1.0f),
                new Vector3(-1.0f, -1.0f,  1.0f),
                new Vector3( 1.0f, -1.0f,  1.0f)

            };

            BufferHandle = GL.GenBuffer();
            ArrayHandle = GL.GenVertexArray();

            GL.BindVertexArray(ArrayHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, BufferHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, CubeVertices.Length * sizeof(float) * 3, CubeVertices, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(float) * 3, 0);

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        public void Draw(Camera camera)
        {
            bool depthEnabled = GL.IsEnabled(EnableCap.DepthTest);
            GL.Disable(EnableCap.DepthTest);

            bool cullingEnabled = GL.IsEnabled(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);

            GL.DepthFunc(DepthFunction.Lequal);

            Texture.BindTexture();
            DefaultShader.UseShader();
            
            OpenTK.Mathematics.Matrix4 viewMatrix = new OpenTK.Mathematics.Matrix4(new OpenTK.Mathematics.Matrix3(camera.GetViewMatrix()));
            DefaultShader.SetMatrix("view", ref viewMatrix);

            OpenTK.Mathematics.Matrix4 projectionMatrix = camera.ProjectionMatrix;
            DefaultShader.SetMatrix("projection", ref projectionMatrix);

            GL.BindVertexArray(ArrayHandle);
            
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            GL.BindVertexArray(0);
            Texture.UnbindTexture();

            GL.DepthFunc(DepthFunction.Less);

            if(depthEnabled)
                GL.Enable(EnableCap.DepthTest);
            
            if(cullingEnabled)
                GL.Enable(EnableCap.CullFace);
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                DefaultShader.Dispose();
                Texture.Dispose();
                GL.DeleteVertexArray(ArrayHandle);
                GL.DeleteBuffer(BufferHandle);
                IsDisposed = true;
            }
        }
    }
}
