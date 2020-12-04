using GameX.Properties;
using System;
using System.Collections.Generic;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;

namespace GameX
{
    public class LineShapeRenderer : IDisposable
    {
        public Vector3 Color = Vector3.UnitY;

        private Shader DefaultShader;
        private LineVertexArrayBuffer VAO;

        public LineShapeRenderer()
        {
            List<Vector3> Lines = new List<Vector3>();

            float x = -1.0f, y = -1.0f, z = -1.0f;

            // left face
            Lines.Add(new Vector3(x, y, z));
            Lines.Add(new Vector3(x + 1.0f, y, z));

            Lines.Add(new Vector3(x + 1.0f, y, z));
            Lines.Add(new Vector3(x + 1.0f, y + 1.0f, z));

            Lines.Add(new Vector3(x + 1.0f, y + 1.0f, z));
            Lines.Add(new Vector3(x, y + 1.0f, z));

            Lines.Add(new Vector3(x, y + 1.0f, z));
            Lines.Add(new Vector3(x, y, z));

            // front face
            Lines.Add(new Vector3(x, y, z));
            Lines.Add(new Vector3(x, y, z + 1.0f));

            Lines.Add(new Vector3(x, y, z + 1.0f));
            Lines.Add(new Vector3(x, y + 1.0f, z + 1.0f));

            Lines.Add(new Vector3(x, y + 1.0f, z + 1.0f));
            Lines.Add(new Vector3(x, y + 1.0f, z));

            // right face
            Lines.Add(new Vector3(x, y + 1.0f, z + 1.0f));

            Lines.Add(new Vector3(x, y, z + 1.0f));
            Lines.Add(new Vector3(x + 1.0f, y, z + 1.0f));

            Lines.Add(new Vector3(x + 1.0f, y, z + 1.0f));
            Lines.Add(new Vector3(x + 1.0f, y + 1.0f, z + 1.0f));

            Lines.Add(new Vector3(x, y + 1.0f, z + 1.0f));


            /*
            Lines.Add(new Vector3(x + 1.0f, 0.0f, y));
            Lines.Add(new Vector3(x + 1.0f, 0.0f, y + 1.0f));

            Lines.Add(new Vector3(x + 1.0f, 0.0f, y + 1.0f));
            Lines.Add(new Vector3(x, 0.0f, y + 1.0f));
            */
            DefaultShader = new Shader(Resources.line_shape_vert_shader, Resources.line_shape_frag_shader, true);
            VAO = new LineVertexArrayBuffer(Lines.ToArray());
        }

        public void Draw(Camera camera, GameObject gameObject)
        {
            bool blendState = GL.IsEnabled(EnableCap.Blend);
            GL.Disable(EnableCap.Blend);
            //GL.BlendEquation(BlendEquationMode.FuncAdd);
            //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);


            DefaultShader.UseShader();

            // compute custom model matrix based on the physics stuff
            OpenTK.Mathematics.Matrix4 min = OpenTK.Mathematics.Matrix4.CreateTranslation(gameObject.BodyReference.BoundingBox.Min.X, gameObject.BodyReference.BoundingBox.Min.Y, gameObject.BodyReference.BoundingBox.Min.Z);
            //OpenTK.Mathematics.Matrix4 max = OpenTK.Mathematics.Matrix4.CreateTranslation(gameObject.BodyReference.BoundingBox.Max.X, gameObject.BodyReference.BoundingBox.Max.Y, gameObject.BodyReference.BoundingBox.Max.Z);



            OpenTK.Mathematics.Matrix4 modelMatrix = gameObject.transform.GetModelMatrix();
            DefaultShader.SetMatrix("model", ref modelMatrix);

            OpenTK.Mathematics.Matrix4 viewMatrix = camera.GetViewMatrix();
            DefaultShader.SetMatrix("view", ref viewMatrix);

            OpenTK.Mathematics.Matrix4 projectionMatrix = camera.ProjectionMatrix;
            DefaultShader.SetMatrix("projection", ref projectionMatrix);

            DefaultShader.SetVector3("lineColor", ref Color);


            GLException.CheckError("LineShapeRenderer Shader");
            VAO.Draw();

            if (blendState)
                GL.Enable(EnableCap.Blend);

            GLException.CheckError("LineShapeRenderer VAO");
        }

        public void Dispose()
        {
            DefaultShader.Dispose();
            VAO.Dispose();
        }


    }
}
