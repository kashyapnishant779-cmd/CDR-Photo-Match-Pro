using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace CDR_Photo_Match_Pro
{
    public static class CdrThumbnailExtractor
    {
        public static Bitmap TryExtractPreview(string cdrPath, out string status)
        {
            status = "Preview not found";
            try
            {
                string folder = Path.GetDirectoryName(cdrPath);
                string name = Path.GetFileNameWithoutExtension(cdrPath);
                string[] sideCars = new[]
                {
                    Path.Combine(folder, name + ".jpg"),
                    Path.Combine(folder, name + ".jpeg"),
                    Path.Combine(folder, name + ".png"),
                    Path.Combine(folder, name + ".bmp")
                };

                foreach (string img in sideCars)
                {
                    if (File.Exists(img))
                    {
                        status = "Matched using sidecar preview image";
                        using (var temp = new Bitmap(img)) return new Bitmap(temp);
                    }
                }

                byte[] data = File.ReadAllBytes(cdrPath);
                Bitmap embedded = FindEmbeddedBitmap(data);
                if (embedded != null)
                {
                    status = "Matched using embedded preview";
                    return embedded;
                }
            }
            catch (Exception ex)
            {
                status = "Preview error: " + ex.Message;
            }
            return null;
        }

        private static Bitmap FindEmbeddedBitmap(byte[] data)
        {
            byte[][] starts = new byte[][]
            {
                new byte[] {0xFF, 0xD8, 0xFF},
                new byte[] {0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A},
                new byte[] {0x42, 0x4D}
            };

            for (int i = 0; i < data.Length - 16; i++)
            {
                foreach (var sig in starts)
                {
                    if (StartsAt(data, i, sig))
                    {
                        for (int len = Math.Min(data.Length - i, 800000); len > 1024; len -= 512)
                        {
                            try
                            {
                                using (var ms = new MemoryStream(data, i, len))
                                using (var img = Image.FromStream(ms, false, false))
                                {
                                    if (img.Width >= 32 && img.Height >= 32)
                                        return new Bitmap(img);
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            return null;
        }

        private static bool StartsAt(byte[] data, int index, byte[] sig)
        {
            if (index + sig.Length >= data.Length) return false;
            for (int j = 0; j < sig.Length; j++) if (data[index + j] != sig[j]) return false;
            return true;
        }
    }
}
