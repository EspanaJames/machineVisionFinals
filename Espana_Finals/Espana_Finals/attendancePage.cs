using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Controls;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Espana_Finals
{
    public partial class attendancePage : Form
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private CascadeClassifier faceDetector;
        private bool isCameraRunning = false;
        private int frameSkip = 0;
        public attendancePage()
        {
            InitializeComponent();
            machineVisionBox.Hide();
            faceDetector = new CascadeClassifier(@"C:\Users\james\Downloads\haarcascade_frontalface_default.xml");
        }

        private void siticonePictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void siticoneButton1_Click(object sender, EventArgs e)
        {
            machineVisionBox.Show();
            userPictureBox.Hide();
            attendanceBox.Hide();
            if (!isCameraRunning)
            {
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (videoDevices.Count == 0)
                {
                    MessageBox.Show("No camera found.");
                    return;
                }

                videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                videoSource.NewFrame += new NewFrameEventHandler(VideoSource_NewFrame);
                videoSource.Start();
                isCameraRunning = true;
            }
            else
            {
                if (videoSource != null && videoSource.IsRunning)
                {
                    videoSource.SignalToStop();
                    videoSource.WaitForStop();
                }

                isCameraRunning = false;
            }
        }
        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap rawFrame = (Bitmap)eventArgs.Frame.Clone();
            Bitmap processedFrame = (Bitmap)rawFrame.Clone();

            if (frameSkip++ % 3 == 0)
            {
                using (Mat mat = BitmapConverter.ToMat(rawFrame))
                {
                    Mat gray = new Mat();
                    Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);

                    Rect[] faces = faceDetector.DetectMultiScale(gray, 1.1, 4);

                    foreach (var face in faces)
                    {
                        Cv2.Rectangle(mat, face, Scalar.Red, 2);
                    }

                    processedFrame.Dispose();
                    processedFrame = BitmapConverter.ToBitmap(mat);
                }
            }

            if (machineVisionBox.InvokeRequired)
            {
                machineVisionBox.Invoke(new MethodInvoker(() =>
                {
                    if (machineVisionBox.Image != null)
                        machineVisionBox.Image.Dispose();
                    machineVisionBox.Image = processedFrame;
                }));
            }
            else
            {
                if (machineVisionBox.Image != null)
                    machineVisionBox.Image.Dispose();
                machineVisionBox.Image = processedFrame;
            }

            rawFrame.Dispose();
        }
        private void loginPage_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
            }
        }
        private void siticoneButton2_Click(object sender, EventArgs e)
        {

        }

        private void siticoneButton3_Click(object sender, EventArgs e)
        {

        }
    }
}
