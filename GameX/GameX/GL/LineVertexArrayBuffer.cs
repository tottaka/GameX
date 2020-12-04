using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL4;

namespace GameX
{
    public sealed class LineVertexArrayBuffer : IDisposable
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
        public LineVertexArrayBuffer(Vector3[] lines)
        {
            IndexCount = lines.Length;
            BufferHandle = GL.GenBuffer();
            ArrayHandle = GL.GenVertexArray();

            Bind();
            GL.BufferData(BufferTarget.ArrayBuffer, lines.Length * sizeof(float) * 3, lines, BufferUsageHint.StaticDraw);
            SetShaderAttribs();
            Unbind();
        }

        public void Draw(PrimitiveType type = PrimitiveType.Lines, int startIndex = 0, int count = -1)
        {
            GL.BindVertexArray(ArrayHandle);
            GL.DrawArrays(type, startIndex, count == -1 ? IndexCount : count);
        }

        private void SetShaderAttribs()
        {
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(float) * 3, 0);

            //GL.EnableVertexAttribArray(1);
            //GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, Line.Size, 12);
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                GL.DeleteVertexArray(ArrayHandle);
                GL.DeleteBuffer(BufferHandle);

                IsDisposed = true;
            }
        }

        private void Bind()
        {
            GL.BindVertexArray(ArrayHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, BufferHandle);
        }

        private void Unbind()
        {
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

    }
}
