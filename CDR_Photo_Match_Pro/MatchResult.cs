using System;

namespace CDR_Photo_Match_Pro
{
    public class MatchResult
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public string Folder { get; set; }
        public double MatchPercent { get; set; }
        public long FileSize { get; set; }
        public string PreviewStatus { get; set; }
    }
}
