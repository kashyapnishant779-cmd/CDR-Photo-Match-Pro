using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;

namespace CDR_Photo_Match_Pro
{
    public class MatchingEngine
    {
        public event Action<int, int, string> ProgressChanged;
        public event Action<MatchResult> ResultFound;
        private volatile bool _cancel;

        public void Cancel() { _cancel = true; }

        public List<MatchResult> Scan(string photoPath, List<string> folders)
        {
            _cancel = false;
            var results = new List<MatchResult>();
            using (Bitmap queryOriginal = new Bitmap(photoPath))
            using (Bitmap query = Normalize(queryOriginal, 64, 64))
            {
                double[] q1 = Fingerprint(query);
                double[] q2 = Fingerprint(Rotate(query, 90));
                double[] q3 = Fingerprint(Rotate(query, 180));
                double[] q4 = Fingerprint(Rotate(query, 270));
                double[][] queries = new[] { q1, q2, q3, q4 };

                var files = FolderManager.GetCdrFiles(folders);
                for (int i = 0; i < files.Count; i++)
                {
                    if (_cancel) break;
                    string file = files[i];
                    RaiseProgress(i + 1, files.Count, file);
                    string status;
                    Bitmap preview = CdrThumbnailExtractor.TryExtractPreview(file, out status);
                    if (preview == null)
                    {
                        continue;
                    }
                    try
                    {
                        using (preview)
                        using (Bitmap normalized = Normalize(preview, 64, 64))
                        {
                            double[] fp = Fingerprint(normalized);
                            double best = 0;
                            foreach (var q in queries) best = Math.Max(best, Similarity(q, fp));
                            best = Math.Max(best, CenterCropSimilarity(query, normalized));
                            var info = new FileInfo(file);
                            var r = new MatchResult
                            {
                                FileName = Path.GetFileName(file),
                                FullPath = file,
                                Folder = Path.GetDirectoryName(file),
                                MatchPercent = Math.Round(best * 100.0, 2),
                                FileSize = info.Length,
                                PreviewStatus = status
                            };
                            if (r.MatchPercent >= 45)
                            {
                                results.Add(r);
                                if (ResultFound != null) ResultFound(r);
                            }
                        }
                    }
                    catch { }
                }
            }
            return results.OrderByDescending(r => r.MatchPercent).ToList();
        }

        private void RaiseProgress(int done, int total, string file)
        {
            if (ProgressChanged != null) ProgressChanged(done, total, file);
        }

        private static Bitmap Normalize(Bitmap src, int w, int h)
        {
            Bitmap dst = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(dst))
            {
                g.Clear(Color.White);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(src, new Rectangle(0, 0, w, h));
            }
            return dst;
        }

        private static Bitmap Rotate(Bitmap src, float angle)
        {
            Bitmap dst = new Bitmap(src.Width, src.Height);
            using (Graphics g = Graphics.FromImage(dst))
            {
                g.Clear(Color.White);
                g.TranslateTransform(src.Width / 2f, src.Height / 2f);
                g.RotateTransform(angle);
                g.TranslateTransform(-src.Width / 2f, -src.Height / 2f);
                g.DrawImage(src, 0, 0);
            }
            return dst;
        }

        private static double[] Fingerprint(Bitmap bmp)
        {
            var values = new List<double>();
            int cells = 8;
            int cw = bmp.Width / cells;
            int ch = bmp.Height / cells;
            for (int cy = 0; cy < cells; cy++)
            for (int cx = 0; cx < cells; cx++)
            {
                double sum = 0, edge = 0;
                int count = 0;
                for (int y = cy * ch; y < Math.Min(bmp.Height - 1, (cy + 1) * ch); y++)
                for (int x = cx * cw; x < Math.Min(bmp.Width - 1, (cx + 1) * cw); x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    double gray = (c.R * 0.299 + c.G * 0.587 + c.B * 0.114) / 255.0;
                    Color cr = bmp.GetPixel(x + 1, y);
                    Color cb = bmp.GetPixel(x, y + 1);
                    double gr = (cr.R * 0.299 + cr.G * 0.587 + cr.B * 0.114) / 255.0;
                    double gb = (cb.R * 0.299 + cb.G * 0.587 + cb.B * 0.114) / 255.0;
                    sum += gray;
                    edge += Math.Abs(gray - gr) + Math.Abs(gray - gb);
                    count++;
                }
                values.Add(count == 0 ? 0 : sum / count);
                values.Add(count == 0 ? 0 : edge / count);
            }
            return values.ToArray();
        }

        private static double Similarity(double[] a, double[] b)
        {
            if (a.Length != b.Length) return 0;
            double diff = 0;
            for (int i = 0; i < a.Length; i++) diff += Math.Abs(a[i] - b[i]);
            double score = 1.0 - (diff / a.Length);
            if (score < 0) score = 0;
            if (score > 1) score = 1;
            return score;
        }

        private static double CenterCropSimilarity(Bitmap query, Bitmap preview)
        {
            Rectangle crop = new Rectangle(preview.Width / 8, preview.Height / 8, preview.Width * 3 / 4, preview.Height * 3 / 4);
            using (Bitmap cropped = preview.Clone(crop, preview.PixelFormat))
            using (Bitmap norm = Normalize(cropped, 64, 64))
            {
                return Similarity(Fingerprint(query), Fingerprint(norm));
            }
        }
    }
}
