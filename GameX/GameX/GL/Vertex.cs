//using OpenTK.Mathematics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace GameX
{
    public struct Vertex
    {
        /// <summary>
        /// Size of the <see cref="Vertex"/> struct in bytes.
        /// </summary>
        public static readonly int Size = Unsafe.SizeOf<Vertex>();

        public readonly Vector3 Position;
        public readonly Vector2 UV;
        public readonly Vector3 Normal;

        public Vertex(Vector3 position, Vector2 uv, Vector3 normal)
        {
            Position = position;
            UV = uv;
            Normal = normal;
        }
    }

    public struct Line
    {
        /// <summary>
        /// Size of the <see cref="Line"/> struct in bytes.
        /// </summary>
        public static readonly int Size = Unsafe.SizeOf<Line>();

        public readonly Vector3 Position;
        public readonly Vector3 Color;

        public Line(Vector3 pos, Vector3 color)
        {
            Position = pos;
            Color = color;
        }
    }
    public static class VectorExtensions
    {

        public static Vector3 ToNumeric(this Assimp.Vector3D vector) => new Vector3(vector.X, vector.Y, vector.Z);
        public static Vector3 ToNumeric(this OpenTK.Mathematics.Vector3 vector) => new Vector3(vector.X, vector.Y, vector.Z);
        public static Vector2 ToNumeric(this Assimp.Vector2D vector) => new Vector2(vector.X, vector.Y);
        public static Vector2 ToNumeric(this OpenTK.Mathematics.Vector2 vector) => new Vector2(vector.X, vector.Y);

    }
}
