using BepuPhysics;
using BepuPhysics.Collidables;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Text;
using MathHelper = OpenTK.Mathematics.MathHelper;

namespace GameX
{
    public class GameObject : IDisposable
    {
        public string Name;
        public Transform transform = new Transform();

        public bool Static = false;

        public MeshRenderer Renderer;
        public Material Material;

        public bool IsDisposed { get; private set; }

        public float Mass = 1.0f;

        private bool physicsEnabled = false;

        internal bool PhysicsBodyInitialized = false;
        internal IConvexShape Shape;
        internal TypedIndex ShapeIndex;
        internal BodyDescription PhysicsDescription;
        internal BodyHandle BodyHandle;
        internal BodyReference BodyReference;
        internal PhysicsType PhysicsType { get; private set; }

        public void Dispose()
        {
            if(!IsDisposed)
            {
                RemoveCollider();

                if (Physics.PhysicsObjects.Contains(this))
                    Physics.PhysicsObjects.Remove(this);

                if (Renderer != null)
                    Renderer.Dispose();

                IsDisposed = true;
            }
        }

        internal void UpdatePhysicsBody()
        {
            if (PhysicsBodyInitialized)
            {
                transform.Position = BodyReference.Pose.Position;
                transform.Rotation = BodyReference.Pose.Orientation;
            }
        }

        public void SetCollider(PhysicsType type)
        {
            if(PhysicsBodyInitialized)
            {
                RemoveCollider();

                if (type == PhysicsType.None)
                {
                    PhysicsType = type;
                    if (Physics.PhysicsObjects.Contains(this))
                        Physics.PhysicsObjects.Remove(this);

                    return;
                }
            }

            if (type == PhysicsType.Cube)
            {
                Box shape = new Box(2.0f, 2.0f, 2.0f);
                ShapeIndex = Physics.simulator.Shapes.Add(shape);
                Shape = shape;
            }
            else if(type == PhysicsType.Sphere)
            {
                Sphere shape = new Sphere(2.0f);
                ShapeIndex = Physics.simulator.Shapes.Add(shape);
                Shape = shape;
            }

            Shape.ComputeInertia(Mass, out BodyInertia inertia);
            
            PhysicsDescription = BodyDescription.CreateDynamic(transform.Position, inertia, new CollidableDescription(ShapeIndex, 0.1f), new BodyActivityDescription(0.01f));
            BodyHandle = Physics.simulator.Bodies.Add(PhysicsDescription);
            BodyReference = Physics.simulator.Bodies.GetBodyReference(BodyHandle);

            if (!Physics.PhysicsObjects.Contains(this))
                Physics.PhysicsObjects.Add(this);

            PhysicsType = type;
            PhysicsBodyInitialized = true;
            physicsEnabled = true;
        }

        private void RemoveCollider()
        {
            if (PhysicsBodyInitialized)
            {
                Physics.simulator.Bodies.Remove(BodyHandle);
                Physics.simulator.Shapes.Remove(ShapeIndex);
                PhysicsBodyInitialized = false;
            }
        }

    }

    internal static partial class TypeSerializer
    {
        public static List<byte> Serialize(GameObject @object)
        {
            List<byte> data = new List<byte>();

            // Serialize name
            byte[] name = Encoding.Unicode.GetBytes(@object.Name);
            byte[] nameLength = BitConverter.GetBytes(name.Length);
            data.AddRange(nameLength);
            data.AddRange(name);

            // Serialize transform
            data.AddRange(Serialize(@object.transform.Position));
            data.AddRange(Serialize(@object.transform.Rotation));
            data.AddRange(Serialize(@object.transform.Scale));

            // Serialize mesh renderer
            bool hasMeshRenderer = @object.Renderer != null;
            data.AddRange(BitConverter.GetBytes(hasMeshRenderer));
            if (hasMeshRenderer)
                data.AddRange(Serialize(@object.Renderer));

            bool hasMaterial = @object.Material != null;
            data.AddRange(BitConverter.GetBytes(hasMaterial));
            if (hasMaterial)
                data.AddRange(Serialize(@object.Material));

            return data;
        }

        public static List<byte> Serialize(Material mat)
        {
            List<byte> data = new List<byte>();

            bool hasShader = mat.Shader != null;
            data.AddRange(BitConverter.GetBytes(hasShader));
            if(hasShader)
                data.AddRange(Serialize(mat.Shader));

            bool hasDiffuseTexture = mat.DiffuseTexture != null;
            data.AddRange(BitConverter.GetBytes(hasDiffuseTexture));
            if (hasDiffuseTexture)
                data.AddRange(Serialize(mat.DiffuseTexture));

            bool hasNormalTexture = mat.NormalTexture != null;
            data.AddRange(BitConverter.GetBytes(hasNormalTexture));
            if (hasNormalTexture)
                data.AddRange(Serialize(mat.NormalTexture));

            bool hasSpecularTexture = mat.SpecularTexture != null;
            data.AddRange(BitConverter.GetBytes(hasSpecularTexture));
            if (hasSpecularTexture)
                data.AddRange(Serialize(mat.SpecularTexture));

            return data;
        }

