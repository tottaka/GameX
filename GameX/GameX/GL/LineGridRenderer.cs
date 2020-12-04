using GameX.Properties;
using System;
using System.Collections.Generic;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;

namespace GameX
{
    public class LineGridRenderer : IDisposable
    {
        public Vector3 GridColor = Vector3.One;

        private Shader DefaultShader;
        private LineVertexArrayBuffer VAO;

        public LineGridRenderer(int size = 64)
        {
            List<Vector3> Lines = new List<Vector3>();
            for(int x = -(size / 2); x < size / 2; x++)
            {
                for (int y = -(size / 2); y < size / 2; y++)
                {
                    Lines.Add(new Vector3(x, 0.0f, y));
                    Lines.Add(new Vector3(x + 1.0f, 0.0f, y));

                    Lines.Add(new Vector3(x + 1.0f, 0.0f, y));
                    Lines.Add(new Vector3(x + 1.0f, 0.0f, y + 1.0f));

                    Lines.Add(new Vector3(x + 1.0f, 0.0f, y + 1.0f));
                    Lines.Add(new Vector3(x, 0.0f, y + 1.0f));
                }
            }

            DefaultShader = new Shader(Resources.grid_vert_shader, Resources.grid_frag_shader, true);
            VAO = new LineVertexArrayBuffer(Lines.ToArray());
        }

        public void Draw(Camera camera)
        {
            bool blendState = GL.IsEnabled(EnableCap.Blend);
            GL.Enable(EnableCap.Blend);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);


            DefaultShader.UseShader();
            OpenTK.Mathematics.Matrix4 modelMatrix = OpenTK.Mathematics.Matrix4.Identity * OpenTK.Mathematics.Matrix4.CreateTranslation(0, 0.0f, 0);
            DefaultShader.SetMatrix("model", ref modelMatrix);

            OpenTK.Mathematics.Matrix4 viewMatrix = camera.GetViewMatrix();
            DefaultShader.SetMatrix("view", ref viewMatrix);

            OpenTK.Mathematics.Matrix4 projectionMatrix = camera.ProjectionMatrix;
            DefaultShader.SetMatrix("projection", ref projectionMatrix);

            DefaultShader.SetVector3("lineColor", ref GridColor);
            DefaultShader.SetVector3("cam_pos", ref camera.transform.Position);


            GLException.CheckError("LineGridRenderer Shader");
            VAO.Draw();

            if (!blendState)
                GL.Disable(EnableCap.Blend);

            GLException.CheckError("LineGridRenderer VAO");
        }

        public void Dispose()
        {
            DefaultShader.Dispose();
            VAO.Dispose();
        }


    }
}
