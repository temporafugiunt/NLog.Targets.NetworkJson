using System.IO;

namespace GDNetworkJSONService.LocalLogStorageDB
{
    public class GdDbHelper
    {
        public static string[] GetGdDbListFromDirectory(string searchDirectory)
        {
            return Directory.GetFiles(searchDirectory, "*.sqlite");
        }
    }
}
