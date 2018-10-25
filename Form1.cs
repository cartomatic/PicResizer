using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cartomatic.Utils.Drawing;

namespace Cartomatic
{
    public partial class PicResizer : Form
    {
        public PicResizer()
        {
            InitializeComponent();

            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;
            backgroundWorker1.DoWork += BackgroundWorker1_DoWork;
            backgroundWorker1.ProgressChanged += BackgroundWorker1_ProgressChanged;
            backgroundWorker1.RunWorkerCompleted += BackgroundWorker1_RunWorkerCompleted;
        }

        

        /// <summary>
        /// Whether or not the resizing is in progress
        /// </summary>
        private bool ResizingInProgress { get; set; }

        private void btnFilePicker_Click(object sender, EventArgs e)
        {
            //folderBrowserDialog1.RootFolder = Environment.SpecialFolder.;
            folderBrowserDialog1.ShowDialog();


            txtFilePath.Text = Directory.Exists(folderBrowserDialog1.SelectedPath) ? folderBrowserDialog1.SelectedPath : string.Empty;

            txtInfo.Text = string.Empty;
        }

        private bool CancelPromptOn { get; set; }

        private void btnClose_Click(object sender, EventArgs e)
        {
            if (ResizingInProgress)
            {
                CancelPromptOn = true;

                var dialogResult = MessageBox.Show("Are you sure you want to cancel the resizing process?",
                    "Resizing in progress", MessageBoxButtons.OKCancel);

                if (dialogResult == DialogResult.OK)
                {
                    ForceStopResizer();
                }

                CancelPromptOn = false;
            }
            else
            {
                PicResizer.ActiveForm?.Close();
            }
        }

        /// <summary>
        /// Force stops the resizer
        /// </summary>
        private void ForceStopResizer()
        {
            if(backgroundWorker1.IsBusy && !backgroundWorker1.CancellationPending)
                backgroundWorker1.CancelAsync();
        }

        /// <summary>
        /// Finalizes harvester
        /// </summary>
        private void FinaliseResizer()
        {
            ResizingInProgress = false;
            btnClose.Text = "Close";

            btnProcess.Enabled = true;
            btnFilePicker.Enabled = true;

            progressBar1.Value = 0;
        }

        /// <summary>
        /// Initiates data processing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnProcess_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(folderBrowserDialog1.SelectedPath))
            {
                MessageBox.Show("You must select a folder first");
                return;
            }

            btnProcess.Enabled = false;
            btnFilePicker.Enabled = false;
            ResizingInProgress = true;
            btnClose.Text = "Cancel";

            ProcessFiles(folderBrowserDialog1.SelectedPath);
        }

        
        private string[] FilesToProcess { get; set; }

        private int WidthPx { get; set; }

        private int HeightPx { get; set; }

        private void ProcessFiles(string path)
        {
            FilesToProcess = new []{"*.jpg", "*.png"}
                .SelectMany(g => Directory.EnumerateFiles(path, g))
                .ToArray();

            if (FilesToProcess.Length == 0)
            {
                MessageBox.Show("Nothing to process dude!");
                FinaliseResizer();
                return;
            }

            WidthPx = (int)numericUpDown1.Value;
            HeightPx = (int) numericUpDown2.Value;

            progressBar1.Minimum = 0;
            progressBar1.Maximum = 100;


            backgroundWorker1.RunWorkerAsync();
        }

        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                txtInfo.AppendText(Environment.NewLine + "Cancelled!");
            }
            else
            {
                txtInfo.AppendText( Environment.NewLine + "Finished!");
            }

            FinaliseResizer();
        }

        private void BackgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            txtInfo.AppendText((string)e.UserState);
  
        }


        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {

            for (var f = 0; f < FilesToProcess.Length; f++)
            {
                while (CancelPromptOn)
                {
                    Thread.Sleep(250);
                }

                if (backgroundWorker1.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }


                var prcntg = (int)Math.Ceiling((double) f / FilesToProcess.Length * 100);
                var file = FilesToProcess[f];

                backgroundWorker1.ReportProgress(prcntg, $"Processing file {file}..." + Environment.NewLine);

                backgroundWorker1.ReportProgress(prcntg, "Reading file... ");

                var img = System.Drawing.Image.FromFile(file);

                backgroundWorker1.ReportProgress(prcntg, "Done!" + Environment.NewLine);


                backgroundWorker1.ReportProgress(prcntg, "Applying exif rotation... ");
                
                img = ((Bitmap) img).ApplyExifRotation();

                backgroundWorker1.ReportProgress(prcntg, "Done!" + Environment.NewLine);


                backgroundWorker1.ReportProgress(prcntg, "Resizing file... ");

                var resized = ((Bitmap) img).Resize(WidthPx, HeightPx);

                backgroundWorker1.ReportProgress(prcntg, "Done!" + Environment.NewLine);



                backgroundWorker1.ReportProgress(prcntg, "Saving file... ");

                var dir = Path.GetDirectoryName(file);
                var resizedDir = Path.Combine(dir, "resized");
                if (!Directory.Exists(resizedDir))
                {
                    Directory.CreateDirectory(resizedDir);
                }

                resized.Save(Path.Combine(resizedDir, Path.GetFileNameWithoutExtension(file)) + ".png", ImageFormat.Png);

                backgroundWorker1.ReportProgress(prcntg, "Done!" + Environment.NewLine + "-----------------------------------------------" + Environment.NewLine + Environment.NewLine);

                img.Dispose();
                resized.Dispose();

                Thread.Sleep(250);
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
