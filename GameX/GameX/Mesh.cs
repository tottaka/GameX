using System;
using System.Collections.Generic;
using System.Numerics;

namespace GameX
{
    public struct BoundingBox
    {
        public Vector3 Min, Max;
        public Vector3 Size;
        public Vector3 Center;

        public BoundingBox(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
            Size = new Vector3(max.X - min.X, max.Y - min.Y, max.Z - min.Z);
            Center = new Vector3((min.X + max.X) / 2.0f, (min.Y + max.Y) / 2, (min.Z + max.Z) / 2.0f);
        }

    }

    public sealed class Mesh
    {
        public int IndexCount => Indices.Length;
        public uint[] Indices;

        public Vertex[] Vertices;
        public BoundingBox BoundingBox;

        internal static Mesh FromAssimp(Assimp.Mesh mesh)
        {
            Mesh newMesh = new Mesh();
            newMesh.Indices = mesh.GetUnsignedIndices();


            bool hasTexCoords = mesh.HasTextureCoords(0);
            List<Assimp.Vector3D> uvs = hasTexCoords ? mesh.TextureCoordinateChannels[0] : null;

            // bounding box values
            float min_x, max_x, min_y, max_y, min_z, max_z;
            min_x = max_x = mesh.Vertices[0].X;
            min_y = max_y = mesh.Vertices[0].Y;
            min_z = max_z = mesh.Vertices[0].Z;

            Vertex[] vertices = new Vertex[mesh.VertexCount];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vec = mesh.Vertices[i].ToNumeric();
                Vector3 norm = mesh.Normals[i].ToNumeric();

                if (hasTexCoords)
                {

                    Assimp.Vector3D uv = uvs[i];
                    vertices[i] = new Vertex(vec, new Vector2(uv.X, 1.0f - uv.Y), norm);

                }
                else
                {

                    vertices[i] = new Vertex(vec, Vector2.Zero, norm);

                }

                if (vec.X < min_x) min_x = vec.X;
                if (vec.X > max_x) max_x = vec.X;
                if (vec.Y < min_y) min_y = vec.Y;
                if (vec.Y > max_y) max_y = vec.Y;
                if (vec.Z < min_z) min_z = vec.Z;
                if (vec.Z > max_z) max_z = vec.Z;

            }

            newMesh.BoundingBox = new BoundingBox(new Vector3(min_x, min_y, min_z), new Vector3(max_x, max_y, max_z));
            newMesh.Vertices = vertices;
            return newMesh;
        }

        public static BoundingBox GetBoundingBox(Vector3[] vertices)
        {
            float min_x, max_x, min_y, max_y, min_z, max_z;
            min_x = max_x = vertices[0].X;
            min_y = max_y = vertices[0].Y;
            min_z = max_z = vertices[0].Z;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (vertices[i].X < min_x) min_x = vertices[i].X;
                if (vertices[i].X > max_x) max_x = vertices[i].X;
                if (vertices[i].Y < min_y) min_y = vertices[i].Y;
                if (vertices[i].Y > max_y) max_y = vertices[i].Y;
                if (vertices[i].Z < min_z) min_z = vertices[i].Z;
                if (vertices[i].Z > max_z) max_z = vertices[i].Z;
            }

            return new BoundingBox(new Vector3(min_x, min_y, min_z), new Vector3(max_x, max_y, max_z));
        }

    }


    public abstract class PrimitiveMesh
    {
        public static Mesh Cube
        {
            get
            {
                Mesh CubeMesh = new Mesh
                {
                    Indices = new uint[] {
                        // note that we start from 0!
                        0, 1, 3,   // first triangle
                        1, 2, 3,    // second triangle

                        7, 5, 4,   // third triangle
                        7, 6, 5,    // 4th triangle
                    },

                    Vertices = new Vertex[] {

                        // front face
                        new Vertex(new Vector3(0.5f,  0.5f, 0.5f), new Vector2(1.0f, 0.0f), new Vector3(0.0f, 0.0f, 1.0f)),  // top right
                        new Vertex(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(1.0f, 1.0f), new Vector3(0.0f, 0.0f, 1.0f)),  // bottom right
                        new Vertex(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(0.0f, 1.0f), new Vector3(0.0f, 0.0f, 1.0f)),  // bottom left
                        new Vertex(new Vector3(-0.5f,  0.5f, 0.5f), new Vector2(0.0f, 0.0f), new Vector3(0.0f, 0.0f, 1.0f)),   // top left

                        // back face
                        new Vertex(new Vector3(0.5f,  0.5f, -0.5f), new Vector2(1.0f, 0.0f), new Vector3(0.0f, 0.0f, -1.0f)),  // top right
                        new Vertex(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(1.0f, 1.0f), new Vector3(0.0f, 0.0f, -1.0f)),  // bottom right
                        new Vertex(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0.0f, 1.0f), new Vector3(0.0f, 0.0f, -1.0f)),  // bottom left
                        new Vertex(new Vector3(-0.5f,  0.5f, -0.5f), new Vector2(0.0f, 0.0f), new Vector3(0.0f, 0.0f, -1.0f)),   // top left

                        /*
                        new Vertex(new Vector3(0.5f,  0.5f, 1.0f), new Vector2(1.0f, 0.0f), new Vector3(0.0f, 0.0f, -1.0f)),  // top right
                        new Vertex(new Vector3(0.5f, -0.5f, 1.0f), new Vector2(1.0f, 1.0f), new Vector3(0.0f, 0.0f, -1.0f)),  // bottom right
                        new Vertex(new Vector3(-0.5f, -0.5f, 1.0f), new Vector2(0.0f, 1.0f), new Vector3(0.0f, 0.0f, -1.0f)),  // bottom left
                        new Vertex(new Vector3(-0.5f,  0.5f, 1.0f), new Vector2(0.0f, 0.0f), new Vector3(0.0f, 0.0f, -1.0f)),   // top left
                        */
                    }
                };

                return CubeMesh;
            }
        }
    }

}
