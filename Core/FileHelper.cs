using System.IO;
using static System.Net.Mime.MediaTypeNames;

namespace Core
{
    internal class FileHelper
    {
    }
    public static class FileExt
    {
        public static bool IsSameFiles(this string NotNormalized, string Normalized)
        {
            return string.Equals(Path.GetFullPath(NotNormalized).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                Normalized, StringComparison.InvariantCultureIgnoreCase);
        }
        public static bool IsSameFiles(this string Relative,string Root, string Normalized)
        {
            return string.Equals(Path.GetFullPath(Relative, 
                Root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar).
                TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                Normalized, StringComparison.InvariantCultureIgnoreCase);
        }
        public static string Relative(this string ext, string root)
        {
            return Path.GetRelativePath(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, ext);
        }
        public static string FullPath(this string ext, string root)
        {
            return Path.GetFullPath(ext, root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
        }

    }
}
