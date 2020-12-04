using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace GameX
{
    public class Scene
    {
        public static Scene CurrentScene { get; private set; }

        public List<GameObject> gameObjects = new List<GameObject>();
        public List<int> Cameras = new List<int>();

        internal static Dictionary<string, Mesh> MeshCache = new Dictionary<string, Mesh>();

        // Lighting Settings
        public float AmbientLightStrength = 0.5f;
        public Vector3 AmbientLightColor = Vector3.One;

        public static void Load(Scene scene)
        {
            if (CurrentScene != null)
                CurrentScene.Close();
            CurrentScene = scene;
        }

        public void Close()
        {
            foreach (GameObject obj in gameObjects)
                obj.Dispose();

            gameObjects.Clear();
            CurrentScene = null;
        }

        public void Save(string path)
        {
            byte[] sceneData = Compression.Zip(TypeSerializer.Serialize(this).ToArray());
            File.WriteAllBytes(path, sceneData);
        }

        public static Scene FromFile(string path, string basePath)
        {
            byte[] sceneData = Compression.Unzip(File.ReadAllBytes(path));
            TypeSerializer.Deserialize(ref sceneData, 0, out Scene scene);

            foreach (GameObject obj in scene.gameObjects)
            {
                if (obj.Renderer != null)
                    obj.Renderer.SetMesh(LoadMeshData(obj.Renderer.RelativeMeshPath, basePath));
            }


            return scene;
        }

        internal static Mesh LoadMeshData(string relativePath, string basePath)
        {
            if (!MeshCache.ContainsKey(relativePath))
            {
                string meshPath = Path.Combine(basePath, relativePath);
                if (File.Exists(meshPath))
                {
                    Console.WriteLine("Found: {0}", meshPath);
                    Assimp.Scene importer = ObjectLoader.Import(meshPath);
                    MeshCache.Add(relativePath, Mesh.FromAssimp(importer.Meshes[0]));
                }
                else
                {
                    Console.WriteLine("Mesh does not exist at '{0}'", meshPath);
                    return null;
                }
            }

            return MeshCache[relativePath];
        }
    }

    internal static partial class TypeSerializer
    {
        internal static List<byte> Serialize(Scene scene)
        {
            List<byte> data = new List<byte>();

            data.AddRange(BitConverter.GetBytes(scene.AmbientLightStrength));
            data.AddRange(TypeSerializer.Serialize(scene.AmbientLightColor));

            data.AddRange(BitConverter.GetBytes(scene.gameObjects.Count));
            foreach (GameObject gameObject in scene.gameObjects)
                data.AddRange(TypeSerializer.Serialize(gameObject));

            return data;
        }

        internal static int Deserialize(ref byte[] data, int startIndex, out Scene scene)
        {
            int byteIndex = 0;
            float lightStrength = BitConverter.ToSingle(data, startIndex);
            byteIndex += sizeof(float);

            byteIndex += TypeSerializer.Deserialize(ref data, startIndex + byteIndex, out System.Numerics.Vector3 lightColor);

            int objectCount = BitConverter.ToInt32(data, startIndex + byteIndex);
            byteIndex += sizeof(int);

            List<GameObject> GameObjects = new List<GameObject>();
            for (int i = 0; i < objectCount; i++) {
                int byteCount = TypeSerializer.Deserialize(ref data, startIndex + byteIndex, out GameObject gameObject);
                GameObjects.Add(gameObject);
                byteIndex += byteCount;
            }

            scene = new Scene {
                AmbientLightStrength = lightStrength,
                AmbientLightColor = new Vector3(lightColor.X, lightColor.Y, lightColor.Z),
                gameObjects = GameObjects
            };

            return byteIndex;
        }
    }

}