        public static List<byte> Serialize(Shader shader)
        {
            List<byte> data = new List<byte>();

            // Add vertex location first
            byte[] vertPath = Encoding.Unicode.GetBytes(shader.RelativeVertexShader);
            byte[] vertLength = BitConverter.GetBytes(vertPath.Length);
            data.AddRange(vertLength);
            data.AddRange(vertPath);

            // then we add the fragment location
            byte[] fragPath = Encoding.Unicode.GetBytes(shader.RelativeFragmentShader);
            byte[] fragLength = BitConverter.GetBytes(fragPath.Length);
            data.AddRange(fragLength);
            data.AddRange(fragPath);

            return data;
        }

        public static List<byte> Serialize(Texture2D tex2d)
        {
            List<byte> data = new List<byte>();
            byte[] localPath = Encoding.Unicode.GetBytes(tex2d.RelativeAssetPath);
            byte[] pathLength = BitConverter.GetBytes(localPath.Length);
            data.AddRange(pathLength);
            data.AddRange(localPath);

            // generateMipMaps = MipmapLevels > 1
            data.AddRange(BitConverter.GetBytes(tex2d.MipmapLevels > 1));

            return data;
        }

        public static List<byte> Serialize(this MeshRenderer renderer)
        {
            List<byte> data = new List<byte>();
            byte[] localPath = Encoding.Unicode.GetBytes(renderer.RelativeMeshPath);
            byte[] pathLength = BitConverter.GetBytes(localPath.Length);
            data.AddRange(pathLength);
            data.AddRange(localPath);
            return data;
        }

        public static List<byte> Serialize(this Vector3 vector)
        {
            List<byte> data = new List<byte>(sizeof(float) * 3);
            data.AddRange(BitConverter.GetBytes(vector.X));
            data.AddRange(BitConverter.GetBytes(vector.Y));
            data.AddRange(BitConverter.GetBytes(vector.Z));
            return data;
        }

        public static List<byte> Serialize(this Quaternion quaternion)
        {
            List<byte> data = new List<byte>(sizeof(float) * 4);
            data.AddRange(BitConverter.GetBytes(quaternion.X));
            data.AddRange(BitConverter.GetBytes(quaternion.Y));
            data.AddRange(BitConverter.GetBytes(quaternion.Z));
            data.AddRange(BitConverter.GetBytes(quaternion.W));
            return data;
        }

        public static int Deserialize(ref byte[] data, int startIndex, out Quaternion vector)
        {
            vector = new Quaternion(BitConverter.ToSingle(data, startIndex), BitConverter.ToSingle(data, startIndex + sizeof(float)), BitConverter.ToSingle(data, startIndex + (sizeof(float) * 2)), BitConverter.ToSingle(data, startIndex + (sizeof(float) * 3)));
            return sizeof(float) * 3;
        }

        public static int Deserialize(ref byte[] data, int startIndex, out Vector3 vector)
        {
            vector = new Vector3(BitConverter.ToSingle(data, startIndex), BitConverter.ToSingle(data, startIndex + sizeof(float)), BitConverter.ToSingle(data, startIndex + (sizeof(float) * 2)));
            return sizeof(float) * 3;
        }


        public static int Deserialize(ref byte[] data, int startIndex, out MeshRenderer renderer)
        {
            int byteIndex = 0;

            int pathLength = BitConverter.ToInt32(data, startIndex);
            byteIndex += sizeof(int);

            string relativePath = Encoding.Unicode.GetString(data, startIndex + byteIndex, pathLength);
            byteIndex += pathLength;

            renderer = new MeshRenderer(relativePath);

            return byteIndex;
        }

        public static int Deserialize(ref byte[] data, int startIndex, out Shader shader)
        {
            int byteIndex = 0;

            int vertPathLength = BitConverter.ToInt32(data, startIndex);
            byteIndex += sizeof(int);

            string vertPath = Encoding.Unicode.GetString(data, startIndex + byteIndex, vertPathLength);
            byteIndex += vertPathLength;

            int fragPathLength = BitConverter.ToInt32(data, startIndex + byteIndex);
            byteIndex += sizeof(int);

            string fragPath = Encoding.Unicode.GetString(data, startIndex + byteIndex, fragPathLength);
            byteIndex += fragPathLength;

            shader = new Shader(vertPath, fragPath);
            return byteIndex;
        }

        public static int Deserialize(ref byte[] data, int startIndex, out Texture2D tex2d)
        {
            int byteIndex = 0;

            int pathLength = BitConverter.ToInt32(data, startIndex);
            byteIndex += sizeof(int);

            string relativePath = Encoding.Unicode.GetString(data, startIndex + byteIndex, pathLength);
            byteIndex += pathLength;

            bool mipmaps = BitConverter.ToBoolean(data, startIndex + byteIndex);
            byteIndex += sizeof(bool);

            tex2d = new Texture2D(relativePath, mipmaps, OpenTK.Graphics.OpenGL4.TextureUnit.Texture0);
            return byteIndex;
        }

