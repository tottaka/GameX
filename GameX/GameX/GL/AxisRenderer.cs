using GameX.Properties;
using System;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using Matrix4 = OpenTK.Mathematics.Matrix4;

namespace GameX
{
    public class AxisRenderer : IDisposable
    {
        public Transform Target;
        public Vector3 Scale = Vector3.One;
        public Vector3 xAxisColor = new Vector3(1.0f, 0.0f, 0.0f);
        public Vector3 yAxisColor = new Vector3(0.0f, 1.0f, 0.0f);
        public Vector3 zAxisColor = new Vector3(0.0f, 0.0f, 1.0f);

        public BoundingBox xAxisBox, yAxisBox, zAxisBox;

        private Shader DefaultShader;
        private LineVertexArrayBuffer VAO;

        private static int num_segments = 128;
        private static int vert_offset = (num_segments * 3) + 1;

        public AxisRenderer(float ConeScale = 1.0f)
        {
            List<Vector3> lines = new List<Vector3>();

            Vector3[] xCone = BuildConeX(new Vector3(4.0f, 0.0f, 0.0f), ConeScale, ConeScale / 3.0f, num_segments);
            Vector3[] yCone = BuildConeY(new Vector3(0.0f, 4.0f, 0.0f), ConeScale, ConeScale / 3.0f, num_segments);
            Vector3[] zCone = BuildConeZ(new Vector3(0.0f, 0.0f, 4.0f), ConeScale, ConeScale / 3.0f, num_segments);

            /*
            xAxisBox = xCone.Item2;
            Console.WriteLine(xCone.Item2);

            yAxisBox = yCone.Item2;
            zAxisBox = zCone.Item2;
            */

            // x
            lines.AddRange(xCone);
            lines.Add(new Vector3(0.0f, 0.0f, 0.0f));

            // y
            lines.AddRange(yCone);
            lines.Add(new Vector3(0.0f, 0.0f, 0.0f));

            // z
            lines.AddRange(zCone);
            lines.Add(new Vector3(0.0f, 0.0f, 0.0f));

            DefaultShader = new Shader(Resources.axis_vert_shader, Resources.axis_frag_shader, true);
            VAO = new LineVertexArrayBuffer(lines.ToArray());
        }

        public void SetTarget(Transform targetTransform)
        {
            Target = targetTransform;
        }

        public void UnsetTarget()
        {
            Target = null;
        }

        public void Draw(Camera camera)
        {
            if (Target == null)
                return;

            bool depthState = GL.IsEnabled(EnableCap.DepthTest);
            GL.Disable(EnableCap.DepthTest);

            DefaultShader.UseShader();
            float distance = Vector3.Distance(camera.transform.Position, Target.Position);
            Matrix4 modelMatrix = Matrix4.Identity * Matrix4.CreateScale(Scale.X * (distance / 32.0f), Scale.Y * (distance / 32.0f), Scale.Z * (distance / 32.0f)) * Matrix4.CreateTranslation(Target.Position.X, Target.Position.Y, Target.Position.Z);
            DefaultShader.SetMatrix("model", ref modelMatrix);

            Matrix4 viewMatrix = camera.GetViewMatrix();
            DefaultShader.SetMatrix("view", ref viewMatrix);

            Matrix4 projectionMatrix = camera.ProjectionMatrix;
            DefaultShader.SetMatrix("projection", ref projectionMatrix);


            // Draw X Axis
            DefaultShader.SetVector3("lineColor", ref xAxisColor);
            GLException.CheckError("AxisRenderer Shader");
            VAO.Draw(PrimitiveType.LineStrip, 0, vert_offset);

            // Draw Y Axis
            DefaultShader.SetVector3("lineColor", ref yAxisColor);
            GLException.CheckError("AxisRenderer Shader");
            VAO.Draw(PrimitiveType.LineStrip, vert_offset, vert_offset * 2);

            // Draw Z Axis
            DefaultShader.SetVector3("lineColor", ref zAxisColor);
            GLException.CheckError("AxisRenderer Shader");
            VAO.Draw(PrimitiveType.LineStrip, vert_offset * 2, vert_offset * 3);

            if (depthState)
                GL.Enable(EnableCap.DepthTest);

            GLException.CheckError("AxisRenderer VAO");
        }

        public void Dispose()
        {
            DefaultShader.Dispose();
            VAO.Dispose();
        }

        private static Vector3[] BuildConeX(Vector3 center, float height, float radius, int num_segments)
        {
            List<Vector3> verticiesList = new List<Vector3>();

            for (int i = 0; i < num_segments; i++)
            {
                float twicePi = 2.0f * MathF.PI;
                float theta = twicePi * (float)i / (float)num_segments;
                float x = radius * MathF.Cos(theta);
                float y = radius * MathF.Sin(theta);

                verticiesList.Add(center);
                verticiesList.Add(new Vector3(center.X, x + center.Y, y + center.Z));
                verticiesList.Add(new Vector3(center.X + height, center.Y, center.Z));
            }

            return verticiesList.ToArray();
        }

        private static Vector3[] BuildConeY(Vector3 center, float height, float radius, int num_segments)
        {
            List<Vector3> verticiesList = new List<Vector3>();

            for (int i = 0; i < num_segments; i++)
            {
                float twicePi = 2.0f * MathF.PI;
                float theta = twicePi * (float)i / (float)num_segments;
                float x = radius * MathF.Cos(theta);
                float y = radius * MathF.Sin(theta);

                verticiesList.Add(center);
                verticiesList.Add(new Vector3(x + center.X, center.Y, y + center.Z));
                verticiesList.Add(new Vector3(center.X, center.Y + height, center.Z));
            }

            return verticiesList.ToArray();
        }

        private static Vector3[] BuildConeZ(Vector3 center, float height, float radius, int num_segments)
        {
            List<Vector3> verticiesList = new List<Vector3>();

            for (int i = 0; i < num_segments; i++)
            {
                float twicePi = 2.0f * MathF.PI;
                float theta = twicePi * (float)i / (float)num_segments;
                float x = radius * MathF.Cos(theta);
                float y = radius * MathF.Sin(theta);

                verticiesList.Add(center);
                verticiesList.Add(new Vector3(x + center.X, y + center.Y, center.Z));
                verticiesList.Add(new Vector3(center.X, center.Y, center.Z + height));
            }

            return verticiesList.ToArray();
        }
    }
}
