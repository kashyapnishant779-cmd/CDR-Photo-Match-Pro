using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CDR_Photo_Match_Pro
{
    public static class ExportHelper
    {
        public static void ExportCsv(string path, IEnumerable<MatchResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Match %,File Name,Full Path,Folder,File Size,Preview Status");
            foreach (var r in results)
            {
                sb.AppendLine(string.Format("{0},\"{1}\",\"{2}\",\"{3}\",{4},\"{5}\"", r.MatchPercent.ToString("0.00"), Escape(r.FileName), Escape(r.FullPath), Escape(r.Folder), r.FileSize, Escape(r.PreviewStatus)));
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        public static void ExportTxt(string path, IEnumerable<MatchResult> results)
        {
            var sb = new StringBuilder();
            foreach (var r in results)
            {
                sb.AppendLine("Match: " + r.MatchPercent.ToString("0.00") + "%");
                sb.AppendLine("File: " + r.FileName);
                sb.AppendLine("Path: " + r.FullPath);
                sb.AppendLine("Folder: " + r.Folder);
                sb.AppendLine("Status: " + r.PreviewStatus);
                sb.AppendLine(new string('-', 60));
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static string Escape(string value)
        {
            return (value ?? "").Replace("\"", "\"\"");
        }
    }
}
