using AForge.Controls;
using AForge.Video;
using AForge.Video.DirectShow;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Espana_Finals
{
    public partial class attendancePage : Form
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private CascadeClassifier faceDetector;
        private bool isCameraRunning = false;
        private string eventNameHolder;

        public attendancePage(string eventName)
        {
            InitializeComponent();
            var cascadeBytes = Espana_Finals.Resource1.frontalFile;
            var cascadePath = Path.GetTempFileName();
            File.WriteAllBytes(cascadePath, cascadeBytes);
            faceDetector = new CascadeClassifier(cascadePath);
            eventNameHolder = eventName;
            siticoneLabel4.Text = eventNameHolder;
        }

        private void StartCamera()
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (videoDevices.Count == 0) return;
            videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
            videoSource.NewFrame += VideoSource_NewFrame;
            videoSource.Start();
            isCameraRunning = true;
        }

        private void StopCamera()
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
            }
            isCameraRunning = false;
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            using (Bitmap rawFrame = (Bitmap)eventArgs.Frame.Clone())
            {
                Mat mat = BitmapConverter.ToMat(rawFrame);
                Mat gray = new Mat();
                Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.EqualizeHist(gray, gray);
                Rect[] faces = faceDetector.DetectMultiScale(gray, 1.1, 5);
                foreach (var face in faces)
                {
                    Cv2.Rectangle(mat, face, Scalar.Red, 2);
                    break;
                }
                Bitmap processedFrame = BitmapConverter.ToBitmap(mat);
                if (machineVisionBox.InvokeRequired)
                {
                    machineVisionBox.Invoke(new MethodInvoker(() =>
                    {
                        machineVisionBox.Image?.Dispose();
                        machineVisionBox.Image = processedFrame;
                    }));
                }
                else
                {
                    machineVisionBox.Image?.Dispose();
                    machineVisionBox.Image = processedFrame;
                }
            }
        }

        private void attendancePage_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopCamera();
        }

        private void siticoneButton2_Click(object sender, EventArgs e)
        {
            if (!isCameraRunning) StartCamera();
        }

        private void siticoneButton3_Click(object sender, EventArgs e)
        {
            StopCamera();
        }
    }
}
