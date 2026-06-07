using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CDR_Photo_Match_Pro
{
    public static class FolderManager
    {
        private static readonly string StoreFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "folders.txt");

        public static void Save(IEnumerable<string> folders)
        {
            try { File.WriteAllLines(StoreFile, folders.Where(Directory.Exists).Distinct().ToArray()); }
            catch { }
        }

        public static List<string> Load()
        {
            try
            {
                if (!File.Exists(StoreFile)) return new List<string>();
                return File.ReadAllLines(StoreFile).Where(Directory.Exists).Distinct().ToList();
            }
            catch { return new List<string>(); }
        }

        public static List<string> GetCdrFiles(IEnumerable<string> folders)
        {
            var result = new List<string>();
            foreach (var folder in folders.Where(Directory.Exists))
            {
                try { result.AddRange(Directory.GetFiles(folder, "*.cdr", SearchOption.AllDirectories)); }
                catch { }
            }
            return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
