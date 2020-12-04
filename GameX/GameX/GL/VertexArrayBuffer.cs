using System;
using OpenTK.Graphics.OpenGL4;

namespace GameX
{
    public sealed class VertexArrayBuffer : IDisposable
    {
        /// <summary>
        /// The total index count (indices) for this instance.
        /// </summary>
        public int IndexCount { get; private set; }

        /// <summary>
        /// The Vertex Buffer Object Handle
        /// </summary>
        public int BufferHandle { get; private set; }

        /// <summary>
        /// The Element Array Object Handle
        /// </summary>
        public int IndexBufferHandle { get; private set; }

        /// <summary>
        /// The Vertex Array Object Handle
        /// </summary>
        public int ArrayHandle { get; private set; }

        public bool IsDisposed { get; private set; }


        // StaticDraw: the data will most likely not change at all or very rarely.
        // DynamicDraw: the data is likely to change a lot.
        // StreamDraw: the data will change every time it is drawn.
        /// <summary>
        /// Create a static VertexArrayBuffer.
        /// </summary>
        public VertexArrayBuffer(Vertex[] vertices, uint[] indices)
        {
            IndexCount = indices.Length;
            BufferHandle = GL.GenBuffer();
            IndexBufferHandle = GL.GenBuffer();
            ArrayHandle = GL.GenVertexArray();

            Bind();
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * Vertex.Size, vertices, BufferUsageHint.StaticDraw);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
            SetShaderAttribs();
            Unbind();
        }

        public void Draw()
        {
            GL.BindVertexArray(ArrayHandle);
            GL.DrawElements(PrimitiveType.Triangles, IndexCount, DrawElementsType.UnsignedInt, 0);
        }

        private void SetShaderAttribs()
        {
            //int posLocation = Shader.GetAttributeLocation("in_position");
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vertex.Size, 0);

            //int texLocation = Shader.GetAttributeLocation("in_texCoord");
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vertex.Size, 12);

            //int normLocation = Shader.GetAttributeLocation("in_normals");
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, Vertex.Size, 20);
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                GL.DeleteBuffer(IndexBufferHandle);
                GL.DeleteVertexArray(ArrayHandle);
                GL.DeleteBuffer(BufferHandle);

                IsDisposed = true;
            }
        }

        private void Bind()
        {
            GL.BindVertexArray(ArrayHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, BufferHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, IndexBufferHandle);
        }

        private void Unbind()
        {
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

    }
}
