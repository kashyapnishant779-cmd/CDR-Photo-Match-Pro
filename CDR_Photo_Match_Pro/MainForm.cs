using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CDR_Photo_Match_Pro
{
    public partial class MainForm : Form
    {
        private string _photoPath;
        private readonly List<MatchResult> _results = new List<MatchResult>();
        private MatchingEngine _engine;
        private bool _scanning;

        public MainForm()
        {
            InitializeComponent();
            AllowDrop = true;
            DragEnter += MainForm_DragEnter;
            DragDrop += MainForm_DragDrop;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            foreach (string f in FolderManager.Load()) lstFolders.Items.Add(f);
        }

        private void btnSelectPhoto_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
                ofd.Title = "Select Design Photo";
                if (ofd.ShowDialog() == DialogResult.OK) LoadPhoto(ofd.FileName);
            }
        }

        private void LoadPhoto(string path)
        {
            try
            {
                if (picPhoto.Image != null) picPhoto.Image.Dispose();
                _photoPath = path;
                picPhoto.Image = new Bitmap(path);
                lblStatus.Text = "Photo selected: " + Path.GetFileName(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Photo load nahi ho payi: " + ex.Message);
            }
        }

        private void btnAddFolder_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select CDR Folder";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    if (!lstFolders.Items.Contains(fbd.SelectedPath)) lstFolders.Items.Add(fbd.SelectedPath);
                    SaveFolders();
                }
            }
        }

        private void btnRemoveFolder_Click(object sender, EventArgs e)
        {
            if (lstFolders.SelectedItem != null)
            {
                lstFolders.Items.Remove(lstFolders.SelectedItem);
                SaveFolders();
            }
        }

        private async void btnScan_Click(object sender, EventArgs e)
        {
            if (_scanning) return;
            if (string.IsNullOrEmpty(_photoPath) || !File.Exists(_photoPath))
            {
                MessageBox.Show("Pehle JPG/PNG/BMP photo select karo.");
                return;
            }
            if (lstFolders.Items.Count == 0)
            {
                MessageBox.Show("Pehle CDR folder add karo.");
                return;
            }

            _results.Clear();
            gridResults.Rows.Clear();
            progressBar.Value = 0;
            _scanning = true;
            SetButtons(false);
            _engine = new MatchingEngine();
            _engine.ProgressChanged += Engine_ProgressChanged;
            _engine.ResultFound += Engine_ResultFound;
            var folders = lstFolders.Items.Cast<string>().ToList();
            lblStatus.Text = "Scanning started...";

            try
            {
                var finalResults = await Task<List<MatchResult>>.Factory.StartNew(() => _engine.Scan(_photoPath, folders));
                _results.Clear();
                _results.AddRange(finalResults);
                RefreshGrid();
                lblStatus.Text = "Done. Matches found: " + _results.Count;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Scan error: " + ex.Message);
            }
            finally
            {
                _scanning = false;
                SetButtons(true);
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (_engine != null) _engine.Cancel();
            lblStatus.Text = "Stopping scan...";
        }

        private void Engine_ProgressChanged(int done, int total, string file)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<int, int, string>(Engine_ProgressChanged), done, total, file);
                return;
            }
            progressBar.Maximum = Math.Max(1, total);
            progressBar.Value = Math.Min(done, progressBar.Maximum);
            lblStatus.Text = string.Format("Scanning {0}/{1}: {2}", done, total, Path.GetFileName(file));
        }

        private void Engine_ResultFound(MatchResult r)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<MatchResult>(Engine_ResultFound), r);
                return;
            }
            _results.Add(r);
            gridResults.Rows.Add(r.MatchPercent.ToString("0.00"), r.FileName, r.FullPath, r.PreviewStatus);
        }

        private void RefreshGrid()
        {
            gridResults.Rows.Clear();
            foreach (var r in _results.OrderByDescending(x => x.MatchPercent))
                gridResults.Rows.Add(r.MatchPercent.ToString("0.00"), r.FileName, r.FullPath, r.PreviewStatus);
        }

        private string SelectedPath()
        {
            if (gridResults.SelectedRows.Count == 0) return null;
            return Convert.ToString(gridResults.SelectedRows[0].Cells[2].Value);
        }

        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            string path = SelectedPath();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            Process.Start("explorer.exe", "/select,\"" + path + "\"");
        }

        private void btnCopyPath_Click(object sender, EventArgs e)
        {
            string path = SelectedPath();
            if (!string.IsNullOrEmpty(path))
            {
                Clipboard.SetText(path);
                lblStatus.Text = "Path copied.";
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (_results.Count == 0)
            {
                MessageBox.Show("Export ke liye pehle scan result chahiye.");
                return;
            }
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV File|*.csv|Text File|*.txt";
                sfd.FileName = "CDR_Match_Results.csv";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    if (Path.GetExtension(sfd.FileName).Equals(".txt", StringComparison.OrdinalIgnoreCase))
                        ExportHelper.ExportTxt(sfd.FileName, _results.OrderByDescending(x => x.MatchPercent));
                    else
                        ExportHelper.ExportCsv(sfd.FileName, _results.OrderByDescending(x => x.MatchPercent));
                    lblStatus.Text = "Results exported.";
                }
            }
        }

        private void SaveFolders()
        {
            FolderManager.Save(lstFolders.Items.Cast<string>());
        }

        private void SetButtons(bool enabled)
        {
            btnSelectPhoto.Enabled = enabled;
            btnAddFolder.Enabled = enabled;
            btnRemoveFolder.Enabled = enabled;
            btnScan.Enabled = enabled;
            btnStop.Enabled = !enabled;
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0) return;
            string ext = Path.GetExtension(files[0]).ToLowerInvariant();
            if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp") LoadPhoto(files[0]);
            else if (Directory.Exists(files[0]) && !lstFolders.Items.Contains(files[0])) { lstFolders.Items.Add(files[0]); SaveFolders(); }
        }
    }
}
