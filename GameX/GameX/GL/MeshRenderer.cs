using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace GameX
{
    public class MeshRenderer : IDisposable
    {
        /// <summary>
        /// The <see cref="Mesh"/> to be rendered.
        /// </summary>
        public Mesh Mesh { get; private set; }

        internal string RelativeMeshPath;

        private VertexArrayBuffer VertexBuffer;

        internal MeshRenderer(string relativePath)
        {
            RelativeMeshPath = relativePath;
        }

        public MeshRenderer(Mesh mesh)
        {
            Mesh = mesh;
            VertexBuffer = new VertexArrayBuffer(Mesh.Vertices, Mesh.Indices);
        }

        public void Draw(Camera camera, Transform transform, Material material)
        {
            if (VertexBuffer != null)
            {
                material.Bind(camera, transform);
                VertexBuffer.Draw();
                material.Unbind();
            }
        }

        public void SetMesh(Mesh mesh)
        {
            Mesh = mesh;
            if(VertexBuffer != null)
                VertexBuffer.Dispose();

            VertexBuffer = new VertexArrayBuffer(Mesh.Vertices, Mesh.Indices);
        }

        public void Dispose()
        {
            VertexBuffer.Dispose();
        }
    }
}
