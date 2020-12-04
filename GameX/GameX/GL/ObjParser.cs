using Assimp.Configs;
using Assimp;

namespace GameX
{
    public abstract class ObjectLoader
    {

        public static Assimp.Scene Import(string filePath)
        {
            AssimpContext importer = new AssimpContext();
            //importer.SetConfig(new NormalSmoothingAngleConfig(66.0f));
            return importer.ImportFile(filePath, PostProcessPreset.TargetRealTimeMaximumQuality);
        }

    }
}
