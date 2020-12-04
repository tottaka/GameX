using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace GameX.Editor
{
    public enum AssetItemType
    {
        Unknown,
        Folder,
        Mesh,
        Text,
        Texture,
        Material,
        Shader
    };

    public sealed class AssetMenuItem
    {
        public string Name { get; internal set; }

        public string SystemPath { get; internal set; }
        public string RelativePath { get; internal set; }
        public AssetItemType Type { get; internal set; }

        public bool Renaming = false;

        internal bool SubItem = false;

        //internal int AssetIndex;
        internal AssetMenuItem Parent;
        internal List<AssetMenuItem> Children = new List<AssetMenuItem>();

    }
}
