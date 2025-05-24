using AForge.Controls;
using AForge.Video;
using AForge.Video.DirectShow;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Espana_Finals
{
    public partial class attendancePage : Form
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private CascadeClassifier faceDetector;
        private bool isCameraRunning = false;
        private string eventNameHolder;
        private DataBase db = new DataBase();

        private List<Mat> knownFaces = new List<Mat>();
        private List<(string IdCard, string FullName)> knownLabels = new List<(string, string)>();
        private Dictionary<string, DateTime> recentRecognitions = new Dictionary<string, DateTime>();
        private const int RecognitionCooldownSeconds = 5;

        public attendancePage(string eventName)
        {
            InitializeComponent();
            var cascadeBytes = Espana_Finals.Resource1.frontalFile;
            var cascadePath = Path.GetTempFileName();
            File.WriteAllBytes(cascadePath, cascadeBytes);
            faceDetector = new CascadeClassifier(cascadePath);
            LoadKnownFacesFromDatabase();
            LoadCascadeAndStartCamera();
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

        private void LoadCascadeAndStartCamera()
        {
            var cascadeBytes = Espana_Finals.Resource1.frontalFile;
            var cascadePath = Path.GetTempFileName();
            File.WriteAllBytes(cascadePath, cascadeBytes);
            faceDetector = new CascadeClassifier(cascadePath);
            StartCamera();
        }

        private void LoadKnownFacesFromDatabase()
        {
            string query = "SELECT idCard, firstName, lastName, picture FROM students";
            DataTable dt = db.ExecuteSelectQuery(query);

            foreach (DataRow row in dt.Rows)
            {
                try
                {
                    byte[] imageBytes = (byte[])row["picture"];
                    using (var ms = new MemoryStream(imageBytes))
                    {
                        Bitmap bmp = new Bitmap(ms);
                        Mat faceMat = BitmapConverter.ToMat(bmp);
                        Cv2.CvtColor(faceMat, faceMat, ColorConversionCodes.BGR2GRAY);
                        knownFaces.Add(faceMat);
                        string fullName = $"{row["firstName"]} {row["lastName"]}";
                        knownLabels.Add((row["idCard"].ToString(), fullName));
                    }
                }
                catch { }
            }
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
                Mat frame = BitmapConverter.ToMat(rawFrame);
                Mat gray = new Mat();
                Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
                var faces = faceDetector.DetectMultiScale(gray, 1.1, 5);

                foreach (var rect in faces)
                {
                    Mat detectedFace = new Mat(gray, rect);
                    Cv2.Resize(detectedFace, detectedFace, new OpenCvSharp.Size(100, 100));

                    string matchedName = "Unknown";
                    string matchedId = "";
                    double minDist = double.MaxValue;

                    for (int i = 0; i < knownFaces.Count; i++)
                    {
                        double dist = Cv2.Norm(knownFaces[i], detectedFace);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            matchedName = knownLabels[i].FullName;
                            matchedId = knownLabels[i].IdCard;
                        }
                    }

                    if (minDist < 3000)
                    {
                        string key = matchedId;
                        if (!recentRecognitions.ContainsKey(key) || (DateTime.Now - recentRecognitions[key]).TotalSeconds > RecognitionCooldownSeconds)
                        {
                            recentRecognitions[key] = DateTime.Now;
                            Console.WriteLine($"Recognized: {matchedName} ({matchedId})");
                        }
                        Cv2.Rectangle(frame, rect, Scalar.Red, 2);
                        Cv2.PutText(frame, $"{matchedName} ({matchedId})", new OpenCvSharp.Point(rect.X, rect.Y - 10), HersheyFonts.HersheySimplex, 0.6, Scalar.Red, 2);
                    }
                    else
                    {
                        Cv2.Rectangle(frame, rect, Scalar.Blue, 2);
                        Cv2.PutText(frame, "Unknown", new OpenCvSharp.Point(rect.X, rect.Y - 10), HersheyFonts.HersheySimplex, 0.6, Scalar.Blue, 2);
                    }
                }

                Bitmap processedFrame = BitmapConverter.ToBitmap(frame);

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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
            }
            base.OnFormClosing(e);
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
