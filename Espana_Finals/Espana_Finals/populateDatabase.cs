using AForge.Video;
using AForge.Video.DirectShow;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Espana_Finals
{
    public partial class populateDatabase : Form
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private CascadeClassifier faceDetector;
        private bool isCameraRunning = false;
        private DataBase db = new DataBase();
        private Dictionary<string, Mat> studentFaces = new Dictionary<string, Mat>();
        private Dictionary<string, (string FirstName, string LastName)> studentNames = new Dictionary<string, (string, string)>();
        private Dictionary<string, DateTime> lastRecognitionTimes = new Dictionary<string, DateTime>();
        private string eventNameHolder;

        public populateDatabase(string eventName)
        {
            InitializeComponent();
            siticoneShimmerLabel3.Text = eventName;
            this.eventNameHolder = eventName;
            var cascadeBytes = Espana_Finals.Resource1.frontalFile;
            var cascadePath = Path.GetTempFileName();
            File.WriteAllBytes(cascadePath, cascadeBytes);
            faceDetector = new CascadeClassifier(cascadePath);
            startCameraAndDetect();
        }

        private void siticoneButton7_Click_1(object sender, EventArgs e)
        {
            string idCard = siticoneTextBox3.Text.Trim();
            string firstName = siticoneTextBox1.Text.Trim();
            string lastName = siticoneTextBox2.Text.Trim();
            string eventName = this.eventNameHolder;

            byte[] picture = CaptureAndPrepareFaceBlob();
            if (picture == null) return;

            string insertQuery = "INSERT INTO students (idCard, firstName, lastName, picture, eventName) VALUES (@idCard, @firstName, @lastName, @picture, @eventName)";
            var parameters = new Dictionary<string, object>
            {
                {"@idCard", idCard},
                {"@firstName", firstName},
                {"@lastName", lastName},
                {"@picture", picture},
                {"@eventName", eventName}
            };

            db.ExecuteQueryWithParameters(insertQuery, parameters);
            this.Hide();
        }

        private byte[] CaptureAndPrepareFaceBlob()
        {
            if (machineVisionBox.Image == null)
            {
                MessageBox.Show("No image available.");
                return null;
            }

            Bitmap currentFrame = new Bitmap(machineVisionBox.Image);
            Mat colorMat = BitmapConverter.ToMat(currentFrame);
            Mat gray = new Mat();
            Cv2.CvtColor(colorMat, gray, ColorConversionCodes.BGR2GRAY);
            Cv2.EqualizeHist(gray, gray);

            var faces = faceDetector.DetectMultiScale(gray, 1.1, 5);
            if (faces.Length == 0)
            {
                MessageBox.Show("No face detected.");
                return null;
            }

            var largestFace = faces.OrderByDescending(f => f.Width * f.Height).First();
            Mat colorFace = new Mat(colorMat, largestFace);
            Cv2.Resize(colorFace, colorFace, new OpenCvSharp.Size(100, 100));

            using (MemoryStream ms = new MemoryStream())
            {
                Bitmap bmpToSave = BitmapConverter.ToBitmap(colorFace);
                bmpToSave.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                return ms.ToArray();
            }
        }

        private void startCameraAndDetect()
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (videoDevices.Count == 0)
            {
                MessageBox.Show("No camera found.");
                return;
            }

            videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
            videoSource.NewFrame += (s, e) =>
            {
                using (Bitmap frame = (Bitmap)e.Frame.Clone())
                {
                    Mat mat = BitmapConverter.ToMat(frame);
                    Mat gray = new Mat();
                    Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);
                    Cv2.EqualizeHist(gray, gray);
                    var faces = faceDetector.DetectMultiScale(gray, 1.1, 5);

                    if (faces.Length > 0)
                    {
                        Bitmap detected = BitmapConverter.ToBitmap(mat);
                        machineVisionBox.Image = (Bitmap)detected.Clone();
                    }
                }
            };

            videoSource.Start();
            isCameraRunning = true;
        }
    }
}
