using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using System.Xml;
using System.Xml.Linq;

namespace GameX.Editor
{
    public class ProjectLoader
    {
        internal readonly string ProjectPath;
        internal readonly string AssetPath;

        public bool IsLoading { get; private set; }

        public AssetMenuItem AssetRoot;

        public ProjectLoader(string projectPath)
        {
            ProjectPath = projectPath;
            AssetPath = Path.Combine(ProjectPath, "Assets");
        }

        public void Load()
        {
            if (IsLoading)
                return;

            if (!Directory.Exists(AssetPath))
                Directory.CreateDirectory(AssetPath);

            IsLoading = true;
            Task.Run(LoadAssets).ContinueWith(t => {
                IsLoading = false;
            });
        }

        private void LoadAssets()
        {
            DirectoryInfo projectDirectory = new DirectoryInfo(AssetPath);
            AssetMenuItem rootAsset = new AssetMenuItem();
            rootAsset.Type = AssetItemType.Folder;
            rootAsset.Name = projectDirectory.Name;
            rootAsset.SystemPath = projectDirectory.FullName;
            rootAsset.RelativePath = Path.GetRelativePath(AssetPath, projectDirectory.FullName);

            foreach (FileSystemInfo entry in projectDirectory.EnumerateFileSystemInfos())
            {
                bool isFolder = entry.Attributes.HasFlag(FileAttributes.Directory);
                if(isFolder)
                {
                    DirectoryInfo directory = new DirectoryInfo(entry.FullName);
                    AssetMenuItem folderItem = LoadFolder(directory, rootAsset);
                    rootAsset.Children.Add(folderItem);
                }
                else
                {
                    if (Path.GetExtension(entry.FullName) == ".meta")
                        continue;

                    AssetMenuItem fileItem = LoadFile(entry, rootAsset);
                    rootAsset.Children.Add(fileItem);
                }
            }

            AssetRoot = rootAsset;
        }


        private AssetMenuItem LoadFolder(DirectoryInfo path, AssetMenuItem parent = null)
        {
            AssetMenuItem item = new AssetMenuItem();
            item.Type = AssetItemType.Folder;
            item.Name = path.Name;
            item.Parent = parent;
            item.SystemPath = path.FullName;
            item.RelativePath = Path.GetRelativePath(AssetPath, path.FullName);

            foreach (FileSystemInfo entry in path.EnumerateFileSystemInfos())
            {
                bool isFolder = entry.Attributes.HasFlag(FileAttributes.Directory);
                if (isFolder)
                {
                    DirectoryInfo directory = new DirectoryInfo(entry.FullName);
                    AssetMenuItem folderItem = LoadFolder(directory, item);
                    item.Children.Add(folderItem);
                }
                else
                {
                    if (Path.GetExtension(entry.FullName) == ".meta")
                        continue;

                    AssetMenuItem fileItem = LoadFile(entry, item);
                    item.Children.Add(fileItem);
                }
            }

            return item;
        }

        private AssetMenuItem LoadFile(FileSystemInfo file, AssetMenuItem parent = null)
        {
            string metaPath = Path.ChangeExtension(file.FullName, ".meta");
            XElement metaData;
            if (!File.Exists(metaPath))
            {
                using (StreamWriter metaWriter = File.CreateText(metaPath))
                {
                    metaData = CreateAssetMeta(file.FullName);
                    metaData.Save(metaWriter);
                }

                File.SetAttributes(metaPath, FileAttributes.Hidden);
            }
            else
            {
                metaData = XElement.Load(metaPath);
            }

            AssetItemType assetType = GetAssetFileType(file.FullName);

            AssetMenuItem item = new AssetMenuItem();
            item.Type = assetType;
            item.Name = file.Name;
            item.Parent = parent;
            item.SystemPath = file.FullName;
            item.RelativePath = Path.GetRelativePath(AssetPath, file.FullName);

            foreach (XElement elem in metaData.Elements())
                ApplyAssetItemMetaData(ref item, elem);

            return item;
        }


        public static AssetItemType GetAssetFileType(string filePath)
        {
            string extension = Path.GetExtension(filePath).TrimStart('.');
            switch (extension)
            {
                default:
                    return AssetItemType.Unknown;
                case "obj":
                case "fbx":
                    return AssetItemType.Mesh;
                case "mat":
                    return AssetItemType.Material;
                case "shader":
                    return AssetItemType.Shader;
                case "jpg":
                case "jpeg":
                case "bmp":
                case "png":
                    return AssetItemType.Texture;

            }
        }

        public static void ApplyAssetItemMetaData(ref AssetMenuItem item, XElement metaData)
        {
            if(item.Type == AssetItemType.Mesh)
            {
                item.Children.Add(CreateSubMeshItem(item, metaData));
            }
            else
            {

            }
        }

        public static AssetMenuItem CreateSubMeshItem(AssetMenuItem parent, XElement item)
        {
            AssetMenuItem subItem = new AssetMenuItem();
            subItem.Type = parent.Type;
            subItem.Name = item.InnerText();
            subItem.Parent = parent;
            subItem.SystemPath = parent.SystemPath;
            subItem.RelativePath = parent.RelativePath;
            subItem.SubItem = true;

            foreach (XElement elem in item.Elements("Mesh"))
                subItem.Children.Add(CreateSubMeshItem(subItem, elem));

            return subItem;
        }

        public static XElement CreateAssetMeta(string filePath)
        {
            AssetItemType assetType = GetAssetFileType(filePath);
            XElement metaData = new XElement("MetaData");

            switch (assetType)
            {
                default:
                    // unknown file
                    break;
                case AssetItemType.Mesh:
                    Assimp.Scene scene = ObjectLoader.Import(filePath);
                    foreach(Assimp.Node node in scene.RootNode.Children)
                        metaData.Add(BuildAssimpNodeTree(node));
                    break;
            }
            
            return metaData;
        }

        public static XElement BuildAssimpNodeTree(Assimp.Node node)
        {
            XElement element = new XElement("Mesh", node.Name);
            foreach (Assimp.Node child in node.Children)
                element.Add(BuildAssimpNodeTree(child));
            return element;
        }

    }

    public static class Extensions
    {
        public static string InnerText(this XElement element)
        {
            foreach (XNode node in element.Nodes())
                if (node.NodeType == XmlNodeType.Text)
                    return node.ToString();
            return string.Empty;
        }
    }
}