        public static int Deserialize(ref byte[] data, int startIndex, out Material material)
        {
            int byteIndex = 0;

            bool hasShader = BitConverter.ToBoolean(data, startIndex);
            byteIndex += sizeof(bool);

            Shader shader = null;
            if(hasShader)
                byteIndex += TypeSerializer.Deserialize(ref data, startIndex, out shader);

            bool hasDiffuseTexture = BitConverter.ToBoolean(data, startIndex + byteIndex);
            byteIndex += sizeof(bool);

            Texture2D diff = null;
            if (hasDiffuseTexture)
                byteIndex += TypeSerializer.Deserialize(ref data, startIndex + byteIndex, out diff);

            bool hasNormalTexture = BitConverter.ToBoolean(data, startIndex + byteIndex);
            byteIndex += sizeof(bool);

            Texture2D norm = null;
            if (hasNormalTexture)
                byteIndex += TypeSerializer.Deserialize(ref data, startIndex + byteIndex, out norm);

            bool hasSpecularTexture = BitConverter.ToBoolean(data, startIndex + byteIndex);
            byteIndex += sizeof(bool);

            Texture2D spec = null;
            if (hasSpecularTexture)
                byteIndex += TypeSerializer.Deserialize(ref data, startIndex + byteIndex, out spec);

            material = new Material(shader, diff, norm, spec);
            return byteIndex;
        }

        public static int Deserialize(ref byte[] data, int startIndex, out GameObject @object)
        {
            int byteIndex = 0;

            int nameLength = BitConverter.ToInt32(data, startIndex);
            byteIndex += sizeof(int);

            string name = Encoding.Unicode.GetString(data, startIndex + byteIndex, nameLength);
            byteIndex += nameLength;

            byteIndex += TypeSerializer.Deserialize(ref data, startIndex + byteIndex, out Vector3 position);
            byteIndex += TypeSerializer.Deserialize(ref data, startIndex + byteIndex, out Quaternion rotation);
            byteIndex += TypeSerializer.Deserialize(ref data, startIndex + byteIndex, out Vector3 scale);


            bool hasMeshRenderer = BitConverter.ToBoolean(data, startIndex + byteIndex);
            byteIndex += sizeof(bool);
            MeshRenderer renderer = null;
            if (hasMeshRenderer)
                byteIndex += TypeSerializer.Deserialize(ref data, startIndex + byteIndex, out renderer);

            bool hasMaterial = BitConverter.ToBoolean(data, startIndex + byteIndex);
            byteIndex += sizeof(bool);
            Material mat = null;
            if (hasMaterial)
                byteIndex += TypeSerializer.Deserialize(ref data, startIndex + byteIndex, out mat);

            @object = new GameObject {
                Name = name,
                Renderer = renderer,
                Material = mat,
                transform = new Transform {
                    Position = position,
                    Rotation = rotation,
                    Scale = scale
                }
            };

            return byteIndex;
        }

    }

    public static class QuaternionExt
    {
        /// <summary>
        /// Convert this instance to an Euler angle representation.
        /// </summary>
        /// <returns>The Euler angles in radians.</returns>
        public static Vector3 ToEulerAngles(this Quaternion q)
        {
            /*
            reference
            http://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles
            http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/
            */

            Vector3 eulerAngles;

            // Threshold for the singularities found at the north/south poles.
            const float SINGULARITY_THRESHOLD = 0.4999995f;

            var sqw = q.W * q.W;
            var sqx = q.X * q.X;
            var sqy = q.Y * q.Y;
            var sqz = q.Z * q.Z;
            var unit = sqx + sqy + sqz + sqw; // if normalised is one, otherwise is correction factor
            var singularityTest = (q.X * q.Z) + (q.W * q.Y);

            if (singularityTest > SINGULARITY_THRESHOLD * unit)
            {
                eulerAngles.Z = (float)(2 * Math.Atan2(q.X, q.W));
                eulerAngles.Y = MathHelper.PiOver2;
                eulerAngles.X = 0;
            }
            else if (singularityTest < -SINGULARITY_THRESHOLD * unit)
            {
                eulerAngles.Z = (float)(-2 * Math.Atan2(q.X, q.W));
                eulerAngles.Y = -MathHelper.PiOver2;
                eulerAngles.X = 0;
            }
            else
            {
                eulerAngles.Z = MathF.Atan2(2 * ((q.W * q.Z) - (q.X * q.Y)), sqw + sqx - sqy - sqz);
                eulerAngles.Y = MathF.Asin(2 * singularityTest / unit);
                eulerAngles.X = MathF.Atan2(2 * ((q.W * q.X) - (q.Y * q.Z)), sqw - sqx - sqy + sqz);
            }

            return eulerAngles;
        }
    }
}
