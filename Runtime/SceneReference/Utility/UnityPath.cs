using System.IO;

namespace Onion.SceneManagement.Utility {
    public class UnityPath {
        private const char UnitySeparator = '/';
        private const char WindowsSeparator = '\\';

        public string normalizedPath { get; }
        public string platformPath { get; }
        public string metaPath { get; }

        public string fileName { get; }
        public string fileNameWithoutExtension { get; }
        public string fileExtension { get; }

        public UnityPath(string path) {
            normalizedPath = path.Replace(WindowsSeparator, UnitySeparator);
            platformPath = normalizedPath.Replace(UnitySeparator, Path.DirectorySeparatorChar);
            metaPath = normalizedPath + ".meta";

            fileName = Path.GetFileName(normalizedPath);
            fileNameWithoutExtension = Path.GetFileNameWithoutExtension(normalizedPath);
            fileExtension = Path.GetExtension(normalizedPath);
        }
        
        public override string ToString() {
            return normalizedPath;
        }

        public static implicit operator string(UnityPath unityPath) {
            return unityPath.normalizedPath;
        }
    }
}