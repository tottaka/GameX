using System;
using System.Numerics;

namespace GameX
{
    public class Material : IDisposable
    {
        public static Material Default = new Material(Shader.Default);

        public Vector3 Color = Vector3.One;

        /// <summary>
        /// The <see cref="GameX.Shader"/> to use to render the mesh.
        /// </summary>
        public Shader Shader;

        /// <summary>
        /// The default diffuse texture to apply to the renderer for this mesh.
        /// </summary>
        public Texture2D DiffuseTexture;

        /// <summary>
        /// Unused
        /// </summary>
        public Texture2D NormalTexture;

        /// <summary>
        /// Unused
        /// </summary>
        public Texture2D SpecularTexture;

        public bool IsDisposed { get; private set; }

        public Material(Shader shader, Texture2D diffuse = null, Texture2D normal = null, Texture2D specular = null)
        {
            Shader = shader;
            DiffuseTexture = diffuse;
            NormalTexture = normal;
            SpecularTexture = specular;
        }

        ~Material()
        {
            Dispose();
        }

        public void Bind(Camera camera, Transform transform)
        {
            if (DiffuseTexture != null)
                DiffuseTexture.BindTexture();

            if (NormalTexture != null)
                NormalTexture.BindTexture();

            if (SpecularTexture != null)
                SpecularTexture.BindTexture();

            Shader.UseShader();
            OpenTK.Mathematics.Matrix4 modelMatrix = transform.GetModelMatrix();
            Shader.SetMatrix("model", ref modelMatrix);

            OpenTK.Mathematics.Matrix4 viewMatrix = camera.GetViewMatrix();
            Shader.SetMatrix("view", ref viewMatrix);

            OpenTK.Mathematics.Matrix4 projectionMatrix = camera.ProjectionMatrix;
            Shader.SetMatrix("projection", ref projectionMatrix);

            Shader.SetVector3("ambientLightColor", ref Scene.CurrentScene.AmbientLightColor);

            Vector3 LightColor = new Vector3(1.0f, 1.0f, 1.0f);
            Shader.SetVector3("lightColor", ref LightColor);

            Vector3 LightPosition = new Vector3(0.0f, 0.0f, 0.0f);
            Shader.SetVector3("lightPos", ref LightPosition);

            Shader.SetFloat("ambientLightStrength", Scene.CurrentScene.AmbientLightStrength);

            Shader.SetVector3("objectColor", ref Color);
        }

        public void Unbind()
        {
            if (DiffuseTexture != null)
                DiffuseTexture.UnbindTexture();

            if (NormalTexture != null)
                NormalTexture.UnbindTexture();

            if (SpecularTexture != null)
                SpecularTexture.UnbindTexture();
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                if (DiffuseTexture != null)
                    DiffuseTexture.Dispose();
                
                if (NormalTexture != null)
                    NormalTexture.Dispose();

                if (SpecularTexture != null)
                    SpecularTexture.Dispose();

                IsDisposed = true;
            }
        }
    }
}
